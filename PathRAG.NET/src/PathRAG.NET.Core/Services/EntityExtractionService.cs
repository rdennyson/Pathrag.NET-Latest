using System.Text.RegularExpressions;
using Microsoft.SemanticKernel.ChatCompletion;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Entity and relationship extraction service using LLM
/// Corresponds to Python PathRAG's extract_entities function in operate.py
/// Uses EXACT prompts from Python PathRAG's prompt.py
/// </summary>
public class EntityExtractionService : IEntityExtractionService
{
    private readonly IChatCompletionService _chatService;
    private readonly PathRAGSettings _settings;

    // Python PathRAG delimiters from prompt.py
    private const string TupleDelimiter = "<|>";
    private const string RecordDelimiter = "##";
    private const string CompletionDelimiter = "<|COMPLETE|>";
    private const string GraphFieldSep = "<SEP>";

    // Default entity types from Python PathRAG
    private static readonly string[] DefaultEntityTypes = ["organization", "person", "geo", "event", "category"];

    public EntityExtractionService(IChatCompletionService chatService, PathRAGSettings settings)
    {
        _chatService = chatService;
        _settings = settings;
    }

    public async Task<(IEnumerable<ExtractedEntityDto> Entities, IEnumerable<ExtractedRelationshipDto> Relationships)>
        ExtractEntitiesAndRelationshipsAsync(
            string text,
            string sourceId,
            CancellationToken cancellationToken = default)
    {
        try
        {
            // Build prompt using exact Python PathRAG format
            var prompt = BuildEntityExtractionPrompt(text);

            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(prompt);

            // Initial extraction
            var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var finalResult = response.Content ?? "";
            var history = new ChatHistory();
            history.AddUserMessage(prompt);
            history.AddAssistantMessage(finalResult);

            // Gleaning: Multiple extraction passes (matching Python's entity_extract_max_gleaning)
            var maxGleaning = _settings.EntityExtractMaxGleaning;
            for (int i = 0; i < maxGleaning; i++)
            {
                // Continue extraction prompt (from Python PathRAG)
                history.AddUserMessage(ContinueExtractionPrompt);
                var gleanResult = await _chatService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
                var gleanContent = gleanResult.Content ?? "";
                history.AddAssistantMessage(gleanContent);
                finalResult += gleanContent;

                if (i == maxGleaning - 1) break;

                // Check if more entities exist
                history.AddUserMessage(IfLoopExtractionPrompt);
                var loopResult = await _chatService.GetChatMessageContentAsync(history, cancellationToken: cancellationToken);
                var loopContent = loopResult.Content?.Trim().Trim('"').Trim('\'').ToLowerInvariant() ?? "";
                history.AddAssistantMessage(loopContent);

                if (loopContent != "yes") break;
            }

            return ParseExtractionResponse(finalResult, sourceId);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            throw;
        }
    }

    private static string BuildEntityExtractionPrompt(string text)
    {
        var entityTypes = string.Join(",", DefaultEntityTypes);
        var examples = GetExtractionExamples();

        // Exact prompt from Python PathRAG's prompt.py PROMPTS["entity_extraction"]
        return $@"-Goal-
Given a text document that is potentially relevant to this activity and a list of entity types, identify all entities of those types from the text and all relationships among the identified entities.
Use English as output language.

-Steps-
1. Identify all entities. For each identified entity, extract the following information:
- entity_name: Name of the entity, use same language as input text. If English, capitalized the name.
- entity_type: One of the following types: [{entityTypes}]
- entity_description: Comprehensive description of the entity's attributes and activities
Format each entity as (""entity""{TupleDelimiter}<entity_name>{TupleDelimiter}<entity_type>{TupleDelimiter}<entity_description>)

2. From the entities identified in step 1, identify all pairs of (source_entity, target_entity) that are *clearly related* to each other.
For each pair of related entities, extract the following information:
- source_entity: name of the source entity, as identified in step 1
- target_entity: name of the target entity, as identified in step 1
- relationship_description: explanation as to why you think the source entity and the target entity are related to each other
- relationship_strength: a numeric score indicating strength of the relationship between the source entity and target entity
- relationship_keywords: one or more high-level key words that summarize the overarching nature of the relationship, focusing on concepts or themes rather than specific details
Format each relationship as (""relationship""{TupleDelimiter}<source_entity>{TupleDelimiter}<target_entity>{TupleDelimiter}<relationship_description>{TupleDelimiter}<relationship_keywords>{TupleDelimiter}<relationship_strength>)

3. Identify high-level key words that summarize the main concepts, themes, or topics of the entire text. These should capture the overarching ideas present in the document.
Format the content-level key words as (""content_keywords""{TupleDelimiter}<high_level_keywords>)

4. Return output in English as a single list of all the entities and relationships identified in steps 1 and 2. Use **{RecordDelimiter}** as the list delimiter.

5. When finished, output {CompletionDelimiter}

######################
-Examples-
######################
{examples}

#############################
-Real Data-
######################
Entity_types: {entityTypes}
Text: {text}
######################
Output:
";
    }

