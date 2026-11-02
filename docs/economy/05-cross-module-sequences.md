# Economy Cross-Module Sequences

> Economy family `GameGuild.Finance.Economy*` · Documented against `develop` commit `ac959fd8f`.
> Citation shorthand: see [`README.md`](README.md). `Payments:` = `apps/api/Source/Modules/GameGuild.Commerce.Payments/`, `Billing:` = `apps/api/Source/Modules/GameGuild.Commerce.Billing/`.

## 1. Economy ↔ Commerce.Payments/Billing — top-up mint (and the refund gap)

The only money ingress into the Economy ledger from fiat payments is the Stripe top-up. Commerce owns the webhook ingress and signature verification; Economy owns a consumer in that chain.

```mermaid
sequenceDiagram
    autonumber
    participant U as User client
    participant E as EconomyWalletController
    participant S as SelfServiceHardCoinTopUpService
    participant St as Stripe
    participant B as Billing webhook ingress
    participant C as StripeEconomyTopUpEventConsumer
    participant L as Ledger writer functions

    U->>E: POST economy top-ups L131
    E->>S: CreateMyHardCoinTopUpCommand L147
    S->>St: create PaymentIntent - StripeEconomyTopUpProvider
    S->>E: intent Prepared - economy_top_up_intents L69
    E-->>U: 201 receipt + client secret
    U->>St: confirm payment off-session
    St->>B: payment_intent dot succeeded webhook
    B->>B: HMAC verify + inbox dedupe - StripeBillingWebhookService
    B->>C: IStripeVerifiedEventConsumer chain
    C->>C: match provider identity to intent L40
    C->>C: authoritative amount currency tenant check L154
    C->>L: protected op ConfirmHardCoinFunding + ConfirmedTopUpMint L67
    L->>L: journal group + credit lot + projection rebuild
    C->>S: ApplyAsync Posted + PostingGroupId L107
    Note over C: refund dot events are unmapped L128<br/>see KNOWN gap K4 - cross ref<br/>docs commerce payments 06-gaps N4 and PR 1202
```

