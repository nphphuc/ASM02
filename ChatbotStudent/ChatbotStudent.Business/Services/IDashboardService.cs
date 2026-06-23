namespace ChatbotStudent.Business.Services;

public class DashboardStats
{
    public int CourseCount { get; set; }
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public int SessionCount { get; set; }
}

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync();
}
