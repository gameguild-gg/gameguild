# Identity.Provisioning Event Flows

> `develop` 7ae5a28a65 · Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Provisioning/` unless stated.

## 1. SCIM provisioning audit events → Compliance.Audit (the module's primary stream)

| Stream | Producer | Consumer | Durability |
|---|---|---|---|
| token-auth success/failure audit (`ScimProvisioningAuditEvent`) | `Authentication/ScimProvisioningAuthenticationHandler.cs:85,102-110` (`RecordAuditAsync` on every outcome) | `IScimProvisioningAuditSink` → **Null** by default (`ProvisioningModuleExtensions.cs:57`) → `CentralScimProvisioningAuditEventSink` when Audit module composes (`GameGuild.Compliance.Audit/Extensions/AuditModule.cs:137`) | in-transaction when the real sink is bound; **silently dropped** otherwise (gap P1) |
| operation audit for SCIM resource writes | `Auditing/ScimProvisioningAudit.cs:60-73` (`RecordAsync` helper wrapping the sink) | same sink | same caveat |

## 2. Durable operation events

Every command emits the generic durable operation event via the use-case operation context (`Architecture/UseCaseEventContracts.g.cs` — 12 contracts, all `identity.provisioning.*`, no named domain events by design). These land in `OutboxMessage` with the SCIM actor stamped (`ScimProvisioningActor.Subject = "scim_token:{id}"`, `ScimRequestContext.cs:18-20`) — so the audit trail can attribute IdP-driven mutations to the exact token.

## 3. What does NOT flow

- No domain notifications — the module defines zero `INotificationHandler` targets and publishes zero in-process notifications (no orphan producers, no dead types — the counter-example to Users/Tenants).
- No `UserCreatedEvent` on SCIM user create: the mapped `Identity.Users` record creation path is the Users module's contract territory; a SCIM-created user produces the operation event here, and `UserCreatedEvent` only if the create path routes through `CreateUserCommand` — worth pinning in a test (see `gaps.md` P5).
