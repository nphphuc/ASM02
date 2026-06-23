namespace ChatbotStudent.Services;

public interface IEmbeddingService
{
    /// <summary>
    /// Generate embedding vector for a single text.
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text);

    /// <summary>
    /// Generate embedding vectors for multiple texts in batch.
    /// </summary>
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts);

    /// <summary>
    /// Get the name of the embedding model being used.
    /// </summary>
    string ModelName { get; }

    /// <summary>
    /// Compute cosine similarity between two vectors.
    /// </summary>
    double CosineSimilarity(float[] a, float[] b);
}
