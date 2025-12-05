using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Interface for entity and relationship merging service
/// Matches Python PathRAG's _merge_nodes_then_upsert and _merge_edges_then_upsert functions
/// </summary>
public interface IEntityMergingService
{
    /// <summary>
    /// Merge entity data with existing entity and upsert
    /// </summary>
    Task<GraphEntity> MergeAndUpsertEntityAsync(
        ExtractedEntityDto newEntity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge relationship data with existing relationship and upsert
    /// </summary>
    Task<GraphRelationship> MergeAndUpsertRelationshipAsync(
        ExtractedRelationshipDto newRelationship,
        CancellationToken cancellationToken = default);
}

