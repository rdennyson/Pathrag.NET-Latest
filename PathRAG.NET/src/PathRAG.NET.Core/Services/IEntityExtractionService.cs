using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Services;

public interface IEntityExtractionService
{
    Task<(IEnumerable<ExtractedEntityDto> Entities, IEnumerable<ExtractedRelationshipDto> Relationships)> 
        ExtractEntitiesAndRelationshipsAsync(
            string text,
            string sourceId,
            CancellationToken cancellationToken = default);
}

