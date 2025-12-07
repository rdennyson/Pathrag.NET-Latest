using Microsoft.EntityFrameworkCore;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Models.Entities;
using System.Linq;

namespace PathRAG.NET.Data.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly PathRAGDbContext _context;

    public DocumentRepository(PathRAGDbContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.FindAsync([id], cancellationToken);
    }

    public async Task<Document?> GetByIdWithChunksAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .Include(d => d.Chunks)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .OrderByDescending(d => d.CreationDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        document.CreationDate = DateTimeOffset.UtcNow;
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FindAsync([id], cancellationToken);
        if (document != null)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Documents.AnyAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetIdsByTypeIdsAsync(IEnumerable<Guid> documentTypeIds, CancellationToken cancellationToken = default)
    {
        if (documentTypeIds == null || !documentTypeIds.Any())
            return Enumerable.Empty<Guid>();

        return await _context.Documents
            .Where(d => documentTypeIds.Contains(d.DocumentTypeId))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
    }
}

