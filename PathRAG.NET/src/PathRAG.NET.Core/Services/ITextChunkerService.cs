using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

public interface ITextChunkerService
{
    IEnumerable<TextChunk> ChunkByTokenSize(
        string text,
        string documentId,
        int maxTokensPerChunk = 1200,
        int overlapTokens = 100);
    
    int CountTokens(string text);
}

public record ChunkResult(
    string ChunkId,
    string Content,
    int TokenCount,
    int ChunkIndex
);

