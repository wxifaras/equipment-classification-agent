namespace equipment_classification_agent_api.Models;

public class GolfBall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Manufacturer { get; set; }
    public string? USGA_Lot_Num { get; set; } 
    public string? Pole_Marking { get; set; } 
    public string? Colour { get; set; }
    public string? ConstCode { get; set; }
    public string? BallSpecs { get; set; }
    public string? Dimples { get; set; }
    public string? Spin { get; set; }
    public string? Pole_2 { get; set; }
    public string? Seam_Marking { get; set; }
    public string? ImageUrl { get; set; }
    public IList<float> VectorContent { get; set; } = new List<float>();
}