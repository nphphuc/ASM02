using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace ChatbotStudent.Business.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[Email disabled] To: {To}, Subject: {Subject}", toEmail, subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.UseSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password)
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", toEmail);
        }
    }

    public async Task SendOtpEmailAsync(string toEmail, string userName, string otpCode)
    {
        var subject = "Mã OTP đặt lại mật khẩu - EduAI";
        var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background:#f5f5f5;padding:20px'>
    <div style='max-width:500px;margin:auto;background:#fff;border-radius:12px;padding:30px;box-shadow:0 2px 8px rgba(0,0,0,.1)'>
        <div style='text-align:center;margin-bottom:20px'>
            <h2 style='color:#4f46e5;margin:0'>🔐 EduAI</h2>
            <p style='color:#666;font-size:14px'>Đặt lại mật khẩu</p>
        </div>
        <p>Xin chào <strong>{userName}</strong>,</p>
        <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP dưới đây:</p>
        <div style='text-align:center;margin:25px 0'>
            <span style='font-size:32px;font-weight:bold;letter-spacing:8px;color:#4f46e5;background:#f0f0ff;padding:12px 24px;border-radius:8px'>{otpCode}</span>
        </div>
        <p style='color:#999;font-size:13px'>Mã OTP có hiệu lực trong <strong>10 phút</strong>. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
        <hr style='border:none;border-top:1px solid #eee;margin:20px 0' />
        <p style='color:#999;font-size:12px;text-align:center'>EduAI - Hệ thống Chatbot hỗ trợ học tập</p>
    </div>
</body>
</html>";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendApprovalNotificationAsync(string toEmail, string userName, bool approved, string? reason = null)
    {
        if (approved)
        {
            var subject = "Tài khoản đã được duyệt - EduAI";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background:#f5f5f5;padding:20px'>
    <div style='max-width:500px;margin:auto;background:#fff;border-radius:12px;padding:30px;box-shadow:0 2px 8px rgba(0,0,0,.1)'>
        <div style='text-align:center;margin-bottom:20px'>
            <h2 style='color:#4f46e5;margin:0'>✅ EduAI</h2>
            <p style='color:#666;font-size:14px'>Tài khoản đã được duyệt</p>
        </div>
        <p>Xin chào <strong>{userName}</strong>,</p>
        <p>Tài khoản của bạn đã được <strong style='color:#22c55e'>duyệt thành công</strong>!</p>
        <p>Bạn có thể đăng nhập ngay bây giờ để sử dụng hệ thống.</p>
        <div style='text-align:center;margin:25px 0'>
            <a href='http://localhost:5000/Auth/Login'
               style='display:inline-block;background:#4f46e5;color:#fff;padding:12px 32px;border-radius:8px;text-decoration:none;font-weight:600'>
                Đăng nhập ngay
            </a>
        </div>
        <hr style='border:none;border-top:1px solid #eee;margin:20px 0' />
        <p style='color:#999;font-size:12px;text-align:center'>EduAI - Hệ thống Chatbot hỗ trợ học tập</p>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body);
        }
        else
        {
            var safeReason = string.IsNullOrEmpty(reason) ? "Không rõ lý do" : reason;
            var subject = "Tài khoản đã bị từ chối - EduAI";
            var body = $@"
<!DOCTYPE html>
<html>
<head><meta charset='utf-8'></head>
<body style='font-family:Arial,sans-serif;background:#f5f5f5;padding:20px'>
    <div style='max-width:500px;margin:auto;background:#fff;border-radius:12px;padding:30px;box-shadow:0 2px 8px rgba(0,0,0,.1)'>
        <div style='text-align:center;margin-bottom:20px'>
            <h2 style='color:#ef4444;margin:0'>❌ EduAI</h2>
            <p style='color:#666;font-size:14px'>Tài khoản bị từ chối</p>
        </div>
        <p>Xin chào <strong>{userName}</strong>,</p>
        <p>Rất tiếc, tài khoản của bạn đã bị <strong style='color:#ef4444'>từ chối</strong>.</p>
        <div style='background:#fef2f2;border-left:4px solid #ef4444;padding:12px 16px;margin:16px 0;border-radius:4px'>
            <p style='margin:0;color:#dc2626;font-size:14px'><strong>Lý do:</strong> {safeReason}</p>
        </div>
        <p style='color:#666;font-size:14px'>Vui lòng liên hệ admin để biết thêm chi tiết.</p>
        <hr style='border:none;border-top:1px solid #eee;margin:20px 0' />
        <p style='color:#999;font-size:12px;text-align:center'>EduAI - Hệ thống Chatbot hỗ trợ học tập</p>
    </div>
</body>
</html>";
            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
