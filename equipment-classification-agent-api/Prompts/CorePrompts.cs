﻿namespace equipment_classification_agent_api.Prompts;

public class CorePrompts
{
    public static string GetImageMarkingsExtractionsPrompt(string manufacturers) => $@"
        Your task is to analyze one or more pictures of a golf ball and extract details from the images. Ensure that all text, markings, 
        and symbols (such as arrows, angle brackets, lines, or other characters) along with their colors, are included **exactly as they appear** if
        they can be represented by a character. Do **not** rephrase or describe any symbols, including angle brackets, carets, percent signs, etc.
        For example, if you see '<< Titleist Pro V1 >>', you will return '<< Titleist Pro V1 >>', not 'Titleist Pro V1 surrounded by angle brackets'.
        If you see a percent symbol ('%') or an ampersand ('&'), return them as '%', '&', etc. Do **not** describe these symbols as words (e.g., 'percent'
        or 'ampersand'). It is also critical that you capture the color of any symbol. For example, if you see a red arrow underneath the text
        'Titleist Pro V1', you will return 'Titleist Pro V1 with a red arrow underneath'.
        
        Instructions:
        1. Manufacturer: Identify the name of the golf ball manufacturer. Store your findings in the 'manufacturer' property of the JSON structure below.
            You must be sure to only use the manufacturers from the following list: {manufacturers}

            If detail you extract does not match one of the manufacturers from the list or you are not sure, please respond use 'unknown' for the manufacturer.
        2. Color: Identify the color of the ball. Store the color in the 'color' property of the JSON structure below.
        3. Markings: Identify any text or a combination of text and symbols on a picture. If there are symbols near or around text, capture the type of symbol along with its color. Store your findings in the 'markings' property of the JSON structure below. Remember to include the symbols exactly as they appear if they can be represented with characters, without describing them.

        JSON Raw Response:
        {{     
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""yellow\"",
            \""markings\"": \""markings\""
        }}";

    public static string GetFinalImageMarkingsExtractionsPrompt(string manufacturers, string json_list) => $@"
        You have received multiple JSON objects representing different markings, details, and information extracted from images of a golf ball. Your task is
        to analyze and consolidate all the information into a single JSON object that best represents the golf ball's features from the images provided. Please
        carefully combine the data, remove any duplicates, and provide the most relevant details. Do **not** rephrase or describe any symbols, including angle brackets,
        carets, percent signs, etc. For example, if you see '<< Titleist Pro V1 >>', in the JSON objects, you will return the text verbatim as '<< Titleist Pro V1 >>'.
        Likewise, if you see a percent symbol ('%') or an ampersand ('&'), return them as '%', '&', etc.

        Here are the JSON objects from the extraction process:

        {json_list}

        Please note the following guidelines:
        - The manufacturer should be the correct and most specific match, considering this list of manufacturers provided: {manufacturers}.
        - The color should be the most representative color based on the image and the data.
        - For the markings, ensure you capture any relevant text and symbols exactly as they appear, with their corresponding colors. If there are any conflicting markings, choose the one that best represents the ball's appearance.
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
        Convert the following JSON into an Azure AI Search natural language processing (NLP) query. Ensure the output is a concise, complete sentence suitable for search input, and contains only the query
        without any additional text. You must **not**  remove any special characters such as percent symbols ('%'), ampersands ('&'), or angle brackets ('<< >>') as these are critical to the search.

        JSON:
        {json}";
}