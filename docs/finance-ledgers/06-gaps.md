# Finance.Ledgers — gaps

> Module `GameGuild.Finance.Ledgers` · Documented against `develop` commit `60100459e6`.
> KNOWN = already tracked elsewhere · NEW = found by this audit. Every entry carries file:line at the documented commit.

## NEW (found by this audit)

### L1 — 20 dead request types (13 commands + 7 queries). **(headline)**

Declared in `Ledgers:/Commands/LedgerCommands.cs` (20 commands) and `Ledgers:/Queries/LedgerQueries.cs` (25 queries); handlers exist for only 7 commands and 18 queries (`Handlers/LedgerHandlers.cs` + `Handlers/EntryHandlers.cs`). The full itemized reconciliation — meaning per type, deepest implementation present, hole vs planned-redundant — is `02-cqrs-wiring-matrix.md` §3 (D1-D20). Summary: **17 holes** (entire ledger-mutation lifecycle D2-D6, entire virtual-ledger write surface D1/D12/D13, batch D7, reconciliation D8, stats refresh D9/D10, generic status D11, search D15/D16/D19, virtual balance D20) and **3 planned-redundant duplicates** (D14/D17/D18). These sit outside the wiring-gap guards by construction: G1 catches *registered-but-unwired handlers*; nothing catches *handlers that were never written*.

### L2 — The module README documents ~16 routes that do not exist.

`Ledgers:/README.md:83-135` advertises `POST {id}/move`, `POST {id}/status`, `POST {id}/refresh-stats`, `POST {id}/entries/batch`, `entries/with-descendants`, `api/v1/ledger-transfers` (real: `ledgers/transfers`), and the five-route `api/v1/virtual-ledgers` CRUD — none exist (`Controllers/` is the truth). Integration work built against the README will 404/405. The README also claims a `Validators/` folder (`:51`) that does not exist.

### L3 — The one integration event publishes to nobody.

`finance.ledger-entry.posted.v1` (`Ledgers:/Services/LedgerEntryService.cs:283-303` → `API:/Database/ApplicationDbContext.cs:184+` → platform outbox) has **zero consumers** (verified by search at `60100459e6`). Same family as economy N1/N2 (no-consumer outboxes); here the producer is alive and the subscriber side was never built. Downstream analytics, audit sinks, or Economy reconciliation cannot start from it.

### L4 — Rollup caches are write-orphaned.

`Ledger.CachedTotalDebit/CachedTotalCredit/CachedNetBalance/CachedEntryCount/StatsCalculatedAt` (`Entities/Ledger.cs:119-135`) exist, `UpdateCachedStats` (`:360-367`) exists, but the only writers would be `RefreshLedgerStatsCommand`/`RefreshSubtreeStatsCommand` (dead D9/D10), and no read path consults them (`Services/LedgerRollupService.cs` recomputes from entries). Dead weight plus a stale-data trap if ever wired.

### L5 — Twelve domain-event records are declared-dead.

`Events/LedgerDomainEvents.cs:20-171` — no construction sites anywhere (see `04-event-inventory.md` §2). The G4 guard (dead domain events, issue #1184 family) does not scan these because they are plain records, not `DurableIntegrationEvent`s. Extend the guard or delete the file.

### L6 — No reconciliation with the Economy journal (or anything else).

The managerial book and the money journal (`05-cross-module-sequences.md` §3) can drift arbitrarily; no detector, no report, no invariant ties `finance.LedgerEntries` to any money-moving module. If the module's purpose is managerial reporting, the absence of a source-of-truth statement is the gap; if it aspires to book-of-record, the absence of balanced-legs invariants is.

### L7 — Posted entries are hard-deletable; the book is mutable history.

`DELETE entries/{entryId}` permits removing a `Posted`, balance-affecting entry outright (`Entities/LedgerEntry.cs:422-443` allows deletion unless reconciled/voided/transfer/reversal-chain; `Services/LedgerEntryService.cs:186-197` executes). For a financial ledger, corrections should be reversals (which exist) — deletion-without-trace plus only best-effort log lines (`04-event-inventory.md` §4) undermines the book's integrity story. At minimum, delete should be audited as a durable operation event with the deleted amounts.

### L8 — No validators anywhere in the module.

No FluentValidation validators exist (README claims a folder, `README.md:51`); every guard is an entity throw mapped to 500. Input-shape errors (negative amounts caught at `Entities/LedgerEntry.cs:211-214` is the exception) surface as `InvalidOperationException`/`ArgumentException` 500s rather than 400s.

## KNOWN (tracked elsewhere, cross-referenced)

| # | Gap | Tracking | Relationship |
|---|-----|----------|--------------|
| K1 | Finance.Ledgers tables once failed to materialize (configs referenced-nowhere) | issue #1158, fixed via `API:/Core/Setup/ApiProductComposition.cs:173-176` + migration `20261102105545_AddLedgersRatingsAndAchievementsTables`; G2 baseline now clean of Ledgers (`wiring-gap-baseline.txt:22-23`) | the pre-history of this module — the 20 dead types are the *next* stratum the guards do not cover |
| K2 | Economy's no-consumer outboxes (posting-accepted, compliance-evidence) | economy package N1/N2 (`docs/economy/07-gaps.md`) | L3 is the platform-rail twin of the same disease |
| K3 | Payments' dead `FinancialLedgerEntryConfiguration` | `wiring-gap-baseline.txt:27`, issue #1184 | the third ledger concept; none reference this module |
| K4 | Dormant-table caveat in the table-count denominator | economy package K5 (`docs/economy/07-gaps.md:14`, issue #1092 §1) | resolved for Ledgers by the #1169-era fix; denominator notes can drop the Ledgers entry |

## Fix sketch (ordering)

1. **L1/L2** — either wire the handler+route layer for the implemented services (D4 move, D7 batch, D8 reconcile are fully backed) or prune the declarations and rewrite the README to the 24 real routes. The half-wired state is the worst of both.
2. **L7** — restrict delete to Pending (already invisible) or require a reversal; emit a durable operation event carrying the deleted amounts.
3. **L3** — land one consumer (analytics rollup or audit sink) or park the event behind a feature flag until one exists.
4. **L5/L4** — delete the dead event records and the cache columns, or wire D9/D10 to make them true.
5. **L6** — write the source-of-truth statement (managerial vs book-of-record) into this package's README and let the reconciliation design follow from it.
