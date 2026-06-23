using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class SettingsModel : PageModel
{
    private readonly ISystemConfigService _configService;
    private readonly ILogger<SettingsModel> _logger;

    public SettingsModel(ISystemConfigService configService, ILogger<SettingsModel> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public List<Data.Models.SystemConfig> Configs { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Configs = await _configService.GetAllConfigsAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync(List<string> keys, List<string> values)
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        var configs = new List<(string Key, string Value)>();
        for (var i = 0; i < keys.Count && i < values.Count; i++)
        {
            configs.Add((keys[i], values[i]));
        }

        await _configService.SaveConfigsAsync(configs, userId);
        SuccessMessage = "Đã lưu cấu hình thành công.";
        Configs = await _configService.GetAllConfigsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await _configService.ResetConfigsAsync();
        SuccessMessage = "Đã khôi phục cấu hình mặc định.";
        Configs = await _configService.GetAllConfigsAsync();
        return Page();
    }
}
