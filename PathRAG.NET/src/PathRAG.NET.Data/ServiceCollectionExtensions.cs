using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PathRAG.NET.Data.Contexts;
using PathRAG.NET.Data.Repositories;

namespace PathRAG.NET.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPathRAGData(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<PathRAGDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.UseVectorSearch();
            });
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();

        return services;
    }
}

