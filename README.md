# Seattle Hotel Booking Agent

An AI-powered hotel booking agent built with C#, .NET Aspire, and Azure AI Foundry. The agent uses natural language to help travelers search for hotels, check availability, and book rooms in Seattle.

## What's Inside

- **`src/SeattleHotelAgent/`** — ACA version: .NET 10 Aspire app with custom REST API endpoints
- **`src/SeattleHotelAgent.Hosted/`** — Hosted Agent version: Foundry Agent Service using the Responses protocol
- **`tutorials/`** — Step-by-step Quick Start tutorials (ACA and Hosted Agent approaches)
- **`learnings/`** — Technical learnings and gotchas from the development process

## Quick Start

Follow one of the tutorials, or jump straight into the code:

- [ACA Quick Start](tutorials/quickstart-aca.md) — Custom REST API deployed to Azure Container Apps
- [Hosted Agent Quick Start](tutorials/quickstart-hosted-agent.md) — Foundry Hosted Agent using the Responses protocol

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- An Azure subscription with an AI Services resource and GPT-4o-mini deployment

### Run Locally

```bash
cd src/SeattleHotelAgent

# Set your Foundry endpoint (replace with your resource)
export ConnectionStrings__chat="Endpoint=https://YOUR-RESOURCE.cognitiveservices.azure.com;DeploymentId=chat"

dotnet run --project SeattleHotelAgent.Api
```

### Test

```powershell
Invoke-RestMethod -Uri "http://localhost:PORT/api/chat" -Method Post `
  -Body '{"message":"Find me a hotel near Pike Place Market, under $300/night"}' `
  -ContentType "application/json" | Select-Object -ExpandProperty reply
```

## Tech Stack

| Technology | Purpose |
|---|---|
| [.NET Aspire](https://learn.microsoft.com/dotnet/aspire) | Service orchestration, OpenTelemetry, health checks |
| [Azure AI Foundry](https://learn.microsoft.com/azure/ai-foundry) | GPT-4o-mini model inference |
| [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions) | `IChatClient` abstraction, tool/function calling |
| [Azure.AI.OpenAI](https://www.nuget.org/packages/Azure.AI.OpenAI) | Azure OpenAI SDK |
| [DefaultAzureCredential](https://learn.microsoft.com/dotnet/azure/sdk/authentication) | Keyless authentication |

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/chat` | Chat with the hotel concierge agent |
| `GET` | `/api/hotels` | List all Seattle hotels |
| `GET` | `/api/hotels/{id}` | Get details for a specific hotel |
| `GET` | `/openapi/v1.json` | OpenAPI document |

## License

See [LICENSE](LICENSE) for details.
