using MediatR;
using PathRAG.NET.Data.Repositories;

namespace PathRAG.NET.Core.Commands.Documents;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest<bool>;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, bool>
{
    private readonly IDocumentRepository _documentRepository;

    public DeleteDocumentCommandHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<bool> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        if (!await _documentRepository.ExistsAsync(request.DocumentId, cancellationToken))
            return false;

        await _documentRepository.DeleteAsync(request.DocumentId, cancellationToken);
        return true;
    }
}

