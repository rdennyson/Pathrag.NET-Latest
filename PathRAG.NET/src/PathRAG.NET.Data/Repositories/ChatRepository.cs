using Microsoft.EntityFrameworkCore;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly PathRAGDbContext _context;

    public ChatRepository(PathRAGDbContext context)
    {
        _context = context;
    }

    public async Task<ChatThread?> GetThreadByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChatThreads.FindAsync([id], cancellationToken);
    }

    public async Task<ChatThread?> GetThreadWithMessagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChatThreads
            .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ChatThread>> GetAllThreadsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ChatThreads
            .OrderByDescending(t => t.LastMessageAt ?? t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatThread> CreateThreadAsync(ChatThread thread, CancellationToken cancellationToken = default)
    {
        thread.CreatedAt = DateTimeOffset.UtcNow;
        _context.ChatThreads.Add(thread);
        await _context.SaveChangesAsync(cancellationToken);
        return thread;
    }

    public async Task<ChatThread> UpdateThreadAsync(ChatThread thread, CancellationToken cancellationToken = default)
    {
        _context.ChatThreads.Update(thread);
        await _context.SaveChangesAsync(cancellationToken);
        return thread;
    }

    public async Task DeleteThreadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var thread = await _context.ChatThreads.FindAsync([id], cancellationToken);
        if (thread != null)
        {
            _context.ChatThreads.Remove(thread);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ChatMessage?> GetMessageByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ChatMessages.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesByThreadIdAsync(Guid threadId, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ChatMessages
            .Where(m => m.ThreadId == threadId)
            .OrderBy(m => m.CreatedAt);

        if (limit.HasValue)
        {
            return await query.TakeLast(limit.Value).ToListAsync(cancellationToken);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        message.CreatedAt = DateTimeOffset.UtcNow;
        _context.ChatMessages.Add(message);
        
        // Update thread's last message time
        var thread = await _context.ChatThreads.FindAsync([message.ThreadId], cancellationToken);
        if (thread != null)
        {
            thread.LastMessageAt = message.CreatedAt;
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }
}

