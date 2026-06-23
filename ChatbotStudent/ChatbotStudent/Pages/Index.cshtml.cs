using ChatbotStudent.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db) => _db = db;

    public int CourseCount { get; set; }
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public int SessionCount { get; set; }

    public string? RegistrationSuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        CourseCount = await _db.Courses.CountAsync();
        DocumentCount = await _db.Documents.CountAsync();
        ChunkCount = await _db.DocumentChunks.CountAsync();
        SessionCount = await _db.ChatSessions.CountAsync();

        // Check for registration success message
        if (TempData.ContainsKey("RegistrationSuccess"))
        {
            RegistrationSuccessMessage = TempData["RegistrationSuccess"] as string;
        }
    }
}
