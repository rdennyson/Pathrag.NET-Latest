using Microsoft.SemanticKernel.ChatCompletion;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Service for merging entities and relationships matching Python PathRAG's
/// _merge_nodes_then_upsert and _merge_edges_then_upsert functions in operate.py
/// </summary>
public class EntityMergingService : IEntityMergingService
{
    private readonly IGraphRepository _graphRepository;
    private readonly IChatCompletionService _chatService;
    private readonly PathRAGSettings _settings;

    // Python PathRAG's GRAPH_FIELD_SEP from prompt.py
    private const string GraphFieldSep = "<SEP>";

    public EntityMergingService(
        IGraphRepository graphRepository,
        IChatCompletionService chatService,
        PathRAGSettings settings)
    {
        _graphRepository = graphRepository;
        _chatService = chatService;
        _settings = settings;
    }

    /// <summary>
    /// Merge entity data with existing entity (matching Python's _merge_nodes_then_upsert)
    /// </summary>
    public async Task<GraphEntity> MergeAndUpsertEntityAsync(
        ExtractedEntityDto newEntity,
        CancellationToken cancellationToken = default)
    {
        var existingEntity = await _graphRepository.GetEntityByNameAsync(newEntity.EntityName, newEntity.DocumentId, cancellationToken: cancellationToken);

        var alreadyEntityTypes = new List<string>();
        var alreadySourceIds = new List<string>();
        var alreadyDescriptions = new List<string>();

        if (existingEntity != null)
        {
            alreadyEntityTypes.Add(existingEntity.EntityType);
            if (!string.IsNullOrEmpty(existingEntity.SourceId))
                alreadySourceIds.AddRange(SplitByGraphFieldSep(existingEntity.SourceId));
            if (!string.IsNullOrEmpty(existingEntity.Description))
                alreadyDescriptions.Add(existingEntity.Description);
        }

        // Determine entity type by most common (matching Python's Counter logic)
        var allTypes = new List<string> { newEntity.EntityType };
        allTypes.AddRange(alreadyEntityTypes);
        var entityType = allTypes
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .First().Key;

        // Merge descriptions with GRAPH_FIELD_SEP (matching Python)
        var allDescriptions = new List<string> { newEntity.Description };
        allDescriptions.AddRange(alreadyDescriptions);
        var mergedDescription = string.Join(GraphFieldSep, allDescriptions.Distinct().OrderBy(d => d));

        // Merge source IDs with GRAPH_FIELD_SEP (matching Python)
        var allSourceIds = new List<string> { newEntity.SourceId };
        allSourceIds.AddRange(alreadySourceIds);
        var mergedSourceId = string.Join(GraphFieldSep, allSourceIds.Distinct());

        // Summarize description if too long (matching Python's _handle_entity_relation_summary)
        mergedDescription = await HandleEntityRelationSummaryAsync(
            newEntity.EntityName, mergedDescription, cancellationToken);

        var graphEntity = new GraphEntity
        {
            Id = existingEntity?.Id ?? Guid.NewGuid(),
            EntityName = newEntity.EntityName,
            EntityType = entityType,
            Description = mergedDescription,
            SourceId = mergedSourceId,
            DocumentId = newEntity.DocumentId
        };

        return await _graphRepository.UpsertEntityAsync(graphEntity, cancellationToken);
    }

