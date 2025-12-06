using MediatR;
using Microsoft.AspNetCore.Mvc;
using PathRAG.NET.Core.Queries.Logs;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.API.Endpoints;

public static class LogEndpoints
{
    public static IEndpointRouteBuilder MapLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/logs")
            .WithTags("Logs")
            .WithOpenApi();

        group.MapGet("/", GetLogs)
            .WithName("GetLogs")
            .WithSummary("Get paginated operation logs")
            .Produces<PagedLogResultDto>();

        group.MapGet("/{id:guid}", GetLogDetails)
            .WithName("GetLogDetails")
            .WithSummary("Get detailed log with all stages")
            .Produces<PathRAGLogDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/performance", GetPerformanceSummary)
            .WithName("GetPerformanceSummary")
            .WithSummary("Get performance summary and statistics")
            .Produces<LogPerformanceSummaryDto>();

        group.MapGet("/stages", GetStages)
            .WithName("GetStages")
            .WithSummary("Get all PathRAG processing stages")
            .Produces<IEnumerable<PathRAGStageDto>>();

        group.MapPost("/seed-stages", SeedStages)
            .WithName("SeedStages")
            .WithSummary("Seed PathRAG stages (run once)")
            .Produces(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> GetLogs(
        [FromQuery] Guid? documentId,
        [FromQuery] Guid? threadId,
        [FromQuery] string? operationType,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] IMediator mediator = null!,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLogsQuery(
            documentId, threadId, operationType, status,
            fromDate, toDate, pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetLogDetails(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var log = await mediator.Send(new GetLogDetailsQuery(id), cancellationToken);
        return log is null ? Results.NotFound() : Results.Ok(log);
    }

    private static async Task<IResult> GetPerformanceSummary(
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetPerformanceSummaryQuery(fromDate, toDate);
        var result = await mediator.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetStages(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var stages = await mediator.Send(new GetStagesQuery(), cancellationToken);
        return Results.Ok(stages);
    }

    private static async Task<IResult> SeedStages(
        [FromServices] IPathRAGLogRepository logRepository,
        CancellationToken cancellationToken)
    {
        await logRepository.SeedStagesAsync(cancellationToken);
        return Results.Ok("Stages seeded successfully");
    }
}

