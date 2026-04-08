# Build a Foundry Hosted Agent with .NET Aspire and the Responses Protocol

In this tutorial we will build a hotel booking agent that runs as a Foundry Hosted Agent using the OpenAI Responses protocol. This is a companion to the [ACA-based tutorial](quickstart-aca.md) — it uses the same hotel data and tools, but instead of custom REST endpoints, the agent implements the [Responses protocol](https://platform.openai.com/docs/api-reference/responses) via the Azure AI AgentServer framework. The completed code for this tutorial can be found at [seattle-hosted-agent](https://github.com/sayedihashimi/seattle-hosted-agent).

In this tutorial we will cover the following.

- Creating a .NET Aspire solution with a console-based agent
- Provisioning Azure AI Foundry resources
- Reusing hotel data and AI tool functions
- Wiring up the agent with the AgentServer framework and Responses protocol
- Testing the agent locally
- Key differences from the ACA-based approach

## Prerequisites

Before getting started, ensure you have the following installed.

| Prerequisite | Description |
|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | .NET 10 or later |
| [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) | `az` command line tool |
| Azure subscription | An active Azure subscription |

You will also need an Azure AI Foundry resource with a GPT-4o-mini deployment. If you have already completed the [ACA-based tutorial](quickstart-aca.md), you can reuse the same Foundry resource. If not, follow the provisioning steps in that tutorial first.

## How this differs from the ACA version

Before we start building, it is helpful to understand the key differences between this approach and the ACA-based approach.

| Aspect | ACA Version | Hosted Agent Version |
|---|---|---|
| **Project type** | ASP.NET Core Web API | Console app |
| **HTTP server** | You build endpoints with minimal APIs | AgentServer framework handles it |
| **Protocol** | Custom REST (`/api/chat`, `/api/hotels`) | OpenAI Responses protocol (`/responses`) |
| **Port** | Dynamic (from launch settings) | 8088 (AgentServer default) |
| **Additional APIs** | Hotel listing, hotel details, OpenAPI | Health check only (`/readiness`) |
| **Key package** | `Microsoft.Extensions.AI` (10.4.x) | `Azure.AI.AgentServer.AgentFramework` (beta) |

The Hosted Agent approach is simpler — you define your agent and call `RunAIAgentAsync()`, and the framework handles the HTTP server, the Responses protocol, and tool invocation. The tradeoff is that you are constrained to the Responses protocol and cannot add custom endpoints.

## Getting started — creating the Aspire solution

To get started we need to create a new .NET Aspire solution with a console app for the agent. Open a terminal and run the following commands.

```
mkdir SeattleHotelAgent.Hosted
cd SeattleHotelAgent.Hosted
dotnet new aspire -n SeattleHotelAgent.Hosted -o .
```

This creates the AppHost and ServiceDefaults projects. Now create the agent project as a console application.

```
dotnet new console -n SeattleHotelAgent.Hosted.Agent -o SeattleHotelAgent.Hosted.Agent
dotnet sln add SeattleHotelAgent.Hosted.Agent
dotnet add SeattleHotelAgent.Hosted.Agent reference SeattleHotelAgent.Hosted.ServiceDefaults
dotnet add SeattleHotelAgent.Hosted.AppHost reference SeattleHotelAgent.Hosted.Agent
```

> **Note:** We are using a console app instead of a web API because the AgentServer framework provides its own HTTP server. There is no need for ASP.NET Core middleware, controllers, or minimal APIs.

Now add the NuGet packages. Package versions are important here — the AgentServer framework has strict compatibility requirements with Microsoft.Extensions.AI.

```
cd SeattleHotelAgent.Hosted.Agent
dotnet add package Azure.AI.AgentServer.AgentFramework --version 1.0.0-beta.9
dotnet add package Microsoft.Agents.AI.OpenAI --version 1.0.0-rc1
dotnet add package Azure.AI.OpenAI --version 2.5.0-beta.1
dotnet add package Azure.Identity --version 1.17.0
dotnet add package Microsoft.Extensions.AI --version 10.3.0
dotnet add package Microsoft.Extensions.AI.OpenAI --version 10.3.0
cd ..
```

> **Important:** Do not use `Microsoft.Extensions.AI` version 10.4.x with `Azure.AI.AgentServer.AgentFramework` 1.0.0-beta.9. The AgentFramework was compiled against MEAI 10.3 and will throw a `TypeLoadException` at runtime if you use a newer version. See the [learnings document](../learnings/foundry-hosted-agent-learnings.md) for details on this compatibility issue.

Build the solution to ensure everything is configured correctly.

```
dotnet build
```

## Adding hotel data and tools

The hotel data and tool functions are identical to the ACA version. Create the **Models** and **Tools** folders in the agent project.

```
mkdir SeattleHotelAgent.Hosted.Agent/Models
mkdir SeattleHotelAgent.Hosted.Agent/Tools
```

Copy the following files from the ACA tutorial (or from the [source repository](https://github.com/sayedihashimi/seattle-hosted-agent)):

- **Models/HotelModels.cs** — Hotel, Room, and other record types
- **Models/HotelData.cs** — 8 fake Seattle hotels with rooms, amenities, and pricing
- **Tools/HotelTools.cs** — SearchHotels, GetHotelDetails, CheckAvailability, BookRoom

After copying, update the namespace in each file from `SeattleHotelAgent.Api` to `SeattleHotelAgent.Hosted.Agent`.

For example, the top of each file should read:

```csharp
namespace SeattleHotelAgent.Hosted.Agent.Models;
// or
namespace SeattleHotelAgent.Hosted.Agent.Tools;
```

The tool functions are the same static methods decorated with `[Description]` attributes. See the [ACA tutorial](quickstart-aca.md#creating-ai-tool-functions) for the full source code.

## Wiring up the agent in Program.cs

This is where the Hosted Agent approach differs significantly from the ACA version. Instead of building an ASP.NET Core app with endpoints, we create a `ChatClientAgent` and call `RunAIAgentAsync()`. Open **Program.cs** in the agent project and replace the contents with the following code.

**Program.cs**

```csharp
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
        - Suggest neighborhoods based on what they want to do
        - Always confirm booking details before finalizing
        - If dates aren't provided, ask for them before checking availability
        """,
    tools: tools)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: "SeattleHotelAgent", configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

await agent.RunAIAgentAsync(telemetrySourceName: "SeattleHotelAgent");
```

Let's walk through the key differences from the ACA version.

**No WebApplication.** There is no `WebApplication.CreateBuilder()`, no `MapGet()`, no `MapPost()`. The `RunAIAgentAsync()` method starts a Kestrel HTTP server on port 8088 and exposes the Responses protocol endpoints automatically.

**Tools on the agent.** Instead of registering tools in DI and passing them in `ChatOptions`, tools are passed directly to the `ChatClientAgent` constructor. The agent handles tool invocation as part of the Responses protocol flow.

**Configuration via environment variables.** The AgentServer pattern uses environment variables (`AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_DEPLOYMENT_NAME`) rather than ASP.NET Core configuration (`appsettings.json`, connection strings).

**ChatClientAgent from MAF.** We use `ChatClientAgent` from `Microsoft.Agents.AI` which wraps the `IChatClient` and provides the agent abstraction that the AgentServer framework expects.

## Configuring the AppHost

Open the **AppHost.cs** file in the AppHost project and update it to register the agent project.

**AppHost.cs**

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.SeattleHotelAgent_Hosted_Agent>("hotel-agent")
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

## Running the agent locally

Set the environment variables for your Foundry resource. Replace the endpoint below with the value from your Azure AI Services resource.

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE.cognitiveservices.azure.com"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "chat"
```

If your subscription tenant differs from your default login tenant, also set the tenant ID.

```powershell
# Get your tenant ID
az account show --query tenantId -o tsv

# Set it
$env:AZURE_AI_TENANT_ID = "YOUR-TENANT-ID"
```

Run the agent directly.

```
dotnet run --project SeattleHotelAgent.Hosted.Agent
```

The agent will start on `http://[::]:8088`. You can verify it is running by checking the readiness endpoint.

```powershell
Invoke-RestMethod -Uri "http://localhost:8088/readiness"
```

This should return `Healthy`.

## Testing with the Responses protocol

The Responses protocol uses a single `POST /responses` endpoint. The request body contains an `input` field with the user's message. Test with the following command.

```powershell
$body = @{ input = "Find me a budget hotel in Ballard for 2 guests, under 200 dollars per night" } | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "http://localhost:8088/responses" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 120
($resp.Content | ConvertFrom-Json).output | ForEach-Object {
    if ($_.type -eq "message" -and $_.content) {
        $_.content | ForEach-Object { if ($_.text) { Write-Host $_.text } }
    }
}
```

You should see the agent respond with the Ballard Nordic Lodge recommendation.

You can also test booking directly. Note that each request is independent — the agent does not remember previous messages.

```powershell
$body = @{ input = "Book a Standard Double at ballard-lodge for Jane Doe from 2026-06-01 to 2026-06-03" } | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "http://localhost:8088/responses" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 120
($resp.Content | ConvertFrom-Json).output | ForEach-Object {
    if ($_.type -eq "message" -and $_.content) {
        $_.content | ForEach-Object { if ($_.text) { Write-Host $_.text } }
    }
}
```

## Important considerations

### Stateless requests
Each POST to `/responses` is independent. The agent has no memory of previous requests. If you need multi-turn conversations, the client must maintain conversation history and re-send it with each request.

### Package version compatibility
The `Azure.AI.AgentServer.AgentFramework` package is in beta and has strict version requirements. Using `Microsoft.Extensions.AI` 10.4.x with AgentFramework beta.9 will cause a `TypeLoadException` at runtime. Stick to the versions specified in this tutorial.

### No custom endpoints
Unlike the ACA version, you cannot add custom REST endpoints like `/api/hotels`. The agent only exposes the Responses protocol (`/responses`) and a health check (`/readiness`). If you need additional APIs, consider the [ACA-based approach](quickstart-aca.md).

## Summary

In this tutorial we created a Foundry Hosted Agent using the AgentServer framework and the Responses protocol. We reused the same hotel data and tools from the ACA tutorial but used a fundamentally different hosting model.

| Technology | Purpose |
|---|---|
| [Azure.AI.AgentServer.AgentFramework](https://www.nuget.org/packages/Azure.AI.AgentServer.AgentFramework) | Responses protocol server and agent hosting |
| [Microsoft.Agents.AI](https://www.nuget.org/packages/Microsoft.Agents.AI) | `ChatClientAgent` abstraction |
| [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions) | `IChatClient`, tool/function calling |
| [Azure.AI.OpenAI](https://www.nuget.org/packages/Azure.AI.OpenAI) | Azure OpenAI SDK |
| [DefaultAzureCredential](https://learn.microsoft.com/dotnet/azure/sdk/authentication) | Keyless authentication |

For a comparison of the two approaches (ACA vs Hosted Agent), see the [learnings document](../learnings/foundry-hosted-agent-learnings.md).

