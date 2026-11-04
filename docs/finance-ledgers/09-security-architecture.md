# Finance.Ledgers — security architecture

> Module `GameGuild.Finance.Ledgers` · Documented against `develop` commit `60100459e6`.
> Consolidates `07-stride-threat-model.md` and `08-dfds.md`: component view, permission map, the book's integrity properties (and non-properties), fail-verdicts, and the boundary statement against the Economy journal.

## 1. Components

```mermaid
flowchart TD
    subgraph HOST["composed host - fully wired"]
        REG["AddLedgerModule - called ApiProductComposition L176<br/>repos services validators scan"]
        SCAN["module scan - 25 handlers"]
        MODEL["FinanceLedgersModelConfiguration<br/>IModelConfiguration scan - 3 tables"]
    end
    subgraph MOD["module"]
        C1["LedgersController 11 routes"]
        C2["LedgerEntriesController 13 routes"]
        H["Handlers 7 commands 18 queries"]
        S1["LedgerHierarchyService - closure maintenance"]
        S2["LedgerEntryService - posting transfers reversals"]
        S3["LedgerRollupService - balances rollups"]
        S4["VirtualLedgerService - write side DEAD D1 D12 D13"]
    end
    subgraph STORES[("finance schema")]
        T1[("Ledgers")]
        T2[("LedgerClosures")]
        T3[("LedgerEntries + integration events queue")]
    end
    OUT[("platform outbox - finance.ledger-entry.posted.v1 no consumer")]
    C1 --> H
    C2 --> H
    H --> S1
    H --> S2
    H --> S3
    S1 --> T1
    S1 --> T2
    S2 --> T3
    S3 --> T3
    T3 --> OUT
```

Not in the picture because they do not exist: 13 command handlers + 7 query handlers (the dead 20, `02-cqrs-wiring-matrix.md` §3), the stats refresh path, the virtual write surface.

## 2. Permission map

| Capability | Gate | Evidence |
|---|---|---|
| Read any ledger/entry/balance/rollup in own tenant | `Ledgers.Read` | `Identity.Authorization/Policies.cs:94`; all read routes |
| Cross-tenant reads | SystemAdmin only | `Controllers/LedgersController.cs:315-316, 328-329` |
| Create root/child, post/transfer/patch/delete/reverse entries | `Ledgers.Write` + tenant/owner visibility | `Controllers/LedgerEntriesController.cs:112, 149, 179, 207, 228` |
| Move/status/budget/delete ledger, virtual CRUD, batch, reconcile | **no route** (dead) | §3 reconciliation |

## 3. Integrity properties (and non-properties) of the book

**Holds:**

1. Transfer atomicity — paired legs in one save; reversal chases the pair (`Services/LedgerEntryService.cs:118-124, 151-162`).
2. One reversal per entry; reversal is opposite-typed and Posted (`Entities/LedgerEntry.cs:331-373`).
3. Posting gates — status/type/direct-entry/delete checks on the ledger (`Entities/Ledger.cs:387-393`).
4. Event/entry transactionality — outbox write inside the entry's SaveChanges (`API:/Database/ApplicationDbContext.cs:184+`).

**Does not hold (documented so nobody assumes it):**

1. No balanced-legs invariant — single-sided postings are legal; this is **not** a double-entry journal (`05-cross-module-sequences.md` §3).
2. Posted entries are deletable without compensating rows (L7).
3. No reconciliation with the Economy money journal or Payments' ledger concepts (L6; Payments' `FinancialLedgerEntry` config is itself dead, wiring-gap-baseline `:27`).
4. Audit trail is log lines, not tamper-evident records (`04-event-inventory.md` §4).

## 4. Fail-open/fail-closed verdicts

| Dependency | Failure | Verdict |
|---|---|---|
| Ledger lookup on posting | missing/inactive/virtual | fail-closed (entity throw) |
| Transfer with one bad side | pre-checks both | fail-closed before writes |
| Reversal of reversed/pair-missing | guards | fail-closed |
| Outbox drain failure | same transaction | fail-closed — posting rolls back |
| Consumer of the posted event | none exists | neutral (nobody to fail) — L3 |
| Cached stats | never written/never read | neutral (accidentally consistent) — L4 |

## 5. Deployment invariants

1. `Finance.Ledgers` stays in `ApiProductComposition.EnabledModules` (`:79`) and `AddLedgerModule` stays called (`:176`) — removing either re-creates the #1158 dead-config state (tables vanish from the model on the next migration).
2. Keep the `finance` schema distinct from `economy_*` — the deliberate separation of the managerial book from the money journal (`05-cross-module-sequences.md` §3); any future bridge must be an explicit reconciliation process, not shared tables.
3. If any of the 20 dead types gets a handler, its use-case contract must be added (`Architecture/UseCaseEventContracts.g.cs` currently covers exactly the 7 live commands) or the contract pipeline will fail the build.
4. Never wire a route that mutates `Ledger` lifecycle (D3/D6) without also deciding the entry consequences — closing/archiving a ledger with live entries is currently unguarded territory the dead commands never had to answer.
