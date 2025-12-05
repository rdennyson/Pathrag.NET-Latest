using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Services;

public interface IKeywordsExtractionService
{
    Task<KeywordsExtractionResult> ExtractKeywordsAsync(
        string query,
        CancellationToken cancellationToken = default);
}

