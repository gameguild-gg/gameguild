# Identity.Tenants Data Flow Diagrams

> `develop` 7ae5a28a65 · Level-0 + level-1 per family. Notation per `docs/security/auth-dfds.md` §1. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Tenants/` unless stated.

## 1. Level 0 — context

```mermaid
flowchart LR
    USER["Tenant member"]
    ADMIN["Tenant admin - SettingsManage MembersManage"]
    ANYAUTH["Any authenticated principal - lifecycle verbs"]
    NOTIF["Notifications module"]

    TEN(["0.0 GameGuild Identity.Tenants module"])

    PG[("PostgreSQL tenants members invites settings usage")]
    OB[("Durable outbox")]
    VOID["Discarded in-memory events"]

    USER -->|"self membership reads"| TEN
    ADMIN -->|"settings and membership writes"| TEN
    ANYAUTH -->|"CRUD lifecycle bulk - class Authorize only - STRIDE T1"| TEN
    TEN -->|"tenant rows"| PG
    TEN -->|"TenantMemberInviteRequestedV1"| OB
    OB -->|"invite emails"| NOTIF
    TEN -->|"7 AddDomainEvent types"| VOID
```

Boundaries: B1 client→API; B2 any-tenant→tenant-data (the takeover surface); B3 module→PostgreSQL; B4 outbox→Notifications.

## 2. Level 1 — F1 lifecycle

```mermaid
flowchart TD
    subgraph API["B1 B2"]
        L1(["1.1 Create - creator becomes Owner"]) --> L2(["1.2 Update - E9 500 on missing"])
        L1 --> L3(["1.3 Activate Deactivate - events discarded"])
        L1 --> L4(["1.4 Archive Unarchive - ValidateForArchive not consulted"])
        L1 --> L5(["1.5 Delete - no member census - ValidateForDeletion not consulted"])
        L5 --> L6(["1.6 Recover Restore Purge - no purge gate"])
        L1 --> L7(["1.7 Bulk x9 - per-item accounting"])
    end
    subgraph STORES["B3"]
        DT[("Tenant rows")]
        DA[("TenantAuditLog - no writers")]
    end
    L1 & L2 & L3 & L4 & L5 & L6 & L7 --> DT
```

## 3. Level 1 — F2 memberships and invites

```mermaid
flowchart TD
    subgraph API["B1 B2 - writes behind MembersManage"]
        M1(["2.1 Add member - direct or invite pending"]) --> M2(["2.2 Update role - unguarded"])
        M1 --> M3(["2.3 Set status activate deactivate - no owner guard"])
        M1 --> M4(["2.4 Invite resend cancel accept - 3-action lifecycle"])
        M4 --> M5(["2.5 Durable TenantMemberInviteRequestedV1 - the one live event"])
        M6(["2.6 Remove member - TenantMemberRemovedEvent discarded"])
    end
    subgraph STORES["B3 B4"]
        DM[("TenantMember rows + invite metadata JSON")]
        DO[("OutboxMessage")]
    end
    M1 & M2 & M3 & M4 & M6 --> DM
    M5 --> DO
```

## 4. Level 1 — F3 tenant resolution

```mermaid
flowchart TD
    subgraph MW["B1 middleware - before authorization"]
        R1(["3.1 X-Tenant-Id header"]) --> R6({"3.6 Resolution arbiter"})
        R2(["3.2 Host domain"]) --> R6
        R3(["3.3 Query string"]) --> R6
        R4(["3.4 Route value"]) --> R6
        R5(["3.5 JWT claim"]) --> R6
        R6 -->|"resolved"| R7(["3.7 Tenant bound to context"])
        R6 -->|"anonymous unresolved"| R8(["3.8 Default tenant - GetDefaultTenantQuery"])
        R6 -->|"authenticated unresolved"| R9(["3.9 No fallback - anti cross-tenant guard - TenantMiddleware.cs 360-368"])
    end
```

## 5. Coverage matrix

| Family | DFD | Highest-risk flow | STRIDE |
|---|---|---|---|
| F1 lifecycle | §2 | 1.5 delete without census (T1/T1a) | §2 |
| F2 memberships | §3 | 2.6 member-remove invisible (T8) | §3 |
| F3 resolution | §4 | 3.6 header-first ladder + default-tenant guard | §4 |
