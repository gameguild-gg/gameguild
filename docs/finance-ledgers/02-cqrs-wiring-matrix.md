# Finance.Ledgers — CQRS / wiring matrix (incl. the 20-dead-types reconciliation)

> Module `GameGuild.Finance.Ledgers` · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `Ledgers:` = `apps/api/Source/Modules/GameGuild.Finance.Ledgers/`, `API:` = `apps/api/Source/GameGuild.API/`.

## 0. DI wiring

| Piece | Registration | Evidence | Live? |
|---|---|---|---|
| Repositories ×3, services ×4, validators-scan | `AddLedgerModule` | `Ledgers:/ServiceCollectionExtensions.cs:20-39`; called at `API:/Core/Setup/ApiProductComposition.cs:176` (the #1158 fix comment at `:173-175`) | **yes** |
| Handlers (25: 7 commands + 18 queries) | module scan | `API:/Core/Setup/ApplicationLayerExtensions.cs:110-145`; `Finance.Ledgers` in `EnabledModules` (`ApiProductComposition.cs:79`) | **yes** |
| Tables ×3 (schema `finance`) | `FinanceLedgersModelConfiguration : IModelConfiguration` | `Ledgers:/Configuration/LedgerEntityConfigurations.cs:372-380` via `API:/Database/ApplicationDbContext.cs:866-883`; migration `20261102105545_AddLedgersRatingsAndAchievementsTables` | **yes** (the #1169-era materialization) |
| G2 baseline | Finance.Ledgers no longer in `apps/api/tests/GameGuild.API.UnitTests/Architecture/wiring-gap-baseline.txt` G2 section | baseline `:22-23` (only Payments/KYC remain) | **clean** |

**Auth legend** — `[Authorize]` + `Policies.LedgersRead` reads / `LedgersWrite` mutations (`Identity.Authorization/Policies.cs:94,97`; seeded `PolicyDefinitionSeeder.cs:1383,1424`). **Rate limit legend** — `—` = none (no `EnableRateLimiting` on either controller).

## 1. Live HTTP surface (24 routes)

### LedgersController (`api/v1/ledgers`, `Ledgers:/Controllers/LedgersController.cs`)

| Route | Request | Validator | Handler | Auth | Rate limit | Unit tests | Integration tests | HOLE? |
|---|---|---|---|---|---|---|---|---|
| GET `{ledgerId}` | `GetLedgerByIdQuery` | — none in module | `GetLedgerByIdHandler` (`Handlers/LedgerHandlers.cs:90`) | LedgersRead | — | `Finance/FinanceLedgersCoverageCompletionTests.cs:655-657` | — | no validator |
| GET `by-code/{tenantId}/{code}` | `GetLedgerByCodeQuery` | — | `:100` | LedgersRead + `CanReadTenant` (`:315-316`) | — | `:658+` | — | no validator |
| GET `tenant/{tenantId}` | `GetLedgersByTenantQuery` | — | `:110` | LedgersRead + tenant | — | same | — | paging params exist on query, route does not expose them |
| GET `tenant/{tenantId}/roots` | `GetRootLedgersQuery` | — | `:120` | LedgersRead | — | same | — | |
| GET `{ledgerId}/children` | `GetLedgerChildrenQuery` | — | `:130` | LedgersRead + ledger visibility | — | same | — | |
| GET `{ledgerId}/hierarchy` | `GetLedgerHierarchyQuery` | — | `:140` | same | — | same | — | |
| GET `{ledgerId}/ancestors` | `GetLedgerAncestorsQuery` | — | `:149` | same | — | same | — | |
| GET `{ledgerId}/descendants` | `GetLedgerDescendantsQuery` | — | `:159` | same | — | same | — | |
| GET `tenant/{tenantId}/virtual` | `GetVirtualLedgersQuery` | — | `:180` | LedgersRead | — | same | — | **reads rows nothing can create via API (D1)** |
| POST `root` | `CreateRootLedgerCommand` | — | `:49` → `LedgerHierarchyService.CreateRootLedgerAsync` | LedgersWrite | — | `:653` | — | no validator (code/name/currency unvalidated at API edge) |
| POST `{parentLedgerId}/children` | `CreateChildLedgerCommand` | — | `:67` → `CreateChildLedgerAsync` | LedgersWrite + parent visibility | — | `:654` | — | no validator |

### LedgerEntriesController (`api/v1/ledgers`, `Ledgers:/Controllers/LedgerEntriesController.cs`)

| Route | Request | Validator | Handler | Auth | Rate limit | Unit tests | Integration tests | HOLE? |
|---|---|---|---|---|---|---|---|---|
| GET `entries/{entryId}` | `GetEntryByIdQuery` | — | `GetEntryByIdHandler` (`Handlers/EntryHandlers.cs:136`) | LedgersRead + entry→ledger tenant | — | completion tests | — | |
| GET `{ledgerId}/entries` | `GetEntriesByLedgerQuery` | — | `:146` | LedgersRead | — | same | — | |
| GET `{ledgerId}/entries/rollup` | `GetEntriesIncludingDescendantsQuery` | — | `:161` | LedgersRead | — | same | — | route name differs from paged dead twin (D18) |
| POST `{ledgerId}/entries` | `PostEntryCommand` | — | `:49` → `LedgerEntryService.PostEntryAsync` | LedgersWrite | — | `FinanceLedgerIntegrationEventTests.cs` (event queueing) | — | no validator (entity guards only) |
| POST `transfers` | `PostTransferCommand` | — | `:69` → `PostTransferAsync` | LedgersWrite + both ledgers visible | — | same | — | no validator |
| PATCH `entries/{entryId}` | `UpdateEntryCommand` | — | `:102` → `UpdateEntryAsync` | LedgersWrite | — | completion tests | — | no validator; reconciled/voided guarded in entity |
| DELETE `entries/{entryId}` | `DeleteEntryCommand` | — | `:118` → `DeleteEntryAsync` | LedgersWrite | — | same | — | deletes posted history (STRIDE 2.4) |
| POST `entries/{entryId}/reverse` | `ReverseEntryCommand` | — | `:87` → `ReverseEntryAsync` | LedgersWrite | — | same | — | |
| GET `{ledgerId}/balance` | `GetLedgerBalanceQuery` | — | `:197` → `LedgerRollupService` | LedgersRead | — | completion tests | — | |
| GET `{ledgerId}/balance/consolidated` | `GetConsolidatedBalanceQuery` | — | `:206` | LedgersRead | — | same | — | |
| GET `{ledgerId}/rollup` | `GetHierarchicalRollupQuery` | — | `:215` | LedgersRead | — | same | — | |
| GET `{ledgerId}/rollup/categories` | `GetCategoryRollupQuery` | — | `:228` | LedgersRead | — | same | — | |
| GET `{ledgerId}/rollup/periods` | `GetPeriodRollupQuery` | — | `:242` | LedgersRead | — | same | — | |

**Orphan handler** (wired but unreachable): `GetVirtualLedgerEntriesHandler` (`Handlers/EntryHandlers.cs:176` → `VirtualLedgerService.GetVirtualEntriesAsync:82`) has **no route** — the 26th handler with no caller.

**Use-case event contracts** cover exactly the 7 live commands (`Ledgers:/Architecture/UseCaseEventContracts.g.cs:2-8`): `PostEntry`/`PostTransfer`/`ReverseEntry` expect `FinanceLedgerEntryPostedEventV1`; the other 4 carry "generic durable operation event only". None of the 20 dead types has a contract.

## 2. Request-type census

| Category | Count | With handler | Without handler |
|---|---|---|---|
| Commands (`Ledgers:/Commands/LedgerCommands.cs`) | 20 | 7 | **13 (D1-D13)** |
| Queries (`Ledgers:/Queries/LedgerQueries.cs`) | 25 | 18 | **7 (D14-D20)** |
| Total | 45 | 25 | **20 dead** |

## 3. The 20-dead-request-types reconciliation

Each dead type: what it was meant to do (declaration + README), what is actually reachable (route / handler / service / entity — the deepest implemented layer), and the verdict — **hole** (gap: functionality promised by the module's own contracts, missing links) or **planned-redundant** (duplicate of a wired twin; declaration noise rather than missing feature).

### Dead commands (13)

| # | Request type | Meant to do | Deepest implementation actually present | Verdict |
|---|---|---|---|---|
| D1 | `CreateVirtualLedgerCommand` (`Commands/LedgerCommands.cs:43-49`) | create filter-based aggregate view (README `:127-135` documents full virtual CRUD) | service `VirtualLedgerService.CreateVirtualLedgerAsync` (`Services/VirtualLedgerService.cs:30`) + entity factory `Ledger.CreateVirtual` (`Entities/Ledger.cs:247-278`) + list/read handlers exist | **hole** — write surface missing; the list route (`tenant/{id}/virtual`) reads a set nothing can populate via API |
| D2 | `UpdateLedgerCommand` (`:54-59`) | rename/re-describe/tag a ledger | entity `Ledger.Update` (`Entities/Ledger.cs:287-293`) | **hole** — no service, handler, route |
| D3 | `ChangeLedgerStatusCommand` (`:64-67`) | lifecycle transitions Draft→Active→Inactive/Archived/Closed with the guarded transition table | entity `Ledger.ChangeStatus` incl. full transition validation (`:298-321`) | **hole** — an entire guarded state machine sits unreachable; every ledger is born `Active` and stays there (`:185, :229`) |
| D4 | `MoveLedgerCommand` (`:72-75`) | re-parent a subtree with closure rebuild | service `MoveLedgerAsync` + cycle guard `CanMoveToParentAsync` + `RebuildSubtreeClosuresAsync` (`Services/LedgerHierarchyService.cs:88-128, :188, :239`) | **hole** — the most complete dead stack in the module (service + guard + rebuild, no handler/route) |
| D5 | `SetLedgerBudgetCommand` (`:80-83`) | set/consume `BudgetLimit` | entity `SetBudgetLimit` (`Entities/Ledger.cs:326-331`) | **hole** — budget column never writable; nothing enforces it either |
| D6 | `DeleteLedgerCommand` (`:88-90`) | soft delete guarded by active children | entity `SoftDelete` (`:372-382`) | **hole** |
| D7 | `PostBatchEntriesCommand` (`:127-129`) | batch post entries (README `:118` documents `entries/batch`) | service `PostBatchAsync` (`Services/LedgerEntryService.cs:200-265`) | **hole** |
| D8 | `ReconcileEntryCommand` (`:142-145`) | mark entry reconciled vs external reference | service `ReconcileEntryAsync` (`:268-281`) + entity `Reconcile` (`Entities/LedgerEntry.cs:311-326`) | **hole** — the `Reconciled` state is unreachable through the API |
| D9 | `RefreshLedgerStatsCommand` (`:170-171`) | refresh cached stats | entity `UpdateCachedStats` (`Entities/Ledger.cs:360-367`) | **hole** — caches write-orphaned (L4) |
| D10 | `RefreshSubtreeStatsCommand` (`:176-177`) | same for a subtree | nothing beyond the entity method | **hole** |
| D11 | `ChangeEntryStatusCommand` (`:182-186`) | generic post/void/reconcile setter | partial: entity has `Post`/`Reconcile` but no generic setter | **hole** (weakest backing — partly subsumed by live reverse/reconcile paths) |
| D12 | `UpdateVirtualLedgerFilterCommand` (`:191-194`) | change a virtual ledger's filter | service `UpdateFilterAsync` (`Services/VirtualLedgerService.cs:63`) | **hole** — moot until D1 exists |
| D13 | `DeleteVirtualLedgerCommand` (`:199-201`) | remove a virtual ledger | entity soft-delete via `Ledger.SoftDelete` | **hole** — same |

### Dead queries (7)

| # | Request type | Meant to do | Deepest implementation | Verdict |
|---|---|---|---|---|
| D14 | `GetChildLedgersQuery` (`Queries/LedgerQueries.cs:43-44`) | children as `LedgerSummary` | twin `GetLedgerChildrenQuery` fully wired (`Handlers/LedgerHandlers.cs:130`) | **planned-redundant** — declared duplicate (summary DTO variant) |
| D15 | `SearchLedgersQuery` (`:81-88`) | tenant-wide ledger search w/ tags + paging | nothing (repository has no search method) | **hole** |
| D16 | `GetEntryByReferenceQuery` (`:103-105`) | find entry by `TXN-…` reference | nothing | **hole** — reference numbers exist (`Entities/LedgerEntry.cs:454-459`) but are unlookupable |
| D17 | `GetLedgerEntriesQuery` (`:110-116`) | paged entries w/ status filter | unpaged twin `GetEntriesByLedgerQuery` wired (`Handlers/EntryHandlers.cs:146`) | **planned-redundant** (paged variant) |
| D18 | `GetLedgerEntriesWithDescendantsQuery` (`:129-135`) | paged rollup entries | unpaged twin wired (`:161`) | **planned-redundant** (paged variant) |
| D19 | `SearchEntriesQuery` (`:158-172`) | filtered entry search (amount range, type, tags) | nothing | **hole** |
| D20 | `GetVirtualLedgerBalanceQuery` (`:222-224`) | balance of a virtual ledger | service `GetVirtualBalanceAsync` (`Services/VirtualLedgerService.cs:161`) | **hole** — service implemented, no handler |

### Reconciliation summary

| Verdict | Count | Items |
|---|---|---|
| **hole** (promised, partially implemented, unreachable) | 17 | D1-D13 (all dead commands), D15, D16, D19, D20 |
| **planned-redundant** (duplicate declaration of a wired twin) | 3 | D14, D17, D18 |

Arithmetic: 13 command holes + 4 query holes = 17 holes; 20 dead types total.

The pattern: **the service/entity layer is ~80% implemented; the handler+route layer exists for the minimal CRUD+rollup spine only.** This is not stub code — `MoveLedgerAsync` and `PostBatchAsync` are fully written — it is an unfinished wiring layer, exactly the shape the wiring-gap guards exist to catch (G1 catches *registered* handlers missing from DI; nothing catches *never-written* handlers, which is why these 20 sit outside the baseline file).

## 4. README-vs-code route reconciliation (16 phantom routes)

`Ledgers:/README.md:83-135` documents routes that do not exist: `POST {id}/move`, `POST {id}/status`, `POST {id}/refresh-stats`, `GET {id}/entries/with-descendants` (real route is `entries/rollup`), `POST {id}/entries/batch`, `api/v1/ledger-transfers` (real route is `ledgers/transfers`), and the full `api/v1/virtual-ledgers` CRUD surface (5 routes). See `06-gaps.md` L2.
