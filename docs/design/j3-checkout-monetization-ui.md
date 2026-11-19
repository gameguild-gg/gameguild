# J3 — Checkout & Monetization UI: Design Proposal

Status: proposed (docs-only; no code in this PR).
Issue: #1298 item J3. Companions: #1290 (promo totals on subscription create, fixed
server-side), #1217 / matrix F2 (promo console raw-fetch break, fix pending),
[Journey 3](../ui/journeys/03-checkout-subscription-promo-payment.md) (the audit that found
the gaps), [subscription billing cycle flow](../commerce/billing-orders/subscription-billing-cycle-flow.md)
(backend authority for everything below).
Audited against `develop` (`7e8e63f82e`). API paths are under
`apps/api/Source/Modules`, web paths under `apps/web/src`, generated client under
`packages/infrastructure/client/src/generated`.

## Summary

The monetization backend is essentially complete: 26 subscription endpoints, public plan
catalog reads, buyer-validatable promo codes, a subscription state machine with trials and
proration, Stripe + economy payment rails, and a full payment-retry/dunning engine. The web
sells exactly one shape of thing — a one-shot digital marketplace order — and hard-blocks
everything else:

- the storefront **404s Subscription-type products** by explicit branch;
- the order pipeline **refuses subscription line items** at complete *and* capture
  (`Orders.SubscriptionAuthorityRequired` — "Subscription products require the authoritative
  subscription checkout", a checkout that does not exist);
- there is **no buyer-facing promo entry** anywhere, while `POST /v1/promo-codes:validate`
  and `:apply` wait with no caller;
- **PastDue/dunning appears nowhere** in `apps/web/src` although the backend moves Active →
  PastDue → Suspended and emails dunning notices;
- the course checkout **hardcodes `paymentMethod: 'test_card'`**, presenting a display-only
  "Total due today".

This proposal adds: a subscription storefront surface (plan picker on Subscription-type
product pages), a dedicated **subscription checkout** outside the one-shot cart, a **promo
code entry** shared by marketplace cart, subscription checkout, and course checkout, a
**billing workspace** (subscriptions list/detail with lifecycle actions and dunning
recovery), and an honest interim state for course checkout. One small backend contract —
the "authoritative subscription checkout" binding — is specified here as a dependency
because the UI cannot ship the pay-then-subscribe leg without it; everything else wires
existing endpoints.

## Current state

### Web monetization surfaces (all of them)

| Surface | Route / component | State |
| --- | --- | --- |
| Marketplace browse | `(public)/marketplace/page.tsx` via generated `CommerceProductsModule` (`lib/marketplace/queries.ts:42/58`) | works |
| Product detail | `(public)/marketplace/[productId]/page.tsx:41` | `if (!product \|\| product.type === 'Physical' \|\| product.type === 'Subscription') notFound();` — subscription products invisible |
| Cart | `MarketplaceCartWorkspace`, generated `getVMarketplaceCart` (`queries.ts:67-68`) | works; one-shot only |
| Checkout | `MarketplaceCheckoutWorkspace` (`components/marketplace/marketplace-checkout-workspace.tsx:12`) | economy settlement or Stripe `PaymentElement` per order; currency-policy toggle lacks `aria-pressed` (`:28-33`) |
| Orders workspace | `(dashboards)/workspace/economy/orders/page.tsx` via `getOrdersForGetOrders()` | works; status badge raw enum |
| Course checkout | `CourseCheckoutButton` → `completeCourseCheckout` (`lib/courses/actions/enrollment.actions.ts:286-352`) | `paymentMethod: 'test_card'` hardcoded (`:326`); `paymentProviderReference` is a fabricated string (`:325`) |
| Promo console (staff) | `(dashboards)/console/economy/promo-campaigns/page.tsx:19-21` | raw fetch to wrong prefix `/api/v1/promo-campaigns` → silent empty list (F2, fix pending on `fix/client-broken-calls`) |
| Subscriptions / billing | — | **no route exists** under `(dashboards)/workspace` (economy has orders, wallet, top-ups, payouts, transfers only) |
| Dunning / PastDue | — | zero references in `apps/web/src` |

### Backend inventory the UI can build on (all shipped)

**Plans (public reads).** `GET /v1/subscription-plans` is `[AllowAnonymous]`
(`SubscriptionPlansCrudController.cs:72`), `GET /v1/subscription-plans/{id}` open, plus
`POST /v1/subscription-plans:compare` and per-plan `GET …/pricing`, `…/usage`,
`…/suggest-upgrades`. Writes carry `MonetizationPermission.Keys.Configure`
(`docs/auth/monetization-permissions.md`). Generated client: `CommerceSubscriptionsModule`
(`packages/infrastructure/client/src/generated/modules/commerce-subscriptions.gen.ts:15`).

**Subscription lifecycle (buyer-callable).** `SubscriptionLifecycleController` is plain
`[Authorize]` (`:26`): `:cancel` (reason/note/effectiveDate, `:96-114`), `:auto-renew`,
`:pause`, `:resume`, `:reactivate`, `:start-trial`, `:end-trial`, `:upgrade`, `:downgrade`.
List/detail are tenant-scoped (`SubscriptionsController.cs:86-140` list defaults to the
actor's tenant; `:186-208` get-by-id returns 404 cross-tenant). Read sides per subscription:
`/billing-history`, `/invoices`, `/usage`, `/breakdown`, and `POST …/pricing:preview`
(upgrade/downgrade proration quote). Admin-gated: PATCH/PUT/DELETE + plan management.

**Creation and the economic invariant.** `POST /v1/subscriptions`
(`SubscriptionsController.cs:36-77`) takes tenant/plan/cycle/amount/currency, optional
`FulfilledOrderId`, `TrialDays`, `PromoCodes` + `PromoCodeIdempotencyKey`, and creation-time
fraud signals. `Subscription.FulfilledOrderId` documents the invariant *"new subscriptions
MUST have a FulfilledOrderId — no subscription without payment proof"*
(`Entities/Subscription.cs:79-115`); omitting it logs a "legacy or admin path" warning.
The state machine starts at `PendingActivation` (or `Trialing`), and the **first confirmed
cycle payment activates** it via `PaymentSubscriptionSyncService.SyncSuccessfulPaymentAsync`
→ `Subscription.RecordPayment` (amount/currency guards, cycle monotonicity, idempotency
`payment:{paymentId}`) → invoice materialization.

**The hard block.** The one-shot order pipeline refuses subscription lines at both payment
exits: `CompleteOrderCommandHandler.cs:103-109` and `CaptureOrderCommandHandler.cs:63-68`
return `Orders.SubscriptionAuthorityRequired` when any `OrderLineItem.IsSubscription` — and
the marketplace cart flags exactly that (`MarketplaceCartCommands.cs:184`). This is
deliberate: the "authoritative subscription checkout" must bind order payment to
subscription creation. **That binding is the one missing backend contract** (below).

**Promo engine (buyer-validatable).** `POST /v1/promo-codes:validate` (code, orderAmount,
productId, planId, captchaToken) → `PromoCodeValidationResult` (`IsValid`, `DiscountAmount`,
`DiscountPercentage`, `TrialExtensionDays`, `FreeMonthsGranted`, `PromoCodeId`) and
`POST /v1/promo-codes:apply` (idempotent on key, optional orderId/planId) →
`PromoCodeApplicationResult` (`OriginalAmount`, `FinalAmount`, `TotalDiscount`,
`AppliedCodes`, `RejectedCodes`, `StackingRuleName`) —
`PromoCodesController.cs:327-372`, `Commerce.Products/Models/PromoCodeModels.cs:79-109`.
Subscription create quotes promo codes fail-closed (all-rejected ⇒ creation fails with
per-code reasons; valid codes can extend trials / grant free months / skip periods —
`CreateSubscriptionCommandHandler.cs:54-108`). #1290's totals bug is already fixed
server-side.

**Payments, refunds, dunning.** Stripe intents per order (`POST /v1/orders/{id}:payment-intent`,
`:capture`, `:complete`, `:refund`), the Payments retry queue with failure-code ladders and
jitter (`docs/billing/payment-retry-and-dunning.md`), payment-method-change pause/resume,
retry-attempt history (`GET /v1/payments/{id}:retry-attempts`), dunning emails at High then
Urgent priority, plan-change saga with daily-rate proration
(`docs/commerce/billing-orders/subscription-billing-cycle-flow.md`), and
`GET /v1/billing/subscriptions/{id}/plan-change:saga` status.

**Entitlements.** Order completion grants per-product entitlements
(`CompleteOrderCommandHandler.cs:146-161`); `GET /v1/user-entitlements` /
`EntitlementsController` exist for gating content.

## Proposed design

### D1. Subscription storefront — plan picker on Subscription-type product pages

Remove the `product.type === 'Subscription'` clause from the 404 branch
(`[productId]/page.tsx:41`); Physical stays 404. For `type === 'Subscription'` render a
**plan picker** server component:

- plans from the product's linked plans (`Product.SubscriptionPlans`,
  `Commerce.Products/Entities/Product.cs:121-123`) joined with
  `GET /v1/subscription-plans?productId=…` for interval prices
  (`SubscriptionPlanIntervalPrice` — Monthly/Quarterly/SemiAnnually/Annually/Biannually);
- billing-cycle segmented control; per-cycle price via the plan `GET …/pricing` endpoint;
- CTA **Subscribe** → `/marketplace/checkout/subscription?productId=…&planId=…&cycle=…`;
- feature comparison affordance via `POST /v1/subscription-plans:compare` when the product
  has ≥2 plans (progressive: details link, not a wall of tables);
- if the buyer already holds a subscription for the plan family, the CTA becomes
  **Manage** → `/workspace/economy/subscriptions/{id}` (list endpoint
  `getSubscriptionsForGetSubscriptions({ planId })`).

### D2. Subscription checkout — dedicated flow, not the cart

The one-shot cart stays untouched (server refuses subscription lines there anyway). New
route `(public)/marketplace/checkout/subscription` (auth-gated like the existing checkout —
cart actions already return "Sign in before…" `lib/marketplace/actions.ts:55`):

1. **Review** — plan, cycle, interval price, proration/trial banner if
   `TrialDays` applies; price display through the existing
   `CurrencyPriceDisplay` transparency component (issue #412 spec E).
2. **Promo** — shared promo entry (D3) calling `promo-codes:validate` with
   `planId` + order amount; show discount, `TrialExtensionDays`, `FreeMonthsGranted`
   explicitly — subscription promos are not always price discounts.
3. **Payment** — same two rails as the marketplace checkout:
   - **economy wallet**: settle the cycle-1 order via the order pipeline, then create the
     subscription against the fulfilled order;
   - **Stripe**: `POST /v1/orders/{id}:payment-intent` → `StripePaymentElement` mount →
     `stripe.confirmPayment` with `return_url` to a subscription-confirmation page →
     server-side reconcile via capture, then subscription create.
   The currency-policy toggle (Hard/Soft/FixedMix) is reused *only* for the economy rail,
   and gains `aria-pressed` (fixes the journey-3 a11y gap in passing,
   `marketplace-checkout-workspace.tsx:28-33`).
4. **Subscription create** — `POST /v1/subscriptions` with `FulfilledOrderId` = the
   fulfilled order, `PlanId`, `BillingCycle`, `Amount`/`Currency` from the plan interval
   price (never client-trusted — validated server-side against the plan),
   `PromoCodes` + `PromoCodeIdempotencyKey` (same key the `:apply` call used), fraud
   headers (`X-Device-Fingerprint`, `X-Client-Timezone`, payer email/card BIN as the
   marketplace flow supplies them, `SubscriptionsController.cs:50-74`).
5. **Confirmation** — subscription id, status (`Trialing`/`Active`), next billing date,
   invoice link; deep link into the billing workspace.

**Idempotency** — one client-generated key (`crypto.randomUUID()`, the house pattern at
`marketplace-checkout-workspace.tsx:44/52`) threads the whole attempt: promo apply → order
payment → subscription create; replays of `POST /v1/subscriptions` with the same
`PromoCodeIdempotencyKey` and a re-derived order are caught server-side by the promo ledger
and `RecordPayment` idempotency.

### D2a. The missing backend binding (dependency, specified for a follow-up issue)

The UI cannot perform step 3→4 atomically today: `complete`/`capture` refuse subscription
lines, and a subscription-first create without payment violates the `FulfilledOrderId`
invariant. Proposed contract (small, additive):

```
POST /v1/subscriptions:checkout
{
  planId, billingCycle, productId?, promoCodes[], promoCodeIdempotencyKey,
  paymentMethod: 'stripe' | 'economy_wallet', currencyPolicy?,
  idempotencyKey, fraud: { country?, payerEmail?, cardBin? }
}
→ 201 { orderId, subscriptionId?, clientSecret?, status: 'payment_required' | 'active' | 'trialing' }
```

Semantics: creates the order (subscription-typed lines allowed for **this** authority only),
drives the chosen rail, and on confirmed payment creates the subscription with
`FulfilledOrderId` set — the authoritative binding the order handlers demand. Server
reuses `CreateSubscriptionCommand`, `OrderPaymentIntentService`, and
`PaymentSubscriptionSyncService` unchanged; the refusal guards in
`CompleteOrder/CaptureOrder` stay for every other caller. Until this lands, D2 ships
dark-launched behind a feature flag with the **economy rail only** replaced by a
"subscriptions available soon" state (see rollout).

### D3. Shared promo entry (three surfaces, one component)

A `PromoCodeEntry` client component used at: marketplace cart (pre-checkout preview),
subscription checkout step 2, and course checkout dialog.

- **Validate-as-you-go**: `POST /v1/promo-codes:validate` on explicit Apply (not per
  keystroke — the endpoint rate-limits and records attempts as a campaign conversion
  denominator, `PromoCodeModels.cs:212-217`); render one of: applied (amount/percent),
  trial-extension, free-months, or the server's per-code rejection copy verbatim
  (`ErrorMessage`).
- **Apply at commit**: cart checkout passes codes into the cart-checkout/economy settlement
  (order line `PromoCodesApplied` already exists, `OrderLineItem.cs:126`); subscription
  checkout passes them to `:checkout`/`POST /v1/subscriptions`; the component never calls
  `:apply` itself except where an order id already exists (apply-with-orderId path).
- **Rejections are terminal per attempt**: a rejected code stays visible with its reason
  until edited; stacking outcome (`StackingRuleName`, `RejectedCodes`) renders after
  commit from the application result.
- a11y: status communicated via `aria-live` polite; success/failure by text + icon, not
  color alone; the input is a labeled text field, not an OTP widget.

### D4. Billing workspace — subscriptions list, detail, and dunning recovery

New `(dashboards)/workspace/economy/subscriptions` (alongside orders/wallet/top-ups):

- **List** — `getSubscriptionsForGetSubscriptions()` filtered to the actor tenant; columns:
  plan, status badge, next billing date, auto-renew indicator; `expiring=true` quick filter
  maps to the server's expiring query (`SubscriptionsController.cs:135-137`).
- **Detail** — status, period window (`CurrentPeriodStart/End`, `NextBillingDate`),
  amount + cycle, payment method (external ids when present), invoices
  (`GET …/invoices`), billing history, usage; actions:
  - `:auto-renew` toggle (persisted optimistic with server truth on failure — the
    `expectedVersion` house pattern from cart edits);
  - **Cancel** (`:cancel`) with reason picker + "end of period" vs "immediate"
    (`EffectiveDate`) explained in copy — backend treats both as terminal Cancelled with
    `EndDate` honored;
  - **Upgrade/Downgrade** via `pricing:preview` first (proration quote shown pre-confirm),
    then `:upgrade`/`:downgrade`; saga-based changes surface the
    `plan-change:saga` status while pending;
  - **Pause/Resume** where the plan allows.
- **Dunning recovery state (the PastDue leg)** — status `PastDue` or `Suspended(reason =
  RetryExhausted)` renders an **action panel**, not a badge: "Your last payment failed on
  {date}: {reason}. Retry now or update your card." Retry re-runs the failed payment (the
  backend's recovery path reactivates on success, guarded by the recorded suspension
  reason); card update follows the payment-method-change flow, which pauses scheduled
  retries while a new instrument is confirmed
  (`PaymentRetryMethodChangeService`, `docs/billing/payment-retry-and-dunning.md`).
  The panel states the next scheduled retry window when the queue has one pending
  (`GET /v1/payments/{id}:retry-attempts` for history).
- **Status vocabulary** — every `SubscriptionStatus` value gets human copy + badge variant;
  `Expired`/`Suspended` distinctions are explained in a tooltip (the state machine is the
  authority, `Entities/Subscription.cs:26-42`).

### D5. Course checkout honesty (interim)

Until courses move to real products: keep the dialog but replace the fabricated
`paymentMethod: 'test_card'` + invented `paymentProviderReference`
(`enrollment.actions.ts:325-326`) with an explicit **free/enrollment-grant** label when the
backend indeed charges nothing, or wire the same D2 flow when the course product is paid.
The "Total due today" line must not render unless an amount was really charged. Promo entry
attaches once course products are real products.

## Edge cases

| # | Case | Handling |
| --- | --- | --- |
| 1 | Payment succeeds, subscription create fails (e.g. fraud Block, `SubscriptionFraudBlockedException`) | order fulfilled + paid but no subscription — D2a contract must make create-then-pay ordering or compensate (refund via `POST /v1/orders/{id}:refund`); UI shows "payment received, subscription pending — support" state; saga journal preferred |
| 2 | Promo valid at validate, rejected at apply (race/limits) | fail-closed per backend; UI re-shows cart at undiscounted price with the rejection reason; no silent drop (`CreateSubscriptionCommandHandler.cs:73-81` semantics preserved) |
| 3 | Double-click Subscribe | idempotency key threading (D2 step 4); `POST /v1/subscriptions` replay is not order-idempotent today — D2a must make the composite endpoint idempotent on `idempotencyKey`; UI also disables during flight |
| 4 | PastDue at renewal while user is mid-purchase elsewhere | dunning panel (D4) is the single recovery surface; purchase flows reject when tenant already holds an active/pastdue subscription for the same plan family (list check in D1) |
| 5 | Upgrade mid-trial | `pricing:preview` before confirm; trial conversion semantics explained in the confirm copy (`EndTrial(true)` converts, `Entities/Subscription.cs:344-360`) |
| 6 | Refund of a subscription payment | backend suspends with `RefundIssued` (full refund) — billing workspace reflects Suspended with reason copy; no UI refund initiation for buyers |
| 7 | Stripe unconfigured | reuse the disabled-button + explanation pattern (`marketplace-checkout-workspace.tsx:59`); economy rail remains |
| 8 | Session expiry mid-checkout | same degradation as cart today: "Sign in before checkout." alert, no redirect loop |
| 9 | Multi-order partial failure (cart + economy) | existing behavior; promo entry adds nothing — codes applied per order at commit; the journey-3 "unreported completed orders" note stands as a separate fix |
| 10 | Currency mismatch plan vs wallet policy | plan currency is authoritative (`Money` guards in `RecordPayment`); UI displays the plan currency and does not offer incompatible rails |

## i18n and accessibility

- All new copy under the existing next-intl namespaces (`marketplace.*` today; add
  `marketplace.checkout.subscription.*`, `workspace.billing.*`) in
  `apps/web/src/i18n/messages/{locale}.json`; server components use `getTranslations`
  exactly as `[productId]/page.tsx:39` does.
- Money and dates always through the existing `CurrencyPriceDisplay` and
  `formatEconomyDate` helpers — no raw number formatting in components.
- Segmented controls (cycle picker, currency policy) get `role="radiogroup"`/`aria-pressed`
  (fixing the known gap); status badges carry text, never color alone; promo outcomes in
  `aria-live` polite; focus management in dialogs by Radix (house pattern,
  `course-checkout-button.tsx:133-152`); loading states disable submit + spinner with
  visible text (house pattern).

## Data contracts summary (web ⇄ API)

| UI need | Endpoint (existing) | Generated client |
| --- | --- | --- |
| Plan catalog + interval prices | `GET /v1/subscription-plans`, `GET …/{id}/pricing` | `CommerceSubscriptionsModule.getSubscriptionPlansFor…` |
| Plan comparison | `POST /v1/subscription-plans:compare` | same module |
| Promo preview | `POST /v1/promo-codes:validate` | `CommerceProductsPromoCampaignsModule`/promo module |
| Promo commit (with order) | `POST /v1/promo-codes:apply` | same |
| My subscriptions | `GET /v1/subscriptions` (tenant-scoped default) | `getSubscriptionsForGetSubscriptions` |
| Subscription detail / invoices / history / usage | `GET /v1/subscriptions/{id}`(+`/invoices`,`/billing-history`,`/usage`) | same module |
| Lifecycle actions | `POST /v1/subscriptions/{id}:cancel/:auto-renew/:pause/:resume/:reactivate/:upgrade/:downgrade` | same module |
| Proration quote | `POST /v1/subscriptions/{id}/pricing:preview` | same module |
| Payment rails | `POST /v1/orders/{id}:payment-intent` / `:capture` / `:complete` / `:refund` | `CommerceOrdersModule` |
| Subscription checkout composite | `POST /v1/subscriptions:checkout` **(proposed, D2a)** | to be generated after API lands |
| Retry history | `GET /v1/payments/{id}:retry-attempts` | payments module |

## Test plan

- **Component**: plan picker (cycles, trial banner, manage-vs-subscribe CTA), promo entry
  (applied/discount/trial/free-months/rejected/stacking outcomes), dunning panel copy per
  status × reason, cancel dialog (end-of-period vs immediate).
- **E2E (Playwright)**: subscribe happy path (Stripe test card + economy), promo applied →
  discounted create, promo rejected → fail-closed copy, PastDue → retry → Active, cancel →
  list reflects terminal state, plan upgrade preview → confirm → saga complete, unconfigured
  Stripe disables rail, sign-in expiry mid-flow.
- **Contract**: every new call goes through the generated client (the journey-3 F3/F4
  lesson) — no raw `fetch` in new code; the api-contract-matrix gains rows for each new
  caller (F8 goes to zero as endpoints light up).
- **A11y**: radiogroup/aria-pressed semantics, aria-live promo results, keyboard path
  through checkout steps, badge text alternatives.
- **Backend (follow-up issue)**: `:checkout` idempotency, fail-closed promo, fraud-gate
  outcomes (Block/Review), payment-then-create failure compensation.

## Rollout plan

1. **Foundation (web, no backend dep)**: promo entry component + cart validation preview;
   billing workspace read-only (list/detail/invoices); orders workspace unchanged.
2. **Storefront**: Subscription-type product pages render the plan picker; subscribe CTA
   behind a runtime flag until step 3.
3. **Backend dependency**: `POST /v1/subscriptions:checkout` (D2a) in a dedicated PR with
   its own tests; contract-first (this doc + OpenAPI regeneration through the real
   pipeline — never hand-spliced).
4. **Checkout GA**: flag on; course checkout honesty fix rides along or follows
   immediately.
5. **Dunning recovery UI**: once retry-queue telemetry exists in a pilot env; the panel is
   additive (read + retry actions), so it can precede or follow GA.

Each step is independently mergeable and none changes existing cart behavior.

## Open questions for the owner

1. **Composite vs choreographed checkout** — one `:checkout` endpoint (D2a, recommended:
  single idempotency surface, atomic binding) or web-driven sequence
  (order → pay → create) with a relaxed complete-guard when a subscription intent is
  declared? The second spreads failure handling into the UI.
2. **Plan-family exclusivity** — one active subscription per plan family per tenant, or
  allow stacking (list-check in D1 assumes exclusivity)?
3. **Trials without cards** — allow `TrialDays` creates with payment captured at conversion
  (provider-billed posture) or require an instrument up front (first-party rail)?
4. **Buyer-initiated refunds** — out of scope here (support-only today); confirm that
   stance for the billing workspace copy.
5. **Course products** — migrate course checkout onto real product/pricing rows (unblocks
   promo + real charges), or keep the enrollment-grant path and label it honestly (D5
   interim)? Sequencing affects #1290's "UI side".
6. **Promo on renewals** — codes apply at create and are locked into the period; should the
   billing workspace surface "your promo ends with this term" messaging (read-only today)?
