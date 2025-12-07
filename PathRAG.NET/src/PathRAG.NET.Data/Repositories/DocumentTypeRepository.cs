using Microsoft.EntityFrameworkCore;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public class DocumentTypeRepository : IDocumentTypeRepository
{
    private readonly PathRAGDbContext _context;

    public DocumentTypeRepository(PathRAGDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DocumentType>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var documentTypes = await _context.DocumentTypes
            .OrderBy(dt => dt.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return documentTypes;
    }
}
