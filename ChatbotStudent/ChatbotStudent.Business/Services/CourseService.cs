using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public class CourseService : ICourseService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CourseService> _logger;

    public CourseService(AppDbContext db, ILogger<CourseService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<List<Course>> GetAllCoursesAsync()
    {
        return _db.Courses.ToListAsync();
    }

    public Task<List<Course>> GetCoursesByLecturerAsync(int lecturerId)
    {
        return _db.Courses
            .Where(c => c.LecturerId == lecturerId)
            .ToListAsync();
    }

    public async Task<List<Course>> GetCoursesByUserRoleAsync(int userId, string role)
    {
        if (role == "Admin")
        {
            return await _db.Courses.ToListAsync();
        }

        var enrolledCourseIds = await _db.CourseEnrollments
            .Where(e => e.UserId == userId)
            .Select(e => e.CourseId)
            .ToListAsync();

        return await _db.Courses
            .Where(c => enrolledCourseIds.Contains(c.Id))
            .ToListAsync();
    }

    public Task<Course?> GetCourseByIdAsync(int courseId)
    {
        return _db.Courses.FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public Task<Course?> GetCourseDetailAsync(int courseId)
    {
        return _db.Courses
            .Include(c => c.Documents)
            .Include(c => c.ChatSessions)
            .Include(c => c.Enrollments)
            .FirstOrDefaultAsync(c => c.Id == courseId);
    }

    public async Task<Course> CreateCourseAsync(string name, string? code, string? description, int? lecturerId)
    {
        var course = new Course
        {
            Name = name.Trim(),
            Code = code?.Trim(),
            Description = description,
            LecturerId = lecturerId,
            CreatedAt = DateTime.UtcNow
        };

        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Created course: {Name} (Code: {Code})", name, code);
        return course;
    }

    public async Task<bool> DeleteCourseAsync(int courseId)
    {
        var course = await _db.Courses.FindAsync(courseId);
        if (course == null) return false;

        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Deleted course: {Name} (ID: {Id})", course.Name, courseId);
        return true;
    }
}
