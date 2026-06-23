using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

public interface ISystemConfigService
{
    Task<List<SystemConfig>> GetAllConfigsAsync();
    Task SaveConfigsAsync(List<(string Key, string Value)> configs, int updatedByUserId);
    Task ResetConfigsAsync();
    Task EnsureDefaultsAsync();
}
