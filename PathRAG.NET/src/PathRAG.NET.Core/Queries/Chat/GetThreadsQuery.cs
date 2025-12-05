using AutoMapper;
using MediatR;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Queries.Chat;

public record GetThreadsQuery : IRequest<IEnumerable<ChatThreadDto>>;

public class GetThreadsQueryHandler : IRequestHandler<GetThreadsQuery, IEnumerable<ChatThreadDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMapper _mapper;

    public GetThreadsQueryHandler(IChatRepository chatRepository, IMapper mapper)
    {
        _chatRepository = chatRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ChatThreadDto>> Handle(GetThreadsQuery request, CancellationToken cancellationToken)
    {
        var threads = await _chatRepository.GetAllThreadsAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ChatThreadDto>>(threads);
    }
}

public record GetThreadByIdQuery(Guid ThreadId) : IRequest<ChatThreadDto?>;

public class GetThreadByIdQueryHandler : IRequestHandler<GetThreadByIdQuery, ChatThreadDto?>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMapper _mapper;

    public GetThreadByIdQueryHandler(IChatRepository chatRepository, IMapper mapper)
    {
        _chatRepository = chatRepository;
        _mapper = mapper;
    }

    public async Task<ChatThreadDto?> Handle(GetThreadByIdQuery request, CancellationToken cancellationToken)
    {
        var thread = await _chatRepository.GetThreadWithMessagesAsync(request.ThreadId, cancellationToken);
        return thread == null ? null : _mapper.Map<ChatThreadDto>(thread);
    }
}

public record GetThreadMessagesQuery(Guid ThreadId) : IRequest<IEnumerable<ChatMessageDto>>;

public class GetThreadMessagesQueryHandler : IRequestHandler<GetThreadMessagesQuery, IEnumerable<ChatMessageDto>>
{
    private readonly IChatRepository _chatRepository;
    private readonly IMapper _mapper;

    public GetThreadMessagesQueryHandler(IChatRepository chatRepository, IMapper mapper)
    {
        _chatRepository = chatRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ChatMessageDto>> Handle(GetThreadMessagesQuery request, CancellationToken cancellationToken)
    {
        var messages = await _chatRepository.GetMessagesByThreadIdAsync(request.ThreadId, cancellationToken: cancellationToken);
        return _mapper.Map<IEnumerable<ChatMessageDto>>(messages);
    }
}

