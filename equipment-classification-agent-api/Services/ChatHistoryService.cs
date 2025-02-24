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
    Task CreateChatSessionAsync(Guid sessionId, DateTime createdAt);
    Task CreateChatMessageAsync(Guid sessionId, string sender, string messageContent, DateTime? timestamp = null);
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

    public async Task CreateChatSessionAsync(Guid sessionId, DateTime createdAt)
    {
        _logger.LogInformation($"Creating chat session with ID {sessionId} at {createdAt}");
        await _azureSQLService.CreateChatSessionAsync(sessionId, createdAt);
    }

    public async Task CreateChatMessageAsync(Guid sessionId, string sender, string messageContent, DateTime? timestamp = null)
    {
        _logger.LogInformation($"Creating chat message in session {sessionId} from {sender} at {timestamp}");
        await _azureSQLService.CreateChatMessageAsync(sessionId, sender, messageContent, timestamp);
    }
}