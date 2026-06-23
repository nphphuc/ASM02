using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Web.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<User> signInManager,
        UserManager<User> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            SuccessMessage = "Bạn đã đăng nhập rồi.";
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập email và mật khẩu.";
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(Email.Trim());
        if (user == null)
        {
            ErrorMessage = "Email hoặc mật khẩu không đúng.";
            return Page();
        }

        if (!user.IsActive)
        {
            ErrorMessage = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ admin.";
            return Page();
        }

        // Check approval status
        if (user.ApprovalStatus == Data.Models.ApprovalStatus.Pending)
        {
            ErrorMessage = "Tài khoản của bạn đang chờ admin duyệt. Vui lòng quay lại sau.";
            return Page();
        }

        if (user.ApprovalStatus == Data.Models.ApprovalStatus.Rejected)
        {
            var reason = !string.IsNullOrEmpty(user.RejectionReason)
                ? $" Lý do: {user.RejectionReason}"
                : "";
            ErrorMessage = $"Tài khoản của bạn đã bị từ chối.{reason}";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(user, Password, true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in", Email);
            return RedirectToPage("/Index");
        }

        if (result.IsLockedOut)
        {
            ErrorMessage = "Tài khoản của bạn đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 15 phút.";
            return Page();
        }

        ErrorMessage = "Email hoặc mật khẩu không đúng.";
        return Page();
    }
}
