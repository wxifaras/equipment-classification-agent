using Azure.Search.Documents.Indexes;
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
    Task CreateAISearchIndexAsync();
    Task IndexDataAsync();

    Task<List<GolfBallAISearch>> SearchGolfBallAsync(
           string query,
           int k = 3,
           int top = 3, // top 3 results
           string? filter = null,
           bool textOnly = false,
           bool hybrid = true,
           bool semantic = false,
           double minRerankerScore = 2.0);
}

public class AzureAISearchService : IAzureAISearchService
{
    const string vectorSearchHnswProfile = "golf-vector-profile";
    const string vectorSearchHnswConfig = "golfHnsw";
    const string vectorSearchVectorizer = "golfOpenAIVectorizer";
    const string semanticSearchConfig = "golf-semantic-config";

    private readonly ILogger<AzureAISearchService> _logger;
    private readonly string _indexName;
    private readonly string _azureOpenAIEndpoint;
    private readonly string _azureOpenAIKey;
    private readonly string _azureOpenAIEmbeddingDimensions;
    private readonly string _azureOpenAIEmbeddingModel;
    private readonly string _azureOpenAIEmbeddingDeployment;
    private readonly IAzureSQLService _azureSQLService;
    private readonly SearchIndexClient _indexClient;
    private readonly AzureOpenAIClient _azureOpenAIClient;
    private readonly SearchClient _searchClient;

    public AzureAISearchService(
        ILogger<AzureAISearchService> logger,
        IOptions<AzureAISearchOptions> azureAISearchOptions,
        IOptions<AzureOpenAIOptions> azureOpenAIOptions,
        IAzureSQLService azureSQLService,
        SearchIndexClient indexClient,
        AzureOpenAIClient azureOpenAIClient,
        SearchClient searchClient)
    {
        _indexName = azureAISearchOptions.Value.IndexName;

        _azureOpenAIEndpoint = azureOpenAIOptions.Value.AzureOpenAIEndPoint;
        _azureOpenAIKey = azureOpenAIOptions.Value.AzureOpenAIKey;
        _azureOpenAIEmbeddingDimensions = azureOpenAIOptions.Value.AzureOpenAIEmbeddingDimensions;
        _azureOpenAIEmbeddingModel = azureOpenAIOptions.Value.AzureOpenAIEmbeddingModel;
        _azureOpenAIEmbeddingDeployment = azureOpenAIOptions.Value.AzureOpenAIEmbeddingDeployment;
        _indexClient = indexClient;
        _azureOpenAIClient = azureOpenAIClient;
        _searchClient = searchClient;

        _logger = logger;
        _azureSQLService = azureSQLService;
    }

