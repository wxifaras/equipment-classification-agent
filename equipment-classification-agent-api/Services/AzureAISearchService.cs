using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;

namespace equipment_classification_agent_api.Services;

public interface IAzureAISearchService
{
}

public class AzureAISearchService : IAzureAISearchService
{
    const string vectorSearchHnswProfile = "golf-vector-profile";
    const string vectorSearchHnswConfig = "golfHnsw";
    const string vectorSearchVectorizer = "golfOpenAIVectorizer";
    const string semanticSearchConfig = "golf-semantic-config";

    public AzureAISearchService(IOptions<AzureAISearchOptions> options)
    {

    }
}