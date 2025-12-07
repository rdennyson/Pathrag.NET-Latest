using System.Net.Http.Json;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.UI.Services;

public interface IDocumentService
{
    Task<IEnumerable<DocumentDto>> GetDocumentsAsync();
    Task<DocumentDto?> GetDocumentByIdAsync(Guid id);
    Task<DocumentUploadResponse> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, Guid documentTypeId);
    Task<bool> DeleteDocumentAsync(Guid id);
}

public class DocumentService : IDocumentService
{
    private readonly HttpClient _httpClient;

    public DocumentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<DocumentDto>> GetDocumentsAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<DocumentDto>>("api/documents");
        return response ?? Enumerable.Empty<DocumentDto>();
    }

    public async Task<DocumentDto?> GetDocumentByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<DocumentDto>($"api/documents/{id}");
    }

    public async Task<DocumentUploadResponse> UploadDocumentAsync(Stream fileStream, string fileName, string contentType, Guid documentTypeId)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(documentTypeId.ToString()), "documentTypeId");

        var response = await _httpClient.PostAsync("api/documents/upload", content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DocumentUploadResponse>() 
            ?? throw new InvalidOperationException("Failed to parse upload response");
    }

    public async Task<bool> DeleteDocumentAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/documents/{id}");
        return response.IsSuccessStatusCode;
    }
}
