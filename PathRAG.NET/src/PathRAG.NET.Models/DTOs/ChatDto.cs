namespace PathRAG.NET.Models.DTOs;

public record ChatThreadDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastMessageAt,
    int MessageCount
);

public record ChatMessageDto(
    Guid Id,
    Guid ThreadId,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    int? InputTokens,
    int? OutputTokens
);

public record ChatRequest(
    string Query,
    Guid? ThreadId = null
);

public record ChatResponse(
    Guid ThreadId,
    string Query,
    string Response,
    TokenUsageDto? TokenUsage,
    IEnumerable<CitationDto>? Citations
);

public record TokenUsageDto(
    int? InputTokens,
    int? OutputTokens,
    int? EmbeddingTokens
);

public record CitationDto(
    Guid DocumentId,
    Guid ChunkId,
    string FileName,
    int? PageNumber,
    int IndexOnPage,
    string Quote
);

public record CreateThreadRequest(
    string Title
);

