namespace PathRAG.NET.Core.Settings;

/// <summary>
/// PathRAG settings matching Python PathRAG's configuration
/// See Python PathRAG's pathrag.py dataclass for reference
/// </summary>
public class PathRAGSettings
{
    // Chunking settings (matching Python PathRAG)
    public int ChunkTokenSize { get; init; } = 1200;  // Python: chunk_token_size = 1200
    public int ChunkOverlapTokenSize { get; init; } = 100;  // Python: chunk_overlap_token_size = 100
    public string TiktokenModelName { get; init; } = "gpt-4o";  // Python: tiktoken_model_name = "gpt-4o"

    // Entity extraction settings (matching Python PathRAG)
    public int EntityExtractMaxGleaning { get; init; } = 1;  // Python: entity_extract_max_gleaning = 1
    public int EntitySummaryToMaxTokens { get; init; } = 500;  // Python: entity_summary_to_max_tokens = 500

    // Embedding settings (matching Python PathRAG)
    public int EmbeddingBatchNum { get; init; } = 32;  // Python: embedding_batch_num = 32
    public int EmbeddingFuncMaxAsync { get; init; } = 16;  // Python: embedding_func_max_async = 16

    // LLM settings (matching Python PathRAG)
    public int LlmModelMaxTokenSize { get; init; } = 32768;  // Python: llm_model_max_token_size = 32768
    public int LlmModelMaxAsync { get; init; } = 16;  // Python: llm_model_max_async = 16

    // Query settings (matching Python PathRAG's QueryParam)
    public int TopK { get; init; } = 40;  // Python: top_k = 40
    public int MaxTokenForTextUnit { get; init; } = 4000;  // Python: max_token_for_text_unit = 4000
    public int MaxTokenForGlobalContext { get; init; } = 3000;  // Python: max_token_for_global_context = 3000
    public int MaxTokenForLocalContext { get; init; } = 5000;  // Python: max_token_for_local_context = 5000

    // Vector search settings
    public double CosineBetterThanThreshold { get; init; } = 0.2;  // Python: cosine_better_than_threshold = 0.2

    // Legacy settings (for backward compatibility)
    public int EmbeddingBatchSize { get => EmbeddingBatchNum; init => EmbeddingBatchNum = value; }
    public int MaxTokensPerLine { get; init; } = 300;
    public int MaxTokensPerParagraph { get => ChunkTokenSize; init => ChunkTokenSize = value; }
    public int OverlapTokens { get => ChunkOverlapTokenSize; init => ChunkOverlapTokenSize = value; }
    public int MaxRelevantChunks { get; init; } = 50;
    public int MaxInputTokens { get => LlmModelMaxTokenSize; init => LlmModelMaxTokenSize = value; }
    public int MaxOutputTokens { get; init; } = 800;
    public TimeSpan MessageExpiration { get; init; } = TimeSpan.FromMinutes(5);
    public int MessageLimit { get; set; } = 20;
}

public class AzureOpenAISettings
{
    public OpenAIServiceSettings ChatCompletion { get; init; } = new();
    public OpenAIServiceSettings Embedding { get; init; } = new();
}

public class OpenAIServiceSettings
{
    public string Endpoint { get; init; } = string.Empty;
    public string Deployment { get; init; } = string.Empty;
    public string ModelId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public int? Dimensions { get; init; }
}
