using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public class BenchmarkResult
{
    public int Id { get; set; }

    public int ExperimentId { get; set; }

    [Required]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string GroundTruth { get; set; } = string.Empty;

    public string? GeneratedAnswer { get; set; }

    /// <summary>
    /// JSON-serialized list of retrieved context chunks
    /// </summary>
    public string? RetrievedContext { get; set; }

    // RAGAS Metrics
    public double? Faithfulness { get; set; }
    public double? AnswerRelevance { get; set; }
    public double? ContextPrecision { get; set; }
    public double? ContextRecall { get; set; }

    // Additional metrics
    public double? CosineSimilarity { get; set; }
    public int? ResponseTimeMs { get; set; }

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public BenchmarkExperiment Experiment { get; set; } = null!;
}
