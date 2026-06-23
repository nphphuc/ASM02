using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public enum ExperimentType
{
    RAG_VS_FINETUNING,
    CHUNKING_STRATEGY,
    EMBEDDING_MODEL
}

public class BenchmarkExperiment
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public ExperimentType Type { get; set; }

    [StringLength(100)]
    public string? EmbeddingModel { get; set; }

    [StringLength(100)]
    public string? ChunkingStrategy { get; set; }

    [StringLength(100)]
    public string? Approach { get; set; } // "RAG" or "FineTuned"

    public int CourseId { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    public bool IsCompleted { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<BenchmarkResult> Results { get; set; } = new List<BenchmarkResult>();
}
