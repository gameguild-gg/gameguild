# Identity.Provisioning — Module Architecture

> `develop` 7ae5a28a65 · Anatomy, composition bindings, protocol posture. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated.

## 1. Anatomy

```
Controllers/        ScimUsersController ScimGroupsController ScimDiscoveryControllers
                    ScimProvisioningTokensAdminController ScimExceptionFilter
Authentication/     ScimProvisioningAuthenticationHandler - custom scheme - hash lookup + claims
Middleware/         ScimFeatureGateMiddleware - 404s /scim when disabled - FIRST middleware in host pipeline
Scim/               Models Filtering (parser + evaluator + attribute whitelist) Patch (user and group appliers)
                    ScimConstants ScimDiscoveryDocuments ScimResourceMappers ScimListResponse ScimException
Services/           ScimUserService ScimGroupService ScimBulkProcessor
Commands/           SCIM verbs + token admin (12 contracts total)
Entities/           ScimProvisioningToken ScimUserMapping ScimGroupMapping (+ configurations)
Auditing/           IScimProvisioningAuditSink + Null implementation + helper
Abstractions/       repositories + context ports
Data/Configurations/
ScimRequestContext.cs ScimScopes.cs ScimProvisioningOptions.cs ProvisioningModuleExtensions.cs
```

## 2. Composition-root bindings (what makes the module tick)

| Binding | Where | Note |
|---|---|---|
| repositories + services + `IScimProvisioningContext` (`HttpScimProvisioningContext`) | `ProvisioningModuleExtensions.cs:41-52` | platform-pure registrations |
| `IScimProvisioningAuditSink` | `:57` `TryAddScoped(Null…)` — **overridden** by `GameGuild.Compliance.Audit/Extensions/AuditModule.cs:137` → `CentralScimProvisioningAuditEventSink` | the Null default is the audit-loss trap (gap P1) |
| feature gate | `UseScimProvisioningFeatureGate()` — host pipeline position 2, before forwarded headers/routing/auth (`GameGuild.API/Core/Setup/PipelineExtensions.cs:43`) | disabled ⇒ 404 pre-auth |
| authentication scheme | registered with the auth stack; selected by `[Authorize(AuthenticationSchemes = ScimProvisioningAuthenticationOptions.SchemeName)]` on every SCIM controller | coexists with JWT/bearer schemes |

## 3. Protocol posture (RFC 7643/7644)

- Filtering per §3.4.2 with an attribute whitelist (`Scim/Filtering/ScimFilterableAttributes.cs`); PATCH per §3.5.2 with typed appliers; error bodies per §3.12 via `ScimExceptionFilter`; bulk per §3.7 with `failOnErrors`; delete-as-deactivate per §3.5.3.
- Deviation (deliberate): discovery endpoints require authentication where the RFC permits anonymous (`Controllers/ScimDiscoveryControllers.cs:16`) — interop note P2.
- Contract tests pin the wire shapes: `GameGuild.Identity.Provisioning.UnitTests/ScimProtocolContractTests.cs` (+ Filtering/Patch suites, auth-handler tests, token tests, feature-gate tests, `ProvisioningModuleOptionsWiringTests` — the options-binding guard that keeps `ScimProvisioningOptions` out of the G5 dead-options class).

## 4. Downstream effects

SCIM writes reach two other modules: `Identity.Users` (user records via mappings) and tenant roles (group membership). The module itself stays side-effect-pure apart from its audit stream — no in-process notifications, no domain events, only durable operation events. This is the counter-example architecture to Users/Tenants' orphan-producer pattern: when there is nothing to say, say nothing.

## 5. Design decisions worth keeping

1. **Plaintext-once token creation with hash-only persistence** — the only workable bearer-token shape; rotation grace makes revocation non-disruptive (`Entities/ScimProvisioningToken.cs`).
2. **Lazy rotation finalization** — the expired-grace token's revocation is persisted by the very request that trips over it; no background sweeper needed.
3. **Feature gate before authentication** — a disabled SCIM surface is indistinguishable from absent (404, no scheme hints).
4. **Scope checks in module code, not policies** — `ScimScopes` throws SCIM-shaped 403s that the shared policy layer could not express.
