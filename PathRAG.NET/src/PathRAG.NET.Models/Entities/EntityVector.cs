namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents an entity vector for semantic search (separate from Graph Node table)
/// Corresponds to Python PathRAG's entities_vdb vector storage
/// Content is: EntityName + Description (for embedding generation)
/// </summary>
public class EntityVector
{
    /// <summary>
    /// Unique identifier (hash of entity name)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// The normalized entity name (uppercase) - links to GraphEntity
    /// </summary>
    public required string EntityName { get; set; }
    
    /// <summary>
    /// Content used for embedding: EntityName + Description
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// Vector embedding for semantic search
    /// </summary>
    public required float[] Embedding { get; set; }
    
    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Document that produced this entity vector
    /// </summary>
    public Guid DocumentId { get; set; }
}

