using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Models;

public class Course
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Code { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();

    public int? LecturerId { get; set; }
    public User? Lecturer { get; set; }
}
