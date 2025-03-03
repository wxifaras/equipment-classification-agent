namespace equipment_classification_agent_api.Prompts;
public class CorePrompts
{
    public static string GetImageMarkingsExtractionsPrompt(string manufacturers) => $@"
        Your task is to analyze one or more pictures of a golf ball and extract details from the images with a **hallucination score strictly less than 1**. 
        
        **Key Requirements:**
        - Extract markings such as **text, special characters, and symbols (arrows, angle brackets, lines, etc.)**  along with their colors **exactly as they appear**.
        - **Do not rephrase, assume, infer, or add characters that do not appear in the image.**
        - If special characters enclose a word (e.g., `< word >`, `| word |`), return them exactly as they appear.
        - **Never describe special characters as words** (e.g., do not return `angle bracket around something` or `pipes surrounding something`).
        - If markings are **partially obscured** or **run off the ball**, ignore them rather than making assumptions.
        - Maintain **consistent results** across multiple evaluations.
        - Don't infer or associate any text or marking with, or as, a brand or manufacturer.
        - Don't infer or associate any of the style or content of the image to a brand or manufacturer.
        - Don't infer or associate any brand with a manufacture.
        
        ### Instructions:
        1. **Manufacturer**
           - Attempt to match the manufacturer from this list {manufacturers} with only the text extracted from the image- don't use your pretrained knowledge to infer. If no match is found then you must set the 'manufacturer' field of the JSON to 'unknown'. Don't use any of your pretrained knowledge to infer who the manufacturer is.  
           - Store the result in the 'manufacturer' field of the JSON.

        2. **Color**: Identify the golf ball's primary color.  
           - Store it in the 'color' field.

        3. **Markings**: Extract **all visible text and symbols**
           - This will fall into two categories: 1) alphanumeric text which can be represented with characters and 2) symbols such as arrows or lines
           - For alphanumeric text, you must return this **exactly as they are shown**. This includes any characters which can be represented such as letters, <, >, !, @, #, $, %, ^, &, etc. Be sure that you don't
           - For symbols, describe them using words instead of textual representations. For example, describe an arrow as ""arrow pointing left,"" ""hollow arrow pointing right,"" or ""solid arrow pointing up"" based on its appearance.
           - Avoid using angle brackets (`<`, `>`) or other symbols (e.g., `|`, `-`) unless they are visibly part of the markings in the image. Do not recreate the symbol with characters. Instead, describe the shape, direction, and type of the symbol in words.
           - Maintain **exact** order and spacing of markings int he result, but use words to describe symbols, not characters.
           - Descriptions of symbols should be precise and unambiguous. Avoid converting symbols into textual forms such as arrows (`<---->` or `-->`).
           - Describe any backgrounds that the markings are on. For example, if a word is over a black box, you must return that the word you extracted is in a black box.
           - Store the result of both text and symbols in the 'markings' field.
        
        4. **Thought Process**: 
           - Provide a brief explanation of your thought process in the 'thought_process' field. 
           - And how you are determining manufacturer, colour, and markings.
           - Provide any additional context that may help the next analyst understand your reasoning.
           - If you are identifying a brand that is associated with the manufacturer. Explain, how you are getting this knowledge ?

        5. **Brand Explanation**:
           - Provide a explanation of how you are identifying the brand in the 'brand_explanation' field. 
           - Provide details on how you are determining the manufacturer for this brand. 
           - Explain, from where you are getting this knowledge from been able to confindently able to associate brand with the manufacturer.
        
        6. **Tags Explanation**:
           - In the tags_explanation field, provide a clear breakdown of when and why the symbols (tags) appear or do not appear in the LLM response.
           - Explain the logic behind the inclusion or exclusion of tags surrounding the text.
           - If certain words, phrases, or sections are tagged, describe the conditions that trigger their appearance.
           - If tags are omitted in some cases, explain why they are not required in those instances.

        ### JSON Response Format:
        JSON Raw Response:
        {{
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""some color\"",
            \""markings\"": \""some markings\""
            \""thought_process\"": \""explanation of thought process\"",
            \""brand_explanation\"": \""explanation of brand selection\"",
            \""tags_explanation\"": \""explanation on how tags are shown or omitted in the response \"",
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
        - Attempt to match the manufacturer from this list {manufacturers} with only the text extracted from the image- don't use your pretrained knowledge to infer. If no match is found then you must set the 'manufacturer' field of the JSON to 'unknown'. Don't use any of your pretrained knowledge to infer who the manufacturer is.  
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

         ###Brand Explanation:
           - Provide a explanation of how you are identifying the brand in the 'brand_explanation' field. 
           - Provide details on how you are determining the manufacturer for this brand. 
           - Explain, from where you are getting this knowledge from been able to confindently able to associate brand with the manufacturer.

        ### Tags Explanation:
           - In the tags_explanation field, provide a clear breakdown of when and why the symbols (tags) appear or do not appear in the LLM response.
           - Explain the logic behind the inclusion or exclusion of tags surrounding the text.
           - If certain words, phrases, or sections are tagged, describe the conditions that trigger their appearance.
           - If tags are omitted in some cases, explain why they are not required in those instances.

        Here are the JSON objects from the extraction process:

        {json_list}

        ### JSON Response Format:
        JSON Raw Response:
       {{
            \""manufacturer\"": \""some manufacturer\"",
            \""color\"": \""some color\"",
            \""markings\"": \""some markings\""
            \""thought_process\"": \""explanation of thought process\"",
            \""brand_explanation\"": \""explanation of brand selection\"",
            \""tags_explanation\"": \""explanation on how tags are shown or omitted in the response \"",
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