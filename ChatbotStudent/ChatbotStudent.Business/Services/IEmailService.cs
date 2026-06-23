namespace ChatbotStudent.Business.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendOtpEmailAsync(string toEmail, string userName, string otpCode);
    Task SendApprovalNotificationAsync(string toEmail, string userName, bool approved, string? reason = null);
    Task SendAccountCreatedNotificationAsync(string toEmail, string userName, string password, string role);
}
