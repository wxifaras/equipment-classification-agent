namespace equipment_classification_agent_api.Models;

public class EquipmentClassificationResponse
{
    public Guid SessionId { get; set; }
    public List<GolfBallAISearch> AzureAISearchQueryResults { get; set; }
    public string AISearchFilter { get; set; }
    public string NLPQuery { get; set; }
}