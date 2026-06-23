using System.ComponentModel.DataAnnotations;

namespace ChatbotStudent.Data.Models;

public enum DocumentType
{
    PDF,
    DOCX,
    PPTX
}

public enum ProcessingStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class Document
{
    public int Id { get; set; }

    [Required]
    [StringLength(500)]
    public string FileName { get; set; } = string.Empty;

    public DocumentType FileType { get; set; }

    public long FileSize { get; set; }

    [StringLength(50)]
    public string? Chapter { get; set; }

    public ProcessingStatus Status { get; set; } = ProcessingStatus.Pending;

    [StringLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Version tracking
    public int Version { get; set; } = 1;
    public int? PreviousVersionId { get; set; }
    public bool IsLatestVersion { get; set; } = true;

    // Foreign key
    public int CourseId { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public Document? PreviousVersion { get; set; }
    public ICollection<Document> LaterVersions { get; set; } = new List<Document>();
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
