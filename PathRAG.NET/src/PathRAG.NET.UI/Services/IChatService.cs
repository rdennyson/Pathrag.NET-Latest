using System.Net.Http.Json;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.UI.Services;

public interface IChatService
{
    Task<IEnumerable<ChatThreadDto>> GetThreadsAsync();
    Task<ChatThreadDto?> GetThreadByIdAsync(Guid id);
    Task<ChatThreadDto> CreateThreadAsync(string? title = null);
    Task<bool> DeleteThreadAsync(Guid id);
    Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid threadId);
    Task<ChatMessageDto> SendMessageAsync(Guid threadId, string message, IEnumerable<Guid>? documentTypeIds = null);
}

public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;

    public ChatService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<ChatThreadDto>> GetThreadsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatThreadDto>>("api/chat/threads");
        return response ?? Enumerable.Empty<ChatThreadDto>();
    }

    public async Task<ChatThreadDto?> GetThreadByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<ChatThreadDto>($"api/chat/threads/{id}");
    }

    public async Task<ChatThreadDto> CreateThreadAsync(string? title = null)
    {
        var response = await _httpClient.PostAsJsonAsync("api/chat/threads", new { Title = title });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatThreadDto>()
            ?? throw new InvalidOperationException("Failed to create thread");
    }

    public async Task<bool> DeleteThreadAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/chat/threads/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<IEnumerable<ChatMessageDto>> GetMessagesAsync(Guid threadId)
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<ChatMessageDto>>($"api/chat/threads/{threadId}/messages");
        return response ?? Enumerable.Empty<ChatMessageDto>();
    }

    public async Task<ChatMessageDto> SendMessageAsync(Guid threadId, string message, IEnumerable<Guid>? documentTypeIds = null)
    {
        var payload = new
        {
            Message = message,
            QueryParams = new QueryParamDto(DocumentTypeIds: documentTypeIds)
        };
        var response = await _httpClient.PostAsJsonAsync($"api/chat/threads/{threadId}/messages", payload);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatMessageDto>()
            ?? throw new InvalidOperationException("Failed to send message");
    }
}
