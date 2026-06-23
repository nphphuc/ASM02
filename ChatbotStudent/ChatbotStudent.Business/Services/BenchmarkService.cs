using System.Diagnostics;
using System.Text.Json;
using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChatbotStudent.Business.Services;

public class BenchmarkService : IBenchmarkService
{
    private readonly AppDbContext _db;
    private readonly IRagService _ragService;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILocalEmbeddingService? _localEmbeddingService;
    private readonly IOpenAiChatClient _chatClient;
    private readonly ILogger<BenchmarkService> _logger;

    public BenchmarkService(
        AppDbContext db,
        IRagService ragService,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        ILocalEmbeddingService? localEmbeddingService,
        IOpenAiChatClient chatClient,
        ILogger<BenchmarkService> logger)
    {
        _db = db;
        _ragService = ragService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _localEmbeddingService = localEmbeddingService;
        _chatClient = chatClient;
        _logger = logger;
    }

    public async Task<BenchmarkExperiment> RunExperimentAsync(
        int courseId,
        string name,
        ExperimentType type,
        string? embeddingModel,
        string? chunkingStrategy,
        string? approach)
    {
        var experiment = new BenchmarkExperiment
        {
            Name = name,
            Type = type,
            EmbeddingModel = embeddingModel,
            ChunkingStrategy = chunkingStrategy,
            Approach = approach,
            CourseId = courseId,
            StartedAt = DateTime.UtcNow,
            IsCompleted = false
        };

        _db.BenchmarkExperiments.Add(experiment);
        await _db.SaveChangesAsync();

        var questions = await _db.BenchmarkQuestions
            .Where(q => q.CourseId == courseId)
            .ToListAsync();

        if (questions.Count == 0)
        {
            _logger.LogWarning("No benchmark questions found for course {CourseId}", courseId);
            experiment.IsCompleted = true;
            experiment.CompletedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return experiment;
        }

        foreach (var question in questions)
        {
            var result = new BenchmarkResult
            {
                ExperimentId = experiment.Id,
                Question = question.Question,
                GroundTruth = question.GroundTruth
            };

            try
            {
                var sw = Stopwatch.StartNew();
                var ragResult = await _ragService.QueryAsync(question.Question, courseId, embeddingModel);
                sw.Stop();

                result.GeneratedAnswer = ragResult.Answer;
                result.RetrievedContext = JsonSerializer.Serialize(
                    ragResult.Sources.Select(s => new
                    {
                        s.DocumentName,
                        s.ChunkContent,
                        s.Score
                    }));
                result.ResponseTimeMs = (int)sw.ElapsedMilliseconds;

                await ComputeMetricsAsync(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating question: {Question}", question.Question);
                result.GeneratedAnswer = $"Error: {ex.Message}";
            }

            _db.BenchmarkResults.Add(result);
        }

        experiment.IsCompleted = true;
        experiment.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Completed experiment {Name} with {Count} questions",
            name, questions.Count);

        return experiment;
    }

    public async Task<List<BenchmarkExperiment>> GetExperimentsAsync(int courseId)
    {
        return await _db.BenchmarkExperiments
            .Where(e => e.CourseId == courseId)
            .OrderByDescending(e => e.StartedAt)
            .ToListAsync();
    }

    public async Task<BenchmarkExperiment?> GetExperimentWithResultsAsync(int experimentId)
    {
        return await _db.BenchmarkExperiments
            .Include(e => e.Results)
            .FirstOrDefaultAsync(e => e.Id == experimentId);
    }

    public async Task ComputeMetricsAsync(BenchmarkResult result)
    {
        if (string.IsNullOrEmpty(result.GeneratedAnswer) || string.IsNullOrEmpty(result.GroundTruth))
            return;

        try
        {
            result.Faithfulness = await ComputeMetricAsync(
                "Bạn là evaluator. Đánh giá câu trả lời có được hỗ trợ bởi ngữ cảnh đã truy xuất không. Trả về MỘT số từ 0.0 đến 1.0.",
                $"Ngữ cảnh: {result.RetrievedContext}\nCâu trả lời: {result.GeneratedAnswer}");

            result.AnswerRelevance = await ComputeMetricAsync(
                "Bạn là evaluator. Đánh giá câu trả lời có liên quan đến câu hỏi không. Trả về MỘT số từ 0.0 đến 1.0.",
                $"Câu hỏi: {result.Question}\nCâu trả lời: {result.GeneratedAnswer}");

            result.ContextPrecision = await ComputeMetricAsync(
                "Bạn là evaluator. Đánh giá ngữ cảnh truy xuất có chứa thông tin liên quan để trả lời câu hỏi không. Trả về MỘT số từ 0.0 đến 1.0.",
                $"Câu hỏi: {result.Question}\nNgữ cảnh: {result.RetrievedContext}");

            result.ContextRecall = await ComputeMetricAsync(
                "Bạn là evaluator. Đánh giá ngữ cảnh truy xuất có chứa đủ thông tin so với câu trả lời đúng không. Trả về MỘT số từ 0.0 đến 1.0.",
                $"Câu trả lời đúng: {result.GroundTruth}\nNgữ cảnh: {result.RetrievedContext}");

            var genEmbedding = await _embeddingService.GenerateEmbeddingAsync(result.GeneratedAnswer);
            var truthEmbedding = await _embeddingService.GenerateEmbeddingAsync(result.GroundTruth);
            result.CosineSimilarity = _embeddingService.CosineSimilarity(genEmbedding, truthEmbedding);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error computing metrics");
        }
    }

    private async Task<double> ComputeMetricAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            var response = await _chatClient.ChatAsync(systemPrompt, userPrompt);

            var cleaned = new string(response.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            cleaned = cleaned.Replace(',', '.');

            if (double.TryParse(cleaned, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var score))
            {
                return Math.Clamp(score, 0.0, 1.0);
            }

            return 0.5;
        }
        catch
        {
            return 0.5;
        }
    }

    public async Task<List<BenchmarkSummary>> GetSummaryAsync(int courseId)
    {
        var experiments = await _db.BenchmarkExperiments
            .Include(e => e.Results)
            .Where(e => e.CourseId == courseId && e.IsCompleted)
            .ToListAsync();

        return experiments.Select(e => new BenchmarkSummary
        {
            ExperimentId = e.Id,
            ExperimentName = e.Name,
            Approach = e.Approach,
            EmbeddingModel = e.EmbeddingModel,
            ChunkingStrategy = e.ChunkingStrategy,
            TotalQuestions = e.Results.Count,
            AvgFaithfulness = e.Results.Any() ? e.Results.Average(r => r.Faithfulness ?? 0) : 0,
            AvgAnswerRelevance = e.Results.Any() ? e.Results.Average(r => r.AnswerRelevance ?? 0) : 0,
            AvgContextPrecision = e.Results.Any() ? e.Results.Average(r => r.ContextPrecision ?? 0) : 0,
            AvgContextRecall = e.Results.Any() ? e.Results.Average(r => r.ContextRecall ?? 0) : 0,
            AvgResponseTimeMs = e.Results.Any() ? e.Results.Average(r => r.ResponseTimeMs ?? 0) : 0
        }).ToList();
    }
}
