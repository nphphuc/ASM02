using ChatbotStudent.Data;
using ChatbotStudent.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace ChatbotStudent.Pages.Api;

[Authorize]
public class ChatFeedbackModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ILogger<ChatFeedbackModel> _logger;

    public ChatFeedbackModel(AppDbContext db, ILogger<ChatFeedbackModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var messageIdStr = Request.Form["messageId"];
        var feedbackStr = Request.Form["feedback"];
        var comment = Request.Form["comment"];

        if (!int.TryParse(messageIdStr, out var messageId) || !int.TryParse(feedbackStr, out var feedbackValue))
            return new JsonResult(new { success = false, error = "Invalid parameters" });

        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

        // Check if feedback already exists
        var existing = await _db.ChatMessageFeedbacks
            .FirstOrDefaultAsync(f => f.MessageId == messageId && f.UserId == userId);

        if (existing != null)
        {
            existing.Feedback = (FeedbackType)feedbackValue;
            if (!string.IsNullOrEmpty(comment))
                existing.Comment = comment;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            _db.ChatMessageFeedbacks.Add(new ChatMessageFeedback
            {
                MessageId = messageId,
                UserId = userId,
                Feedback = (FeedbackType)feedbackValue,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return new JsonResult(new { success = true });
    }
}
