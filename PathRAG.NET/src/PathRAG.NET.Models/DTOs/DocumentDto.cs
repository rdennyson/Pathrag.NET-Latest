namespace PathRAG.NET.Models.DTOs;

public record DocumentDto(
    Guid Id,
    string Name,
    string? ContentType,
    long FileSize,
    string Status,
    string? ErrorMessage,
    DateTimeOffset CreationDate,
    DateTimeOffset? ProcessedAt,
    int ChunkCount
);

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

public record DocumentChunkDto(
    Guid Id,
    int Index,
    string Content,
    int? PageNumber,
    int IndexOnPage,
    int TokenCount,
    float[]? Embedding
);

