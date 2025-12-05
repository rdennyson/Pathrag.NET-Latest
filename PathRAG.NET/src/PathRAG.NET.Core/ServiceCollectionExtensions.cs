using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using PathRAG.NET.Core.Services;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Models.Mappings;

namespace PathRAG.NET.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPathRAGCore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register settings
        var pathRAGSettings = configuration.GetSection("PathRAG").Get<PathRAGSettings>() ?? new PathRAGSettings();
        services.AddSingleton(pathRAGSettings);

        var azureOpenAISettings = configuration.GetSection("AzureOpenAI").Get<AzureOpenAISettings>() ?? new AzureOpenAISettings();
        services.AddSingleton(azureOpenAISettings);

        // Register Semantic Kernel
        var kernelBuilder = Kernel.CreateBuilder();
        
        // Add Azure OpenAI Chat Completion
        if (!string.IsNullOrEmpty(azureOpenAISettings.ChatCompletion.Endpoint))
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: azureOpenAISettings.ChatCompletion.Deployment,
                endpoint: azureOpenAISettings.ChatCompletion.Endpoint,
                apiKey: azureOpenAISettings.ChatCompletion.ApiKey,
                modelId: azureOpenAISettings.ChatCompletion.ModelId);
        }

        // Add Azure OpenAI Embeddings
        if (!string.IsNullOrEmpty(azureOpenAISettings.Embedding.Endpoint))
        {
            kernelBuilder.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: azureOpenAISettings.Embedding.Deployment,
                endpoint: azureOpenAISettings.Embedding.Endpoint,
                apiKey: azureOpenAISettings.Embedding.ApiKey,
                modelId: azureOpenAISettings.Embedding.ModelId);
        }

        var kernel = kernelBuilder.Build();
        services.AddSingleton(kernel);
        services.AddSingleton(kernel.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>());
        services.AddSingleton(kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());

        // Register services
        services.AddScoped<IContentDecoderService, ContentDecoderService>();
        services.AddScoped<ITextChunkerService, TextChunkerService>();
        services.AddScoped<IEntityExtractionService, EntityExtractionService>();
        services.AddScoped<IKeywordsExtractionService, KeywordsExtractionService>();
        services.AddScoped<IPathRAGQueryService, PathRAGQueryService>();

        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly));

        // Register AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        return services;
    }
}

