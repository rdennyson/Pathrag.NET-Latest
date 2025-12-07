namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents an entity node in the knowledge graph (SQL Server Graph Node table)
/// Corresponds to Python PathRAG's entity extraction
/// Note: Embeddings are stored separately in EntityVector table (matching Python PathRAG architecture)
/// </summary>
public class GraphEntity
{
    /// <summary>
    /// Auto-generated $node_id by SQL Server Graph
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The normalized entity name (uppercase)
    /// </summary>
    public required string EntityName { get; set; }

    /// <summary>
    /// Type of entity (organization, person, geo, event, category, etc.)
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Comprehensive description of the entity
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Source chunk IDs where this entity was found (separated by <SEP>)
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Document that produced this entity
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

