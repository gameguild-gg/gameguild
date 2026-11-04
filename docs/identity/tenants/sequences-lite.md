# Identity.Tenants — Sequences (lite)

> `develop` 7ae5a28a65 · Cross-module sequences only. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated.

## 1. Request → tenant resolution → authorization (the per-request frame)

```mermaid
sequenceDiagram
    participant C as Client with or without tenant hint
    participant TM as TenantMiddleware
    participant R as Resolution ladder
    participant AC as ActorContext
    participant AZ as Authorization policies

    C->>TM: request - PipelineExtensions.cs 109
    TM->>R: header then domain then query then route then claim - TenantMiddleware.cs 12
    alt resolved
        R-->>TM: tenant bound to HttpContext
    else anonymous and unresolved
        R->>R: platform default tenant - GetDefaultTenantQuery - TenantMiddleware.cs 368
    else authenticated and unresolved
        R-->>TM: NO fallback - anti cross-tenant guard - TenantMiddleware.cs 360-367
    end
    TM->>AC: immutable actor capture - PipelineExtensions.cs 114
    AC->>AZ: policy evaluation with tenant context
```

Verdict: **fail-closed for authenticated cross-tenant ambiguity** (no default-tenant binding); anonymous traffic is intentionally default-tenant scoped.

## 2. Invite delivery (the module's one durable outbound)

```mermaid
sequenceDiagram
    participant A as Admin with MembersManage
    participant AM as AddTenantMemberCommandHandler
    participant M as TenantMember
    participant OB as OutboxMessage
    participant OD as OutboxDispatcher
    participant N as Notifications TenantInviteRequestedHandler

    A->>AM: POST users id memberships with invitee email
    AM->>M: create member Pending + IsActive false when RequiresAcceptance
    AM->>M: AddIntegrationEvent TenantMemberInviteRequestedV1 - handler 125-133
    M->>OB: captured transactionally
    OB-->>OD: relay
    OD->>N: invite email via durable queue - NotificationsModule.cs 92
    Note over AM: AddDomainEvent TenantMemberAddedEvent raised alongside is discarded - see event-flows section 2
```

Verdict: invite email **durable**; the member-added side-signal **lost**.

## 3. Tenant deletion vs members and sessions (absent coordination)

```mermaid
sequenceDiagram
    participant A as Any authenticated principal - see STRIDE T1
    participant DC as DeleteTenantCommandHandler
    participant DB as Tenant row
    participant MEM as TenantMember rows
    participant AUTHN as Authentication sessions

    A->>DC: DELETE tenants id - class Authorize only
    DC->>DB: repository DeleteAsync - no member census
    DC-->>A: 200
    Note over MEM: memberships remain pointing at a deleted tenant - no event no cleanup
    Note over AUTHN: session eviction on tenant deletion is the Authentication module story docs auth cross-module-sequences section 2 - here nothing notifies it
```

## 4. Plan change (absent flow)

Plan changes are owned by Commerce.Billing (`OrchestrateSubscriptionPlanChange`); this module keeps only the orphan `PlanChangeRequest` DTO (`Commands/ChangeTenantPlan/PlanChangeRequest.cs`). The designed `TenantSubscriptionPlanChangedEvent` is dead (G4) — tenant-scoped limits/features learn of a plan change only on next read.
