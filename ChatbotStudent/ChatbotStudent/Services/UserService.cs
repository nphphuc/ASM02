using ChatbotStudent.Data;
using ChatbotStudent.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        AppDbContext db,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IEmailService emailService,
        ILogger<UserService> logger)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<User?> GetUserByIdAsync(int userId)
        => await _userManager.FindByIdAsync(userId.ToString());

    public async Task<User?> GetUserByEmailAsync(string email)
        => await _userManager.FindByEmailAsync(email);

    public async Task<List<User>> GetAllUsersAsync()
        => await _userManager.Users.OrderBy(u => u.CreatedAt).ToListAsync();

    public async Task<List<User>> GetUsersByRoleAsync(UserRole role)
    {
        var roleName = role.ToString();
        var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
        return usersInRole.ToList();
    }

    public async Task<User> CreateUserAsync(string email, string password, string fullName, UserRole role)
    {
        return await CreateUserWithDetailsAsync(email, password, fullName, role, "", null, null, null);
    }

    public async Task<User> CreateUserWithDetailsAsync(string email, string password, string fullName, UserRole role,
        string universityName, string? studentCode, string? lecturerCode, string? title)
    {
        return await CreateUserWithDetailsAsync(email, password, fullName, role,
            universityName, studentCode, lecturerCode, title, autoApprove: true);
    }

    public async Task<User> CreateUserWithDetailsAsync(string email, string password, string fullName, UserRole role,
        string universityName, string? studentCode, string? lecturerCode, string? title, bool autoApprove)
    {
        var user = new User
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            Role = role,
            UniversityName = universityName,
            StudentCode = studentCode,
            LecturerCode = lecturerCode,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailConfirmed = true,
            ApprovalStatus = autoApprove ? ApprovalStatus.Approved : ApprovalStatus.Pending
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        // Ensure role exists
        var roleName = role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new Role(roleName));
        }

        await _userManager.AddToRoleAsync(user, roleName);

        _logger.LogInformation("Created user {Email} with role {Role} (status: {Status})",
            email, roleName, user.ApprovalStatus);
        return user;
    }

    public async Task<bool> ApproveUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.ApprovalStatus = ApprovalStatus.Approved;
        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Approved user {Email}", user.Email);

        // Send email notification
        if (user.Email != null)
        {
            await _emailService.SendApprovalNotificationAsync(user.Email, user.FullName, true);
        }

        return true;
    }

    public async Task<bool> RejectUserAsync(int userId, string reason)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.ApprovalStatus = ApprovalStatus.Rejected;
        user.RejectionReason = reason;
        user.IsActive = false;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Rejected user {Email}: {Reason}", user.Email, reason);

        // Send email notification
        if (user.Email != null)
        {
            await _emailService.SendApprovalNotificationAsync(user.Email, user.FullName, false, reason);
        }

        return true;
    }

    public async Task<List<User>> GetPendingUsersAsync()
    {
        return await _userManager.Users
            .Where(u => u.ApprovalStatus == ApprovalStatus.Pending)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, UserRole newRole)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        // Remove from all existing roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Add to new role
        var roleName = newRole.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new Role(roleName));
        }
        await _userManager.AddToRoleAsync(user, roleName);

        user.Role = newRole;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Updated user {Email} role to {Role}", user.Email, roleName);
        return true;
    }

    public async Task<bool> ToggleUserActiveAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        await _userManager.DeleteAsync(user);
        return true;
    }

    public async Task<List<Course>> GetEnrolledCoursesAsync(int userId)
    {
        return await _db.CourseEnrollments
            .Where(e => e.UserId == userId)
            .Include(e => e.Course)
            .Select(e => e.Course)
            .ToListAsync();
    }

    public async Task<bool> EnrollStudentInCourseAsync(int userId, int courseId)
    {
        if (await _db.CourseEnrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId))
            return false;

        _db.CourseEnrollments.Add(new CourseEnrollment
        {
            UserId = userId,
            CourseId = courseId,
            EnrolledAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveStudentFromCourseAsync(int userId, int courseId)
    {
        var enrollment = await _db.CourseEnrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CourseId == courseId);
        if (enrollment == null) return false;

        _db.CourseEnrollments.Remove(enrollment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<User>> GetStudentsNotEnrolledAsync(int courseId)
    {
        var enrolledIds = await _db.CourseEnrollments
            .Where(e => e.CourseId == courseId)
            .Select(e => e.UserId)
            .ToListAsync();

        var roleName = UserRole.Student.ToString();
        var students = await _userManager.GetUsersInRoleAsync(roleName);
        return students.Where(s => !enrolledIds.Contains(s.Id)).ToList();
    }
}
