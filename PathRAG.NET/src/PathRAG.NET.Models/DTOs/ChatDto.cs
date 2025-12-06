namespace PathRAG.NET.Models.DTOs;

public class ChatThreadDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
}

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

