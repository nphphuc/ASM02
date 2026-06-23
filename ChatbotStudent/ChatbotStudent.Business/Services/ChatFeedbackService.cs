using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public interface IChatFeedbackService
{
    Task<bool> SubmitFeedbackAsync(int messageId, int userId, FeedbackType feedback, string? comment);
}

public class ChatFeedbackService : IChatFeedbackService
{
    private readonly AppDbContext _db;

    public ChatFeedbackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> SubmitFeedbackAsync(int messageId, int userId, FeedbackType feedback, string? comment)
    {
        var existing = await _db.ChatMessageFeedbacks
            .FirstOrDefaultAsync(f => f.MessageId == messageId && f.UserId == userId);

        if (existing != null)
        {
            existing.Feedback = feedback;
            if (!string.IsNullOrEmpty(comment))
                existing.Comment = comment;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ChatMessageFeedbacks.Add(new ChatMessageFeedback
            {
                MessageId = messageId,
                UserId = userId,
                Feedback = feedback,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
