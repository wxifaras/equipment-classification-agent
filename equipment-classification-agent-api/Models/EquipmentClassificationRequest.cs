namespace equipment_classification_agent_api.Models;

public class EquipmentClassificationRequest
{
    public string SessionId { get; set; }
    public IFormFile Image { get; set; }
}