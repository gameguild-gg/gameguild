# Finance.Ledgers — STRIDE threat model

> Module `GameGuild.Finance.Ledgers` · Documented against `develop` commit `60100459e6`.
> Families aligned with `01-user-flows.md`: **F1** tree mutation, **F2** entry posting/transfer/reversal, **F3** rollup reads, **F4** tenant scoping, **F5** integration-event egress + audit rail.

## F1 — Ledger tree mutation (create root/child; move/status/delete declared-dead)

| # | Threat (STRIDE) | Scenario | Mitigation (file:line) or residual |
|---|---|---|---|
| 1.1 | Spoofing/Elevation | Foreign-tenant root creation via body `TenantId` | actor-tenant pinning; SystemAdmin-only override (`Controllers/LedgersController.cs:231-247`) |
| 1.2 | Tampering | Cross-tenant child creation under foreign parent | parent visibility 404s (`:279-281`, `:322-326`) |
| 1.3 | Tampering | Structure corruption via bad hierarchy moves | designed cycle guard + closure rebuild (`Services/LedgerHierarchyService.cs:188, :239`) — **unreachable (D4)**; today the tree is append-only via API, so the threat is dormant with the feature |
| 1.4 | Repudiation | "Who created this ledger" | `CreatedByUserId` actor-pinned (`:249, :284`) + generic operation event per command; no hash chain (F5.2) |
| 1.5 | DoS | Ledger/code flooding | unique `(TenantId,Code)` caps per tenant at same code only; no quota event on the ledger commands (`Architecture/UseCaseEventContracts.g.cs:2-3` carry no QuotaImpact) — **residual** |
| 1.6 | Elevation | Writes without `Ledgers.Write` | policy on every mutation route (`Controllers/LedgerEntriesController.cs:112, 149, 179, 207, 228`) |

## F2 — Entry posting / transfer / reversal

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 2.1 | Spoofing | Client-supplied actor on entries | actor-pinned `CreatedByUserId` (`LedgerEntriesController.cs:126-134, :163-169, :193-197, :220, :242-245`) |
| 2.2 | Tampering | Negative/zero amounts | `Create` guard (`Entities/LedgerEntry.cs:211-214`) |
| 2.3 | Tampering | One-sided transfer | paired factory + single-save (`:244-286`, `Services/LedgerEntryService.cs:118-124`) |
| 2.4 | Tampering/Repudiation | Posted history silently deleted | `EnsureCanDelete` blocks reconciled/voided/transfer/reversal-chain but **permits deleting a plain Posted entry** (`:422-443`) — **residual L7**: the book is mutable history with only log-line audit |
| 2.5 | Repudiation | Double reversal disputes | one-reversal guard + pair-chasing (`:338-341`, `Services/...:151-162`); reversal references original and vice versa |
| 2.6 | DoS | Entry flooding inflating rollups | no rate limit, no quota events on posting — **residual** |
| 2.7 | Info disclosure | Entry data of another tenant | entry→ledger→tenant resolution 404 (`LedgerEntriesController.cs:394-408`) |

## F3 — Rollup reads (balance/consolidated/tree/categories/periods)

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 3.1 | Info disclosure | Cross-tenant balances | ledger visibility gate before every read (`:258-370` all call `CanReadLedgerAsync`) |
| 3.2 | Spoofing/Tampering | Forged cached stats | caches never read (`06-gaps.md` L4) — accidentally safe; recompute-per-read is the current integrity property |
| 3.3 | DoS | Deep-tree rollup costs | denormalized `ParentLedgerIds` keeps aggregation non-recursive (`Entities/LedgerEntry.cs:53-57`) |
| 3.4 | Repudiation | "Which entries made this balance" | rollups computed from rows at read time — reproducible, but no snapshot/timestamp pinning (as-of filters are the only provenance) |

## F4 — Tenant scoping

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 4.1 | Elevation | SystemAdmin impersonation breadth | `IsSystemAdmin` bypasses tenant clamps by design (`LedgersController.cs:316, :328-329`; `LedgerEntriesController.cs:385-387`) — typed admin, auditable; residual: admin reads are unlogged |
| 4.2 | Info disclosure | Tenant enumeration via by-code route | `CanReadTenant` 403 (`:63-77`) distinguishes "other tenant" (403) from "no such ledger" (404) — mild oracle, consistent with the rest of the platform |
| 4.3 | Tampering | Route-tenant substitution | tenant comes from actor except SystemAdmin (`:231-247`) |

## F5 — Integration-event egress + audit rail

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 5.1 | Tampering | Event payload altered post-posting | event written transactionally with the entry (`API:/Database/ApplicationDbContext.cs:184+`), dedup by EventId; platform outbox integrity applies |
| 5.2 | Repudiation | Book changes without durable evidence | only generic operation events + **log lines** (`Services/LedgerEntryService.cs:305-323`) — no hash chain, no tamper-evident store (contrast Compliance.Audit ECDSA chains, Economy journal integrity increments) — **residual**: the weakest audit rail in the money family (L7 companion) |
| 5.3 | Info disclosure | Event leaks amounts/parties | payload is `[NonPersonalEventData]` amounts/dates only (`Contracts:/FinanceIntegrationEvents.cs:32-38`) |
| 5.4 | DoS | Outbox growth with no consumer | unbounded accumulation of `finance.ledger-entry.posted.v1` rows (L3) — operational rather than security, but a quiet disk/latency drag |
| 5.5 | Spoofing | Forged events injected downstream | not applicable while no consumer exists; any future consumer MUST verify source module + schema version (`DurableIntegrationEventValidator` on the write side is not a subscriber-side check) |

## Family → gap mapping

| Family | Residual risks live in |
|---|---|
| F1 | L1 (move/status/delete dead — and the designed guards with them), 1.5 no quota |
| F2 | 2.4/L7 deletable posted history, 2.6 no rate limit/quota |
| F3 | clean (accidental cache safety) |
| F4 | 4.1 admin read logging |
| F5 | 5.2 weak audit rail, 5.4/L3 no-consumer outbox growth |
