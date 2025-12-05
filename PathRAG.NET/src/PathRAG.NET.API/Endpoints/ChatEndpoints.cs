using MediatR;
using Microsoft.AspNetCore.Mvc;
using PathRAG.NET.Core.Commands.Chat;
using PathRAG.NET.Core.Queries.Chat;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.API.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/chat")
            .WithTags("Chat")
            .WithOpenApi();

        group.MapGet("/threads", GetThreads)
            .WithName("GetThreads")
            .WithSummary("Get all chat threads")
            .Produces<IEnumerable<ChatThreadDto>>();

        group.MapGet("/threads/{id:guid}", GetThreadById)
            .WithName("GetThreadById")
            .WithSummary("Get chat thread by ID with messages")
            .Produces<ChatThreadDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/threads", CreateThread)
            .WithName("CreateThread")
            .WithSummary("Create a new chat thread")
            .Produces<ChatThreadDto>(StatusCodes.Status201Created);

        group.MapDelete("/threads/{id:guid}", DeleteThread)
            .WithName("DeleteThread")
            .WithSummary("Delete a chat thread")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/threads/{id:guid}/messages", GetThreadMessages)
            .WithName("GetThreadMessages")
            .WithSummary("Get messages for a chat thread")
            .Produces<IEnumerable<ChatMessageDto>>();

        group.MapPost("/threads/{id:guid}/messages", SendMessage)
            .WithName("SendMessage")
            .WithSummary("Send a message to a chat thread")
            .Produces<ChatMessageDto>(StatusCodes.Status201Created);

        return app;
    }

    private static async Task<IResult> GetThreads(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var threads = await mediator.Send(new GetThreadsQuery(), cancellationToken);
        return Results.Ok(threads);
    }

    private static async Task<IResult> GetThreadById(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var thread = await mediator.Send(new GetThreadByIdQuery(id), cancellationToken);
        return thread is null ? Results.NotFound() : Results.Ok(thread);
    }

    private static async Task<IResult> CreateThread(
        [FromBody] CreateThreadRequest? request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var thread = await mediator.Send(new CreateThreadCommand(request?.Title), cancellationToken);
        return Results.Created($"/api/chat/threads/{thread.Id}", thread);
    }

    private static async Task<IResult> DeleteThread(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteThreadCommand(id), cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetThreadMessages(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var messages = await mediator.Send(new GetThreadMessagesQuery(id), cancellationToken);
        return Results.Ok(messages);
    }

    private static async Task<IResult> SendMessage(
        [FromRoute] Guid id,
        [FromBody] SendMessageRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(id, request.Message, request.QueryParams);
        var message = await mediator.Send(command, cancellationToken);
        return Results.Created($"/api/chat/threads/{id}/messages/{message.Id}", message);
    }
}

public record CreateThreadRequest(string? Title);
public record SendMessageRequest(string Message, QueryParamDto? QueryParams = null);

