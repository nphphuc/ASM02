using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;

    public IndexModel(IDashboardService dashboardService) => _dashboardService = dashboardService;

    public int CourseCount { get; set; }
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public int SessionCount { get; set; }

    public string? RegistrationSuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        var stats = await _dashboardService.GetStatsAsync();
        CourseCount = stats.CourseCount;
        DocumentCount = stats.DocumentCount;
        ChunkCount = stats.ChunkCount;
        SessionCount = stats.SessionCount;

        // Check for registration success message
        if (TempData.ContainsKey("RegistrationSuccess"))
        {
            RegistrationSuccessMessage = TempData["RegistrationSuccess"] as string;
        }
    }
}
