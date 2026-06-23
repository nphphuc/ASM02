using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChatbotStudent.Models;
using ChatbotStudent.Services;

namespace ChatbotStudent.Pages.Auth;

public class ResetPasswordModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(
        IUserService userService,
        IOtpService otpService,
        IEmailService emailService,
        UserManager<User> userManager,
        ILogger<ResetPasswordModel> logger)
    {
        _userService = userService;
        _otpService = otpService;
        _emailService = emailService;
        _userManager = userManager;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public bool IsVerified { get; set; } = false;
    public string? Email { get; set; }

    [BindProperty]
    public string Step { get; set; } = string.Empty;

    [BindProperty]
    public string OtpCode { get; set; } = string.Empty;

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public void OnGet()
    {
        Email = TempData["ResetEmail"] as string;
        if (!string.IsNullOrEmpty(Email))
        {
            TempData.Keep("ResetUserId");
            TempData.Keep("ResetEmail");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Email = TempData["ResetEmail"] as string;
        var userIdStr = TempData["ResetUserId"] as string;

        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(userIdStr))
        {
            ErrorMessage = "Phiên đặt lại mật khẩu không hợp lệ. Vui lòng thử lại.";
            return Page();
        }

        if (!int.TryParse(userIdStr, out var userId))
        {
            ErrorMessage = "Phiên đặt lại mật khẩu không hợp lệ.";
            return Page();
        }

        TempData.Keep("ResetUserId");
        TempData.Keep("ResetEmail");

        if (Step == "verify")
        {
            return await HandleVerifyOtp(userId);
        }
        else if (Step == "reset")
        {
            return await HandleResetPassword(userId);
        }

        ErrorMessage = "Yêu cầu không hợp lệ.";
        return Page();
    }

    private async Task<IActionResult> HandleVerifyOtp(int userId)
    {
        if (string.IsNullOrWhiteSpace(OtpCode) || OtpCode.Length != 6)
        {
            ErrorMessage = "Vui lòng nhập mã OTP 6 số.";
            return Page();
        }

        var valid = await _otpService.VerifyOtpAsync(userId, OtpCode.Trim());
        if (!valid)
        {
            ErrorMessage = "Mã OTP không đúng hoặc đã hết hạn. Vui lòng thử lại.";
            return Page();
        }

        // Mark OTP as used immediately to prevent replay
        await _otpService.InvalidateOtpAsync(userId);

        IsVerified = true;
        return Page();
    }

    private async Task<IActionResult> HandleResetPassword(int userId)
    {
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return Page();
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return Page();
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            ErrorMessage = "Không tìm thấy người dùng.";
            return Page();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            ErrorMessage = $"Lỗi: {errors}";
            return Page();
        }

        _logger.LogInformation("Password reset successful for {Email}", user.Email);
        SuccessMessage = "Mật khẩu đã được đặt lại thành công! Đang chuyển hướng...";

        // Clear temp data
        TempData.Remove("ResetUserId");
        TempData.Remove("ResetEmail");

        // Auto-redirect to login after 2 seconds
        Response.Headers.Append("REFRESH", "2;URL=/Auth/Login");
        return Page();
    }

    public async Task<IActionResult> OnPostResendAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "Vui lòng nhập email.";
            return Page();
        }

        var user = await _userService.GetUserByEmailAsync(email.Trim());
        if (user == null)
        {
            SuccessMessage = "Nếu email tồn tại, bạn sẽ nhận được mã OTP trong vài phút.";
            return Page();
        }

        var otp = await _otpService.GenerateOtpAsync(user.Id);
        await _emailService.SendOtpEmailAsync(user.Email!, user.FullName, otp);

        TempData["ResetUserId"] = user.Id;
        TempData["ResetEmail"] = user.Email;

        _logger.LogInformation("OTP resent for password reset: {Email}", email);
        SuccessMessage = "Mã OTP mới đã được gửi đến email của bạn.";
        return Page();
    }
}
