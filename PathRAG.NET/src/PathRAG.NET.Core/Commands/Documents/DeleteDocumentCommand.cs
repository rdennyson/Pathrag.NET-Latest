using MediatR;
using Microsoft.Extensions.AI;
using PathRAG.NET.Core.Services;
using PathRAG.NET.Data.Graph.Interfaces;
using PathRAG.NET.Data.Repositories;

namespace PathRAG.NET.Core.Commands.Documents;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest<bool>;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, bool>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IGraphRepository _graphRepository;
    private readonly IGraphVectorRepository _graphVectorRepository;

 
    public DeleteDocumentCommandHandler(IDocumentRepository documentRepository, 
        IGraphRepository graphRepository,
        IGraphVectorRepository graphVectorRepository)
    {
        _documentRepository = documentRepository;
        _graphRepository = graphRepository;
        _graphVectorRepository = graphVectorRepository;
    }

    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!await _documentRepository.ExistsAsync(request.DocumentId, cancellationToken))
            return false;
        await _graphRepository.DeleteRelationshipAsync(request.DocumentId, cancellationToken);
        await _graphRepository.DeleteEntityAsync(request.DocumentId, cancellationToken);
        await _graphVectorRepository.DeleteRelationshipVectorAsync(request.DocumentId, cancellationToken);
        await _graphVectorRepository.DeleteEntityVectorAsync(request.DocumentId, cancellationToken);
        await _documentRepository.DeleteAsync(request.DocumentId, cancellationToken);
        return true;
    }
}

