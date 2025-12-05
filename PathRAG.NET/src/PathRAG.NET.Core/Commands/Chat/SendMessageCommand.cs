using System.Text;
using MediatR;
using Microsoft.SemanticKernel.ChatCompletion;
using PathRAG.NET.Core.Services;
using PathRAG.NET.Core.Settings;
using PathRAG.NET.Data.Repositories;
using PathRAG.NET.Models.DTOs;
using PathRAG.NET.Models.Entities;

namespace PathRAG.NET.Core.Commands.Chat;

public record SendMessageCommand(
    Guid ThreadId,
    string Message,
    QueryParamDto? QueryParams = null
) : IRequest<ChatMessageDto>;

public class SendMessageCommandHandler : IRequestHandler<SendMessageCommand, ChatMessageDto>
{
    private readonly IChatRepository _chatRepository;
    private readonly IPathRAGQueryService _pathRAGService;
    private readonly IChatCompletionService _chatService;
    private readonly PathRAGSettings _settings;

    public SendMessageCommandHandler(
        IChatRepository chatRepository,
        IPathRAGQueryService pathRAGService,
        IChatCompletionService chatService,
        PathRAGSettings settings)
    {
        _chatRepository = chatRepository;
        _pathRAGService = pathRAGService;
        _chatService = chatService;
        _settings = settings;
    }

    public async Task<ChatMessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // Step 1: Save user message
        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = request.ThreadId,
            Role = "user",
            Content = request.Message
        };
        await _chatRepository.AddMessageAsync(userMessage, cancellationToken);

        // Step 2: Build PathRAG context
        var context = await _pathRAGService.BuildQueryContextAsync(
            request.Message, 
            request.QueryParams, 
            cancellationToken);

        // Step 3: Build chat history with context
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(BuildSystemPrompt(context));

        // Add previous messages from thread
        var previousMessages = await _chatRepository.GetMessagesByThreadIdAsync(
            request.ThreadId, _settings.MessageLimit, cancellationToken);
        
        foreach (var msg in previousMessages)
        {
            if (msg.Role == "user")
                chatHistory.AddUserMessage(msg.Content);
            else if (msg.Role == "assistant")
                chatHistory.AddAssistantMessage(msg.Content);
        }

        // Step 4: Get AI response
        var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
        var responseContent = response.Content ?? "I apologize, but I couldn't generate a response.";

        // Step 5: Save assistant message
        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ThreadId = request.ThreadId,
            Role = "assistant",
            Content = responseContent,
            InputTokens = 0, // Could be extracted from response metadata
            OutputTokens = 0
        };
        await _chatRepository.AddMessageAsync(assistantMessage, cancellationToken);

        return new ChatMessageDto(
            assistantMessage.Id,
            assistantMessage.ThreadId,
            assistantMessage.Role,
            assistantMessage.Content,
            assistantMessage.CreatedAt,
            assistantMessage.InputTokens,
            assistantMessage.OutputTokens
        );
    }

    private static string BuildSystemPrompt(QueryContextDto context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a helpful AI assistant with access to a knowledge graph. Use the following context to answer questions accurately.");
        sb.AppendLine();
        sb.AppendLine("=== KNOWLEDGE GRAPH CONTEXT ===");
        sb.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(context.HighLevelEntitiesContext))
        {
            sb.AppendLine("## High-Level Entities (Global Context):");
            sb.AppendLine(context.HighLevelEntitiesContext);
        }
        
        if (!string.IsNullOrWhiteSpace(context.HighLevelRelationsContext))
        {
            sb.AppendLine("## High-Level Relationships:");
            sb.AppendLine(context.HighLevelRelationsContext);
        }
        
        if (!string.IsNullOrWhiteSpace(context.LowLevelEntitiesContext))
        {
            sb.AppendLine("## Specific Entities (Local Context):");
            sb.AppendLine(context.LowLevelEntitiesContext);
        }
        
        if (!string.IsNullOrWhiteSpace(context.LowLevelRelationsContext))
        {
            sb.AppendLine("## Specific Relationships:");
            sb.AppendLine(context.LowLevelRelationsContext);
        }
        
        if (!string.IsNullOrWhiteSpace(context.TextUnitsContext))
        {
            sb.AppendLine("## Source Documents:");
            sb.AppendLine(context.TextUnitsContext);
        }
        
        sb.AppendLine();
        sb.AppendLine("=== INSTRUCTIONS ===");
        sb.AppendLine("1. Use the knowledge graph context to provide accurate, grounded answers");
        sb.AppendLine("2. If the context doesn't contain relevant information, say so");
        sb.AppendLine("3. Cite sources when possible");
        sb.AppendLine("4. Be concise but comprehensive");
        
        return sb.ToString();
    }
}

