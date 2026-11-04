# Identity.Tenants STRIDE Threat Model

> `develop` 7ae5a28a65 · Families F1 lifecycle, F2 memberships/invites, F3 resolution middleware. Focus: **elevation of privilege / cross-tenant takeover** (tenancy is the platform's isolation spine). Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated.

## 1. Trust boundaries

TB1 client → API; TB2 any-tenant actor → tenant-scoped data (the module's core boundary); TB3 module → persistence; TB4 module → Notifications (invite email); TB5 module → downstream consumers (via the void — see E-class below).

## 2. F1 — lifecycle (the takeover class)

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| **E — tenant takeover**: any authenticated principal updates/deactivates/archives/deletes/purges an arbitrary tenant | CRUD/lifecycle/bulk/metadata controllers carry class-level `[Authorize]` only; handlers perform no ownership/permission check | **none** (`Controllers/TenantsController.cs:14`, `TenantLifecycleController.cs:14`, `TenantBulkOperationsController.cs:14`, `TenantMetadataController.cs:15`; `Commands/DeleteTenant/DeleteTenantCommandHandler.cs:11-19`, `Commands/UpdateTenant/UpdateTenantCommandHandler.cs`, bulk twins) | **open — critical (gap T1)**: contrast `TenantSettingsController` (`SettingsRead/Manage`) which does gate the same tenant's resources |
| E — self-service create used to mint an Owner role | intended behavior; creator becomes Owner (`Commands/CreateTenant/CreateTenantCommandHandler.cs:38-49`) | product decision | accepted (documented) |
| T — delete a tenant with active members / sessions | no member census, no session-evocation event | `ValidateForDeletion` exists on the entity (`Entities/Tenant.cs:293`) but the delete handler never calls it | **open (gap T1a)** |
| T — archive without policy consultation | `ValidateForArchive` (`:278`) not called by the archive command | **open (same class)** |
| R — slug squatting | uniqueness check on create (`CreateTenantCommandHandler.cs:24-28`) | covered (but 500 not 409 — gap T3) |
| I — audit trail absent | `TenantAuditLog` has no writers in this module (`event-flows.md` §4) | **open (gap T10)** |

## 3. F2 — memberships and invites

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| E — grant oneself a role | membership writes require `MembersManage` policy (`Controllers/UserMembershipsController.cs:35,78`) | covered |
| E — ownerless tenant via Owner deactivation | no owner-protection guard on `SetTenantMembershipStatus` | **open (gap T7)** |
| S — enumerate another user's memberships | reads gated `UsersReadSelf` (same SelfOrPermission contract as Users — ordinary users 403 until granted `users:read:self`; see `docs/identity/users/flows/preferences-and-notifications-flow.md` §2) | covered-for-holders / 403-for-others |
| T — invite forgery | invite accept is self-scoped on the invitee route; metadata is server-stamped (`Entities/TenantMemberInviteMetadata.cs:36-45`) | covered |
| I — invite email content | flows through the Notifications module's durable queue with per-channel prefs | covered |

## 4. F3 — tenant resolution middleware

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| E — header spoofing (`X-Tenant-Id` of another tenant) | resolution accepts the header first | mitigated downstream: policy rules run tenant-membership checks (TenantMatch rule family) and permission lookups are tenant-scoped — binding a *context* is not *membership* | covered at the policy layer; the middleware itself trusts the header by design (`Middleware/TenantMiddleware.cs:12,31`) |
| E — authenticated principal silently bound to default tenant | fallback to default tenant restricted to anonymous traffic (`:360-368`) | covered (explicit anti-cross-tenant guard, cited in code) |
| S/I — tenant identity leakage via host-domain resolution | domain map is public config | accepted |

## 5. Cross-cutting

| Threat | Vector | Control | Verdict |
|---|---|---|---|
| I — lifecycle invisibility | 7 events discarded + 3 never constructed (`event-flows.md` §2-§3) — no downstream audit/alert for archive/delete/member-remove | **open (gap T8)** — the member-remove case is the sharpest: removed members keep cached permissions until TTL |
| DoS — unbounded bulk bodies | bulk verbs without validators on some paths (gap T6) | partial |
| S — 500-instead-of-404 error bodies (E9) | no data leak (generic body) but breaks client retry logic | open (gap T2) |

## 6. Threat-to-gap index

Takeover (T1, T1a) → gaps T1; ownerless tenants → T7; lifecycle invisibility → T8 + G4/#1184; E9 → T2; audit absence → T10 (see `gaps.md`).
