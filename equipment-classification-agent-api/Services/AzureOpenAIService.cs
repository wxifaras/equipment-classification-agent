using Azure.AI.OpenAI;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using equipment_classification_agent_api.Prompts;
using OpenAI.Chat;
using OpenAI.Images;
using Microsoft.AspNetCore.Routing.Constraints;

namespace equipment_classification_agent_api.Services;

public interface IAzureOpenAIService
{
    Task ExtractImageDetailsAsync(EquipmentClassificationRequest request);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly AzureOpenAIClient _azureOpenAIClient;
    private readonly AzureStorageService _azureStorageService;
    private readonly string _deploymentName;

    public AzureOpenAIService(
        IOptions<AzureOpenAIOptions> options, 
        ILogger<AzureOpenAIService> logger,
        AzureStorageService azureStorageService)
    {
        _azureOpenAIClient = new(
            new Uri(options.Value.AzureOpenAIEndPoint),
            new AzureKeyCredential(options.Value.AzureOpenAIKey));

        _deploymentName = options.Value.AzureOpenAIDeploymentName;
        _azureStorageService = azureStorageService;
        _logger = logger;
        
    }

    public async Task ExtractImageDetailsAsync(EquipmentClassificationRequest request)
    {
       
        ChatClient chatClient = _azureOpenAIClient.GetChatClient(_deploymentName);

        var imageUrlList = new List<string>();
        string fileName = string.Empty;
        foreach (var image in request.Images)
        {
            fileName = $"{request.SessionId}/{image.FileName}";
            var imageUrl = await _azureStorageService.GenerateSasUriAsync(fileName);
            imageUrlList.Add(imageUrl);
        }

        // Get the system prompt
        var systemPrompt = CorePrompts.GetSystemPrompt();

        ChatImageDetailLevel? imageDetailLevel = ChatImageDetailLevel.High;
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(
            new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart("Analyze these images:"),
            }.Concat(imageUrlList.Select(url => ChatMessageContentPart.CreateImagePart(new Uri(url), imageDetailLevel))).ToList())
        };

        //Create chat completion options
        var options = new ChatCompletionOptions
        {
            Temperature = (float)0.7,
            MaxOutputTokenCount = 800,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };

        try
        {
            // Create the chat completion request
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

            // Print the response
            if (completion.Content != null && completion.Content.Count > 0)
            {
                EquipmentClassificationResponse response = new EquipmentClassificationResponse();
                response.AzureAISearchQuery = completion.Content[0].Text;
                response.SessionId = request.SessionId;
                Console.WriteLine($"{completion.Content[0].Kind}: {completion.Content[0].Text}");
            }
            else
            {
                Console.WriteLine("No response received.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

    }
}