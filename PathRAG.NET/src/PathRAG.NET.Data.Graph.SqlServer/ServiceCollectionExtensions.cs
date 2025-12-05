using Microsoft.Extensions.DependencyInjection;
using PathRAG.NET.Data.Graph;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Data.Graph.SqlServer.Repositories;

namespace PathRAG.NET.Data.Graph.SqlServer;

/// <summary>
/// Extension methods for registering SQL Server Graph services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add SQL Server Graph repository implementation
    /// </summary>
    public static IServiceCollection AddSqlServerGraphServices(
        this IServiceCollection services,
        Action<GraphSettings>? configure = null)
    {
        if (configure != null)
        {
            var settings = new GraphSettings();
            configure(settings);
            services.AddSingleton(settings);
        }

        services.AddScoped<IGraphRepository, SqlServerGraphRepository>();
        services.AddScoped<IGraphVectorRepository, SqlServerGraphVectorRepository>();
        
        return services;
    }
}

