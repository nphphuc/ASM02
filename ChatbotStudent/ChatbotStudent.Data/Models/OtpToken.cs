using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public class OtpToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [StringLength(6)]
    public string Code { get; set; } = string.Empty;

    /// <summary>Mục đích: ResetPassword, VerifyEmail, etc.</summary>
    [StringLength(50)]
    public string Purpose { get; set; } = "ResetPassword";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    // Navigation
    public User User { get; set; } = null!;
}
