using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Data.Repositories;

namespace PathRAG.NET.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPathRAGData(
        this IServiceCollection services,
        string connectionString,
        string schemaName = "PathRAG")
    {
        services.AddDbContext<PathRAGDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseVectorSearch();
            });
        });

        // Register schema name for DbContext to use
        services.AddSingleton(new PathRAGDataSettings { SchemaName = schemaName });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        return services;
    }
}

/// <summary>
/// Settings for PathRAG Data layer
/// </summary>
public class PathRAGDataSettings
{
    public string SchemaName { get; set; } = "PathRAG";
}
