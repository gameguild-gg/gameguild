# Economy STRIDE Threat Model — value-movement grade

Status: living document, current as of 2026-11-04 against `origin/develop` (21b58ac6a7). First full STRIDE pass for the Economy family — [`07-gaps.md`](07-gaps.md) tracks the plumbing gaps; this document adds the attacker/regulator view per flow family.

Companion documents: [`09-security-architecture.md`](09-security-architecture.md) (trust boundaries, who-can-move-money), [`10-dfds.md`](10-dfds.md) (data flow diagrams), [`05-cross-module-sequences.md`](05-cross-module-sequences.md) (cross-module sequences). Citation shorthand as in [`README.md`](README.md); `Migration:` = the economy production-platform migration.

**Post-#1266 note (verified)**: the mint/reversal asymmetry documented as K4 is FIXED at this tree — `charge.refunded` now drives `TryReverseRefundedTopUpAsync` (`Core:/Integrations/StripeEconomyTopUpEventConsumer.cs:33,43-44,158-266`) through the persisted provider-reversal writer with `economy:stripe:reverse` idempotency and `ProviderReversalDisposition.ResponsibleDebt` partitioning for already-spent units. Family E1 models the new symmetric rail.

## 1. Methodology

STRIDE per flow family, judged against two adversaries at once: the classic attacker (steal/lock/inflate value) and the auditor (prove every unit's provenance). Economy-specific invariant under test: **double-entry integrity** — every movement posts balanced legs to the journal through SECURITY DEFINER writer functions (`economy_private.*`, `Migration:`), with FIFO credit-lot escrow fragments and reserve versions fencing concurrent spends.

Flow families (DFDs in [`10-dfds.md`](10-dfds.md)):

| # | Family | Entry surface |
|---|--------|---------------|
| E1 | HardCoin top-up mint and provider reversal | Stripe webhook consumer, `API:/Core/Controllers/EconomyWalletController.cs:131` region |
| E2 | Hard-to-soft conversion and self-service transfers | `EconomyWalletController.cs:67,84` |
| E3 | Payout request dual-control and fenced execution | `EconomyWalletController.cs:267,321`; `EconomyPayoutExecutionController`; `EconomyPayoutAdministrationController` |
| E4 | Treasury platform-fee withdrawal | `EconomyTreasuryAdministrationController` |
| E5 | Marketplace settlement and refund | `EconomyMarketplaceController` |
| E6 | Bounty escrow lifecycle | `EconomyBountiesController` |
| E7 | Ad-reward issuance | `EconomyAdRewardsController` |
| E8 | AI-credit reservation | AiCredits module, no public controller |
| E9 | Control plane — capability policies, kill switches, holds, risk, KYC gate | `EconomyControlPlaneAdministrationController` et al. |

## 2. E1 — Top-up mint and provider reversal

Entry: Stripe webhook → `StripeEconomyTopUpEventConsumer` (verified HMAC ingress, provider environment/account pinning); fiat capture mints HardCoin via the funding gateway's durable confirmation.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — forged provider event | Unsigned POST mints coins | Signature verification before identity resolution; canonical payment-intent identity from provider object fields (`StripeEconomyTopUpEventConsumer.cs:175-186` region — provider env + account + object id); `MapStatus` only maps `payment_intent.*` (`:150-157`) so unknown event shapes cannot drive postings |
| T — double mint on redelivery | Webhook replay posts twice | Durable claim + authorization ids stored per intent (`fundingGateway.ConfirmDurable`, `:120-135` region); posting idempotency at the writer function level |
| T — mint without capture | `processing`/`requires_action` minted early | `MapStatus` maps only `payment_intent.succeeded → Posted` (`:150-157`); other statuses are transitional states, not value |
| R — "I paid but got no coins" | Dispute of a mint | Top-up rows carry provider evidence + posting receipt (`:120-135` region); journal legs attributable |
| I — webhook payload leakage | Pan/PII in logs | Events verified then reduced to provider ids and hashes; no card data touches the consumer (Stripe redacts at source) |
| D — webhook flood | Unthrottled ingress | No `Webhook` rate policy on this consumer's host endpoint (N9 family — no Economy controller applies rate limiting) |
| E — reversal bypass | Refund leaves mint intact | **Fixed by #1266**: `charge.refunded` (cumulative `amount_refunded`) drives `TryReverseRefundedTopUpAsync` (`:33,158-266`) — protected operation `ReverseProviderFunding`, idempotency `economy:stripe:reverse`, spent units become wallet debt (`ProviderReversalDisposition.ResponsibleDebt`, `:239-251`), event then RELEASED for the Commerce lifecycle consumer (`:261-266`). Partial refunds step up cumulatively; whole-unit requirement enforced (`:268-278`) |

**Double-entry integrity**: mint and reversal both post through the same SECURITY DEFINER writer family — symmetric rails, balanced legs, FIFO fragments move back on reversal. The residual asymmetry is *timing*: reversal fires on provider webhook cadence, so a refunded wallet can spend between refund and reversal — the debt partition is the containment, and it is documented in code (`:246-251`).

## 3. E2 — Conversion and self-service transfers

Entry: `POST economy/conversions/hard-to-soft` (`EconomyWalletController.cs:67-80`), `POST economy/transfers` (`:84-130`).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — transfer as another user | Caller moves another wallet's funds | `HasSelfServiceContext()` gate + "server resolves wallets, jurisdiction, policy, reserve, risk, posting authority; the request contains business intent only" (`:86-89` EndpointDescription, enforced server-side) |
| T — amount/recipient tampering | Client-side amount editing | Server-side resolution of both wallets from actor context; typed Money/CoinAmount values at the writer boundary |
| R — deny initiating a transfer | "I never sent that" | Posting receipts + journal legs + risk legs (`RiskCurrencyLeg` on every protected intent, `:229` in the consumer's intent shape — same pattern family-wide); audit via the compliance evidence stream for compliance-relevant ops |
| I — balance oracle | Enumerate other users' wallets | Self-service reads are actor-scoped (`TryGetSelfServiceActor`, `:257,311,362`) |
| D — transfer spam | Journal flooding | No rate policy on `POST transfers` (N9); the risk rail throttles by policy not rate |
| E — overdraft via concurrency | Two concurrent transfers spend the same coins | Reserve versions fence concurrent authorization (`receipt.ReserveVersion` equality, `DurablePayoutSettlementWorkflow.cs:405` pattern); FIFO fragments consumed under writer-function serialization |

## 4. E3 — Payout dual-control and fenced execution

Entry: request `POST economy/payout-requests` (`EconomyWalletController.cs:267`), cancel `:321`; review `EconomyPayoutAdministrationController` (`CanAdministerWallets` = typed permission `economy:review:payouts`, `:123-128`); execution `EconomyPayoutExecutionController` behind `EconomyStepUpExecutor`.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — requester executes own payout | Same person requests and dispatches | Dual-control by construction: request (self-service) → review (typed `economy:review:payouts`) → execute (`economy:operate:payouts` + step-up receipt bound to operation/target/payload-hash, `EconomyStepUpExecutor.cs:9-65` Serializable) |
| S — stale authorization replay | Reuse an old receipt/epoch | Receipt epoch equality + reserve version + policy version + destination hash + source root hashes all checked (`DurablePayoutSettlementWorkflow.cs:400-410`); `PayoutStaleCommandException` on mismatch — the fencing-token core |
| T — dispatch payload swap | Modify operation between authorize and send | `DestinationHash`/`ProviderHash` bound into the receipt (`:407-409`); fenced allocation via `PostgreSqlPayoutFencingTokenAllocator` (`PayoutsModule.cs:16`) |
| R — deny a payout | "We never sent it" | Immutable review trail (`EconomyPayoutAdministrationController.cs:47`), authorization-evidence writer (`PayoutsModule.cs:25`), provider evidence verifier (`:23`), dispatch outbox history |
| I — payout data harvest | Read others' payout operations | Self lists are actor-filtered (`ListMyPayoutOperationsQuery` tenant+actor, `:257-262`); admin reads permission-gated |
| D — pinned reservations | Reserved-but-undispatched operation holds escrow forever | **N4 stands**: `Reserved → Cancelled` unreachable in production (coordinator not composed, `PayoutsModule.cs:14-31`); request-level cancel exists (`:321`) but operation-level does not |
| E — kill switch bypass in flight | Activate switch after dispatch enqueue | **N6 stands**: dispatch outbox processor does not re-check gates/epoch before provider send (`PostgreSqlPayoutDispatchOutbox.cs:73-160` region); enforcement happens at authorization time (`DurablePayoutSettlementWorkflow.cs:405`) |

**Dual-control verdict**: the request→review→execute chain with step-up receipts and fenced tokens is genuinely strong; the open exposures are operational dead ends (N4) and the in-flight window (N6/N7 ambiguity without sweep).

## 5. E4 — Treasury platform-fee withdrawal

Entry: `EconomyTreasuryAdministrationController` (`:44-206` region). Engine: `DurableAdminWithdrawalApplicationService` — proposal requires fresh reauthentication evidence (`ReauthenticationEvidenceValidator.RequireFresh`, `:95-100`), reserves funds at proposal.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — unauthorized withdrawal | Non-treasury actor proposes a run | Typed permission `economy:operate:treasury` (`TypedPermissions.cs:942`) + fresh reauth evidence bound to (period, amount, destination hash, idempotency key) (`:88-100`) |
| T — destination swap after approval | Dispatch to a different account | `DestinationHash` bound at proposal into the protected-operation binding (`:86-87`) |
| R — deny a withdrawal | Run trail | Versioned run states + execution history through the durable workflow |
| I — platform-fee visibility | Fee flows observable | Admin reads permission-gated (`economy:read:operations`) |
| D — pinned proposal blocks period | `PendingApproval` run holds value indefinitely | **N5 stands**: no cancel path in the durable controller; the overlap guard treats every non-Failed/non-Cancelled run as blocking (`PostgreSqlAdminWithdrawalStore.cs:238-240`) |
| E — overlapping withdrawals drain fees | Two runs same period | Overlap guard keyed on period + state (`:236-240`) — effective while N5 pins runs |

## 6. E5 — Marketplace settlement and refund

Entry: `EconomyMarketplaceController` (self-service + administration). Engine: `DurableMarketplaceSettlementService` — settlement posts buyer→seller→platform legs atomically with deterministic entitlement ids (`:92,150,212`); born-`Settled` by design (K8).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — settle an unpaid order | Entitlement without payment | Settlement authority consumes the Commerce order decision (`CommerceOrderMarketplaceSettlementAuthority.cs:10-15`) — the seam Payment→Order→Economy |
| T — forged entitlement | Double-issue entitlements | Deterministic ids `DeterministicId(settlementId, entitlement)` (`:150`) — replay collides rather than duplicates |
| R — deny a purchase | Receipt trail | Entitlement receipt + settlement receipt with posting ids (`MarketplaceContracts.cs:164-175` region) |
| I — counterparty exposure | Buyer sees seller identity | Wallet projections are id-based; no direct PII on settlement rows |
| D — refund livelock | Poison refund row retried forever | **N3 stands**: no attempt cap on the marketplace outbox (`PostgreSqlMarketplaceOutboxProcessor.cs:96-110` — failure releases lease, `AttemptCount` grows unbounded) |
| E — refund without reversal symmetry | Refund leg unbalanced | Refund legs post through `post_marketplace_refund_v1` writer (atomic with entitlement release) — symmetric by construction; the E1-style asymmetry does NOT exist here |

## 7. E6 — Bounty escrow

Entry: `EconomyBountiesController`. Escrow posts FIFO fragments at bounty creation; expiration swept by the durable worker safety cycle (`EconomyDurableWorker.cs:72-75` — `IDurableBountyExpirationWorkflow.ExpireDueAsync`).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — claim as non-winner | Issuance to wrong actor | Award path posts through the protected-operation rail with subject ids; completion idempotent |
| T — double payout of a bounty | Concurrent claims | Writer-function serialization + unique completion keys (4-table sub-module, `02-cqrs-wiring-matrix.md`) |
| R — deny receiving a bounty | Trail | Journal legs + work-queue history |
| I — poster wallet exposure | — | id-based, as E5 |
| D — expired bounties pin escrow | Funds never return | Expiration sweep runs in the default-ON safety cycle (`EconomyDurableWorker.cs:58-75`) — the counter-example to N4/N5 |
| E — self-award | Poster claims own bounty | Guarded in the command layer (`02-cqrs-wiring-matrix.md` bounty rows) |

## 8. E7 — Ad-reward issuance

Entry: `EconomyAdRewardsController` + `AdRewardProviderAdapters.cs`. Sessions carry milestones; completions claim through `economy_ad_reward_pending_claims`.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — forged ad completion | Client fabricates a milestone | Provider adapters verify provider-side completion before issuance posts (`post_ad_reward_issuance_v1` writer) |
| T — replay a completion | Same completion claimed twice | Pending-claims queue rows are consumed exactly once by the writer transaction |
| R — deny earning | Trail | Issuance posting ids per session |
| I — session metadata | — | No PII on session rows |
| D — pending-claims backlog | Provider outage | Safety-cycle marketplace outbox only; pending claims drain on their own cadence — unbounded retry class N3 applies |
| E — milestone inflation | Client rounds up | Server-side milestone thresholds; writer validates amounts |

## 9. E8 — AI-credit reservation

Entry: in-process `AiCreditWallet` (`AiCredits:/Integrations/AiCreditWallet.cs:105-115` Reserve/Settle/Release); balance view subtracts reserved+settled contributors (`:250-264`, `Core:/Queries/EconomyWalletQueries.cs:120-133`).

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — reserve for another user | — | Contributor interface is internal; no public controller |
| T — settle above reserve | Overdraft via settlement | Argument guards on Settle (`:105-115`); idempotency keys |
| R — deny usage | — | Reservation/settlement rows per provider usage id |
| I — prompt/content leakage | Model I/O in economy rows | Only token counts and unit values stored — no content |
| D/E — crashed reservation pins Soft forever | Availability theft from the user | **N8 stands**: no expiry sweep; `Reserved → Released` has no worker (`AiCreditsModule.cs`, durable worker — nothing sweeps) |
| E — usage outside double-entry | Journal drift | K6 deliberate: usage rides a contributor view, not `Burn` postings — flagged for the #1092 ledger collapse |

## 10. E9 — Control plane

Entry: `EconomyControlPlaneAdministrationController` (permission echo at `:445`), `EconomyComplianceAdministrationController`, `EconomyComplianceHoldAdministrationController`, `EconomyRiskReviewAdministrationController`, `EconomyKycController`.

| Threat | Scenario | Mitigation or gap |
|--------|----------|-------------------|
| S — kill-switch activation by non-admin | Attacker halts or unfreezes value flows | Typed permissions `economy:manage:kill-switches` etc. (`TypedPermissions.cs:931-944`); controller re-checks `actor.HasPermission` (`EconomyControlPlaneAdministrationController.cs:445`) |
| T — policy tampering | Weakened capability policy version | Signed policies; writer functions pin policy versions into receipts (`DurablePayoutSettlementWorkflow.cs:403-405`) |
| R — compliance decision dispute | "Why was I blocked" | Compliance holds and risk reviews are durable, versioned rows; KYC evidence versioned envelopes (F8 of the compliance STRIDE) |
| I — KYC subject data | Cross-subject reads | Self-scoped `TryActor` (`EconomyKycController.cs:100-110`); evidence reads id-scoped |
| D — control-plane flood | — | N9 no rate limits |
| E — gate bypass via legacy ledger | N9 (compliance) status regression flips payout eligibility | `ComplianceKycPayoutEligibilitySource.cs:22,36-37` reads the legacy ledger — the KYC STRIDE (compliance package) N7/N9 apply here as the money-side consequence |

## 11. Summary matrix

| Family | S | T | R | I | D | E | Open money-relevant gap |
|--------|---|---|---|---|---|---|-------------------------|
| E1 mint+reversal | strong | strong | strong | ok | gap N9 | **fixed #1266** | unthrottled ingress |
| E2 conversion/transfer | strong | strong | strong | strong | gap N9 | strong | — |
| E3 payout dual-control | **strong** | **strong (fencing)** | strong | strong | gap N4 | gap N6 | pinned reservations; in-flight switch |
| E4 treasury | strong | strong | strong | ok | **gap N5** | strong | pinned proposals |
| E5 marketplace | strong | strong | strong | strong | gap N3 | strong | poison-refund livelock |
| E6 bounty | strong | strong | strong | strong | strong | strong | — |
| E7 ad rewards | strong | strong | strong | strong | gap N3 | strong | — |
| E8 AI credits | n/a | strong | strong | strong | **gap N8** | gap K6 | reservations never expire |
| E9 control plane | strong | strong | strong | strong | gap N9 | gap N7-spillover | legacy KYC gate |
