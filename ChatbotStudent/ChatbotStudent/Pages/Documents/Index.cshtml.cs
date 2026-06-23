using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Documents;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IDocumentManagementService _documentService;
    private readonly ICourseService _courseService;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IDocumentManagementService documentService,
        ICourseService courseService,
        IWebHostEnvironment env,
        ILogger<IndexModel> logger)
    {
        _documentService = documentService;
        _courseService = courseService;
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

        // Filter courses by role using ICourseService
        if (isAdmin)
        {
            Courses = await _courseService.GetAllCoursesAsync();
        }
        else if (isLecturer)
        {
            Courses = await _courseService.GetCoursesByLecturerAsync(userId);
        }
        else
        {
            Courses = await _courseService.GetCoursesByUserRoleAsync(userId, "Student");
        }

        Documents = await _documentService.GetDocumentsByCourseAsync(courseId, userId,
            isAdmin ? "Admin" : isLecturer ? "Lecturer" : "Student");
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
            // Save file to disk first
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var safeFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, safeFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Process through DocumentManagementService
            using var fileStream = file.OpenReadStream();
            var role = User.IsInRole("Admin") ? "Admin" : "Lecturer";
            await _documentService.UploadDocumentAsync(
                courseId, chapter, chunkingStrategy, embeddingModel,
                file.FileName, file.Length, fileStream, role);
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

        await _documentService.DeleteDocumentAsync(documentId);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetChunksJsonAsync(int documentId)
    {
        var chunks = await _documentService.GetChunksJsonAsync(documentId);
        return new JsonResult(chunks);
    }
}
