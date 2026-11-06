# Identity.Provisioning CQRS Wiring Matrix

> `develop` 7ae5a28a65 · Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/`. All 12 command contracts handled and routed — the cleanest wiring of the identity packages documented in this campaign.

## 1. Matrix

| Route | Request | Handler | Auth | Notes |
|---|---|---|---|---|
| GET `/scim/v2/Users` (+filter) | — (service path) | `Services/ScimUserService.cs` | token scheme + `scim:read` | |
| POST `/scim/v2/Users` | `ScimCreateUserCommand` | `Commands/ScimProvisioningTokenCommands.cs` sibling handlers (SCIM commands in `Commands/` + services) | token scheme + `scim:write` | creates/maps `Identity.Users` record via `ScimUserMapping` |
| GET/PUT/PATCH/DELETE `/scim/v2/Users/{userId}` | `ScimReplaceUserCommand` / `ScimPatchUserCommand` / `ScimDeleteUserCommand` | same | token scheme + scope per verb | RFC 7644 semantics |
| GET/POST `/scim/v2/Groups`; GET/PUT/PATCH/DELETE `/scim/v2/Groups/{roleId}` | `ScimCreateGroupCommand` / `ScimReplaceGroupCommand` / `ScimPatchGroupCommand` / `ScimDeleteGroupCommand` | `Services/ScimGroupService.cs` | token scheme + scope | Groups = tenant roles |
| GET `/scim/v2/ServiceProviderConfig`, `/Schemas[/{id}]`, `/ResourceTypes` | — | `Scim/ScimDiscoveryDocuments.cs` | token scheme | RFC-permitted anon discovery deliberately gated |
| POST `/scim/v2/Bulk` | `ScimBulkCommand` | `Services/ScimBulkProcessor.cs` | token scheme + scopes per inner op | `failOnErrors` envelope |
| POST `v{v}/auth/scim-provisioning-tokens` | `CreateScimProvisioningTokenCommand` | `Commands/ScimProvisioningTokenCommands.cs:121+` | **`Policies.ProvisioningManageTokens`** (`Controllers/ScimProvisioningTokensAdminController.cs:22`) | plaintext once |
| GET `v{v}/auth/scim-provisioning-tokens` | list query | same | `ProvisioningManageTokens` (`:35`) | |
| POST `…/{tokenId}:rotate` | `RotateScimProvisioningTokenCommand` | same (`:205-206` grace) | `ProvisioningManageTokens` (`:46`) | |
| POST `…/{tokenId}:revoke` | `RevokeScimProvisioningTokenCommand` | same | `ProvisioningManageTokens` (`:64`) | |

Contract registry: 12 `UseCaseEventContracts` (`Architecture/UseCaseEventContracts.g.cs`) — 8 SCIM + 3 token + 1 bulk; each names its `identity.provisioning.*` operation event; no `ExpectedEventTypes` (no named domain events by design — the durable generic operation event is the contract).

## 2. HOLE register

| # | Hole | Evidence | Severity |
|---|---|---|---|
| P1 | **Audit sink defaults to Null** — `TryAddScoped<IScimProvisioningAuditSink>(NullScimProvisioningAuditSink.Instance)`; real sink bound only when the Audit module composes | `ProvisioningModuleExtensions.cs:57` vs `GameGuild.Compliance.Audit/Extensions/AuditModule.cs:137` | medium (silent audit loss if composition drifts) |
| P2 | Discovery endpoints require auth (RFC 7644 §4 allows anonymous ServiceProviderConfig discovery) — interop friction for IdPs that probe before configuring tokens | `Controllers/ScimDiscoveryControllers.cs:16,34,62,78` | low (deliberate posture; document as decision) |
| P3 | No pagination-limit guard documented for `count` on list endpoints beyond processor defaults | `Services/ScimUserService.cs` list path | low |
| P4 | Token list endpoint exposure of metadata (name, created-by, prefixes) to any `ProvisioningManageTokens` holder is fine, but no rate-limit policy is attached to the admin controller (contrast `PolicyBundlesController` in Authorization) | absence of `[EnableRateLimiting]` on `ScimProvisioningTokensAdminController` | low |
