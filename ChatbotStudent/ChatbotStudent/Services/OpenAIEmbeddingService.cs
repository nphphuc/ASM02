using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatbotStudent.Services;

public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public string ModelName => _options.EmbeddingModel;

    public OpenAIEmbeddingService(
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<OpenAiOptions> options,
        ILogger<OpenAIEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var request = new
        {
            model = _options.EmbeddingModel,
            input = text
        };

        var response = await _httpClient.PostAsJsonAsync("/embeddings", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
        if (result?.Data == null || result.Data.Length == 0)
            throw new Exception("No embedding returned from OpenAI");

        return result.Data[0].Embedding!;
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts)
    {
        var embeddings = new List<float[]>();

        // Process in batches of 20 (OpenAI limit per request)
        const int batchSize = 20;
        for (var i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();
            var request = new
            {
                model = _options.EmbeddingModel,
                input = batch
            };

            var response = await _httpClient.PostAsJsonAsync("/embeddings", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
            if (result?.Data != null)
            {
                // Sort by index to maintain order
                var sorted = result.Data.OrderBy(d => d.Index).ToArray();
                embeddings.AddRange(sorted.Select(d => d.Embedding!));
            }

            // Rate limiting - small delay between batches
            if (i + batchSize < texts.Count)
                await Task.Delay(500);
        }

        _logger.LogInformation("Generated {Count} embeddings using {Model}",
            embeddings.Count, ModelName);
        return embeddings;
    }

    public double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have the same length");

        double dotProduct = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB) + 1e-10);
    }

    // JSON response models
    private class EmbeddingResponse
    {
        [JsonPropertyName("data")]
        public EmbeddingData[]? Data { get; set; }
    }

    private class EmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
