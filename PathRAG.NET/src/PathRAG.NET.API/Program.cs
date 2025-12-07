using Microsoft.EntityFrameworkCore;
using PathRAG.NET.API.Endpoints;
using PathRAG.NET.Core;
using PathRAG.NET.Data;
using PathRAG.NET.Data.Graph;
using PathRAG.NET.Data.Graph.SqlServer;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add CORS for Blazor UI
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Get connection string and schema name
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var schemaName = builder.Configuration.GetValue<string>("Database:SchemaName") ?? "PathRAG";

// Register PathRAG services
builder.Services.AddPathRAGData(connectionString, schemaName);
builder.Services.AddGraphServices(settings =>
{
    settings.ConnectionString = connectionString;
    settings.Provider = "SqlServer";
    settings.EmbeddingDimensions = 1536;
    settings.SchemaName = schemaName;
});
builder.Services.AddSqlServerGraphServices();
builder.Services.AddPathRAGCore(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("PathRAG.NET API")
               .WithTheme(ScalarTheme.BluePlanet)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseHttpsRedirection();
app.UseCors();

// Map endpoints
app.MapDocumentEndpoints();
app.MapDocumentTypeEndpoints();
app.MapChatEndpoints();
app.MapGraphEndpoints();
app.MapLogEndpoints();

// Initialize database (ensure EF migrations run before graph tables)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PathRAG.NET.Data.Contexts.PathRAGDbContext>();
    await context.Database.MigrateAsync();

    var graphRepo = scope.ServiceProvider.GetRequiredService<PathRAG.NET.Data.Graph.Interfaces.IGraphRepository>();
    var graphVectorRepo = scope.ServiceProvider.GetRequiredService<PathRAG.NET.Data.Graph.Interfaces.IGraphVectorRepository>();

    // Initialize graph NODE and EDGE tables
    await graphRepo.InitializeDatabaseAsync();

    // Initialize separate vector tables for entities and relationships
    // (matching Python PathRAG's entities_vdb and relationships_vdb)
    await graphVectorRepo.InitializeVectorTablesAsync();
}

app.Run();

