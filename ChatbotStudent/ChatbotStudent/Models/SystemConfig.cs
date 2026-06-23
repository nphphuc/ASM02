using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Models;

public class SystemConfig
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    public string Value { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string Category { get; set; } = "General";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int? UpdatedByUserId { get; set; }
}

public static class SystemConfigDefaults
{
    public const string ChunkSize = "ChunkSize";
    public const string ChunkOverlap = "ChunkOverlap";
    public const string TopKChunks = "TopKChunks";
    public const string SimilarityThreshold = "SimilarityThreshold";
    public const string Temperature = "Temperature";
    public const string MaxTokens = "MaxTokens";
    public const string DefaultEmbeddingModel = "DefaultEmbeddingModel";
    public const string DefaultChunkingStrategy = "DefaultChunkingStrategy";
    public const string StudentRateLimitPerMinute = "StudentRateLimitPerMinute";
    public const string LecturerRateLimitPerMinute = "LecturerRateLimitPerMinute";
    public const string ChatHistoryRetentionDays = "ChatHistoryRetentionDays";
    public const string EnableStreaming = "EnableStreaming";
    public const string EnableSourceCitations = "EnableSourceCitations";

    public static List<(string Key, string DefaultValue, string Description, string Category)> GetDefaults() => new()
    {
        (ChunkSize, "512", "Kích thước chunk (tokens)", "Chunking"),
        (ChunkOverlap, "50", "Số token chồng lấp giữa các chunk", "Chunking"),
        (TopKChunks, "5", "Số chunk truy xuất cho mỗi câu hỏi", "RAG"),
        (SimilarityThreshold, "0.5", "Ngưỡng tương đồng tối thiểu", "RAG"),
        (Temperature, "0.3", "Độ sáng tạo của câu trả lời (0.0 - 1.0)", "Chat"),
        (MaxTokens, "2048", "Số token tối đa cho mỗi câu trả lời", "Chat"),
        (DefaultEmbeddingModel, "OpenAI", "Model embedding mặc định", "Embedding"),
        (DefaultChunkingStrategy, "Recursive", "Chiến lược chunking mặc định", "Chunking"),
        (StudentRateLimitPerMinute, "30", "Giới hạn request/phút cho sinh viên", "Rate Limit"),
        (LecturerRateLimitPerMinute, "100", "Giới hạn request/phút cho giảng viên", "Rate Limit"),
        (ChatHistoryRetentionDays, "365", "Số ngày lưu trữ lịch sử chat", "Privacy"),
        (EnableStreaming, "true", "Bật streaming câu trả lời", "Chat"),
        (EnableSourceCitations, "true", "Hiển thị trích dẫn nguồn", "Chat"),
    };
}
