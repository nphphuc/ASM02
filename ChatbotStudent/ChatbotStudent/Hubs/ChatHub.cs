using System.Text.Json;
using ChatbotStudent.Models;
using ChatbotStudent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatbotStudent.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IRagService _ragService;
    private readonly IChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IRagService ragService,
        IChatService chatService,
        ILogger<ChatHub> logger)
    {
        _ragService = ragService;
        _chatService = chatService;
        _logger = logger;
    }

    public async Task JoinSession(int sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");
        _logger.LogInformation("Client {ConnectionId} joined session {SessionId}",
            Context.ConnectionId, sessionId);
    }

    public async Task LeaveSession(int sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");
    }

    public async Task SendMessage(int sessionId, string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return;

        // Get current user ID from claims
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            await Clients.Caller.SendAsync("Error", "Không xác thực được người dùng.");
            return;
        }

        var session = await _chatService.GetSessionAsync(sessionId);
        if (session == null)
        {
            await Clients.Caller.SendAsync("Error", "Phiên chat không tồn tại.");
            return;
        }

        // Ownership check: session must belong to the current user
        if (session.UserId != userId)
        {
            await Clients.Caller.SendAsync("Error", "Bạn không có quyền truy cập phiên chat này.");
            return;
        }

        // Save user message
        var userMessage = await _chatService.AddMessageAsync(
            sessionId, MessageRole.User, question);

        // Notify clients about user message
        await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
        {
            role = "User",
            content = question,
            createdAt = userMessage.CreatedAt
        });

        // Send typing indicator
        await Clients.Group($"session_{sessionId}").SendAsync("Typing", true);

        try
        {
            // Generate RAG response
            var result = await _ragService.QueryAsync(question, session.CourseId);

            // Save assistant message
            var sourcesJson = JsonSerializer.Serialize(result.Sources);
            var assistantMessage = await _chatService.AddMessageAsync(
                sessionId, MessageRole.Assistant, result.Answer,
                sourcesJson, result.ResponseTimeMs);

            // Send typing indicator off
            await Clients.Group($"session_{sessionId}").SendAsync("Typing", false);

            // Send assistant response
            await Clients.Group($"session_{sessionId}").SendAsync("ReceiveMessage", new
            {
                role = "Assistant",
                content = result.Answer,
                sources = result.Sources,
                responseTimeMs = result.ResponseTimeMs,
                createdAt = assistantMessage.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response for session {SessionId}", sessionId);
            await Clients.Group($"session_{sessionId}").SendAsync("Typing", false);
            await Clients.Caller.SendAsync("Error",
                "Đã xảy ra lỗi khi xử lý câu hỏi. Vui lòng thử lại.");
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            _logger.LogError(exception, "Client {ConnectionId} disconnected with error",
                Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
