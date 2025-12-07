using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PathRAG.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "PathRAG");

            migrationBuilder.CreateTable(
                name: "ChatThreads",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    LastMessageAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatThreads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentDocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentTypes_DocumentTypes_ParentDocumentTypeId",
                        column: x => x.ParentDocumentTypeId,
                        principalSchema: "PathRAG",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PathRAGLogs",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalStages = table.Column<int>(type: "int", nullable: false),
                    CompletedStages = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathRAGLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PathRAGStages",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StageCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StageOrder = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathRAGStages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    InputTokens = table.Column<int>(type: "int", nullable: true),
                    OutputTokens = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_ChatThreads_ThreadId",
                        column: x => x.ThreadId,
                        principalSchema: "PathRAG",
                        principalTable: "ChatThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "pending"),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreationDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "PathRAG",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PathRAGStageLogs",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    LogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StageId = table.Column<int>(type: "int", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    ItemsProcessed = table.Column<int>(type: "int", nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PathRAGStageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PathRAGStageLogs_PathRAGLogs_LogId",
                        column: x => x.LogId,
                        principalSchema: "PathRAG",
                        principalTable: "PathRAGLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PathRAGStageLogs_PathRAGStages_StageId",
                        column: x => x.StageId,
                        principalSchema: "PathRAG",
                        principalTable: "PathRAGStages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentChunks",
                schema: "PathRAG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    IndexOnPage = table.Column<int>(type: "int", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Embedding = table.Column<string>(type: "vector(1536)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentChunks_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalSchema: "PathRAG",
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "PathRAG",
                table: "DocumentTypes",
                columns: new[] { "Id", "Description", "Name", "ParentDocumentTypeId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), "General files without a specialized category", "General Documents", null },
                    { new Guid("00000000-0000-0000-0000-000000000002"), "Human resources policies, onboarding, and compliance", "HR Documents", null },
                    { new Guid("00000000-0000-0000-0000-000000000008"), "Budgets, forecasts, and compliance materials", "Finance Documents", null }
                });

            migrationBuilder.InsertData(
                schema: "PathRAG",
                table: "PathRAGStages",
                columns: new[] { "Id", "Description", "IsActive", "OperationType", "StageCode", "StageName", "StageOrder" },
                values: new object[,]
                {
                    { 1, "Initialize document record in database", true, "DocumentUpload", "DOC_CREATE", "Create Document Record", 1 },
                    { 2, "Extract text from PDF/DOCX/TXT files", true, "DocumentUpload", "DOC_DECODE", "Decode Content", 2 },
                    { 3, "Split text into token-sized chunks", true, "DocumentUpload", "DOC_CHUNK", "Text Chunking", 3 },
                    { 4, "Generate embeddings for document chunks", true, "DocumentUpload", "DOC_EMBED_CHUNKS", "Generate Chunk Embeddings", 4 },
                    { 5, "LLM extraction of entities and relationships from chunks", true, "DocumentUpload", "DOC_EXTRACT_ENTITIES", "Extract Entities & Relationships", 5 },
                    { 6, "Merge duplicate entities with LLM", true, "DocumentUpload", "DOC_MERGE_ENTITIES", "Merge Entities", 6 },
                    { 7, "Merge duplicate relationships with LLM", true, "DocumentUpload", "DOC_MERGE_RELS", "Merge Relationships", 7 },
                    { 8, "Store entity vectors for semantic search", true, "DocumentUpload", "DOC_EMBED_ENTITIES", "Generate Entity Embeddings", 8 },
                    { 9, "Store relationship vectors for semantic search", true, "DocumentUpload", "DOC_EMBED_RELS", "Generate Relationship Embeddings", 9 },
                    { 10, "Mark document as completed", true, "DocumentUpload", "DOC_COMPLETE", "Complete Processing", 10 },
                    { 20, "Save user message and start processing", true, "SendMessage", "MSG_START", "Initialize Message", 1 },
                    { 21, "Extract high-level and low-level keywords from query", true, "SendMessage", "MSG_KEYWORDS", "Extract Keywords", 2 },
                    { 22, "Generate embedding for semantic search", true, "SendMessage", "MSG_EMBED", "Generate Query Embedding", 3 },
                    { 23, "Vector search for relevant entities", true, "SendMessage", "MSG_SEARCH_ENTITIES", "Search Entities", 4 },
                    { 24, "Vector search for relevant relationships", true, "SendMessage", "MSG_SEARCH_RELS", "Search Relationships", 5 },
                    { 25, "Combine entities, relationships, and text units", true, "SendMessage", "MSG_BUILD_CONTEXT", "Build Query Context", 6 },
                    { 26, "Load previous messages from thread", true, "SendMessage", "MSG_CHAT_HISTORY", "Build Chat History", 7 },
                    { 27, "Generate final response with LLM", true, "SendMessage", "MSG_LLM_RESPONSE", "Generate LLM Response", 8 },
                    { 28, "Save assistant response and return", true, "SendMessage", "MSG_COMPLETE", "Complete Message", 9 },
                    { 30, "Start graph visualization query", true, "GetKnowledgeGraph", "GRAPH_START", "Initialize Graph Query", 1 },
                    { 31, "Fetch entities from graph database", true, "GetKnowledgeGraph", "GRAPH_LOAD_ENTITIES", "Load Entities", 2 },
                    { 32, "Fetch relationships from graph database", true, "GetKnowledgeGraph", "GRAPH_LOAD_RELS", "Load Relationships", 3 },
                    { 33, "Construct graph visualization data", true, "GetKnowledgeGraph", "GRAPH_BUILD", "Build Graph Response", 4 },
                    { 34, "Return graph data", true, "GetKnowledgeGraph", "GRAPH_COMPLETE", "Complete Graph Query", 5 }
                });

            migrationBuilder.InsertData(
                schema: "PathRAG",
                table: "DocumentTypes",
                columns: new[] { "Id", "Description", "Name", "ParentDocumentTypeId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000003"), "Formal policy files created by HR", "Policy Documents", new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000007"), "New hire and onboarding materials", "Onboarding Documents", new Guid("00000000-0000-0000-0000-000000000002") },
                    { new Guid("00000000-0000-0000-0000-000000000009"), "Monthly and quarterly budget reports", "Budget Reports", new Guid("00000000-0000-0000-0000-000000000008") },
                    { new Guid("00000000-0000-0000-0000-00000000000a"), "Finance expense policy documentation", "Expense Policies", new Guid("00000000-0000-0000-0000-000000000008") },
                    { new Guid("00000000-0000-0000-0000-000000000004"), "Leave, PTO, and time-off policies", "Leave Policies", new Guid("00000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000005"), "Travel, expense, and reimbursement guidelines", "Travel Policies", new Guid("00000000-0000-0000-0000-000000000003") },
                    { new Guid("00000000-0000-0000-0000-000000000006"), "Benefits, compensation, and perks documentation", "Benefits Policies", new Guid("00000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ThreadId",
                schema: "PathRAG",
                table: "ChatMessages",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentChunks_DocumentId",
                schema: "PathRAG",
                table: "DocumentChunks",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentTypeId",
                schema: "PathRAG",
                table: "Documents",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_ParentDocumentTypeId",
                schema: "PathRAG",
                table: "DocumentTypes",
                column: "ParentDocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGLogs_DocumentId",
                schema: "PathRAG",
                table: "PathRAGLogs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGLogs_OperationType_StartedAt",
                schema: "PathRAG",
                table: "PathRAGLogs",
                columns: new[] { "OperationType", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGLogs_ThreadId",
                schema: "PathRAG",
                table: "PathRAGLogs",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGStageLogs_DocumentId",
                schema: "PathRAG",
                table: "PathRAGStageLogs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGStageLogs_LogId_StageId",
                schema: "PathRAG",
                table: "PathRAGStageLogs",
                columns: new[] { "LogId", "StageId" });

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGStageLogs_StageId",
                schema: "PathRAG",
                table: "PathRAGStageLogs",
                column: "StageId");

            migrationBuilder.CreateIndex(
                name: "IX_PathRAGStages_StageCode",
                schema: "PathRAG",
                table: "PathRAGStages",
                column: "StageCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "DocumentChunks",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "PathRAGStageLogs",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "ChatThreads",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "PathRAGLogs",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "PathRAGStages",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "DocumentTypes",
                schema: "PathRAG");
        }
    }
}
