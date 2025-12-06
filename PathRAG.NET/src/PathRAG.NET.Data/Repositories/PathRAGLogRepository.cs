using Microsoft.EntityFrameworkCore;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public class PathRAGLogRepository : IPathRAGLogRepository
{
    private readonly PathRAGDbContext _context;

    public PathRAGLogRepository(PathRAGDbContext context)
    {
        _context = context;
    }

    public async Task<PathRAGLog> CreateLogAsync(PathRAGLog log, CancellationToken cancellationToken = default)
    {
        _context.PathRAGLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
        return log;
    }

    public async Task<PathRAGLog?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PathRAGLogs.FindAsync([id], cancellationToken);
    }

    public async Task<PathRAGLog?> GetLogWithStagesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.PathRAGLogs
            .Include(l => l.StageLogs)
            .ThenInclude(s => s.Stage)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<PathRAGLog>> GetLogsAsync(
        Guid? documentId = null,
        Guid? threadId = null,
        string? operationType = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PathRAGLogs.AsQueryable();

        if (documentId.HasValue)
            query = query.Where(l => l.DocumentId == documentId);
        if (threadId.HasValue)
            query = query.Where(l => l.ThreadId == threadId);
        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(l => l.OperationType == operationType);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);
        if (fromDate.HasValue)
            query = query.Where(l => l.StartedAt >= fromDate);
        if (toDate.HasValue)
            query = query.Where(l => l.StartedAt <= toDate);

        return await query
            .OrderByDescending(l => l.StartedAt)
            .Skip(skip)
            .Take(take)
            .Include(l => l.StageLogs)
            .ThenInclude(s => s.Stage)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetLogCountAsync(
        Guid? documentId = null,
        Guid? threadId = null,
        string? operationType = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PathRAGLogs.AsQueryable();

        if (documentId.HasValue)
            query = query.Where(l => l.DocumentId == documentId);
        if (threadId.HasValue)
            query = query.Where(l => l.ThreadId == threadId);
        if (!string.IsNullOrEmpty(operationType))
            query = query.Where(l => l.OperationType == operationType);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(l => l.Status == status);
        if (fromDate.HasValue)
            query = query.Where(l => l.StartedAt >= fromDate);
        if (toDate.HasValue)
            query = query.Where(l => l.StartedAt <= toDate);

        return await query.CountAsync(cancellationToken);
    }

    public async Task UpdateLogAsync(PathRAGLog log, CancellationToken cancellationToken = default)
    {
        _context.PathRAGLogs.Update(log);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStageLogAsync(PathRAGStageLog stageLog, CancellationToken cancellationToken = default)
    {
        _context.PathRAGStageLogs.Add(stageLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStageLogAsync(PathRAGStageLog stageLog, CancellationToken cancellationToken = default)
    {
        _context.PathRAGStageLogs.Update(stageLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PathRAGStageLog?> GetStageLogByIdAsync(Guid stageLogId, CancellationToken cancellationToken = default)
    {
        return await _context.PathRAGStageLogs
            .Include(s => s.Stage)
            .FirstOrDefaultAsync(s => s.Id == stageLogId, cancellationToken);
    }

    public async Task<IEnumerable<PathRAGStageLog>> GetStageLogsByLogIdAsync(Guid logId, CancellationToken ct = default)
    {
        return await _context.PathRAGStageLogs
            .Where(s => s.LogId == logId)
            .Include(s => s.Stage)
            .OrderBy(s => s.Stage!.StageOrder)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<PathRAGStage>> GetAllStagesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PathRAGStages.OrderBy(s => s.StageOrder).ToListAsync(cancellationToken);
    }

    public async Task<PathRAGStage?> GetStageByCodeAsync(string stageCode, CancellationToken cancellationToken = default)
    {
        return await _context.PathRAGStages.FirstOrDefaultAsync(s => s.StageCode == stageCode, cancellationToken);
    }

    public async Task SeedStagesAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.PathRAGStages.AnyAsync(cancellationToken))
            return;

        var stages = new List<PathRAGStage>
        {
            // Document Upload Stages
            new() { Id = 1, StageCode = "DOC_CREATE", StageName = "Create Document Record", OperationType = "DocumentUpload", StageOrder = 1, Description = "Initialize document record in database" },
            new() { Id = 2, StageCode = "DOC_DECODE", StageName = "Decode Content", OperationType = "DocumentUpload", StageOrder = 2, Description = "Extract text from PDF/DOCX/TXT files" },
            new() { Id = 3, StageCode = "DOC_CHUNK", StageName = "Text Chunking", OperationType = "DocumentUpload", StageOrder = 3, Description = "Split text into token-sized chunks" },
            new() { Id = 4, StageCode = "DOC_EMBED_CHUNKS", StageName = "Generate Chunk Embeddings", OperationType = "DocumentUpload", StageOrder = 4, Description = "Generate embeddings for document chunks" },
            new() { Id = 5, StageCode = "DOC_EXTRACT_ENTITIES", StageName = "Extract Entities & Relationships", OperationType = "DocumentUpload", StageOrder = 5, Description = "LLM extraction of entities and relationships from chunks" },
            new() { Id = 6, StageCode = "DOC_MERGE_ENTITIES", StageName = "Merge Entities", OperationType = "DocumentUpload", StageOrder = 6, Description = "Merge duplicate entities with LLM" },
            new() { Id = 7, StageCode = "DOC_MERGE_RELS", StageName = "Merge Relationships", OperationType = "DocumentUpload", StageOrder = 7, Description = "Merge duplicate relationships with LLM" },
            new() { Id = 8, StageCode = "DOC_EMBED_ENTITIES", StageName = "Generate Entity Embeddings", OperationType = "DocumentUpload", StageOrder = 8, Description = "Store entity vectors for semantic search" },
            new() { Id = 9, StageCode = "DOC_EMBED_RELS", StageName = "Generate Relationship Embeddings", OperationType = "DocumentUpload", StageOrder = 9, Description = "Store relationship vectors for semantic search" },
            new() { Id = 10, StageCode = "DOC_COMPLETE", StageName = "Complete Processing", OperationType = "DocumentUpload", StageOrder = 10, Description = "Mark document as completed" },

            // Query Stages
            new() { Id = 20, StageCode = "QUERY_START", StageName = "Initialize Query", OperationType = "Query", StageOrder = 1, Description = "Start query processing" },
            new() { Id = 21, StageCode = "QUERY_KEYWORDS", StageName = "Extract Keywords", OperationType = "Query", StageOrder = 2, Description = "Extract high-level and low-level keywords from query" },
            new() { Id = 22, StageCode = "QUERY_EMBED", StageName = "Generate Query Embedding", OperationType = "Query", StageOrder = 3, Description = "Generate embedding for semantic search" },
            new() { Id = 23, StageCode = "QUERY_SEARCH_ENTITIES", StageName = "Search Entities", OperationType = "Query", StageOrder = 4, Description = "Vector search for relevant entities" },
            new() { Id = 24, StageCode = "QUERY_SEARCH_RELS", StageName = "Search Relationships", OperationType = "Query", StageOrder = 5, Description = "Vector search for relevant relationships" },
            new() { Id = 25, StageCode = "QUERY_BUILD_CONTEXT", StageName = "Build Query Context", OperationType = "Query", StageOrder = 6, Description = "Combine entities, relationships, and text units" },
            new() { Id = 26, StageCode = "QUERY_PATH_FINDING", StageName = "Path Finding", OperationType = "Query", StageOrder = 7, Description = "Find paths between entities (1-3 hops)" },
            new() { Id = 27, StageCode = "QUERY_LLM_RESPONSE", StageName = "Generate LLM Response", OperationType = "Query", StageOrder = 8, Description = "Generate final response with LLM" },
            new() { Id = 28, StageCode = "QUERY_COMPLETE", StageName = "Complete Query", OperationType = "Query", StageOrder = 9, Description = "Return response to user" },

            // Graph Query Stages
            new() { Id = 30, StageCode = "GRAPH_START", StageName = "Initialize Graph Query", OperationType = "GraphQuery", StageOrder = 1, Description = "Start graph visualization query" },
            new() { Id = 31, StageCode = "GRAPH_EMBED", StageName = "Generate Query Embedding", OperationType = "GraphQuery", StageOrder = 2, Description = "Generate embedding for graph search" },
            new() { Id = 32, StageCode = "GRAPH_SEARCH", StageName = "Search Graph Nodes", OperationType = "GraphQuery", StageOrder = 3, Description = "Search for relevant graph nodes and edges" },
            new() { Id = 33, StageCode = "GRAPH_BUILD", StageName = "Build Graph Response", OperationType = "GraphQuery", StageOrder = 4, Description = "Construct graph visualization data" },
            new() { Id = 34, StageCode = "GRAPH_COMPLETE", StageName = "Complete Graph Query", OperationType = "GraphQuery", StageOrder = 5, Description = "Return graph data" }
        };

        _context.PathRAGStages.AddRange(stages);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<PathRAGStageLog>> GetStagePerformanceDataAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PathRAGStageLogs
            .Include(s => s.Stage)
            .Where(s => s.Status == "Completed" && s.DurationMs.HasValue);

        if (fromDate.HasValue)
            query = query.Where(s => s.StartedAt >= fromDate);
        if (toDate.HasValue)
            query = query.Where(s => s.StartedAt <= toDate);

        return await query.ToListAsync(cancellationToken);
    }
}
