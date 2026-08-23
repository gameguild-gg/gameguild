# Economy production traceability

This matrix links the normative architecture and threat model to the executable
surface. `missing` means the implementation is not sufficient for production;
existing prototype code is cited so the replacement work has an explicit owner.
`blocked-by-configuration` is never an implicit permission to move value.

| Requirement | Status | Executable evidence or implementation target |
| --- | --- | --- |
| Fixed parity, append-only postings, source stamps, FIFO allocation and provenance reversals | implemented | `GameGuild.Economy` posting and PostgreSQL gateway tests, including `apps/api/tests/GameGuild.Economy.UnitTests/Persistence/PostgreSqlFifoFragmentReservationGatewayTests.cs` and `apps/api/tests/GameGuild.API.UnitTests/Database/EconomyWriterParityPostgreSqlMigrationTests.cs` |
| Risk-reserve binding, unique outbox, FIFO payout reservation, debt preservation and actor/tenant/risk/root binding | implemented | `PostgreSqlHardToSoftConversionGatewayTests`, `PostgreSqlFifoFragmentReservationGatewayTests`, `PostgreSqlProviderReversalGatewayTests` and `DurablePayoutReservationWorkflowTests` provide behavioral coverage; `EconomyWriterParityPostgreSqlMigrationTests` validates migration rollback and replay. Retain this evidence when retiring `feat/economy-ledger-hardening`. |
| Runtime uses PostgreSQL rather than process memory for entity graph, aggregate limits, cooldown, review, reserve, custody, anchors, AdRewards and Marketplace | missing | Replace the in-memory registrations under `apps/api/Source/Modules/GameGuild.Economy*` with durable gateway implementations and integration tests |
| Financial Crime, Trust/Safety and KYC inputs are versioned, expiring, auditable, durable and fail-closed | missing | Implement durable inbox/outbox-backed input adapters; disabled sources are not production evidence |
| Journal verification, projection recomposition and independent signed WORM anchors | missing | Add durable verifier, signed anchor publisher, independent storage adapter and restore/replay tests |
| Capability composition enables Core, Bounties, Marketplace, AdRewards, Payouts and Treasury only when their predicates are valid | missing | `apps/api/Source/GameGuild.API/Core/Setup/ApiProductComposition.cs` currently composes only Core and Payouts |
| Versioned policies for fees, limits, reserves, cooldowns, providers and jurisdictions have no permissive default | missing | `AllowedJurisdictions` now keeps all value movement disabled while the global allowlist is empty. Implement the signed durable policy store, per-operation jurisdiction binding, and the remaining policy versions. |
| Shadow legacy migration includes provenance backfill, reconciliation, reversible cutover and read-only mode | missing | Add a dedicated migration workflow and production-snapshot upgrade verification |
| Self-service wallet, conversion and payout request/read/cancel routes | implemented | `apps/api/Source/GameGuild.API/Core/Controllers/EconomyWalletController.cs` and its controller tests |
| Tenant-scoped payout review with immutable reason, two distinct administrators and append-only audit | implemented | `EconomyPayoutAdministrationController`, `ReviewPayoutRequestCommand`, `20260821100000_AddTenantScopedPayoutReview*` and their unit/PostgreSQL migration tests. The tenant comes solely from actor context; the first approval enters `AwaitingSecondApproval`, and a different tenant administrator must make the final decision. |
| Provider-neutral Stripe Connect onboarding, dispatch, signed webhook, timeout ambiguity and reconciliation | missing | Implement an `IConnectPayoutProvider` adapter plus contract tests; retain reservations until terminal provider failure |
| Bounty, Marketplace, AdRewards and Treasury operational flows use durable Core posting workflows | missing | Wire their existing module workflows through authoritative APIs and PostgreSQL gateways |
| Economy administration API has independent permissions for payout review, risk, reserves/custody, reconciliation, ledger health and kill switches | missing | Payout review is implemented at `/api/v1/admin/economy/payout-requests`; add the remaining tenant-derived actor-context APIs and do not add generic debit, credit, conversion or adjustment endpoints. |
| Generated OpenAPI client covers the protected Economy API with a clean diff | missing | Regenerate after API work and verify with `scripts/ci/verify-openapi-client.sh` |
| User wallet/payout UI and payout/risk/reserve/reconciliation/ledger/kill-switch operational consoles | missing | Implement under `apps/web` with explicit disabled, review, challenge, hold and provider-unavailable states |
| Unit, PostgreSQL, provider-contract and E2E suites enforce no skips and at least 98% Economy/Treasury branch coverage | missing | Extend `scripts/ci/economy-projects.json` and the Economy CI Gate as each capability becomes executable |
| Capability activation after legal, KYC/AML, signed policy, certified provider and operational approval | blocked-by-configuration | Defined by `ADR-20260821-economy-production-rollout.md`; the global allowlist starts empty |
| Stripe Connect account credentials, external KYC/AML registrations and WORM storage credentials | blocked-by-configuration | Must be supplied and certified outside source control before activation |

## Evidence gates

- Repository policy: `scripts/ci/verify-repository-policy.sh` writes
  `artifacts/test-results/repository-policy/preflight-summary.txt` for both
  success and failure.
- Economy: `scripts/ci/verify-economy.sh` writes
  `artifacts/test-results/economy/preflight-summary.txt` on every exit path,
  with per-stage logs, TRX, coverage, OpenAPI, Vitest, Playwright and container
  evidence under the same Economy-specific artifact root.
- The release checklist must verify remote branch protection for both required
  gates and must reject any skipped Docker or payout suites.
