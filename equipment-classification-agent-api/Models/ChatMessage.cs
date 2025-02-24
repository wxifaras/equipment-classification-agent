namespace equipment_classification_agent_api.Models;

public record ChatMessage
{
    public int MessageId { get; set; }
    public Guid SessionId { get; set; }
    public string Sender { get; set; } // E.g., 'User' / 'Assistant' / 'System'
    public string MessageContent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}