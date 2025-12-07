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
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<ChatThread> ChatThreads => Set<ChatThread>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<PathRAGLog> PathRAGLogs => Set<PathRAGLog>();
    public DbSet<PathRAGStage> PathRAGStages => Set<PathRAGStage>();
    public DbSet<PathRAGStageLog> PathRAGStageLogs => Set<PathRAGStageLog>();

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
            entity.Property(e => e.DocumentTypeId).IsRequired();

            entity.HasOne(e => e.DocumentType)
                  .WithMany(dt => dt.Documents)
                  .HasForeignKey(e => e.DocumentTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

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

        // DocumentType configuration
        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.ToTable("DocumentTypes", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasOne(e => e.ParentDocumentType)
                  .WithMany(e => e.Children)
                  .HasForeignKey(e => e.ParentDocumentTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasData(
                new DocumentType
                {
                    Id = DocumentTypeConstants.GeneralDocumentTypeId,
                    Name = "General Documents",
                    Description = "General files without a specialized category"
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = "HR Documents",
                    Description = "Human resources policies, onboarding, and compliance"
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Name = "Policy Documents",
                    Description = "Formal policy files created by HR",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002")
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    Name = "Leave Policies",
                    Description = "Leave, PTO, and time-off policies",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000003")
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    Name = "Travel Policies",
                    Description = "Travel, expense, and reimbursement guidelines",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000003")
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                    Name = "Benefits Policies",
                    Description = "Benefits, compensation, and perks documentation",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000003")
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
                    Name = "Onboarding Documents",
                    Description = "New hire and onboarding materials",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002")
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000008"),
                    Name = "Finance Documents",
                    Description = "Budgets, forecasts, and compliance materials"
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000009"),
                    Name = "Budget Reports",
                    Description = "Monthly and quarterly budget reports",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000008")
                },
                new DocumentType
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-00000000000A"),
                    Name = "Expense Policies",
                    Description = "Finance expense policy documentation",
                    ParentDocumentTypeId = Guid.Parse("00000000-0000-0000-0000-000000000008")
                }
            );
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

        // PathRAGLog configuration
        modelBuilder.Entity<PathRAGLog>(entity =>
        {
            entity.ToTable("PathRAGLogs", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.ThreadId);
            entity.HasIndex(e => new { e.OperationType, e.StartedAt });
        });

        // PathRAGStage configuration
        modelBuilder.Entity<PathRAGStage>(entity =>
        {
            entity.ToTable("PathRAGStages", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StageName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StageCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.StageCode).IsUnique();

            // Seed PathRAG stages - automatically inserted on migration
            entity.HasData(
                // DocumentUpload stages
                new PathRAGStage { Id = 1, StageCode = "DOC_CREATE", StageName = "Create Document Record", OperationType = "DocumentUpload", StageOrder = 1, Description = "Initialize document record in database" },
                new PathRAGStage { Id = 2, StageCode = "DOC_DECODE", StageName = "Decode Content", OperationType = "DocumentUpload", StageOrder = 2, Description = "Extract text from PDF/DOCX/TXT files" },
                new PathRAGStage { Id = 3, StageCode = "DOC_CHUNK", StageName = "Text Chunking", OperationType = "DocumentUpload", StageOrder = 3, Description = "Split text into token-sized chunks" },
                new PathRAGStage { Id = 4, StageCode = "DOC_EMBED_CHUNKS", StageName = "Generate Chunk Embeddings", OperationType = "DocumentUpload", StageOrder = 4, Description = "Generate embeddings for document chunks" },
                new PathRAGStage { Id = 5, StageCode = "DOC_EXTRACT_ENTITIES", StageName = "Extract Entities & Relationships", OperationType = "DocumentUpload", StageOrder = 5, Description = "LLM extraction of entities and relationships from chunks" },
                new PathRAGStage { Id = 6, StageCode = "DOC_MERGE_ENTITIES", StageName = "Merge Entities", OperationType = "DocumentUpload", StageOrder = 6, Description = "Merge duplicate entities with LLM" },
                new PathRAGStage { Id = 7, StageCode = "DOC_MERGE_RELS", StageName = "Merge Relationships", OperationType = "DocumentUpload", StageOrder = 7, Description = "Merge duplicate relationships with LLM" },
                new PathRAGStage { Id = 8, StageCode = "DOC_EMBED_ENTITIES", StageName = "Generate Entity Embeddings", OperationType = "DocumentUpload", StageOrder = 8, Description = "Store entity vectors for semantic search" },
                new PathRAGStage { Id = 9, StageCode = "DOC_EMBED_RELS", StageName = "Generate Relationship Embeddings", OperationType = "DocumentUpload", StageOrder = 9, Description = "Store relationship vectors for semantic search" },
                new PathRAGStage { Id = 10, StageCode = "DOC_COMPLETE", StageName = "Complete Processing", OperationType = "DocumentUpload", StageOrder = 10, Description = "Mark document as completed" },

                // SendMessage (Chat/Query) stages
                new PathRAGStage { Id = 20, StageCode = "MSG_START", StageName = "Initialize Message", OperationType = "SendMessage", StageOrder = 1, Description = "Save user message and start processing" },
                new PathRAGStage { Id = 21, StageCode = "MSG_KEYWORDS", StageName = "Extract Keywords", OperationType = "SendMessage", StageOrder = 2, Description = "Extract high-level and low-level keywords from query" },
                new PathRAGStage { Id = 22, StageCode = "MSG_EMBED", StageName = "Generate Query Embedding", OperationType = "SendMessage", StageOrder = 3, Description = "Generate embedding for semantic search" },
                new PathRAGStage { Id = 23, StageCode = "MSG_SEARCH_ENTITIES", StageName = "Search Entities", OperationType = "SendMessage", StageOrder = 4, Description = "Vector search for relevant entities" },
                new PathRAGStage { Id = 24, StageCode = "MSG_SEARCH_RELS", StageName = "Search Relationships", OperationType = "SendMessage", StageOrder = 5, Description = "Vector search for relevant relationships" },
                new PathRAGStage { Id = 25, StageCode = "MSG_BUILD_CONTEXT", StageName = "Build Query Context", OperationType = "SendMessage", StageOrder = 6, Description = "Combine entities, relationships, and text units" },
                new PathRAGStage { Id = 26, StageCode = "MSG_CHAT_HISTORY", StageName = "Build Chat History", OperationType = "SendMessage", StageOrder = 7, Description = "Load previous messages from thread" },
                new PathRAGStage { Id = 27, StageCode = "MSG_LLM_RESPONSE", StageName = "Generate LLM Response", OperationType = "SendMessage", StageOrder = 8, Description = "Generate final response with LLM" },
                new PathRAGStage { Id = 28, StageCode = "MSG_COMPLETE", StageName = "Complete Message", OperationType = "SendMessage", StageOrder = 9, Description = "Save assistant response and return" },

                // GetKnowledgeGraph stages
                new PathRAGStage { Id = 30, StageCode = "GRAPH_START", StageName = "Initialize Graph Query", OperationType = "GetKnowledgeGraph", StageOrder = 1, Description = "Start graph visualization query" },
                new PathRAGStage { Id = 31, StageCode = "GRAPH_LOAD_ENTITIES", StageName = "Load Entities", OperationType = "GetKnowledgeGraph", StageOrder = 2, Description = "Fetch entities from graph database" },
                new PathRAGStage { Id = 32, StageCode = "GRAPH_LOAD_RELS", StageName = "Load Relationships", OperationType = "GetKnowledgeGraph", StageOrder = 3, Description = "Fetch relationships from graph database" },
                new PathRAGStage { Id = 33, StageCode = "GRAPH_BUILD", StageName = "Build Graph Response", OperationType = "GetKnowledgeGraph", StageOrder = 4, Description = "Construct graph visualization data" },
                new PathRAGStage { Id = 34, StageCode = "GRAPH_COMPLETE", StageName = "Complete Graph Query", OperationType = "GetKnowledgeGraph", StageOrder = 5, Description = "Return graph data" }
            );
        });

        // PathRAGStageLog configuration
        modelBuilder.Entity<PathRAGStageLog>(entity =>
        {
            entity.ToTable("PathRAGStageLogs", _schemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LogLevel).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StartedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()");
            entity.HasIndex(e => new { e.LogId, e.StageId });
            entity.HasIndex(e => e.DocumentId);

            entity.HasOne(e => e.Log)
                  .WithMany(l => l.StageLogs)
                  .HasForeignKey(e => e.LogId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Stage)
                  .WithMany()
                  .HasForeignKey(e => e.StageId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

