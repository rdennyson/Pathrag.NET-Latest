using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PathRAG.NET.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPathRAGLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "PathRAGStageLogs",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "PathRAGLogs",
                schema: "PathRAG");

            migrationBuilder.DropTable(
                name: "PathRAGStages",
                schema: "PathRAG");
        }
    }
}
