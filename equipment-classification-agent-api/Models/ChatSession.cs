using OpenAI.Chat;

namespace equipment_classification_agent_api.Models;

public record ChatSession
{
    public Guid SessionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Models.ChatMessage> ChatMessages { get; set; } = new List<Models.ChatMessage>();
}