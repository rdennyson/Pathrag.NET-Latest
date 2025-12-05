using AutoMapper;
using MediatR;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;

namespace PathRAG.NET.Core.Queries.Documents;

public record GetDocumentsQuery : IRequest<IEnumerable<DocumentDto>>;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, IEnumerable<DocumentDto>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMapper _mapper;

    public GetDocumentsQueryHandler(IDocumentRepository documentRepository, IMapper mapper)
    {
        _documentRepository = documentRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<DocumentDto>>(documents);
    }
}

public record GetDocumentByIdQuery(Guid DocumentId) : IRequest<DocumentDto?>;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IMapper _mapper;

    public GetDocumentByIdQueryHandler(IDocumentRepository documentRepository, IMapper mapper)
    {
        _documentRepository = documentRepository;
        _mapper = mapper;
    }

    public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdWithChunksAsync(request.DocumentId, cancellationToken);
        return document == null ? null : _mapper.Map<DocumentDto>(document);
    }
}

