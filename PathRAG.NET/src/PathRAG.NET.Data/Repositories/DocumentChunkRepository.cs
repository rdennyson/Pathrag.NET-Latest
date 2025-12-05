using Microsoft.EntityFrameworkCore;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly PathRAGDbContext _context;

    public DocumentChunkRepository(PathRAGDbContext context)
    {
        _context = context;
    }

    public async Task<DocumentChunk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentChunks.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.Index)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DocumentChunk>> AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        var chunkList = chunks.ToList();
        _context.DocumentChunks.AddRange(chunkList);
        await _context.SaveChangesAsync(cancellationToken);
        return chunkList;
    }

    public async Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var chunks = await _context.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .ToListAsync(cancellationToken);
        
        _context.DocumentChunks.RemoveRange(chunks);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<DocumentChunk>> SearchByVectorAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default)
    {
        // Use EF Core Vector Search extension
        return await _context.DocumentChunks
            .OrderBy(c => EF.Functions.VectorDistance("cosine", c.Embedding, embedding))
            .Take(topK)
            .Include(c => c.Document)
            .ToListAsync(cancellationToken);
    }
}

