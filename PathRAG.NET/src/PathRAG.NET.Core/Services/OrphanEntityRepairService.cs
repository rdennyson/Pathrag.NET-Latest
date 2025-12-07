using System.Text;
using Microsoft.Extensions.AI;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

public class OrphanEntityRepairService : IOrphanEntityRepairService
{
    private const int MaxContextCharacters = 8000;

    private readonly IGraphRepository _graphRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IEntityExtractionService _entityExtractionService;
    private readonly IEntityMergingService _entityMergingService;
    private readonly IGraphVectorRepository _graphVectorRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public OrphanEntityRepairService(
        IGraphRepository graphRepository,
        IDocumentChunkRepository chunkRepository,
        IEntityExtractionService entityExtractionService,
        IEntityMergingService entityMergingService,
        IGraphVectorRepository graphVectorRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _graphRepository = graphRepository;
        _chunkRepository = chunkRepository;
        _entityExtractionService = entityExtractionService;
        _entityMergingService = entityMergingService;
        _graphVectorRepository = graphVectorRepository;
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<OrphanEntityRepairResult> RepairAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var entities = (await _graphRepository.GetAllEntitiesAsync(int.MaxValue, new[] { documentId }, cancellationToken)).ToList();
        var relationships = (await _graphRepository.GetRelationshipsByDocumentIdAsync(documentId, cancellationToken)).ToList();

        var connectedNames = relationships
            .SelectMany(r => new[] { r.SourceEntityName, r.TargetEntityName })
            .Select(name => name.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphanEntities = entities
            .Where(e => !connectedNames.Contains(e.EntityName.ToUpperInvariant()))
            .ToList();

        if (orphansAny(orphanEntities))
        {
            var contextText = await BuildContextTextAsync(documentId, cancellationToken);
            if (string.IsNullOrWhiteSpace(contextText))
            {
                return new OrphanEntityRepairResult(orphanEntities.Count, 0);
            }

            var (extractedEntities, extractedRelationships) =
                await _entityExtractionService.ExtractEntitiesAndRelationshipsAsync(contextText, documentId, "OrphanRepair", cancellationToken);

            var orphanNames = orphanEntities.Select(e => e.EntityName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var relevantRels = extractedRelationships
                .Where(rel => orphanNames.Contains(rel.SourceEntity) || orphanNames.Contains(rel.TargetEntity))
                .ToList();

            var created = 0;
            foreach (var rel in relevantRels)
            {
                var merged = await _entityMergingService.MergeAndUpsertRelationshipAsync(rel, cancellationToken);

                var content = $"{merged.SourceEntityName}{merged.TargetEntityName}{merged.Keywords}{merged.Description}";
                var embedding = await _embeddingGenerator.GenerateAsync([content], cancellationToken: cancellationToken);

                var vector = new RelationshipVector
                {
                    Id = Guid.NewGuid(),
                    SourceEntityName = merged.SourceEntityName,
                    TargetEntityName = merged.TargetEntityName,
                    Content = content,
                    DocumentId = documentId,
                    Embedding = embedding[0].Vector.ToArray()
                };

                await _graphVectorRepository.UpsertRelationshipVectorAsync(vector, cancellationToken);
                created++;
            }

            return new OrphanEntityRepairResult(orphanEntities.Count, created);
        }

        return new OrphanEntityRepairResult(0, 0);
    }

    private static bool orphansAny(List<GraphEntity> orphanEntities) => orphanEntities != null && orphanEntities.Count > 0;

    private async Task<string> BuildContextTextAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var chunks = await _chunkRepository.GetByDocumentIdAsync(documentId, cancellationToken);
        if (!chunks.Any())
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var chunk in chunks)
        {
            if (builder.Length >= MaxContextCharacters)
            {
                break;
            }

            var text = chunk.Content;
            var remaining = MaxContextCharacters - builder.Length;
            if (text.Length > remaining)
            {
                text = text[..remaining];
            }

            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(text);
        }

        return builder.ToString();
    }
}
