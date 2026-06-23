using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public class ChatService : IChatService
{
    private readonly AppDbContext _db;

    public ChatService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatSession> CreateSessionAsync(int courseId, int userId, string? title = null)
    {
        var session = new ChatSession
        {
            CourseId = courseId,
            UserId = userId,
            Title = title ?? $"Phiên chat {DateTime.Now:dd/MM/yyyy HH:mm}",
            StartedAt = DateTime.UtcNow
        };

        _db.ChatSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<ChatSession?> GetSessionAsync(int sessionId)
    {
        return await _db.ChatSessions
            .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<List<ChatSession>> GetSessionsByCourseAsync(int courseId)
    {
        return await _db.ChatSessions
            .Where(s => s.CourseId == courseId)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .ToListAsync();
    }

    public async Task<List<ChatSession>> GetUserSessionsByCourseAsync(int courseId, int userId)
    {
        return await _db.ChatSessions
            .Where(s => s.CourseId == courseId && s.UserId == userId)
            .OrderByDescending(s => s.LastMessageAt ?? s.StartedAt)
            .ToListAsync();
    }

    public async Task<List<ChatMessage>> GetMessagesAsync(int sessionId)
    {
        return await _db.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<ChatMessage> AddMessageAsync(int sessionId, MessageRole role, string content,
        string? sourcesJson = null, int? responseTimeMs = null)
    {
        var message = new ChatMessage
        {
            SessionId = sessionId,
            Role = role,
            Content = content,
            SourcesJson = sourcesJson,
            ResponseTimeMs = responseTimeMs,
            CreatedAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);

        // Update session's last message time
        var session = await _db.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastMessageAt = DateTime.UtcNow;
            // Auto-title from first user message
            if (string.IsNullOrEmpty(session.Title) && role == MessageRole.User)
            {
                session.Title = content.Length > 100 ? content[..100] + "..." : content;
            }
        }

        await _db.SaveChangesAsync();
        return message;
    }

    public async Task DeleteSessionAsync(int sessionId)
    {
        var session = await _db.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            _db.ChatSessions.Remove(session);
            await _db.SaveChangesAsync();
        }
    }
}
