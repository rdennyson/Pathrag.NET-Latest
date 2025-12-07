namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a document that has been uploaded and processed
/// </summary>
public class Document
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public Guid DocumentTypeId { get; set; }
    public virtual DocumentType DocumentType { get; set; } = null!;
    public string Status { get; set; } = "pending"; // pending, processing, completed, failed
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    
    public virtual ICollection<DocumentChunk> Chunks { get; set; } = [];
}

