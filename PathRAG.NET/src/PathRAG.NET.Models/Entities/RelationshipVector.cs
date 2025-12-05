namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a relationship vector for semantic search (separate from Graph Edge table)
/// Corresponds to Python PathRAG's relationships_vdb vector storage
/// Content is: Keywords + SourceEntityName + TargetEntityName + Description (for embedding generation)
/// </summary>
public class RelationshipVector
{
    /// <summary>
    /// Unique identifier (hash of source + target)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Source entity name - links to GraphRelationship
    /// </summary>
    public required string SourceEntityName { get; set; }
    
    /// <summary>
    /// Target entity name - links to GraphRelationship
    /// </summary>
    public required string TargetEntityName { get; set; }
    
    /// <summary>
    /// Content used for embedding: Keywords + SourceEntity + TargetEntity + Description
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
}

