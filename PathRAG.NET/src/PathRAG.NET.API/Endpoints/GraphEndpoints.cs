using MediatR;
using Microsoft.AspNetCore.Mvc;
using PathRAG.NET.Core.Queries.Graph;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.API.Endpoints;

public static class GraphEndpoints
{
    public static IEndpointRouteBuilder MapGraphEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/graph")
            .WithTags("Knowledge Graph")
            .WithOpenApi();

        group.MapGet("/", GetKnowledgeGraph)
            .WithName("GetKnowledgeGraph")
            .WithSummary("Get the full knowledge graph")
            .Produces<KnowledgeGraphDto>();

        group.MapGet("/stats", GetGraphStats)
            .WithName("GetGraphStats")
            .WithSummary("Get knowledge graph statistics")
            .Produces<GraphStatsDto>();

        group.MapPost("/query", QueryGraph)
            .WithName("QueryGraph")
            .WithSummary("Query the knowledge graph with a natural language query")
            .Produces<KnowledgeGraphDto>();

        return app;
    }

    private static async Task<IResult> GetKnowledgeGraph(
        [FromQuery] int limit,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var graph = await mediator.Send(new GetKnowledgeGraphQuery(limit > 0 ? limit : 100), cancellationToken);
        return Results.Ok(graph);
    }

    private static async Task<IResult> GetGraphStats(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var stats = await mediator.Send(new GetGraphStatsQuery(), cancellationToken);
        return Results.Ok(stats);
    }

    private static async Task<IResult> QueryGraph(
        [FromBody] GraphQueryRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var graph = await mediator.Send(new GetQueryGraphQuery(request.Query, request.TopK), cancellationToken);
        return Results.Ok(graph);
    }
}

public record GraphQueryRequest(string Query, int TopK = 40);

