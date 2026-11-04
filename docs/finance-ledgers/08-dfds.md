# Finance.Ledgers — data-flow diagrams

> Module `GameGuild.Finance.Ledgers` · Documented against `develop` commit `60100459e6`.
> PII note: the module stores no personal identifiers beyond actor/creator user ids; entries carry counterparty *names* (`CounterpartyName`, `Entities/LedgerEntry.cs:110-112`) — flagged below.

## Level 0 — context

```mermaid
flowchart TD
    ACT(("tenant actor - Ledgers.Read or Write"))
    ADMIN(("SystemAdmin"))
    OUT[("platform outbox rail")]
    NONE(("would-be consumers - NONE L3"))
    MOD["GameGuild.Finance.Ledgers<br/>hierarchy posting rollups"]
    STORES[("finance.Ledgers<br/>finance.LedgerClosures<br/>finance.LedgerEntries")]
    ECON(("Economy money journal<br/>disjoint - no bridge L6 05-cross-module section 3"))

    ACT -->|"browse post transfer reverse read balances"| MOD
    ADMIN -->|"cross-tenant reads creates"| MOD
    MOD --> STORES
    MOD -->|"finance.ledger-entry.posted.v1"| OUT
    OUT -.->|"no subscriber"| NONE
    ECON -.->|"no shared tables events or sync"| MOD
```

Trust boundaries: TB1 authenticated tenant actor → API; TB1a SystemAdmin (tenant clamp bypass); TB2 API → finance schema (in-process EF); TB3 API → platform outbox (transactional write, external dispatch).

## F1 — Tree mutation (level 1)

```mermaid
flowchart TD
    subgraph TB1["TB1 tenant actor"]
        A1(("actor Ledgers.Write"))
    end
    subgraph API["API process"]
        M1["tenancy resolve - actor tenant unless SystemAdmin<br/>LedgersController.cs L231-247"] --> M2["CreateRoot or CreateChild command<br/>actor-pinned creator L249-258 L284-298"]
        M2 --> M3["LedgerHierarchyService<br/>entity factories + closure rows"]
    end
    A1 --> M1
    M3 --> S1[("finance.Ledgers")]
    M3 --> S2[("finance.LedgerClosures")]
```

Dormant legs: move (D4), status (D3), budget (D5), delete (D6) — commands exist, deepest implementations exist, no handler/route (`02-cqrs-wiring-matrix.md` §3).

## F2 — Entry posting / transfer / reversal (level 1)

```mermaid
flowchart TD
    subgraph TB1b["TB1 tenant actor"]
        E1(("actor Ledgers.Write"))
    end
    subgraph API["API process"]
        P1["visibility gate CanReadLedgerAsync<br/>LedgerEntriesController.cs L383-392"] --> P2["LedgerEntryService PostEntry PostTransfer Reverse<br/>L37-197"]
        P2 --> P3{"CanAcceptEntry<br/>Active not Virtual AllowDirectEntries"}
        P3 --> P4["entry factory - Pending then Post inline"]
        P4 --> P5["QueuePostedEvent V1 with correlation<br/>L283-303"]
    end
    E1 --> P1
    P4 --> S3[("finance.LedgerEntries<br/>PII - CounterpartyName optional")]
    P5 --> OUT[("platform outbox TB3")]
    P2 --> LOG["structured log lines only<br/>no hash chain L7"]
```

PII: `CounterpartyName` (`Entities/LedgerEntry.cs:110`) is the only free-text counterparty field — crosses TB1 and is excluded from the event payload (`Contracts:/FinanceIntegrationEvents.cs:32-38`).

## F3 — Rollup reads (level 1)

```mermaid
flowchart TD
    subgraph TB1c["TB1 tenant actor or SystemAdmin"]
        R1(("reader Ledgers.Read"))
    end
    subgraph API["API process"]
        G1["five read routes L258-370"] --> G2["visibility gate per ledger"]
        G2 --> G3["LedgerRollupService recompute from entries<br/>ParentLedgerIds aggregation"]
    end
    R1 --> G1
    G3 --> S3[("finance.LedgerEntries")]
    G3 -.->|"caches never read L4"| S1[("finance.Ledgers.Cached columns<br/>write-orphaned")]
```

## F4/F5 — Tenant scoping and egress (level 1)

```mermaid
flowchart TD
    subgraph TB1d["TB1 tenant boundary"]
        T1(("caller"))
    end
    subgraph API["API process"]
        SC1["route tenant honored only for SystemAdmin<br/>L308-329 L376-408"] --> SC2["cross-tenant = 404 not 403"]
    end
    T1 --> SC1
    SC2 --> S1[("tenant-scoped rows")]
    subgraph TB3["TB3 outbox boundary"]
        O1["SaveChangesAsync drain<br/>API ApplicationDbContext.cs L184+"]
    end
    O1 --> O2[("OutboxMessage rail<br/>dispatch DLQ replay")]
    O2 -.->|"finance.ledger-entry.posted.v1 - zero consumers L3"| C1(("nobody"))
```

## Boundary inventory

| Boundary | Crossers | Control |
|---|---|---|
| TB1 tenant actor → API | 24 routes | `[Authorize]` + `Ledgers.Read`/`Ledgers.Write`; actor-pinned identity fields; 404 tenant cloaking; no rate limiting |
| TB1a SystemAdmin | cross-tenant reads/creates | typed admin claim; unlogged reads (STRIDE 4.1) |
| TB2 API ↔ finance schema | 3 tables | in-process EF; unique keys for tenant/code, sequence, closure pairs; optimistic RowVersion on entities |
| TB3 API → platform outbox | one event type | transactional write, dedup by EventId, validator on drain; downstream consumers: none (L3) |
| TB-Economy (documentation boundary) | nothing | disjoint ledgers, no bridge, no reconciliation (L6) |
