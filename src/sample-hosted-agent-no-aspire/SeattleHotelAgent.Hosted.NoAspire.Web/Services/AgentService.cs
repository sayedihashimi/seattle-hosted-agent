using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeattleHotelAgent.Hosted.NoAspire.Web.Services;

public class AgentService(HttpClient httpClient)
{
    public async Task<string> SendMessageAsync(string input, List<ConversationTurn>? history = null)
    {
        try
        {
            // Build the input as an array of messages for conversation context
            var messages = new List<object>();

            if (history != null)
            {
                foreach (var turn in history)
                {
                    messages.Add(new { role = turn.Role, content = turn.Content });
                }
            }

            messages.Add(new { role = "user", content = input });

            var payload = JsonSerializer.Serialize(new { input = messages });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("/responses", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return $"Agent returned an error (HTTP {(int)response.StatusCode}): {errorBody}";
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("code", out var code))
            {
                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                return $"Agent error ({code.GetString()}): {message}";
            }

            if (root.TryGetProperty("output", out var output))
            {
                var texts = new List<string>();
                foreach (var item in output.EnumerateArray())
                {
                    if (item.TryGetProperty("type", out var type) && type.GetString() == "message" &&
                        item.TryGetProperty("content", out var contentArray))
                    {
                        foreach (var contentItem in contentArray.EnumerateArray())
                        {
                            if (contentItem.TryGetProperty("text", out var text))
                            {
                                texts.Add(text.GetString() ?? "");
                            }
                        }
                    }
                }

                return texts.Count > 0
                    ? string.Join("\n\n", texts)
                    : "The agent didn't return a text response.";
            }

            return "Unexpected response format from the agent.";
        }
        catch (TaskCanceledException)
        {
            return "The request timed out. The agent may be processing a complex query — please try again.";
        }
        catch (HttpRequestException ex)
        {
            return $"Could not connect to the agent at {httpClient.BaseAddress}. Make sure the agent is running. Error: {ex.Message}";
        }
    }
}

public record ConversationTurn(string Role, string Content);
