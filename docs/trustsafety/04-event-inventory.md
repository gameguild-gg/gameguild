# TrustSafety — Event inventory

> Module `GameGuild.TrustSafety` · develop `ce31d5850c`. Shorthand `TS:`.

## 1. The `TrustSafetyEvent` record — a would-be integration contract

`TS:/PostgreSqlTrustSafetyControlPlane.cs:20-24` — the event the control plane consumes:

| Field | Role |
|---|---|
| `Id`, `EventId` | row id + idempotency key (unique index) |
| `TenantId`, `SubjectHash` | subject scope (hashes only — no raw user ids in the trust plane) |
| `Kind` | `Moderation=1, Report=2, MarketplaceAbuse=3, WashTrading=4, BountyCycling=5, AdFraud=6, AccountRestriction=7, Appeal=8` (`:11-15`) |
| `Version`, `PolicyVersion` | per-subject monotonic version; policy that produced the outcome |
| `Outcome` | Approved=1 / Rejected=2 / NeedsReview=3 / Unavailable=4 (`:17`) |
| `PayloadHash`, `EvidenceHash` | integrity pins (replay-different-payload detection; evidence join) |
| `RawObjectReference` | opaque pointer to the underlying object (≤2048) |
| `KeyId`, `Signature` | HMAC-style signature over the canonical form (`Canonicalize :285-291`), verified via the economy capability-policy verifier (`EconomyTrustSafetyEventSignatureVerifier :39-48`) |
| `IssuedAt`, `ExpiresAt`, `ReceivedAt` | lifetime triangle, DB check-constrained (`TrustSafetyPersistence.cs:23`) |

**Producers: zero.** No module constructs or dispatches `TrustSafetyEvent` (grep over `Source/` + `tests/` — only the record, the control plane, and unit tests of the control plane itself). The kind taxonomy (moderation/report/abuse…) describes an intended producer family that does not exist yet — the moderation lane (Assets) and the report lifecycles (Assets/Ratings/Courses) never emit anything into this plane.

## 2. Evidence publication (outbound, inside ingest)

A successfully processed event publishes a `ComplianceEvidenceEnvelope` with `EvidenceKind = ComplianceEvidenceKinds.TrustSafety` (`TS:/PostgreSqlTrustSafetyControlPlane.cs:171-177`) through `IComplianceEvidenceStore.IngestAsync` — the same durable evidence rail KYC and FinancialCrime use (docs/compliance-financialcrime/ §evidence). This is the module's only outbound effect; it is not a publish-subscribe event but a stored, versioned, expiring evidence document that the economy risk chain reads by kind.

## 3. Compliance holds (outbound side effect)

Non-Approved outcomes activate a `ComplianceHoldActivation` scoped `(tenantId, subjectHash)` with kind = event kind and the event's evidence hash (`:145-155`); Approved releases the active hold as the system actor `00000000-0000-0000-0000-000000000002` (`:61, 139-144`). Holds gate economy operations (the Economy package's compliance hold store) — i.e. this module is designed to be the **economy-blocking enforcement arm** for trust/safety verdicts, which is exactly why the missing producer (§1) matters.

## 4. Appeals as "events"?

`TrustSafetyAppeal` (`:28-32`) is a workflow row, not an event; the kind enum's `Appeal=8` suggests appeal *outcomes* were meant to ride the same evidence rail. Nothing does that today — appeal decisions stop at the row (`DecideAppealAsync` writes state, emits nothing).

## 5. What the moderation/report surfaces emit instead (adjacent census)

| Surface | Events emitted |
|---|---|
| Assets report/review | none (rows only; no domain events on report or review) |
| Ratings moderation | 5 declared domain events (`Ratings:/Events/RatingEvents.cs:8-56`) — **all declared-dead** (zero construction; see docs/social/ratings/04) |
| Auto-moderation lane | none (status transitions on the asset row) |

So the platform's trust/safety telemetry story is: **one designed-but-unwired evidence rail (this module) + three local row lifecycles that emit nothing.** Any dashboard promising "trust & safety events" today has no data source.
