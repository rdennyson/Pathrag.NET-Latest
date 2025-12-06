using AutoMapper;
using MediatR;
using PathRAG.NET.Core.Services;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Queries.Graph;

public record GetKnowledgeGraphQuery(int Limit = 100) : IRequest<KnowledgeGraphDto>;

public class GetKnowledgeGraphQueryHandler : IRequestHandler<GetKnowledgeGraphQuery, KnowledgeGraphDto>
{
    private readonly IGraphRepository _graphRepository;
    private readonly IMapper _mapper;

    public GetKnowledgeGraphQueryHandler(IGraphRepository graphRepository, IMapper mapper)
    {
        _graphRepository = graphRepository;
        _mapper = mapper;
    }

    public async Task<KnowledgeGraphDto> Handle(GetKnowledgeGraphQuery request, CancellationToken cancellationToken)
    {
        var entities = await _graphRepository.GetAllEntitiesAsync(request.Limit, cancellationToken);
        var relationships = await _graphRepository.GetAllRelationshipsAsync(request.Limit, cancellationToken);

        var nodes = entities.Select(e => new GraphNodeDto
        {
            Id = e.EntityName,
            Label = e.EntityName,
            Type = e.EntityType,
            Description = e.Description
        });
        var edges = relationships.Select(r => new GraphEdgeDto
        {
            Id = $"{r.SourceEntityName}->{r.TargetEntityName}",
            Source = r.SourceEntityName,
            Target = r.TargetEntityName,
            Label = r.Keywords,
            Weight = r.Weight
        });

        return new KnowledgeGraphDto { Nodes = nodes, Edges = edges };
    }
}

public record GetQueryGraphQuery(string Query, int TopK = 40) : IRequest<KnowledgeGraphDto>;

public class GetQueryGraphQueryHandler : IRequestHandler<GetQueryGraphQuery, KnowledgeGraphDto>
{
    private readonly IPathRAGQueryService _pathRAGService;

    public GetQueryGraphQueryHandler(IPathRAGQueryService pathRAGService)
    {
        _pathRAGService = pathRAGService;
    }

    public async Task<KnowledgeGraphDto> Handle(GetQueryGraphQuery request, CancellationToken cancellationToken)
    {
        return await _pathRAGService.GetQueryGraphAsync(request.Query, request.TopK, null, cancellationToken);
    }
}

public record GetGraphStatsQuery : IRequest<GraphStatsDto>;

public class GetGraphStatsQueryHandler : IRequestHandler<GetGraphStatsQuery, GraphStatsDto>
{
    private readonly IGraphRepository _graphRepository;

    public GetGraphStatsQueryHandler(IGraphRepository graphRepository)
    {
        _graphRepository = graphRepository;
    }

    public async Task<GraphStatsDto> Handle(GetGraphStatsQuery request, CancellationToken cancellationToken)
    {
        var entities = await _graphRepository.GetAllEntitiesAsync(int.MaxValue, cancellationToken);
        var relationships = await _graphRepository.GetAllRelationshipsAsync(int.MaxValue, cancellationToken);

        var entityList = entities.ToList();
        var relationshipList = relationships.ToList();

        var entityTypes = entityList
            .GroupBy(e => e.EntityType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new GraphStatsDto
        {
            TotalEntities = entityList.Count,
            TotalRelationships = relationshipList.Count,
            EntityTypeDistribution = entityTypes
        };
    }
}

