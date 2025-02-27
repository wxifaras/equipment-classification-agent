﻿namespace equipment_classification_agent_api.Prompts;

public class CorePrompts
{
    public static string GetImageMarkingsExtractionsPrompt(string manufacturers) => $@"
        Your task is to analyze one or more pictures of a golf ball and extract details from the images with a **hallucination score strictly less than 1**. 
        
        **Key Requirements:**
        - Extract **all text, markings, and symbols** (arrows, angle brackets, lines, etc.) along with their colors **exactly as they appear**.
        - **Angle brackets ('<', '>') must never be missed if present on the ball.**
        - **Do not rephrase, assume, infer, or add characters that do not appear in the image.**
        - If symbols enclose a word (e.g., `< word >`, `| word |`), return them exactly as they appear.
        - **Never describe symbols as words** (e.g., do not say `angle bracket around something` or `pipes surrounding something`).
        - If markings are** partially obscured** or **run off the ball**, ignore them rather than making assumptions.
        - Maintain** consistent results** across multiple evaluations.
        
        ### Instructions:
        1. **Manufacturer**: Identify the golf ball manufacturer.
           - The manufacturer **must** match one of the following: {manufacturers}.  
           - If no match is found, **return 'unknown'**.
           - Store the result in the 'manufacturer' field of the JSON.

        2. **Color**: Identify the golf ball's primary color.  
           - Store it in the 'color' field.

        3. **Markings**: Extract** all visible text and symbols** exactly as they appear.  
           - **Do not omit `<` brackets if they are present.**
           - Capture special characters without rewording or describing them.
           - Maintain** original order and spacing**.  
           - Store the result in the 'markings' field.
        
        4. **Thought Process**: 
           - Provide a brief explanation of your thought process in the 'thought_process' field. 
           - And how you are determining manufacturer, colour, and markings.
           - Provide any additional context that may help the next analyst understand your reasoning.

        ### JSON Response Format:
        JSON Raw Response:
        {{
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""some color\"",
            \""markings\"": \""some markings\""
            \""thought_process\"": \""explanation of thought process\""
        }}";

    public static string GetFinalImageMarkingsExtractionsPrompt(string manufacturers, string json_list) => $@"
        You have received multiple JSON objects representing different color, manufacturer, markings, details, and information extracted from images of a golf ball. 
        Your task is to **consolidate the most accurate information** while maintaining **consistency** and ensuring a **hallucination score less than 1** .
        
        **Critical Rules:**
        - **Angle brackets ('<', '>') must never be missed.**
        - **Maintain consistency** across repeated analyses.
        - **Do not infer, assume, or add missing data.**
        - If markings conflict across images, select the **most accurate** version.
        - Preserve **original text and symbols exactly** without paraphrasing.
    
        ### Instructions:
        - The **manufacturer** must match one of the manufacturers from the following list: {manufacturers}. If there is no match, or you are not sure, you **must** set the manufacturer as 'unknown'.
        - The color should be the most representative color based on the image and the data.
        - For the markings, ensure you capture any relevant text and symbols exactly as they appear, with their corresponding colors. If there are any conflicting markings, choose the one that best represents the ball's appearance.
        - In the case of duplicate markings, consolidate or choose the most accurate version.
        
        ### Based on the provided JSON objects and what you see in the images, please return a consolidated JSON response that best represents the image and provides the most accurate and detailed information about the golf ball, including:
        - The manufacturer of the golf ball.
        - The color of the ball.
        - The markings on the ball.
        - The JSON response contains only extracted data exactly as found in the image, without alterations or inferred details. Do not add words, reword, or structure data beyond its original form.
        - Ensure each property is represented in natural language, which will be used for Azure AI Search. Do *not* use fragmented sentences or phrases. 

        ### Thought Process: 
        - Provide a brief explanation of your thought process in the 'thought_process' field. 
        - And how you are determining manufacturer, colour, and markings.
        - Provide any additional context that may help the next analyst understand your reasoning.

        Here are the JSON objects from the extraction process:

        {json_list}

        ### JSON Response Format:
        JSON Raw Response:
        {{
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""some color\"",
            \""markings\"": \""some markings\"",
            \""thought_process\"": \""explanation of thought process\""
        }}";

    public static string GetNlpPrompt(string json) => $@"
        Given the JSON at the bottom, you must extract  the **markings** field and convert this value into an **Azure AI Search natural language processing (NLP) query**. Ensure the output is a concise
        and in a complete sentence suitable for search input.
        
        ### Instructions:
        - You must **only use** the markings field from the JSON; ignore the manufacturer and color fields.
        - **Do not remove** any special characters such as percent symbols ('%'), ampersands ('&'), double angle brackets ('<< >>'), or single angle brackets ('< >') as these are critical to the search.
        - **Do not modify the original wording, symbols, or spacing**.
        - You must add quotes to important phrases or keywords that should be treated as a single entity in the search query. 
          You must add a + sign to the front of the phrase to indicate that it is a required term.
          For example, if the query is ""Find the best restaurants in New York,"" the result should be: +""best"" +""restaurants"" in +""New York""
        - Don't add quotes to phrases or keywords that already have quotes. 
        - Don't add extra words or extra characters to the query including slashes, brackets, or other punctuation unless they are part of the original query.

        JSON:
        {json}";
}

