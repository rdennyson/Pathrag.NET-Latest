namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a message in a chat conversation
/// </summary>
public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ThreadId { get; set; }
    public required string Role { get; set; } // user, assistant
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    
    public virtual ChatThread Thread { get; set; } = null!;
}

