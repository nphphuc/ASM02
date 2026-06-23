using System.Text.Json;
using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Chat;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IChatService _chatService;
    private readonly IRagService _ragService;
    private readonly ICourseService _courseService;

    public IndexModel(IChatService chatService, IRagService ragService, ICourseService courseService)
    {
        _chatService = chatService;
        _ragService = ragService;
        _courseService = courseService;
    }

    public List<Course> Courses { get; set; } = new();
    public List<ChatSession> Sessions { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
    public ChatSession? CurrentSession { get; set; }
    public int? SelectedCourseId { get; set; }
    public int? CurrentSessionId { get; set; }

    public async Task OnGetAsync(int? courseId = null, int? sessionId = null)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var isAdmin = User.IsInRole("Admin");
        var isLecturer = User.IsInRole("Lecturer");

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

        if (sessionId.HasValue)
        {
            CurrentSession = await _chatService.GetSessionAsync(sessionId.Value);
            // Ownership check: ensure session belongs to current user (unless admin)
            if (CurrentSession != null && (CurrentSession.UserId == userId || isAdmin))
            {
                SelectedCourseId = CurrentSession.CourseId;
                CurrentSessionId = sessionId;
                Sessions = await _chatService.GetUserSessionsByCourseAsync(CurrentSession.CourseId, userId);
                Messages = await _chatService.GetMessagesAsync(sessionId.Value);
            }
            else
            {
                CurrentSession = null;
            }
        }
        else if (courseId.HasValue && Courses.Any(c => c.Id == courseId.Value))
        {
            SelectedCourseId = courseId;
            Sessions = await _chatService.GetUserSessionsByCourseAsync(courseId.Value, userId);
        }
    }

    public async Task<IActionResult> OnPostCreateSessionAsync(int courseId)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var session = await _chatService.CreateSessionAsync(courseId, userId);
        return new JsonResult(new { sessionId = session.Id });
    }
}
