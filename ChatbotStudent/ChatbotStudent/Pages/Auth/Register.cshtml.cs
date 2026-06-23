using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;

namespace ChatbotStudent.Web.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        IUserService userService,
        ILogger<RegisterModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    public string Role { get; set; } = "Student";

    [BindProperty]
    public string UniversityName { get; set; } = string.Empty;

    [BindProperty]
    public string? StudentCode { get; set; }

    [BindProperty]
    public string? LecturerCode { get; set; }

    [BindProperty]
    public string? Title { get; set; }

    [BindProperty]
    public string? CustomTitle { get; set; }

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "Vui lòng điền đầy đủ thông tin.";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            return Page();
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(UniversityName))
        {
            ErrorMessage = "Vui lòng nhập tên trường đại học.";
            return Page();
        }

        var isLecturer = Role == "Lecturer";

        if (isLecturer && string.IsNullOrWhiteSpace(LecturerCode))
        {
            ErrorMessage = "Vui lòng nhập mã số giảng viên.";
            return Page();
        }

        if (!isLecturer && string.IsNullOrWhiteSpace(StudentCode))
        {
            ErrorMessage = "Vui lòng nhập mã số sinh viên.";
            return Page();
        }

        // Handle custom title for lecturers
        if (isLecturer)
        {
            if (Title == "__other__")
            {
                if (string.IsNullOrWhiteSpace(CustomTitle))
                {
                    ErrorMessage = "Vui lòng nhập chức danh của bạn.";
                    return Page();
                }
                Title = CustomTitle.Trim();
            }
            else if (string.IsNullOrWhiteSpace(Title))
            {
                ErrorMessage = "Vui lòng chọn chức danh giảng viên.";
                return Page();
            }
        }

        try
        {
            var userRole = isLecturer ? UserRole.Lecturer : UserRole.Student;
            var user = await _userService.CreateUserWithDetailsAsync(
                Email.Trim(), Password, FullName.Trim(), userRole,
                UniversityName.Trim(),
                isLecturer ? null : StudentCode?.Trim(),
                isLecturer ? LecturerCode?.Trim() : null,
                isLecturer ? Title?.Trim() : null,
                autoApprove: false);

            _logger.LogInformation("New user registered: {Email} as {Role} — pending approval",
                Email, Role);

            // Don't sign in — redirect to Index with pending message
            TempData["RegistrationSuccess"] = $"Tài khoản {Email} đã được tạo thành công.";
            return RedirectToPage("/Index");
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for {Email}", Email);
            ErrorMessage = "Đăng ký thất bại. Vui lòng thử lại.";
            return Page();
        }
    }
}
