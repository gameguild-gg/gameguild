# ADR 2026-08-21: Economy production rollout remains fail-closed

Status: Accepted

## Context

The dual-currency architecture, threat model, and whitepaper describe a financial
platform whose capabilities must not become active through deployment defaults or
partial provider configuration. The application already exposes safe reads and a
small self-service payout surface, while the full operating model remains under
implementation.

## Decision

- Economy provider contracts remain domain-neutral. `IConnectPayoutProvider` is
  the payout boundary and Stripe Connect is the first adapter, never a domain
  dependency.
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
- Repository Policy Gate and Economy CI Gate are independent required checks. A
  branch-protection rule must prohibit administrator/force bypasses while either
  check is red.

## Consequences

- A configured Stripe Connect adapter is necessary but never sufficient to enable
  payout dispatch.
- Commercial decisions remain explicitly `blocked-by-configuration`; they are not
  inferred from code defaults.
- The architecture and threat model retain their review status until the
  traceability matrix contains no `missing` technical requirement.
- GitHub branch protection is remote repository administration, so its effective
  state must be verified during release readiness rather than represented by a
  local workflow file alone.

## Activation evidence

Activation of one capability requires a versioned evidence record containing the
jurisdiction allowlist entry, policy and reserve versions, provider certification,
the current kill-switch epoch, and the approving operational actors. Removing any
one predicate disables the capability without deleting financial evidence.
