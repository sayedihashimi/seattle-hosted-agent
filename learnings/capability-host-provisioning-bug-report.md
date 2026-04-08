# Bug Report: Foundry Capability Host Provisioning Fails with VNet Error

**Date:** 2026-04-08
**Severity:** HIGH — Blocks all Foundry Agent Service deployments via both Aspire and azd
**Status:** Unresolved — workaround exists but eliminates Aspire Foundry integration value

---

## Summary

When using either `Aspire.Hosting.Foundry`'s `AddFoundry()` or `azd ai agent init`'s generated Bicep to provision a Foundry capability host resource, the deployment consistently fails with:

```
CapabilityHostOperationFailed: The environment network configuration is invalid:
Invalid vnet resource ID provided, or the virtual network could not be found.
```

This occurs even though the Bicep correctly specifies `enablePublicHostingEnvironment: true` (which should bypass VNet requirements). The error originates from the Azure `capabilityHosts@2025-10-01-preview` API, but both Aspire and azd surface the same failure because they generate identical Bicep for this resource.

---

## Where to File the Issue

**Primary: Azure Foundry Service (the `capabilityHosts` REST API)**

The bug is in the **Azure Foundry backend service** — specifically the `Microsoft.CognitiveServices/.../capabilityHosts@2025-10-01-preview` resource provider. Both Aspire and azd are generating correct Bicep; the API itself is rejecting valid configurations.

**Evidence that this is NOT an Aspire bug:**

1. The Aspire source code (`FoundryExtensions.cs` in `microsoft/aspire`) generates a `CognitiveServicesCapabilityHost` with `CapabilityHostKind = Agents` and a `PublicHostingCognitiveServicesCapabilityHostProperties` class that sets `enablePublicHostingEnvironment: true`. This is the correct configuration per Azure documentation.

2. The `azd ai agent init` template (v0.1.20-preview) generates nearly identical Bicep in `infra/core/ai/ai-project.bicep`:
   ```bicep
   resource aiFoundryAccountCapabilityHost 'capabilityHosts@2025-10-01-preview' = {
     name: 'agents'
     properties: {
       capabilityHostKind: 'Agents'
       enablePublicHostingEnvironment: true
     }
   }
   ```

3. Both paths produce the same error from the same Azure API endpoint.

