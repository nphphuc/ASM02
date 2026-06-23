using System.Text.Json;
using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public class DocumentManagementService : IDocumentManagementService
{
    private readonly AppDbContext _db;
    private readonly IDocumentParserService _parser;
    private readonly IChunkingService _chunking;
    private readonly IEmbeddingService _embedding;
    private readonly ILocalEmbeddingService? _localEmbedding;
    private readonly ILogger<DocumentManagementService> _logger;

    public DocumentManagementService(
        AppDbContext db,
        IDocumentParserService parser,
        IChunkingService chunking,
        IEmbeddingService embedding,
        ILocalEmbeddingService? localEmbedding,
        ILogger<DocumentManagementService> logger)
    {
        _db = db;
        _parser = parser;
        _chunking = chunking;
        _embedding = embedding;
        _localEmbedding = localEmbedding;
        _logger = logger;
    }

    public async Task<List<Document>> GetDocumentsByCourseAsync(int? courseId, int userId, string role)
    {
        var query = _db.Documents
            .Include(d => d.Course)
            .Include(d => d.Chunks)
            .Where(d => !d.IsDeleted && d.IsLatestVersion)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(d => d.CourseId == courseId.Value);

        if (role != "Admin" && role != "Lecturer")
        {
            var enrolledCourseIds = await _db.CourseEnrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();
            query = query.Where(d => enrolledCourseIds.Contains(d.CourseId));
        }

        return await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
    }

    public async Task<Document> UploadDocumentAsync(
        int courseId, string? chapter, string chunkingStrategy, string embeddingModel,
        string fileName, long fileSize, Stream fileStream, string uploadedByRole)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var docType = ext switch
        {
            ".pdf" => Data.Models.DocumentType.PDF,
            ".docx" => Data.Models.DocumentType.DOCX,
            ".pptx" => Data.Models.DocumentType.PPTX,
            _ => throw new NotSupportedException($"File type '{ext}' is not supported.")
        };

        var document = new Document
        {
            FileName = fileName,
            FileType = docType,
            FileSize = fileSize,
            Chapter = chapter,
            CourseId = courseId,
            Status = ProcessingStatus.Processing,
            UploadedAt = DateTime.UtcNow
        };
        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        try
        {
            var text = await _parser.ExtractTextAsync(fileStream, fileName);

            if (string.IsNullOrWhiteSpace(text))
            {
                document.Status = ProcessingStatus.Failed;
                document.ErrorMessage = "Không trích xuất được nội dung từ file.";
                await _db.SaveChangesAsync();
                return document;
            }

            var chunks = _chunking.ChunkText(text, chunkingStrategy);
            var chunkContents = chunks.Select(c => c.Content).ToList();

            List<float[]> embeddings;

            if (embeddingModel == "OpenAI")
            {
                embeddings = await _embedding.GenerateEmbeddingsAsync(chunkContents);
            }
            else if (_localEmbedding != null)
            {
                embeddings = await _localEmbedding.GenerateEmbeddingsAsync(chunkContents, embeddingModel);
            }
            else
            {
                embeddings = chunkContents.Select(_ =>
                {
                    var rng = new Random();
                    return Enumerable.Range(0, 384).Select(_ => (float)rng.NextDouble()).ToArray();
                }).ToList();
            }

            for (var i = 0; i < chunks.Count; i++)
            {
                _db.DocumentChunks.Add(new DocumentChunk
                {
                    DocumentId = document.Id,
                    Position = chunks[i].Position,
                    Content = chunks[i].Content,
                    ChunkingStrategy = chunkingStrategy,
                    TokenCount = chunks[i].TokenCount,
                    EmbeddingJson = JsonSerializer.Serialize(embeddings[i]),
                    EmbeddingModel = embeddingModel
                });
            }

            document.Status = ProcessingStatus.Completed;
            document.ProcessedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Processed document {FileName}: {ChunkCount} chunks", fileName, chunks.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document {FileName}", fileName);
            document.Status = ProcessingStatus.Failed;
            document.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
        }

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId)
    {
        var doc = await _db.Documents.FindAsync(documentId);
        if (doc == null) return false;

        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<object?> GetChunksJsonAsync(int documentId)
    {
        return await _db.DocumentChunks
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.Position)
            .Select(c => new
            {
                c.Id,
                c.Position,
                c.Content,
                c.ChunkingStrategy,
                c.TokenCount,
                c.EmbeddingModel
            })
            .ToListAsync();
    }
}
