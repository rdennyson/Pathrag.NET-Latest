using AutoMapper;
using MediatR;
using PathRAG.NET.Core.Services;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;
using System.Linq;

namespace PathRAG.NET.Core.Queries.Graph;

public record GetKnowledgeGraphQuery(int Limit = 100, IEnumerable<Guid>? DocumentTypeIds = null) : IRequest<KnowledgeGraphDto>;

public class GetKnowledgeGraphQueryHandler : IRequestHandler<GetKnowledgeGraphQuery, KnowledgeGraphDto>
{
    private readonly IGraphRepository _graphRepository;
    private readonly IMapper _mapper;
    private readonly IDocumentRepository _documentRepository;

    public GetKnowledgeGraphQueryHandler(IGraphRepository graphRepository, IMapper mapper, IDocumentRepository documentRepository)
    {
        _graphRepository = graphRepository;
        _mapper = mapper;
        _documentRepository = documentRepository;
    }

    public async Task<KnowledgeGraphDto> Handle(GetKnowledgeGraphQuery request, CancellationToken cancellationToken)
    {
        var documentIds = await ResolveDocumentIdsAsync(request.DocumentTypeIds, cancellationToken);
        var entities = await _graphRepository.GetAllEntitiesAsync(request.Limit, documentIds, cancellationToken);
        var relationships = await _graphRepository.GetAllRelationshipsAsync(request.Limit, documentIds, cancellationToken);

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

    private async Task<IEnumerable<Guid>?> ResolveDocumentIdsAsync(IEnumerable<Guid>? documentTypeIds, CancellationToken cancellationToken)
    {
        if (documentTypeIds == null || !documentTypeIds.Any())
        {
            return null;
        }

        var ids = await _documentRepository.GetIdsByTypeIdsAsync(documentTypeIds, cancellationToken);
        return ids.Any() ? ids : null;
    }
}

public record GetQueryGraphQuery(string Query, int TopK = 40, IEnumerable<Guid>? DocumentTypeIds = null) : IRequest<KnowledgeGraphDto>;

public class GetQueryGraphQueryHandler : IRequestHandler<GetQueryGraphQuery, KnowledgeGraphDto>
{
    private readonly IPathRAGQueryService _pathRAGService;

    public GetQueryGraphQueryHandler(IPathRAGQueryService pathRAGService)
    {
        _pathRAGService = pathRAGService;
    }

    public async Task<KnowledgeGraphDto> Handle(GetQueryGraphQuery request, CancellationToken cancellationToken)
    {
        return await _pathRAGService.GetQueryGraphAsync(request.Query, request.TopK, request.DocumentTypeIds, cancellationToken: cancellationToken);
    }
}

public record GetGraphStatsQuery(IEnumerable<Guid>? DocumentTypeIds = null) : IRequest<GraphStatsDto>;

public class GetGraphStatsQueryHandler : IRequestHandler<GetGraphStatsQuery, GraphStatsDto>
{
    private readonly IGraphRepository _graphRepository;
    private readonly IDocumentRepository _documentRepository;

    public GetGraphStatsQueryHandler(IGraphRepository graphRepository, IDocumentRepository documentRepository)
    {
        _graphRepository = graphRepository;
        _documentRepository = documentRepository;
    }

    public async Task<GraphStatsDto> Handle(GetGraphStatsQuery request, CancellationToken cancellationToken)
    {
        var documentIds = await ResolveDocumentIdsAsync(request.DocumentTypeIds, cancellationToken);
        var entities = await _graphRepository.GetAllEntitiesAsync(int.MaxValue, documentIds, cancellationToken);
        var relationships = await _graphRepository.GetAllRelationshipsAsync(int.MaxValue, documentIds, cancellationToken);

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

    private async Task<IEnumerable<Guid>?> ResolveDocumentIdsAsync(IEnumerable<Guid>? documentTypeIds, CancellationToken cancellationToken)
    {
        if (documentTypeIds == null || !documentTypeIds.Any())
        {
            return null;
        }

        var ids = await _documentRepository.GetIdsByTypeIdsAsync(documentTypeIds, cancellationToken);
        return ids.Any() ? ids : null;
    }
}

