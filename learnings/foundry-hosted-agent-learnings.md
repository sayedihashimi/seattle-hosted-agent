# Learnings: Foundry Hosted Agent (AgentServer / Responses Protocol)

These learnings were captured while building the SeattleHotelAgent.Hosted project —
the Foundry Agent Service version of the hotel booking agent.

## 1. Package Version Compatibility is Critical (BLOCKER)

### The Problem
`Azure.AI.AgentServer.AgentFramework` is tightly coupled to specific versions of `Microsoft.Extensions.AI` and `Microsoft.Agents.AI`. Mixing versions causes runtime `TypeLoadException` errors.

### Errors Encountered
- **beta.11 + MEAI 10.4.x**: `Could not load type 'Microsoft.Extensions.AI.UserInputRequestContent'` — this type does NOT exist in any public release of MEAI (10.3 or 10.4). The AgentFramework beta.11 appears to have been compiled against an internal/unreleased version.
- **beta.6 + MEAI 10.4.x**: `Could not load type 'Microsoft.Agents.AI.AgentRunResponse'` — compiled against a different version of Microsoft.Agents.AI.Abstractions.
- **beta.9 + MEAI 10.3.0**: ✅ Works! This is the combination used by the reference `AgentWithTools` project.

### Working Version Combination
```xml
<PackageReference Include="Azure.AI.AgentServer.AgentFramework" Version="1.0.0-beta.9" />
<PackageReference Include="Microsoft.Agents.AI.OpenAI" Version="1.0.0-rc1" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.5.0-beta.1" />
<PackageReference Include="Azure.Identity" Version="1.17.0" />
<PackageReference Include="Microsoft.Extensions.AI" Version="10.3.0" />
<PackageReference Include="Microsoft.Extensions.AI.OpenAI" Version="10.3.0" />
```

### Recommendation
The AgentFramework package should declare strict dependency version constraints in its nuspec. Currently, using the latest MEAI (10.4.x) with any AgentFramework version causes runtime failures that are impossible to diagnose without trial-and-error.

**Severity: HIGH** — This will block every developer who tries to use the latest packages together.

## 2. Responses Protocol is Stateless

Each POST to `/responses` is an independent request. There is no server-side conversation state. If a user asks "confirm the booking" in a follow-up request, the agent has no memory of the previous search/booking context.

### Implication for Hotel Booking Agent
Multi-turn conversations (search → check availability → book) require the client to maintain conversation history and re-send it with each request, or use the `previous_response_id` field if supported.

### Comparison with ACA Version
The ACA version (`/api/chat`) has the same limitation (stateless per request), but since we control the endpoints, we could add session/conversation management. With the Responses protocol, we're constrained by the protocol spec.

## 3. AgentFramework Handles HTTP Server Setup

Unlike the ACA version where we use ASP.NET Core minimal APIs:
- `RunAIAgentAsync()` starts a Kestrel HTTP server on port 8088 automatically
- Exposes `/responses` (OpenAI Responses protocol) and `/readiness` (health check)
- No need for `WebApplication.CreateBuilder()`, `MapGet()`, etc.
- The agent is a console app, not a web app

### Default Port
The AgentServer listens on `http://[::]:8088` by default.

## 4. Tool Registration Pattern Differs

### ACA Version (MEAI)
```csharp
builder.Services.AddSingleton<IList<AITool>>([
    AIFunctionFactory.Create(HotelTools.SearchHotels),
    ...
]);
// Tools passed via ChatOptions in each request
```

### Hosted Version (MAF + AgentFramework)
```csharp
var tools = new AIFunction[] {
    AIFunctionFactory.Create(HotelTools.SearchHotels),
    ...
};
var agent = new ChatClientAgent(chatClient, name: "...", instructions: "...", tools: tools);
```

Tools are registered directly on the agent constructor. The `ChatClientAgent` handles tool invocation as part of the Responses protocol flow.

## 5. dotnet new Template Naming Issue

The `dotnet new aspire` template with `-n SeattleHotelAgent.Hosted` creates nested folder structure. Using `-o .` after creating the folder manually fixes this. Tutorial instructions must be precise about the order:

```bash
mkdir SeattleHotelAgent.Hosted
cd SeattleHotelAgent.Hosted
dotnet new aspire -n SeattleHotelAgent.Hosted -o .
```

## 6. Aspire Integration with Hosted Agents

The Aspire `PublishAsHostedAgent()` API exists for deploying to Foundry Agent Service:
```csharp
builder.AddProject<Projects.Agent>("agent")
    .PublishAsHostedAgent(project);
```

However, this requires `foundry.AddProject()` which creates a "capability host" — and that failed during `azd up` with a VNet configuration error. This is the same issue we hit with the ACA version when trying to use `AddFoundry().AddDeployment()`.

### Recommendation
The Aspire Foundry hosting integration's `AddProject()` / capability host provisioning needs better error messages and documentation about VNet requirements. For now, manual deployment with `azd ai agent deploy` is more reliable.

## 7. Comparison: ACA vs Foundry Hosted Agent

| Aspect | ACA (SeattleHotelAgent) | Hosted (SeattleHotelAgent.Hosted) |
|---|---|---|
| **Protocol** | Custom REST endpoints | OpenAI Responses protocol |
| **Server setup** | ASP.NET Core minimal APIs | AgentFramework `RunAIAgentAsync()` |
| **Port** | Dynamic (from launch settings) | 8088 (AgentServer default) |
| **Endpoints** | `/api/chat`, `/api/hotels`, `/api/hotels/{id}` | `/responses`, `/readiness` |
| **Additional APIs** | Hotel listing, hotel details | None (agent-only) |
| **OpenAPI** | Yes (`/openapi/v1.json`) | No |
| **State management** | Could add session support | Stateless per request (protocol constraint) |
| **Deploy target** | Azure Container Apps | Foundry Agent Service (ACI) |
| **Deploy command** | `azd up` | `azd ai agent deploy` |
| **MEAI version** | 10.4.x (latest) | 10.3.0 (locked to AgentFramework compat) |
| **Package stability** | Stable/GA packages | Beta/RC packages with version conflicts |
| **Local dev** | Works with Aspire dashboard | Works standalone on port 8088 |
