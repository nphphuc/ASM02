using System.Net.Http.Json;
using System.Text.Json;
using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

public interface IOpenAiChatClient
{
    Task<string> ChatAsync(string systemPrompt, string userPrompt, List<ChatMessage>? history = null);
}

public class OpenAiChatClient : IOpenAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatClient> _logger;

    public OpenAiChatClient(
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt,
        List<ChatMessage>? history = null)
    {
        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };

        // Add conversation history if available
        if (history != null)
        {
            foreach (var msg in history.TakeLast(10)) // Last 10 messages for context
            {
                messages.Add(new
                {                        role = msg.Role == MessageRole.User ? "user" : "assistant",
                    content = msg.Content
                });
            }
        }

        messages.Add(new { role = "user", content = userPrompt });

        var request = new
        {
            model = _options.ChatModel,
            messages = messages.ToArray(),
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature
        };

        var response = await _httpClient.PostAsJsonAsync("/chat/completions", request);

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenRouter API returned {StatusCode}: {Body}",
                response.StatusCode, json[..Math.Min(json.Length, 500)]);
            return $"❌ Lỗi từ AI: {response.StatusCode}. Vui lòng thử lại sau.";
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? "Xin lỗi, không thể tạo câu trả lời.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON from OpenRouter API: {Body}",
                json[..Math.Min(json.Length, 500)]);
            return $"❌ Lỗi xử lý phản hồi từ AI: Dịch vụ tạm thời không khả dụng.";
        }
    }
}
