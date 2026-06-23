using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChatbotStudent.Services;

namespace ChatbotStudent.Pages.Auth;

public class ForgotPasswordModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordModel> _logger;

    public ForgotPasswordModel(
        IUserService userService,
        IOtpService otpService,
        IEmailService emailService,
        ILogger<ForgotPasswordModel> logger)
    {
        _userService = userService;
        _otpService = otpService;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Vui lòng nhập email.";
            return Page();
        }

        var user = await _userService.GetUserByEmailAsync(Email.Trim());
        if (user == null)
        {
            // Don't reveal whether email exists — show generic success
            SuccessMessage = "Nếu email tồn tại, bạn sẽ nhận được mã OTP trong vài phút.";
            _logger.LogInformation("Forgot password requested for non-existent email: {Email}", Email);
            return Page();
        }

        try
        {
            var otp = await _otpService.GenerateOtpAsync(user.Id);
            await _emailService.SendOtpEmailAsync(user.Email!, user.FullName, otp);

            // Store userId in TempData to pass to ResetPassword page
            TempData["ResetUserId"] = user.Id;
            TempData["ResetEmail"] = user.Email;

            _logger.LogInformation("OTP sent for password reset: {Email}", Email);
            return RedirectToPage("/Auth/ResetPassword");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP for {Email}", Email);
            ErrorMessage = "Có lỗi xảy ra khi gửi OTP. Vui lòng thử lại sau.";
            return Page();
        }
    }
}
