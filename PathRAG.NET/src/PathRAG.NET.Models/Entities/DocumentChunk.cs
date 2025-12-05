namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a chunk of text extracted from a document
/// </summary>
public class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int Index { get; set; }
    public int? PageNumber { get; set; }
    public int IndexOnPage { get; set; }
    public int TokenCount { get; set; }
    public required string Content { get; set; }
    public required float[] Embedding { get; set; }
    
    public virtual Document Document { get; set; } = null!;
}

