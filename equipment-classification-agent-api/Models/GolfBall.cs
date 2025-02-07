namespace equipment_classification_agent_api.Models;

public class GolfBall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Country { get; set; }
    public string? Manufacturer { get; set; }
    public string? USGA_Lot_Num { get; set; } 
    public string? Pole_Marking { get; set; } 
    public string? Pole1_Web { get; set; } 
    public string? Colour { get; set; }
    public string? ConstCode { get; set; }
    public string? WoundCode { get; set; }
    public string? CenterCode { get; set; } 
    public string? CoverCode { get; set; } 
    public string? BallSpecs1 { get; set; }
    public string? BallSpecs { get; set; }
    public string? Dimples { get; set; }
    public string? Spin { get; set; }
    public string? Pole_2 { get; set; }
    public string? Pole2_Web { get; set; }
    public string? Seam_Marking { get; set; }
    public string? Seam1_Web { get; set; }
    public string? Seam_2 { get; set; }
    public string? Seam2_Web { get; set; }
    public string? DecisionNumber { get; set; }
    public string? ImageUrl { get; set; }
    public IList<float> VectorContent { get; set; } = new List<float>();
}