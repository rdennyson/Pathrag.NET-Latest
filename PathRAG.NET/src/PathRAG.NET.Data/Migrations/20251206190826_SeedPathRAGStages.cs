using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PathRAG.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedPathRAGStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                schema: "PathRAG",
                table: "PathRAGStages",
                keyColumn: "Id",
                keyValue: 34);
        }
    }
}
