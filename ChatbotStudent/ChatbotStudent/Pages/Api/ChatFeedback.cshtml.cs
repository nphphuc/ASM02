using ChatbotStudent.Data.Models;
using ChatbotStudent.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChatbotStudent.Web.Pages.Api;

[Authorize]
public class ChatFeedbackModel : PageModel
{
    private readonly IChatFeedbackService _feedbackService;
    private readonly ILogger<ChatFeedbackModel> _logger;

    public ChatFeedbackModel(IChatFeedbackService feedbackService, ILogger<ChatFeedbackModel> logger)
    {
        _feedbackService = feedbackService;
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

        await _feedbackService.SubmitFeedbackAsync(messageId, userId, (FeedbackType)feedbackValue, comment);

        return new JsonResult(new { success = true });
    }
}
