# Finance.Ledgers user flows — happy path and every edge case

> Module `GameGuild.Finance.Ledgers` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Ledgers:` = `apps/api/Source/Modules/GameGuild.Finance.Ledgers/`.

## 1. Create root / child ledger — closure-table maintenance

`POST api/v1/ledgers/root` (`Ledgers:/Controllers/LedgersController.cs:222-262`) and `POST api/v1/ledgers/{parentLedgerId}/children` (`:267-302`) → `CreateRootLedgerCommand`/`CreateChildLedgerCommand` → handlers (`Handlers/LedgerHandlers.cs:49-84`) → `LedgerHierarchyService.CreateRootLedgerAsync/CreateChildLedgerAsync` (`Services/LedgerHierarchyService.cs:26-87`) which maintain the closure table.

```mermaid
flowchart TD
    subgraph TREE["Ledger tree creation - LedgersController POST root L222"]
        T1["Client POST ledgers root<br/>code name currencyCode"] --> T2{"Actor SystemAdmin and body TenantId set<br/>L233-247"}
        T2 -- "yes" --> T3["target tenant from body"]
        T2 -- "no" --> T4{"Actor tenant present L240-246"}
        T4 -- "absent" --> TX1["403 Forbid L242-244"]
        T4 -- "present" --> T5["tenant pinned to actor tenant"]
        T3 --> T6["CreateRootLedgerCommand<br/>CreatedByUserId from actor L249-258"]
        T5 --> T6
        T6 --> T7["Ledger CreateRoot<br/>Type Root Status Active depth 0 path slash code<br/>Entities Ledger.cs L168-196"]
        T7 --> T8["closure self-row depth 0<br/>HierarchyService L26-51"]
        T8 --> T9["201 LedgerDto"]
        C1["POST ledgers parent children L267"] --> C2{"CanReadLedgerAsync<br/>parent visible in caller tenant L279-281"}
        C2 -- "no" --> CX1["404 - other tenants invisible"]
        C2 -- "yes" --> C3["CreateChildLedgerCommand<br/>type code name L286-298"]
        C3 --> C4{"Entity guards<br/>parent not Virtual and type not Root<br/>Entities Ledger.cs L210-218"}
        C4 -- "violation" --> CX2["InvalidOperation 500"]
        C4 -- "ok" --> C5["Ledger CreateChild<br/>currency inherits parent unless given L220-242"]
        C5 --> C6["closure rows for every ancestor<br/>HierarchyService L53-87"]
        C6 --> C7["201 LedgerDto"]
    end
```

### 1a. Edge-case matrix — tree creation

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Non-admin supplies foreign `TenantId` in body | actor-tenant pinning (`LedgersController.cs:231-247`) | silently pinned to own tenant — no cross-tenant root creation |
| 2 | Actor without tenant creates root | guard (`:240-246`) | 403 |
| 3 | Duplicate code in tenant | unique index `(TenantId,Code)` (`Configuration/LedgerEntityConfigurations.cs:21-23`) | DB uniqueness violation → 500/409 mapping |
| 4 | Child under a virtual ledger | entity guard (`Entities/Ledger.cs:210-213`) | `InvalidOperationException` |
| 5 | Creating a `Root`-typed child | entity guard (`:215-218`) | `InvalidOperationException` |
| 6 | Child currency unspecified | inheritance (`:230`) | parent currency adopted |
| 7 | Moving a ledger | `MoveLedgerCommand` declared (`Commands/LedgerCommands.cs:72-75`), service `MoveLedgerAsync` implemented (`Services/LedgerHierarchyService.cs:88-128`), cycle guard `CanMoveToParentAsync` (`:188`) — **no handler, no route** (dead type D4, `02-cqrs-wiring-matrix.md` §3) |

## 2. Post entry / transfer / reverse — the double-sided surface that exists

`POST api/v1/ledgers/{ledgerId}/entries` (`Ledgers:/Controllers/LedgerEntriesController.cs:111-143`), `POST api/v1/ledgers/transfers` (`:148-173`), `POST api/v1/ledgers/entries/{entryId}/reverse` (`:227-249`) → `LedgerEntryService` (`Services/LedgerEntryService.cs:37-165`).

```mermaid
flowchart TD
    subgraph POSTING["Entry posting - LedgerEntryService PostEntryAsync L37-78"]
        E1["PostEntryCommand<br/>actor-pinned CreatedByUserId L127-139"] --> E2{"Ledger exists L49-50"}
        E2 -- "no" --> EX1["InvalidOperation"]
        E2 -- "yes" --> E3{"CanAcceptEntry<br/>Active and AllowDirectEntries and not Virtual and not deleted<br/>Entities Ledger.cs L387-393"}
        E3 -- "no" --> EX2["cannot accept entries"]
        E3 -- "yes" --> E4["ancestor ids from closure minus self L56-61"]
        E4 --> E5["LedgerEntry Create - status Pending amount must be positive<br/>Entities LedgerEntry.cs L195-238"]
        E5 --> E6["entry Post - Posted same transaction L71"]
        E6 --> E7["QueuePostedEvent - FinanceLedgerEntryPostedEventV1<br/>with correlation from operation context L283-303"]
        E7 --> E8["finance audit log line L75"]
        E8 --> E9["SaveChanges - event drained to platform outbox"]
        TR1["PostTransferCommand L148-173"] --> TR2["two legs - source Credit destination Debit<br/>CreateTransfer L244-286"]
        TR2 --> TR3["pair ids linked both ways L277-283"]
        TR3 --> TR4["both posted both queued L119-122"]
        RV1["ReverseEntryCommand L227-249"] --> RV2{"entry not Voided and not already reversed<br/>CreateReversal L331-341"}
        RV2 -- "violated" --> RVX["InvalidOperation"]
        RV2 -- "ok" --> RV3["opposite-type Posted reversal<br/>original becomes Voided L343-370"]
        RV3 --> RV4{"TransferPairEntryId set L151-162"}
        RV4 -- "yes" --> RV5["pair entry reversed too - double-sided undo"]
        RV4 -- "no" --> RV6["single-sided reversal"]
        RV5 --> RV7["reversal event queued for each"]
        RV6 --> RV7
    end
