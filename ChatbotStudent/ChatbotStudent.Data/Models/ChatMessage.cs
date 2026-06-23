using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public enum MessageRole
{
    User,
    Assistant,
    System
}

public class ChatMessage
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public MessageRole Role { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized list of source references for RAG answers.
    /// Format: [{"documentId":1,"documentName":"file.pdf","chunkContent":"...","score":0.95}]
    /// </summary>
    public string? SourcesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? ResponseTimeMs { get; set; }

    // Navigation property
    public ChatSession Session { get; set; } = null!;
}
