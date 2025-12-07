namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a relationship edge in the knowledge graph (SQL Server Graph Edge table)
/// Corresponds to Python PathRAG's relationship extraction
/// Note: Embeddings are stored separately in RelationshipVector table (matching Python PathRAG architecture)
/// </summary>
public class GraphRelationship
{
    /// <summary>
    /// Auto-generated $edge_id by SQL Server Graph
    /// </summary>
    public string? EdgeId { get; set; }

    /// <summary>
    /// Unique identifier for the relationship
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Source entity name (from node)
    /// </summary>
    public required string SourceEntityName { get; set; }

    /// <summary>
    /// Target entity name (to node)
    /// </summary>
    public required string TargetEntityName { get; set; }

    /// <summary>
    /// Relationship strength/weight score
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Description of why the entities are related
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// High-level keywords summarizing the relationship
    /// </summary>
    public string? Keywords { get; set; }

    /// <summary>
    /// Source chunk IDs where this relationship was found (separated by <SEP>)
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Document that produced this relationship
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

