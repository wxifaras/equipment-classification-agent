using Azure.AI.OpenAI;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using equipment_classification_agent_api.Prompts;
using OpenAI.Chat;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Linq;
using Azure.Search.Documents;
using System;

namespace equipment_classification_agent_api.Services;

public interface IAzureOpenAIService
{
    Task<(string nlpQuery, string filter)> ExtractImageDetailsAsync(EquipmentClassificationRequest request);
}

public class AzureOpenAIService : IAzureOpenAIService
{
    private readonly ILogger<AzureOpenAIService> _logger;
    private readonly AzureOpenAIClient _azureOpenAIClient;
    private readonly AzureStorageService _azureStorageService;
    private readonly string _deploymentName;
    private readonly ICacheService _cacheService;
    private readonly IChatHistoryService _chatHistoryService;

    public AzureOpenAIService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIService> logger,
        AzureStorageService azureStorageService,
        SearchClient searchClient,
        ICacheService cacheService,
        IChatHistoryService chatHistoryService)
    {
        _azureOpenAIClient = new(
            new Uri(options.Value.AzureOpenAIEndPoint),
            new AzureKeyCredential(options.Value.AzureOpenAIKey));

        _deploymentName = options.Value.AzureOpenAIDeploymentName;
        _azureStorageService = azureStorageService;
        _logger = logger;
        _cacheService = cacheService;
        _chatHistoryService = chatHistoryService;
    }

    public async Task<(string nlpQuery, string filter)> ExtractImageDetailsAsync(EquipmentClassificationRequest request)
    {
        (string nlpQuery, string filter) queryTuple  = (string.Empty, string.Empty);
        var chatClient = _azureOpenAIClient.GetChatClient(_deploymentName);
        var imageUrlList = new List<string>();
        var fileName = string.Empty;
        var userPrompt = "Analyze these images:";

        foreach (var image in request.Images)
        {
            fileName = $"{request.SessionId}/{image.FileName}";
            var imageUrl = await _azureStorageService.GenerateSasUriAsync(fileName);
            imageUrlList.Add(imageUrl);
        }

        var manufacturers = await _cacheService.GetManufacturers();

        var commaSeparatedManufacturers = string.Join(", ", manufacturers);

        // Get the system prompt
        var systemPrompt = CorePrompts.GetSystemPrompt(commaSeparatedManufacturers);

        ChatImageDetailLevel? imageDetailLevel = ChatImageDetailLevel.High;
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(
            new List<ChatMessageContentPart>
            {
                ChatMessageContentPart.CreateTextPart(userPrompt),
            }.Concat(imageUrlList.Select(url => ChatMessageContentPart.CreateImagePart(new Uri(url), imageDetailLevel))).ToList())
        };

        var generator = new JSchemaGenerator();
        var jsonSchema = generator.Generate(typeof(GolfBallLLMDetail)).ToString();

        //Create chat completion options
        var options = new ChatCompletionOptions
        {
            Temperature = (float)0.7,
            MaxOutputTokenCount = 800,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("GolfBallDetail", BinaryData.FromString(jsonSchema))
        };

        try
        {
            var jsonResponse = string.Empty;

            // Create the chat completion request
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages, options);

            // Print the response
            if (completion.Content != null && completion.Content.Count > 0)
            {
                _logger.LogInformation($"Result: {completion.Content[0].Text}");
                jsonResponse = $"{completion.Content[0].Text}";

                var jsonObject = JObject.Parse(jsonResponse);
                var golfBallDetail = jsonObject.ToObject<GolfBallLLMDetail>();

                //await _chatHistoryService.CreateChatSessionAsync(request.SessionId, DateTime.UtcNow);
                //foreach (var message in messages)
                //{
                //    var role = string.Empty;
                //    if (message is OpenAI.Chat.SystemChatMessage)
                //    {
                //        role = ChatRole.System.ToString();
                //        await _chatHistoryService.CreateChatMessageAsync(request.SessionId, role, message.Content[0].Text);
                //    }

                //    if (message is OpenAI.Chat.UserChatMessage)
                //    {
                //        role = ChatRole.User.ToString();

                //        foreach(var content in message.Content)
                //        {
                //            if (content.Kind == ChatMessageContentPartKind.Text)
                //            {
                //                await _chatHistoryService.CreateChatMessageAsync(request.SessionId, role, content.Text, DateTime.UtcNow);
                //            }

                //            if (content.Kind == ChatMessageContentPartKind.Image)
                //            {
                //                await _chatHistoryService.CreateChatMessageAsync(request.SessionId, role, content.ImageUri.ToString(), DateTime.UtcNow);
                //            }
                //        }
                //    }
                //}

                //await _chatHistoryService.CreateChatMessageAsync(request.SessionId, ChatRole.Assistant.ToString(), jsonResponse, DateTime.UtcNow);

                // 2nd LLM call for NLP query
                var nlpPrompt = CorePrompts.GetNlpPrompt(jsonObject.ToString());
                
                messages = new List<OpenAI.Chat.ChatMessage>
                {
                    new SystemChatMessage(nlpPrompt)                 
                };

                completion = await chatClient.CompleteChatAsync(messages);
                var nlpQuery = completion.Content[0].Text;
                queryTuple = (nlpQuery, filter: $"colour eq '{golfBallDetail?.colour}'");

                //await _chatHistoryService.CreateChatMessageAsync(request.SessionId, ChatRole.System.ToString(), nlpPrompt, DateTime.UtcNow);
                //await _chatHistoryService.CreateChatMessageAsync(request.SessionId, ChatRole.Assistant.ToString(), nlpQuery, DateTime.UtcNow);
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

        return queryTuple;
    }
}