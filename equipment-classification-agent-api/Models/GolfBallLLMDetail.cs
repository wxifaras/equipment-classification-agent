namespace equipment_classification_agent_api.Models;

public class GolfBallLLMDetail
{
    public string manufacturer { get; set; }
    public string colour { get; set; }
    public string seam_marking { get; set; }
    public string pole_marking { get; set; }

    public override string ToString()
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(manufacturer)) values.Add(manufacturer);
        if (!string.IsNullOrWhiteSpace(colour)) values.Add(colour);
        if (!string.IsNullOrWhiteSpace(seam_marking)) values.Add(seam_marking);
        if (!string.IsNullOrWhiteSpace(pole_marking)) values.Add(pole_marking);

        return string.Join(", ", values);
    }
}