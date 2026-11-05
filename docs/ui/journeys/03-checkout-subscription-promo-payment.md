# Journey 3 — Checkout: browse product, pay, and where subscription/promo would live

Part of the [UI journeys pack](./README.md). Audited against `develop` (`630eb8c6d7`).

Reality check up front, because it shapes this whole page: **the web app has no
subscription-creation UI and no promo-code entry for buyers.** The buy path that
exists is the marketplace cart → economy/Stripe checkout, plus a course-enrollment
checkout. Promo campaigns appear only in an admin console, and the subscription
endpoints (26 of them) have zero web callers (matrix F8). This journey documents
the real click-paths and marks the absent legs as gaps.

## Entry points

| Entry | Route / component | file:line |
| --- | --- | --- |
| Marketplace browse | `/[locale]/marketplace` | `apps/web/src/app/[locale]/(public)/marketplace/page.tsx` |
| Product detail | `/[locale]/marketplace/[productId]` | `apps/web/src/app/[locale]/(public)/marketplace/[productId]/page.tsx:36` |
| Add to cart | `AddToCartForm` | `apps/web/src/components/marketplace/add-to-cart-form.tsx:10` |
| Cart | `/[locale]/marketplace/cart` | `apps/web/src/app/[locale]/(public)/marketplace/cart/page.tsx:9` |
| Checkout | `/[locale]/marketplace/checkout` | `apps/web/src/app/[locale]/(public)/marketplace/checkout/page.tsx:5` |
| Checkout workspace | `MarketplaceCheckoutWorkspace` | `apps/web/src/components/marketplace/marketplace-checkout-workspace.tsx:12` |
| Stripe element | `StripePaymentElement` | `apps/web/src/components/marketplace/stripe-payment-element.tsx:48` |
| Course enrollment checkout | `CourseCheckoutButton` | `apps/web/src/components/courses/course/course-checkout-button.tsx:44` |
| Promo console (admin) | `/[locale]/console/economy/promo-campaigns` | `apps/web/src/app/[locale]/(dashboards)/console/economy/promo-campaigns/page.tsx:25` |

## Step-by-step click-path

### 1. Browse — `/marketplace`

