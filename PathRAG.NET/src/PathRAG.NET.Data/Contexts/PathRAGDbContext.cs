using Microsoft.EntityFrameworkCore;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Data.Contexts;

/// <summary>
/// EF Core DbContext for PathRAG (non-graph tables)
/// Graph tables are handled by Dapper in PathRAG.NET.Data.Graph.SqlServer
/// </summary>
public class PathRAGDbContext : DbContext
{
    private readonly string _schemaName;

    public PathRAGDbContext(DbContextOptions<PathRAGDbContext> options, PathRAGDataSettings? settings = null) : base(options)
    {
        _schemaName = settings?.SchemaName ?? "PathRAG";
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema for all tables
        modelBuilder.HasDefaultSchema(_schemaName);

        // Document configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Documents", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50).HasDefaultValue("pending");
            entity.Property(e => e.CreationDate).HasDefaultValueSql("SYSDATETIMEOFFSET()");

            entity.HasMany(e => e.Chunks)
                  .WithOne(c => c.Document)
                  .HasForeignKey(c => c.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DocumentChunk configuration with Vector column
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.ToTable("DocumentChunks", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Embedding)
                  .HasColumnType("vector(1536)")
                  .IsRequired();
        });

        // ChatThread configuration
        modelBuilder.Entity<ChatThread>(entity =>
        {
            entity.ToTable("ChatThreads", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");

            entity.HasMany(e => e.Messages)
                  .WithOne(m => m.Thread)
                  .HasForeignKey(m => m.ThreadId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("ChatMessages", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
        });
    }
}

