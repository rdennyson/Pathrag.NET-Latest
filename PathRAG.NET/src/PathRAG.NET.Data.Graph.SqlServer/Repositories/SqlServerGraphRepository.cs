using Dapper;
using Microsoft.Data.SqlClient;
using PathRAG.NET.Data.Graph;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Graph.SqlServer.Repositories;

/// <summary>
/// SQL Server Graph Tables implementation using Dapper
/// Uses CREATE TABLE AS NODE and CREATE TABLE AS EDGE with MATCH queries
/// </summary>
public class SqlServerGraphRepository : IGraphRepository
{
    private readonly string _connectionString;
    private readonly int _embeddingDimensions;
    private readonly string _schemaName;

    // Schema-qualified table names
    private string GraphEntities => $"[{_schemaName}].[GraphEntities]";
    private string GraphRelationships => $"[{_schemaName}].[GraphRelationships]";

    public SqlServerGraphRepository(GraphSettings settings)
    {
        _connectionString = settings.ConnectionString ?? throw new ArgumentNullException(nameof(settings.ConnectionString));
        _embeddingDimensions = settings.EmbeddingDimensions;
        _schemaName = settings.SchemaName ?? "PathRAG";
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    #region Entity (Node) Operations

    public async Task<GraphEntity?> GetEntityByNameAsync(string entityName, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT
                CAST($node_id AS NVARCHAR(1000)) AS NodeId,
                Id, EntityName, EntityType, Description, SourceId, CreatedAt
            FROM {GraphEntities}
            WHERE EntityName = @EntityName";

        var result = await connection.QueryFirstOrDefaultAsync<GraphEntityDto>(sql, new { EntityName = entityName.ToUpperInvariant() });
        return result?.ToEntity();
    }

    public async Task<GraphEntity?> GetEntityByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT
                CAST($node_id AS NVARCHAR(1000)) AS NodeId,
                Id, EntityName, EntityType, Description, SourceId, CreatedAt
            FROM {GraphEntities}
            WHERE Id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<GraphEntityDto>(sql, new { Id = id });
        return result?.ToEntity();
    }

    public async Task<IEnumerable<GraphEntity>> GetAllEntitiesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT TOP (@Limit)
                CAST($node_id AS NVARCHAR(1000)) AS NodeId,
                Id, EntityName, EntityType, Description, SourceId, CreatedAt
            FROM {GraphEntities}
            ORDER BY CreatedAt DESC";

