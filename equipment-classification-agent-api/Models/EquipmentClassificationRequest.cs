using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models;

public class EquipmentClassificationRequest
{
    public Guid SessionId { get; set; }

    [Required]
    public List<IFormFile> Images { get; set; }
}