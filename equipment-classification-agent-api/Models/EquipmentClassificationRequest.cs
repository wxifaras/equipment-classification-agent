namespace equipment_classification_agent_api.Models;

public class EquipmentClassificationRequest
{
    public string SessionId { get; set; }
    public List<IFormFile> Images { get; set; }
}