        var results = await connection.QueryAsync<GraphEntityDto>(sql, new { Limit = limit });
        return results.Select(r => r.ToEntity());
    }

    public async Task<IEnumerable<GraphEntity>> GetEntitiesByNamesAsync(IEnumerable<string> entityNames, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var normalizedNames = entityNames.Select(n => n.ToUpperInvariant()).ToList();
        var sql = $@"
            SELECT
                CAST($node_id AS NVARCHAR(1000)) AS NodeId,
                Id, EntityName, EntityType, Description, SourceId, CreatedAt
            FROM {GraphEntities}
            WHERE EntityName IN @EntityNames";

        var results = await connection.QueryAsync<GraphEntityDto>(sql, new { EntityNames = normalizedNames });
        return results.Select(r => r.ToEntity());
    }

    public async Task<GraphEntity> UpsertEntityAsync(GraphEntity entity, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        entity.EntityName = entity.EntityName.ToUpperInvariant();
        entity.CreatedAt = entity.CreatedAt == default ? DateTimeOffset.UtcNow : entity.CreatedAt;

        var sql = $@"
            MERGE {GraphEntities} AS target
            USING (SELECT @Id AS Id) AS source
            ON target.EntityName = @EntityName
            WHEN MATCHED THEN
                UPDATE SET
                    EntityType = @EntityType,
                    Description = CASE WHEN @Description IS NOT NULL THEN @Description ELSE target.Description END,
                    SourceId = CASE WHEN target.SourceId IS NULL THEN @SourceId
                               ELSE target.SourceId + '<SEP>' + @SourceId END
            WHEN NOT MATCHED THEN
                INSERT (Id, EntityName, EntityType, Description, SourceId, CreatedAt)
                VALUES (@Id, @EntityName, @EntityType, @Description, @SourceId, @CreatedAt);

            SELECT
                CAST($node_id AS NVARCHAR(1000)) AS NodeId,
                Id, EntityName, EntityType, Description, SourceId, CreatedAt
            FROM {GraphEntities} WHERE EntityName = @EntityName;";

        var result = await connection.QueryFirstOrDefaultAsync<GraphEntityDto>(sql, new
        {
            entity.Id,
            entity.EntityName,
            entity.EntityType,
            entity.Description,
            entity.SourceId,
            entity.CreatedAt
        });

        return result?.ToEntity() ?? entity;
    }

    public async Task<IEnumerable<GraphEntity>> UpsertEntitiesAsync(IEnumerable<GraphEntity> entities, CancellationToken cancellationToken = default)
    {
        var results = new List<GraphEntity>();
        foreach (var entity in entities)
        {
            var result = await UpsertEntityAsync(entity, cancellationToken);
            results.Add(result);
        }
        return results;
    }

    public async Task<bool> EntityExistsAsync(string entityName, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $"SELECT COUNT(1) FROM {GraphEntities} WHERE EntityName = @EntityName";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EntityName = entityName.ToUpperInvariant() });
        return count > 0;
    }

    public async Task DeleteEntityAsync(string entityName, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $"DELETE FROM {GraphEntities} WHERE EntityName = @EntityName";
        await connection.ExecuteAsync(sql, new { EntityName = entityName.ToUpperInvariant() });
    }

    #endregion

    #region Relationship (Edge) Operations

    public async Task<GraphRelationship?> GetRelationshipAsync(string sourceEntity, string targetEntity, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT
                CAST(r.$edge_id AS NVARCHAR(1000)) AS EdgeId,
                r.Id, r.SourceEntityName, r.TargetEntityName, r.Weight,
                r.Description, r.Keywords, r.SourceId, r.CreatedAt
            FROM {GraphRelationships} r, {GraphEntities} src, {GraphEntities} tgt
            WHERE MATCH(src-(r)->tgt)
            AND src.EntityName = @SourceEntity
            AND tgt.EntityName = @TargetEntity";

        var result = await connection.QueryFirstOrDefaultAsync<GraphRelationshipDto>(sql, new
        {
            SourceEntity = sourceEntity.ToUpperInvariant(),
            TargetEntity = targetEntity.ToUpperInvariant()
        });
        return result?.ToEntity();
    }

    public async Task<GraphRelationship?> GetRelationshipByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT
                CAST($edge_id AS NVARCHAR(1000)) AS EdgeId,
                Id, SourceEntityName, TargetEntityName, Weight,
                Description, Keywords, SourceId, CreatedAt
            FROM {GraphRelationships}
            WHERE Id = @Id";

        var result = await connection.QueryFirstOrDefaultAsync<GraphRelationshipDto>(sql, new { Id = id });
        return result?.ToEntity();
    }

    public async Task<IEnumerable<GraphRelationship>> GetAllRelationshipsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT TOP (@Limit)
                CAST($edge_id AS NVARCHAR(1000)) AS EdgeId,
                Id, SourceEntityName, TargetEntityName, Weight,
                Description, Keywords, SourceId, CreatedAt
            FROM {GraphRelationships}
            ORDER BY CreatedAt DESC";

        var results = await connection.QueryAsync<GraphRelationshipDto>(sql, new { Limit = limit });
        return results.Select(r => r.ToEntity());
    }

    public async Task<IEnumerable<GraphRelationship>> GetRelationshipsByEntityAsync(string entityName, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT
                CAST(r.$edge_id AS NVARCHAR(1000)) AS EdgeId,
                r.Id, r.SourceEntityName, r.TargetEntityName, r.Weight,
                r.Description, r.Keywords, r.SourceId, r.CreatedAt
            FROM {GraphRelationships} r, {GraphEntities} src, {GraphEntities} tgt
            WHERE MATCH(src-(r)->tgt)
            AND (src.EntityName = @EntityName OR tgt.EntityName = @EntityName)";

        var results = await connection.QueryAsync<GraphRelationshipDto>(sql, new { EntityName = entityName.ToUpperInvariant() });
        return results.Select(r => r.ToEntity());
    }

    public async Task<GraphRelationship> UpsertRelationshipAsync(GraphRelationship relationship, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        relationship.SourceEntityName = relationship.SourceEntityName.ToUpperInvariant();
        relationship.TargetEntityName = relationship.TargetEntityName.ToUpperInvariant();
        relationship.CreatedAt = relationship.CreatedAt == default ? DateTimeOffset.UtcNow : relationship.CreatedAt;

        // Check if relationship exists
        var checkSql = $@"
            SELECT COUNT(1) FROM {GraphRelationships} r, {GraphEntities} src, {GraphEntities} tgt
            WHERE MATCH(src-(r)->tgt)
            AND src.EntityName = @SourceEntityName AND tgt.EntityName = @TargetEntityName";

        var exists = await connection.ExecuteScalarAsync<int>(checkSql, new
        {
            relationship.SourceEntityName,
            relationship.TargetEntityName
        }) > 0;

        if (exists)
        {
            var updateSql = $@"
                UPDATE r SET
                    Weight = @Weight,
                    Description = CASE WHEN @Description IS NOT NULL THEN @Description ELSE r.Description END,
                    Keywords = CASE WHEN @Keywords IS NOT NULL THEN @Keywords ELSE r.Keywords END,
                    SourceId = CASE WHEN r.SourceId IS NULL THEN @SourceId
                               ELSE r.SourceId + '<SEP>' + @SourceId END
                FROM {GraphRelationships} r, {GraphEntities} src, {GraphEntities} tgt
                WHERE MATCH(src-(r)->tgt)
                AND src.EntityName = @SourceEntityName AND tgt.EntityName = @TargetEntityName";

            await connection.ExecuteAsync(updateSql, new
            {
                relationship.SourceEntityName,
                relationship.TargetEntityName,
                relationship.Weight,
                relationship.Description,
                relationship.Keywords,
                relationship.SourceId
            });
        }
        else
        {
            var insertSql = $@"
                INSERT INTO {GraphRelationships}
                    ($from_id, $to_id, Id, SourceEntityName, TargetEntityName, Weight, Description, Keywords, SourceId, CreatedAt)
                SELECT
                    src.$node_id, tgt.$node_id, @Id, @SourceEntityName, @TargetEntityName, @Weight, @Description, @Keywords, @SourceId, @CreatedAt
                FROM {GraphEntities} src, {GraphEntities} tgt
                WHERE src.EntityName = @SourceEntityName AND tgt.EntityName = @TargetEntityName";

            await connection.ExecuteAsync(insertSql, new
            {
                relationship.Id,
                relationship.SourceEntityName,
                relationship.TargetEntityName,
                relationship.Weight,
                relationship.Description,
                relationship.Keywords,
                relationship.SourceId,
                relationship.CreatedAt
            });
        }

        return (await GetRelationshipAsync(relationship.SourceEntityName, relationship.TargetEntityName, cancellationToken))!;
    }

    public async Task<IEnumerable<GraphRelationship>> UpsertRelationshipsAsync(IEnumerable<GraphRelationship> relationships, CancellationToken cancellationToken = default)
    {
        var results = new List<GraphRelationship>();
        foreach (var relationship in relationships)
        {
            var result = await UpsertRelationshipAsync(relationship, cancellationToken);
            results.Add(result);
        }
        return results;
    }

    public async Task DeleteRelationshipAsync(string sourceEntity, string targetEntity, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            DELETE r FROM {GraphRelationships} r, {GraphEntities} src, {GraphEntities} tgt
            WHERE MATCH(src-(r)->tgt)
            AND src.EntityName = @SourceEntity AND tgt.EntityName = @TargetEntity";

        await connection.ExecuteAsync(sql, new
        {
            SourceEntity = sourceEntity.ToUpperInvariant(),
            TargetEntity = targetEntity.ToUpperInvariant()
        });
    }

    #endregion

    #region Graph Traversal Operations

    public async Task<IEnumerable<GraphEntity>> GetNeighborsAsync(string entityName, int maxHops = 1, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();

        var sql = maxHops == 1
            ? $@"
                SELECT DISTINCT
                    CAST(tgt.$node_id AS NVARCHAR(1000)) AS NodeId,
                    tgt.Id, tgt.EntityName, tgt.EntityType, tgt.Description, tgt.SourceId, tgt.CreatedAt
                FROM {GraphEntities} src, {GraphRelationships} r, {GraphEntities} tgt
                WHERE MATCH(src-(r)->tgt)
                AND src.EntityName = @EntityName"
            : $@"
                SELECT DISTINCT
                    CAST(LastNode.$node_id AS NVARCHAR(1000)) AS NodeId,
                    LastNode.Id, LastNode.EntityName, LastNode.EntityType,
                    LastNode.Description, LastNode.SourceId, LastNode.CreatedAt
                FROM {GraphEntities} AS src,
                     {GraphRelationships} FOR PATH AS r,
                     {GraphEntities} FOR PATH AS tgt
                WHERE MATCH(SHORTEST_PATH(src(-(r)->tgt)+))
                AND src.EntityName = @EntityName
                AND LEN(tgt.$node_id) - LEN(REPLACE(tgt.$node_id, '/', '')) <= @MaxHops";

        var results = await connection.QueryAsync<GraphEntityDto>(sql, new { EntityName = entityName.ToUpperInvariant(), MaxHops = maxHops });
        return results.Select(r => r.ToEntity());
    }

    public async Task<IEnumerable<(GraphEntity Source, GraphRelationship Edge, GraphEntity Target)>> GetOneHopPathsAsync(string entityName, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"
            SELECT
                CAST(src.$node_id AS NVARCHAR(1000)) AS SrcNodeId,
                src.Id AS SrcId, src.EntityName AS SrcEntityName, src.EntityType AS SrcEntityType,
                src.Description AS SrcDescription, src.SourceId AS SrcSourceId, src.CreatedAt AS SrcCreatedAt,
                CAST(r.$edge_id AS NVARCHAR(1000)) AS EdgeId,
                r.Id AS RelId, r.SourceEntityName, r.TargetEntityName, r.Weight,
                r.Description AS RelDescription, r.Keywords, r.SourceId AS RelSourceId, r.CreatedAt AS RelCreatedAt,
                CAST(tgt.$node_id AS NVARCHAR(1000)) AS TgtNodeId,
                tgt.Id AS TgtId, tgt.EntityName AS TgtEntityName, tgt.EntityType AS TgtEntityType,
                tgt.Description AS TgtDescription, tgt.SourceId AS TgtSourceId, tgt.CreatedAt AS TgtCreatedAt
            FROM {GraphEntities} src, {GraphRelationships} r, {GraphEntities} tgt
            WHERE MATCH(src-(r)->tgt)
            AND (src.EntityName = @EntityName OR tgt.EntityName = @EntityName)";

        var results = await connection.QueryAsync<OneHopPathDto>(sql, new { EntityName = entityName.ToUpperInvariant() });
        return results.Select(r => (r.ToSourceEntity(), r.ToRelationship(), r.ToTargetEntity()));
    }

    public async Task<IEnumerable<(List<GraphEntity> Nodes, List<GraphRelationship> Edges)>> GetMultiHopPathsAsync(string sourceEntity, int maxHops = 3, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();

        var sql = $@"
            SELECT
                STRING_AGG(CAST(tgt.EntityName AS NVARCHAR(MAX)), '->') WITHIN GROUP (GRAPH PATH) AS PathNodes,
                STRING_AGG(CAST(r.Description AS NVARCHAR(MAX)), '|') WITHIN GROUP (GRAPH PATH) AS PathEdgeDescriptions,
                LAST_VALUE(tgt.EntityName) WITHIN GROUP (GRAPH PATH) AS LastNode
            FROM {GraphEntities} AS src,
                 {GraphRelationships} FOR PATH AS r,
                 {GraphEntities} FOR PATH AS tgt
            WHERE MATCH(SHORTEST_PATH(src(-(r)->tgt){{1,3}}))
            AND src.EntityName = @SourceEntity";

        var pathResults = await connection.QueryAsync<PathQueryResult>(sql, new { SourceEntity = sourceEntity.ToUpperInvariant() });

        var result = new List<(List<GraphEntity> Nodes, List<GraphRelationship> Edges)>();
        foreach (var path in pathResults)
        {
            var nodeNames = path.PathNodes?.Split("->") ?? [];
            var nodes = (await GetEntitiesByNamesAsync(nodeNames, cancellationToken)).ToList();

            var edges = new List<GraphRelationship>();
            for (int i = 0; i < nodeNames.Length - 1; i++)
            {
                var edge = await GetRelationshipAsync(nodeNames[i], nodeNames[i + 1], cancellationToken);
                if (edge != null) edges.Add(edge);
            }

            result.Add((nodes, edges));
        }

        return result;
    }

    #endregion

    #region Database Initialization

    public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();

        // Create schema if it doesn't exist
        var createSchemaSql = $@"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{_schemaName}')
            BEGIN
                EXEC('CREATE SCHEMA [{_schemaName}]')
            END";
        await connection.ExecuteAsync(createSchemaSql);

        // Create Graph NODE and EDGE tables without embeddings
        // Embeddings are stored in separate EntityVectors and RelationshipVectors tables
        // (matching Python PathRAG architecture with entities_vdb and relationships_vdb)
        var createTablesSql = $@"
            IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'GraphEntities' AND s.name = '{_schemaName}')
            BEGIN
                CREATE TABLE [{_schemaName}].[GraphEntities] (
                    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    EntityName NVARCHAR(500) NOT NULL,
                    EntityType NVARCHAR(100) NOT NULL,
                    Description NVARCHAR(MAX),
                    SourceId NVARCHAR(MAX),
                    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT PK_GraphEntities PRIMARY KEY (Id),
                    CONSTRAINT UQ_GraphEntities_Name UNIQUE (EntityName)
                ) AS NODE;
            END

            IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'GraphRelationships' AND s.name = '{_schemaName}')
            BEGIN
                CREATE TABLE [{_schemaName}].[GraphRelationships] (
                    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    SourceEntityName NVARCHAR(500) NOT NULL,
                    TargetEntityName NVARCHAR(500) NOT NULL,
                    Weight FLOAT NOT NULL DEFAULT 1.0,
                    Description NVARCHAR(MAX),
                    Keywords NVARCHAR(MAX),
                    SourceId NVARCHAR(MAX),
                    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT PK_GraphRelationships PRIMARY KEY (Id)
                ) AS EDGE;
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GraphEntities_EntityName')
                CREATE INDEX IX_GraphEntities_EntityName ON [{_schemaName}].[GraphEntities](EntityName);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GraphRelationships_Source')
                CREATE INDEX IX_GraphRelationships_Source ON [{_schemaName}].[GraphRelationships](SourceEntityName);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_GraphRelationships_Target')
                CREATE INDEX IX_GraphRelationships_Target ON [{_schemaName}].[GraphRelationships](TargetEntityName);
        ";

        await connection.ExecuteAsync(createTablesSql);
    }

    public async Task<bool> IsDatabaseInitializedAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $@"SELECT COUNT(*) FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
                     WHERE t.name IN ('GraphEntities', 'GraphRelationships') AND s.name = '{_schemaName}'";
        var count = await connection.ExecuteScalarAsync<int>(sql);
        return count == 2;
    }

    #endregion
}
