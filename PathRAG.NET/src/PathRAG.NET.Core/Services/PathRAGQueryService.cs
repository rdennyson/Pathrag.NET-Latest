using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.AI;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Services;

/// <summary>
/// Core PathRAG query service implementing graph-based retrieval
/// Matches Python PathRAG's kg_query and _build_query_context EXACTLY
/// Uses separate vector tables (EntityVectors, RelationshipVectors) matching Python architecture
/// </summary>
public class PathRAGQueryService : IPathRAGQueryService
{
    private readonly IKeywordsExtractionService _keywordsService;
    private readonly IGraphRepository _graphRepository;
    private readonly IGraphVectorRepository _graphVectorRepository;
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
    private readonly PathRAGSettings _settings;
    private readonly IPathRAGLoggerService _logger;
    private readonly IDocumentRepository _documentRepository;

    // Python PathRAG's GRAPH_FIELD_SEP from prompt.py
    private const string GraphFieldSep = "<SEP>";

    public PathRAGQueryService(
        IKeywordsExtractionService keywordsService,
        IGraphRepository graphRepository,
        IGraphVectorRepository graphVectorRepository,
        IDocumentChunkRepository chunkRepository,
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        PathRAGSettings settings,
        IPathRAGLoggerService logger,
        IDocumentRepository documentRepository)
    {
        _keywordsService = keywordsService;
        _graphRepository = graphRepository;
        _graphVectorRepository = graphVectorRepository;
        _chunkRepository = chunkRepository;
        _embeddingGenerator = embeddingGenerator;
        _settings = settings;
        _logger = logger;
        _documentRepository = documentRepository;
    }

