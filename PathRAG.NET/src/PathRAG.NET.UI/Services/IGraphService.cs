using System.Net.Http.Json;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.UI.Services;

public interface IGraphService
{
    Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int limit = 100);
    Task<GraphStatsDto> GetGraphStatsAsync();
    Task<KnowledgeGraphDto> QueryGraphAsync(string query, int topK = 40);
}

public class GraphService : IGraphService
{
    private readonly HttpClient _httpClient;

    public GraphService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<KnowledgeGraphDto> GetKnowledgeGraphAsync(int limit = 100)
    {
        var response = await _httpClient.GetFromJsonAsync<KnowledgeGraphDto>($"api/graph?limit={limit}");
        return response ?? new KnowledgeGraphDto();
    }

    public async Task<GraphStatsDto> GetGraphStatsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<GraphStatsDto>("api/graph/stats");
        return response ?? new GraphStatsDto();
    }

    public async Task<KnowledgeGraphDto> QueryGraphAsync(string query, int topK = 40)
    {
        var response = await _httpClient.PostAsJsonAsync("api/graph/query", new { Query = query, TopK = topK });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<KnowledgeGraphDto>()
            ?? new KnowledgeGraphDto();
    }
}

