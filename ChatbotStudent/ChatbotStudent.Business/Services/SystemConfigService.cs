using ChatbotStudent.Data;
using ChatbotStudent.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Business.Services;

public class SystemConfigService : ISystemConfigService
{
    private readonly AppDbContext _db;

    public SystemConfigService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SystemConfig>> GetAllConfigsAsync()
    {
        var configs = await _db.SystemConfigs.ToListAsync();

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

        return configs.OrderBy(c => c.Category).ThenBy(c => c.Key).ToList();
    }

    public async Task SaveConfigsAsync(List<(string Key, string Value)> configs, int updatedByUserId)
    {
        foreach (var (key, value) in configs)
        {
            var config = await _db.SystemConfigs.FirstOrDefaultAsync(c => c.Key == key);
            if (config != null)
            {
                config.Value = value;
                config.UpdatedAt = DateTime.UtcNow;
                config.UpdatedByUserId = updatedByUserId;
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task ResetConfigsAsync()
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
    }

    public async Task EnsureDefaultsAsync()
    {
        await GetAllConfigsAsync(); // This method already ensures defaults
    }
}
