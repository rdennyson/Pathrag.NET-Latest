using MediatR;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Commands.Chat;

public record CreateThreadCommand(string? Title = null) : IRequest<ChatThreadDto>;

public class CreateThreadCommandHandler : IRequestHandler<CreateThreadCommand, ChatThreadDto>
{
    private readonly IChatRepository _chatRepository;

    public CreateThreadCommandHandler(IChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<ChatThreadDto> Handle(CreateThreadCommand request, CancellationToken cancellationToken)
    {
        var thread = new ChatThread
        {
            Id = Guid.NewGuid(),
            Title = request.Title ?? "New Chat"
        };

        await _chatRepository.CreateThreadAsync(thread, cancellationToken);

        return new ChatThreadDto
        {
            Id = thread.Id,
            Title = thread.Title,
            CreatedAt = thread.CreatedAt,
            LastMessageAt = thread.LastMessageAt,
            MessageCount = 0
        };
    }
}

