using MediatR;
using PathRAG.NET.Data.Repositories;

namespace PathRAG.NET.Core.Commands.Chat;

public record DeleteThreadCommand(Guid ThreadId) : IRequest<bool>;

public class DeleteThreadCommandHandler : IRequestHandler<DeleteThreadCommand, bool>
{
    private readonly IChatRepository _chatRepository;

    public DeleteThreadCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<bool> Handle(DeleteThreadCommand request, CancellationToken cancellationToken)
    {
        var thread = await _chatRepository.GetThreadByIdAsync(request.ThreadId, cancellationToken);
        if (thread == null)
            return false;

        await _chatRepository.DeleteThreadAsync(request.ThreadId, cancellationToken);
        return true;
    }
}

