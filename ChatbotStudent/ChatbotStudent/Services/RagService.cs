using System.Text;
using System.Text.Json;
using ChatbotStudent.Data;
using ChatbotStudent.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChatbotStudent.Services;

public class RagService : IRagService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILocalEmbeddingService? _localEmbeddingService;
    private readonly IOpenAiChatClient _chatClient;
    private readonly RagSettings _ragSettings;
    private readonly ILogger<RagService> _logger;

    public RagService(
        AppDbContext db,
        IEmbeddingService embeddingService,
        ILocalEmbeddingService? localEmbeddingService,
        IOpenAiChatClient chatClient,
        IOptions<RagSettings> ragSettings,
        ILogger<RagService> logger)
    {
        _db = db;
        _embeddingService = embeddingService;
        _localEmbeddingService = localEmbeddingService;
        _chatClient = chatClient;
        _ragSettings = ragSettings.Value;
        _logger = logger;
    }

    public async Task<RagResult> QueryAsync(string question, int courseId, string? embeddingModel = null)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 1. Retrieve relevant chunks
        var retrieved = await RetrieveAsync(question, courseId, _ragSettings.TopKChunks, embeddingModel);

        // 2. Build context
        var contextBuilder = new StringBuilder();
        var sources = new List<SourceReference>();

        foreach (var (chunk, score) in retrieved)
        {
            var doc = await _db.Documents.FindAsync(chunk.DocumentId);
            var docName = doc?.FileName ?? "Unknown";

            contextBuilder.AppendLine($"--- Nguồn: {docName} (Vị trí chunk: {chunk.Position}, Điểm: {score:F3}) ---");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();

            sources.Add(new SourceReference
            {
                DocumentId = chunk.DocumentId,
                DocumentName = docName,
                ChunkContent = chunk.Content.Length > 500
                    ? chunk.Content[..500] + "..."
                    : chunk.Content,
                ChunkPosition = chunk.Position,
                Score = Math.Round(score, 4)
            });
        }

        // 3. Generate answer using LLM via IOpenAiChatClient
        var systemPrompt = @"Bạn là trợ lý học tập thông minh cho sinh viên. Nhiệm vụ của bạn là trả lời câu hỏi dựa trên tài liệu được cung cấp.

QUY TẮC:
- Chỉ sử dụng thông tin từ ngữ cảnh (tài liệu) được cung cấp để trả lời.
- Nếu thông tin không có trong tài liệu, hãy nói rõ: 'Dựa trên tài liệu được cung cấp, tôi không tìm thấy thông tin về vấn đề này.'
- Trả lời bằng tiếng Việt, rõ ràng và chi tiết.
- Luôn trích dẫn nguồn tài liệu khi trả lời.
- Định dạng câu trả lời dễ đọc.";

        var userPrompt = $@"NGỮ CẢNH TỪ TÀI LIỆU:
{contextBuilder}

CÂU HỎI: {question}

Hãy trả lời câu hỏi dựa trên ngữ cảnh trên. Nếu có thể, trích dẫn nguồn tài liệu.";

        var answer = await _chatClient.ChatAsync(systemPrompt, userPrompt);

        sw.Stop();

        return new RagResult
        {
            Answer = answer,
            Sources = sources,
            ResponseTimeMs = (int)sw.ElapsedMilliseconds
        };
    }

    public async Task<List<(DocumentChunk Chunk, double Score)>> RetrieveAsync(
        string query, int courseId, int topK = 5, string? embeddingModel = null)
    {
        // Generate query embedding
        float[] queryEmbedding;
        if (_localEmbeddingService != null)
        {
            // Use local embedding server (multilingual-e5-base) when available
            var model = !string.IsNullOrEmpty(embeddingModel) && embeddingModel != "OpenAI"
                ? embeddingModel
                : "multilingual-e5-base";
            queryEmbedding = await _localEmbeddingService.GenerateEmbeddingAsync(query, model);
        }
        else
        {
            queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
        }

        // Get all chunks for the course with their embeddings
        var chunks = await _db.DocumentChunks
            .Include(c => c.Document)
            .Where(c => c.Document.CourseId == courseId
                        && c.EmbeddingJson != null
                        && c.EmbeddingModel != null)
            .ToListAsync();

        // Compute similarity and rank
        var scored = new List<(DocumentChunk Chunk, double Score)>();

        foreach (var chunk in chunks)
        {
            var chunkEmbedding = JsonSerializer.Deserialize<float[]>(chunk.EmbeddingJson!);
            if (chunkEmbedding == null || chunkEmbedding.Length == 0) continue;

            var similarity = _embeddingService.CosineSimilarity(queryEmbedding, chunkEmbedding);
            if (similarity >= _ragSettings.SimilarityThreshold)
            {
                scored.Add((chunk, similarity));
            }
        }

        // Return top-K results sorted by score descending
        var results = scored
            .OrderByDescending(s => s.Score)
            .Take(topK)
            .ToList();

        _logger.LogInformation("Retrieved {Count} chunks for query (top {TopK}, threshold {Threshold})",
            results.Count, topK, _ragSettings.SimilarityThreshold);

        return results;
    }
}
