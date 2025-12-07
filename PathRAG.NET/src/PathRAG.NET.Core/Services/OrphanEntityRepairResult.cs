namespace PathRAG.NET.Core.Services;

public record OrphanEntityRepairResult(
    int OrphanEntities,
    int RelationshipsAdded
);
