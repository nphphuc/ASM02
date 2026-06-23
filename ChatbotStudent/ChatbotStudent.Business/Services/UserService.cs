using System.Security.Cryptography;
using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

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

    public async Task<User> CreateUserWithNotificationAsync(string email, string password, string fullName,
        UserRole role, string universityName, string? studentCode, string? lecturerCode, string? title)
    {
        var user = await CreateUserWithDetailsAsync(email, password, fullName, role,
            universityName, studentCode, lecturerCode, title, autoApprove: true);

        // Send welcome email with credentials
        if (user.Email != null)
        {
            await _emailService.SendAccountCreatedNotificationAsync(
                user.Email, user.FullName, password, role.ToString());
        }

        return user;
    }

    public async Task<BulkCreateResult> BulkCreateUsersFromExcelAsync(Stream fileStream, string fileName)
    {
        var result = new BulkCreateResult();

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header row

            foreach (var row in rows)
            {
                try
                {
                    var fullName = row.Cell(1).GetString().Trim();
                    var email = row.Cell(2).GetString().Trim();
                    var roleStr = row.Cell(3).GetString().Trim();
                    var university = row.Cell(4).GetString().Trim();
                    var code = row.Cell(5).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
                    {
                        result.FailCount++;
                        result.Errors.Add($"Dòng {row.RowNumber()}: Thiếu họ tên hoặc email.");
                        continue;
                    }

                    if (!email.Contains('@'))
                    {
                        result.FailCount++;
                        result.Errors.Add($"Dòng {row.RowNumber()}: Email '{email}' không hợp lệ.");
                        continue;
                    }

                    var role = roleStr.ToLowerInvariant() switch
                    {
                        "giang vien" or "lecturer" or "gv" => UserRole.Lecturer,
                        _ => UserRole.Student
                    };

                    // Generate random password
                    var password = GenerateRandomPassword();

                    if (role == UserRole.Lecturer)
                    {
                        await CreateUserWithDetailsAsync(email, password, fullName, role,
                            university, null, string.IsNullOrEmpty(code) ? null : code, null, autoApprove: true);
                    }
                    else
                    {
                        await CreateUserWithDetailsAsync(email, password, fullName, role,
                            university, string.IsNullOrEmpty(code) ? null : code, null, null, autoApprove: true);
                    }

                    // Send welcome email
                    await _emailService.SendAccountCreatedNotificationAsync(
                        email, fullName, password, role.ToString());

                    result.SuccessCount++;
                    result.SuccessEmails.Add(email);
                    _logger.LogInformation("Bulk created user {Email} with role {Role}", email, role);
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.Errors.Add($"Dòng {row.RowNumber()}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Lỗi đọc file Excel: {ex.Message}");
        }

        _logger.LogInformation("Bulk create result: {Success} success, {Fail} failed",
            result.SuccessCount, result.FailCount);
        return result;
    }

    private static string GenerateRandomPassword()
    {
        const string chars = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var password = new char[8];
        for (var i = 0; i < 8; i++)
            password[i] = chars[RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(password) + "@1";
    }

    public async Task<bool> ApproveUserAsync(int userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.ApprovalStatus = ApprovalStatus.Approved;
        user.IsActive = true;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Approved user {Email}", user.Email);

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

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

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
