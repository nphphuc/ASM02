namespace ChatbotStudent.Services;

public enum ChunkingStrategy
{
    FixedSize,
    SentenceBased,
    ParagraphBased,
    Recursive
}

public class TextChunk
{
    public string Content { get; set; } = string.Empty;
    public int Position { get; set; }
    public int TokenCount { get; set; }
    public string Strategy { get; set; } = string.Empty;
}

public interface IChunkingService
{
    List<TextChunk> ChunkText(string text, ChunkingStrategy strategy, int maxTokensPerChunk = 512);
    List<TextChunk> ChunkText(string text, string strategyName, int maxTokensPerChunk = 512);
}
