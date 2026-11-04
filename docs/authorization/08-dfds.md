# Authorization Data Flow Diagrams

> Module `GameGuild.Identity.Authorization` · Documented against `develop` commit `7ae5a28a65` (post-#1203/#1230).
> Level-0 context plus one level-1 DFD per flow family (the seven families of [`flows/`](flows/), F1-F7). Notation follows `docs/security/auth-dfds.md` §1: external entities as rectangles, processes as stadiums, stores as cylinders, trust boundaries as subgraphs. STRIDE cross-reference: each family's threat analysis lives in [`07-stride-threat-model.md`](07-stride-threat-model.md) §2-§8.

Paths relative to `apps/api/Source/Modules/GameGuild.Identity.Authorization/` unless stated otherwise. Abbreviations: `Authn/` = `apps/api/Source/Modules/GameGuild.Identity.Authentication/`, `Audit/` = `apps/api/Source/Modules/GameGuild.Compliance.Audit/`, `Host/` = `apps/api/Source/GameGuild.API/`, `Notif/` = `apps/api/Source/Modules/GameGuild.Notifications/`.

## 1. Level 0 — context diagram

```mermaid
flowchart LR
    USER["Authenticated principal"]
    ADMIN["Tenant or system admin"]
    REVIEWER["Access reviewer"]
    IDPHOST["SCIM or external IdP webhook consumer"]

    AUTHZ(["0.0 GameGuild authorization engine"])

    PG[("PostgreSQL grants policies ACLs audits")]
    REDIS[("Redis L2 permission cache")]
    AUDIT[("Compliance audit log via sinks")]
    NOTIF[("Notifications delivery")]

    USER -->|"permission-gated requests via Policies"| AUTHZ
    ADMIN -->|"grant role JIT bundle management"| AUTHZ
    REVIEWER -->|"campaign decisions"| AUTHZ
    AUTHZ -->|"grants policies versions evaluations"| PG
    AUTHZ <-->|"cached decisions and invalidation"| REDIS
    AUTHZ -->|"required audit events in transaction"| AUDIT
    AUTHZ -->|"expiration and reminder alerts"| NOTIF
    AUTHZ -->|"permission revoked webhooks best effort"| IDPHOST
    AUTHZ -->|"allow or deny plus 403 or 200"| USER
```

Trust boundaries: B1 internet → API host (middleware chain, `cross-module-sequences.md` §1), B2 engine → PostgreSQL (tamper surface), B3 engine → Redis (staleness surface), B4 engine → audit sinks (in-transaction), B5 engine → notifications/webhooks (best-effort, TB-out without receipt).

## 2. Level 1 — F1 grant lifecycle

Source family: [`flows/permission-grant-lifecycle-flow.md`](flows/permission-grant-lifecycle-flow.md). Entities: `Entities/TenantPermission.cs`, `Entities/PermissionDelegation.cs`.

```mermaid
flowchart TD
    subgraph CLIENT["B1 client"]
        ADMIN1["Grantor admin"]
    end
    subgraph API["B2 authorization engine"]
        P1(["1.1 Grant mutation entry"]) --> P2(["1.2 PermissionMutationTransaction"])
        P2 --> P3(["1.3 Grant persist + audit row"])
        P2 --> P4(["1.4 Expiration stamp + alerts"])
        P2 --> P5(["1.5 Security version bump + cache invalidation"])
        P6(["1.6 Lazy expiry check at resolution"]) --> P2
        P7(["1.7 Expiration worker sweep"]) --> P4
    end
    subgraph STORES["B3 persistence"]
        D1[("TenantPermission grants")]
        D2[("PermissionAuditLog")]
        D3[("Redis permission cache")]
        D4[("Outbox durable events")]
    end
    ADMIN1 -->|"POST grants"| P1
    P3 --> D1
    P3 --> D2
    P4 -->|"best effort post commit gap E2"| D4
    P5 --> D3
    P7 -->|"expired grants"| D1
```

Flows of note: 1.4 is the only grant-lifecycle flow that leaves the transaction without an outbox row (best-effort alert, `Services/PermissionExpirationService.cs:83-89`); every other write is atomic with its audit row.

## 3. Level 1 — F2 JIT elevation

Source family: [`flows/permission-grant-lifecycle-flow.md`](flows/permission-grant-lifecycle-flow.md) (JIT section). Entity: `Entities/JitElevationRequest.cs`.

```mermaid
flowchart TD
    subgraph CLIENT["B1 client"]
        REQ["Requester"]
        APP["Approver must differ"]
    end
    subgraph API["B2 engine"]
        J1(["2.1 Request elevation"]) --> J2{"2.2 Approve guard - pending only - no self approval"}
        J2 -->|"approve + auto activate"| J3(["2.3 Activation window opens"])
        J2 -->|"deny"| J4(["2.4 Denied terminal"])
        J3 --> J5(["2.5 In-force check IsGrantInForce"])
        J3 --> J6(["2.6 Revoke + tenant security version"])
    end
    subgraph STORES["B3"]
        DJ[("JitElevationRequest")]
        DV[("TenantSecurityVersion")]
    end
    REQ --> J1
    APP --> J2
    J1 --> DJ
    J3 --> DJ
    J6 --> DV
```

Flows of note: 2.6 bumps the tenant version only — no user-level security version exists on this path (gap G10, `Services/JitElevationService.cs:280-303`); 2.2's guards are `Entities/JitElevationRequest.cs:106-113`.

## 4. Level 1 — F3 ABAC overlay at resolution

Source family: [`flows/abac-policy-crud-evaluation-flow.md`](flows/abac-policy-crud-evaluation-flow.md). Extension: `Services/AbacPolicyEvaluationExtension.cs` (`Order` 500).

```mermaid
flowchart TD
    subgraph ENGINE["B2 effective permission resolver"]
        A1(["3.1 Built-in layers 1-8"]) --> A2(["3.2 Extension chain by Order"])
        A2 --> A3(["3.3 ABAC overlay Order 500"])
        A3 -->|"deny subtract - allow never mints"| A4(["3.4 DENY-WINS merge"])
        A3 -->|"store failure - permissive skip gap A2"| A4
    end
    DAB[("AbacPolicy tables")]
    A3 <-->|"policy read"| DAB
```

## 5. Level 1 — F4 conditional policies (management + resolution veto)

Source family: [`flows/conditional-policy-crud-evaluation-flow.md`](flows/conditional-policy-crud-evaluation-flow.md). Extension: `Services/ConditionalPolicyEvaluationExtension.cs` (post-#1230).

```mermaid
flowchart TD
    subgraph CLIENT["B1 client"]
        POL["Policy admin"]
    end
    subgraph API["B2 engine"]
        C1(["4.1 Draft policy - 422 planned surface for CRUD via unavailability contract"]) --> C2(["4.2 Sign bundle - IPolicyBundleSignatureService"])
        C2 --> C3(["4.3 Approve and deploy version snapshot"])
        C3 --> C4(["4.4 Conditional overlay at resolution Order 400 - registered by 1230"])
        C4 -->|"deny subtracts"| C5(["4.5 DENY-WINS merge then ABAC overlay last veto"])
        C4 -->|"evaluation error + FailClosedOnEvaluationError true - deny all of the resolution"| C5
        C4 -->|"no active policies - contributes nothing documented default"| C5
    end
    subgraph STORES["B3"]
        DC[("ConditionalPolicy + PolicyVersionHistory + bundles")]
    end
    POL -->|"management reads live via 1203 handlers"| C1
    C3 --> DC
    C4 <-->|"active policy read once per resolution"| DC
```

## 6. Level 1 — F5 policy bundles and version history

Source family: [`flows/conditional-policy-crud-evaluation-flow.md`](flows/conditional-policy-crud-evaluation-flow.md) (bundle path).

```mermaid
flowchart TD
    subgraph API["B2"]
        B1P(["5.1 Create draft bundle"]) --> B2P(["5.2 Sign - signature service"])
        B2P --> B3P(["5.3 Approve"])
        B3P --> B4P(["5.4 Deploy - version snapshot appended"])
        B4P --> B5P(["5.5 Rollback to snapshot"])
        B4P --> B6{"5.6 Serve - Revoked check"}
        B6 -->|"Revoked has no writer gap G6"| B7P(["5.7 Active serve"])
    end
    DB[("PolicyVersionHistory + bundles")]
    B4P --> DB
    B5P --> DB
```

## 7. Level 1 — F6 access review campaigns

Source family: [`flows/access-review-campaign-flow.md`](flows/access-review-campaign-flow.md). Post-#1230 remediation included.

```mermaid
flowchart TD
    subgraph CLIENT["B1"]
        RV["Reviewer"]
    end
    subgraph API["B2 engine"]
        R1(["6.1 Campaign create start items"]) --> R2(["6.2 Decision approve or revoke with state guards post 1230"])
        R2 -->|"revoke"| R3(["6.3 Remediation - grant revocation + role removal + audit row - fixed D1 in 1230"])
        R1 --> R4(["6.4 Reminders - notification now consumed post 1230"])
        R2 --> R5(["6.5 Complete cancel expire counters"])
    end
    subgraph STORES["B3"]
        DR[("AccessReview campaign + items")]
        DA[("Central audit log via sinks")]
        DN[("Notifications durable queue")]
    end
    RV --> R2
    R1 --> DR
    R3 --> DA
    R4 -->|"reminder stamp then publish gap G12"| DN
```

## 8. Level 1 — F7 orchestrated CQRS checks

Source family: [`flows/orchestrated-permission-checks-flow.md`](flows/orchestrated-permission-checks-flow.md). Behavior: `Behaviors/AuthorizationBehavior.cs`.

```mermaid
flowchart TD
    subgraph PIPELINE["B2 CQRS pipeline"]
        O1(["7.1 Mediator send with AuthorizeRequest attribute"]) --> O2{"7.2 Attribute resolved - typed only"}
        O2 --> O3(["7.3 ResourceId extraction by reflection - no startup contract validation gap G9"])
        O3 --> O4(["7.4 ACL subject ForUser only - role and group entries cannot match gap G8"])
        O4 --> O5(["7.5 Requirement handlers + rule evaluator chain"])
        O5 -->|"satisfied"| O6(["7.6 Handler executes"])
        O5 -->|"not satisfied"| O7(["7.7 Deny - SecurityException mapped by ExceptionHandlingMiddleware"])
    end
    DACL[("AccessControlListEntry")]
    O4 <-->|"ACL lookup"| DACL
```

## 9. Coverage matrix

| Family | Level-1 DFD | Highest-risk flow | STRIDE section |
|---|---|---|---|
| F1 grant lifecycle | §2 | 1.4 best-effort expiration alert (no outbox) | §2 |
| F2 JIT elevation | §3 | 2.6 tenant-only version bump | §3 |
| F3 ABAC overlay | §4 | 3.3 permissive skip on store failure | §4 |
| F4 conditional | §5 | 4.4 fail-closed guard (post-#1230); default-allow by absence | §5 |
| F5 bundles | §6 | 5.6 revocation check without a writer | §6 |
| F6 access review | §7 | 6.3 remediation (post-#1230) | §7 |
| F7 orchestrated | §8 | 7.3/7.4 silent ACL narrowing | §8 |

## 10. Maintenance rules

- Keep families in lockstep with the `flows/` directory — a new flow file adds a family here, in the STRIDE doc, and in the tracker row.
- Store shapes mirror EF configurations; if a table is added or renamed (`G2` guard territory), update the cylinders here in the same PR.
- The management-surface posture (16 × designed 422 via the unavailable-contract, hidden controllers) is pinned by `apps/api/tests/GameGuild.API.UnitTests/Architecture/PlannedAuthorizationSurfaceGuardTests.cs`; DFDs must keep depicting that surface as *reads live / writes planned*, not dead.
