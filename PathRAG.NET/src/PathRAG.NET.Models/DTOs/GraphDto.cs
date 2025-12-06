namespace PathRAG.NET.Models.DTOs;

public class GraphEntityDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Rank { get; set; }
}

public class GraphRelationshipDto
{
    public Guid Id { get; set; }
    public string SourceEntityName { get; set; } = string.Empty;
    public string TargetEntityName { get; set; } = string.Empty;
    public double Weight { get; set; }
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public int Rank { get; set; }
}

public class GraphNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class GraphEdgeDto
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Label { get; set; }
    public double Weight { get; set; }
}

public class KnowledgeGraphDto
{
    public IEnumerable<GraphNodeDto> Nodes { get; set; } = [];
    public IEnumerable<GraphEdgeDto> Edges { get; set; } = [];
}

public record GraphQueryRequest(
    string Query,
    int TopK = 40
);

public class GraphQueryResponse
{
    public string Query { get; set; } = string.Empty;
    public KnowledgeGraphDto Graph { get; set; } = new();
    public IEnumerable<GraphPathDto> Paths { get; set; } = [];
}

public class GraphPathDto
{
    public IEnumerable<string> Nodes { get; set; } = [];
    public string Context { get; set; } = string.Empty;
    public int HopCount { get; set; }
}

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

public class GraphStatsDto
{
    public int TotalEntities { get; set; }
    public int TotalRelationships { get; set; }
    public Dictionary<string, int> EntityTypeDistribution { get; set; } = [];
}
