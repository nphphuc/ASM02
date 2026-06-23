using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Admin;

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
    public BulkCreateResult? BulkResult { get; set; }

    public async Task OnGetAsync()
    {
        Users = await _userService.GetAllUsersAsync();
        PendingUsers = await _userService.GetPendingUsersAsync();

        // Read messages persisted across redirect via TempData
        if (TempData["SuccessMessage"] is string sm)
            SuccessMessage = sm;
        if (TempData["ErrorMessage"] is string em)
            ErrorMessage = em;
        if (TempData["BulkResult"] is string br && !string.IsNullOrEmpty(br))
        {
            try
            {
                var opt = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                BulkResult = System.Text.Json.JsonSerializer.Deserialize<BulkCreateResult>(br, opt);
            }
            catch { }
        }
    }

    public async Task<IActionResult> OnPostCreateUserAsync(
        string email, string password, string fullName, string role, string universityName,
        string? studentCode, string? lecturerCode, string? title)
    {
        try
        {
            var userRole = role switch
            {
                "Admin" => UserRole.Admin,
                "Lecturer" => UserRole.Lecturer,
                _ => UserRole.Student
            };

            if (userRole == UserRole.Admin)
            {
                // Admin accounts: manual creation without email
                await _userService.CreateUserWithDetailsAsync(
                    email.Trim(), password, fullName.Trim(), userRole,
                    universityName?.Trim() ?? "", null, null, null);
                TempData["SuccessMessage"] = $"Đã tạo admin {fullName} thành công.";
            }
            else
            {
                // Lecturer/Student: create + send welcome email with password
                await _userService.CreateUserWithNotificationAsync(
                    email.Trim(), password, fullName.Trim(), userRole,
                    universityName?.Trim() ?? "",
                    string.IsNullOrWhiteSpace(studentCode) ? null : studentCode.Trim(),
                    string.IsNullOrWhiteSpace(lecturerCode) ? null : lecturerCode.Trim(),
                    string.IsNullOrWhiteSpace(title) ? null : title.Trim());
                TempData["SuccessMessage"] = $"Đã tạo tài khoản {fullName} thành công. Email thông báo đã được gửi tới {email.Trim()}.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi: {ex.Message}";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadExcelAsync(List<IFormFile> files)
    {
        if (files == null || files.Count == 0 || files[0].Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn file Excel để upload.";
            return RedirectToPage();
        }

        var file = files[0];
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (ext != ".xlsx" && ext != ".xls")
        {
            TempData["ErrorMessage"] = "Chỉ hỗ trợ file Excel (.xlsx, .xls).";
            return RedirectToPage();
        }

        try
        {
            using var stream = file.OpenReadStream();
            var bulkResult = await _userService.BulkCreateUsersFromExcelAsync(stream, file.FileName);

            // Serialize BulkResult to TempData so it can be shown after redirect
            var json = System.Text.Json.JsonSerializer.Serialize(bulkResult);
            TempData["BulkResult"] = json;

            if (bulkResult.SuccessCount > 0)
            {
                TempData["SuccessMessage"] = $"Đã tạo thành công {bulkResult.SuccessCount} tài khoản. Email thông báo đã được gửi.";
            }
            if (bulkResult.FailCount > 0)
            {
                TempData["ErrorMessage"] = $"Có {bulkResult.FailCount} tài khoản tạo thất bại. Xem chi tiết bên dưới.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Lỗi xử lý file: {ex.Message}";
            _logger.LogError(ex, "Error processing Excel upload");
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(int userId)
    {
        var success = await _userService.ToggleUserActiveAsync(userId);
        if (!success)
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
        else
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái.";
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
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
        else
            TempData["SuccessMessage"] = "Đã đổi vai trò thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int userId)
    {
        var success = await _userService.DeleteUserAsync(userId);
        if (!success)
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
        else
            TempData["SuccessMessage"] = "Đã xóa người dùng.";
        return RedirectToPage();
    }

    public IActionResult OnGetDownloadTemplate()
    {
        // Generate a simple Excel template with headers and sample row
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Danh sach");

        ws.Cell(1, 1).Value = "Ho ten";
        ws.Cell(1, 2).Value = "Email";
        ws.Cell(1, 3).Value = "Vai tro";
        ws.Cell(1, 4).Value = "Truong";
        ws.Cell(1, 5).Value = "Ma so";

        ws.Cell(2, 1).Value = "Nguyen Van A";
        ws.Cell(2, 2).Value = "nguyenvana@example.com";
        ws.Cell(2, 3).Value = "Sinh vien";
        ws.Cell(2, 4).Value = "Dai hoc Cong nghe";
        ws.Cell(2, 5).Value = "2202xxxx";

        ws.Cell(3, 1).Value = "Tran Van B";
        ws.Cell(3, 2).Value = "tranvanb@example.com";
        ws.Cell(3, 3).Value = "Giang vien";
        ws.Cell(3, 4).Value = "Dai hoc Cong nghe";
        ws.Cell(3, 5).Value = "GV.2021001";

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "template_danh_sach_tai_khoan.xlsx");
    }

    public async Task<IActionResult> OnPostApproveAsync(int userId)
    {
        var success = await _userService.ApproveUserAsync(userId);
        if (!success)
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
        else
            TempData["SuccessMessage"] = "Đã duyệt tài khoản thành công.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int userId, string rejectionReason)
    {
        if (string.IsNullOrWhiteSpace(rejectionReason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToPage();
        }

        var success = await _userService.RejectUserAsync(userId, rejectionReason.Trim());
        if (!success)
            TempData["ErrorMessage"] = "Không tìm thấy người dùng.";
        else
            TempData["SuccessMessage"] = "Đã từ chối tài khoản.";
        return RedirectToPage();
    }
}
