using System.Collections.Generic;

namespace PathRAG.NET.Models.DTOs;

/// <summary>
/// Query parameters for PathRAG queries (similar to Python QueryParam)
/// </summary>
public record QueryParamDto(
    string Mode = "hybrid",
    bool OnlyNeedContext = false,
    bool OnlyNeedPrompt = false,
    string ResponseType = "Multiple Paragraphs",
    bool Stream = false,
    int TopK = 40,
    int MaxTokenForTextUnit = 4000,
    int MaxTokenForGlobalContext = 3000,
    int MaxTokenForLocalContext = 5000,
    IEnumerable<Guid>? DocumentTypeIds = null
);

public record KeywordsExtractionResult(
    IEnumerable<string> HighLevelKeywords,
    IEnumerable<string> LowLevelKeywords
);

public record QueryContextDto(
    string HighLevelEntitiesContext,
    string HighLevelRelationsContext,
    string LowLevelEntitiesContext,
    string LowLevelRelationsContext,
    string TextUnitsContext
);
