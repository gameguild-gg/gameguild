# GameGuild.Identity.Provisioning — Documentation Package

> The SCIM 2.0 (RFC 7643/7644) inbound provisioning module: tenant-scoped bearer tokens, SCIM Users → `Identity.Users` records, SCIM Groups → tenant roles, discovery + bulk · Documented against `develop` commit `7ae5a28a65`.
> Conventions follow the campaign package shape (see `docs/authorization/README.md`).

## Scope

`GameGuild.Identity.Provisioning` — 4 SCIM controllers (`ScimUsersController`, `ScimGroupsController`, `ScimDiscoveryControllers` (ServiceProviderConfig/Schemas/ResourceTypes/Bulk), `ScimProvisioningTokensAdminController`) + `ScimExceptionFilter`; 22 SCIM endpoints + 4 token-admin endpoints; 12 command contracts (`Architecture/UseCaseEventContracts.g.cs`), all handled; token authentication handler; feature-gate middleware.

## Contents

| Document | What it covers |
|---|---|
| [`flows/token-lifecycle-flow.md`](flows/token-lifecycle-flow.md) | Token create (SHA-256-at-rest, plaintext shown once) → rotate with overlap grace → revoke; per-request token authentication |
| [`flows/scim-resource-operations-flow.md`](flows/scim-resource-operations-flow.md) | SCIM Users/Groups CRUD + RFC 7644 PATCH + filtering, discovery, bulk with `failOnErrors` |
| [`cqrs-wiring-matrix.md`](cqrs-wiring-matrix.md) | Every endpoint × command × handler × auth scheme/policy × scopes |
| [`state-machines.md`](state-machines.md) | Token lifecycle (active/grace/revoked/expired), user-mapping states |
| [`event-flows.md`](event-flows.md) | SCIM audit events → Compliance.Audit sink; operation events; no domain notifications |
| [`sequences-lite.md`](sequences-lite.md) | IdP → gate → auth → controller; token rotation overlap; audit fan-out |
| [`07-stride-threat-model.md`](07-stride-threat-model.md) | STRIDE — bearer-token compromise focus, scope enforcement, tenant isolation |
| [`08-dfds.md`](08-dfds.md) | Level-0 + level-1 per family |
| [`09-architecture.md`](09-architecture.md) | Module anatomy, the Null-audit-sink default, composition-root bindings |
| [`gaps.md`](gaps.md) | Consolidated gap register with file:line |

## Headline findings

1. **The healthiest wiring of the three identity packages audited here** — every declared command has a handler and a route; no dead request types; dedicated unit + protocol-contract test trees (`GameGuild.Identity.Provisioning.UnitTests/`: `ScimProtocolContractTests`, `ScimProvisioningAuthenticationHandlerTests`, `ScimProvisioningTokenTests`, `ScimFeatureGateMiddlewareTests`, Filtering/Patch suites).
2. **Token design is sound**: 256-bit random body (`Entities/ScimProvisioningToken.cs:105-121`), SHA-256 hash at rest with unique index (`:15,51`), plaintext returned exactly once at create; rotation uses an **overlap grace window** (`BeginRotationGrace`, `:188-191`) with lazy fail-closed finalization once grace expires (`:142-158,195-206`); every authentication (success AND failure) is audited through the sink (`Authentication/ScimProvisioningAuthenticationHandler.cs:85,102-110`).
3. **Audit sink defaults to Null** — the module registers `NullScimProvisioningAuditSink` via `TryAddScoped` (`ProvisioningModuleExtensions.cs:57`); the real sink (`CentralScimProvisioningAuditEventSink`) is bound by the Compliance.Audit module (`GameGuild.Compliance.Audit/Extensions/AuditModule.cs:137`). If the Audit module is ever not composed, SCIM audit silently vanishes — the TryAdd default hides the misconfiguration.
4. **Groups are tenant ROLES** — SCIM Groups map onto `roleId` resources (`Scim/ScimResourceMappers.cs:92-108`): an IdP with a compromised write-scoped token can reshape tenant authorization, not just membership. Scope checks are enforced per request (`ScimScopes.cs:21,29` — `scim:read`/`scim:write`).
5. **Feature gate is the first middleware in the host pipeline** (`GameGuild.API/Core/Setup/PipelineExtensions.cs:43`): with `ScimProvisioning:Enabled=false` every `/scim` path answers 404 before authentication (`Middleware/ScimFeatureGateMiddleware.cs:19-27`) — unauthenticated probing sees nothing.

## How to read the diagrams

Mermaid `flowchart TD` / `stateDiagram-v2`, no parentheses or colons in labels. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated otherwise.
