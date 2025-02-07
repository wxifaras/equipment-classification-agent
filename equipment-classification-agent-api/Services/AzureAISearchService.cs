using Azure.Search.Documents.Indexes;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using Azure.Search.Documents.Indexes.Models;

namespace equipment_classification_agent_api.Services;

public interface IAzureAISearchService
{
    Task CreateAISearchIndexAsync();
    Task IndexDataAsync();
}

public class AzureAISearchService : IAzureAISearchService
{
    const string vectorSearchHnswProfile = "golf-vector-profile";
    const string vectorSearchHnswConfig = "golfHnsw";
    const string vectorSearchVectorizer = "golfOpenAIVectorizer";
    const string semanticSearchConfig = "golf-semantic-config";

    private readonly ILogger<AzureAISearchService> _logger;
    private readonly string _searchServiceEndpoint;
    private readonly string _searchAdminKey;
    private readonly string _indexName;
    private readonly string _azureOpenAIEndpoint;
    private readonly string _azureOpenAIKey;
    private readonly string _azureOpenAIEmbeddingDimensions;
    private readonly string _azureOpenAIEmbeddingModel;
    private readonly string _azureOpenAIEmbeddingDeployment;

    public AzureAISearchService(
        IOptions<AzureAISearchOptions> azureAISearchOptions, 
        IOptions<AzureOpenAIOptions> azureOpenAIOptions,
        ILogger<AzureAISearchService> logger)
    {
        _searchServiceEndpoint = azureAISearchOptions.Value.SearchServiceEndpoint;
        _searchAdminKey = azureAISearchOptions.Value.SearchAdminKey;
        _indexName = azureAISearchOptions.Value.IndexName;

        _azureOpenAIEndpoint = azureOpenAIOptions.Value.AzureOpenAIEndPoint;
        _azureOpenAIKey = azureOpenAIOptions.Value.AzureOpenAIKey;
        _azureOpenAIEmbeddingDimensions = azureOpenAIOptions.Value.AzureOpenAIEmbeddingDimensions;
        _azureOpenAIEmbeddingModel = azureOpenAIOptions.Value.AzureOpenAIEmbeddingModel;
        _azureOpenAIEmbeddingDeployment = azureOpenAIOptions.Value.AzureOpenAIEmbeddingDeployment;

        _logger = logger;
    }

    public async Task CreateAISearchIndexAsync()
    {
        var searchIndexClient = new SearchIndexClient(
        new Uri(_searchServiceEndpoint),
        new AzureKeyCredential(_searchAdminKey));

        SearchIndex searchIndex = new(_indexName)
        {
            VectorSearch = new()
            {
                Profiles =
                {
                    new VectorSearchProfile(vectorSearchHnswProfile, vectorSearchHnswConfig)
                    {
                        VectorizerName = vectorSearchVectorizer
                    }
                },
                Algorithms =
                {
                    new HnswAlgorithmConfiguration(vectorSearchHnswConfig)
                    {
                        Parameters = new HnswParameters
                        {
                            M = 4,
                            EfConstruction = 400,
                            EfSearch = 500,
                            Metric = "cosine"
                        }
                    }
                },
                Vectorizers =
                {
                    new AzureOpenAIVectorizer(vectorSearchVectorizer)
                    {
                        Parameters = new AzureOpenAIVectorizerParameters
                        {
                            ResourceUri = new Uri(_azureOpenAIEndpoint),
                            ModelName = _azureOpenAIEmbeddingModel,
                            DeploymentName = _azureOpenAIEmbeddingDeployment,
                            ApiKey = _azureOpenAIKey
                        }
                    }
                }
            },
            SemanticSearch = new()
            {
                Configurations =
                {
                    new SemanticConfiguration(semanticSearchConfig, new()
                    {
                        TitleField = new SemanticField("manufacturer"),
                        ContentFields =
                        {
                            new SemanticField("pole_marking"),
                            new SemanticField("seam_marking")
                        }
                    })
                }
            },
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true, IsFilterable = true },
                new SearchableField("manufacturer") { IsFilterable = true, IsSortable = true },
                new SearchableField("usga_lot_num") { IsFilterable = true },
                new SearchableField("pole_marking") { IsFilterable = true },
                new SearchableField("colour") { IsFilterable = true },
                new SearchableField("constCode") { IsFilterable = true },
                new SearchableField("ballSpecs") { IsFilterable = true },
                new SimpleField("dimples", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchableField("spin") { IsFilterable = true },
                new SearchableField("pole_2") { IsFilterable = true },
                new SearchableField("seam_marking") { IsFilterable = true },
                new SimpleField("imageUrl", SearchFieldDataType.String) { IsFilterable = false },
                new SearchField("vectorContent", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = int.Parse(_azureOpenAIEmbeddingDimensions),
                    VectorSearchProfileName = vectorSearchHnswProfile
                }
            }
        };

        await searchIndexClient.CreateOrUpdateIndexAsync(searchIndex);
    }

    public Task IndexDataAsync()
    {
        throw new NotImplementedException();
    }
}