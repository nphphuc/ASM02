using ChatbotStudent.Models;
using ChatbotStudent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Pages.Admin;

[Authorize(Roles = "Admin")]
public class UsersModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersModel> _logger;

    public UsersModel(IUserService userService, ILogger<UsersModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public List<User> Users { get; set; } = new();
    public List<User> PendingUsers { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Users = await _userService.GetAllUsersAsync();
        PendingUsers = await _userService.GetPendingUsersAsync();
    }

    public async Task<IActionResult> OnPostCreateUserAsync(
        string email, string password, string fullName, string role, string universityName)
    {
        try
        {
            var userRole = role switch
            {
                "Admin" => UserRole.Admin,
                "Lecturer" => UserRole.Lecturer,
                _ => UserRole.Student
            };

            await _userService.CreateUserWithDetailsAsync(
                email.Trim(), password, fullName.Trim(), userRole,
                universityName?.Trim() ?? "", null, null, null);
            SuccessMessage = $"Đã tạo người dùng {fullName} thành công.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int userId)
    {
        var success = await _userService.ToggleUserActiveAsync(userId);
        if (!success)
            ErrorMessage = "Không tìm thấy người dùng.";
        else
            SuccessMessage = "Đã cập nhật trạng thái.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangeRoleAsync(int userId, string newRole)
    {
        var userRole = newRole switch
        {
            "Admin" => UserRole.Admin,
            "Lecturer" => UserRole.Lecturer,
            _ => UserRole.Student
        };

        var success = await _userService.UpdateUserRoleAsync(userId, userRole);
        if (!success)
            ErrorMessage = "Không tìm thấy người dùng.";
        else
            SuccessMessage = "Đã đổi vai trò thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int userId)
    {
        var success = await _userService.DeleteUserAsync(userId);
        if (!success)
            ErrorMessage = "Không tìm thấy người dùng.";
        else
            SuccessMessage = "Đã xóa người dùng.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int userId)
    {
        var success = await _userService.ApproveUserAsync(userId);
        if (!success)
            ErrorMessage = "Không tìm thấy người dùng.";
        else
            SuccessMessage = "Đã duyệt tài khoản thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int userId, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            ErrorMessage = "Vui lòng nhập lý do từ chối.";
            return RedirectToPage();
        }

        var success = await _userService.RejectUserAsync(userId, rejectionReason.Trim());
        if (!success)
            ErrorMessage = "Không tìm thấy người dùng.";
        else
            SuccessMessage = "Đã từ chối tài khoản.";
        return RedirectToPage();
    }
}
