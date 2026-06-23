using ChatbotStudent.Data;
using ChatbotStudent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(AppDbContext db, ILogger<SettingsModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<SystemConfig> Configs { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadConfigsAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync(List<string> keys, List<string> values)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        for (var i = 0; i < keys.Count && i < values.Count; i++)
        {
            var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == keys[i]);
            if (config != null)
            {
                config.Value = values[i];
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedByUserId = userId;
            }
        }

        await _db.SaveChangesAsync();
        SuccessMessage = "Đã lưu cấu hình thành công.";
        await LoadConfigsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        _db.SystemConfigs.RemoveRange(await _db.SystemConfigs.ToListAsync());
        await _db.SaveChangesAsync();

        foreach (var (key, defaultValue, description, category) in SystemConfigDefaults.GetDefaults())
        {
            _db.SystemConfigs.Add(new SystemConfig
            {
                Key = key,
                Value = defaultValue,
                Description = description,
                Category = category,
                UpdatedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();

        SuccessMessage = "Đã khôi phục cấu hình mặc định.";
        await LoadConfigsAsync();
        return Page();
    }

    private async Task LoadConfigsAsync()
    {
        var configs = await _db.SystemConfigs.ToListAsync();

        // Ensure all default configs exist
        foreach (var (key, defaultValue, description, category) in SystemConfigDefaults.GetDefaults())
        {
            if (!configs.Any(c => c.Key == key))
            {
                var config = new SystemConfig
                {
                    Key = key,
                    Value = defaultValue,
                    Description = description,
                    Category = category,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.SystemConfigs.Add(config);
                configs.Add(config);
            }
        }
        await _db.SaveChangesAsync();

        Configs = configs.OrderBy(c => c.Category).ThenBy(c => c.Key).ToList();
    }
}