**Where to file:**
- **Azure Foundry/Cognitive Services**: File against the capability host API team. The closest public repo is likely [azure/azure-rest-api-specs](https://github.com/Azure/azure-rest-api-specs) for the API definition, or through Azure Support.
- **Secondary (azd)**: [Azure/azure-dev](https://github.com/Azure/azure-dev) — The `azd ai agent` extension should handle this failure more gracefully and provide actionable workaround guidance. There's already a related closed issue: [Azure/azure-dev#6991](https://github.com/Azure/azure-dev/issues/6991).
- **Secondary (Aspire)**: [microsoft/aspire](https://github.com/microsoft/aspire) — The `Aspire.Hosting.Foundry` integration should either document this limitation or provide a fallback path. Note that `RunAsHostedAgent()` is explicitly `throw new NotImplementedException()` in the current source.

---

## Detailed Timeline of Investigation

### Attempt 1: `azd ai agent init` → `azd provision` (2026-04-03)

**Session:** `a6d20a9b-32cd-456f-a127-4fe2214780cf`

1. Ran `azd ai agent init` with the "Agent with Tools" template
2. Set region to East US 2
3. Ran `azd provision`
4. All resources provisioned successfully **except** the capability host:
   ```
   (✓) Done: Resource group: rg-sayedha-foundry-8059
   (✓) Done: Log Analytics workspace
   (✓) Done: Application Insights
   (✓) Done: Azure AI Services Model Deployment
   (✓) Done: Foundry (AI account)
   (✓) Done: Foundry project
   (✓) Done: Container Registry
   (x) Failed: Foundry capability host: sayedha-foundry-8059-resource/agents (2m51s)

   CapabilityHostOperationFailed: The environment network configuration is invalid:
   Invalid vnet resource ID provided, or the virtual network could not be found.
   ```
5. Attempted workarounds:
   - `azd down --purge` and re-provision → same error
   - Set `ENABLE_CAPABILITY_HOST=false` to skip → provisioning succeeds but no agent hosting
   - Create capability host via Azure portal → portal auto-provisions with Microsoft-managed networking (works)

### Attempt 2: `azd provision` with fresh resource group (2026-04-04)

**Session:** `c002e7e2-85f7-4d45-9d23-463f79cc7cfd`

1. Created fresh `azd` environment, new resource group
2. Same error on capability host
3. Analyzed the Bicep template code — confirmed the template is correct:
   - `enablePublicHostingEnvironment: true` is set
   - No VNet resource ID is referenced
   - The API is rejecting it anyway
4. Tried setting `enableCapabilityHost=false` as workaround
5. Eventually got the agent deployed by creating capability host through Azure portal, then running `azd deploy`

### Attempt 3: Aspire `AddFoundry().AddDeployment()` (2026-04-07)

**Session:** `0b550466-9483-46ae-9501-4c25770c99d5`

1. In the sample-aca app, attempted to use the full Aspire Foundry integration:
   ```csharp
   var foundry = builder.AddFoundry("foundry");
   var chat = foundry.AddDeployment("chat", FoundryModel.OpenAI.Gpt4oMini);
   ```
2. In local development (`dotnet run`), Aspire blocks while trying to resolve/provision the Foundry resource
3. For `azd up` (publish mode), Aspire generates Bicep that includes the capability host — same VNet error
4. Fell back to `AddConnectionString("chat")` pattern for local dev
5. `AddConnectionString` then caused a **different** issue with `azd deploy`: it creates a `securedParameter("chat")` in the generated Bicep that conflicts with custom infrastructure templates
6. Final workaround: removed all Aspire Foundry integration, used bare `AddProject().WithExternalHttpEndpoints()`, configured AI endpoint via environment variables

### Attempt 4: Aspire `PublishAsHostedAgent()` for hosted agent (2026-04-07)

**Session:** `0b550466-9483-46ae-9501-4c25770c99d5` (turn 37+)

1. For the hosted agent app (Foundry Agent Service), attempted:
   ```csharp
   builder.AddProject<Projects.Agent>("agent")
       .PublishAsHostedAgent(project);
   ```
2. Same capability host provisioning failure
3. Also discovered that `RunAsHostedAgent()` throws `NotImplementedException` — it's not implemented in the current Aspire source
4. Fell back to bare `AddProject().WithExternalHttpEndpoints()` again

---

## Root Cause Analysis

### The Bicep is correct; the API rejects it

Both Aspire and azd generate capability host resources with the proper configuration:

```bicep
resource capabilityHost 'Microsoft.CognitiveServices/accounts/capabilityHosts@2025-10-01-preview' = {
  name: 'agents'
  parent: cognitiveServicesAccount
  properties: {
    capabilityHostKind: 'Agents'
    enablePublicHostingEnvironment: true
  }
}
```

The `enablePublicHostingEnvironment: true` property should instruct the service to use Microsoft-managed networking instead of requiring a customer VNet. However, the API returns a VNet-related error regardless.

### Portal provisioning works

When the capability host is created through the Azure Foundry portal (ai.azure.com → Project → Agents), it successfully provisions with Microsoft-managed networking. This confirms:
- The subscription/region supports capability hosts
- The Azure account has sufficient permissions
- The issue is specific to the ARM/Bicep provisioning path

### Possible causes

1. **API version bug**: The `@2025-10-01-preview` API may have a regression where `enablePublicHostingEnvironment: true` isn't properly handled
2. **Subscription type limitation**: Testing was done on MSDN (Visual Studio Ultimate) subscriptions — the API may behave differently on Enterprise subscriptions
3. **Missing prerequisite**: The API may require a resource provider registration or feature flag that isn't documented
4. **Race condition**: The capability host may need the parent AI Services account to fully propagate before creation, and Bicep's dependency chain may not enforce sufficient delay

---

## Aspire Source Code Analysis

The relevant Aspire source code is at [`microsoft/aspire/src/Aspire.Hosting.Foundry/`](https://github.com/microsoft/aspire/tree/main/src/Aspire.Hosting.Foundry).

### Key files:

| File | Role |
|---|---|
| `FoundryExtensions.cs` | `AddFoundry()` — creates the Foundry resource and default capability host |
| `FoundryResource.cs` | Resource model; defines `PublicHostingCognitiveServicesCapabilityHostProperties` |
| `HostedAgent/HostedAgentBuilderExtension.cs` | `PublishAsHostedAgent()` — orchestrates agent deployment |

### How the capability host is generated (from `FoundryExtensions.ConfigureInfrastructure`):

```csharp
var capHost = new CognitiveServicesCapabilityHost(
    NormalizeBicepIdentifier($"{resource.Name}-caphost"),
    "2025-10-01-preview")
{
    Name = "foundry-caphost",
    Parent = cogServicesAccount,
    Properties = new PublicHostingCognitiveServicesCapabilityHostProperties()
    {
        CapabilityHostKind = CapabilityHostKind.Agents
    }
};
```

Where `PublicHostingCognitiveServicesCapabilityHostProperties` extends `CognitiveServicesCapabilityHostProperties` to add `enablePublicHostingEnvironment: true`.

### Notable: `RunAsHostedAgent()` is not implemented

```csharp
public static IResourceBuilder<T> RunAsHostedAgent<T>(...)
{
    // TODO: Implement this.
    throw new NotImplementedException("RunAsHostedAgent is not yet implemented.");
}
```

This means the Aspire Foundry integration can only publish agents — it cannot run them locally through the hosted agent path.

---

## Impact on This Repository

Both apps in `src/` reference `Aspire.Hosting.Foundry` (v13.2.1-preview.1.26180.6) in their `.csproj` files but **do not use any Foundry APIs** in their AppHost code. The `AppHost.cs` files for both `sample-aca` and `sample-hosted-agent` contain only:

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.MyProject>("name")
    .WithExternalHttpEndpoints();
builder.Build().Run();
```

The package reference is effectively dead weight — kept for when the capability host bug is resolved, but providing no value currently.

---

## Workarounds

### For local development
Use environment variables or user-secrets for the AI endpoint configuration. Do not use `AddFoundry()` or `AddConnectionString()` in the AppHost.

```csharp
// AppHost.cs — minimal, no Foundry integration
var builder = DistributedApplication.CreateBuilder(args);
builder.AddProject<Projects.Api>("api")
    .WithExternalHttpEndpoints();
builder.Build().Run();
```

### For Azure deployment
1. Provision infrastructure manually or with custom Bicep (skip capability host)
2. Create the capability host through the Azure portal (ai.azure.com → Project → Agents)
3. Deploy the app with `azd deploy` (not `azd up`)
4. Configure connection info via post-deployment env var updates

---

## Recommended Issue Filing

### Issue 1: Azure Foundry capability host API (PRIMARY)

**Title:** `capabilityHosts@2025-10-01-preview` fails with VNet error when `enablePublicHostingEnvironment: true`

**Repo:** Azure Support ticket or [Azure/azure-rest-api-specs](https://github.com/Azure/azure-rest-api-specs)

**Body:**
- Provisioning a `capabilityHosts` resource via Bicep/ARM with `enablePublicHostingEnvironment: true` fails with `CapabilityHostOperationFailed: The environment network configuration is invalid`
- Portal provisioning of the same capability host succeeds
- Tested on MSDN subscription in East US 2
- Blocks all tooling-based deployments (Aspire, azd, custom Bicep)

### Issue 2: Aspire should handle capability host failure gracefully

**Title:** `AddFoundry()` should provide a fallback path when capability host provisioning fails

**Repo:** [microsoft/aspire](https://github.com/microsoft/aspire)

**Body:**
- When `AddFoundry()` generates infrastructure that includes a capability host, and that host fails to provision, there's no way to proceed
- The `AddFoundry().AddDeployment()` pattern blocks local development while resolving Azure resources
- `RunAsHostedAgent()` is not implemented (`NotImplementedException`)
- Suggestion: Allow `AddFoundry()` to work without a capability host for scenarios that only need model deployments (not hosted agents)

### Issue 3: `azd ai agent` extension should provide better error handling

**Title:** `azd ai agent init` → `azd provision` fails on capability host with no actionable workaround

**Repo:** [Azure/azure-dev](https://github.com/Azure/azure-dev)

**Body:**
- Related to [#6991](https://github.com/Azure/azure-dev/issues/6991) (VNet-related `azd ai agent init` failure)
- The error message provides no guidance on resolution
- Suggestion: Detect the failure and offer to skip the capability host, then guide the user to create it via the portal

---

## Environment Details

| Component | Version |
|---|---|
| `Aspire.Hosting.Foundry` | 13.2.1-preview.1.26180.6 |
| `azd` | 1.23.7+ |
| `azd ai agent` extension | v0.1.20-preview |
| .NET SDK | 10.0 |
| Azure subscription | MSDN (Visual Studio Ultimate) |
| Region | East US 2 |
| Capability host API | `@2025-10-01-preview` |

---

## References

- Aspire Foundry source: https://github.com/microsoft/aspire/tree/main/src/Aspire.Hosting.Foundry
- Aspire Foundry docs: https://aspire.dev/integrations/cloud/azure/azure-ai-foundry/azure-ai-foundry-host/
- Capability host concepts: https://learn.microsoft.com/en-us/azure/foundry/agents/concepts/capability-hosts
- Related azd issue: https://github.com/Azure/azure-dev/issues/6991
- Working sample (uses portal-created capability host): https://github.com/josemzr/foundry-hosted-agents
