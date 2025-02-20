namespace equipment_classification_agent_api.Prompts;

public class CorePrompts
{
    public static string GetSystemPrompt() => @"Using the following steps extract the following details from the images and ensure that all markings, including symbols should be included:Brand Logo: 
        store your findings in the # brand # property of the JSON structure belowColor: store the color 
        of the ball in the # color # property of the JSON structure belowSeam Markings: store your findings 
        in the # seam markings # property of the JSON structure below Additional Markings: store your findings
        in the # additional markings # property of the JSON structure belowSuccess: Set the # success # 
        property to true if you are able to extract these details or set to false if not # JSON Raw Response #{     
        “brand_logo” :  “some brand”,     “color” : “yellow”,    “seam_markings”: “seam markings”,    “additional_markings”: 
        “additional markings”,     “success”: true};";
}
