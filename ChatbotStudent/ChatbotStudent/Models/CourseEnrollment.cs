using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Models;

public class CourseEnrollment
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
