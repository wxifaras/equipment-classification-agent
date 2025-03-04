using Azure.AI.OpenAI;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using equipment_classification_agent_api.Prompts;
using OpenAI.Chat;
using Azure.Search.Documents;
using System.Text.Json;
using System.Drawing;

namespace equipment_classification_agent_api.Services;

public interface IAzureOpenAIService
{
    /// <summary>
    /// Extracts image details from the given list of image URLs. If the <paramref name="golfBallLLMDetails"/> is not null, the prompt
    /// to judge the generated GolfBallDetails it will be used to extract details from the images.
    /// </summary>
    /// <param name="imageUrlList"></param>
    /// <param name="golfBallLLMDetails"></param>
    /// <returns></returns>
    Task<GolfBallLLMDetail> ExtractImageDetailsAsync(List<string> imageUrlList, List<GolfBallLLMDetail> golfBallLLMDetails, Guid sessionId);

    Task<(string nlpQuery, string filter)> GenerateNLQueryAsync(GolfBallLLMDetail golfBallDetails, Guid sessionId);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly AzureOpenAIClient _azureOpenAIClient;
    private readonly string _deploymentName;
    private readonly ICacheService _cacheService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly bool _enableChatHistory;

    public AzureOpenAIService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIService> logger,
        SearchClient searchClient,
        ICacheService cacheService,
        IChatHistoryService chatHistoryService)
    {
        _azureOpenAIClient = new(
            new Uri(options.Value.AzureOpenAIEndPoint),
            new AzureKeyCredential(options.Value.AzureOpenAIKey));

        _deploymentName = options.Value.AzureOpenAIDeploymentName;
        _logger = logger;
        _cacheService = cacheService;
        _chatHistoryService = chatHistoryService;
        _enableChatHistory = options.Value.EnableChatHistory;
    }

    public async Task<GolfBallLLMDetail> ExtractImageDetailsAsync(List<string> imageUrlList, List<GolfBallLLMDetail> golfBallLLMDetails, Guid sessionId)
    {
        var chatClient = _azureOpenAIClient.GetChatClient(_deploymentName);

        var manufacturers = await _cacheService.GetManufacturers();

        var commaSeparatedManufacturers = string.Join(", ", manufacturers);

        var systemPrompt = string.Empty;

        // Get the system prompt
        if (golfBallLLMDetails == null)
        {
            systemPrompt = CorePrompts.GetImageMarkingsExtractionsPrompt(commaSeparatedManufacturers);
        }
        else
        {
            var golfBallLLMDetailsJson = JsonSerializer.Serialize(golfBallLLMDetails, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            systemPrompt = CorePrompts.GetFinalImageMarkingsExtractionsPrompt(commaSeparatedManufacturers, golfBallLLMDetailsJson);
        }

        ChatImageDetailLevel? imageDetailLevel = ChatImageDetailLevel.High;
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(
            new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart("Analyze these images:"),
            }.Concat(imageUrlList.Select(url => ChatMessageContentPart.CreateImagePart(new Uri(url), imageDetailLevel))).ToList())
        };

        var schemaFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models", "GolfBallLLMDetail.json");
        var jsonSchema = File.ReadAllText(schemaFilePath);

        //Create chat completion options
        var options = new ChatCompletionOptions
        {
            Temperature = (float)0.7,
            MaxOutputTokenCount = 800,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("GolfBallDetail", BinaryData.FromString(jsonSchema))
        };

        // Create the chat completion request
        ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

        var golfBallDetail = new GolfBallLLMDetail();
        var jsonResponse = string.Empty;

        try
        {
            if (completion.Content != null && completion.Content.Count > 0)
            {
                _logger.LogInformation($"Result: {completion.Content[0].Text}");
                jsonResponse = $"{completion.Content[0].Text}";

                golfBallDetail = JsonSerializer.Deserialize<GolfBallLLMDetail>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (_enableChatHistory)
                {
                    await _chatHistoryService.SaveChatHistoryAsync(sessionId, messages, jsonResponse);
                }
            }
            else
            {
                _logger.LogInformation("No response received.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"An error occurred: {ex.Message}");
        }

        return golfBallDetail;
    }

    public async Task<(string nlpQuery, string filter)> GenerateNLQueryAsync(GolfBallLLMDetail golfBallDetails, Guid sessionId)
    {
        var chatClient = _azureOpenAIClient.GetChatClient(_deploymentName);

        (string nlpQuery, string filter) queryTuple = (string.Empty, string.Empty);

        try
        {
            var golfBallDetailsJson = JsonSerializer.Serialize(golfBallDetails, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var options = new ChatCompletionOptions
            {
                Temperature = (float)0.7,
                MaxOutputTokenCount = 800,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };

            // 2nd LLM call for NLP query
            var nlpPrompt = CorePrompts.GetNlpPrompt(golfBallDetailsJson);

            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(nlpPrompt)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

            var nlpQuery = completion.Content[0].Text;

            var filter = string.Empty;
            if (golfBallDetails.colour.Contains('/'))
            {
                var ballColors = golfBallDetails.colour.Split('/');
                filter = $"colour eq '{ballColors[0].ToLower()}/{ballColors[1].ToLower()}' or colour eq '{ballColors[1].ToLower()}/{ballColors[0].ToLower()}'";
            }
            else
            {
                filter = $"colour eq '{golfBallDetails?.colour.ToLower()}'";
            }

            if (!string.IsNullOrEmpty(golfBallDetails?.manufacturer))
            {
                var manufacturer = golfBallDetails.manufacturer.Trim();
                if (!manufacturer.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    // Escape single quotes by doubling them
                    var escapedManufacturer = manufacturer.Replace("'", "''");
                    filter += $" and manufacturer eq '{escapedManufacturer}'";
                }
            }

            queryTuple = (nlpQuery, filter);
            if (_enableChatHistory)
            {
                await _chatHistoryService.SaveChatHistoryAsync(sessionId, nlpPrompt, nlpQuery);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"An error occurred: {ex.Message}");
        }

        return queryTuple;
    }
}