    /// <summary>
    /// Build query context matching Python PathRAG's _build_query_context exactly
    /// </summary>
    public async Task<QueryContextDto> BuildQueryContextAsync(
        string query,
        QueryParamDto? queryParams = null,
        Guid? logId = null,
        CancellationToken cancellationToken = default)
    {
        queryParams ??= new QueryParamDto();
        var documentIds = await ResolveDocumentIdsAsync(queryParams.DocumentTypeIds, cancellationToken);

        // If no logId provided, this is a standalone query - create our own operation log
        var isStandaloneQuery = !logId.HasValue;
        if (isStandaloneQuery)
        {
            logId = await _logger.StartOperationAsync("SendMessage", metadata: $"{{\"query\":\"{query.Replace("\"", "\\\"")}\"}}",
                cancellationToken: cancellationToken);
        }

        try
        {
            // Step 1: Extract keywords from query (matching Python's kg_query)
            var stageLogId = await _logger.StartStageAsync(logId!.Value, "MSG_KEYWORDS", message: "Extracting keywords from query", cancellationToken: cancellationToken);
            var keywords = await _keywordsService.ExtractKeywordsAsync(query, cancellationToken);
            var llKeywords = string.Join(", ", keywords.LowLevelKeywords);
            var hlKeywords = string.Join(", ", keywords.HighLevelKeywords);
            await _logger.CompleteStageAsync(stageLogId, details: $"LL: {llKeywords}, HL: {hlKeywords}", cancellationToken: cancellationToken);

            // Step 2: Generate query embedding
            stageLogId = await _logger.StartStageAsync(logId.Value, "MSG_EMBED", message: "Generating query embedding", cancellationToken: cancellationToken);
            // Embedding is generated inside GetNodeDataAsync and GetEdgeDataAsync
            await _logger.CompleteStageAsync(stageLogId, details: "Embedding generated for entity and relationship search", cancellationToken: cancellationToken);

            // Step 3: Get low-level context using _get_node_data logic
            stageLogId = await _logger.StartStageAsync(logId.Value, "MSG_SEARCH_ENTITIES", message: "Searching entities (low-level)", cancellationToken: cancellationToken);
            var (llEntitiesContext, llRelationsContext, llTextUnitsContext) =
                await GetNodeDataAsync(llKeywords, queryParams, documentIds, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, details: $"Found {llEntitiesContext.Split('\n').Length - 2} entities", cancellationToken: cancellationToken);

            // Step 4: Get high-level context using _get_edge_data logic
            stageLogId = await _logger.StartStageAsync(logId.Value, "MSG_SEARCH_RELS", message: "Searching relationships (high-level)", cancellationToken: cancellationToken);
            var (hlEntitiesContext, hlRelationsContext, hlTextUnitsContext) =
                await GetEdgeDataAsync(hlKeywords, queryParams, documentIds, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, details: $"Found {hlRelationsContext.Split('\n').Length - 2} relationships", cancellationToken: cancellationToken);

            // Step 5: Combine contexts (matching Python's combine_contexts)
            stageLogId = await _logger.StartStageAsync(logId.Value, "MSG_BUILD_CONTEXT", message: "Building query context", cancellationToken: cancellationToken);
            var textUnitsContext = CombineTextUnits(hlTextUnitsContext, llTextUnitsContext);
            await _logger.CompleteStageAsync(stageLogId, cancellationToken: cancellationToken);

            if (isStandaloneQuery)
            {
                await _logger.CompleteOperationAsync(logId.Value, cancellationToken);
            }

            return new QueryContextDto(
                hlEntitiesContext,
                hlRelationsContext,
                llEntitiesContext,
                llRelationsContext,
                textUnitsContext
            );
        }
        catch (Exception ex)
        {
            if (isStandaloneQuery)
            {
                await _logger.FailOperationAsync(logId!.Value, ex.Message, cancellationToken);
            }
            throw;
        }
    }

    private async Task<IReadOnlyCollection<Guid>?> ResolveDocumentIdsAsync(IEnumerable<Guid>? documentTypeIds, CancellationToken cancellationToken)
    {
        if (documentTypeIds == null || !documentTypeIds.Any())
        {
            return null;
        }

        var ids = await _documentRepository.GetIdsByTypeIdsAsync(documentTypeIds, cancellationToken);
        var distinctIds = ids.Distinct().ToList();
        return distinctIds.Any() ? distinctIds : null;
    }

    /// <summary>
    /// Format context as Python PathRAG's _build_query_context output
    /// </summary>
    public string FormatQueryContext(QueryContextDto context)
    {
        // Exact format from Python PathRAG's _build_query_context (operate.py lines 661-684)
        return $@"
-----global-information-----
-----high-level entity information-----
```csv
{context.HighLevelEntitiesContext}
```
-----high-level relationship information-----
```csv
{context.HighLevelRelationsContext}
```
-----Sources-----
```csv
{context.TextUnitsContext}
```
-----local-information-----
-----low-level entity information-----
```csv
{context.LowLevelEntitiesContext}
```
-----low-level relationship information-----
```csv
{context.LowLevelRelationsContext}
```
";
    }

    /// <summary>
    /// Get node data matching Python PathRAG's _get_node_data (operate.py lines 686-750)
    /// </summary>
    private async Task<(string EntitiesContext, string RelationsContext, string TextUnitsContext)>
        GetNodeDataAsync(string keywords, QueryParamDto queryParams, IReadOnlyCollection<Guid>? documentIds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keywords))
            return ("", "", "");

        // Generate embedding for keywords
        var embeddingResult = await _embeddingGenerator.GenerateAsync([keywords], cancellationToken: cancellationToken);
        var embeddingArray = embeddingResult[0].Vector.ToArray();

        // Query entities_vdb (matching Python's entities_vdb.query)
        var entityVectors = await _graphVectorRepository.SearchEntitiesByVectorAsync(
            embeddingArray, queryParams.TopK, documentIds, cancellationToken);

        if (!entityVectors.Any())
            return ("", "", "");

        // Get node data and degrees (matching Python's get_node and node_degree)
        var nodeDatas = new List<NodeData>();
        foreach (var ev in entityVectors)
        {
            var node = await _graphRepository.GetEntityByNameAsync(ev.EntityName, cancellationToken: cancellationToken);
            if (node == null) continue;

            var degree = await _graphRepository.GetNeighborsAsync(ev.EntityName, 1, cancellationToken);
            nodeDatas.Add(new NodeData
            {
                EntityName = ev.EntityName,
                EntityType = node.EntityType,
                Description = node.Description ?? string.Empty,
                SourceId = node.SourceId ?? string.Empty,
                Rank = degree.Count()
            });
        }

        // Find related text units (matching Python's _find_most_related_text_unit_from_entities)
        var textUnits = await FindMostRelatedTextUnitsFromEntitiesAsync(nodeDatas, queryParams, cancellationToken);

        // Find related edges using path algorithm (matching Python's _find_most_related_edges_from_entities3)
        var relations = await FindMostRelatedEdgesFromEntitiesAsync(nodeDatas, queryParams, documentIds, cancellationToken);

        // Build CSV context (matching Python's list_of_list_to_csv)
        var entitiesContext = BuildEntitiesCsv(nodeDatas);
        var relationsContext = BuildRelationsCsv(relations);
        var textUnitsContext = BuildTextUnitsCsv(textUnits);

        return (entitiesContext, relationsContext, textUnitsContext);
    }

    /// <summary>
    /// Get edge data matching Python PathRAG's _get_edge_data (operate.py lines 826-900)
    /// </summary>
    private async Task<(string EntitiesContext, string RelationsContext, string TextUnitsContext)>
        GetEdgeDataAsync(string keywords, QueryParamDto queryParams, IReadOnlyCollection<Guid>? documentIds, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(keywords))
            return ("", "", "");

        // Generate embedding for keywords
        var embeddingResult = await _embeddingGenerator.GenerateAsync([keywords], cancellationToken: cancellationToken);
        var embeddingArray = embeddingResult[0].Vector.ToArray();

        // Query relationships_vdb (matching Python's relationships_vdb.query)
        var relationshipVectors = await _graphVectorRepository.SearchRelationshipsByVectorAsync(
            embeddingArray, queryParams.TopK, documentIds, cancellationToken);

        if (!relationshipVectors.Any())
            return ("", "", "");

        // Get edge data and degrees (matching Python's get_edge and edge_degree)
        var edgeDatas = new List<EdgeData>();
        foreach (var rv in relationshipVectors)
        {
            var edge = await _graphRepository.GetRelationshipAsync(rv.SourceEntityName, rv.TargetEntityName, rv.DocumentId, cancellationToken: cancellationToken);
            if (edge == null) continue;

            // Edge degree = sum of degrees of both endpoints
            var srcNeighbors = await _graphRepository.GetNeighborsAsync(rv.SourceEntityName, 1, cancellationToken);
            var tgtNeighbors = await _graphRepository.GetNeighborsAsync(rv.TargetEntityName, 1, cancellationToken);
            var edgeDegree = srcNeighbors.Count() + tgtNeighbors.Count();

            edgeDatas.Add(new EdgeData
            {
                SrcId = rv.SourceEntityName,
                TgtId = rv.TargetEntityName,
                Description = edge.Description ?? string.Empty,
                Keywords = edge.Keywords ?? string.Empty,
                Weight = edge.Weight,
                Rank = edgeDegree
            });
        }

        // Sort by rank and weight (matching Python)
        edgeDatas = edgeDatas.OrderByDescending(e => e.Rank).ThenByDescending(e => e.Weight).ToList();

        // Truncate by token size
        edgeDatas = TruncateByTokenSize(edgeDatas, e => e.Description, queryParams.MaxTokenForGlobalContext);

        // Find related entities (matching Python's _find_most_related_entities_from_relationships)
        var entities = await FindMostRelatedEntitiesFromRelationshipsAsync(edgeDatas, queryParams, cancellationToken);

        // Find related text units
        var textUnits = await FindRelatedTextUnitsFromRelationshipsAsync(edgeDatas, queryParams, cancellationToken);

        // Build CSV context (matching Python's list_of_list_to_csv)
        var relationsContext = BuildEdgesCsv(edgeDatas);
        var entitiesContext = BuildEntitiesCsv(entities);
        var textUnitsContext = BuildTextUnitsCsv(textUnits);

        return (entitiesContext, relationsContext, textUnitsContext);
    }



    /// <summary>
    /// Find most related edges from entities using path algorithm
    /// Matches Python PathRAG's _find_most_related_edges_from_entities3 (operate.py lines 1107-1239)
    /// </summary>
    private async Task<List<string>> FindMostRelatedEdgesFromEntitiesAsync(
        List<NodeData> nodeDatas, QueryParamDto queryParams, IReadOnlyCollection<Guid>? documentIds, CancellationToken cancellationToken)
    {
        var sourceNodes = nodeDatas.Select(n => n.EntityName).ToList();
        if (sourceNodes.Count < 2) return [];

        // Get all edges and nodes from graph
        var allRelationships = await _graphRepository.GetAllRelationshipsAsync(1000, documentIds, cancellationToken);
        var allEntities = await _graphRepository.GetAllEntitiesAsync(1000, documentIds, cancellationToken);

        // Build adjacency list for path finding
        var adjacency = new Dictionary<string, HashSet<string>>();
        foreach (var entity in allEntities)
        {
            adjacency[entity.EntityName] = [];
        }
        foreach (var rel in allRelationships)
        {
            if (!adjacency.ContainsKey(rel.SourceEntityName))
                adjacency[rel.SourceEntityName] = [];
            if (!adjacency.ContainsKey(rel.TargetEntityName))
                adjacency[rel.TargetEntityName] = [];
            adjacency[rel.SourceEntityName].Add(rel.TargetEntityName);
            adjacency[rel.TargetEntityName].Add(rel.SourceEntityName);
        }

        // Find paths between source nodes (1-3 hops) using DFS
        var (pathResult, oneHopPaths, twoHopPaths, threeHopPaths) =
            FindPathsAndEdgesWithStats(adjacency, sourceNodes);

        // Apply BFS weighted path scoring (threshold=0.3, alpha=0.8)
        const double threshold = 0.3;
        const double alpha = 0.8;
        var allResults = new List<(List<string> Path, double Weight)>();

        foreach (var node1 in sourceNodes)
        {
            foreach (var node2 in sourceNodes)
            {
                if (node1 != node2 && pathResult.TryGetValue((node1, node2), out var pathData))
                {
                    var results = BfsWeightedPaths(pathData.Paths, node1, node2, threshold, alpha);
                    allResults.AddRange(results);
                }
            }
        }

        // Sort by weight and deduplicate
        allResults = allResults.OrderByDescending(r => r.Weight).ToList();
        var seen = new HashSet<string>();
        var resultEdges = new List<(List<string> Path, double Weight)>();
        foreach (var (path, weight) in allResults)
        {
            var sortedKey = string.Join("-", path.OrderBy(p => p));
            if (!seen.Contains(sortedKey))
            {
                seen.Add(sortedKey);
                resultEdges.Add((path, weight));
            }
        }

        // Select paths (matching Python's logic)
        var length1 = oneHopPaths.Count / 2;
        var length2 = twoHopPaths.Count / 2;
        var length3 = threeHopPaths.Count / 2;
        var selectedPaths = oneHopPaths.Take(length1)
            .Concat(twoHopPaths.Take(length2))
            .Concat(threeHopPaths.Take(length3))
            .ToList();

        var totalEdges = Math.Min(15, selectedPaths.Count);
        var sortResult = resultEdges.Take(totalEdges).Select(r => r.Path).ToList();

        // Build natural language relationship descriptions (matching Python)
        var relationships = new List<string>();
        foreach (var path in sortResult)
        {
            var description = await BuildPathDescriptionAsync(path, cancellationToken);
            if (!string.IsNullOrEmpty(description))
                relationships.Add(description);
        }

        // Truncate by token size and reverse (matching Python)
        relationships = TruncateByTokenSize(relationships, r => r, queryParams.MaxTokenForLocalContext);
        relationships.Reverse();

        return relationships;
    }

    /// <summary>
    /// Build natural language description for a path (matching Python's path description logic)
    /// </summary>
    private async Task<string> BuildPathDescriptionAsync(List<string> path, CancellationToken cancellationToken)
    {
        if (path.Count == 2)
        {
            // 1-hop path
            var edge = await GetEdgeAsync(path[0], path[1], cancellationToken);
            if (edge == null) return "";

            var s = await _graphRepository.GetEntityByNameAsync(path[0], cancellationToken: cancellationToken);
            var t = await _graphRepository.GetEntityByNameAsync(path[1], cancellationToken: cancellationToken);
            if (s == null || t == null) return "";

            var sDesc = $"The entity {path[0]} is a {s.EntityType} with the description({s.Description})";
            var tDesc = $"The entity {path[1]} is a {t.EntityType} with the description({t.Description})";
            var eDesc = $"through edge({edge.Keywords}) to connect to {path[0]} and {path[1]}.";

            return $"{sDesc}{eDesc}{tDesc}";
        }
        else if (path.Count == 3)
        {
            // 2-hop path
            var edge0 = await GetEdgeAsync(path[0], path[1], cancellationToken);
            var edge1 = await GetEdgeAsync(path[1], path[2], cancellationToken);
            if (edge0 == null || edge1 == null) return "";

            var s = await _graphRepository.GetEntityByNameAsync(path[0], cancellationToken: cancellationToken);
            var b = await _graphRepository.GetEntityByNameAsync(path[1], cancellationToken: cancellationToken);
            var t = await _graphRepository.GetEntityByNameAsync(path[2], cancellationToken: cancellationToken);
            if (s == null || b == null || t == null) return "";

            var sDesc = $"The entity {path[0]} is a {s.EntityType} with the description({s.Description})";
            var bDesc = $"The entity {path[1]} is a {b.EntityType} with the description({b.Description})";
            var tDesc = $"The entity {path[2]} is a {t.EntityType} with the description({t.Description})";
            var e1Desc = $"through edge({edge0.Keywords}) to connect to {path[0]} and {path[1]}.";
            var e2Desc = $"through edge({edge1.Keywords}) to connect to {path[1]} and {path[2]}.";

            return $"{sDesc}{e1Desc}{bDesc}and{bDesc}{e2Desc}{tDesc}";
        }
        else if (path.Count == 4)
        {
            // 3-hop path
            var edge0 = await GetEdgeAsync(path[0], path[1], cancellationToken);
            var edge1 = await GetEdgeAsync(path[1], path[2], cancellationToken);
            var edge2 = await GetEdgeAsync(path[2], path[3], cancellationToken);
            if (edge0 == null || edge1 == null || edge2 == null) return "";

            var s = await _graphRepository.GetEntityByNameAsync(path[0], cancellationToken: cancellationToken);
            var b1 = await _graphRepository.GetEntityByNameAsync(path[1], cancellationToken: cancellationToken);
            var b2 = await _graphRepository.GetEntityByNameAsync(path[2], cancellationToken: cancellationToken);
            var t = await _graphRepository.GetEntityByNameAsync(path[3], cancellationToken: cancellationToken);
            if (s == null || b1 == null || b2 == null || t == null) return "";

            var sDesc = $"The entity {path[0]} is a {s.EntityType} with the description({s.Description})";
            var b1Desc = $"The entity {path[1]} is a {b1.EntityType} with the description({b1.Description})";
            var b2Desc = $"The entity {path[2]} is a {b2.EntityType} with the description({b2.Description})";
            var tDesc = $"The entity {path[3]} is a {t.EntityType} with the description({t.Description})";
            var e1Desc = $"through edge ({edge0.Keywords}) to connect to {path[0]} and {path[1]}.";
            var e2Desc = $"through edge ({edge1.Keywords}) to connect to {path[1]} and {path[2]}.";
            var e3Desc = $"through edge ({edge2.Keywords}) to connect to {path[2]} and {path[3]}.";

            return $"{sDesc}{e1Desc}{b1Desc}and{b1Desc}{e2Desc}{b2Desc}and{b2Desc}{e3Desc}{tDesc}";
        }

        return "";
    }

    /// <summary>
    /// Get edge in either direction (matching Python's get_edge fallback logic)
    /// </summary>
    private async Task<GraphRelationship?> GetEdgeAsync(string src, string tgt, CancellationToken cancellationToken)
    {
        var edge = await _graphRepository.GetRelationshipAsync(src, tgt, cancellationToken: cancellationToken);
        edge ??= await _graphRepository.GetRelationshipAsync(tgt, src, cancellationToken: cancellationToken);
        return edge;
    }

    /// <summary>
    /// Find paths and edges with stats (matching Python's find_paths_and_edges_with_stats)
    /// </summary>
    private static (Dictionary<(string, string), PathData> Result, List<List<string>> OneHop, List<List<string>> TwoHop, List<List<string>> ThreeHop)
        FindPathsAndEdgesWithStats(Dictionary<string, HashSet<string>> adjacency, List<string> targetNodes)
    {
        var result = new Dictionary<(string, string), PathData>();
        var oneHopPaths = new List<List<string>>();
        var twoHopPaths = new List<List<string>>();
        var threeHopPaths = new List<List<string>>();

        void Dfs(string current, string target, List<string> path, int depth)
        {
            if (depth > 3) return;
            if (current == target)
            {
                var key = (path[0], target);
                if (!result.ContainsKey(key))
                    result[key] = new PathData();
                result[key].Paths.Add(new List<string>(path));

                for (int i = 0; i < path.Count - 1; i++)
                {
                    var edge = (path[i], path[i + 1]);
                    var sortedEdge = string.Compare(edge.Item1, edge.Item2) < 0 ? edge : (edge.Item2, edge.Item1);
                    result[key].Edges.Add(sortedEdge);
                }

                if (depth == 1) oneHopPaths.Add(new List<string>(path));
                else if (depth == 2) twoHopPaths.Add(new List<string>(path));
                else if (depth == 3) threeHopPaths.Add(new List<string>(path));
                return;
            }

            if (!adjacency.TryGetValue(current, out var neighbors)) return;
            foreach (var neighbor in neighbors)
            {
                if (!path.Contains(neighbor))
                {
                    path.Add(neighbor);
                    Dfs(neighbor, target, path, depth + 1);
                    path.RemoveAt(path.Count - 1);
                }
            }
        }

        foreach (var node1 in targetNodes)
        {
            foreach (var node2 in targetNodes)
            {
                if (node1 != node2)
                {
                    Dfs(node1, node2, [node1], 0);
                }
            }
        }

        return (result, oneHopPaths, twoHopPaths, threeHopPaths);
    }

    /// <summary>
    /// BFS weighted paths (matching Python's bfs_weighted_paths)
    /// </summary>
    private static List<(List<string> Path, double Weight)> BfsWeightedPaths(
        List<List<string>> paths, string source, string target, double threshold, double alpha)
    {
        var results = new List<(List<string> Path, double Weight)>();
        var edgeWeights = new Dictionary<(string, string), double>();
        var followDict = new Dictionary<string, HashSet<string>>();

        // Build follow dictionary
        foreach (var p in paths)
        {
            for (int i = 0; i < p.Count - 1; i++)
            {
                var current = p[i];
                var next = p[i + 1];
                if (!followDict.ContainsKey(current))
                    followDict[current] = [];
                followDict[current].Add(next);
            }
        }

        if (!followDict.TryGetValue(source, out var sourceNeighbors)) return results;

        foreach (var neighbor in sourceNeighbors)
        {
            var key = (source, neighbor);
            edgeWeights[key] = edgeWeights.GetValueOrDefault(key, 0) + 1.0 / sourceNeighbors.Count;

            if (neighbor == target)
            {
                results.Add(([source, neighbor], edgeWeights[key]));
                continue;
            }

            if (edgeWeights[key] > threshold && followDict.TryGetValue(neighbor, out var secondNeighbors))
            {
                foreach (var secondNeighbor in secondNeighbors)
                {
                    var weight = edgeWeights[key] * alpha / secondNeighbors.Count;
                    var key2 = (neighbor, secondNeighbor);
                    edgeWeights[key2] = edgeWeights.GetValueOrDefault(key2, 0) + weight;

                    if (secondNeighbor == target)
                    {
                        results.Add(([source, neighbor, secondNeighbor], edgeWeights[key2]));
                        continue;
                    }

                    if (edgeWeights[key2] > threshold && followDict.TryGetValue(secondNeighbor, out var thirdNeighbors))
                    {
                        foreach (var thirdNeighbor in thirdNeighbors)
                        {
                            var weight3 = edgeWeights[key2] * alpha / thirdNeighbors.Count;
                            var key3 = (secondNeighbor, thirdNeighbor);
                            edgeWeights[key3] = edgeWeights.GetValueOrDefault(key3, 0) + weight3;

                            if (thirdNeighbor == target)
                            {
                                results.Add(([source, neighbor, secondNeighbor, thirdNeighbor], edgeWeights[key3]));
                            }
                        }
                    }
                }
            }
        }

        // Calculate path weights
        var pathWeights = new List<double>();
        foreach (var p in paths)
        {
            double pathWeight = 0;
            for (int i = 0; i < p.Count - 1; i++)
            {
                var edge = (p[i], p[i + 1]);
                pathWeight += edgeWeights.GetValueOrDefault(edge, 0);
            }
            pathWeights.Add(pathWeight / (p.Count - 1));
        }

        return paths.Zip(pathWeights, (p, w) => (p, w)).ToList();
    }

    /// <summary>
    /// Find most related text units from entities (matching Python's _find_most_related_text_unit_from_entities)
    /// </summary>
    private async Task<List<TextUnitData>> FindMostRelatedTextUnitsFromEntitiesAsync(
        List<NodeData> nodeDatas, QueryParamDto queryParams, CancellationToken cancellationToken)
    {
        var textUnits = new List<TextUnitData>();
        var seenChunkIds = new HashSet<Guid>();

        foreach (var node in nodeDatas)
        {
            if (string.IsNullOrEmpty(node.SourceId)) continue;

            // Parse source IDs (separated by GRAPH_FIELD_SEP)
            var sourceIds = node.SourceId.Split(GraphFieldSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sourceId in sourceIds)
            {
                if (Guid.TryParse(sourceId.Trim(), out var chunkId) && !seenChunkIds.Contains(chunkId))
                {
                    var chunk = await _chunkRepository.GetByIdAsync(chunkId, cancellationToken);
                    if (chunk != null)
                    {
                        seenChunkIds.Add(chunkId);
                        textUnits.Add(new TextUnitData
                        {
                            Id = chunkId.ToString(),
                            Content = chunk.Content
                        });
                    }
                }
            }
        }

        return TruncateByTokenSize(textUnits, t => t.Content, queryParams.MaxTokenForTextUnit);
    }

    /// <summary>
    /// Find most related entities from relationships (matching Python's _find_most_related_entities_from_relationships)
    /// Preserves order: src_id first, then tgt_id (matching Python)
    /// </summary>
    private async Task<List<NodeData>> FindMostRelatedEntitiesFromRelationshipsAsync(
        List<EdgeData> edgeDatas, QueryParamDto queryParams, CancellationToken cancellationToken)
    {
        // Preserve order like Python: src_id first, then tgt_id
        var entityNames = new List<string>();
        var seen = new HashSet<string>();
        foreach (var edge in edgeDatas)
        {
            if (!seen.Contains(edge.SrcId))
            {
                entityNames.Add(edge.SrcId);
                seen.Add(edge.SrcId);
            }
            if (!seen.Contains(edge.TgtId))
            {
                entityNames.Add(edge.TgtId);
                seen.Add(edge.TgtId);
            }
        }

        var nodeDatas = new List<NodeData>();
        foreach (var name in entityNames)
        {
            var node = await _graphRepository.GetEntityByNameAsync(name, cancellationToken: cancellationToken);
            if (node == null) continue;

            var neighbors = await _graphRepository.GetNeighborsAsync(name, 1, cancellationToken);
            nodeDatas.Add(new NodeData
            {
                EntityName = name,
                EntityType = node.EntityType,
                Description = node.Description ?? "UNKNOWN",
                SourceId = node.SourceId ?? string.Empty,
                Rank = neighbors.Count()
            });
        }

        // Python doesn't sort here, just truncates by token size
        return TruncateByTokenSize(nodeDatas, n => n.Description, queryParams.MaxTokenForLocalContext);
    }

    /// <summary>
    /// Find related text units from relationships
    /// </summary>
    private async Task<List<TextUnitData>> FindRelatedTextUnitsFromRelationshipsAsync(
        List<EdgeData> edgeDatas, QueryParamDto queryParams, CancellationToken cancellationToken)
    {
        var textUnits = new List<TextUnitData>();
        var seenChunkIds = new HashSet<Guid>();

        // Get source IDs from related entities
        var entityNames = new HashSet<string>();
        foreach (var edge in edgeDatas)
        {
            entityNames.Add(edge.SrcId);
            entityNames.Add(edge.TgtId);
        }

        foreach (var name in entityNames)
        {
            var node = await _graphRepository.GetEntityByNameAsync(name, cancellationToken: cancellationToken);
            if (node == null || string.IsNullOrEmpty(node.SourceId)) continue;

            var sourceIds = node.SourceId.Split(GraphFieldSep, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sourceId in sourceIds)
            {
                if (Guid.TryParse(sourceId.Trim(), out var chunkId) && !seenChunkIds.Contains(chunkId))
                {
                    var chunk = await _chunkRepository.GetByIdAsync(chunkId, cancellationToken);
                    if (chunk != null)
                    {
                        seenChunkIds.Add(chunkId);
                        textUnits.Add(new TextUnitData
                        {
                            Id = chunkId.ToString(),
                            Content = chunk.Content
                        });
                    }
                }
            }
        }

        return TruncateByTokenSize(textUnits, t => t.Content, queryParams.MaxTokenForTextUnit);
    }

    /// <summary>
    /// Combine text units from high-level and low-level contexts (matching Python's combine_contexts)
    /// </summary>
    private static string CombineTextUnits(string hlTextUnits, string llTextUnits)
    {
        var combined = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(hlTextUnits))
            combined.AppendLine(hlTextUnits);
        if (!string.IsNullOrWhiteSpace(llTextUnits))
            combined.AppendLine(llTextUnits);
        return combined.ToString();
    }

    /// <summary>
    /// Truncate list by token size (matching Python's truncate_list_by_token_size)
    /// </summary>
    private static List<T> TruncateByTokenSize<T>(List<T> items, Func<T, string> contentSelector, int maxTokens)
    {
        var result = new List<T>();
        var totalTokens = 0;

        foreach (var item in items)
        {
            var content = contentSelector(item);
            var tokens = content.Length / 4; // Approximate token count
            if (totalTokens + tokens > maxTokens) break;
            totalTokens += tokens;
            result.Add(item);
        }

        return result;
    }

    #region CSV Building Methods (matching Python's list_of_list_to_csv)

    /// <summary>
    /// Build entities CSV (matching Python's format exactly)
    /// Python: [["id", "entity", "type", "description", "rank"]]
    /// </summary>
    private static string BuildEntitiesCsv(IEnumerable<NodeData> nodes)
    {
        var rows = new List<List<object>> { new() { "id", "entity", "type", "description", "rank" } };
        var i = 0;
        foreach (var node in nodes)
        {
            rows.Add([i++, node.EntityName, node.EntityType, node.Description, node.Rank]);
        }
        return ListOfListToCsv(rows);
    }

    /// <summary>
    /// Build relations CSV (matching Python's format for low-level relations - natural language paths)
    /// Python: [["id", "context"]]
    /// </summary>
    private static string BuildRelationsCsv(IEnumerable<string> relations)
    {
        var rows = new List<List<object>> { new() { "id", "context" } };
        var i = 0;
        foreach (var rel in relations)
        {
            rows.Add([i++, rel]);
        }
        return ListOfListToCsv(rows);
    }

    /// <summary>
    /// Build edges CSV (matching Python's format for high-level edges)
    /// Python: [["id", "source", "target", "description", "keywords", "weight", "rank"]]
    /// </summary>
    private static string BuildEdgesCsv(IEnumerable<EdgeData> edges)
    {
        var rows = new List<List<object>> { new() { "id", "source", "target", "description", "keywords", "weight", "rank" } };
        var i = 0;
        foreach (var edge in edges)
        {
            rows.Add([i++, edge.SrcId, edge.TgtId, edge.Description, edge.Keywords, edge.Weight, edge.Rank]);
        }
        return ListOfListToCsv(rows);
    }

    /// <summary>
    /// Build text units CSV (matching Python's format)
    /// Python: [["id", "content"]]
    /// </summary>
    private static string BuildTextUnitsCsv(IEnumerable<TextUnitData> textUnits)
    {
        var rows = new List<List<object>> { new() { "id", "content" } };
        var i = 0;
        foreach (var unit in textUnits)
        {
            rows.Add([i++, unit.Content]);
        }
        return ListOfListToCsv(rows);
    }

    /// <summary>
    /// Convert list of lists to CSV string (matching Python's list_of_list_to_csv)
    /// </summary>
    private static string ListOfListToCsv(List<List<object>> data)
    {
        var sb = new StringBuilder();
        foreach (var row in data)
        {
            var escapedValues = row.Select(v => EscapeCsvValue(v?.ToString() ?? ""));
            sb.AppendLine(string.Join(",", escapedValues));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Escape CSV value (matching Python's csv.writer behavior)
    /// </summary>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    #endregion

    #region GetQueryGraphAsync (for visualization)

    public async Task<KnowledgeGraphDto> GetQueryGraphAsync(
        string query,
        int topK = 40,
        IEnumerable<Guid>? documentTypeIds = null,
        Guid? logId = null,
        CancellationToken cancellationToken = default)
    {
        // If no logId provided, create our own operation log
        var isStandaloneQuery = !logId.HasValue;
        if (isStandaloneQuery)
        {
            logId = await _logger.StartOperationAsync("GetKnowledgeGraph", metadata: $"{{\"query\":\"{query.Replace("\"", "\\\"")}\"}}",
                cancellationToken: cancellationToken);
        }

        Guid stageLogId;
        var documentIds = await ResolveDocumentIdsAsync(documentTypeIds, cancellationToken);

        try
        {
            // Stage 1: Initialize graph query
            stageLogId = await _logger.StartStageAsync(logId!.Value, "GRAPH_START", message: $"Starting graph query: {query}", cancellationToken: cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, cancellationToken: cancellationToken);

            // Generate embedding
            var embeddingResult = await _embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken);
            var embeddingArray = embeddingResult[0].Vector.ToArray();

            // Stage 2: Load entities
            stageLogId = await _logger.StartStageAsync(logId.Value, "GRAPH_LOAD_ENTITIES", message: "Searching entity vectors", cancellationToken: cancellationToken);
            var entityVectors = await _graphVectorRepository.SearchEntitiesByVectorAsync(embeddingArray, topK, documentIds, cancellationToken);
            var entityNames = entityVectors.Select(e => e.EntityName).ToList();
            var entities = await _graphRepository.GetEntitiesByNamesAsync(entityNames, documentIds, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: entities.Count(), details: $"Found {entities.Count()} entities", cancellationToken: cancellationToken);

            // Stage 3: Load relationships
            stageLogId = await _logger.StartStageAsync(logId.Value, "GRAPH_LOAD_RELS", message: "Searching relationship vectors", cancellationToken: cancellationToken);
            var relationshipVectors = await _graphVectorRepository.SearchRelationshipsByVectorAsync(embeddingArray, topK, documentIds, cancellationToken);
            var allRelationships = new List<GraphRelationship>();
            foreach (var rv in relationshipVectors)
            {
                var rel = await _graphRepository.GetRelationshipAsync(rv.SourceEntityName, rv.TargetEntityName, rv.DocumentId, cancellationToken: cancellationToken);
                if (rel != null) allRelationships.Add(rel);
            }

            // Also get path relationships
            foreach (var entity in entities.Take(10))
            {
                var paths = await _graphRepository.GetOneHopPathsAsync(entity.EntityName, documentIds, cancellationToken);
                allRelationships.AddRange(paths.Select(p => p.Edge));
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: allRelationships.Count, details: $"Found {allRelationships.Count} relationships", cancellationToken: cancellationToken);

            // Stage 4: Build graph response
            stageLogId = await _logger.StartStageAsync(logId.Value, "GRAPH_BUILD", message: "Building graph response", cancellationToken: cancellationToken);
            var nodes = entities.Select(e => new GraphNodeDto
            {
                Id = e.EntityName,
                Label = e.EntityName,
                Type = e.EntityType,
                Description = e.Description
            });
            var edges = allRelationships.DistinctBy(r => new { r.SourceEntityName, r.TargetEntityName }).Select(r => new GraphEdgeDto
            {
                Id = $"{r.SourceEntityName}->{r.TargetEntityName}",
                Source = r.SourceEntityName,
                Target = r.TargetEntityName,
                Label = r.Keywords,
                Weight = r.Weight
            });
            await _logger.CompleteStageAsync(stageLogId, details: $"Built {nodes.Count()} nodes, {edges.Count()} edges", cancellationToken: cancellationToken);

            // Stage 5: Complete
            stageLogId = await _logger.StartStageAsync(logId.Value, "GRAPH_COMPLETE", message: "Graph query completed", cancellationToken: cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, cancellationToken: cancellationToken);

            if (isStandaloneQuery)
            {
                await _logger.CompleteOperationAsync(logId.Value, cancellationToken);
            }

            return new KnowledgeGraphDto { Nodes = nodes, Edges = edges };
        }
        catch (Exception ex)
        {
            if (isStandaloneQuery)
            {
                await _logger.FailOperationAsync(logId!.Value, ex.Message, cancellationToken);
            }
            throw;
        }
    }

    #endregion

    #region Data Classes

    private class NodeData
    {
        public string EntityName { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string Description { get; set; } = "";
        public string SourceId { get; set; } = "";
        public int Rank { get; set; }
    }

    private class EdgeData
    {
        public string SrcId { get; set; } = "";
        public string TgtId { get; set; } = "";
        public string Description { get; set; } = "";
        public string Keywords { get; set; } = "";
        public double Weight { get; set; }
        public int Rank { get; set; }
    }

    private class TextUnitData
    {
        public string Id { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private class PathData
    {
        public List<List<string>> Paths { get; set; } = [];
        public HashSet<(string, string)> Edges { get; set; } = [];
    }

    #endregion
}
