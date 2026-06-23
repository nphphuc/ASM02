namespace ChatbotStudent.Services;

public interface IOtpService
{
    /// <summary>Tạo OTP ngẫu nhiên 6 số và lưu vào DB</summary>
    Task<string> GenerateOtpAsync(int userId, string purpose = "ResetPassword");
    /// <summary>Xác thực OTP (kiểm tra hạn, đúng code, chưa dùng)</summary>
    Task<bool> VerifyOtpAsync(int userId, string code, string purpose = "ResetPassword");
    /// <summary>Đánh dấu OTP đã dùng</summary>
    Task InvalidateOtpAsync(int userId, string purpose = "ResetPassword");
}
