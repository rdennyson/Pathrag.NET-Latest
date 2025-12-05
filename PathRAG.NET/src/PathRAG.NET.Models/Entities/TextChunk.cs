namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a text chunk stored in the KV storage
/// This is the data structure used for PathRAG text chunk storage
/// </summary>
public class TextChunk
{
    public required string Id { get; set; }
    public int Tokens { get; set; }
    public required string Content { get; set; }
    public required string FullDocId { get; set; }
    public int ChunkOrderIndex { get; set; }
}

