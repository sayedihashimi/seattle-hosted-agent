using Azure.AI.AgentServer.AgentFramework.Extensions;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Azure.AI.OpenAI;
using Azure.Identity;
using SeattleHotelAgent.Hosted.Agent.Tools;

var openAiEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
    ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "chat";
var tenantId = Environment.GetEnvironmentVariable("AZURE_AI_TENANT_ID");

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    TenantId = tenantId
});

// Register hotel tools for function calling
var tools = new AIFunction[]
{
    AIFunctionFactory.Create(HotelTools.SearchHotels),
    AIFunctionFactory.Create(HotelTools.GetHotelDetails),
    AIFunctionFactory.Create(HotelTools.CheckAvailability),
    AIFunctionFactory.Create(HotelTools.BookRoom)
};

var chatClient = new AzureOpenAIClient(new Uri(openAiEndpoint), credential)
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsBuilder()
    .UseFunctionInvocation()
    .UseOpenTelemetry(sourceName: "SeattleHotelAgent", configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

var agent = new ChatClientAgent(chatClient,
    name: "SeattleHotelConcierge",
    instructions: """
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
        """,
    tools: tools)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "SeattleHotelAgent", configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

await agent.RunAIAgentAsync(telemetrySourceName: "SeattleHotelAgent");
