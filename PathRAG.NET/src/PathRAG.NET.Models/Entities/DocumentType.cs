using System.Collections.Generic;

namespace PathRAG.NET.Models.Entities;

public class DocumentType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentDocumentTypeId { get; set; }

    public virtual DocumentType? ParentDocumentType { get; set; }
    public virtual ICollection<DocumentType> Children { get; set; } = new List<DocumentType>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
}
