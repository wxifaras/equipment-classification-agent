namespace equipment_classification_agent_api.Models;

public class EquipmentClassificationResponse
{
    public string SessionId { get; set; }
    public List<GolfBallAISearch> AzureAISearchQuery { get; set; }
}