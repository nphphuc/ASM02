using ChatbotStudent.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        return new DashboardStats
        {
            CourseCount = await _db.Courses.CountAsync(),
            DocumentCount = await _db.Documents.CountAsync(),
            ChunkCount = await _db.DocumentChunks.CountAsync(),
            SessionCount = await _db.ChatSessions.CountAsync()
        };
    }
}
