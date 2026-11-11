# Payments Data Flow Diagrams — DFDs

> Module `GameGuild.Commerce.Payments` (+ Stripe ingress in `GameGuild.Commerce.Billing`, Economy top-up in `GameGuild.Finance.Economy`) · Documented against `develop` commit `21b58ac6a7`.
> Companion documents: flows (`01`), STRIDE (`07-stride-threat-model.md`), architecture (`../09-security-architecture.md`). Threat numbering in the annotations refers to the STRIDE document.

## 1. Notation and conventions

Mermaid flowchart with standard DFD shapes:

- External entity — rectangle: `E1["Client"]`
- Process — stadium: `P1(["1.1 Validate and enrich charge request"])`
- Data store — cylinder: `D1[("payments table")]`
- Trust boundary — one subgraph per zone

Zones used throughout (money-path trust boundaries, detailed in `../09-security-architecture.md` §2):

| Zone | Boundary id | Meaning |
|------|-------------|---------|
| Client | TB1 | User-controlled browser or API consumer |
| API | — | The modular monolith trust zone (controllers, CQRS, entities) |
| Stripe | TB2 | Outbound provider zone — money actually moves here |
| Webhook ingress | TB3/TB4 | Anonymous perimeter that becomes trusted only after signature verification |
| Outbox | TB5 | Durable integration-event transport |
| Economy | TB6 | HardCoin mint/reversal authority |

Citation shorthand: `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`, `Economy:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/`, `API:` = `apps/api/Source/GameGuild.API/`.

## 2. Level 0 — context diagram

The payments surface as one process against its external actors and stores.

```mermaid
flowchart LR
    USER["Payer and subscriber"]
    ADMIN["Tenant or system admin - refunds fraud disputes"]
    STRIPE["Stripe API"]
    NB["Email and notification providers"]

    PAY(["0.0 GameGuild Payments surface"])

    PG[("Commerce PostgreSQL - payments refunds disputes wallets")]
    INBOX[("billing_webhook_events durable inbox")]
    OUTBOX[("outbox integration events")]
    ECON[("Economy HardCoin wallets")]

    USER -->|"charge and checkout requests with fraud metadata"| PAY
    ADMIN -->|"refund approve reject fraud review dispute actions"| PAY
    STRIPE -->|"signed webhook deliveries"| PAY
    PAY -->|"PaymentIntent create confirm refund dispute evidence"| STRIPE
    PAY -->|"receipts dunning fraud notices"| NB
    PAY -->|"payment refund dispute fraud wallet rows"| PG
    PAY -->|"verified inbox rows"| INBOX
    PAY -->|"invoice paid and webhook outcome events"| OUTBOX
    PAY -->|"top-up mint and reversal confirmations"| ECON
```

Boundaries crossed at level 0: TB1 client to API, TB2 API to Stripe, TB3 Stripe to webhook ingress, TB5 outbox, TB6 Economy.

## 3. Level 1 — flow families

### F1 — Payment initiation and Stripe exchange

Entry `POST api/v1/payments` (`Payments:/Controllers/PaymentsController.cs:130-184`); core `ProcessPaymentCommandHandler` (`Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:36-389`); gateway `StripePaymentService.cs:593-642`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone - TB1"]
        E1["Payer browser or API client"]
    end

    subgraph API["API trust zone"]
        P1(["1.1 Validate tenant and amount and token<br/>PaymentsController cs L130 L138"])
        P2(["1.2 Collect fraud metadata<br/>IP fingerprint BIN timezone L163"])
        P3(["1.3 Resolve subscription context and guards<br/>handler L41 L50"])
        P4(["1.4 Derive cycle idempotency key<br/>L60 replay lookup L70"])
        P5(["1.5 Fraud gate AssessPaymentAsync<br/>L138"])
        P6(["1.6 Plan account credit tender<br/>L196"])
        P7(["1.7 Persist Processing claim<br/>GatewayPaymentResultBinder cs L10"])
        P8(["1.8 Bind outcome MarkAsSucceeded<br/>RequiresAction Failed L281 L290"])
        P9(["1.9 Consume credits sync subscription<br/>receipt notify L323 L342 L353"])
    end

    subgraph STRIPE["Stripe zone - TB2"]
        S1(["1.10 PaymentIntent create Confirm true<br/>IdempotencyKey StripePaymentService cs L599"])
    end

    D1[("payments table - idempotency key unique")]
    D2[("payment_retry_attempts and audit rows")]
    D3[("Stripe PaymentIntent - provider money truth")]

    E1 -->|"POST payments amount method token"| P1
    P1 --> P2
    P2 --> P3
    P3 --> P4
    P4 --> P5
    P5 -->|"Allow"| P6
    P5 -->|"Review or Block"| D1
    P6 --> P7
    P7 --> S1
    S1 -->|"gateway result or unknown"| P8
    P8 --> D1
    P8 --> D2
    P8 -->|"on success"| P9
    P9 --> D1
    P4 -->|"replay returns recorded row"| D1
