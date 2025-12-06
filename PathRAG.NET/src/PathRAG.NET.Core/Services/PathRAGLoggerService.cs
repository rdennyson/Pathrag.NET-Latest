using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

public class PathRAGLoggerService : IPathRAGLoggerService
{
    private readonly IPathRAGLogRepository _logRepository;
    private readonly ILogger<PathRAGLoggerService> _logger;
    private readonly Dictionary<Guid, Stopwatch> _stageStopwatches = new();
    private readonly Dictionary<Guid, Stopwatch> _operationStopwatches = new();

    public PathRAGLoggerService(
        IPathRAGLogRepository logRepository,
        ILogger<PathRAGLoggerService> logger)
    {
        _logRepository = logRepository;
        _logger = logger;
    }

    public async Task<Guid> StartOperationAsync(
        string operationType,
        Guid? documentId = null,
        Guid? threadId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var log = new PathRAGLog
        {
            Id = Guid.NewGuid(),
            OperationType = operationType,
            DocumentId = documentId,
            ThreadId = threadId,
            Status = "Started",
            StartedAt = DateTimeOffset.UtcNow,
            Metadata = metadata,
            TotalStages = GetTotalStagesForOperation(operationType),
            CompletedStages = 0
        };

        await _logRepository.CreateLogAsync(log, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        _operationStopwatches[log.Id] = stopwatch;

        _logger.LogInformation("Started {OperationType} operation {LogId}", operationType, log.Id);
        return log.Id;
    }

    public async Task<Guid> StartStageAsync(
        Guid logId,
        string stageCode,
        Guid? documentId = null,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var stage = await _logRepository.GetStageByCodeAsync(stageCode, cancellationToken);
        if (stage == null)
        {
            _logger.LogWarning("Unknown stage code: {StageCode}", stageCode);
            return Guid.Empty;
        }

        var stageLog = new PathRAGStageLog
        {
            Id = Guid.NewGuid(),
            LogId = logId,
            StageId = stage.Id,
            DocumentId = documentId,
            Status = "Started",
            StartedAt = DateTimeOffset.UtcNow,
            LogLevel = "Information",
            Message = message ?? $"Started {stage.StageName}"
        };

        await _logRepository.AddStageLogAsync(stageLog, cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        _stageStopwatches[stageLog.Id] = stopwatch;

        _logger.LogInformation("[{StageCode}] {Message}", stageCode, stageLog.Message);
        return stageLog.Id;
    }

    public async Task CompleteStageAsync(
        Guid stageLogId,
        int? itemsProcessed = null,
        int? tokensUsed = null,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        if (stageLogId == Guid.Empty) return;

        // Use correct method to get stage log by its own ID, not parent log ID
        var stageLog = await _logRepository.GetStageLogByIdAsync(stageLogId, cancellationToken);
        if (stageLog == null) return;

        stageLog.Status = "Completed";
        stageLog.CompletedAt = DateTimeOffset.UtcNow;
        stageLog.ItemsProcessed = itemsProcessed;
        stageLog.TokensUsed = tokensUsed;
        stageLog.Details = details;

        if (_stageStopwatches.TryGetValue(stageLogId, out var sw))
        {
            sw.Stop();
            stageLog.DurationMs = sw.ElapsedMilliseconds;
            _stageStopwatches.Remove(stageLogId);
        }

        await _logRepository.UpdateStageLogAsync(stageLog, cancellationToken);

        // Update parent log completed stages count
        var log = await _logRepository.GetLogByIdAsync(stageLog.LogId, cancellationToken);
        if (log != null)
        {
            log.CompletedStages++;
            await _logRepository.UpdateLogAsync(log, cancellationToken);
        }

        _logger.LogInformation("[{StageCode}] Completed in {DurationMs}ms",
            stageLog.Stage?.StageCode ?? "UNKNOWN", stageLog.DurationMs);
    }

    public async Task FailStageAsync(
        Guid stageLogId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (stageLogId == Guid.Empty) return;

        // Use correct method to get stage log by its own ID, not parent log ID
        var stageLog = await _logRepository.GetStageLogByIdAsync(stageLogId, cancellationToken);
        if (stageLog == null) return;

        stageLog.Status = "Failed";
        stageLog.CompletedAt = DateTimeOffset.UtcNow;
        stageLog.ErrorMessage = errorMessage;
        stageLog.LogLevel = "Error";

        if (_stageStopwatches.TryGetValue(stageLogId, out var sw))
        {
            sw.Stop();
            stageLog.DurationMs = sw.ElapsedMilliseconds;
            _stageStopwatches.Remove(stageLogId);
        }

        await _logRepository.UpdateStageLogAsync(stageLog, cancellationToken);

        _logger.LogError("[{StageCode}] Failed: {ErrorMessage}",
            stageLog.Stage?.StageCode ?? "UNKNOWN", errorMessage);
    }

    public async Task LogInfoAsync(
        Guid logId,
        string stageCode,
        string message,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var stage = await _logRepository.GetStageByCodeAsync(stageCode, cancellationToken);
        var stageLog = new PathRAGStageLog
        {
            Id = Guid.NewGuid(),
            LogId = logId,
            StageId = stage?.Id ?? 0,
            DocumentId = documentId,
            Status = "Logged",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            LogLevel = "Information",
            Message = message
        };

        await _logRepository.AddStageLogAsync(stageLog, cancellationToken);
        _logger.LogInformation("[{StageCode}] {Message}", stageCode, message);
    }

    public async Task LogWarningAsync(
        Guid logId,
        string stageCode,
        string message,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var stage = await _logRepository.GetStageByCodeAsync(stageCode, cancellationToken);
        var stageLog = new PathRAGStageLog
        {
            Id = Guid.NewGuid(),
            LogId = logId,
            StageId = stage?.Id ?? 0,
            DocumentId = documentId,
            Status = "Logged",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            LogLevel = "Warning",
            Message = message
        };

        await _logRepository.AddStageLogAsync(stageLog, cancellationToken);
        _logger.LogWarning("[{StageCode}] {Message}", stageCode, message);
    }

    public async Task LogErrorAsync(
        Guid logId,
        string stageCode,
        string message,
        string? errorDetails = null,
        Guid? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var stage = await _logRepository.GetStageByCodeAsync(stageCode, cancellationToken);
        var stageLog = new PathRAGStageLog
        {
            Id = Guid.NewGuid(),
            LogId = logId,
            StageId = stage?.Id ?? 0,
            DocumentId = documentId,
            Status = "Logged",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            LogLevel = "Error",
            Message = message,
            ErrorMessage = errorDetails
        };

        await _logRepository.AddStageLogAsync(stageLog, cancellationToken);
        _logger.LogError("[{StageCode}] {Message}: {ErrorDetails}", stageCode, message, errorDetails);
    }

    public async Task CompleteOperationAsync(
        Guid logId,
        CancellationToken cancellationToken = default)
    {
        var log = await _logRepository.GetLogByIdAsync(logId, cancellationToken);
        if (log == null) return;

        log.Status = "Completed";
        log.CompletedAt = DateTimeOffset.UtcNow;

        if (_operationStopwatches.TryGetValue(logId, out var sw))
        {
            sw.Stop();
            log.DurationMs = sw.ElapsedMilliseconds;
            _operationStopwatches.Remove(logId);
        }

        await _logRepository.UpdateLogAsync(log, cancellationToken);

        _logger.LogInformation("Completed {OperationType} operation {LogId} in {DurationMs}ms",
            log.OperationType, logId, log.DurationMs);
    }

    public async Task FailOperationAsync(
        Guid logId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var log = await _logRepository.GetLogByIdAsync(logId, cancellationToken);
        if (log == null) return;

        log.Status = "Failed";
        log.CompletedAt = DateTimeOffset.UtcNow;
        log.ErrorMessage = errorMessage;

        if (_operationStopwatches.TryGetValue(logId, out var sw))
        {
            sw.Stop();
            log.DurationMs = sw.ElapsedMilliseconds;
            _operationStopwatches.Remove(logId);
        }

        await _logRepository.UpdateLogAsync(log, cancellationToken);

        _logger.LogError("Failed {OperationType} operation {LogId}: {ErrorMessage}",
            log.OperationType, logId, errorMessage);
    }

    public async Task<PathRAGLog?> GetLogDetailsAsync(
        Guid logId,
        CancellationToken cancellationToken = default)
    {
        return await _logRepository.GetLogWithStagesAsync(logId, cancellationToken);
    }

    private static int GetTotalStagesForOperation(string operationType) => operationType switch
    {
        "DocumentUpload" => 10,
        "SendMessage" => 9,
        "GetKnowledgeGraph" => 5,
        _ => 0
    };
}
