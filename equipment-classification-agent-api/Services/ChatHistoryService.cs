using OpenAI.Chat;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace equipment_classification_agent_api.Services;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatRole
{
    [EnumMember(Value = "system")]
    System,

    [EnumMember(Value = "user")]
    User,

    [EnumMember(Value = "assistant")]
    Assistant,

    [EnumMember(Value = "function")]
    Function,

    [EnumMember(Value = "tool")]
    Tool
}

public interface IChatHistoryService
{
    Task SaveChatHistoryAsync(Guid sessionId, List<OpenAI.Chat.ChatMessage> messages, string jsonResponse);
    Task SaveChatHistoryAsync(Guid sessionId, string nlpPrompt, string nlpQuery);
}

public class ChatHistoryService : IChatHistoryService
{
    private IAzureSQLService _azureSQLService;
    private ILogger<ChatHistoryService> _logger;

    public ChatHistoryService(
        IAzureSQLService azureSQLService, 
        ILogger<ChatHistoryService> logger)
    {
        _azureSQLService = azureSQLService;
        _logger = logger;
    }

    public async Task SaveChatHistoryAsync(Guid sessionId, List<OpenAI.Chat.ChatMessage> messages, string jsonResponse)
    {
        await CreateChatSessionAsync(sessionId, DateTime.UtcNow);
        foreach (var message in messages)
        {
            var role = string.Empty;
            if (message is OpenAI.Chat.SystemChatMessage)
            {
                role = ChatRole.System.ToString();
                await CreateChatMessageAsync(sessionId, role, message.Content[0].Text);
            }

            if (message is OpenAI.Chat.UserChatMessage)
            {
                role = ChatRole.User.ToString();

                foreach (var content in message.Content)
                {
                    if (content.Kind == ChatMessageContentPartKind.Text)
                    {
                        await CreateChatMessageAsync(sessionId, role, content.Text, DateTime.UtcNow);
                    }

                    if (content.Kind == ChatMessageContentPartKind.Image)
                    {
                        await CreateChatMessageAsync(sessionId, role, content.ImageUri.ToString(), DateTime.UtcNow);
                    }
                }
            }
        }

        await CreateChatMessageAsync(sessionId, ChatRole.Assistant.ToString(), jsonResponse, DateTime.UtcNow);
    }

    public async Task SaveChatHistoryAsync(Guid sessionId, string nlpPrompt, string nlpQuery)
    {
        await CreateChatMessageAsync(sessionId, ChatRole.System.ToString(), nlpPrompt, DateTime.UtcNow);
        await CreateChatMessageAsync(sessionId, ChatRole.Assistant.ToString(), nlpQuery, DateTime.UtcNow);
    }

    private async Task CreateChatSessionAsync(Guid sessionId, DateTime createdAt)
    {
        _logger.LogInformation($"Creating chat session with ID {sessionId} at {createdAt}");
        await _azureSQLService.CreateChatSessionAsync(sessionId, createdAt);
    }

    private async Task CreateChatMessageAsync(Guid sessionId, string sender, string messageContent, DateTime? timestamp = null)
    {
        _logger.LogInformation($"Creating chat message in session {sessionId} from {sender} at {timestamp}");
        await _azureSQLService.CreateChatMessageAsync(sessionId, sender, messageContent, timestamp);
    }
}