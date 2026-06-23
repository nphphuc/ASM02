using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

public interface ICourseService
{
    Task<List<Course>> GetAllCoursesAsync();
    Task<List<Course>> GetCoursesByLecturerAsync(int lecturerId);
    Task<List<Course>> GetCoursesByUserRoleAsync(int userId, string role);
    Task<Course?> GetCourseByIdAsync(int courseId);
    Task<Course?> GetCourseDetailAsync(int courseId);
    Task<Course> CreateCourseAsync(string name, string? code, string? description, int? lecturerId);
    Task<bool> DeleteCourseAsync(int courseId);
}
