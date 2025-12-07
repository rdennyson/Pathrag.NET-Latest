using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Graph.Interfaces;

/// <summary>
/// Interface for vector search operations on graph entities and relationships
/// Uses separate vector tables (EntityVectors, RelationshipVectors) matching Python PathRAG architecture
/// </summary>
public interface IGraphVectorRepository
{
    // Entity Vector Operations

    /// <summary>
    /// Search entities by vector similarity
    /// </summary>
    Task<IEnumerable<EntityVector>> SearchEntitiesByVectorAsync(
        float[] embedding,
        int topK = 10,
        IEnumerable<Guid>? documentIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert an entity vector (insert or update)
    /// </summary>
    Task<EntityVector> UpsertEntityVectorAsync(
        EntityVector entityVector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all entity vectors
    /// </summary>
    Task<IEnumerable<EntityVector>> GetAllEntityVectorsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete entity vector by entity name
    /// </summary>
    Task DeleteEntityVectorAsync(
        string entityName,
        CancellationToken cancellationToken = default);

    // Relationship Vector Operations

    /// <summary>
    /// Search relationships by vector similarity
    /// </summary>
    Task<IEnumerable<RelationshipVector>> SearchRelationshipsByVectorAsync(
        float[] embedding,
        int topK = 10,
        IEnumerable<Guid>? documentIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert a relationship vector (insert or update)
    /// </summary>
    Task<RelationshipVector> UpsertRelationshipVectorAsync(
        RelationshipVector relationshipVector,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all relationship vectors
    /// </summary>
    Task<IEnumerable<RelationshipVector>> GetAllRelationshipVectorsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete relationship vector by source and target entity
    /// </summary>
    Task DeleteRelationshipVectorAsync(
        string sourceEntity,
        string targetEntity,
        CancellationToken cancellationToken = default);

    // Database Operations

    /// <summary>
    /// Initialize vector tables
    /// </summary>
    Task InitializeVectorTablesAsync(CancellationToken cancellationToken = default);
}
