using MediatR;
using Microsoft.AspNetCore.Mvc;
using PathRAG.NET.Core.Commands.Documents;
using PathRAG.NET.Core.Queries.Documents;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.API.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .WithOpenApi();

        group.MapGet("/", GetDocuments)
            .WithName("GetDocuments")
            .WithSummary("Get all documents")
            .Produces<IEnumerable<DocumentDto>>();

        group.MapGet("/{id:guid}", GetDocumentById)
            .WithName("GetDocumentById")
            .WithSummary("Get document by ID")
            .Produces<DocumentDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/upload", UploadDocument)
            .WithName("UploadDocument")
            .WithSummary("Upload and process a document")
            .Produces<DocumentUploadResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .DisableAntiforgery();

        group.MapDelete("/{id:guid}", DeleteDocument)
            .WithName("DeleteDocument")
            .WithSummary("Delete a document")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetDocuments(
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var documents = await mediator.Send(new GetDocumentsQuery(), cancellationToken);
        return Results.Ok(documents);
    }

    private static async Task<IResult> GetDocumentById(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var document = await mediator.Send(new GetDocumentByIdQuery(id), cancellationToken);
        return document is null ? Results.NotFound() : Results.Ok(document);
    }

    private static async Task<IResult> UploadDocument(
        [FromForm] IFormFile file,
        [FromForm] Guid? documentTypeId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest("No file uploaded");

        using var stream = file.OpenReadStream();
        var typeId = documentTypeId ?? DocumentTypeConstants.GeneralDocumentTypeId;
        var command = new UploadDocumentCommand(file.FileName, file.ContentType, stream, typeId);
        var result = await mediator.Send(command, cancellationToken);
        
        return Results.Created($"/api/documents/{result.DocumentId}", result);
    }

    private static async Task<IResult> DeleteDocument(
        [FromRoute] Guid id,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var deleted = await mediator.Send(new DeleteDocumentCommand(id), cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}

