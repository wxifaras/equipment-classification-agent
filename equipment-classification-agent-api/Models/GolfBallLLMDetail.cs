namespace equipment_classification_agent_api.Models;

public class GolfBallLLMDetail
{
    public string manufacturer { get; set; }
    public string colour { get; set; }
    public string markings { get; set; }

    public override string ToString()
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(manufacturer)) values.Add(manufacturer);
        if (!string.IsNullOrWhiteSpace(colour)) values.Add(colour);
        if (!string.IsNullOrWhiteSpace(markings)) values.Add(markings);

        return string.Join(", ", values);
    }
}