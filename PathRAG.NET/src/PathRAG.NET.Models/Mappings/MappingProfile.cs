using AutoMapper;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Models.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Document mappings
        CreateMap<Document, DocumentDto>()
            .ForMember(dest => dest.ChunkCount, opt => opt.MapFrom(src => src.Chunks.Count));
        
        CreateMap<DocumentChunk, DocumentChunkDto>();
        
        // Chat mappings
        CreateMap<ChatThread, ChatThreadDto>()
            .ForMember(dest => dest.MessageCount, opt => opt.MapFrom(src => src.Messages.Count));
        
        CreateMap<ChatMessage, ChatMessageDto>();
        
        // Graph entity mappings
        CreateMap<GraphEntity, GraphEntityDto>()
            .ForMember(dest => dest.Rank, opt => opt.Ignore());
        
        CreateMap<GraphEntity, GraphNodeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.EntityName))
            .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.EntityName))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.EntityType));
        
        // Graph relationship mappings
        CreateMap<GraphRelationship, GraphRelationshipDto>()
            .ForMember(dest => dest.Rank, opt => opt.Ignore());
        
        CreateMap<GraphRelationship, GraphEdgeDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => $"{src.SourceEntityName}->{src.TargetEntityName}"))
            .ForMember(dest => dest.Source, opt => opt.MapFrom(src => src.SourceEntityName))
            .ForMember(dest => dest.Target, opt => opt.MapFrom(src => src.TargetEntityName))
            .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Keywords));

        // Log mappings
        CreateMap<PathRAGLog, PathRAGLogDto>()
            .ForMember(dest => dest.StageLogs, opt => opt.MapFrom(src => src.StageLogs));

        CreateMap<PathRAGStage, PathRAGStageDto>();

        CreateMap<PathRAGStageLog, PathRAGStageLogDto>()
            .ForMember(dest => dest.StageCode, opt => opt.MapFrom(src => src.Stage != null ? src.Stage.StageCode : string.Empty))
            .ForMember(dest => dest.StageName, opt => opt.MapFrom(src => src.Stage != null ? src.Stage.StageName : string.Empty))
            .ForMember(dest => dest.StageOrder, opt => opt.MapFrom(src => src.Stage != null ? src.Stage.StageOrder : 0));
    }
}

