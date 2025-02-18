using Azure.Search.Documents.Indexes;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using Azure.Search.Documents.Indexes.Models;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using OpenAI.Embeddings;
using System.Diagnostics;

namespace equipment_classification_agent_api.Services;

public interface IAzureAISearchService
{
    Task<SearchIndex> CreateAISearchIndexAsync();
    Task IndexDataAsync(SearchIndex searchIndex);
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
    private readonly IAzureSQLService _azureSQLService;

    public AzureAISearchService(
        ILogger<AzureAISearchService> logger,
        IOptions<AzureAISearchOptions> azureAISearchOptions,
        IOptions<AzureOpenAIOptions> azureOpenAIOptions,
        IAzureSQLService azureSQLService)
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
        _azureSQLService = azureSQLService;
    }

    public async Task<SearchIndex> CreateAISearchIndexAsync()
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
                new SearchableField("country") { IsFilterable = true },
                new SearchableField("manufacturer") { IsFilterable = true, IsSortable = true },
                new SearchableField("usga_lot_num") { IsFilterable = true },
                new SearchableField("pole_marking") { IsFilterable = true },
                new SearchableField("pole1_web") { IsFilterable = true },
                new SearchableField("colour") { IsFilterable = true },
                new SearchableField("constCode") { IsFilterable = true },
                new SearchableField("woundcode") { IsFilterable = true },
                new SearchableField("centercode") { IsFilterable = true },
                new SearchableField("covercode") { IsFilterable = true },
                new SearchableField("ballspecs1") { IsFilterable = true },
                new SearchableField("ballSpecs") { IsFilterable = true },
                new SimpleField("dimples", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchableField("spin") { IsFilterable = true },
                new SearchableField("pole_2") { IsFilterable = true },
                new SearchableField("pole2_web") { IsFilterable = true },
                new SearchableField("seam_marking") { IsFilterable = true },
                new SearchableField("seam1_web") { IsFilterable = true },
                new SearchableField("seam_2"){ IsFilterable = true },
                new SearchableField("seam2_web") { IsFilterable = true },
                new SearchableField("DecisionNumber") { IsFilterable = true },
                new SimpleField("imageUrl", SearchFieldDataType.String) { IsFilterable = false },
                new SearchField("vectorContent", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = int.Parse(_azureOpenAIEmbeddingDimensions),
                    VectorSearchProfileName = vectorSearchHnswProfile
                }
            }
        };

        _logger.LogInformation($"Creating index {searchIndex}");

        await searchIndexClient.CreateOrUpdateIndexAsync(searchIndex);

        _logger.LogInformation($"Completed creating index {searchIndex}");

        return searchIndex;
    }

    public async Task IndexDataAsync(SearchIndex searchIndex)
    {
        var stopwatch = Stopwatch.StartNew();
        var searchClient = new SearchClient(new Uri(_searchServiceEndpoint), searchIndex.Name, new AzureKeyCredential(_searchAdminKey));

        AzureOpenAIClient _azureOpenAIClient = new(
            new Uri(_azureOpenAIEndpoint),
            new AzureKeyCredential(_azureOpenAIKey));

        var golfBalls = await _azureSQLService.GetGolfBallsAsync();
        var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(_azureOpenAIEmbeddingModel);

        foreach (var golfBall in golfBalls)
        {
            var textForEmbedding = $"Manufacturer: {golfBall.Manufacturer}, " +
                   $"Pole Marking: {golfBall.Pole_Marking}, " +
                   $"Color: {golfBall.Colour}, " +
                   $"Seam Marking: {golfBall.Seam_Marking}";

            OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(textForEmbedding);
            golfBall.VectorContent = embedding.ToFloats().ToArray().ToList();
        }

        var batch = IndexDocumentsBatch.Upload(golfBalls);
        var result = await searchClient.IndexDocumentsAsync(batch);
        _logger.LogInformation($"Indexed {golfBalls.Count} golf balls.");
        stopwatch.Stop();
        _logger.LogInformation($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }
}