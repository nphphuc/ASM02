using ChatbotStudent.Data;
using ChatbotStudent.Models;
using ChatbotStudent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages.Research;

[Authorize(Roles = "Admin,Lecturer")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IBenchmarkService _benchmark;

    public IndexModel(AppDbContext db, IBenchmarkService benchmark)
    {
        _db = db;
        _benchmark = benchmark;
    }

    public List<Course> Courses { get; set; } = new();
    public List<BenchmarkExperiment> Experiments { get; set; } = new();
    public List<BenchmarkSummary> Summaries { get; set; } = new();
    public int? SelectedCourseId { get; set; }

    public async Task OnGetAsync(int? courseId = null)
    {
        Courses = await _db.Courses.ToListAsync();

        if (courseId.HasValue)
        {
            SelectedCourseId = courseId;
            Experiments = await _benchmark.GetExperimentsAsync(courseId.Value);
            Summaries = await _benchmark.GetSummaryAsync(courseId.Value);
        }
    }

    public async Task<IActionResult> OnPostRunExperimentAsync(
        int courseId, string name, string type, string? embeddingModel,
        string? chunkingStrategy, string? approach)
    {
        var experimentType = type switch
        {
            "RAG_VS_FINETUNING" => ExperimentType.RAG_VS_FINETUNING,
            "CHUNKING_STRATEGY" => ExperimentType.CHUNKING_STRATEGY,
            "EMBEDDING_MODEL" => ExperimentType.EMBEDDING_MODEL,
            _ => ExperimentType.RAG_VS_FINETUNING
        };

        // Run experiment in background
        _ = Task.Run(async () =>
        {
            using var scope = HttpContext.RequestServices.CreateScope();
            var benchmark = scope.ServiceProvider.GetRequiredService<IBenchmarkService>();
            await benchmark.RunExperimentAsync(courseId, name, experimentType,
                embeddingModel, chunkingStrategy, approach);
        });

        TempData["Info"] = $"Experiment \"{name}\" đã bắt đầu chạy. Trang sẽ tự động cập nhật.";
        return RedirectToPage(new { courseId });
    }
}
