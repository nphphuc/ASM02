using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<List<User>> GetAllUsersAsync();
    Task<List<User>> GetUsersByRoleAsync(UserRole role);
    Task<User> CreateUserAsync(string email, string password, string fullName, UserRole role);
    Task<User> CreateUserWithDetailsAsync(string email, string password, string fullName, UserRole role,
        string universityName, string? studentCode, string? lecturerCode, string? title);
    Task<User> CreateUserWithDetailsAsync(string email, string password, string fullName, UserRole role,
        string universityName, string? studentCode, string? lecturerCode, string? title, bool autoApprove);
    Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole);
    Task<bool> ApproveUserAsync(int userId);
    Task<bool> RejectUserAsync(int userId, string reason);
    Task<List<User>> GetPendingUsersAsync();
    Task<bool> ToggleUserActiveAsync(int userId);
    Task<bool> DeleteUserAsync(int userId);
    Task<List<Course>> GetEnrolledCoursesAsync(int userId);
    Task<bool> EnrollStudentInCourseAsync(int userId, int courseId);
    Task<bool> RemoveStudentFromCourseAsync(int userId, int courseId);
    Task<List<User>> GetStudentsNotEnrolledAsync(int courseId);
}
