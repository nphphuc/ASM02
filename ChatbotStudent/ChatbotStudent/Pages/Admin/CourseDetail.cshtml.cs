using ChatbotStudent.Data;
using ChatbotStudent.Models;
using ChatbotStudent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CourseDetailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IUserService _userService;
    private readonly ILogger<CourseDetailModel> _logger;

    public CourseDetailModel(AppDbContext db, IUserService userService, ILogger<CourseDetailModel> logger)
    {
        _db = db;
        _userService = userService;
        _logger = logger;
    }

    public Course? Course { get; set; }
    public List<User> Students { get; set; } = new();
    public List<User> AvailableStudents { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync(int courseId)
    {
        Course = await _db.Courses
            .Include(c => c.Documents)
            .Include(c => c.ChatSessions)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (Course == null) return;

        var enrolledIds = await _db.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.UserId)
            .ToListAsync();

        Students = await _userService.GetUsersByRoleAsync(UserRole.Student);
        Students = Students.Where(s => enrolledIds.Contains(s.Id)).ToList();

        AvailableStudents = await _userService.GetStudentsNotEnrolledAsync(courseId);
    }

    public async Task<IActionResult> OnPostAddStudentAsync(int courseId, int userId)
    {
        var success = await _userService.EnrollStudentInCourseAsync(userId, courseId);
        SuccessMessage = success ? "Đã thêm sinh viên vào lớp." : "Sinh viên đã có trong lớp.";
        return RedirectToPage(new { courseId });
    }

    public async Task<IActionResult> OnPostRemoveStudentAsync(int courseId, int userId)
    {
        await _userService.RemoveStudentFromCourseAsync(userId, courseId);
        SuccessMessage = "Đã xóa sinh viên khỏi lớp.";
        return RedirectToPage(new { courseId });
    }
}
