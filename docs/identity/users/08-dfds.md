# Identity.Users Data Flow Diagrams

> `develop` 7ae5a28a65 · Level-0 + level-1 per family. Notation per `docs/security/auth-dfds.md` §1. Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Users/` unless stated.

## 1. Level 0 — context

```mermaid
flowchart LR
    SELF["User - own record"]
    ADMIN["Users admin"]
    AUTHN["Authentication module - sign-up"]
    AUTHZ["Authorization policies"]

    USERS(["0.0 GameGuild Identity.Users module"])

    PG[("PostgreSQL users profiles preferences metadata notifications")]
    OB[("Durable outbox")]

    SELF -->|"self reads and writes via UsersReadSelf EditSelf"| USERS
    ADMIN -->|"lifecycle and bulk via UsersAdmin Purge"| USERS
    AUTHN -->|"UserCreatedEvent on sign-up"| USERS
    AUTHZ -->|"policy verdicts"| USERS
    USERS -->|"user rows + JSON preference columns"| PG
    USERS -->|"UserCreatedEvent UserDeletedEvent"| OB
    USERS -->|"UserPurgedNotification and siblings - zero consumers"| VOID["no consumer"]
```

Boundaries: B1 client→API (attribute gate), B2 module→PostgreSQL, B3 module→outbox (durable), B4 self↔any-user (policy layer).

## 2. Level 1 — F1 lifecycle

```mermaid
flowchart TD
    subgraph API["B1 B4"]
        L1(["1.1 Create - UsersCreate"]) --> L2(["1.2 Update - EditSelf"])
        L2 --> L3(["1.3 Suspend Deactivate Activate Unsuspend - UsersAdmin"])
        L2 --> L4(["1.4 Soft delete - DeleteSelf"])
        L4 --> L5(["1.5 Restore - UsersAdmin"])
        L4 --> L6(["1.6 Purge - UsersPurge - ValidatePurge gate"])
        L3 --> L7(["1.7 Orphan UserSuspended and similar notifications - no consumer"])
        L1 --> L8(["1.8 durable UserCreatedEvent"])
        L4 --> L9(["1.9 durable UserDeletedEvent"])
    end
    subgraph STORES["B2 B3"]
        DU[("User rows")]
        DO[("OutboxMessage")]
    end
    L1 & L2 & L3 & L4 & L5 & L6 --> DU
    L8 & L9 --> DO
```

## 3. Level 1 — F2 preferences / metadata / profile

```mermaid
flowchart TD
    subgraph API["B1 B4 - all routes behind UsersReadSelf or UsersEditSelf"]
        P1(["2.1 GET - one query GetUserPreferencesQuery projected per family"]) --> P2(["2.2 Replace - full JSON column write"])
        P1 --> P3(["2.3 Update - partial merge"])
        P2 & P3 --> P4(["2.4 Reset - rewrite defaults"])
        P5(["2.5 Metadata and profile verbs - same shape"])
    end
    subgraph STORES["B2"]
        DP[("UserPreferences 5 JSON columns + UserMetadata + UserProfile")]
    end
    P2 & P3 & P4 & P5 --> DP
```

## 4. Level 1 — F3 per-user notifications

```mermaid
flowchart TD
    subgraph API["B1 B4"]
        N1(["3.1 List and get - UsersReadSelf"]) --> N2(["3.2 Mark read or unread - guarded no-ops"])
        N1 --> N3(["3.3 Archive unarchive - unguarded"])
        N2 & N3 --> N4(["3.4 Bulk twins - 4 commands without validators"])
    end
    subgraph STORES["B2"]
        DN[("UserNotification rows - IsRead IsArchived ArchivedAt")]
    end
    N2 & N3 & N4 --> DN
```

## 5. Coverage matrix

| Family | DFD | Highest-risk flow | STRIDE |
|---|---|---|---|
| F1 lifecycle | §2 | 1.7 orphan producers (invisible suspensions) | §2 |
| F2 preferences | §3 | 2.2/2.4 no-audit PII writes; split-brain notification prefs | §3 |
| F3 notifications | §4 | 3.4 unvalidated bulk verbs | §4 |
