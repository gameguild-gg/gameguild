# Commerce Billing / Orders / Subscriptions — Documentation Package

> Modules `GameGuild.Commerce.Billing` + `GameGuild.Commerce.Orders` + `GameGuild.Commerce.Subscriptions`
> (+ `GameGuild.Commerce.Payments`, `GameGuild.Finance.Economy.Marketplace`, `GameGuild.Notifications` as cross-module counterparts).
> Documented against `develop` commit `74e9bba4c`. Follows the merged conventions of `docs/auth/flows/*`.

## Scope — billing-cycle money flows

Subscription purchase → billing period → invoice generation → payment retry / dunning → cancellation (mid-cycle, end-of-cycle); order placement → fulfillment → completion / cancellation / refund; plan change with proration (change saga); trial periods. Everything a subscription invoice rides on, from checkout to bad-debt.

## Contents

| Document | Covers |
|----------|--------|
| [subscription-billing-cycle-flow.md](./subscription-billing-cycle-flow.md) | User flow diagram — purchase, billing period, invoice generation, payment retry/dunning, cancellation, plan change, trials. Edge-case matrix + endpoint inventory. |
| [order-lifecycle-flow.md](./order-lifecycle-flow.md) | User flow diagram — order placement, payment capture, fulfillment, completion, cancellation, hold/release, refund, marketplace cart. Edge-case matrix + endpoint inventory. |
| [cqrs-wiring-matrix.md](./cqrs-wiring-matrix.md) | Every Billing / Orders / Subscriptions controller action: route, command/query, validator, handler, auth, rate limit, unit test, integration test, holes. |
| [state-machines.md](./state-machines.md) | `stateDiagram-v2` for Subscription, Invoice, Order (+ plan-change saga). Transitions audit with triggers at file:line, unreachable states, missing guards. |
| [event-flow-inventory.md](./event-flow-inventory.md) | Billing/order events → outbox → consumers (Payments wallet credit, Notifications dunning, Compliance-visible inbox), retry / DLQ per consumer. |
| [cross-module-sequences.md](./cross-module-sequences.md) | Billing ↔ Payments, Billing ↔ Notifications, Orders ↔ Economy sequence diagrams with timeouts and fail-open/closed verdicts. |
| [gaps.md](./gaps.md) | Consolidated gap register — known vs NEW, each with file:line. |
| [07-stride-threat-model.md](./07-stride-threat-model.md) | STRIDE per flow family (subscription create post-#1229 plan-derived amounts, billing cycle/invoice, dunning, cancellation/pause, plan-change saga, order lifecycle, order refund post-#1229, webhook ingress, FSM integrity) — every threat mitigated by file:line or registered as residual; includes the post-#1229 verification notes (G-1/2/4/5/8/9/10/11/13 fixed; residuals registered). |
| [08-dfds.md](./08-dfds.md) | Level-0 context + level-1 DFDs per flow family with explicit trust boundaries (client → API → provider → webhook ingress → Payments port → Economy → outbox/notifications). |
| [../09-security-architecture.md](../09-security-architecture.md) | Cross-module money-path architecture — component diagram, trust boundaries, who-can-move-money permission map, plan→economy-reversal integrity chain, fail-open/closed verdicts per external dependency. |

## Reading conventions

- All `file:line` references are relative to `apps/api/Source/Modules/...` unless the path starts with `GameGuild.API/` (= `apps/api/Source/GameGuild.API/...`) or `tests/` (= `apps/api/tests/...`).
- Mermaid labels avoid parentheses and colons (render-safe); `L123` means line 123 of the file named in the same node or the preceding text.
- Money invariants cited are the ones enforced by entity code (`Invoice` immutability after issue, single-payment-per-invoice, monotonic order FSM, subscription billing-cycle monotonicity with idempotency keys).

## Companion documents (pre-existing coverage)

- `docs/billing/payment-retry-and-dunning.md` — Payments-side retry ladder and dunning escalation configuration (issue #403/#415). The subscription-side view here complements it.
- `docs/billing/dynamic-tiered-pricing-engine.md`, `docs/billing/product-catalog-bundling.md`, `docs/billing/webhook-raw-payload-retention.md` — adjacent billing surfaces.
