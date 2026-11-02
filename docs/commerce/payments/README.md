# GameGuild.Commerce.Payments — Documentation Package

> Module `GameGuild.Commerce.Payments` (+ Stripe ingress in `GameGuild.Commerce.Billing` and Economy top-up consumer in `GameGuild.Finance.Economy`) · Documented against `develop` commit `74e9bba4c`.
> Scope: money-critical payment surface — payment initiation through the gateway layer (Stripe primary; PayPal, Adyen, Braintree, Authorize.Net registered), refunds (full/partial/prorated with approval workflow), fraud gate (signals, thresholds, manual review), chargeback/dispute management, currency suggestion, payment-method lifecycle, wallet/tax/pricing/FX surfaces owned by the module, and the Stripe webhook ingress that reconciles provider truth into the local aggregates.

## Document inventory

| File | Contents |
|------|----------|
| [`01-user-flows.md`](01-user-flows.md) | Mermaid `flowchart TD` user flows — payment initiation to Stripe success/failure, refund flow with all legs, fraud assessment incl. `GetFraudStatistics`, chargeback/dispute, currency suggestion, payment-method lifecycle — each with an edge-case matrix and file:line per node |
| [`02-cqrs-wiring-matrix.md`](02-cqrs-wiring-matrix.md) | Route → command/query → validator → handler → auth → rate limit → unit test → integration test wiring matrix for every controller in the module, `HOLE?` column for missing links |
| [`03-state-machines.md`](03-state-machines.md) | `stateDiagram-v2` for the `Payment` aggregate, fraud review states, refund states, dispute states; transitions audit with triggers, file:line, unreachable states and missing guards |
| [`04-event-flow-inventory.md`](04-event-flow-inventory.md) | Payment events → outbox/inbox → consumers (Economy, Notifications, Compliance.Audit), Stripe webhook ingress path (signature → idempotency → routing), retry/DLQ posture per consumer |
| [`05-cross-module-sequences.md`](05-cross-module-sequences.md) | `sequenceDiagram` cross-module interactions — Payments↔Economy, Payments↔Notifications, Payments↔Compliance, Payments↔Stripe (API + webhooks, timeouts, fail-open/fail-closed verdicts) |
| [`06-gaps.md`](06-gaps.md) | Gaps with file:line, marked KNOWN (already tracked) vs NEW (found by this audit) |

## Conventions

- Follows the merged auth-module doc conventions (`docs/auth/flows/*`): file:line on every node, `L###` shorthand inside Mermaid labels, edge-case matrices as tables, honest gaps section.
- Mermaid labels contain no parentheses and no colons (repo rendering constraint); route custom actions are spelled `id-colon-action` when a file:line is not enough.
- `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`, `Economy:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/` inside citation shorthand.
- Tests live in `apps/api/tests/GameGuild.Commerce.Payments.UnitTests/` (126 files) and `apps/api/tests/GameGuild.Commerce.Payments.IntegrationTests/`; webhook integration tests live in `apps/api/tests/GameGuild.Commerce.Billing.IntegrationTests/`.

## Module shape at a glance

- **Controllers (22)**: Payments, Refunds, Disputes, PaymentMethods (route `billing/payment-methods`), BillingCharges, Currencies, CurrencyConversion, FxRates, Pricing, PriceLocalization, Taxes, TaxJurisdictions, TaxRules, Wallets, Gifts, Referrals, AccountCredits, FraudStatistics, FraudBlacklist, FraudReviews, FraudDevices, RevenueAuditing (`Payments:/Controllers/`).
- **Aggregate entities**: `Payment`, `Refund`, `PaymentDispute`, `DisputeEvidence`, `PaymentMethod`, `PaymentRetryAttempt`, `FinancialLedgerEntry`, `RevenueEvent`, `AuditTrail`, `UserWallet`, `WalletTransaction`, `FraudBlacklistEntry`, `CurrencyConfiguration`, `FxRateSnapshot` … (`Payments:/Entities/`).
- **Hosted services (9)**: refund workflow, dispute deadline reminder, revenue anomaly detection, fraud background re-scoring, payment retry queue, payment-method expiry monitor, wallet balance alert monitor, account-credit expiry, FX rate sync (`Payments:/PaymentsModule.cs:70,79,99,147,163,271,280,348`).
- **Gateway layer**: `IPaymentGateway` with Stripe primary (`Payments:/Services/StripePaymentGateway.cs:11`), failover wrapper (`Payments:/Services/FailoverPaymentGateway.cs`), circuit breaker (`Payments:/CircuitBreaking/InMemoryGatewayCircuitBreaker.cs`), credential store with DataProtection encryption (`Payments:/Services/GatewayCredentialStore.cs`).
- **Stripe webhook ingress** lives in the sibling Billing module (`Billing:/Controllers/BillingWebhooksController.cs:186` → `Billing:/Services/StripeBillingWebhookService.cs:70`) and feeds Payments through the verified-event consumer extension point (`IStripeVerifiedEventConsumer`).
