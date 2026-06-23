namespace ChatbotStudent.Services;

public interface IDocumentParserService
{
    /// <summary>
    /// Extract text content from a document file stream.
    /// </summary>
    /// <param name="fileStream">The uploaded file stream</param>
    /// <param name="fileName">Original filename to determine type</param>
    /// <returns>Extracted text content</returns>
    Task<string> ExtractTextAsync(Stream fileStream, string fileName);
}
