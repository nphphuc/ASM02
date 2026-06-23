using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Web.Pages.Auth;

[Microsoft.AspNetCore.Authorization.Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ILogger<ChangePasswordModel> _logger;

    public ChangePasswordModel(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ILogger<ChangePasswordModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return Page();
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            ErrorMessage = "Không tìm thấy người dùng.";
            return Page();
        }

        var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            ErrorMessage = $"Đổi mật khẩu thất bại: {errors}";
            return Page();
        }

        // Re-sign in to refresh the cookie
        await _signInManager.RefreshSignInAsync(user);

        _logger.LogInformation("User {Email} changed password successfully", user.Email);
        SuccessMessage = "Mật khẩu đã được thay đổi thành công.";
        return Page();
    }
}
