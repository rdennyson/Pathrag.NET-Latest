namespace PathRAG.NET.Models.Entities;

/// <summary>
/// Represents a log entry for PathRAG operations (document processing, queries, etc.)
/// </summary>
public class PathRAGLog
{
    public Guid Id { get; set; }
    public Guid? DocumentId { get; set; }
    public Guid? ThreadId { get; set; }
    public string OperationType { get; set; } = string.Empty; // DocumentUpload, Query, GraphQuery
    public string Status { get; set; } = "Started"; // Started, InProgress, Completed, Failed
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; } // JSON for additional data
    public int TotalStages { get; set; }
    public int CompletedStages { get; set; }
    
    public virtual ICollection<PathRAGStageLog> StageLogs { get; set; } = [];
}

/// <summary>
/// Represents the predefined stages in PathRAG processing pipeline
/// </summary>
public class PathRAGStage
{
    public int Id { get; set; }
    public string StageName { get; set; } = string.Empty;
    public string StageCode { get; set; } = string.Empty;
    public string OperationType { get; set; } = string.Empty; // DocumentUpload, Query, Both
    public int StageOrder { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Represents a log entry for a specific stage in a PathRAG operation
/// </summary>
public class PathRAGStageLog
{
    public Guid Id { get; set; }
    public Guid LogId { get; set; }
    public int StageId { get; set; }
    public Guid? DocumentId { get; set; }
    public string Status { get; set; } = "Started"; // Started, InProgress, Completed, Failed, Skipped
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long? DurationMs { get; set; }
    public int? ItemsProcessed { get; set; }
    public int? TokensUsed { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Details { get; set; } // JSON for stage-specific details
    public string LogLevel { get; set; } = "Information";
    public string? Message { get; set; }
    
    public virtual PathRAGLog? Log { get; set; }
    public virtual PathRAGStage? Stage { get; set; }
}

