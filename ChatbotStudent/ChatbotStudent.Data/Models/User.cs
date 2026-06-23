using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ChatbotStudent.Data.Models;

public class User : IdentityUser<int>
{
    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Student;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    // ── Registration approval fields ───────────────────────────────
    [Required]
    [StringLength(200)]
    public string UniversityName { get; set; } = string.Empty;

    /// <summary>Mã số sinh viên (dành cho Student)</summary>
    [StringLength(50)]
    public string? StudentCode { get; set; }

    /// <summary>Mã số giảng viên (dành cho Lecturer)</summary>
    [StringLength(50)]
    public string? LecturerCode { get; set; }

    /// <summary>Chức danh giảng viên (dành cho Lecturer)</summary>
    [StringLength(100)]
    public string? Title { get; set; }

    /// <summary>Trạng thái duyệt tài khoản</summary>
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;

    /// <summary>Lý do từ chối (nếu có)</summary>
    [StringLength(500)]
    public string? RejectionReason { get; set; }

    // Navigation properties
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}

public enum UserRole
{
    Admin = 0,
    Lecturer = 1,
    Student = 2
}

public enum ApprovalStatus
{
    /// <summary>Chờ admin duyệt</summary>
    Pending = 0,
    /// <summary>Admin đã duyệt</summary>
    Approved = 1,
    /// <summary>Admin từ chối</summary>
    Rejected = 2
}

public class Role : IdentityRole<int>
{
    public Role() { }
    public Role(string name) : base(name) { }
}
