using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.ChatCompletion;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Keywords extraction service for PathRAG queries
/// Extracts high-level (relationship) and low-level (entity) keywords
/// Uses EXACT prompts from Python PathRAG's prompt.py PROMPTS["keywords_extraction"]
/// </summary>
public class KeywordsExtractionService : IKeywordsExtractionService
{
    private readonly IChatCompletionService _chatService;

    public KeywordsExtractionService(IChatCompletionService chatService)
    {
        _chatService = chatService;
    }

    public async Task<KeywordsExtractionResult> ExtractKeywordsAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        // Use exact Python PathRAG prompt from prompt.py PROMPTS["keywords_extraction"]
        var prompt = BuildKeywordsExtractionPrompt(query);

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        var responseText = response.Content ?? "";

        return ParseKeywordsResponse(responseText);
    }

    private static string BuildKeywordsExtractionPrompt(string query)
    {
        var examples = GetKeywordsExtractionExamples();

        // Exact prompt from Python PathRAG's prompt.py PROMPTS["keywords_extraction"]
        return $@"---Role---

You are a helpful assistant tasked with identifying both high-level and low-level keywords in the user's query.

---Goal---

Given the query, list both high-level and low-level keywords. High-level keywords focus on overarching concepts or themes, while low-level keywords focus on specific entities, details, or concrete terms.

---Instructions---

- Output the keywords in JSON format.
- The JSON should have two keys:
  - ""high_level_keywords"" for overarching concepts or themes.
  - ""low_level_keywords"" for specific entities or details.

######################
-Examples-
######################
{examples}

#############################
-Real Data-
######################
Query: {query}
######################
The `Output` should be human text, not unicode characters. Keep the same language as `Query`.
Output:

";
    }

    private static string GetKeywordsExtractionExamples()
    {
        // Exact examples from Python PathRAG's prompt.py PROMPTS["keywords_extraction_examples"]
        return @"Example 1:

Query: ""How does international trade influence global economic stability?""
################
Output:
{
  ""high_level_keywords"": [""International trade"", ""Global economic stability"", ""Economic impact""],
  ""low_level_keywords"": [""Trade agreements"", ""Tariffs"", ""Currency exchange"", ""Imports"", ""Exports""]
}
#############################
Example 2:

Query: ""What are the environmental consequences of deforestation on biodiversity?""
################
Output:
{
  ""high_level_keywords"": [""Environmental consequences"", ""Deforestation"", ""Biodiversity loss""],
  ""low_level_keywords"": [""Species extinction"", ""Habitat destruction"", ""Carbon emissions"", ""Rainforest"", ""Ecosystem""]
}
#############################
Example 3:

Query: ""What is the role of education in reducing poverty?""
################
Output:
{
  ""high_level_keywords"": [""Education"", ""Poverty reduction"", ""Socioeconomic development""],
  ""low_level_keywords"": [""School access"", ""Literacy rates"", ""Job training"", ""Income inequality""]
}
#############################";
    }

    private static KeywordsExtractionResult ParseKeywordsResponse(string response)
    {
        var highLevel = new List<string>();
        var lowLevel = new List<string>();

        try
        {
            // Extract JSON from response (matching Python's regex: r"\{.*\}")
            var match = Regex.Match(response, @"\{.*\}", RegexOptions.Singleline);
            if (match.Success)
            {
                var jsonStr = match.Value;
                using var doc = JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;

                if (root.TryGetProperty("high_level_keywords", out var highElement))
                {
                    foreach (var kw in highElement.EnumerateArray())
                    {
                        var keyword = kw.GetString();
                        if (!string.IsNullOrWhiteSpace(keyword))
                            highLevel.Add(keyword);
                    }
                }

                if (root.TryGetProperty("low_level_keywords", out var lowElement))
                {
                    foreach (var kw in lowElement.EnumerateArray())
                    {
                        var keyword = kw.GetString();
                        if (!string.IsNullOrWhiteSpace(keyword))
                            lowLevel.Add(keyword);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Return empty collections on parse error
        }

        return new KeywordsExtractionResult(highLevel, lowLevel);
    }
}

