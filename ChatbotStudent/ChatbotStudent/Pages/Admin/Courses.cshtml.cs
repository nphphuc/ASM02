using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CoursesModel : PageModel
{
    private readonly ICourseService _courseService;
    private readonly IUserService _userService;
    private readonly ILogger<CoursesModel> _logger;

    public CoursesModel(ICourseService courseService, IUserService userService, ILogger<CoursesModel> logger)
    {
        _courseService = courseService;
        _userService = userService;
        _logger = logger;
    }

    public List<Course> Courses { get; set; } = new();
    public List<User> Lecturers { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Courses = await _courseService.GetAllCoursesAsync();
        Lecturers = await _userService.GetUsersByRoleAsync(UserRole.Lecturer);

        if (TempData["SuccessMessage"] is string sm) SuccessMessage = sm;
        if (TempData["ErrorMessage"] is string em) ErrorMessage = em;
    }

    public async Task<IActionResult> OnPostCreateCourseAsync(string name, string? code, string? description, int? lecturerId)
    {
        var course = await _courseService.CreateCourseAsync(name, code, description, lecturerId);

        // If lecturer specified, enroll them too
        if (lecturerId.HasValue)
        {
            await _userService.EnrollStudentInCourseAsync(lecturerId.Value, course.Id);
        }

        TempData["SuccessMessage"] = $"Đã tạo môn học {name} thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCourseAsync(int courseId)
    {
        var success = await _courseService.DeleteCourseAsync(courseId);
        if (success)
        {
            TempData["SuccessMessage"] = $"Đã xóa môn học.";
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignLecturerAsync(int courseId, int? lecturerId)
    {
        await _courseService.AssignLecturerAsync(courseId, lecturerId);
        if (lecturerId.HasValue)
            TempData["SuccessMessage"] = "Đã gán giảng viên cho môn học.";
        else
            TempData["SuccessMessage"] = "Đã gỡ giảng viên khỏi môn học.";
        return RedirectToPage();
    }
}
