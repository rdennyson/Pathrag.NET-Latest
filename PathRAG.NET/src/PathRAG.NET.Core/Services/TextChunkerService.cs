using Microsoft.ML.Tokenizers;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Text chunking service similar to Python PathRAG's chunking_by_token_size
/// </summary>
public class TextChunkerService : ITextChunkerService
{
    private readonly Tokenizer _tokenizer;
    private readonly PathRAGSettings _settings;

    public TextChunkerService(PathRAGSettings settings)
    {
        _settings = settings;
        _tokenizer = TiktokenTokenizer.CreateForModel("gpt-4o");
    }

    public IEnumerable<TextChunk> ChunkByTokenSize(
        string text,
        string documentId,
        int maxTokensPerChunk = 1200,
        int overlapTokens = 100)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        var tokens = _tokenizer.EncodeToTokens(text, out _);
        var tokenList = tokens.ToList();
        
        if (tokenList.Count == 0)
            yield break;

        int chunkIndex = 0;
        int startIndex = 0;

        while (startIndex < tokenList.Count)
        {
            int endIndex = Math.Min(startIndex + maxTokensPerChunk, tokenList.Count);
            
            // Get the text for this chunk
            var chunkTokens = tokenList.Skip(startIndex).Take(endIndex - startIndex).ToList();
            var chunkText = string.Join("", chunkTokens.Select(t => t.Value));
            
            var chunkId = $"{documentId}-chunk-{chunkIndex}";
            
            yield return new TextChunk
            {
                Id = chunkId,
                Content = chunkText.Trim(),
                Tokens = chunkTokens.Count,
                FullDocId = documentId,
                ChunkOrderIndex = chunkIndex
            };

            chunkIndex++;
            
            // Move start index forward, accounting for overlap
            if (endIndex >= tokenList.Count)
                break;
                
            startIndex = endIndex - overlapTokens;
            if (startIndex < 0) startIndex = 0;
            
            // Prevent infinite loop
            if (startIndex >= endIndex)
                break;
        }
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
            
        return _tokenizer.CountTokens(text);
    }
}

