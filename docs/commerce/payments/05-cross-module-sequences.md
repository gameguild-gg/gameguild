# Payments Cross-Module Sequence Diagrams

> Module `GameGuild.Commerce.Payments` · Documented against `develop` commit `74e9bba4c`.
> Citation shorthand: `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`, `Economy:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/`, `API:` = `apps/api/Source/GameGuild.API/`.

## 1. Payments ↔ Stripe — synchronous charge

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant PC as PaymentsController
    participant H as ProcessPaymentCommandHandler
    participant FG as IPaymentGateway selector plus failover
    participant SPS as StripePaymentService
    participant ST as Stripe API

    C->>PC: POST api v1 payments with fraud metadata
    PC->>H: ProcessPaymentCommand
    H->>H: idempotency check and fraud gate and credit planning
    H->>FG: ProcessPaymentAsync GatewayPaymentRequest
    FG->>SPS: route by token and circuit state
    SPS->>ST: POST v1 payment_intents Confirm true IdempotencyKey
    alt provider answers within client timeout
        ST-->>SPS: PaymentIntent status
        SPS-->>FG: GatewayPaymentResult with ProviderMapping
    else StripeException 408 429 5xx or transport error
        ST-->>SPS: StripeException
        SPS-->>SPS: IsOutcomeUnknown L701 - fail OPEN to Processing<br/>OutcomeUnknown FailureKind L706
        Note over SPS: unknown outcome keeps the processing claim<br/>handler L293 - no retry double-charge
    else hard decline with Stripe error body
        ST-->>SPS: StripeException with StripeError
        SPS-->>FG: Failed TerminalDecline - fail CLOSED
    end
    FG-->>H: GatewayPaymentResult
    H->>H: BindVerifiedProviderMapping then MarkAsSucceeded<br/>RequiresAction or MarkAsFailed with retry schedule
    H-->>C: 201 PaymentResult
```

Fail-open/fail-closed verdicts: outcome-unknown (timeout/5xx/429/no body) is deliberately **fail-open** — the payment stays `Processing` because the provider may already have charged (`StripePaymentService.cs:701-715`, handler `:293-299`); every decline with a Stripe error body is **fail-closed** `TerminalDecline` (`:677-699`). Simulated mode replaces the provider leg entirely (`:717-723`).

## 2. Payments ↔ Stripe — webhook ingress and reconciliation

```mermaid
sequenceDiagram
    autonumber
    participant ST as Stripe
    participant WC as BillingWebhooksController
    participant VH as StripeWebhookVerifier
    participant IB as Webhook inbox BillingWebhookRepository
    participant SV as StripeBillingWebhookService routing
    participant LC as StripePaymentLifecycleEventConsumer
    participant PR as PaymentRepository

    ST->>WC: POST billing webhooks stripe with Stripe-Signature
    WC->>WC: response bound CTS under 5s L394
    WC->>VH: Verify payload signature
    VH->>VH: HMAC any active secret plus livemode<br/>api version account pinning L15
    alt signature invalid
        VH-->>WC: InvalidWebhookSignatureException
        WC-->>ST: 400 - provider will not retry
    end
    VH-->>SV: VerifiedStripeWebhookEvent minimized
    SV->>IB: GetByProviderScopeAsync idempotency L97
    alt already processed
        IB-->>ST: 200 WasAlreadyProcessed plus replay security event
    end
    SV->>IB: CreateAsync inbox row IsSignatureVerified true
    SV->>IB: TryClaimProcessingAsync single worker L170
    SV->>LC: RouteStripeEventAsync consumer chain L460
    alt payment_intent.succeeded for a commerce payment
        LC->>PR: resolve by provider mapping or legacy id L317
        LC->>PR: MarkAsSucceeded when non terminal L93
    else payment_intent.payment_failed
        LC->>PR: MarkAsFailed with default retry schedule L120
    else top-up intent not commerce
        LC-->>SV: released false L84
        Note over SV: StripeEconomyTopUpEventConsumer claims next
    end
    SV->>IB: MarkAsProcessed plus WebhookProcessedV1 outbox event
    WC-->>ST: 200 received
    alt processing exceeded the response bound
        WC-->>ST: 200 deferred - inbox retry worker completes L416
    else handler threw inside the bound
        SV->>IB: MarkAsFailed plus WebhookFailedV1
        WC-->>ST: 500 - Stripe redelivers
    end
