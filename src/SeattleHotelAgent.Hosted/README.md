# Seattle Hotel Booking Agent — Hosted Agent (Responses Protocol)

A .NET 10 Aspire app that runs a hotel booking agent using the Azure AI AgentServer framework and the OpenAI Responses protocol. The agent can search for hotels, check availability, and book rooms in Seattle using natural language.

## Architecture

- **Azure.AI.AgentServer.AgentFramework** — Responses protocol server and agent hosting
- **Microsoft.Agents.AI** — `ChatClientAgent` abstraction
- **Microsoft.Extensions.AI** — `IChatClient`, tool/function calling
- **Azure.AI.OpenAI** — Azure OpenAI SDK with `DefaultAzureCredential`
- **Azure AI Foundry** — GPT-4o-mini model inference
- **.NET Aspire** — Service orchestration, OpenTelemetry

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli)
- An Azure subscription with an AI Services resource and GPT-4o-mini deployment

## Run Locally

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE.cognitiveservices.azure.com"
$env:AZURE_OPENAI_DEPLOYMENT_NAME = "chat"

dotnet run --project SeattleHotelAgent.Hosted.Agent
```

The agent starts on `http://[::]:8088`.

## Test

```powershell
# Health check
Invoke-RestMethod -Uri "http://localhost:8088/readiness"

# Chat via Responses protocol
$body = @{ input = "Find me a hotel in Ballard for 2 guests, under $200/night" } | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "http://localhost:8088/responses" -Method Post -Body $body -ContentType "application/json" -TimeoutSec 120
($resp.Content | ConvertFrom-Json).output | ForEach-Object {
    if ($_.type -eq "message" -and $_.content) {
        $_.content | ForEach-Object { if ($_.text) { Write-Host $_.text } }
    }
}
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/responses` | Chat via OpenAI Responses protocol |
| `GET` | `/readiness` | Health check |

## How This Differs from the ACA Version

| Aspect | This (Hosted Agent) | ACA Version |
|---|---|---|
| **Protocol** | OpenAI Responses | Custom REST API |
| **Server** | AgentServer framework (port 8088) | ASP.NET Core minimal APIs |
| **Custom endpoints** | No | Yes (`/api/hotels`, `/api/chat`) |
| **Deploy target** | Foundry Agent Service | Azure Container Apps |

## Troubleshooting

**TypeLoadException: UserInputRequestContent**
→ Package version mismatch. Use `Microsoft.Extensions.AI` version **10.3.0** (not 10.4.x) with `AgentFramework` beta.9.

**Tenant mismatch error**
→ Set `AZURE_AI_TENANT_ID` to your subscription's tenant ID.

## Tutorial

See [quickstart-hosted-agent.md](../../tutorials/quickstart-hosted-agent.md) for a step-by-step guide.