    public async Task CreateAISearchIndexAsync()
    {
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
                },//text to vectorized representation
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
            },// Config Semantic Search for better NLP
            SemanticSearch = new()
            {
                Configurations =
                {
                    new SemanticConfiguration(semanticSearchConfig, new()
                    {
                        ContentFields =
                        {
                            new SemanticField("manufacturer"),
                            new SemanticField("colour"),
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
                    VectorSearchDimensions = int.Parse(_azureOpenAIEmbeddingDimensions!),
                    VectorSearchProfileName = vectorSearchHnswProfile
                }
            }
        };

        var indexNames = _indexClient.GetIndexNames();

        // Check if the specified index exists
        bool indexExists = indexNames.Contains(_indexName);

        if (indexExists)
        {
            await _indexClient.DeleteIndexAsync(_indexName);
        }

        await _indexClient.CreateOrUpdateIndexAsync(searchIndex);

        _logger.LogInformation($"Completed creating index {searchIndex}");
    }

    public async Task IndexDataAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var golfBalls = await _azureSQLService.GetGolfBallsAsync();

        if (golfBalls == null || golfBalls.Count == 0)
        {
            throw new ArgumentException("No golf ball data found in SQL.");
        }

        var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(_azureOpenAIEmbeddingDeployment);

        foreach (var golfBall in golfBalls)
        {
            string textForEmbedding = $"manufacturer: {golfBall.Manufacturer}, " +
                                      $"pole_marking: {golfBall.Pole_Marking}, " +
                                      $"colour: {golfBall.Colour}, " +
                                      $"seam_marking: {golfBall.Seam_Marking}";

            OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(textForEmbedding);
            golfBall.VectorContent = embedding.ToFloats().ToArray().ToList();
        }

        var batch = IndexDocumentsBatch.Upload(golfBalls);

        var result = await _searchClient.IndexDocumentsAsync(batch);

        _logger.LogInformation($"Indexed {golfBalls.Count} golf balls.");
        stopwatch.Stop();
        _logger.LogInformation($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
    }

    public async Task<List<GolfBallAISearch>> SearchGolfBallAsync(
           string query,
           int k = 3,
           int top = 3, // top 3 results
           string? filter = null,
           bool textOnly = false,
           bool hybrid = true,
           bool semantic = false,
           double minRerankerScore = 2.0)
    {
        var searchOptions = new SearchOptions
        {
            Filter = filter,
            Size = top,
            Select = { "id", "manufacturer", "pole_marking", "usga_lot_num", "constCode", "ballSpecs", "dimples", "spin", "pole_2", "colour", "seam_marking", "imageUrl" },
            IncludeTotalCount = true
        };

        if (!textOnly)
        {
            searchOptions.VectorSearch = new()
            {
                Queries = {
                    new VectorizableTextQuery(text: query)
                    {
                        KNearestNeighborsCount = k,
                        Fields = { "vectorContent" }
                    }
                }
            };
        }

        if (hybrid || semantic)
        {
            searchOptions.QueryType = SearchQueryType.Semantic;
            searchOptions.SemanticSearch = new SemanticSearchOptions
            {
                SemanticConfigurationName = "golf-semantic-config",
                QueryCaption = new QueryCaption(QueryCaptionType.Extractive),
                QueryAnswer = new QueryAnswer(QueryAnswerType.Extractive),
            };
        }

        string? queryText = (textOnly || hybrid || semantic) ? query : null;
        SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(queryText, searchOptions);

        var golfballDataList = new List<GolfBallAISearch>();
        await foreach (var result in response.GetResultsAsync())
        {
            double? relevanceScore = result.SemanticSearch?.RerankerScore ?? result.Score;

            if (result.SemanticSearch?.RerankerScore >= minRerankerScore)
            {
                var golfBall = new GolfBallAISearch
                {
                    reRankerScore = result.SemanticSearch?.RerankerScore.ToString() ?? result.Score.ToString(),
                    Manufacturer = result.Document["manufacturer"]?.ToString() ?? string.Empty,
                    Pole_Marking = result.Document["pole_marking"]?.ToString() ?? string.Empty,
                    USGA_Lot_Num = result.Document["usga_lot_num"]?.ToString() ?? string.Empty,
                    ConstCode = result.Document["constCode"]?.ToString() ?? string.Empty,
                    BallSpecs = result.Document["ballSpecs"]?.ToString() ?? string.Empty,
                    Dimples = result.Document["dimples"]?.ToString() ?? string.Empty,
                    Spin = result.Document["spin"]?.ToString() ?? string.Empty,
                    Pole_2 = result.Document["pole_2"]?.ToString() ?? string.Empty,
                    Colour = result.Document["colour"]?.ToString() ?? string.Empty,
                    Seam_Marking = result.Document["seam_marking"]?.ToString() ?? string.Empty,
                    ImageUrl = result.Document["imageUrl"]?.ToString() ?? string.Empty
                };

                golfballDataList.Add(golfBall);
            }
        }

        _logger.LogInformation($"Found {golfballDataList.Count} golf ball matches.");
        return golfballDataList;
    }
}