```

## 3. Payments ↔ Economy — crediting and debit legs

Economy money movement touching payments happens on two rails: Stripe **top-up** minting (webhook rail) and **invoice-paid wallet credit** (outbox rail). Subscription payment success does **not** credit Economy (no consumer; see `06-gaps.md` N13).

```mermaid
sequenceDiagram
    autonumber
    participant ST as Stripe
    participant SV as Billing webhook routing
    participant EC as StripeEconomyTopUpEventConsumer Economy
    participant TS as EconomyTopUpSettlementStore
    participant PO as Protected operation authorizer
    participant GW as HardCoinFundingGateway
    participant OB as Outbox dispatcher
    participant WH as BillingInvoicePaidWalletCreditHandler

    ST->>SV: payment_intent.succeeded top-up intent
    SV->>EC: TryConsumeAsync verified event
    EC->>TS: FindAsync by provider identity L40
    alt no settlement row - not a top-up
        EC-->>SV: false released
    end
    EC->>EC: EnsureAuthoritativePayment amount currency tenant L154
    EC->>PO: ExecuteAsync ConfirmHardCoinFunding intent L83
    PO->>GW: Observe funding observation L89
    PO->>GW: ConfirmDurable confirmation L106
    GW-->>EC: posting receipt id
    EC->>TS: ApplyAsync Posted with posting id L107
    Note over EC: failure path marks Held and rethrows L119<br/>inbox redelivery retries the mint
    SV-->>ST: 200 processed

    Note over OB: separate rail - invoice paid wallet credit
    OB->>WH: BillingInvoicePaidV1 outbox event
    WH->>WH: wallet credit when WalletsOptions enabled L34
    WH-->>OB: receipt completed or dead-lettered after retries
```

Refunds debit no Economy wallet by default — the opt-in `CreditWalletOnPartialRefund` actually *credits* the local `UserWallet` (Commerce wallet, `RefundProcessingService.cs:810-844`), it does not reverse a HardCoin mint; top-up refunds are not wired back to Economy (gap N4).

## 4. Payments ↔ Notifications — receipts and dunning

```mermaid
sequenceDiagram
    autonumber
    participant H as ProcessPaymentCommandHandler
    participant N as INotificationService
    participant RPS as RefundProcessingService
    participant FDC as FraudDispute consumers
    participant U as User or Admin channel

    Note over H: payment succeeded with owner L340
    H->>N: SendAsync PaymentReceipt email L776
    N->>U: receipt email best effort
    Note over H: second consecutive failure L358
    H->>N: SendAsync PaymentMethodUpdateRequired High L830
    Note over H: credits applied L369
    H->>N: SendAsync CreditApplied email L722
    Note over H: fraud held L149
    H->>N: admin Urgent or High in-app L660
    H->>N: customer in-app only - email opt in L542
    Note over RPS: refund completed L729
    RPS->>N: SendAsync RefundCompleted to payment owner L753
    Note over RPS: queued for approval L284
    RPS->>N: approver emails from options L943
    Note over FDC: dispute created
    FDC->>N: dispute alert to team StripeDisputeEventConsumer L120
    Note over N: every leg best effort -<br/>delivery failures never change money outcomes
```

## 5. Payments ↔ Compliance — audit posture

```mermaid
sequenceDiagram
    autonumber
    participant RPS as RefundProcessingService
    participant RAS as RevenueAuditService Payments local
    participant DB as payments database
    participant CA as Compliance.Audit IAuditService
    participant BIL as Billing commands

    Note over RPS: refund completed L618 or L387
    RPS->>RAS: RecordRevenueEventAsync RefundProcessed L911
    RAS->>DB: revenue_events row
    RPS->>RAS: CreateLedgerEntryAsync Refund L921
    RAS->>DB: financial_ledger_entries row Dr Refunds Cr Cash
    RPS->>RAS: RecordAuditTrailAsync Payment StatusChanged L932
    RAS->>DB: audit_trails row - module local table
    Note over CA: no runtime bridge from Payments
    Note over BIL: only Billing security actions reach<br/>Compliance.Audit e.g. RotateWebhookSecretCommand L126
    BIL->>CA: LogAsync BillingWebhookSecretRotated Security Medium
```

Key finding: Payments money movements (success, refund, dispute, fraud hold) are audited into the module-local `audit_trails` / `revenue_events` tables only (`Payments:/Services/RevenueAuditService.cs:131+`); nothing projects them into the central `GameGuild.Compliance.Audit` module, whose `IAuditService.LogAsync` is currently invoked by Billing only for webhook-secret rotation (`Billing:/Commands/RotateWebhookSecret/RotateWebhookSecretCommand.cs:126-141`). Retention, chain-of-custody and security aggregation of the compliance module therefore do not cover payment money events — gap N15.

## 6. End-to-end success and refund composite

```mermaid
sequenceDiagram
    autonumber
    participant C as Client
    participant PC as Payments stack
    participant ST as Stripe
    participant SUB as Subscriptions sync
    participant N as Notifications
    participant AUD as Revenue audit local

    C->>PC: POST payments
    PC->>ST: PaymentIntent confirm
    ST-->>PC: succeeded
    PC->>SUB: SyncSuccessfulPaymentAsync L342
    PC->>N: receipt email L353
    Note over PC: PaymentSucceededEvent raised locally<br/>but no consumer exists - gap N13
    C->>PC: POST payments id refund with reason
    PC->>ST: POST v1 refunds idempotent by refund key
    ST-->>PC: refund object
    PC->>AUD: revenue event plus ledger plus audit trail L893
    PC->>SUB: suspend on full refund L851 best effort
    PC->>N: refund confirmation to owner L753
    ST--)PC: refund.created then charge.refunded webhook
    PC->>PC: CompleteFromGatewayAsync or coalesce L552 L245
```
