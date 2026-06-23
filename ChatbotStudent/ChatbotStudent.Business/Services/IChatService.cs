using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

public interface IChatService
{
    Task<ChatSession> CreateSessionAsync(int courseId, int userId, string? title = null);
    Task<ChatSession?> GetSessionAsync(int sessionId);
    Task<List<ChatSession>> GetSessionsByCourseAsync(int courseId);
    Task<List<ChatSession>> GetUserSessionsByCourseAsync(int courseId, int userId);
    Task<List<ChatMessage>> GetMessagesAsync(int sessionId);
    Task<ChatMessage> AddMessageAsync(int sessionId, MessageRole role, string content,
        string? sourcesJson = null, int? responseTimeMs = null);
    Task DeleteSessionAsync(int sessionId);
}
