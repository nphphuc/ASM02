namespace ChatbotStudent.Business.Services;

public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string ChatModel { get; set; } = "gpt-3.5-turbo";
    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.3;
}

public class EmbeddingServiceOptions
{
    public string DefaultProvider { get; set; } = "OpenAI";
    public string LocalApiUrl { get; set; } = "http://localhost:8000";
}

public class RagSettings
{
    public int TopKChunks { get; set; } = 5;
    public double SimilarityThreshold { get; set; } = 0.5;
    public int MaxContextLength { get; set; } = 4000;
}

public class EmailSettings
{
    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
