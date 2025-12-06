using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Service for logging PathRAG operations with stage tracking
/// </summary>
public interface IPathRAGLoggerService
{
    /// <summary>
    /// Start a new operation log (DocumentUpload, Query, GraphQuery)
    /// </summary>
    Task<Guid> StartOperationAsync(
        string operationType,
        Guid? documentId = null,
        Guid? threadId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a stage within an operation
    /// </summary>
    Task<Guid> StartStageAsync(
        Guid logId,
        string stageCode,
        Guid? documentId = null,
        string? message = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete a stage successfully
    /// </summary>
    Task CompleteStageAsync(
        Guid stageLogId,
        int? itemsProcessed = null,
        int? tokensUsed = null,
        string? details = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a stage as failed
    /// </summary>
    Task FailStageAsync(
        Guid stageLogId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log an informational message within a stage
    /// </summary>
    Task LogInfoAsync(
        Guid logId,
        string stageCode,
        string message,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log a warning within a stage
    /// </summary>
    Task LogWarningAsync(
        Guid logId,
        string stageCode,
        string message,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Log an error within a stage
    /// </summary>
    Task LogErrorAsync(
        Guid logId,
        string stageCode,
        string message,
        string? errorDetails = null,
        Guid? documentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Complete an operation successfully
    /// </summary>
    Task CompleteOperationAsync(
        Guid logId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark an operation as failed
    /// </summary>
    Task FailOperationAsync(
        Guid logId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get log details with all stages
    /// </summary>
    Task<PathRAGLog?> GetLogDetailsAsync(
        Guid logId,
        CancellationToken cancellationToken = default);
}