Server page lists products through the generated `CommerceProductsModule`
(`apps/web/src/lib/marketplace/queries.ts:42/58` — `getProducts`/product lookups).
Price transparency renders the listing price plus an approximate converted amount
(issue #412 spec E, `components/currency/currency-price-display.tsx`).

### 2. Product detail → add to cart

The product page **404s Subscription- and Physical-type products**
(`[productId]/page.tsx:37-38`) — the storefront deliberately sells only digital
types. `AddToCartForm` (`add-to-cart-form.tsx:10`) shows quantity (1–100) and
Add; the button is disabled with an "unavailable" label when no published price
version exists (`add-to-cart-form.tsx:19/30`). Submit → server action
`addMarketplaceCartItemAction` (`apps/web/src/lib/marketplace/actions.ts:41-60`)
→ generated `postVMarketplaceCartItems('1', …)` (`actions.ts:56`) with a
`crypto.randomUUID()` idempotency key. Signed-out users get
"Sign in before adding items to the cart." (`actions.ts:55`) via the
`MarketplaceActionNotice` alert (`components/marketplace/marketplace-action-notice.tsx:8-16`).

### 3. Cart → checkout

Cart page hydrates the durable cart (`getMarketplaceCart` → generated
`getVMarketplaceCart('1')`, `queries.ts:67-68`) plus per-item product lookups;
`MarketplaceCartWorkspace` handles quantity updates
(`patchVMarketplaceCartItems`, `actions.ts:69`) and removals
(`deleteVMarketplaceCartItems`, `actions.ts:81`), both optimistic-version guarded
(`expectedVersion`). A checkout link appears only with items
(`cart/page.tsx:33-37`).

### 4. Payment — economy wallet or Stripe

`MarketplaceCheckoutWorkspace` (`marketplace-checkout-workspace.tsx:12`) offers:

- **Pay with economy** (`:43-46`) — `checkoutMarketplaceEconomyAction`
  (`actions.ts:87-111`): generated `postVMarketplaceCartCheckout('1', …)` then, per
  order, `postOrdersComplete` with a `marketplaceSettlement` carrying the chosen
  currency policy (`Hard` / `Soft` / `FixedMix`, `:28-35`) and a derived
  idempotency key. Failure mid-loop returns "Order … requires review."
- **Pay with Stripe** (`:48-62`) — `prepareMarketplaceStripeCheckoutAction`
  (`actions.ts:113-137`): the same cart checkout, then per order
  `postOrdersPaymentIntent` (`:126`) to obtain `clientSecret`s. Each secret mounts
  a `StripePaymentElement`; confirming runs `stripe.confirmPayment` with
  `redirect: 'if_required'` and `return_url` to the order page
  (`stripe-payment-element.tsx:22-26`), then reconciles server-side via
  `reconcileMarketplaceStripeOrderAction` → generated `postOrdersCapture`
  (`actions.ts:139-155`). Fulfillment stays webhook-authoritative (message copy at
  `actions.ts:151-154`).
- Without `NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY` the Stripe button is disabled with
  an explanatory line (`marketplace-checkout-workspace.tsx:59`,
  `stripeBlocked` copy).

### 5. Course enrollment checkout (the other buy path)

From a public course page, `CourseCheckoutButton` opens a dialog with product
choice (radio group when multiple), price summary and a destructive error alert
(`course-checkout-button.tsx:104-207`). Confirm → `completeCourseCheckout`
(`apps/web/src/lib/courses/actions/enrollment.actions.ts:286-352`) → raw
`client.request POST /v1/courses/{id}/checkout/complete` (`:320-327`) with
`paymentMethod: 'test_card'` **hardcoded** (`:326`). Success routes into the
learner app; the destructive alert shows the formatted API error otherwise
(`course-checkout-button.tsx:181-187`).

### 6. Where promo and subscription actually appear

- **Promo** — the only promo surface is the staff console
  `/console/economy/promo-campaigns` (`promo-campaigns/page.tsx:25`), which loads
  campaigns via a raw fetch to `/api/v1/promo-campaigns` (`:19`). The API serves
  `/v1/promo-campaigns`, so on `develop` this always fails and
  `if (!response.ok) return [];` (`:21`) renders an **empty dashboard** — matrix
  finding **F2**, issue #1217. The pending `fix/client-broken-calls` PR replaces
  the loader with the generated `CommerceProductsPromoCampaignsModule` and adds
  the destructive-alert fallback the convention requires.
- **Subscription** — absent. `commerce-subscriptions`'s 26 endpoints have no web
  caller (matrix F8); the storefront explicitly filters Subscription products out
  (`[productId]/page.tsx:37-38`); no checkout surface posts to a
  subscriptions-create endpoint.
- **Dunning / PastDue** — there is no `PastDue` string anywhere in `apps/web/src`
  and no payment-retry/healing UI; the Stripe reconcile message distinguishes
  only `Succeeded` vs pending reconciliation (`actions.ts:150-154`).

## Error states shown by the UI

| State | Component | Where | Shape |
| --- | --- | --- | --- |
| Not signed in (any cart action) | `MarketplaceActionNotice` | `actions.ts:55/68/81/93` etc. | destructive `Alert` with the action message |
| Cart/version conflict | same | `actions.ts:70` | destructive alert "The cart changed elsewhere. Refresh and try again." |
| Quantity invalid | client | `actions.ts:51-53` | destructive alert with the rule |
| Stripe confirm failed | `StripePaymentElement` | `stripe-payment-element.tsx:27-29` | destructive alert with Stripe's message |
| Capture/reconcile failed | same | `actions.ts:146-147` | destructive alert "requires reconciliation" |
| Stripe unconfigured | checkout workspace | `marketplace-checkout-workspace.tsx:59` | disabled button + muted explanation |
| Course checkout failure | `CourseCheckoutButton` | `course-checkout-button.tsx:181-187` | destructive `Alert` with `formatApiError` output (`[status] detail`) |
| Course not enrollable | same | `enrollment.actions.ts:307/313` | same alert shape |
| Loading | both | `marketplace-checkout-workspace.tsx:43/58`, `course-checkout-button.tsx:194-206` | disabled buttons; spinner + "Completing checkout..." |
| Empty cart | checkout | `marketplace-checkout-workspace.tsx:20` | muted empty-cart copy |
| Promo console load failure | console page | `promo-campaigns/page.tsx:21` | **silent empty list** (the pre-#1217 anti-pattern; fix pending) |

## Accessibility notes

- Currency choice buttons expose their state only via `variant` styling
  (`marketplace-checkout-workspace.tsx:28-33`) — no `aria-pressed`, so assistive
  tech hears three identical buttons. (Gap-noted below.)
- The cart quantity input is a labeled number input with min/max
  (`add-to-cart-form.tsx:26`); errors arrive after submit as alerts.
- `CourseCheckoutButton`: native radio group for product choice
  (`course-checkout-button.tsx:133-152`), dialog focus management by Radix, the
  spinner icon is decorative alongside visible "Completing checkout..." text.
  When no product exists the trigger renders a disabled "Checkout unavailable"
  button (`:80-86`) — state is conveyed in text, not color alone.
- Stripe's `PaymentElement` renders inside a labeled form with a real submit
  button (`stripe-payment-element.tsx:37-40`); Stripe's own iframe a11y applies.
- Result alerts use the design-system `Alert` roles; success and failure are
  distinguished by variant and title text, not color alone.

## Edge cases

- **Offline** — every action result renders as a destructive alert; no optimistic
  cart state, so a failed update leaves server truth visible after
  `revalidatePath`.
- **Session expiry mid-checkout** — cart actions re-read the session per action
  (`actions.ts:17-19`); expiry degrades to "Sign in before checkout." alerts rather
  than a redirect loop. The Stripe path pins `return_url` to the order page so a
  redirect-based 3DS return lands somewhere meaningful.
- **Double submit** — idempotency keys are generated client-side per attempt
  (`crypto.randomUUID()`, `marketplace-checkout-workspace.tsx:44/52`), so a
  retried click is a *new* attempt, not a replay; server-side expectedVersion on
  the cart is the actual double-checkout guard.
- **Partial multi-order failure** — cart checkout can return several orders; the
  economy path stops at the first failed settlement and reports that order id
  (`actions.ts:106`); earlier orders in the loop are already completed (no
  client-side rollback).
- **MFA interrupt** — checkout never step-ups; economy console actions elsewhere
  use step-up challenges (`lib/economy/console-actions.ts`), but the marketplace
  checkout does not.

## Gaps

**Known:**

- **F2 / #1217 (pending PR)** — promo console fetches `/api/v1/promo-campaigns`,
  silently rendering empty on failure (`promo-campaigns/page.tsx:19-21`). The fix
  on `fix/client-broken-calls` routes through the generated module and adds the
  destructive-alert fallback.
- **#1290 (open, backend)** — promo totals 500→422 on subscription creation was
  fixed server-side (commit `950524675a`); its "UI side" is exactly this console
  page plus the absent buyer promo surface below.
- **F8 (matrix)** — `commerce-subscriptions` 26 endpoints, 0 web callers.

**NEW:**

- **No buyer-facing promo entry.** No cart, checkout or course-checkout surface
  accepts a promo code; the promo engine is only operable from the console. A
  user who receives a campaign code has nowhere to type it.
- **No subscription purchase path at all.** The journey's "subscription create
  with promo" leg does not exist: storefront filters Subscription products
  (`[productId]/page.tsx:37-38`), checkout only settles one-shot orders, and
  nothing links the 26 subscription endpoints to a component.
- **No dunning / PastDue healing UX.** Zero `PastDue` references in `apps/web/src`;
  a failed renewal surfaces nowhere — the user's only signal would be a
  subsequently failing checkout or an order stuck in the workspace orders list.
- **Course checkout hardcodes `paymentMethod: 'test_card'`**
  (`enrollment.actions.ts:326`) — the dialog presents a payment summary but no
  payment occurs; "Total due today" is display-only. Ship risk if mistaken for a
  real charge path.
- **Currency-policy toggle lacks `aria-pressed`** (`marketplace-checkout-workspace.tsx:28-33`).
- **Multi-order partial failure leaves completed orders unreported** — the notice
  shows only the failing order; completed ones are visible only after navigating
  to the orders workspace.

## Flow diagram

```mermaid
flowchart TD
    A["User browses slash marketplace"] --> B[["getProducts via CommerceProductsModule"]]
    B --> C["User opens a digital product page"]
    C --> D{"Product type is Subscription or Physical"}
    D -- "yes" --> E["404 notFound"]
    D -- "no" --> F["AddToCartForm with quantity"]
    F --> G[["postVMarketplaceCartItems with idempotency key"]]
    G -- "not signed in" --> H["Alert sign in before adding items"]
    G -- "ok" --> I["Cart page with update and remove"]
    I --> J[["patchVMarketplaceCartItems or deleteVMarketplaceCartItems with expectedVersion"]]
    I --> K["User opens checkout"]
    K --> L{"Payment choice"}
    L -- "economy" --> M[["postVMarketplaceCartCheckout then postOrdersComplete per order"]]
    M -- "failure" --> N["Alert order requires review"]
    M -- "ok" --> O["Checkout submitted to economy workflow"]
    L -- "stripe" --> P[["postVMarketplaceCartCheckout then postOrdersPaymentIntent per order"]]
    P --> Q["StripePaymentElement mounts with clientSecret"]
    Q --> R["User confirms payment"]
    R --> S["stripe.confirmPayment with return_url to order"]
    S -- "stripe error" --> T["Alert with stripe message"]
    S -- "ok" --> U[["postOrdersCapture reconcile via reconcileMarketplaceStripeOrderAction"]]
    U --> V["Alert succeeded or pending reconciliation"]
    W["Learner clicks Enroll on a course page"] --> X["CourseCheckoutButton dialog"]
    X --> Y[["POST /v1/courses id checkout complete with hardcoded test_card"]]
    Y -- "fail" --> Z["Destructive Alert with formatted API error"]
    Y -- "ok" --> AA["Redirect into learner content"]
    AB["Staff opens console promo-campaigns"] --> AC[["Raw fetch to /api/v1/promo-campaigns wrong prefix F2"]]
    AC --> AD["Silent empty dashboard on develop"]
