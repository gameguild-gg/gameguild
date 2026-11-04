# SCIM Resource Operations Flow — Users, Groups, discovery, bulk

> Families F2-F4 of `GameGuild.Identity.Provisioning` · `develop` 7ae5a28a65. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated. All `/scim/v2/*` routes authorize exclusively with the provisioning-token scheme (`Controllers/ScimUsersController.cs:16`).

## 1. SCIM Users (F2) — `/scim/v2/Users`

| Operation | Route | Command | Semantics |
|---|---|---|---|
| list + filter | GET `Users` | — (service + `Scim/Filtering/`) | RFC 7644 §3.4.2 filter expressions via `ScimFilterParser`/`ScimFilterEvaluator` |
| create | POST `Users` | `ScimCreateUserCommand` | requires `userName`/`displayName`/email — `ScimException.InvalidValue` otherwise (`Services/ScimUserService.cs:109-116`); duplicate `userName`/email → `ScimException.Uniqueness` (`:295`) |
| get | GET `Users/{userId}` | — | missing → `ScimException.NotFound` (`:40`) |
| replace | PUT `Users/{userId}` | `ScimReplaceUserCommand` | full-resource semantics |
| patch | PATCH `Users/{userId}` | `ScimPatchUserCommand` | RFC 7644 §3.5.2 via `Scim/UserPatchApplier` — empty op list → `BadPayload` (`:200`); `displayName` emptied → `InvalidValue` (`:422`) |
| delete (deactivate per RFC) | DELETE `Users/{userId}` | `ScimDeleteUserCommand` | RFC 7644 §3.5.3 delete-as-deactivate |

Errors convert to RFC 7644 §3.12 bodies by `ScimExceptionFilter` (`Controllers/ScimExceptionFilter.cs:11-23`).

## 2. SCIM Groups (F3) — `/scim/v2/Groups`

Groups are **tenant roles**: mapping `roleId` ↔ SCIM Group id (`Scim/ScimResourceMappers.cs:92-108`, location header `{GroupsPath}/{roleId}`). CRUD + patch mirror the Users set with `ScimCreateGroupCommand`, `ScimReplaceGroupCommand`, `ScimPatchGroupCommand` (`Scim/ScimGroupPatchApplier.cs`), `ScimDeleteGroupCommand` via `Services/ScimGroupService.cs`. Members operations in patch mutate role assignments — see STRIDE §4 for the elevation reading.

## 3. Discovery (F4a)

| Route | Handler |
|---|---|
| GET `ServiceProviderConfig` | `Scim/ScimDiscoveryDocuments.cs` (auth required — nonstandard-safe: RFC allows anon discovery, this deployment gates it) |
| GET `Schemas` / `Schemas/{schemaId}` | same |
| GET `ResourceTypes` | same |

## 4. Bulk (F4b) — POST `/scim/v2/Bulk`

`ScimBulkCommand` → `Services/ScimBulkProcessor.cs`: iterates operations, honors `failOnErrors` (`:41-54` — stop once error count reaches the threshold), returns a per-operation status envelope (`:228` bulkIds mapping). Partial failure is first-class, not transactional.

## 5. Scope enforcement

Every operation checks the token's scopes (`ScimScopes.cs:21,29`): reads need `scim:read`, writes need `scim:write`; any-of semantics mirroring API-key scopes (`:5-7`). Scope failures are SCIM 403 bodies.

## 6. Edge matrix (F2-F4)

| Edge | Guard | Result | Evidence |
|---|---|---|---|
| filter on unfilterable attribute | `ScimFilterableAttributes` whitelist | 400 invalid filter | `Scim/Filtering/ScimFilterableAttributes.cs` |
| patch with empty operations | `BadPayload` | 400 | `Services/ScimUserService.cs:200` |
| cross-tenant resource id | mapping lookups are tenant-scoped by the request context | 404 | `Scim/ScimResourceMappers.cs` + context `ScimRequestContext.cs:18` |
| bulk exceeding `failOnErrors` | counter stops the loop | partial envelope with stopped status | `Services/ScimBulkProcessor.cs:41-54` |
| write without `scim:write` | scope check | 403 SCIM body | `ScimScopes.cs:29` |
| discovery while feature disabled | gate middleware 404s all `/scim` before auth | 404 | `Middleware/ScimFeatureGateMiddleware.cs:19-27` |