    /// <summary>
    /// Merge relationship data with existing relationship (matching Python's _merge_edges_then_upsert)
    /// </summary>
    public async Task<GraphRelationship> MergeAndUpsertRelationshipAsync(
        ExtractedRelationshipDto newRelationship,
        CancellationToken cancellationToken = default)
    {
        var existingRel = await _graphRepository.GetRelationshipAsync(
            newRelationship.SourceEntity, newRelationship.TargetEntity, newRelationship.DocumentId, cancellationToken: cancellationToken);

        var alreadyWeights = new List<double>();
        var alreadySourceIds = new List<string>();
        var alreadyDescriptions = new List<string>();
        var alreadyKeywords = new List<string>();

        if (existingRel != null)
        {
            alreadyWeights.Add(existingRel.Weight);
            if (!string.IsNullOrEmpty(existingRel.SourceId))
                alreadySourceIds.AddRange(SplitByGraphFieldSep(existingRel.SourceId));
            if (!string.IsNullOrEmpty(existingRel.Description))
                alreadyDescriptions.Add(existingRel.Description);
            if (!string.IsNullOrEmpty(existingRel.Keywords))
                alreadyKeywords.AddRange(SplitByGraphFieldSep(existingRel.Keywords));
        }

        // Sum weights (matching Python)
        var weight = newRelationship.Weight + alreadyWeights.Sum();

        // Merge descriptions with GRAPH_FIELD_SEP (matching Python)
        var allDescriptions = new List<string> { newRelationship.Description };
        allDescriptions.AddRange(alreadyDescriptions);
        var mergedDescription = string.Join(GraphFieldSep, allDescriptions.Distinct().OrderBy(d => d));

        // Merge keywords with GRAPH_FIELD_SEP (matching Python)
        var allKeywords = new List<string> { newRelationship.Keywords };
        allKeywords.AddRange(alreadyKeywords);
        var mergedKeywords = string.Join(GraphFieldSep, allKeywords.Distinct().OrderBy(k => k));

        // Merge source IDs with GRAPH_FIELD_SEP (matching Python)
        var allSourceIds = new List<string> { newRelationship.SourceId };
        allSourceIds.AddRange(alreadySourceIds);
        var mergedSourceId = string.Join(GraphFieldSep, allSourceIds.Distinct());

        // Ensure source and target entities exist (matching Python's logic)
        await EnsureEntityExistsAsync(newRelationship.SourceEntity, mergedSourceId, mergedDescription, newRelationship.DocumentId, cancellationToken);
        await EnsureEntityExistsAsync(newRelationship.TargetEntity, mergedSourceId, mergedDescription, newRelationship.DocumentId, cancellationToken);

        // Summarize description if too long (matching Python's _handle_entity_relation_summary)
        mergedDescription = await HandleEntityRelationSummaryAsync(
            $"({newRelationship.SourceEntity}, {newRelationship.TargetEntity})", mergedDescription, cancellationToken);

        var graphRel = new GraphRelationship
        {
            Id = existingRel?.Id ?? Guid.NewGuid(),
            SourceEntityName = newRelationship.SourceEntity,
            TargetEntityName = newRelationship.TargetEntity,
            Description = mergedDescription,
            Keywords = mergedKeywords,
            Weight = weight,
            SourceId = mergedSourceId,
            DocumentId = newRelationship.DocumentId
        };

        return await _graphRepository.UpsertRelationshipAsync(graphRel, cancellationToken);
    }

    /// <summary>
    /// Ensure entity exists, create with UNKNOWN type if not (matching Python's logic in _merge_edges_then_upsert)
    /// </summary>
    private async Task EnsureEntityExistsAsync(
        string entityName,
        string sourceId,
        string description,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!await _graphRepository.EntityExistsAsync(entityName, documentId, cancellationToken))
        {
            var entity = new GraphEntity
            {
                Id = Guid.NewGuid(),
                EntityName = entityName,
                EntityType = "UNKNOWN",  // Python uses '"UNKNOWN"'
                Description = description,
                SourceId = sourceId
            };
            entity.DocumentId = documentId;
            await _graphRepository.UpsertEntityAsync(entity, cancellationToken);
        }
    }

    /// <summary>
    /// Handle entity/relation summary (matching Python's _handle_entity_relation_summary)
    /// Summarizes description if it exceeds token limit
    /// </summary>
    private async Task<string> HandleEntityRelationSummaryAsync(
        string entityOrRelationName,
        string description,
        CancellationToken cancellationToken)
    {
        // Simple token estimation (4 chars per token average)
        var estimatedTokens = description.Length / 4;
        if (estimatedTokens < _settings.EntitySummaryToMaxTokens)
        {
            return description;
        }

        // Use LLM to summarize (matching Python's prompt)
        var descriptionList = description.Split([GraphFieldSep], StringSplitOptions.RemoveEmptyEntries);
        var prompt = $@"You are a helpful assistant responsible for generating a comprehensive summary of the data provided below.
Given one or two entities, and a list of descriptions, all related to the same entity or group of entities.
Please concatenate all of these into a single, comprehensive description. Make sure to include information collected from all the descriptions.
If the provided descriptions are contradictory, please resolve the contradictions and provide a single, coherent summary.
Make sure it is written in third person, and include the entity names so we the have full context.
Use English as output language.

#######
-Data-
Entities: {entityOrRelationName}
Description List: {string.Join("\n", descriptionList)}
#######
Output:
";

        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);

        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        return response.Content ?? description;
    }

    /// <summary>
    /// Split string by GRAPH_FIELD_SEP (matching Python's split_string_by_multi_markers)
    /// </summary>
    private static List<string> SplitByGraphFieldSep(string input)
    {
        if (string.IsNullOrEmpty(input)) return [];
        return input.Split([GraphFieldSep], StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
