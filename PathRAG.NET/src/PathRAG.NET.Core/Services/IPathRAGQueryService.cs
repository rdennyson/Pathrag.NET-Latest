using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Services;

public interface IPathRAGQueryService
{
    /// <summary>
    /// Execute a PathRAG query with graph traversal
    /// </summary>
    Task<QueryContextDto> BuildQueryContextAsync(
        string query,
        QueryParamDto? queryParams = null,
        Guid? logId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get knowledge graph visualization data for a query
    /// </summary>
    Task<KnowledgeGraphDto> GetQueryGraphAsync(
        string query,
        int topK = 40,
        IEnumerable<Guid>? documentTypeIds = null,
        Guid? logId = null,
        CancellationToken cancellationToken = default);
}
