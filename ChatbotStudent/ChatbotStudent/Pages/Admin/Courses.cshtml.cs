using ChatbotStudent.Data;
using ChatbotStudent.Models;
using ChatbotStudent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages.Admin;

[Authorize(Roles = "Admin")]
public class CoursesModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IUserService _userService;
    private readonly ILogger<CoursesModel> _logger;

    public CoursesModel(AppDbContext db, IUserService userService, ILogger<CoursesModel> logger)
    {
        _db = db;
        _userService = userService;
        _logger = logger;
    }

    public List<Models.Course> Courses { get; set; } = new();
    public List<User> Lecturers { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Courses = await _db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.Documents)
            .OrderBy(c => c.Name)
            .ToListAsync();

        Lecturers = await _userService.GetUsersByRoleAsync(UserRole.Lecturer);
    }

    public async Task<IActionResult> OnPostCreateCourseAsync(string name, string? code, string? description, int? lecturerId)
    {
        var course = new Models.Course
        {
            Name = name.Trim(),
            Code = code?.Trim(),
            Description = description,
            LecturerId = lecturerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();

        // If lecturer specified, enroll them too
        if (lecturerId.HasValue)
        {
            await _userService.EnrollStudentInCourseAsync(lecturerId.Value, course.Id);
        }

        SuccessMessage = $"Đã tạo môn học {name} thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteCourseAsync(int courseId)
    {
        var course = await _db.Courses.FindAsync(courseId);
        if (course != null)
        {
            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
            SuccessMessage = $"Đã xóa môn học {course.Name}.";
        }
        return RedirectToPage();
    }
}