```

### 2a. Edge-case matrix — posting/transfer/reversal

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Client-supplied `CreatedByUserId` | actor-pinned (`LedgerEntriesController.cs:126-134`) | actor identity wins |
| 2 | Zero/negative amount | `Create` guard (`Entities/LedgerEntry.cs:211-214`) | `ArgumentException` |
| 3 | Posting to virtual / archived / inactive ledger | `CanAcceptEntry` (`Entities/Ledger.cs:387-393`) | rejected |
| 4 | Cross-tenant ledger | 404 via `CanReadLedgerAsync` (`LedgerEntriesController.cs:383-392`) | invisible, not 403 |
| 5 | Transfer where only one side is postable | both ledgers checked (`Services/LedgerEntryService.cs:97-101`) | rejected before any write |
| 6 | Transfer legs atomicity | `AddRangeAsync([source,dest])` single save (`:124`) | both or neither |
| 7 | Reversing one leg of a transfer | pair-chasing (`:151-162`) | both legs reversed — paired integrity holds |
| 8 | Double reversal | `ReversedByEntryId` guard (`Entities/LedgerEntry.cs:338-341`) | rejected |
| 9 | Deleting a posted (non-reconciled, non-transfer) entry | `EnsureCanDelete` (`:422-443`) | allowed — posted history can vanish from the book (STRIDE 2.4) |
| 10 | Batch posting | `PostBatchEntriesCommand` declared, service `PostBatchAsync` implemented (`:200-265`) — **no handler, no route** (dead type D7) |
| 11 | Reconciliation against external refs | `ReconcileEntryCommand` declared, service `ReconcileEntryAsync` + entity `Reconcile` implemented (`:268-281`, `Entities/LedgerEntry.cs:311-326`) — **no handler, no route** (dead type D8) |

## 3. Balance / consolidated / rollup reads

`GET {ledgerId}/balance`, `/balance/consolidated`, `/rollup`, `/rollup/categories`, `/rollup/periods` (`Ledgers:/Controllers/LedgerEntriesController.cs:258-370`) → `LedgerRollupService` (`Services/LedgerRollupService.cs`).

```mermaid
flowchart TD
    subgraph ROLLUP["Rollup reads"]
        B1["GET balance L258"] --> B2["GetBalanceAsync - direct entries only<br/>sum debit minus credit as of date"]
        B3["GET balance consolidated L279"] --> B4["GetConsolidatedBalanceAsync<br/>entries where id in ParentLedgerIds or direct"]
        B5["GET rollup L300"] --> B6["GetHierarchicalRollupAsync<br/>tree with per-node balances"]
        B7["GET rollup categories L322"] --> B8["GetCategoryRollupAsync includeDescendants"]
        B9["GET rollup periods L348"] --> B10["GetPeriodRollupAsync granularity<br/>Day Week Month Quarter Year"]
    end
```

### 3a. Edge-case matrix — rollups

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Ledger with no entries | balance zero record returned | deterministic zero, not 404 |
| 2 | `asOfDate` boundary | end-of-day conversion (`Services/LedgerRollupService.cs:33-40`) | inclusive-to-end-of-day semantics |
| 3 | Deep hierarchies | closure-table `ParentLedgerIds` denormalized on entries (`Entities/LedgerEntry.cs:53-57`) | subtree aggregation without recursive CTE |
| 4 | Cached stats used? | `CachedNetBalance` etc. (`Entities/Ledger.cs:119-135`) | **never read by the rollup paths — recomputed every time; the caches are write-orphaned (L4)** |
| 5 | Virtual-ledger balance | `GetVirtualLedgerBalanceQuery` — **no handler** (dead type D20); service `GetVirtualBalanceAsync` implemented (`Services/VirtualLedgerService.cs:161`) — unreachable |
| 6 | Searching ledgers/entries | `SearchLedgersQuery`/`SearchEntriesQuery` — **no handlers** (dead types D15/D19 in §3) |
