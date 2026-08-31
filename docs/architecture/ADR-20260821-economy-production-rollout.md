# ADR 2026-08-21: Economy production rollout remains fail-closed

Status: Accepted

## Context

The dual-currency architecture, threat model, and whitepaper describe a financial
platform whose capabilities must not become active through deployment defaults or
partial provider configuration. The durable backend operating model, protected
APIs, generated client and release gates are implemented; commercial activation
and the explicitly excluded operational web consoles are separate concerns.

## Decision

- Economy provider contracts remain domain-neutral. `IConnectPayoutProvider` is
  the payout boundary and Stripe Connect is the first adapter, never a domain
  dependency.
- `IKycAmlProvider` is the compliance boundary and SumSub is its first adapter.
  Trust/Safety evidence originates from signed internal events; neither source can
  authorize value when unavailable, stale, unordered, or unverifiable.
- `IAnchorSigner` and `IWormAnchorStore` remain provider-neutral. Their first
  production adapters are asymmetric AWS KMS signatures and S3 Object Lock with
  mandatory read-back. HMAC and process memory are test-only implementations.
- Every value-moving capability is disabled unless its complete capability
  predicate succeeds. Missing configuration, stale evidence, an unknown
  jurisdiction, a missing provider, or a failed readiness check resolves to
  `Disabled`.
- The global jurisdiction allowlist is empty by default. A jurisdiction can only
  be enabled through signed policy configuration after Legal, KYC/AML, provider
  certification, and operational approval.
- The first production deployment is read-only for value movement. It may expose
  evidence intake, health, wallet reads, and containment diagnostics, but it may
  not mint, convert, settle, reserve for external dispatch, or dispatch a payout.
- Protected payout changes require tenant-scoped review, immutable reasons,
  distinct reviewer approvals, transaction-bound reauthentication, risk evidence,
  reserve and custody snapshots, a verified anchor, FIFO reservation, durable
  outbox dispatch, and reconciliation.
- Legacy `UserWallet`, `WalletTransaction` and `FinancialLedgerEntry` data is
  migrated through a tenant-scoped shadow workflow. Capture is serializable and
  read-only, non-zero balances post through the protected writer with explicit
  `PurchasedHard` provenance, and cutover requires a proposer plus two different
  reauthenticated approvers. The source remains reversible until reconciliation
  and an explicit cutover; an active cutover blocks new legacy wallet writes.
- Signed policy publication, reserve-head activation, projection cutover and
  kill-switch release require distinct actors. A kill switch activates
  immediately; release requires a proposal, two independent reauthenticated
  approvals and a fresh PostgreSQL readiness proof for journal, projection,
  reserve, custody, anchor and the affected capability policy.
- Repository Policy Gate and Economy CI Gate are independent required checks. A
  branch-protection rule must prohibit administrator/force bypasses while either
  check is red.

## Consequences

- A configured Stripe Connect adapter is necessary but never sufficient to enable
  payout dispatch.
- Commercial decisions remain explicitly `blocked-by-configuration`; they are not
  inferred from code defaults.
- Legacy migration is an operational capability, not an activation default. Its
  signed policy must bind USD minor units 1:1 to `PurchasedHard`, and every
  capture, posting, approval and rollback remains tenant-scoped and auditable.
- The traceability matrix contains no `missing` technical requirement for this
  backend delivery. Architecture review may advance while provider credentials,
  live certification and Legal/KYC/AML authorization remain
  `blocked-by-configuration`.
- GitHub branch protection is remote repository administration, so its effective
  state must be verified during release readiness rather than represented by a
  local workflow file alone.

## Activation evidence

Activation of one capability requires a versioned evidence record containing the
jurisdiction allowlist entry, policy and reserve versions, provider certification,
the current kill-switch epoch, and the approving operational actors. Removing any
one predicate disables the capability without deleting financial evidence.

The public readiness states are `Disabled`, `Ready`, `InvalidPolicy`,
`JurisdictionBlocked`, `ComplianceUnavailable`, `ComplianceStale`,
`ReviewRequired`, `LedgerUnhealthy`, `ProjectionMismatch`,
`ReserveInsufficient`, `CustodyUnreconciled`, `AnchorInvalid`,
`ProviderNotReady` and `KillSwitchActive`.
