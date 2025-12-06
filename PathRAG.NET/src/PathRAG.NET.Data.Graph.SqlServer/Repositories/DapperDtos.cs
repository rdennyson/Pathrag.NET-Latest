using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Graph.SqlServer.Repositories;

/// <summary>
/// Internal DTO for mapping Dapper query results to GraphEntity
/// Note: Graph tables no longer contain embeddings (stored in EntityVectors table)
/// </summary>
internal class GraphEntityDto
{
    public string? NodeId { get; set; }
    public Guid Id { get; set; }
    public string EntityName { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string? Description { get; set; }
    public string? SourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public GraphEntity ToEntity() => new()
    {
        NodeId = NodeId,
        Id = Id,
        EntityName = EntityName,
        EntityType = EntityType,
        Description = Description,
        SourceId = SourceId,
        CreatedAt = CreatedAt
    };
}

/// <summary>
/// Internal DTO for mapping Dapper query results to GraphRelationship
/// Note: Graph tables no longer contain embeddings (stored in RelationshipVectors table)
/// </summary>
internal class GraphRelationshipDto
{
    public string? EdgeId { get; set; }
    public Guid Id { get; set; }
    public string SourceEntityName { get; set; } = null!;
    public string TargetEntityName { get; set; } = null!;
    public double Weight { get; set; }
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public string? SourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public GraphRelationship ToEntity() => new()
    {
        EdgeId = EdgeId,
        Id = Id,
        SourceEntityName = SourceEntityName,
        TargetEntityName = TargetEntityName,
        Weight = Weight,
        Description = Description,
        Keywords = Keywords,
        SourceId = SourceId,
        CreatedAt = CreatedAt
    };
}

/// <summary>
/// Internal DTO for mapping Dapper query results to EntityVector
/// SQL Server VECTOR type returns as string like "[0.123,0.456,...]"
/// </summary>
internal class EntityVectorDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? Embedding { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public EntityVector ToEntity() => new()
    {
        Id = Id,
        EntityName = EntityName,
        Content = Content,
        Embedding = ParseVectorString(Embedding),
        CreatedAt = CreatedAt
    };

    private static float[] ParseVectorString(string? vectorString)
    {
        if (string.IsNullOrEmpty(vectorString)) return [];

        // SQL Server VECTOR returns as "[0.123,0.456,...]" string
        var trimmed = vectorString.Trim('[', ']');
        if (string.IsNullOrEmpty(trimmed)) return [];

        return trimmed.Split(',')
            .Select(s => float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f)
            .ToArray();
    }
}

/// <summary>
/// Internal DTO for mapping Dapper query results to RelationshipVector
/// SQL Server VECTOR type returns as string like "[0.123,0.456,...]"
/// </summary>
internal class RelationshipVectorDto
{
    public Guid Id { get; set; }
    public string SourceEntityName { get; set; } = null!;
    public string TargetEntityName { get; set; } = null!;
    public string Content { get; set; } = null!;
    public string? Embedding { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public RelationshipVector ToEntity() => new()
    {
        Id = Id,
        SourceEntityName = SourceEntityName,
        TargetEntityName = TargetEntityName,
        Content = Content,
        Embedding = ParseVectorString(Embedding),
        CreatedAt = CreatedAt
    };

    private static float[] ParseVectorString(string? vectorString)
    {
        if (string.IsNullOrEmpty(vectorString)) return [];

        // SQL Server VECTOR returns as "[0.123,0.456,...]" string
        var trimmed = vectorString.Trim('[', ']');
        if (string.IsNullOrEmpty(trimmed)) return [];

        return trimmed.Split(',')
            .Select(s => float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var f) ? f : 0f)
            .ToArray();
    }
}

/// <summary>
/// DTO for one-hop path query results
/// Note: No embeddings - they are stored in separate vector tables
/// </summary>
internal class OneHopPathDto
{
    public string? SrcNodeId { get; set; }
    public Guid SrcId { get; set; }
    public string SrcEntityName { get; set; } = null!;
    public string SrcEntityType { get; set; } = null!;
    public string? SrcDescription { get; set; }
    public string? SrcSourceId { get; set; }
    public DateTimeOffset SrcCreatedAt { get; set; }

    public string? EdgeId { get; set; }
    public Guid RelId { get; set; }
    public string SourceEntityName { get; set; } = null!;
    public string TargetEntityName { get; set; } = null!;
    public double Weight { get; set; }
    public string? RelDescription { get; set; }
    public string? Keywords { get; set; }
    public string? RelSourceId { get; set; }
    public DateTimeOffset RelCreatedAt { get; set; }

    public string? TgtNodeId { get; set; }
    public Guid TgtId { get; set; }
    public string TgtEntityName { get; set; } = null!;
    public string TgtEntityType { get; set; } = null!;
    public string? TgtDescription { get; set; }
    public string? TgtSourceId { get; set; }
    public DateTimeOffset TgtCreatedAt { get; set; }

    public GraphEntity ToSourceEntity() => new()
    {
        NodeId = SrcNodeId, Id = SrcId, EntityName = SrcEntityName, EntityType = SrcEntityType,
        Description = SrcDescription, SourceId = SrcSourceId, CreatedAt = SrcCreatedAt
    };

    public GraphRelationship ToRelationship() => new()
    {
        EdgeId = EdgeId, Id = RelId, SourceEntityName = SourceEntityName, TargetEntityName = TargetEntityName,
        Weight = Weight, Description = RelDescription, Keywords = Keywords, SourceId = RelSourceId, CreatedAt = RelCreatedAt
    };

    public GraphEntity ToTargetEntity() => new()
    {
        NodeId = TgtNodeId, Id = TgtId, EntityName = TgtEntityName, EntityType = TgtEntityType,
        Description = TgtDescription, SourceId = TgtSourceId, CreatedAt = TgtCreatedAt
    };
}

/// <summary>
/// DTO for multi-hop path query results
/// </summary>
internal class PathQueryResult
{
    public string? PathNodes { get; set; }
    public string? PathEdgeDescriptions { get; set; }
    public string? LastNode { get; set; }
}

