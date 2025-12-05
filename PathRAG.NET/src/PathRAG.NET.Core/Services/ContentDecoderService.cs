using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace PathRAG.NET.Core.Services;

public class ContentDecoderService : IContentDecoderService
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
        "text/markdown",
        "text/html"
    };

    public bool CanDecode(string contentType) => SupportedTypes.Contains(contentType);

    public async Task<string> DecodeAsync(Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => await DecodePdfAsync(content, cancellationToken),
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => await DecodeDocxAsync(content, cancellationToken),
            "text/plain" or "text/markdown" => await DecodeTextAsync(content, cancellationToken),
            "text/html" => await DecodeHtmlAsync(content, cancellationToken),
            _ => throw new NotSupportedException($"Content type '{contentType}' is not supported")
        };
    }

    private static async Task<string> DecodePdfAsync(Stream content, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var document = PdfDocument.Open(memoryStream);
        var textBuilder = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            var pageText = ContentOrderTextExtractor.GetText(page);
            textBuilder.AppendLine(pageText);
        }

        return textBuilder.ToString();
    }

    private static async Task<string> DecodeDocxAsync(Stream content, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        using var document = WordprocessingDocument.Open(memoryStream, false);
        var body = document.MainDocumentPart?.Document.Body;
        
        if (body == null)
            return string.Empty;

        var textBuilder = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            textBuilder.AppendLine(paragraph.InnerText);
        }

        return textBuilder.ToString();
    }

    private static async Task<string> DecodeTextAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string> DecodeHtmlAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content);
        var html = await reader.ReadToEndAsync(cancellationToken);
        
        // Simple HTML tag removal - could use HtmlAgilityPack for better parsing
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}

