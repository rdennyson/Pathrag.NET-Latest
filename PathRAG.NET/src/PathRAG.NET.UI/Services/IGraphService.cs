using System.Net;
using System.Net.Http.Json;
using System.Linq;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.UI.Services;

public interface IGraphService
{
    Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int limit = 100, IEnumerable<Guid>? documentTypeIds = null);
    Task<GraphStatsDto> GetGraphStatsAsync(IEnumerable<Guid>? documentTypeIds = null);
    Task<KnowledgeGraphDto> QueryGraphAsync(string query, int topK = 40, IEnumerable<Guid>? documentTypeIds = null);
}

public class GraphService : IGraphService
{
    private readonly HttpClient _httpClient;

    public GraphService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int limit = 100, IEnumerable<Guid>? documentTypeIds = null)
    {
        var parameters = new List<string> { $"limit={limit}" };
        AppendDocumentTypeFilters(parameters, documentTypeIds);
        var query = BuildQueryString(parameters);
        var response = await _httpClient.GetFromJsonAsync<KnowledgeGraphDto>($"api/graph{query}");
        return response ?? new KnowledgeGraphDto();
    }

    public async Task<GraphStatsDto> GetGraphStatsAsync(IEnumerable<Guid>? documentTypeIds = null)
    {
        var parameters = new List<string>();
        AppendDocumentTypeFilters(parameters, documentTypeIds);
        var query = BuildQueryString(parameters);
        var response = await _httpClient.GetFromJsonAsync<GraphStatsDto>($"api/graph/stats{query}");
        return response ?? new GraphStatsDto();
    }

    public async Task<KnowledgeGraphDto> QueryGraphAsync(string query, int topK = 40, IEnumerable<Guid>? documentTypeIds = null)
    {
        var payload = new
        {
            Query = query,
            TopK = topK,
            DocumentTypeIds = documentTypeIds?.ToArray()
        };
        var response = await _httpClient.PostAsJsonAsync("api/graph/query", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KnowledgeGraphDto>()
            ?? new KnowledgeGraphDto();
    }

    private static string BuildQueryString(List<string> parameters)
    {
        return parameters.Any() ? $"?{string.Join("&", parameters)}" : string.Empty;
    }

    private static void AppendDocumentTypeFilters(List<string> parameters, IEnumerable<Guid>? documentTypeIds)
    {
        if (documentTypeIds == null) return;
        foreach (var id in documentTypeIds)
        {
            parameters.Add($"documentTypeIds={Uri.EscapeDataString(id.ToString())}");
        }
    }
}
