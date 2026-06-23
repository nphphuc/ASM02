using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Models;

public enum FeedbackType
{
    ThumbsUp = 1,
    ThumbsDown = -1
}

public class ChatMessageFeedback
{
    public int Id { get; set; }

    public int MessageId { get; set; }

    public int UserId { get; set; }

    public FeedbackType Feedback { get; set; }

    /// <summary>
    /// Optional detailed feedback text when user thumbs down
    /// </summary>
    [StringLength(2000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ChatMessage Message { get; set; } = null!;
    public User User { get; set; } = null!;
}
