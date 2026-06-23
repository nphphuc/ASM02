namespace ChatbotStudent.Services;

public class ChunkingService : IChunkingService
{
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(ILogger<ChunkingService> logger)
    {
        _logger = logger;
    }

    public List<TextChunk> ChunkText(string text, string strategyName, int maxTokensPerChunk = 512)
    {
        var strategy = strategyName.ToLowerInvariant() switch
        {
            "fixedsize" or "fixed" => ChunkingStrategy.FixedSize,
            "sentence" or "sentencebased" => ChunkingStrategy.SentenceBased,
            "paragraph" or "paragraphbased" => ChunkingStrategy.ParagraphBased,
            "recursive" => ChunkingStrategy.Recursive,
            _ => ChunkingStrategy.Recursive
        };
        return ChunkText(text, strategy, maxTokensPerChunk);
    }

    public List<TextChunk> ChunkText(string text, ChunkingStrategy strategy, int maxTokensPerChunk = 512)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<TextChunk>();

        var chunks = strategy switch
        {
            ChunkingStrategy.FixedSize => FixedSizeChunking(text, maxTokensPerChunk),
            ChunkingStrategy.SentenceBased => SentenceBasedChunking(text, maxTokensPerChunk),
            ChunkingStrategy.ParagraphBased => ParagraphBasedChunking(text, maxTokensPerChunk),
            ChunkingStrategy.Recursive => RecursiveChunking(text, maxTokensPerChunk),
            _ => RecursiveChunking(text, maxTokensPerChunk)
        };

        _logger.LogInformation("Chunked text into {Count} chunks using {Strategy} strategy",
            chunks.Count, strategy);
        return chunks;
    }

    private List<TextChunk> FixedSizeChunking(string text, int maxTokens)
    {
        // Approximate: 1 token ≈ 4 characters for English, ~2 for Vietnamese
        var maxChars = maxTokens * 3;
        var chunks = new List<TextChunk>();
        var position = 0;

        for (var i = 0; i < text.Length; i += maxChars)
        {
            var length = Math.Min(maxChars, text.Length - i);
            var content = text.Substring(i, length);

            chunks.Add(new TextChunk
            {
                Content = content.Trim(),
                Position = position++,
                TokenCount = EstimateTokenCount(content),
                Strategy = "FixedSize"
            });
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();
    }

    private List<TextChunk> SentenceBasedChunking(string text, int maxTokens)
    {
        var sentences = SplitIntoSentences(text);
        var chunks = new List<TextChunk>();
        var currentChunk = new System.Text.StringBuilder();
        var currentTokens = 0;
        var position = 0;

        foreach (var sentence in sentences)
        {
            var sentenceTokens = EstimateTokenCount(sentence);

            if (currentTokens + sentenceTokens > maxTokens && currentChunk.Length > 0)
            {
                chunks.Add(new TextChunk
                {
                    Content = currentChunk.ToString().Trim(),
                    Position = position++,
                    TokenCount = currentTokens,
                    Strategy = "SentenceBased"
                });
                currentChunk.Clear();
                currentTokens = 0;
            }

            currentChunk.AppendLine(sentence);
            currentTokens += sentenceTokens;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new TextChunk
            {
                Content = currentChunk.ToString().Trim(),
                Position = position++,
                TokenCount = currentTokens,
                Strategy = "SentenceBased"
            });
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();
    }

    private List<TextChunk> ParagraphBasedChunking(string text, int maxTokens)
    {
        var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<TextChunk>();
        var currentChunk = new System.Text.StringBuilder();
        var currentTokens = 0;
        var position = 0;

        foreach (var paragraph in paragraphs)
        {
            var paraTokens = EstimateTokenCount(paragraph);

            if (currentTokens + paraTokens > maxTokens && currentChunk.Length > 0)
            {
                chunks.Add(new TextChunk
                {
                    Content = currentChunk.ToString().Trim(),
                    Position = position++,
                    TokenCount = currentTokens,
                    Strategy = "ParagraphBased"
                });
                currentChunk.Clear();
                currentTokens = 0;
            }

            currentChunk.AppendLine(paragraph);
            currentChunk.AppendLine();
            currentTokens += paraTokens;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(new TextChunk
            {
                Content = currentChunk.ToString().Trim(),
                Position = position++,
                TokenCount = currentTokens,
                Strategy = "ParagraphBased"
            });
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c.Content)).ToList();
    }

    private List<TextChunk> RecursiveChunking(string text, int maxTokens)
    {
        // Recursive splitting: try paragraph → sentence → character boundaries
        var separators = new[] { "\n\n", "\n", ". ", "! ", "? ", "; ", ", ", " " };
        return RecursiveSplit(text, separators, 0, maxTokens, 0);
    }

    private List<TextChunk> RecursiveSplit(string text, string[] separators, int separatorIndex,
        int maxTokens, int position)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<TextChunk>();

        var tokens = EstimateTokenCount(text);
        if (tokens <= maxTokens)
        {
            return new List<TextChunk>
            {
                new TextChunk
                {
                    Content = text.Trim(),
                    Position = position,
                    TokenCount = tokens,
                    Strategy = "Recursive"
                }
            };
        }

        if (separatorIndex >= separators.Length)
        {
            // Force split at character boundary
            var maxChars = maxTokens * 3;
            var chunks = new List<TextChunk>();
            for (var i = 0; i < text.Length; i += maxChars)
            {
                var length = Math.Min(maxChars, text.Length - i);
                var content = text.Substring(i, length).Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    chunks.Add(new TextChunk
                    {
                        Content = content,
                        Position = position++,
                        TokenCount = EstimateTokenCount(content),
                        Strategy = "Recursive"
                    });
                }
            }
            return chunks;
        }

        var sep = separators[separatorIndex];
        var parts = text.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<TextChunk>();

        var currentChunk = new System.Text.StringBuilder();
        var currentTokens = 0;

        foreach (var part in parts)
        {
            var partWithSep = part + sep;
            var partTokens = EstimateTokenCount(partWithSep);

            if (currentTokens + partTokens > maxTokens && currentChunk.Length > 0)
            {
                result.AddRange(RecursiveSplit(
                    currentChunk.ToString(), separators, separatorIndex + 1,
                    maxTokens, position));
                position = result.Count > 0 ? result.Max(c => c.Position) + 1 : position;
                currentChunk.Clear();
                currentTokens = 0;
            }

            currentChunk.Append(partWithSep);
            currentTokens += partTokens;
        }

        if (currentChunk.Length > 0)
        {
            result.AddRange(RecursiveSplit(
                currentChunk.ToString(), separators, separatorIndex + 1,
                maxTokens, position));
        }

        return result;
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var current = new System.Text.StringBuilder();

        foreach (var c in text)
        {
            current.Append(c);
            if (c is '.' or '!' or '?' or '。' or '!' or '?')
            {
                var s = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(s))
                    sentences.Add(s);
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            var s = current.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(s))
                sentences.Add(s);
        }

        return sentences;
    }

    private static int EstimateTokenCount(string text)
    {
        // Rough estimation: ~3 chars per token for mixed Vietnamese/English
        return Math.Max(1, (int)Math.Ceiling(text.Length / 3.0));
    }
}
