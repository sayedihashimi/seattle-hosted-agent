using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SeattleHotelAgent.Hosted.NoAspire.Web.Services;

namespace SeattleHotelAgent.Hosted.NoAspire.Web.Pages;

public class IndexModel(AgentService agentService) : PageModel
{
    public List<ChatMessage> Conversation { get; set; } = [];
    public string? ErrorMessage { get; set; }

    public void OnGet(string? ask)
    {
        LoadConversation();
        PrefilledMessage = ask;
    }

    public string? PrefilledMessage { get; set; }

    public async Task<IActionResult> OnPostAsync(string userMessage)
    {
        LoadConversation();

        if (string.IsNullOrWhiteSpace(userMessage))
            return Page();

        Conversation.Add(new ChatMessage(userMessage, true));

        var reply = await agentService.SendMessageAsync(userMessage);

        if (reply.StartsWith("Could not connect") || reply.StartsWith("The request timed out"))
        {
            ErrorMessage = reply;
        }
        else
        {
            Conversation.Add(new ChatMessage(reply, false));
        }

        SaveConversation();
        return Page();
    }

    public IActionResult OnPostClear()
    {
        HttpContext.Session.Remove("Conversation");
        return RedirectToPage();
    }

    private void LoadConversation()
    {
        var json = HttpContext.Session.GetString("Conversation");
        if (!string.IsNullOrEmpty(json))
        {
            Conversation = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? [];
        }
    }

    private void SaveConversation()
    {
        var json = JsonSerializer.Serialize(Conversation);
        HttpContext.Session.SetString("Conversation", json);
    }
}

public record ChatMessage(string Text, bool IsUser);
