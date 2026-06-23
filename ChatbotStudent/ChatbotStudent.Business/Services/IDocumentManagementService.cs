using ChatbotStudent.Data.Models;

namespace ChatbotStudent.Business.Services;

public interface IDocumentManagementService
{
    Task<List<Document>> GetDocumentsByCourseAsync(int? courseId, int userId, string role);
    Task<Document> UploadDocumentAsync(
        int courseId, string? chapter, string chunkingStrategy, string embeddingModel,
        string fileName, long fileSize, Stream fileStream, string uploadedByRole);
    Task<bool> DeleteDocumentAsync(int documentId);
    Task<object?> GetChunksJsonAsync(int documentId);
}
