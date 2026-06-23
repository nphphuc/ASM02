using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ChatbotStudent.Business.Services;

public interface ILocalEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, string modelName);
    Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, string modelName);
    string ModelName { get; }
}

public class LocalEmbeddingService : ILocalEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LocalEmbeddingService> _logger;

    public string ModelName { get; private set; } = "local";

    public LocalEmbeddingService(HttpClient httpClient, ILogger<LocalEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, string modelName)
    {
        var request = new { model = modelName, input = text };
        var response = await _httpClient.PostAsJsonAsync("/embed", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LocalEmbeddingResponse>();
        return result?.Embedding ?? Array.Empty<float>();
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, string modelName)
    {
        var request = new { model = modelName, input = texts };
        var response = await _httpClient.PostAsJsonAsync("/embed_batch", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LocalBatchEmbeddingResponse>();
        return result?.Embeddings ?? new List<float[]>();
    }

    private class LocalEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }

    private class LocalBatchEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public List<float[]>? Embeddings { get; set; }
    }
}
