namespace equipment_classification_agent_api.Models;

public class GolfBall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Country { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string USGA_Lot_Num { get; set; } = string.Empty;
    public string Pole_Marking { get; set; } = string.Empty;
    public string Pole1_Web { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string ConstCode { get; set; } = string.Empty;
    public string WoundCode { get; set; } = string.Empty;
    public string CenterCode { get; set; } = string.Empty;
    public string CoverCode { get; set; } = string.Empty;
    public string BallSpecs1 { get; set; } = string.Empty;
    public string BallSpecs { get; set; } = string.Empty;
    public string Dimples { get; set; } = string.Empty;
    public string Spin { get; set; } = string.Empty;
    public string Pole_2 { get; set; } = string.Empty;
    public string Pole2_Web { get; set; } = string.Empty;
    public string Seam_Marking { get; set; } = string.Empty;
    public string Seam1_Web { get; set; } = string.Empty;
    public string Seam_2 { get; set; } = string.Empty;
    public string Seam2_Web { get; set; } = string.Empty;
    public string DecisionNumber { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public IList<float> VectorContent { get; set; } = new List<float>();
}