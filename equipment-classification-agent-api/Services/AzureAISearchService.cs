using Azure.Search.Documents.Indexes;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using Azure.Search.Documents.Indexes.Models;
using Azure.AI.OpenAI;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using OpenAI.Embeddings;
using System.Diagnostics;
using Azure;

namespace equipment_classification_agent_api.Services;

public interface IAzureAISearchService
{
    Task CreateAISearchIndexAsync();
    Task IndexDataAsync();
    Task<List<GolfBallAISearch>> SearchGolfBallAsync(
           string query,
           int k = 5,
           int top = 10, // top 10 results
           string? filter = null,
           bool textOnly = false,
           bool hybrid = true,
           bool semantic = false,
           double minRerankerScore = 2.0);

    Task DeleteAISearchIndexAsync();
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
        _indexName = azureAISearchOptions.Value.IndexName ?? throw new ArgumentNullException(nameof(azureAISearchOptions.Value.IndexName));
        _azureOpenAIEndpoint = azureOpenAIOptions.Value.AzureOpenAIEndPoint ?? throw new ArgumentNullException(nameof(azureOpenAIOptions.Value.AzureOpenAIEndPoint));
        _azureOpenAIKey = azureOpenAIOptions.Value.AzureOpenAIKey ?? throw new ArgumentNullException(nameof(azureOpenAIOptions.Value.AzureOpenAIKey));
        _azureOpenAIEmbeddingDimensions = azureOpenAIOptions.Value.AzureOpenAIEmbeddingDimensions ?? throw new ArgumentNullException(nameof(azureOpenAIOptions.Value.AzureOpenAIEmbeddingDimensions));
        _azureOpenAIEmbeddingModel = azureOpenAIOptions.Value.AzureOpenAIEmbeddingModel ?? throw new ArgumentNullException(nameof(azureOpenAIOptions.Value.AzureOpenAIEmbeddingModel));
        _azureOpenAIEmbeddingDeployment = azureOpenAIOptions.Value.AzureOpenAIEmbeddingDeployment ?? throw new ArgumentNullException(nameof(azureOpenAIOptions.Value.AzureOpenAIEmbeddingDeployment));
        _indexClient = indexClient ?? throw new ArgumentNullException(nameof(indexClient));
        _azureOpenAIClient = azureOpenAIClient ?? throw new ArgumentNullException(nameof(azureOpenAIClient));
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _azureSQLService = azureSQLService ?? throw new ArgumentNullException(nameof(azureSQLService));
    }

    public async Task CreateAISearchIndexAsync()
    {
        try
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
                            ContentFields =
                            {
                                new SemanticField("pole_marking"),
                                new SemanticField("seam_marking"),
                                new SemanticField("pole_2")
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

            await _indexClient.CreateOrUpdateIndexAsync(searchIndex).ConfigureAwait(false);

            _logger.LogInformation($"Completed creating index {searchIndex}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating AI search index.");
            throw;
        }
    }

    public async Task DeleteAISearchIndexAsync()
    {
        try
        {
            // Attempt to delete the index
            await _indexClient.DeleteIndexAsync(_indexName).ConfigureAwait(false);
            _logger.LogInformation($"Index '{_indexName}' deleted successfully.");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Handle the case where the index does not exist
            _logger.LogWarning($"Index '{_indexName}' does not exist. No deletion performed.");
        }
        catch (Exception ex)
        {
            // Handle other exceptions
            _logger.LogError(ex, $"An error occurred while deleting the index '{_indexName}'.");
            throw;
        }
    }

    public async Task IndexDataAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var golfBalls = await _azureSQLService.GetGolfBallsAsync().ConfigureAwait(false);

            if (golfBalls == null || golfBalls.Count == 0)
            {
                throw new ArgumentException("No golf ball data found in SQL.");
            }

            var embeddingClient = _azureOpenAIClient.GetEmbeddingClient(_azureOpenAIEmbeddingDeployment);

            foreach (var golfBall in golfBalls)
            {
                string textForEmbedding = $"pole_marking: {golfBall.Pole_Marking}, " +
                                          $"seam_marking: {golfBall.Seam_Marking}, " +
                                          $"pole_2: {golfBall.Pole_2}";

                OpenAIEmbedding embedding = await embeddingClient.GenerateEmbeddingAsync(textForEmbedding).ConfigureAwait(false);
                golfBall.VectorContent = embedding.ToFloats().ToArray().ToList();
            }

            var batch = IndexDocumentsBatch.Upload(golfBalls);

            var result = await _searchClient.IndexDocumentsAsync(batch).ConfigureAwait(false);

            _logger.LogInformation($"Indexed {golfBalls.Count} golf balls.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing data.");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation($"Execution Time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    public async Task<List<GolfBallAISearch>> SearchGolfBallAsync(
           string query,
           int k = 5,
           int top = 10, // top 10 results
           string? filter = null,
           bool textOnly = false,
           bool hybrid = true,
           bool semantic = false,
           double minRerankerScore = 2.0)
    {
        try
        {
            var searchOptions = new SearchOptions
            {
                Filter = filter,
                Size = top,
                Select = { "id", "manufacturer", "pole_marking", "usga_lot_num", "constCode", "ballSpecs", "dimples", "spin", "pole_2", "colour", "seam_marking", "imageUrl" },
                IncludeTotalCount = true,
                QueryType = SearchQueryType.Semantic,
                SearchMode = SearchMode.Any
            };

            if (!textOnly)
            {
                searchOptions.VectorSearch = new()
                {
                    Queries = {
                        new VectorizableTextQuery(text: query)
                        {
                            Exhaustive = true,
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
            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(queryText, searchOptions).ConfigureAwait(false);

            var golfballDataList = new List<GolfBallAISearch>();
            await foreach (var result in response.GetResultsAsync().ConfigureAwait(false))
            {
                if (result.SemanticSearch?.RerankerScore >= minRerankerScore)
                {
                    var golfBall = new GolfBallAISearch
                    {
                        ReRankerScore = result.SemanticSearch?.RerankerScore.ToString() ?? result.Score.ToString(),
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for golf balls.");
            throw;
        }
    }
}