using ChatbotStudent.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Data;

public class AppDbContext : IdentityDbContext<User, Role, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<BenchmarkExperiment> BenchmarkExperiments => Set<BenchmarkExperiment>();
    public DbSet<BenchmarkResult> BenchmarkResults => Set<BenchmarkResult>();
    public DbSet<BenchmarkQuestion> BenchmarkQuestions => Set<BenchmarkQuestion>();

    // Course enrollment (many-to-many between User and Course)
    public DbSet<CourseEnrollment> CourseEnrollments => Set<CourseEnrollment>();

    // Chat message feedback
    public DbSet<ChatMessageFeedback> ChatMessageFeedbacks => Set<ChatMessageFeedback>();

    // System configuration
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();

    // OTP tokens for password reset
    public DbSet<OtpToken> OtpTokens => Set<OtpToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Identity table names ─────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.UniversityName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.StudentCode).HasMaxLength(50);
            entity.Property(u => u.LecturerCode).HasMaxLength(50);
            entity.Property(u => u.Title).HasMaxLength(100);
            entity.Property(u => u.ApprovalStatus).HasConversion<int>().HasDefaultValue(ApprovalStatus.Pending);
            entity.Property(u => u.RejectionReason).HasMaxLength(500);
        });
        modelBuilder.Entity<Role>(entity => entity.ToTable("Roles"));
        modelBuilder.Entity<IdentityUserRole<int>>(entity => entity.ToTable("UserRoles"));
        modelBuilder.Entity<IdentityUserClaim<int>>(entity => entity.ToTable("UserClaims"));
        modelBuilder.Entity<IdentityUserLogin<int>>(entity => entity.ToTable("UserLogins"));
        modelBuilder.Entity<IdentityUserToken<int>>(entity => entity.ToTable("UserTokens"));
        modelBuilder.Entity<IdentityRoleClaim<int>>(entity => entity.ToTable("RoleClaims"));

        // ── Course ────────────────────────────────
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Code).HasMaxLength(50);
            entity.HasIndex(e => e.Code).IsUnique().HasFilter("[Code] IS NOT NULL");
        });

        // ── Course Enrollment (many-to-many) ──────
        modelBuilder.Entity<CourseEnrollment>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.CourseId });
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Enrollments)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Document ──────────────────────────────
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Chapter).HasMaxLength(50);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(e => e.Course)
                  .WithMany(c => c.Documents)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Self-referencing for version tracking (no cascade to avoid cycles)
            entity.HasOne(e => e.PreviousVersion)
                  .WithMany(e => e.LaterVersions)
                  .HasForeignKey(e => e.PreviousVersionId)
                  .OnDelete(DeleteBehavior.NoAction)
                  .IsRequired(false);
        });

        // ── DocumentChunk ─────────────────────────
        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ChunkingStrategy).HasMaxLength(200);
            entity.Property(e => e.EmbeddingModel).HasMaxLength(200);
            entity.HasOne(e => e.Document)
                  .WithMany(d => d.Chunks)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.DocumentId, e.Position });
        });

        // ── ChatSession ───────────────────────────
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.HasOne(e => e.Course)
                  .WithMany(c => c.ChatSessions)
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
        });

        // ── ChatMessage ───────────────────────────
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Session)
                  .WithMany(s => s.Messages)
                  .HasForeignKey(e => e.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.SessionId, e.CreatedAt });
        });

        // ── OtpToken ─────────────────────────────────
        modelBuilder.Entity<OtpToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(6).IsRequired();
            entity.Property(e => e.Purpose).HasMaxLength(50);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.UserId, e.Purpose, e.IsUsed });
        });

        // ── ChatMessageFeedback ────────────────────
        modelBuilder.Entity<ChatMessageFeedback>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Message)
                  .WithMany()
                  .HasForeignKey(e => e.MessageId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();
        });

        // ── SystemConfig ───────────────────────────
        modelBuilder.Entity<SystemConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Key).IsUnique();
        });

        // ── BenchmarkExperiment ───────────────────
        modelBuilder.Entity<BenchmarkExperiment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EmbeddingModel).HasMaxLength(100);
            entity.Property(e => e.ChunkingStrategy).HasMaxLength(100);
            entity.Property(e => e.Approach).HasMaxLength(100);
            entity.HasOne(e => e.Course)
                  .WithMany()
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BenchmarkResult ────────────────────
        modelBuilder.Entity<BenchmarkResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RetrievedContext).HasColumnType("nvarchar(max)");
            entity.HasOne(e => e.Experiment)
                  .WithMany(x => x.Results)
                  .HasForeignKey(e => e.ExperimentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BenchmarkQuestion ─────────────────────
        modelBuilder.Entity<BenchmarkQuestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasMaxLength(200);
            entity.Property(e => e.Difficulty).HasMaxLength(100);
            entity.Property(e => e.SourceDocumentNames).HasMaxLength(500);
            entity.HasOne(e => e.Course)
                  .WithMany()
                  .HasForeignKey(e => e.CourseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
