namespace equipment_classification_agent_api.Prompts;

public class CorePrompts
{
    public static string GetImageMarkingsExtractionsPrompt(string manufacturers) => $@"
        Your task is to analyze one or more pictures of a golf ball and extract details from the images. Ensure that all text, markings, 
        and symbols (such as arrows, angle brackets, lines, or other characters) along with their colors, are included **exactly as they appear** if
        they can be represented by a character. Do **not** rephrase or describe any symbols, including angle brackets, carets, percent signs, etc.
        If you see a an angle bracket ('>'), percent symbol ('%') or an ampersand ('&'), return them as '>' '%', '&', etc. Do **not** describe these symbols
        as words (e.g., 'angle bracket', 'percent' or 'ampersand'). It is also critical that you capture the color of any symbol. For example, if you see
        a red arrow underneath the text 'Titleist Pro V1', you will return 'Titleist Pro V1 with a red arrow underneath'.
        
        Instructions:
        1. Manufacturer: Identify the name of the golf ball manufacturer. Store your findings in the 'manufacturer' property of the JSON structure below.
            You must be sure to only use the manufacturers from the following list: {manufacturers}

            If the markings you extract do not match one of the manufacturers from the list or you are not sure, please respond with 'unknown' for the manufacturer.
        2. Color: Identify the color of the ball. Store the color in the 'color' property of the JSON structure below.
        3. Markings: Identify any text or a combination of text and symbols on a picture. If there are symbols near or around text, capture the type of symbol along with its color. You
           **must not** project any markings if you cannot clearly identify what they are. For example, if markings are partially obscured or run off of the ball, don't make any assumptions about what they are.
           Store your findings in the 'markings' property of the JSON structure below. Remember to include the symbols exactly as they appear if they can be represented with characters, 
           without describing them.

        JSON Raw Response:
        {{     
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""yellow\"",
            \""markings\"": \""markings\""
        }}";

    public static string GetFinalImageMarkingsExtractionsPrompt(string manufacturers, string json_list) => $@"
        You have received multiple JSON objects representing different color, manufacturer, markings, details, and information extracted from images of a golf ball. Your task is
        to analyze and consolidate all the information into a single JSON object that best represents the golf ball's features from the images provided. Please
        carefully combine the data, remove any duplicates, and provide the most relevant details. Do **not** rephrase or describe any symbols, including angle brackets,
        carets, percent signs, etc. If you see a an angle bracket ('>'), percent symbol ('%') or an ampersand ('&'), return them as '>' '%', '&', etc. Do **not** describe these symbols
        as words (e.g., 'angle bracket', 'percent' or 'ampersand').

        Here are the JSON objects from the extraction process:

        {json_list}

        Instructions:
        - The manufacturer should match one of the manufacturers from the following list: {manufacturers}. If there is no match, or you are not sure, you **must** set the manufacturer as 'unknown'.
        - The color should be the most representative color based on the image and the data.
        - For the markings, ensure you capture any relevant text and symbols exactly as they appear, with their corresponding colors. If there are any conflicting markings, 
          choose the one that best represents the ball's appearance.
        - In case of duplicate markings, consolidate or choose the most accurate version.
        
        Based on the provided JSON objects and what you see in the images, please return a consolidated JSON response that best represents the image and provides the most accurate and detailed information about the golf ball, including:
        - The manufacturer of the golf ball.
        - The color of the ball.
        - The markings on the ball.

        Ensure each property is represented in natural language, which will be used for Azure AI Search. Do *not* use fragmented sentences or phrases. The JSON response should be structured as follows:

        JSON Raw Response:
        {{
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""some color\"",
            \""markings\"": \""some markings\""
        }}";

    public static string GetNlpPrompt(string json) => $@"
        Given the JSON at the bottom, you must extract only the markings field and convert this value into an Azure AI Search natural language processing (NLP) query. Ensure the output is a concise
        and in a complete sentence suitable for search input.
        
        Instructions:
        - You must only use the markings field from the JSON; ignore the manufacturer and color fields.
        - You must **not** remove any special characters such as percent symbols ('%'), ampersands ('&'), double angle brackets ('<< >>'), or single angle brackets ('< >') as these are critical to the search.
        - You must add quotes to important phrases or keywords that should be treated as a single entity in the search query. 
          You must add a + sign to the front of the phrase to indicate that it is a required term.
          For example, if the query is ""Find the best restaurants in New York,"" the result should be: +""best"" +""restaurants"" in +""New York""
        - Don't add quotes to phrases or keywords that already have quotes. 
        - Don't add extra words or extra characters to the query including slashes, brackets, or other punctuation unless they are part of the original query.

        JSON:
        {json}";
}