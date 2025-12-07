using Microsoft.AspNetCore.Mvc;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.API.Endpoints;

public static class DocumentTypeEndpoints
{
    public static IEndpointRouteBuilder MapDocumentTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documenttypes")
            .WithTags("Document Types")
            .WithOpenApi();

        group.MapGet("/", GetDocumentTypes)
            .WithName("GetDocumentTypes")
            .Produces<IEnumerable<DocumentTypeDto>>();

        return app;
    }

    private static async Task<IResult> GetDocumentTypes(
        [FromServices] IDocumentTypeRepository repository,
        CancellationToken cancellationToken)
    {
        var types = (await repository.GetAllAsync(cancellationToken)).ToList();
        var dtos = types.ToDictionary(
            dt => dt.Id,
            dt => new DocumentTypeDto
            {
                Id = dt.Id,
                Name = dt.Name,
                Description = dt.Description,
                ParentDocumentTypeId = dt.ParentDocumentTypeId
            });

        foreach (var dto in dtos.Values)
        {
            if (dto.ParentDocumentTypeId.HasValue && dtos.TryGetValue(dto.ParentDocumentTypeId.Value, out var parent))
            {
                parent.Children.Add(dto);
            }
        }

        var rootTypes = dtos.Values.Where(dt => dt.ParentDocumentTypeId == null).ToList();
        return Results.Ok(rootTypes);
    }
}
