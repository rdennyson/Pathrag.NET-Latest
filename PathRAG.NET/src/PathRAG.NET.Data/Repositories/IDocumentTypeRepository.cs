using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public interface IDocumentTypeRepository
{
    Task<IEnumerable<DocumentType>> GetAllAsync(CancellationToken cancellationToken = default);
}
