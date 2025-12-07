using System.Net.Http.Json;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.UI.Services;

public interface IDocumentTypeService
{
    Task<IEnumerable<DocumentTypeDto>> GetDocumentTypesAsync();
}

public class DocumentTypeService : IDocumentTypeService
{
    private readonly HttpClient _httpClient;

    public DocumentTypeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IEnumerable<DocumentTypeDto>> GetDocumentTypesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<IEnumerable<DocumentTypeDto>>("api/documenttypes");
        return response ?? Enumerable.Empty<DocumentTypeDto>();
    }
}
