using ChatbotStudent.Models;

namespace ChatbotStudent.Services;

public class RagResult
{
    public string Answer { get; set; } = string.Empty;
    public List<SourceReference> Sources { get; set; } = new();
    public int ResponseTimeMs { get; set; }
}

public class SourceReference
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string ChunkContent { get; set; } = string.Empty;
    public int ChunkPosition { get; set; }
    public double Score { get; set; }
}

public interface IRagService
{
    /// <summary>
    /// Generate an answer using RAG for a given question within a course.
    /// </summary>
    Task<RagResult> QueryAsync(string question, int courseId, string? embeddingModel = null);

    /// <summary>
    /// Retrieve relevant chunks without generating an answer (for benchmarking).
    /// </summary>
    Task<List<(DocumentChunk Chunk, double Score)>> RetrieveAsync(
        string query, int courseId, int topK = 5, string? embeddingModel = null);
}
