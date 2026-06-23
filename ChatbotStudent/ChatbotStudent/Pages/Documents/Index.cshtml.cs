using ChatbotStudent.Data;
using ChatbotStudent.Models;
using ChatbotStudent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IDocumentParserService _parser;
    private readonly IChunkingService _chunking;
    private readonly IEmbeddingService _embedding;
    private readonly ILocalEmbeddingService? _localEmbedding;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        AppDbContext db,
        IDocumentParserService parser,
        IChunkingService chunking,
        IEmbeddingService embedding,
        ILocalEmbeddingService? localEmbedding,
        IWebHostEnvironment env,
        ILogger<IndexModel> logger)
    {
        _db = db;
        _parser = parser;
        _chunking = chunking;
        _embedding = embedding;
        _localEmbedding = localEmbedding;
        _env = env;
        _logger = logger;
    }

    public List<Document> Documents { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public int TotalChunks { get; set; }
    public int? SelectedCourseId { get; set; }

    public async Task OnGetAsync(int? courseId = null)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");
        var isLecturer = User.IsInRole("Lecturer");

        SelectedCourseId = courseId;

        // Filter courses by role
        if (isAdmin)
        {
            Courses = await _db.Courses.Where(c => !c.IsDeleted).ToListAsync();
        }
        else if (isLecturer)
        {
            Courses = await _db.Courses.Where(c => c.LecturerId == userId && !c.IsDeleted).ToListAsync();
        }
        else
        {
            var enrolledCourseIds = await _db.CourseEnrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();
            Courses = await _db.Courses
                .Where(c => enrolledCourseIds.Contains(c.Id) && !c.IsDeleted)
                .ToListAsync();
        }

        var query = _db.Documents
            .Include(d => d.Course)
            .Include(d => d.Chunks)
            .Where(d => !d.IsDeleted && d.IsLatestVersion)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(d => d.CourseId == courseId.Value);

        // For students, only show documents from enrolled courses
        if (!isAdmin && !isLecturer)
        {
            var enrolledCourseIds = await _db.CourseEnrollments
                .Where(e => e.UserId == userId)
                .Select(e => e.CourseId)
                .ToListAsync();
            query = query.Where(d => enrolledCourseIds.Contains(d.CourseId));
        }

        Documents = await query.OrderByDescending(d => d.UploadedAt).ToListAsync();
        TotalChunks = Documents.Sum(d => d.Chunks.Count);
    }

    public async Task<IActionResult> OnPostUploadAsync(
        int courseId, string? chapter, string chunkingStrategy, string embeddingModel,
        List<IFormFile> files)
    {
        // Only Admin/Lecturer can upload
        if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer"))
        {
            TempData["Error"] = "Bạn không có quyền upload tài liệu.";
            return RedirectToPage();
        }

        if (files == null || files.Count == 0)
        {
            TempData["Error"] = "Vui lòng chọn file để upload.";
            return RedirectToPage();
        }

        // Ensure upload directory exists
        var uploadDir = Path.Combine(_env.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadDir);

        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var docType = ext switch
            {
                ".pdf" => DocumentType.PDF,
                ".docx" => DocumentType.DOCX,
                ".pptx" => DocumentType.PPTX,
                _ => throw new NotSupportedException($"File type '{ext}' is not supported.")
            };

            // Save file to disk
            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, safeFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Create document record
            var document = new Document
            {
                FileName = file.FileName,
                FileType = docType,
                FileSize = file.Length,
                Chapter = chapter,
                CourseId = courseId,
                Status = ProcessingStatus.Processing,
                UploadedAt = DateTime.UtcNow
            };
            _db.Documents.Add(document);
            await _db.SaveChangesAsync();

            try
            {
                // Extract text
                using var fileStream = file.OpenReadStream();
                var text = await _parser.ExtractTextAsync(fileStream, file.FileName);

                if (string.IsNullOrWhiteSpace(text))
                {
                    document.Status = ProcessingStatus.Failed;
                    document.ErrorMessage = "Không trích xuất được nội dung từ file.";
                    await _db.SaveChangesAsync();
                    continue;
                }

                // Chunk text
                var chunks = _chunking.ChunkText(text, chunkingStrategy);

                // Generate embeddings
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
                    // Fallback: generate random embeddings for demo
                    embeddings = chunkContents.Select(_ =>
                    {
                        var rng = new Random();
                        return Enumerable.Range(0, 384).Select(_ => (float)rng.NextDouble()).ToArray();
                    }).ToList();
                }

                // Save chunks with embeddings
                for (var i = 0; i < chunks.Count; i++)
                {
                    var chunk = new DocumentChunk
                    {
                        DocumentId = document.Id,
                        Position = chunks[i].Position,
                        Content = chunks[i].Content,
                        ChunkingStrategy = chunkingStrategy,
                        TokenCount = chunks[i].TokenCount,
                        EmbeddingJson = System.Text.Json.JsonSerializer.Serialize(embeddings[i]),
                        EmbeddingModel = embeddingModel
                    };
                    _db.DocumentChunks.Add(chunk);
                }

                document.Status = ProcessingStatus.Completed;
                document.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Processed document {FileName}: {ChunkCount} chunks",
                    file.FileName, chunks.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing document {FileName}", file.FileName);
                document.Status = ProcessingStatus.Failed;
                document.ErrorMessage = ex.Message;
                await _db.SaveChangesAsync();
            }
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int documentId)
    {
        // Only Admin/Lecturer can delete
        if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer"))
        {
            TempData["Error"] = "Bạn không có quyền xóa tài liệu.";
            return RedirectToPage();
        }

        var doc = await _db.Documents.FindAsync(documentId);
        if (doc != null)
        {
            _db.Documents.Remove(doc);
            await _db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetChunksJsonAsync(int documentId)
    {
        var chunks = await _db.DocumentChunks
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

        return new JsonResult(chunks);
    }
}
