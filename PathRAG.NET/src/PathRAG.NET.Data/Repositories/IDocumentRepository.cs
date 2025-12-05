using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Document?> GetByIdWithChunksAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
    Task<Document> UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IDocumentChunkRepository
{
    Task<DocumentChunk?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentChunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<DocumentChunk>> AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    
    // Vector search
    Task<IEnumerable<DocumentChunk>> SearchByVectorAsync(float[] embedding, int topK = 10, CancellationToken cancellationToken = default);
}

public interface IChatRepository
{
    // Thread operations
    Task<ChatThread?> GetThreadByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChatThread?> GetThreadWithMessagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatThread>> GetAllThreadsAsync(CancellationToken cancellationToken = default);
    Task<ChatThread> CreateThreadAsync(ChatThread thread, CancellationToken cancellationToken = default);
    Task<ChatThread> UpdateThreadAsync(ChatThread thread, CancellationToken cancellationToken = default);
    Task DeleteThreadAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Message operations
    Task<ChatMessage?> GetMessageByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatMessage>> GetMessagesByThreadIdAsync(Guid threadId, int? limit = null, CancellationToken cancellationToken = default);
    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);
}

