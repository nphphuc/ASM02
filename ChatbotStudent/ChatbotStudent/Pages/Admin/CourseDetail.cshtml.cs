using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CourseDetailModel : PageModel
{
    private readonly ICourseService _courseService;
    private readonly IUserService _userService;
    private readonly ILogger<CourseDetailModel> _logger;

    public CourseDetailModel(ICourseService courseService, IUserService userService, ILogger<CourseDetailModel> logger)
    {
        _courseService = courseService;
        _userService = userService;
        _logger = logger;
    }

    public Course? Course { get; set; }
    public List<User> Students { get; set; } = new();
    public List<User> AvailableStudents { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync(int courseId)
    {
        Course = await _courseService.GetCourseDetailAsync(courseId);

        if (Course == null) return;

        Students = await _userService.GetUsersByRoleAsync(UserRole.Student);
        var enrolledIds = Course.Enrollments.Select(e => e.UserId).ToList();
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
