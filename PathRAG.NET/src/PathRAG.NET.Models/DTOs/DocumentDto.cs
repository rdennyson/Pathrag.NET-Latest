namespace PathRAG.NET.Models.DTOs;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreationDate { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int ChunkCount { get; set; }
}

public record DocumentUploadRequest(
    string FileName,
    string ContentType,
    Stream FileStream
);

public record DocumentUploadResponse(
    Guid DocumentId,
    string Message,
    int TokenCount
);

public class DocumentChunkDto
{
    public Guid Id { get; set; }
    public int Index { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public int IndexOnPage { get; set; }
    public int TokenCount { get; set; }
    public float[]? Embedding { get; set; }
}