Fail-open/closed verdicts: the mint is fail-closed — any mismatch or posting failure parks the intent in `Held` (`StripeEconomyTopUpEventConsumer.cs:119-125`) rather than crediting. The reverse leg (provider refund) fails **open**: the minted HardCoin stays with the wallet (`MapStatus :128-136` has no `refund.*` arm — documented as the top-up refund gap in the Payments audit package, `docs/commerce/payments/06-gaps.md` N4, PR #1202). Economy's own reversal rail (templates `ProviderReversalFull/Partial`, `economy_root_reversal_states`, wallet debts) is reachable only through internal reversal tooling, not the webhook chain.

## 2. Economy ↔ auth step-up — EconomyStepUpExecutor

Every admin money mutation wraps its action in a Serializable transaction that first consumes a step-up receipt bound to the exact operation fingerprint.

```mermaid
sequenceDiagram
    autonumber
    participant O as Admin operator
    participant A as Economy admin controller
    participant X as EconomyStepUpExecutor
    participant R as IStepUpReceiptService - Identity Authentication
    participant D as Durable economy service

    O->>O: MFA step-up - obtains receipt string
    O->>A: POST reserve dispatch propose with StepUpReceipt
    A->>A: EconomyStepUpOperation Create L14<br/>operation type + target ref + payload hash
    A->>X: ExecuteAsync operation receipt action
    X->>R: ConsumeAsync binding OperationType TargetReference PayloadHash L68
    R-->>X: consumed - replay of receipt impossible
    X->>X: evidenceHash = SHA256 receipt L75
    X->>D: protected action under Serializable tx L63
    D-->>A: result
    Note over X,R: binding is transaction specific -<br/>economy.payout.reserve versus economy.payout.dispatch<br/>differ in TargetReference and PayloadHash
```

Binding sources: payout reserve (`economy.payout.reserve`, target `payout-request:{id}`), payout dispatch (`economy.payout.dispatch`, target `payout-operation:{id}`, payload includes `ExpectedVersion` — `EconomyPayoutExecutionController.cs:168-171,199-204`), treasury propose/approve/dispatch (`EconomyTreasuryAdministrationController.cs:109-114,144-148,174-179`), legacy cutover (`EconomyLegacyMigrationAdministrationController.cs:116-155`). Receipt reuse across bindings fails in `ConsumeAsync` (receipt already consumed) — single-use proof, persisted by the auth module's step-up challenge store. The evidence hash feeds the economy's own `ReauthenticationEvidence` with 1-minute freshness (`EconomyFundsEndpointCommands.cs:226-238`).

## 3. Economy ↔ AI credits — reservation against Soft balance

```mermaid
sequenceDiagram
    autonumber
    participant AI as Authoring AI service - Courses or Blog
    participant W as AiCreditWalletService
    participant DB as ai_credit tables
    participant P as Wallet balance projection

    AI->>W: QuoteAsync service provider model max tokens
    W->>DB: effective rate card or default auto create L277
    W-->>AI: quote with MaximumSoftUnits
    AI->>W: ReserveAsync runId + quote + idempotency
    W->>P: lock wallet row - ambient transaction join
    W->>DB: AiCreditReservation Reserved L98
    Note over W,P: balance = projection AvailableSoftToSpend<br/>minus reserved minus settled L250
    AI->>AI: provider call executes
    AI->>W: SettleAsync actual tokens + providerUsageId
    W->>DB: SettledSoftUnits = actual - ReleasedSoftUnits = delta L134
    AI->>W: ReleaseAsync on failure - full release L159
```

Key property: the reservation ledger lives **outside** the economy journal — the wallet summary subtracts usage through `IEconomyWalletSoftUsageContributor` (`Core:/Queries/EconomyWalletQueries.cs:120-133`; registration `AiCredits:/AiCreditsModule.cs`). Settling AI usage never posts a journal group (no `PostingTemplateKind` covers it; `Burn` 7 is unused by this path) — SoftCoin is treated as unbacked utility credit, documented in the contributor interface doc comment. Provider cost facts are recorded separately (`Core:/Integrations/AI/AiCostAccountingCoordinator.cs:96`) with risk gating (`AiCostRiskGate.cs`).

## 4. Economy ↔ Commerce orders/entitlements — marketplace outbox bridge

```mermaid
sequenceDiagram
    autonumber
    participant B as Buyer
    participant M as DurableMarketplaceSettlementService
    participant W as economy_private.post_marketplace_settlement_v1
    participant Q as economy_marketplace_outbox
    participant K as EconomyDurableWorker
    participant H as CommerceMarketplaceOutboxHandler
    participant Cm as Commerce orders + entitlements

    B->>M: POST orders id settle
    M->>W: posting with legs + reservations + entitlementId
    W->>Q: insert marketplace.entitlement.grant.v1 - same tx
    K->>Q: claim lease - ProcessNextAsync
    K->>H: dispatch message
    H->>Cm: GrantEntitlementAsync buyer product L221
    H->>Cm: order MarkAsPaid then MarkAsFulfilled L239 L244
    Note over H,Cm: revoke leg mirrors this on full refund -<br/>RevokeEntitlementAsync + ProcessRefund L261 L274
```

Failure semantics: if Commerce rejects (entitlement failure, order drift), the handler throws, the lease is released and the row retries — the ledger has already settled, so the bridge is eventually-consistent by design; order-line snapshot re-verification (`PostgreSqlMarketplaceOutboxProcessor.cs:281-296`) protects against Commerce-side mutation. No attempt cap exists (gap N3).

## 5. Economy ↔ KYC (SumSub) — evidence into payout eligibility

```mermaid
sequenceDiagram
    autonumber
    participant SS as SumSub
    participant K as EconomySumSubWebhookController
    participant E as EconomyKycEvidenceStore
    participant CS as PostgreSqlComplianceEvidenceStore
    participant Q as economy_compliance_outbox
    participant P as ComplianceKycPayoutEligibilitySource

    SS->>K: POST integrations economy sumsub webhook L121
    K->>E: IngestSumSubWebhookEndpointCommand
    E->>CS: envelope with provider event id
    CS->>CS: inbox dedupe by ProviderEventId L193
    CS->>CS: persist evidence + compliance hold state
    CS->>Q: economy.compliance-evidence.published.v1 L288
    P->>CS: read fresh approved evidence for payee
    P-->>P: PayoutKycSnapshot feeds reserve checks
    Note over Q: outbox row has no consumer today - gap N2
```

## 6. Economy ↔ Stripe Connect — payout provider dispatch and reconciliation

```mermaid
sequenceDiagram
    autonumber
    participant Op as Admin operator
    participant E as EconomyPayoutExecutionController
    participant D as DurablePayoutApplicationService
    participant Q as payout dispatch outbox
    participant W as EconomyDurableWorker
    participant St as Stripe Connect

    Op->>E: POST operations id dispatch - ExpectedVersion + StepUpReceipt
    E->>D: DispatchPayoutOperationCommand L203
    D->>D: policy + reserve head + destination + fragments recheck
    D->>Q: BeginDispatchAsync - enqueue fenced command L142
    W->>Q: claim FOR UPDATE SKIP LOCKED
    W->>St: DispatchAsync payout - idempotency key L106
    St-->>W: receipt - Submitted Ambiguous Succeeded Failed
    W->>D: transition or bind provider payout id L132
    St--)E: webhook payout dot paid - integrations stripe-connect L337
    E->>D: ApplyProviderEventAsync - signature verified L229
    Op->>E: POST operations id reconcile - manual exit for Ambiguous L216
```

Timeout/ambiguity: a provider timeout yields `Ambiguous` (`DurablePayoutSettlementWorkflow.cs:134-137`); the exit rails are the webhook and operator reconcile (`DurablePayoutApplicationService.cs:279-310`) — there is no scheduled ambiguity sweep (gap N7).
