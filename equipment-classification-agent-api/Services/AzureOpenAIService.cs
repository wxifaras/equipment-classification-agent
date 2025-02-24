using Azure.AI.OpenAI;
using Azure;
using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using equipment_classification_agent_api.Prompts;
using OpenAI.Chat;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Linq;
using Azure.Search.Documents;

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

    public AzureOpenAIService(
        IOptions<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIService> logger,
        AzureStorageService azureStorageService,
        SearchClient searchClient,
        ICacheService cacheService)
    {
        _azureOpenAIClient = new(
            new Uri(options.Value.AzureOpenAIEndPoint),
            new AzureKeyCredential(options.Value.AzureOpenAIKey));

        _deploymentName = options.Value.AzureOpenAIDeploymentName;
        _azureStorageService = azureStorageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<(string nlpQuery, string filter)> ExtractImageDetailsAsync(EquipmentClassificationRequest request)
    {
        (string nlpQuery, string filter) queryTuple  = (string.Empty, string.Empty);
        var chatClient = _azureOpenAIClient.GetChatClient(_deploymentName);
        var imageUrlList = new List<string>();

        string fileName = string.Empty;

        foreach (var image in request.Images)
        {
            fileName = $"{request.SessionId}/{image.FileName}";
            var imageUrl = await _azureStorageService.GenerateSasUriAsync(fileName);
            imageUrlList.Add(imageUrl);
        }

        var manufacturers = await _cacheService.GetManufacturers();

        string commaSeparatedManufacturers = string.Join(", ", manufacturers);

        // Get the system prompt
        var systemPrompt = CorePrompts.GetSystemPrompt(commaSeparatedManufacturers);

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
                var properties = FetchPropertiesFromJson(jsonResponse);

                var jsonObject = JObject.Parse(jsonResponse);
                var golfBallDetail = jsonObject.ToObject<GolfBallLLMDetail>();

                // 2nd LLM call for NLP query
                var nlpPrompt = CorePrompts.GetNlpPrompt(jsonObject.ToString());
                
                messages = new List<ChatMessage>
                {
                    new SystemChatMessage(nlpPrompt)                 
                };

                completion = await chatClient.CompleteChatAsync(messages);
                var nlpQuery = completion.Content[0].Text;
                queryTuple = (nlpQuery, filter: $"colour eq '{golfBallDetail?.colour}'");
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

    public string? FetchPropertiesFromJson(string llmJsonResult)
    {
        if (string.IsNullOrWhiteSpace(llmJsonResult))
        {
            _logger.LogWarning("Received empty JSON response.");
            return null;
        }

        try
        {
            var jsonObject = JObject.Parse(llmJsonResult);
            var golfBallDetail = jsonObject.ToObject<GolfBallLLMDetail>();

            if (golfBallDetail == null)
            {
                _logger.LogWarning("Failed to deserialize JSON to GolfBallLLMDetail.");
                return null;
            }

            var properties = new List<string>();

            if (!string.IsNullOrWhiteSpace(golfBallDetail.manufacturer) && !golfBallDetail.manufacturer.Equals("unknown"))
            {
                properties.Add($"manufacturer:{golfBallDetail.manufacturer}");
            }

            if (!string.IsNullOrWhiteSpace(golfBallDetail.pole_marking))
            {
                //properties.Add($"pole_marking:{golfBallDetail.pole_marking}");
                properties.Add($"{golfBallDetail.pole_marking}");
            }

            var seamMarkingText = string.Empty;

            if (!string.IsNullOrWhiteSpace(golfBallDetail.seam_marking))
            {
                //seamMarkingText = $"seam_marking:{golfBallDetail.seam_marking}";
                seamMarkingText = $"{golfBallDetail.seam_marking}";
            }

            if (!string.IsNullOrWhiteSpace(seamMarkingText))
            {
                properties.Add(" " + seamMarkingText);
            }

            return string.Join(" and ", properties);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error parsing JSON response: {ex.Message}");
            return null;
        }
    }
}