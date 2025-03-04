﻿namespace equipment_classification_agent_api.Models;

public class GolfBallLLMDetail
{
    public string manufacturer { get; set; }
    public string colour { get; set; }
    public string markings { get; set; }
    public string thought_process { get; set; }
    public string brand_explanation { get; set; }
    public string tags_explanation { get; set; }

    public override string ToString()
    {
        var values = new List<string>();

        if (!string.IsNullOrWhiteSpace(manufacturer)) values.Add(manufacturer);
        if (!string.IsNullOrWhiteSpace(colour)) values.Add(colour);
        if (!string.IsNullOrWhiteSpace(markings)) values.Add(markings);
        if (!string.IsNullOrWhiteSpace(thought_process)) values.Add(thought_process);
        if (!string.IsNullOrWhiteSpace(brand_explanation)) values.Add(brand_explanation);
        if (!string.IsNullOrWhiteSpace(tags_explanation)) values.Add(tags_explanation);

        return string.Join(", ", values);
    }
}