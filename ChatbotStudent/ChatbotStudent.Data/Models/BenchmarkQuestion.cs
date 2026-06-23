using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public class BenchmarkQuestion
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    [Required]
    public string Question { get; set; } = string.Empty;

    [Required]
    public string GroundTruth { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Category { get; set; }

    [StringLength(100)]
    public string? Difficulty { get; set; }

    /// <summary>
    /// Reference to which document(s) contain the answer
    /// </summary>
    public string? SourceDocumentNames { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Course Course { get; set; } = null!;
}
