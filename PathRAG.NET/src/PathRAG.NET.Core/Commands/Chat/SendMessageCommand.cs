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
    private readonly IPathRAGLoggerService _logger;

    public SendMessageCommandHandler(
        IChatRepository chatRepository,
        IPathRAGQueryService pathRAGService,
        IChatCompletionService chatService,
        PathRAGSettings settings,
        IPathRAGLoggerService logger)
    {
        _chatRepository = chatRepository;
        _pathRAGService = pathRAGService;
        _chatService = chatService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ChatMessageDto> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        // Start operation logging
        var logId = await _logger.StartOperationAsync(
            "SendMessage",
            threadId: request.ThreadId,
            metadata: $"{{\"messageLength\":{request.Message.Length}}}",
            cancellationToken: cancellationToken);

        Guid stageLogId;

        try
        {
            // Stage 1: Save user message
            stageLogId = await _logger.StartStageAsync(logId, "MSG_START", message: "Saving user message", cancellationToken: cancellationToken);
            var userMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ThreadId = request.ThreadId,
                Role = "user",
                Content = request.Message
            };
            await _chatRepository.AddMessageAsync(userMessage, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, details: $"Message ID: {userMessage.Id}", cancellationToken: cancellationToken);

            // Stage 2-6: Build PathRAG context (logging is inside PathRAGQueryService)
            var context = await _pathRAGService.BuildQueryContextAsync(
                request.Message,
                request.QueryParams,
                logId,
                cancellationToken);

            // Stage 7: Build chat history with context
            stageLogId = await _logger.StartStageAsync(logId, "MSG_CHAT_HISTORY", message: "Building chat history", cancellationToken: cancellationToken);
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(BuildSystemPrompt(context));

            var previousMessages = await _chatRepository.GetMessagesByThreadIdAsync(
                request.ThreadId, _settings.MessageLimit, cancellationToken);

            foreach (var msg in previousMessages)
            {
                if (msg.Role == "user")
                    chatHistory.AddUserMessage(msg.Content);
                else if (msg.Role == "assistant")
                    chatHistory.AddAssistantMessage(msg.Content);
            }
            await _logger.CompleteStageAsync(stageLogId, itemsProcessed: previousMessages.Count(), details: $"Loaded {previousMessages.Count()} previous messages", cancellationToken: cancellationToken);

            // Stage 8: Get AI response
            stageLogId = await _logger.StartStageAsync(logId, "MSG_LLM_RESPONSE", message: "Generating LLM response", cancellationToken: cancellationToken);
            var response = await _chatService.GetChatMessageContentAsync(chatHistory, cancellationToken: cancellationToken);
            var responseContent = response.Content ?? "I apologize, but I couldn't generate a response.";
            await _logger.CompleteStageAsync(stageLogId, details: $"Response length: {responseContent.Length} chars", cancellationToken: cancellationToken);

            // Stage 9: Save assistant message
            stageLogId = await _logger.StartStageAsync(logId, "MSG_COMPLETE", message: "Saving assistant response", cancellationToken: cancellationToken);
            var assistantMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                ThreadId = request.ThreadId,
                Role = "assistant",
                Content = responseContent,
                InputTokens = 0,
                OutputTokens = 0
            };
            await _chatRepository.AddMessageAsync(assistantMessage, cancellationToken);
            await _logger.CompleteStageAsync(stageLogId, details: $"Assistant message ID: {assistantMessage.Id}", cancellationToken: cancellationToken);

            // Complete operation
            await _logger.CompleteOperationAsync(logId, cancellationToken);

            return new ChatMessageDto
            {
                Id = assistantMessage.Id,
                ThreadId = assistantMessage.ThreadId,
                Role = assistantMessage.Role,
                Content = assistantMessage.Content,
                CreatedAt = assistantMessage.CreatedAt,
                InputTokens = assistantMessage.InputTokens,
                OutputTokens = assistantMessage.OutputTokens
            };
        }
        catch (Exception ex)
        {
            await _logger.FailOperationAsync(logId, ex.Message, cancellationToken);
            throw;
        }
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