```

Key flows across boundaries: fraud metadata crosses TB1 inbound (server-side collected, client AVS or CVV ignored — STRIDE 4.2); the idempotency key crosses TB2 so a replayed charge cannot double-move money (STRIDE 1.4); outcome-unknown responses keep the local claim and never re-cross TB2 blindly (STRIDE 1.7).

### F2 — Refund legs — request approval execution webhook completion

Entries `PaymentsController.cs:445` and `RefundsController.cs:36`; execution `RefundProcessingService.ExecuteAsync :336`; webhook completion `CompleteFromGatewayAsync :607+`.

```mermaid
flowchart TB
    subgraph CLIENT["Client zone - TB1"]
        E1["Requester"]
        E2["Approver with payments refunds approve"]
    end

    subgraph API["API trust zone"]
        P1(["2.1 Validate status and bounds<br/>remainder single open refund RPS cs L185 L246"])
        P2(["2.2 Threshold route<br/>approval queue or immediate L297 L316"])
        P3(["2.3 ExecuteAsync MarkProcessing<br/>gateway refund idempotent L336 L354"])
        P4(["2.4 CompleteFromGatewayAsync<br/>amount verified against local row L644"])
        P5(["2.5 ApplyRefundToPaymentAsync<br/>Version serialized cumulative guard L539"])
        P6(["2.6 Record revenue event ledger audit L893"])
    end

    subgraph STRIPE["Stripe zone - TB2"]
        S1(["2.7 Stripe RefundCreateOptions<br/>payment currency minor units cs L754"])
    end

    subgraph WBI["Webhook ingress - TB3 TB4"]
        W1(["2.8 refund created or updated<br/>verified event consumer chain"])
    end

    D1[("refunds table - one open row per payment")]
    D2[("payments table - RefundedAmount cumulative")]
    D3[("revenue_events financial_ledger_entries audit_trails")]

    E1 -->|"refund request reason amount"| P1
    E2 -->|"approve or reject"| P2
    P2 --> P3
    P3 --> S1
    S1 -->|"refund object"| P3
    W1 -->|"gateway id and reported amount"| P4
    P4 --> P5
    P5 --> D2
    P3 --> D1
    P4 --> D1
    P5 --> P6
    P6 --> D3
```

Boundary notes: the refund amount crosses TB2 converted with the **payment's** currency (the #1202 class fix, `StripePaymentService.cs:754-768`); the webhook leg crosses TB3/TB4 and its reported amount is checked against the local row before completion (`RefundProcessingService.cs:644-653`); both completion paths serialize on the entity `Version` token (STRIDE 3.3).

### F3 — Dispute and chargeback

Ingress `Billing:/Services/StripeDisputeEventConsumer.cs:25-80`; operator surface `Payments:/Controllers/DisputesController.cs`.

```mermaid
flowchart TB
    subgraph STRIPE["Stripe zone - TB2"]
        S1["Cardholder bank raises dispute"]
    end

    subgraph WBI["Webhook ingress - TB3 TB4"]
        W1(["3.1 Verify signature and inbox<br/>charge dispute events consumer L31"])
    end

    subgraph API["API trust zone"]
        P1(["3.2 Resolve payment fail closed<br/>unknown target throws L50"])
        P2(["3.3 Create dispute record fee<br/>MarkAsDisputed only from Succeeded L103 L109"])
        P3(["3.4 Collect automatic evidence L117"])
        P4(["3.5 Operator respond accept challenge<br/>submit evidence resolve"])
        P5(["3.6 Accept records chargeback settled refund<br/>ExecuteAtGateway false path"])
    end

    subgraph CLIENT["Client zone - TB1"]
        E1["Dispute operator"]
    end

    D1[("payment_disputes dispute_evidence tables")]

    S1 -->|"signed charge dispute created"| W1
    W1 --> P1
    P1 --> P2
    P2 --> P3
    E1 -->|"dispute decisions"| P4
    P4 --> P5
    P4 --> D1
    P2 --> D1
    P5 --> D1
    P4 -->|"evidence upload"| S1
```

Open note: the operator respond action can set arbitrary dispute statuses through the default branch (`Payments:/Services/DisputeService.cs:81`) — STRIDE 5.4, residual R-P1.

### F4 — Fraud assessment review blacklist

Gate `Payments:/Services/FraudDetection/FraudDetectionService.cs:36-174`; review queue `FraudReviewsController`; background re-score `PaymentsModule.cs:147`.

```mermaid
flowchart TB
    subgraph API["API trust zone"]
        P1(["4.1 Build signal context<br/>13 providers clamped score L62 L142"])
        P2(["4.2 Decide by thresholds<br/>Block 70 Review 40 L146"])
        P3(["4.3 RecordFraudAssessment<br/>hold or block no gateway call L138"])
        P4(["4.4 Background re-score tightening only<br/>Payment cs L818 L840"])
        P5(["4.5 Review resolve approve or deny<br/>auto blacklist on deny"])
    end

    subgraph CLIENT["Client zone - TB1"]
        E1["Fraud admin"]
    end

    subgraph WORKER["Background worker zone"]
        B1(["4.6 FraudBackgroundScoringWorker<br/>in process queue - volatile R-P5"])
    end

    D1[("payments - FraudScore evidence JSON")]
    D2[("fraud_blacklist_entries tenant scoped")]

    P1 --> P2
    P2 --> P3
    P3 --> D1
    P4 --> D1
    B1 --> P4
    P4 -->|"deferred jobs"| B1
    E1 --> P5
    P5 --> D2
    P5 --> D1
