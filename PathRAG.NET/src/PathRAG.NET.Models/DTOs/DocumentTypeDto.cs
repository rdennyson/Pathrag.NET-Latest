namespace PathRAG.NET.Models.DTOs;

public class DocumentTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDocumentTypeId { get; set; }
    public List<DocumentTypeDto> Children { get; set; } = new();
}
