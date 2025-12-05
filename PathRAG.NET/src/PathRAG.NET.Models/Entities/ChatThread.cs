namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a chat conversation thread
/// </summary>
public class ChatThread
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastMessageAt { get; set; }
    
    public virtual ICollection<ChatMessage> Messages { get; set; } = [];
}

