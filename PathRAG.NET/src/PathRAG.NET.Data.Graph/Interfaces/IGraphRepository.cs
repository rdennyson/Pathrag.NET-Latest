using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Graph.Interfaces;

/// <summary>
/// Interface for knowledge graph repository operations (Node/Edge tables)
/// Implementations: SQL Server Graph, Neo4j, etc.
/// </summary>
public interface IGraphRepository
{
    // Entity (Node) Operations
    Task<GraphEntity?> GetEntityByNameAsync(string entityName, Guid? documentId = null, CancellationToken cancellationToken = default);
    Task<GraphEntity?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GraphEntity>> GetAllEntitiesAsync(int limit = 100, IEnumerable<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<GraphEntity>> GetEntitiesByNamesAsync(IEnumerable<string> entityNames, IEnumerable<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    Task<GraphEntity> UpsertEntityAsync(GraphEntity entity, CancellationToken cancellationToken = default);
    Task<IEnumerable<GraphEntity>> UpsertEntitiesAsync(IEnumerable<GraphEntity> entities, CancellationToken cancellationToken = default);
    Task<bool> EntityExistsAsync(string entityName, Guid? documentId = null, CancellationToken cancellationToken = default);
    Task DeleteEntityAsync(string entityName, CancellationToken cancellationToken = default);
    
    // Relationship (Edge) Operations
    Task<GraphRelationship?> GetRelationshipAsync(string sourceEntity, string targetEntity, Guid? documentId = null, CancellationToken cancellationToken = default);
    Task<GraphRelationship?> GetRelationshipByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GraphRelationship>> GetAllRelationshipsAsync(int limit = 100, IEnumerable<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<GraphRelationship>> GetRelationshipsByEntityAsync(string entityName, CancellationToken cancellationToken = default);
    Task<GraphRelationship> UpsertRelationshipAsync(GraphRelationship relationship, CancellationToken cancellationToken = default);
    Task<IEnumerable<GraphRelationship>> UpsertRelationshipsAsync(IEnumerable<GraphRelationship> relationships, CancellationToken cancellationToken = default);
    Task DeleteRelationshipAsync(string sourceEntity, string targetEntity, CancellationToken cancellationToken = default);
    
    // Graph Traversal Operations (PathRAG specific)
    Task<IEnumerable<GraphEntity>> GetNeighborsAsync(string entityName, int maxHops = 1, CancellationToken cancellationToken = default);
    Task<IEnumerable<(GraphEntity Source, GraphRelationship Edge, GraphEntity Target)>> GetOneHopPathsAsync(string entityName, IEnumerable<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<(List<GraphEntity> Nodes, List<GraphRelationship> Edges)>> GetMultiHopPathsAsync(string sourceEntity, int maxHops = 3, IEnumerable<Guid>? documentIds = null, CancellationToken cancellationToken = default);
    
    // Database Initialization
    Task InitializeDatabaseAsync(CancellationToken cancellationToken = default);
    Task<bool> IsDatabaseInitializedAsync(CancellationToken cancellationToken = default);
}

