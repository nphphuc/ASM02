using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public class ChatSession
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    public int UserId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastMessageAt { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
