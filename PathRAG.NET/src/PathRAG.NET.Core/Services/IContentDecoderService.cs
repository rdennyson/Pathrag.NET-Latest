namespace PathRAG.NET.Core.Services;

public interface IContentDecoderService
{
    Task<string> DecodeAsync(Stream content, string contentType, CancellationToken cancellationToken = default);
    bool CanDecode(string contentType);
}

