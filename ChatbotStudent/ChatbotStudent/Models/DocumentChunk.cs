using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Models;

public class DocumentChunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int Position { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [StringLength(200)]
    public string? ChunkingStrategy { get; set; }

    public int TokenCount { get; set; }

    /// <summary>
    /// JSON-serialized float array representing the embedding vector.
    /// Stored as string since SQL Server doesn't natively support vector types in EF Core.
    /// </summary>
    public string? EmbeddingJson { get; set; }

    [StringLength(200)]
    public string? EmbeddingModel { get; set; }

    // Navigation property
    public Document Document { get; set; } = null!;
}
