namespace PathRAG.NET.Models.DTOs;

public class PathRAGLogDto
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public string? DocumentName { get; set; }
    public Guid? ThreadId { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public int TotalStages { get; set; }
    public int CompletedStages { get; set; }
    public List<PathRAGStageLogDto> StageLogs { get; set; } = [];
}

public class PathRAGStageLogDto
{
    public Guid Id { get; set; }
    public int StageId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StageCode { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public int? ItemsProcessed { get; set; }
    public int? TokensUsed { get; set; }
    public string? ErrorMessage { get; set; }
    public string LogLevel { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class PathRAGStageDto
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StageCode { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty;
    public int StageOrder { get; set; }
    public string? Description { get; set; }
}

public class LogPerformanceSummaryDto
{
    public int TotalOperations { get; set; }
    public int CompletedOperations { get; set; }
    public int FailedOperations { get; set; }
    public int InProgressOperations { get; set; }
    public long AverageOperationDurationMs { get; set; }
    public List<StagePerformanceDto> StagePerformance { get; set; } = [];
    public List<OperationTypeBreakdownDto> OperationTypeBreakdown { get; set; } = [];
    public List<StagePerformanceDto> SlowestStages { get; set; } = [];
}

public class StagePerformanceDto
{
    public string StageCode { get; set; } = string.Empty;
    public string StageName { get; set; } = string.Empty;
    public int TotalExecutions { get; set; }
    public long AverageDurationMs { get; set; }
    public long MinDurationMs { get; set; }
    public long MaxDurationMs { get; set; }
    public long TotalDurationMs { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
}

public class OperationTypeBreakdownDto
{
    public string OperationType { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
    public int CompletedOperations { get; set; }
    public int FailedOperations { get; set; }
    public long AverageDurationMs { get; set; }
}

public class LogFilterDto
{
    public Guid? DocumentId { get; set; }
    public Guid? ThreadId { get; set; }
    public string? OperationType { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PagedLogResultDto
{
    public List<PathRAGLogDto> Logs { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

