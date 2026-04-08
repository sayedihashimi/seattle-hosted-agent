using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using SeattleHotelAgent.Api.Models;
using SeattleHotelAgent.Api.Tools;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddOpenApi();

// Get Foundry endpoint from connection string or configuration
var connectionString = builder.Configuration.GetConnectionString("chat");
string endpoint;
string deploymentName;

if (!string.IsNullOrEmpty(connectionString))
{
    // Parse "Endpoint=https://...;DeploymentId=chat"
    var parts = connectionString.Split(';').Select(p => p.Split('=', 2)).ToDictionary(p => p[0].Trim(), p => p[1].Trim());
    endpoint = parts["Endpoint"].TrimEnd('/');
    deploymentName = parts.GetValueOrDefault("DeploymentId", "chat");
}
else
{
    endpoint = builder.Configuration["AzureAI:Endpoint"]
        ?? throw new InvalidOperationException("Set ConnectionStrings:chat or AzureAI:Endpoint");
    deploymentName = builder.Configuration["AzureAI:DeploymentName"] ?? "chat";
}

// Create the chat client using AzureOpenAIClient + MEAI
var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    TenantId = builder.Configuration["AzureAI:TenantId"]
});

var chatClient = new AzureOpenAIClient(new Uri(endpoint), credential)
    .GetChatClient(deploymentName)
    .AsIChatClient();

builder.Services.AddChatClient(chatClient)
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: "SeattleHotelAgent");

// Register hotel tools for the agent
builder.Services.AddSingleton<IList<AITool>>(
[
    AIFunctionFactory.Create(HotelTools.SearchHotels),
    AIFunctionFactory.Create(HotelTools.GetHotelDetails),
    AIFunctionFactory.Create(HotelTools.CheckAvailability),
    AIFunctionFactory.Create(HotelTools.BookRoom)
]);

var app = builder.Build();
app.MapDefaultEndpoints();
app.MapOpenApi();

var agentInstructions = """
    You are the Seattle Hotel Concierge, a friendly and knowledgeable AI assistant that helps 
    travelers find and book hotels in Seattle, Washington.

    Your capabilities:
    - Search for hotels by neighborhood, star rating, price, and guest count
    - Provide detailed information about specific hotels
    - Check room availability for specific dates
    - Book hotel rooms

    Guidelines:
    - Always be warm and welcoming — Seattle is a great city to visit!
    - When users ask vague questions, help narrow down their preferences
    - Suggest neighborhoods based on what they want to do (e.g., Pike Place for food lovers, 
      Capitol Hill for nightlife, Ballard for breweries, Fremont for quirky arts)
    - Always confirm booking details before finalizing
    - Mention relevant amenities that match what the user seems to care about
    - If dates aren't provided, ask for them before checking availability
    """;

app.MapPost("/api/chat", async (ChatRequest request, IChatClient chatClient, IList<AITool> tools) =>
{
    var messages = new List<ChatMessage>
    {
        new(ChatRole.System, agentInstructions),
        new(ChatRole.User, request.Message)
    };

    var response = await chatClient.GetResponseAsync(
        messages,
        new() { Tools = [.. tools] });

    return Results.Ok(new AgentResponse
    {
        Reply = response.Text ?? "I'm sorry, I couldn't process that request.",
        SessionId = request.SessionId ?? Guid.NewGuid().ToString()[..8]
    });
});

app.MapGet("/api/hotels", () => Results.Ok(HotelData.Hotels));

app.MapGet("/api/hotels/{id}", (string id) =>
{
    var hotel = HotelData.Hotels.FirstOrDefault(h =>
        h.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    return hotel is not null ? Results.Ok(hotel) : Results.NotFound();
});

app.Run();

