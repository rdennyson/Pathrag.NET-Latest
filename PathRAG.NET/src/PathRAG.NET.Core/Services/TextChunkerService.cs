using System;
using System.Linq;
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
        _tokenizer = CreateTokenizer(settings.TiktokenModelName);
    }

    public IEnumerable<TextChunk> ChunkByTokenSize(
        string text,
        string documentId,
        int maxTokensPerChunk = 1200,
        int overlapTokens = 100)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        if (maxTokensPerChunk <= 0)
            yield break;

        overlapTokens = Math.Max(0, overlapTokens);
        if (overlapTokens >= maxTokensPerChunk)
        {
            throw new ArgumentException("overlapTokens must be less than maxTokensPerChunk", nameof(overlapTokens));
        }

        var encodedTokens = _tokenizer.EncodeToTokens(text, out _).ToArray();
        if (encodedTokens.Length == 0)
            yield break;

        var tokenIds = encodedTokens.Select(t => t.Id).ToArray();
        var stride = Math.Max(1, maxTokensPerChunk - overlapTokens);
        var chunkIndex = 0;

        for (var start = 0; start < tokenIds.Length; start += stride)
        {
            var length = Math.Min(maxTokensPerChunk, tokenIds.Length - start);
            var chunkIds = tokenIds.AsSpan(start, length).ToArray();
            var chunkText = (_tokenizer.Decode(chunkIds) ?? string.Empty).Trim();

            yield return new TextChunk
            {
                Id = $"{documentId}-chunk-{chunkIndex}",
                Content = chunkText,
                Tokens = chunkIds.Length,
                FullDocId = documentId,
                ChunkOrderIndex = chunkIndex
            };

            chunkIndex++;
        }
    }

    public int CountTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return _tokenizer.EncodeToTokens(text, out _).Count;
    }

    private static Tokenizer CreateTokenizer(string? modelName)
    {
        var fallbackModel = "gpt-4o";
        var requestedModel = string.IsNullOrWhiteSpace(modelName) ? fallbackModel : modelName;

        try
        {
            return TiktokenTokenizer.CreateForModel(requestedModel);
        }
        catch
        {
            // Fallback to GPT-4o tokenizer if the configured model is unavailable locally
            return TiktokenTokenizer.CreateForModel(fallbackModel);
        }
    }
}
