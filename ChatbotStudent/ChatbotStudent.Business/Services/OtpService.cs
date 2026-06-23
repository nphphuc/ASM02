using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public class OtpService : IOtpService
{
    private readonly AppDbContext _db;

    public OtpService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string> GenerateOtpAsync(int userId, string purpose = "ResetPassword")
    {
        // Invalidate any existing OTPs for this user + purpose
        var existing = await _db.OtpTokens
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();
        foreach (var otp in existing)
        {
            otp.IsUsed = true;
        }

        // Generate new OTP
        var code = new Random().Next(100000, 999999).ToString();
        _db.OtpTokens.Add(new OtpToken
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            IsUsed = false
        });

        await _db.SaveChangesAsync();
        return code;
    }

    public async Task<bool> VerifyOtpAsync(int userId, string code, string purpose = "ResetPassword")
    {
        var otp = await _db.OtpTokens
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null) return false;
        if (otp.Code != code) return false;
        if (otp.ExpiresAt < DateTime.UtcNow) return false;

        return true;
    }

    public async Task InvalidateOtpAsync(int userId, string purpose = "ResetPassword")
    {
        var tokens = await _db.OtpTokens
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsUsed = true;
        }

        await _db.SaveChangesAsync();
    }
}
