namespace PathRAG.NET.Models.DTOs;

public record GraphEntityDto(
    Guid Id,
    string EntityName,
    string EntityType,
    string? Description,
    int Rank
);

public record GraphRelationshipDto(
    Guid Id,
    string SourceEntityName,
    string TargetEntityName,
    double Weight,
    string? Description,
    string? Keywords,
    int Rank
);

public record GraphNodeDto(
    string Id,
    string Label,
    string Type,
    string? Description
);

public record GraphEdgeDto(
    string Id,
    string Source,
    string Target,
    string? Label,
    double Weight
);

public record KnowledgeGraphDto(
    IEnumerable<GraphNodeDto> Nodes,
    IEnumerable<GraphEdgeDto> Edges
);

public record GraphQueryRequest(
    string Query,
    int TopK = 40
);

public record GraphQueryResponse(
    string Query,
    KnowledgeGraphDto Graph,
    IEnumerable<GraphPathDto> Paths
);

public record GraphPathDto(
    IEnumerable<string> Nodes,
    string Context,
    int HopCount
);

public record ExtractedEntityDto(
    string EntityName,
    string EntityType,
    string Description,
    string SourceId
);

public record ExtractedRelationshipDto(
    string SourceEntity,
    string TargetEntity,
    string Description,
    string Keywords,
    double Weight,
    string SourceId
);

public record GraphStatsDto(
    int TotalEntities,
    int TotalRelationships,
    Dictionary<string, int> EntityTypeDistribution
);
