using System.Net.Http.Json;
using System.Text.Json;
using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

/// <summary>
/// Chat client for Google Gemini API.
/// Implements IOpenAiChatClient with the same interface but calls Gemini's generateContent endpoint.
/// Gemini API: POST {BaseUrl}/models/{model}:generateContent?key={apiKey}
/// </summary>
public class GeminiChatClient : IOpenAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<GeminiChatClient> _logger;

    public GeminiChatClient(
        HttpClient httpClient,
        Microsoft.Extensions.Options.IOptions<OpenAiOptions> options,
        ILogger<GeminiChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> ChatAsync(string systemPrompt, string userPrompt,
        List<ChatMessage>? history = null)
    {
        var apiKey = _options.ApiKey;
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var model = _options.ChatModel;
        var url = $"{baseUrl}/models/{model}:generateContent?key={apiKey}";

        // Build contents array (conversation history)
        var contents = new List<object>();

        // Add conversation history if available
        if (history != null)
        {
            foreach (var msg in history.TakeLast(10))
            {
                contents.Add(new
                {
                    role = msg.Role == MessageRole.User ? "user" : "model",
                    parts = new[] { new { text = msg.Content } }
                });
            }
        }

        // Add current user message
        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userPrompt } }
        });

        // Build request body
        var requestBody = new Dictionary<string, object>
        {
            ["contents"] = contents,
            ["generationConfig"] = new
            {
                temperature = _options.Temperature,
                maxOutputTokens = _options.MaxTokens
            }
        };

        // Add system instruction if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            requestBody["systemInstruction"] = new
            {
                parts = new[] { new { text = systemPrompt } }
            };
        }

        var response = await _httpClient.PostAsJsonAsync(url, requestBody);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini API returned {StatusCode}: {Body}",
                response.StatusCode, TruncateJson(json));
            return $"❌ Lỗi từ AI: {response.StatusCode}. Vui lòng thử lại sau.";
        }

        return ParseGeminiResponse(json);
    }

    private string ParseGeminiResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            // Check for error in response
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                var message = error.TryGetProperty("message", out var msg)
                    ? msg.GetString() ?? "Unknown error"
                    : "Unknown error";
                _logger.LogError("Gemini API error: {Error}", message);
                return $"❌ Lỗi Gemini API: {message}";
            }

            // Parse candidates[0].content.parts[0].text
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0)
            {
                // Check if blocked by safety
                if (doc.RootElement.TryGetProperty("promptFeedback", out var feedback))
                {
                    var blockReason = feedback.TryGetProperty("blockReason", out var reason)
                        ? reason.GetString()
                        : "Unknown";
                    _logger.LogWarning("Gemini response blocked: {Reason}", blockReason);
                    return "❌ Câu trả lời đã bị chặn do vi phạm nguyên tắc an toàn.";
                }
                return "Xin lỗi, không thể tạo câu trả lời.";
            }

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "Xin lỗi, không thể tạo câu trả lời.";
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON from Gemini API: {Body}", TruncateJson(json));
            return $"❌ Lỗi xử lý phản hồi từ AI: Dịch vụ tạm thời không khả dụng.";
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogError(ex, "Unexpected response format from Gemini API: {Body}", TruncateJson(json));
            return $"❌ Lỗi xử lý phản hồi từ AI: Định dạng không mong đợi.";
        }
    }

    private static string TruncateJson(string json)
    {
        return json.Length > 500 ? json[..500] + "..." : json;
    }
}
