using ChatbotStudent.Models;

namespace ChatbotStudent.Services;

public interface IBenchmarkService
{
    /// <summary>
    /// Run a benchmark experiment with the specified configuration.
    /// </summary>
    Task<BenchmarkExperiment> RunExperimentAsync(
        int courseId,
        string name,
        ExperimentType type,
        string? embeddingModel,
        string? chunkingStrategy,
        string? approach);

    /// <summary>
    /// Get all experiments for a course.
    /// </summary>
    Task<List<BenchmarkExperiment>> GetExperimentsAsync(int courseId);

    /// <summary>
    /// Get detailed results for an experiment.
    /// </summary>
    Task<BenchmarkExperiment?> GetExperimentWithResultsAsync(int experimentId);

    /// <summary>
    /// Compute RAGAS-like metrics for a single result.
    /// </summary>
    Task ComputeMetricsAsync(BenchmarkResult result);

    /// <summary>
    /// Get benchmark summary statistics for comparison.
    /// </summary>
    Task<List<BenchmarkSummary>> GetSummaryAsync(int courseId);
}

public class BenchmarkSummary
{
    public int ExperimentId { get; set; }
    public string ExperimentName { get; set; } = string.Empty;
    public string? Approach { get; set; }
    public string? EmbeddingModel { get; set; }
    public string? ChunkingStrategy { get; set; }
    public double AvgFaithfulness { get; set; }
    public double AvgAnswerRelevance { get; set; }
    public double AvgContextPrecision { get; set; }
    public double AvgContextRecall { get; set; }
    public double AvgResponseTimeMs { get; set; }
    public int TotalQuestions { get; set; }
}
