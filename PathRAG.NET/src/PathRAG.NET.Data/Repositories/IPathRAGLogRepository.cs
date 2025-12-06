using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Repositories;

public interface IPathRAGLogRepository
{
    Task<PathRAGLog> CreateLogAsync(PathRAGLog log, CancellationToken cancellationToken = default);
    Task<PathRAGLog?> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PathRAGLog?> GetLogWithStagesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PathRAGLog>> GetLogsAsync(
        Guid? documentId = null,
        Guid? threadId = null,
        string? operationType = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
    Task<int> GetLogCountAsync(
        Guid? documentId = null,
        Guid? threadId = null,
        string? operationType = null,
        string? status = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
    Task UpdateLogAsync(PathRAGLog log, CancellationToken cancellationToken = default);
    
    // Stage logs
    Task AddStageLogAsync(PathRAGStageLog stageLog, CancellationToken cancellationToken = default);
    Task UpdateStageLogAsync(PathRAGStageLog stageLog, CancellationToken cancellationToken = default);
    Task<PathRAGStageLog?> GetStageLogByIdAsync(Guid stageLogId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PathRAGStageLog>> GetStageLogsByLogIdAsync(Guid logId, CancellationToken cancellationToken = default);
    
    // Stages (reference data)
    Task<IEnumerable<PathRAGStage>> GetAllStagesAsync(CancellationToken cancellationToken = default);
    Task<PathRAGStage?> GetStageByCodeAsync(string stageCode, CancellationToken cancellationToken = default);
    Task SeedStagesAsync(CancellationToken cancellationToken = default);
    
    // Performance queries
    Task<IEnumerable<PathRAGStageLog>> GetStagePerformanceDataAsync(
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default);
}

