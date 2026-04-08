# Comparison: Three Foundry Agent Approaches

This document compares three different approaches to building AI agents with Azure AI Foundry and .NET, based on the apps built during this project.

| App | Description | Location |
|---|---|---|
| **Hotel Agent (ACA)** | Single conversational agent deployed to Azure Container Apps | `src/sample-aca/` |
| **Hotel Agent (Hosted)** | Same agent using Foundry Agent Service + Responses protocol | `src/sample-hosted-agent/` |
| **ClaimsAgent** | Multi-agent workflow with persistent Foundry audit agent | [github.com/JeremyLikness/foundrytest](https://github.com/JeremyLikness/foundrytest) |

## Feature Comparison

| Aspect | Hotel Agent (ACA) | Hotel Agent (Hosted) | ClaimsAgent |
|---|---|---|---|
| **Number of agents** | 1 | 1 | 5 (triage + 3 specialists + audit) |
| **Orchestration** | Direct `IChatClient` call | `ChatClientAgent` + `RunAIAgentAsync()` | MAF handoff workflow (`AgentWorkflowBuilder`) |
| **Protocol** | Custom REST API | OpenAI Responses protocol | Custom REST API |
| **Endpoints** | `/api/chat`, `/api/hotels`, `/api/hotels/{id}` | `/responses`, `/readiness` | `/api/claims/submit`, `/api/claims/stream`, etc. |
| **OpenAPI** | Yes (`/openapi/v1.json`) | No | No |
| **Streaming** | No | No | Yes (SSE via `/api/claims/stream`) |
| **Custom endpoints** | Yes — full control | No — protocol-constrained | Yes — full control |
| **Stateful sessions** | No (stateless per request) | No (stateless per request) | Yes — Foundry persistent agent with server-side memory |
| **RAG / file search** | No | No | Yes — Foundry vector store + file search |
| **Multi-agent handoff** | No | No | Yes — MAF `transfer_to_*` tools |

## Technology Comparison

| Technology | Hotel Agent (ACA) | Hotel Agent (Hosted) | ClaimsAgent |
|---|---|---|---|
| **AI client** | `AzureOpenAIClient` → `IChatClient` | `AzureOpenAIClient` → `IChatClient` → `ChatClientAgent` | `AzureOpenAIClient` → `IChatClient` + `AIProjectClient` |
| **Tool registration** | `AIFunctionFactory` → `ChatOptions.Tools` | `AIFunctionFactory` → `ChatClientAgent` constructor | `AIFunctionFactory` → `ChatClientAgent` constructor |
| **Agent framework** | None (MEAI only) | MAF `ChatClientAgent` + AgentServer SDK | MAF `ChatClientAgent` + Workflows + Foundry Agents |
| **Aspire hosting** | `AddProject().WithExternalHttpEndpoints()` | `AddProject().WithExternalHttpEndpoints()` | `AddProject().WithExternalHttpEndpoints()` |
| **Project type** | ASP.NET Core Web API | Console app (AgentServer handles HTTP) | ASP.NET Core Web API |
| **HTTP server** | Kestrel via `WebApplication` | Kestrel via `RunAIAgentAsync()` (port 8088) | Kestrel via `WebApplication` |
| **MEAI version** | 10.4.x (latest) | 10.3.0 (locked to AgentFramework compat) | 10.4.x |

## Package Comparison

| Package | Hotel Agent (ACA) | Hotel Agent (Hosted) | ClaimsAgent |
|---|---|---|---|
| `Microsoft.Extensions.AI` | ✅ 10.4.x | ✅ 10.3.0 | ✅ 10.4.x |
| `Microsoft.Extensions.AI.OpenAI` | ✅ 10.4.x | ✅ 10.3.0 | ✅ 10.4.x |
| `Azure.AI.OpenAI` | ✅ | ✅ | ✅ |
| `Azure.Identity` | ✅ | ✅ | ✅ |
| `Microsoft.Agents.AI` | ❌ | ✅ (rc1) | ✅ (1.0.0) |
| `Microsoft.Agents.AI.OpenAI` | ❌ | ✅ (rc1) | ✅ (1.0.0) |
| `Microsoft.Agents.AI.Workflows` | ❌ | ❌ | ✅ (1.0.0) |
| `Microsoft.Agents.AI.Foundry` | ❌ | ❌ | ✅ (1.0.0) |
| `Azure.AI.AgentServer.AgentFramework` | ❌ | ✅ (beta.9) | ❌ |
| `Azure.AI.Projects` | ❌ | ❌ | ✅ |
| `Azure.AI.Projects.Agents` | ❌ | ❌ | ✅ |
| `Aspire.Azure.AI.Inference` | ✅ | ❌ | ❌ |

## Deployment Comparison

| Aspect | Hotel Agent (ACA) | Hotel Agent (Hosted) | ClaimsAgent |
|---|---|---|---|
| **Deploy target** | Azure Container Apps | Foundry Agent Service | None (local only) |
| **Deploy command** | `azd up` | `azd deploy` (via `azure.ai.agents` ext) | N/A |
| **Deploy status** | ✅ Working | ❌ Blocked (capability host failure) | N/A (no deploy infra) |
| **Could deploy to ACA?** | ✅ (already does) | ✅ (would need custom endpoints) | ✅ (needs azure.yaml + Bicep) |
| **Infra provisioning** | Custom Bicep (AI Services + ACA) | `azd ai agent` Bicep (AI Services + Project + ACR) | ARM template (`foundry.json`) |
| **Docker required** | Yes (for `azd up`) | Yes (for container build) | No |
| **Config method** | Connection string env var | `AZURE_OPENAI_ENDPOINT` env var | `appsettings.json` or `AZURE_AI_PROJECT_ENDPOINT` |

## Configuration Comparison

| Aspect | Hotel Agent (ACA) | Hotel Agent (Hosted) | ClaimsAgent |
|---|---|---|---|
| **Endpoint type** | Base endpoint (`https://xxx.cognitiveservices.azure.com`) | Base endpoint | Project endpoint (`https://xxx.services.ai.azure.com/api/projects/name`) |
| **Auth** | `DefaultAzureCredential` with optional TenantId | `DefaultAzureCredential` with optional TenantId | `DefaultAzureCredential` |
| **Config source** | `ConnectionStrings:chat` or `AzureAI:Endpoint` | `AZURE_OPENAI_ENDPOINT` env var | `AzureAI:ProjectEndpoint` in appsettings |

## When to Use Each Approach

### Hotel Agent (ACA) — Custom REST API on Container Apps
**Best for:** Apps that need custom endpoints, OpenAPI docs, and full control over the HTTP API. Good for web frontends or APIs consumed by other services.

### Hotel Agent (Hosted) — Foundry Agent Service
**Best for:** Agents that should be callable via the standard OpenAI Responses protocol. Good for integration with Foundry playground, other agents, and OpenAI-compatible clients. Currently blocked by capability host provisioning issues.

### ClaimsAgent — Multi-Agent Workflow
**Best for:** Complex domain-specific workflows with multiple specialized agents, persistent memory, and RAG. Good for enterprise scenarios where different agents handle different aspects of a process.

## Key Issues Discovered

### 1. AgentFramework Package Version Incompatibility (HIGH)
`Azure.AI.AgentServer.AgentFramework` beta.9/beta.11 are tightly coupled to specific MEAI versions. Using MEAI 10.4.x causes `TypeLoadException` at runtime. Only MEAI 10.3.0 works with beta.9.

### 2. Foundry Capability Host Provisioning Failure (HIGH)
The capability host (`capabilityHosts@2025-10-01-preview`) fails to provision on MSDN subscriptions with VNet errors, even with `enablePublicHostingEnvironment: true`. This blocks all Foundry Agent Service deployments.

### 3. Aspire `AddConnectionString` + azd Deploy Conflict (MEDIUM)
Aspire's `AddConnectionString()` creates a `securedParameter` dependency in azd deploy templates that doesn't work with custom Bicep. Workaround: skip `AddConnectionString` and configure env vars post-deploy.

### 4. DefaultAzureCredential Tenant Mismatch (MEDIUM)
`DefaultAzureCredential` picks the wrong tenant in multi-tenant environments (e.g., Microsoft corp tenant vs MSDN tenant). Must specify `TenantId` explicitly.

### 5. AI Services Custom Domain Required (LOW)
Without `--custom-domain` on `az cognitiveservices account create`, the endpoint returns a regional URL that `AzureOpenAIClient` can't use. Must set a custom domain to get a `xxx.cognitiveservices.azure.com` endpoint.
