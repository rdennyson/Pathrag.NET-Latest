namespace PathRAG.NET.Data.Graph;

/// <summary>
/// Configuration settings for graph database provider
/// </summary>
public class GraphSettings
{
    /// <summary>
    /// The graph database provider to use (SqlServer, Neo4j, etc.)
    /// </summary>
    public string Provider { get; set; } = "SqlServer";

    /// <summary>
    /// Connection string for the graph database
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Embedding dimensions for vector columns
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// Database schema name for all tables (default: PathRAG)
    /// </summary>
    public string SchemaName { get; set; } = "PathRAG";
}

