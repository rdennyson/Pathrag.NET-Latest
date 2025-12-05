using Dapper;
using Microsoft.Data.SqlClient;
using PathRAG.NET.Data.Graph;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Graph.SqlServer.Repositories;

/// <summary>
/// SQL Server Vector Search implementation using separate vector tables
/// Matches Python PathRAG architecture with entities_vdb and relationships_vdb
/// </summary>
public class SqlServerGraphVectorRepository : IGraphVectorRepository
{
    private readonly string _connectionString;
    private readonly int _embeddingDimensions;
    private readonly string _schemaName;

    // Schema-qualified table names
    private string EntityVectors => $"[{_schemaName}].[EntityVectors]";
    private string RelationshipVectors => $"[{_schemaName}].[RelationshipVectors]";

    public SqlServerGraphVectorRepository(GraphSettings settings)
    {
        _connectionString = settings.ConnectionString ?? throw new ArgumentNullException(nameof(settings.ConnectionString));
        _embeddingDimensions = settings.EmbeddingDimensions;
        _schemaName = settings.SchemaName ?? "PathRAG";
    }

    private SqlConnection CreateConnection() => new(_connectionString);

    #region Entity Vector Operations

    public async Task<IEnumerable<EntityVector>> SearchEntitiesByVectorAsync(
        float[] embedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var embeddingJson = $"[{string.Join(",", embedding)}]";

        var sql = $@"
            SELECT TOP (@TopK)
                Id, EntityName, Content, Embedding, CreatedAt,
                VECTOR_DISTANCE('cosine', Embedding, CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions}))) AS Distance
            FROM {EntityVectors}
            ORDER BY VECTOR_DISTANCE('cosine', Embedding, CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions})))";

        var results = await connection.QueryAsync<EntityVectorDto>(sql, new { TopK = topK, EmbeddingJson = embeddingJson });
        return results.Select(r => r.ToEntity());
    }

    public async Task<EntityVector> UpsertEntityVectorAsync(
        EntityVector entityVector,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        entityVector.EntityName = entityVector.EntityName.ToUpperInvariant();
        entityVector.CreatedAt = entityVector.CreatedAt == default ? DateTimeOffset.UtcNow : entityVector.CreatedAt;

        var embeddingJson = $"[{string.Join(",", entityVector.Embedding)}]";

        var sql = $@"
            MERGE {EntityVectors} AS target
            USING (SELECT @EntityName AS EntityName) AS source
            ON target.EntityName = source.EntityName
            WHEN MATCHED THEN
                UPDATE SET
                    Content = @Content,
                    Embedding = CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions}))
            WHEN NOT MATCHED THEN
                INSERT (Id, EntityName, Content, Embedding, CreatedAt)
                VALUES (@Id, @EntityName, @Content, CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions})), @CreatedAt);

            SELECT Id, EntityName, Content, Embedding, CreatedAt
            FROM {EntityVectors} WHERE EntityName = @EntityName;";

        var result = await connection.QueryFirstOrDefaultAsync<EntityVectorDto>(sql, new
        {
            entityVector.Id,
            entityVector.EntityName,
            entityVector.Content,
            EmbeddingJson = embeddingJson,
            entityVector.CreatedAt
        });

        return result?.ToEntity() ?? entityVector;
    }

    public async Task<IEnumerable<EntityVector>> GetAllEntityVectorsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $"SELECT Id, EntityName, Content, Embedding, CreatedAt FROM {EntityVectors}";
        var results = await connection.QueryAsync<EntityVectorDto>(sql);
        return results.Select(r => r.ToEntity());
    }

    public async Task DeleteEntityVectorAsync(string entityName, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $"DELETE FROM {EntityVectors} WHERE EntityName = @EntityName";
        await connection.ExecuteAsync(sql, new { EntityName = entityName.ToUpperInvariant() });
    }

    #endregion

    #region Relationship Vector Operations

    public async Task<IEnumerable<RelationshipVector>> SearchRelationshipsByVectorAsync(
        float[] embedding,
        int topK = 10,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var embeddingJson = $"[{string.Join(",", embedding)}]";

        var sql = $@"
            SELECT TOP (@TopK)
                Id, SourceEntityName, TargetEntityName, Content, Embedding, CreatedAt,
                VECTOR_DISTANCE('cosine', Embedding, CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions}))) AS Distance
            FROM {RelationshipVectors}
            ORDER BY VECTOR_DISTANCE('cosine', Embedding, CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions})))";

        var results = await connection.QueryAsync<RelationshipVectorDto>(sql, new { TopK = topK, EmbeddingJson = embeddingJson });
        return results.Select(r => r.ToEntity());
    }

    public async Task<RelationshipVector> UpsertRelationshipVectorAsync(
        RelationshipVector relationshipVector,
        CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        relationshipVector.SourceEntityName = relationshipVector.SourceEntityName.ToUpperInvariant();
        relationshipVector.TargetEntityName = relationshipVector.TargetEntityName.ToUpperInvariant();
        relationshipVector.CreatedAt = relationshipVector.CreatedAt == default ? DateTimeOffset.UtcNow : relationshipVector.CreatedAt;

        var embeddingJson = $"[{string.Join(",", relationshipVector.Embedding)}]";

        var sql = $@"
            MERGE {RelationshipVectors} AS target
            USING (SELECT @SourceEntityName AS Src, @TargetEntityName AS Tgt) AS source
            ON target.SourceEntityName = source.Src AND target.TargetEntityName = source.Tgt
            WHEN MATCHED THEN
                UPDATE SET
                    Content = @Content,
                    Embedding = CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions}))
            WHEN NOT MATCHED THEN
                INSERT (Id, SourceEntityName, TargetEntityName, Content, Embedding, CreatedAt)
                VALUES (@Id, @SourceEntityName, @TargetEntityName, @Content, CAST(@EmbeddingJson AS VECTOR({_embeddingDimensions})), @CreatedAt);

            SELECT Id, SourceEntityName, TargetEntityName, Content, Embedding, CreatedAt
            FROM {RelationshipVectors} WHERE SourceEntityName = @SourceEntityName AND TargetEntityName = @TargetEntityName;";

        var result = await connection.QueryFirstOrDefaultAsync<RelationshipVectorDto>(sql, new
        {
            relationshipVector.Id,
            relationshipVector.SourceEntityName,
            relationshipVector.TargetEntityName,
            relationshipVector.Content,
            EmbeddingJson = embeddingJson,
            relationshipVector.CreatedAt
        });

        return result?.ToEntity() ?? relationshipVector;
    }

    public async Task<IEnumerable<RelationshipVector>> GetAllRelationshipVectorsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $"SELECT Id, SourceEntityName, TargetEntityName, Content, Embedding, CreatedAt FROM {RelationshipVectors}";
        var results = await connection.QueryAsync<RelationshipVectorDto>(sql);
        return results.Select(r => r.ToEntity());
    }

    public async Task DeleteRelationshipVectorAsync(string sourceEntity, string targetEntity, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = $"DELETE FROM {RelationshipVectors} WHERE SourceEntityName = @SourceEntity AND TargetEntityName = @TargetEntity";
        await connection.ExecuteAsync(sql, new
        {
            SourceEntity = sourceEntity.ToUpperInvariant(),
            TargetEntity = targetEntity.ToUpperInvariant()
        });
    }

    #endregion

    #region Database Initialization

    public async Task InitializeVectorTablesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();

        // Create schema if it doesn't exist
        var createSchemaSql = $@"
            IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{_schemaName}')
            BEGIN
                EXEC('CREATE SCHEMA [{_schemaName}]')
            END";
        await connection.ExecuteAsync(createSchemaSql);

        var sql = $@"
            IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'EntityVectors' AND s.name = '{_schemaName}')
            BEGIN
                CREATE TABLE [{_schemaName}].[EntityVectors] (
                    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    EntityName NVARCHAR(500) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    Embedding VECTOR({_embeddingDimensions}) NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT PK_EntityVectors PRIMARY KEY (Id),
                    CONSTRAINT UQ_EntityVectors_Name UNIQUE (EntityName)
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE t.name = 'RelationshipVectors' AND s.name = '{_schemaName}')
            BEGIN
                CREATE TABLE [{_schemaName}].[RelationshipVectors] (
                    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
                    SourceEntityName NVARCHAR(500) NOT NULL,
                    TargetEntityName NVARCHAR(500) NOT NULL,
                    Content NVARCHAR(MAX) NOT NULL,
                    Embedding VECTOR({_embeddingDimensions}) NOT NULL,
                    CreatedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
                    CONSTRAINT PK_RelationshipVectors PRIMARY KEY (Id),
                    CONSTRAINT UQ_RelationshipVectors_Pair UNIQUE (SourceEntityName, TargetEntityName)
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EntityVectors_EntityName')
                CREATE INDEX IX_EntityVectors_EntityName ON [{_schemaName}].[EntityVectors](EntityName);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RelationshipVectors_Source')
                CREATE INDEX IX_RelationshipVectors_Source ON [{_schemaName}].[RelationshipVectors](SourceEntityName);

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RelationshipVectors_Target')
                CREATE INDEX IX_RelationshipVectors_Target ON [{_schemaName}].[RelationshipVectors](TargetEntityName);
        ";

        await connection.ExecuteAsync(sql);
    }

    #endregion
}
