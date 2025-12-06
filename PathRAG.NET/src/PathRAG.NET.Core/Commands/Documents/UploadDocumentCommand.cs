using System.Text.Json;
using MediatR;
using Microsoft.Extensions.AI;
using PathRAG.NET.Core.Services;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Commands.Documents;

public record UploadDocumentCommand(
    string FileName,
    string ContentType,
    Stream FileStream
) : IRequest<DocumentUploadResponse>;

/// <summary>
/// Document upload handler matching Python PathRAG's extract_entities flow in operate.py
/// </summary>
public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentUploadResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IGraphVectorRepository _graphVectorRepository;
    private readonly IContentDecoderService _contentDecoder;
    private readonly ITextChunkerService _textChunker;
    private readonly IEntityExtractionService _entityExtractor;
    private readonly IEntityMergingService _entityMergingService;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly PathRAGSettings _settings;
    private readonly IPathRAGLoggerService _logger;

    public UploadDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentChunkRepository chunkRepository,
        IGraphVectorRepository graphVectorRepository,
        IContentDecoderService contentDecoder,
        ITextChunkerService textChunker,
        IEntityExtractionService entityExtractor,
        IEntityMergingService entityMergingService,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PathRAGSettings settings,
        IPathRAGLoggerService logger)
    {
        _documentRepository = documentRepository;
        _chunkRepository = chunkRepository;
        _graphVectorRepository = graphVectorRepository;
        _contentDecoder = contentDecoder;
        _textChunker = textChunker;
        _entityExtractor = entityExtractor;
        _entityMergingService = entityMergingService;
        _embeddingGenerator = embeddingGenerator;
        _settings = settings;
        _logger = logger;
    }

    public async Task<DocumentUploadResponse> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // Start operation logging
        var logId = await _logger.StartOperationAsync(
            "DocumentUpload",
            metadata: $"{{\"fileName\":\"{request.FileName}\",\"contentType\":\"{request.ContentType}\"}}",
            cancellationToken: cancellationToken);

        Guid stageLogId;

        // Step 1: Create document record
        stageLogId = await _logger.StartStageAsync(logId, "DOC_CREATE", message: $"Creating document record for {request.FileName}", cancellationToken: cancellationToken);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            Name = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileStream.Length,
            Status = "processing"
        };
        await _documentRepository.AddAsync(document, cancellationToken);
        await _logger.CompleteStageAsync(stageLogId, details: $"Document ID: {document.Id}", cancellationToken: cancellationToken);

        try
        {
            // Step 2: Decode content
            stageLogId = await _logger.StartStageAsync(logId, "DOC_DECODE", document.Id, $"Decoding {request.ContentType} content", cancellationToken);
            var text = await _contentDecoder.DecodeAsync(request.FileStream, request.ContentType, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, details: $"Decoded {text.Length} characters", cancellationToken: cancellationToken);

            // Step 3: Chunk the text
            stageLogId = await _logger.StartStageAsync(logId, "DOC_CHUNK", document.Id, "Chunking text by token size", cancellationToken);
            var chunks = _textChunker.ChunkByTokenSize(
                text,
                document.Id.ToString(),
                _settings.MaxTokensPerParagraph,
                _settings.OverlapTokens
            ).ToList();

            var totalTokens = chunks.Sum(c => c.Tokens);
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: chunks.Count, tokensUsed: totalTokens, details: $"Created {chunks.Count} chunks with {totalTokens} total tokens", cancellationToken: cancellationToken);

            // Step 4: Generate embeddings for chunks (in batches) and store in DocumentChunks
            stageLogId = await _logger.StartStageAsync(logId, "DOC_EMBED_CHUNKS", document.Id, $"Generating embeddings for {chunks.Count} chunks", cancellationToken);
            var documentChunks = new List<DocumentChunk>();
            for (int i = 0; i < chunks.Count; i += _settings.EmbeddingBatchSize)
            {
                var batch = chunks.Skip(i).Take(_settings.EmbeddingBatchSize).ToList();
                var embeddings = await _embeddingGenerator.GenerateAsync(
                    batch.Select(c => c.Content).ToList(),
                    cancellationToken: cancellationToken);

                for (int j = 0; j < batch.Count; j++)
                {
                    documentChunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = document.Id,
                        Index = i + j,
                        Content = batch[j].Content,
                        TokenCount = batch[j].Tokens,
                        Embedding = embeddings[j].Vector.ToArray()
                    });
                }
            }
            await _chunkRepository.AddRangeAsync(documentChunks, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: documentChunks.Count, details: $"Generated embeddings for {documentChunks.Count} chunks", cancellationToken: cancellationToken);

            // Step 5: Extract entities and relationships from each chunk
            // (matching Python PathRAG's extract_entities flow in operate.py)
            // First collect all entities/relationships, then merge duplicates
            stageLogId = await _logger.StartStageAsync(logId, "DOC_EXTRACT_ENTITIES", document.Id, $"Extracting entities and relationships from {chunks.Count} chunks", cancellationToken);
            var maybeNodes = new Dictionary<string, List<ExtractedEntityDto>>();
            var maybeEdges = new Dictionary<(string, string), List<ExtractedRelationshipDto>>();

            foreach (var chunk in chunks)
            {
                var (entities, relationships) = await _entityExtractor.ExtractEntitiesAndRelationshipsAsync(
                    chunk.Content, chunk.Id, cancellationToken);

                // Group entities by name (matching Python's maybe_nodes dict)
                foreach (var entity in entities)
                {
                    if (!maybeNodes.ContainsKey(entity.EntityName))
                        maybeNodes[entity.EntityName] = [];
                    maybeNodes[entity.EntityName].Add(entity);
                }

                // Group relationships by (src, tgt) pair (matching Python's maybe_edges dict)
                foreach (var rel in relationships)
                {
                    var key = (rel.SourceEntity, rel.TargetEntity);
                    if (!maybeEdges.ContainsKey(key))
                        maybeEdges[key] = [];
                    maybeEdges[key].Add(rel);
                }
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: maybeNodes.Count + maybeEdges.Count, details: $"Extracted {maybeNodes.Count} unique entities and {maybeEdges.Count} unique relationships", cancellationToken: cancellationToken);

            // Step 6: Merge and upsert entities (matching Python's _merge_nodes_then_upsert)
            stageLogId = await _logger.StartStageAsync(logId, "DOC_MERGE_ENTITIES", document.Id, $"Merging {maybeNodes.Count} entities", cancellationToken);
            var allEntitiesData = new List<GraphEntity>();
            foreach (var (entityName, entityList) in maybeNodes)
            {
                // Use the first entity as base, merging service handles the rest
                var mergedEntity = await _entityMergingService.MergeAndUpsertEntityAsync(
                    entityList.First(), cancellationToken);

                // If multiple extractions for same entity, merge them all
                foreach (var entity in entityList.Skip(1))
                {
                    mergedEntity = await _entityMergingService.MergeAndUpsertEntityAsync(entity, cancellationToken);
                }
                allEntitiesData.Add(mergedEntity);
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: allEntitiesData.Count, details: $"Merged and upserted {allEntitiesData.Count} entities", cancellationToken: cancellationToken);

            // Step 7: Merge and upsert relationships (matching Python's _merge_edges_then_upsert)
            stageLogId = await _logger.StartStageAsync(logId, "DOC_MERGE_RELS", document.Id, $"Merging {maybeEdges.Count} relationships", cancellationToken);
            var allRelationshipsData = new List<GraphRelationship>();
            foreach (var ((srcId, tgtId), relList) in maybeEdges)
            {
                // Use the first relationship as base, merging service handles the rest
                var mergedRel = await _entityMergingService.MergeAndUpsertRelationshipAsync(
                    relList.First(), cancellationToken);

                // If multiple extractions for same edge, merge them all
                foreach (var rel in relList.Skip(1))
                {
                    mergedRel = await _entityMergingService.MergeAndUpsertRelationshipAsync(rel, cancellationToken);
                }
                allRelationshipsData.Add(mergedRel);
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: allRelationshipsData.Count, details: $"Merged and upserted {allRelationshipsData.Count} relationships", cancellationToken: cancellationToken);

            // Step 8: Store entity embeddings in EntityVectors table
            // (matching Python PathRAG's entities_vdb.upsert in operate.py lines 432-440)
            stageLogId = await _logger.StartStageAsync(logId, "DOC_EMBED_ENTITIES", document.Id, $"Generating embeddings for {allEntitiesData.Count} entities", cancellationToken);
            foreach (var entity in allEntitiesData)
            {
                // Python format: content = dp["entity_name"] + dp["description"] (operate.py line 435)
                var entityContent = entity.EntityName + entity.Description;
                var embeddingResult = await _embeddingGenerator.GenerateAsync(
                    [entityContent],
                    cancellationToken: cancellationToken);

                var entityVector = new EntityVector
                {
                    Id = Guid.NewGuid(),
                    EntityName = entity.EntityName,
                    Content = entityContent,
                    Embedding = embeddingResult[0].Vector.ToArray()
                };
                await _graphVectorRepository.UpsertEntityVectorAsync(entityVector, cancellationToken);
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: allEntitiesData.Count, details: $"Stored {allEntitiesData.Count} entity vectors", cancellationToken: cancellationToken);

            // Step 9: Store relationship embeddings in RelationshipVectors table
            // (matching Python PathRAG's relationships_vdb.upsert in operate.py lines 442-454)
            stageLogId = await _logger.StartStageAsync(logId, "DOC_EMBED_RELS", document.Id, $"Generating embeddings for {allRelationshipsData.Count} relationships", cancellationToken);
            foreach (var rel in allRelationshipsData)
            {
                // Python format: content = dp["keywords"] + dp["src_id"] + dp["tgt_id"] + dp["description"]
                var relContent = rel.Keywords + rel.SourceEntityName + rel.TargetEntityName + rel.Description;
                var embeddingResult = await _embeddingGenerator.GenerateAsync(
                    [relContent],
                    cancellationToken: cancellationToken);

                var relationshipVector = new RelationshipVector
                {
                    Id = Guid.NewGuid(),
                    SourceEntityName = rel.SourceEntityName,
                    TargetEntityName = rel.TargetEntityName,
                    Content = relContent,
                    Embedding = embeddingResult[0].Vector.ToArray()
                };
                await _graphVectorRepository.UpsertRelationshipVectorAsync(relationshipVector, cancellationToken);
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: allRelationshipsData.Count, details: $"Stored {allRelationshipsData.Count} relationship vectors", cancellationToken: cancellationToken);

            // Step 10: Update document status
            stageLogId = await _logger.StartStageAsync(logId, "DOC_COMPLETE", document.Id, "Completing document processing", cancellationToken);
            document.Status = "completed";
            document.ProcessedAt = DateTimeOffset.UtcNow;
            await _documentRepository.UpdateAsync(document, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, details: "Document processing completed successfully", cancellationToken: cancellationToken);

            // Complete the operation
            await _logger.CompleteOperationAsync(logId, cancellationToken);

            return new DocumentUploadResponse(document.Id, "Document processed successfully", totalTokens);
        }
        catch (Exception ex)
        {
            document.Status = "failed";
            document.ErrorMessage = ex.Message;
            await _documentRepository.UpdateAsync(document, cancellationToken);
            await _logger.FailOperationAsync(logId, ex.Message, cancellationToken);
            throw;
        }
    }
}

