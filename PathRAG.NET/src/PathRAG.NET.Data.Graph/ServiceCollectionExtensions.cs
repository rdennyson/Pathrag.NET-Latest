using Microsoft.Extensions.DependencyInjection;
using PathRAG.NET.Data.Graph.Interfaces;

namespace PathRAG.NET.Data.Graph;

/// <summary>
/// Extension methods for registering graph services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add graph services with the specified configuration action
    /// The actual implementation is registered by the provider-specific package
    /// </summary>
    public static IServiceCollection AddGraphServices(
        this IServiceCollection services,
        Action<GraphSettings> configure)
    {
        var settings = new GraphSettings();
        configure(settings);
        services.AddSingleton(settings);
        return services;
    }
}