    private static string GetExtractionExamples()
    {
        // Example 1 from Python PathRAG prompt.py
        return $@"Example 1:

Entity_types: [person, technology, mission, organization, location]
Text:
while Alex clenched his jaw, the buzz of frustration dull against the backdrop of Taylor's authoritarian certainty. It was this competitive undercurrent that kept him alert, the sense that his and Jordan's shared commitment to discovery was an unspoken rebellion against Cruz's narrowing vision of control and order.

Then Taylor did something unexpected. They paused beside Jordan and, for a moment, observed the device with something akin to reverence. ""If this tech can be understood..."" Taylor said, their voice quieter, ""It could change the game for us. For all of us.""

The underlying dismissal earlier seemed to falter, replaced by a glimpse of reluctant respect for the gravity of what lay in their hands. Jordan looked up, and for a fleeting heartbeat, their eyes locked with Taylor's, a wordless clash of wills softening into an uneasy truce.

It was a small transformation, barely perceptible, but one that Alex noted with an inward nod. They had all been brought here by different paths
################
Output:
(""entity""{TupleDelimiter}""Alex""{TupleDelimiter}""person""{TupleDelimiter}""Alex is a character who experiences frustration and is observant of the dynamics among other characters.""){RecordDelimiter}
(""entity""{TupleDelimiter}""Taylor""{TupleDelimiter}""person""{TupleDelimiter}""Taylor is portrayed with authoritarian certainty and shows a moment of reverence towards a device, indicating a change in perspective.""){RecordDelimiter}
(""entity""{TupleDelimiter}""Jordan""{TupleDelimiter}""person""{TupleDelimiter}""Jordan shares a commitment to discovery and has a significant interaction with Taylor regarding a device.""){RecordDelimiter}
(""entity""{TupleDelimiter}""Cruz""{TupleDelimiter}""person""{TupleDelimiter}""Cruz is associated with a vision of control and order, influencing the dynamics among other characters.""){RecordDelimiter}
(""entity""{TupleDelimiter}""The Device""{TupleDelimiter}""technology""{TupleDelimiter}""The Device is central to the story, with potential game-changing implications, and is revered by Taylor.""){RecordDelimiter}
(""relationship""{TupleDelimiter}""Alex""{TupleDelimiter}""Taylor""{TupleDelimiter}""Alex is affected by Taylor's authoritarian certainty and observes changes in Taylor's attitude towards the device.""{TupleDelimiter}""power dynamics, perspective shift""{TupleDelimiter}7){RecordDelimiter}
(""relationship""{TupleDelimiter}""Alex""{TupleDelimiter}""Jordan""{TupleDelimiter}""Alex and Jordan share a commitment to discovery, which contrasts with Cruz's vision.""{TupleDelimiter}""shared goals, rebellion""{TupleDelimiter}6){RecordDelimiter}
(""relationship""{TupleDelimiter}""Taylor""{TupleDelimiter}""Jordan""{TupleDelimiter}""Taylor and Jordan interact directly regarding the device, leading to a moment of mutual respect and an uneasy truce.""{TupleDelimiter}""conflict resolution, mutual respect""{TupleDelimiter}8){RecordDelimiter}
(""relationship""{TupleDelimiter}""Jordan""{TupleDelimiter}""Cruz""{TupleDelimiter}""Jordan's commitment to discovery is in rebellion against Cruz's vision of control and order.""{TupleDelimiter}""ideological conflict, rebellion""{TupleDelimiter}5){RecordDelimiter}
(""relationship""{TupleDelimiter}""Taylor""{TupleDelimiter}""The Device""{TupleDelimiter}""Taylor shows reverence towards the device, indicating its importance and potential impact.""{TupleDelimiter}""reverence, technological significance""{TupleDelimiter}9){RecordDelimiter}
(""content_keywords""{TupleDelimiter}""power dynamics, ideological conflict, discovery, rebellion""){CompletionDelimiter}
#############################";
    }

    private static (IEnumerable<ExtractedEntityDto> Entities, IEnumerable<ExtractedRelationshipDto> Relationships)
        ParseExtractionResponse(string response, string sourceId)
    {
        var entities = new List<ExtractedEntityDto>();
        var relationships = new List<ExtractedRelationshipDto>();

        // Split by record delimiter and completion delimiter (matching Python's split_string_by_multi_markers)
        var records = SplitByMultiMarkers(response, [RecordDelimiter, CompletionDelimiter]);

        foreach (var record in records)
        {
            // Extract content within parentheses (matching Python's regex: r"\((.*)\)")
            var match = Regex.Match(record, @"\((.*)\)", RegexOptions.Singleline);
            if (!match.Success) continue;

            var content = match.Groups[1].Value;
            // Split by tuple delimiter but DON'T clean yet - Python checks raw first attribute
            var rawAttributes = SplitByMultiMarkers(content, [TupleDelimiter]);
            if (rawAttributes.Count == 0) continue;

            // Python checks: record_attributes[0] != '"entity"' - matches the raw string WITH quotes
            var firstAttr = rawAttributes[0].Trim();

            // Parse entity (matching Python's _handle_single_entity_extraction)
            // Python: if len(record_attributes) < 4 or record_attributes[0] != '"entity"'
            if (rawAttributes.Count >= 4 && firstAttr == "\"entity\"")
            {
                var entityName = CleanString(rawAttributes[1]).ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(entityName)) continue;

                var entityType = CleanString(rawAttributes[2]).ToUpperInvariant();
                var description = CleanString(rawAttributes[3]);

                entities.Add(new ExtractedEntityDto(entityName, entityType, description, sourceId));
            }
            // Parse relationship (matching Python's _handle_single_relationship_extraction)
            // Python: if len(record_attributes) < 5 or record_attributes[0] != '"relationship"'
            else if (rawAttributes.Count >= 5 && firstAttr == "\"relationship\"")
            {
                var source = CleanString(rawAttributes[1]).ToUpperInvariant();
                var target = CleanString(rawAttributes[2]).ToUpperInvariant();
                if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target)) continue;

                var description = CleanString(rawAttributes[3]);
                var keywords = CleanString(rawAttributes[4]);

                // Weight is the last attribute if it's a float (matching Python's is_float_regex check)
                var weight = 1.0;
                var lastAttr = rawAttributes[^1].Trim();
                if (rawAttributes.Count > 5 && IsFloat(lastAttr))
                {
                    weight = double.Parse(lastAttr);
                }

                relationships.Add(new ExtractedRelationshipDto(source, target, description, keywords, weight, sourceId));
            }
        }

        return (entities, relationships);
    }

    /// <summary>
    /// Split string by multiple markers (matching Python's split_string_by_multi_markers)
    /// </summary>
    private static List<string> SplitByMultiMarkers(string content, string[] markers)
    {
        if (markers.Length == 0) return [content];

        // Build regex pattern: marker1|marker2|...
        var pattern = string.Join("|", markers.Select(Regex.Escape));
        var results = Regex.Split(content, pattern);
        return results.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToList();
    }

    /// <summary>
    /// Check if string is a valid float (matching Python's is_float_regex)
    /// </summary>
    private static bool IsFloat(string value)
    {
        return Regex.IsMatch(value.Trim(), @"^[-+]?[0-9]*\.?[0-9]+$");
    }

    /// <summary>
    /// Clean string matching Python PathRAG's clean_str function
    /// </summary>
    private static string CleanString(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input.Trim().Trim('"').Trim('\'').Trim();
    }

    // Python PathRAG prompts from prompt.py
    private const string ContinueExtractionPrompt = "MANY entities were missed in the last extraction.  Add them below using the same format:\n";
    private const string IfLoopExtractionPrompt = "It appears some entities may have still been missed.  Answer YES | NO if there are still entities that need to be added.\n";
}

