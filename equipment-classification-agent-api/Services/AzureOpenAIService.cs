using Azure.AI.OpenAI;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;

namespace equipment_classification_agent_api.Services;

public interface IAzureOpenAIService
{
    Task ExtractImageDetailsAsync();
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly AzureOpenAIClient _azureOpenAIClient;
    private readonly string _deploymentName;

    public AzureOpenAIService(
        IOptions<AzureOpenAIOptions> options, 
        ILogger<AzureOpenAIService> logger)
    {
        _azureOpenAIClient = new(
            new Uri(options.Value.AzureOpenAIEndPoint),
            new AzureKeyCredential(options.Value.AzureOpenAIEndPoint));

        _deploymentName = options.Value.AzureOpenAIDeploymentName;
        _logger = logger;
    }

    public Task ExtractImageDetailsAsync()
    {
        var chatClient = _azureOpenAIClient.GetChatClient(_deploymentName);
        throw new NotImplementedException();
    }
}