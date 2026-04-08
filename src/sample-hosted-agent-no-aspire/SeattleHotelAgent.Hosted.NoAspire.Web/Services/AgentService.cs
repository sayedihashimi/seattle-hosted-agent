using System.Text.Json;

namespace SeattleHotelAgent.Hosted.NoAspire.Web.Services;

public class AgentService(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<string> SendMessageAsync(string input)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { input });
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

            // Check for error response
            if (root.TryGetProperty("code", out var code))
            {
                var message = root.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
                return $"Agent error ({code.GetString()}): {message}";
            }

            // Extract text from output[].content[].text
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
