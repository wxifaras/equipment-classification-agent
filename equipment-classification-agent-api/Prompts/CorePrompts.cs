namespace equipment_classification_agent_api.Prompts;

public class CorePrompts
{
    public static string GetSystemPrompt(string manufacturers) => $@"
        Your task is to analyze one or more pictures of a golf ball and extract details from the images. Ensure that all text and markings, 
        including symbols such as arrows and angle brackets or lines along with their colors, are included. It is crucial to extract symbols and
        the color of the symbols. For example, if you see a red arrow underneath the text 'Titleist Pro V1', you will
        return 'Titleist Pro V1 with a red arrow underneath'.
        
        Instructions:
        1. Manufacturer: Identify the name of the golf ball manufacturer. Store your findings in the 'manufacturer' property of the JSON structure below.
            You must be sure to only use the manufacturers from the following list: {manufacturers}

            If detail you extract does not match one of the manufacturers from the list or you are not sure, please respond use 'unknown' for the manufacturer.
        2. Color: Identify the color of the ball. Store the color in the 'color' property of the JSON structure below.
        3. Seam Markings: Identify any text or a combination of text and symbols near the seam. If there are symbols near or around text, capture the type of symbol along with its color. Store your findings in the 'seam_markings' property of the JSON structure below.
        4. Pole Markings: Identify any text or a combination of text and symbols near the pole. If there are symbols near or around text, capture the type of symbol along with its color. Store your findings in the 'pole_markings' property of the JSON structure below.

        JSON Raw Response:
        {{     
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""yellow\"",
            \""seam_markings\"": \""seam markings\"",
            \""pole_markings\"": \""pole markings\""
        }}";

    public static string GetNlpPrompt(string json) => $@"
        Convert the following JSON into an Azure AI Search natural language processing (NLP) query. Ensure the output is a concise, complete sentence suitable for search input, and contains only the query without any additional text.

        JSON:
        {json}";
}