```

### F5 — Webhook ingress — Stripe to platform

Entry `BillingWebhooksController.cs:186-189`; pipeline `StripeBillingWebhookService.cs:70+`.

```mermaid
flowchart TB
    subgraph STRIPE["Stripe zone - TB3"]
        S1["Stripe webhook sender"]
    end

    subgraph WBI["Webhook ingress zone - anonymous until verified"]
        W1(["5.1 Signature and payload verification<br/>HMAC rotation window StripeWebhookVerifier cs L15"])
        W2(["5.2 Inbox idempotency by provider scope<br/>replay raises security event cs L97 L114"])
        W3(["5.3 Claim single worker L170"])
        W4(["5.4 Route consumer chain L460"])
    end

    subgraph API["API trust zone"]
        P1(["5.5 Lifecycle consumer - succeed fail refund legs"])
        P2(["5.6 Dispute consumer"])
        P3(["5.7 Economy top up consumer"])
        P4(["5.8 Subscription invoice routing L472"])
    end

    D1[("billing_webhook_events + raw delivery opt in")]
    D2[("outbox - WebhookProcessedV1 FailedV1")]

    S1 -->|"raw payload and Stripe Signature header"| W1
    W1 -->|"invalid signature 400"| S1
    W1 --> W2
    W2 --> W3
    W3 --> W4
    W4 --> P1
    W4 --> P2
    W4 --> P3
    W4 --> P4
    W2 --> D1
    W4 --> D2
    W4 -->|"failure 500"| S1
```

The entire ingress zone is untrusted until 5.1 passes; signature-unverified legacy rows are never replayed (`StripeBillingWebhookService.cs:235-245`).

### F6 — Economy interface — top up mint and reversal

`Economy:/Integrations/StripeEconomyTopUpEventConsumer.cs`; wallet credit `API:/Core/Billing/BillingInvoicePaidWalletCreditHandler.cs`.

```mermaid
flowchart TB
    subgraph WBI["Verified webhook chain - TB4"]
        W1(["6.1 payment_intent success for top up intent<br/>settlement row required cs L40"])
        W2(["6.2 charge.refunded reversal trigger<br/>cumulative amount_refunded cs L43"])
    end

    subgraph API["API trust zone"]
        P1(["6.3 ConfirmHardCoinFunding<br/>protected operation authorization cs L83"])
        P2(["6.4 Provider reversal of the mint cs L161"])
        P3(["6.5 Wallet credit on BillingInvoicePaidV1<br/>idempotent per event id"])
    end

    subgraph ECON["Economy zone - TB6"]
        D1[("Economy top up settlements - Posted or Held")]
        D2[("HardCoin wallets")]
    end

    subgraph OBX["Outbox zone - TB5"]
        D3[("outbox receipts - 5 attempts then dead letter")]
    end

    W1 --> P1
    W1 --> D1
    P2 --> D2
    W2 --> P2
    D3 --> P3
    P3 --> D2
    P1 --> D1
```

The mint (6.1→6.3) only fires on a verified success event bound to an existing settlement row; the reversal (6.2→6.4) exists since #1266 and closes the refund-then-keep-coins drain (STRIDE 7.2).

## 4. Data store inventory on the money path

| Store | Contents that matter | Money integrity role |
|-------|----------------------|----------------------|
| `payments` | status, amount, currency, idempotency key unique, provider mapping, `RefundedAmount`, `Version` | double-charge and double-refund backstop |
| `refunds` | one open row per payment, status, gateway refund id, approval state | refund leg serialization |
| `payment_disputes` + `dispute_evidence` | dispute lifecycle, fee, evidence | chargeback truth |
| `billing_webhook_events` | `(ExternalEventId, Provider)` unique, attempts, dead-letter | replay and poison control |
| `outbox` + receipts | integration events, per-consumer attempts | durable fan-out |
| module-local `revenue_events` `financial_ledger_entries` `audit_trails` | accounting mirror of every money mutation | repudiation defense |
| `user_wallets` `wallet_transactions` | Commerce wallet balances | account-credit and refund-credit target |

## 5. Coverage notes

- Every flow family of `01-user-flows.md` is rendered: initiation (F1), refunds (F2), disputes (F3), fraud (F4), webhook ingress (F5), payment-method lifecycle folds into F1 (checkout sub-flow, `PaymentsController.cs:208-311`), currency suggestion and FX surfaces are read-only config flows with no money mutation and are covered at architecture level (`../09-security-architecture.md` §6).
- Threat annotations map 1:1 to `07-stride-threat-model.md` sections.
