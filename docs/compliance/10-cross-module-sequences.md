# Compliance Cross-Module Sequences — who feeds the evidence store, who consumes the gates

Status: living document, current as of 2026-11-04 against `origin/develop` (21b58ac6a7). This is the package's fifth functional artifact (the tracker's *Sequências* column).

Citation shorthand as in [`README.md`](README.md). Mermaid `sequenceDiagram` labels carry file:line as `L###`; no parentheses or colons inside message text beyond the `file Lnnn` form.

## 1. AuthenticationAuditEvent — producer to central store (transactional for MFA)

```mermaid
sequenceDiagram
    participant AS as AuthAttemptService - Authentication
    participant SINK as CentralAuthenticationAuditEventSink - Audit
    participant PIPE as SecurityEventLogger - Audit
    participant DB as AuditLogs plus SecurityAlerts

    AS->>SINK: RecordAsync AuthenticationAuditEvent
    SINK->>SINK: MFA action types require transactional persistence L18-19
    SINK->>PIPE: CaptureAsync risk mapped from authentication risk L36-43
    PIPE->>PIPE: Taxonomy.Classify then 3 DB attempts
    PIPE->>DB: persist + AlertRuleEvaluator same context
    alt persistence failed and MFA required
        SINK-->>AS: throw - the owning command fails L50-53
    else persistence failed non-MFA and spooling enabled
        PIPE->>PIPE: PII-sanitized spool append L246-253
        Note over PIPE: SpoolDrainerBackgroundService replays idempotently by event id L125-129
    end
```

Citations: sink binding `Audit:/Extensions/AuditModule.cs:135-147`; producer-side usage `Identity.Authentication/Services/AuthAttemptService.cs`, `MfaAttemptTrackingService.cs`; interface `Identity.Authentication/Abstractions/IAuthenticationAuditEventSink.cs:7`.

## 2. ApiKeyAuditEvent — best-effort lifecycle trail

```mermaid
sequenceDiagram
    participant KC as ApiKeyCommands - Authentication
    participant SINK as CentralApiKeyAuditEventSink - Audit
    participant AUD as AuditService
    participant DB as AuditLogs

    KC->>SINK: RecordAsync create rotate revoke L61 L199 L333
    SINK->>AUD: LogAsync fixed Medium risk L29
    AUD->>DB: 2 attempts
    alt both attempts fail
        AUD-->>SINK: swallowed - never throws L35-38
        Note over SINK,DB: fail-open by design - key material is never logged L8-9
    end
```

## 3. Payment events — the stream that does not arrive (K2)

```mermaid
sequenceDiagram
    participant PAY as Commerce.Payments RevenueAuditService
    participant LOCAL as audit_trails table - module-local
    participant BILL as Commerce.Billing RotateWebhookSecret
    participant CENTRAL as IAuditService - Compliance.Audit

    PAY->>LOCAL: RecordAuditTrailAsync refund chargeback success
    Note over PAY,LOCAL: money movement never crosses into the central family
    BILL->>CENTRAL: LogAdminActionAsync webhook-secret rotation only L126-141
    Note over CENTRAL: retention custody and security aggregation do not cover payments
```

The only payment-family signal that reaches this module is Billing webhook-secret rotation (`Billing:/Commands/RotateWebhookSecret/RotateWebhookSecretCommand.cs:126-141`); everything money-shaped stays module-local (`docs/commerce/payments/06-gaps.md` N15). Any regulator asking for a unified money-movement trail across Commerce and Compliance cannot be served today.

## 4. Consent bridge — audit rides, consent writes proceed on audit failure

```mermaid
sequenceDiagram
    participant CC as ConsentController - dead at runtime N1
    participant CS as ConsentService - Consent
    participant SINK as CentralConsentAuditEventSink - Audit
    participant PIPE as SecurityEventLogger

    CC->>CS: grant revoke submit process
    CS->>CS: write UserConsent row
    CS->>SINK: RecordAsync Consent.Granted Revoked DSAR events L125-131
    SINK->>PIPE: CaptureAsync
    alt sink throws
        CS-->>CS: swallowed L141-162 - the consent write stands unaudited
    end
    Note over CC,CS: at develop 21b58ac6a7 every endpoint 500s on DI resolution - N1 - PR 1268 open
```

## 5. FERPA ↔ Learning — the handoff that does not exist

Verified by search at 21b58ac6a7: **no module under `Modules/GameGuild.Learning*` references FERPA**, and nothing outside `Modules/GameGuild.Compliance.FERPA/` references it either. The only education-record ingress is FERPA's own command:

```mermaid
sequenceDiagram
    participant FC as FerpaController
    participant FS as FerpaService
    participant LMOD as Learning modules
    participant DB as ferpa_ tables

    FC->>FS: RegisterRecord command
    FS->>DB: insert education record - nothing else consults it
    LMOD->>LMOD: learning progress grades certificates
    Note over LMOD,DB: no bridge - Learning outcomes that are education records under FERPA never reach the FERPA disclosure gate
```

Consequence for the regulatory lens: disclosures logged by FERPA cover only records someone manually registered; the platform's actual education data (Learning) is invisible to the FERPA machinery. This is a structural finding for any FERPA audit — registered as a companion to N5/N6, tracked nowhere yet.

## 6. KYC — two ledgers, two gates, zero bridges (N7)

The divergence points, exactly:

```mermaid
sequenceDiagram
    participant SUM as SumSub provider
    participant HOOK as EconomySumSubWebhookController - anonymous HMAC
    participant ORCH as SumSubKycAmlOrchestrator - KYC
    participant EV as Economy compliance evidence ledger
    participant LEG as KycService legacy ledger
    participant GATE as KycAndHoldSignInCompliancePolicy
    participant PAYOUT as ComplianceKycPayoutEligibilitySource

    SUM->>HOOK: signed webhook
    HOOK->>ORCH: IngestSumSubWebhookEndpointCommand L121-143
    ORCH->>ORCH: verify HMAC archive raw to S3 inbox dedupe L158-178
    ORCH->>EV: PublishAsync versioned evidence L268-293
    Note over ORCH,EV: approval visible ONLY to the economy tier

    GATE->>LEG: GetLatestVerificationAsync L53
    LEG-->>GATE: legacy status - deny challenge approve L68-77
    Note over GATE,LEG: sign-in decisions read the legacy ledger only

    PAYOUT->>LEG: GetLatestVerificationAsync L22
    LEG-->>PAYOUT: Approved plus not soft-deleted plus not expired L36-37
    Note over PAYOUT,LEG: payout eligibility ALSO reads the legacy ledger
```

Divergence points, stated plainly:

1. A user approved by SumSub has a compliant evidence envelope in the economy tier — and is `VerificationMissing` to the sign-in gate (challenge) and `missing` to payout eligibility (unavailable).
2. A user approved in the legacy ledger satisfies both gates — with no signed evidence, no transition control (N9) and a stubbed document rail (N11).
3. The sign-in gate is disabled by default (K8, `API:/Core/Setup/ApiProductComposition.cs:120-125` region), so today the divergence's live blast radius is the payout gate only — which reads the WEAKER ledger while the stronger tier sits idle. `ComplianceKycPayoutEligibilitySource.cs:22` calling `KycService` (not the evidence store) is the exact line where the strong tier is bypassed.

## 7. Entity-change capture — the silent fourth stream

```mermaid
sequenceDiagram
    participant APP as any audited mutation SaveChanges
    participant ICE as IEntityChangeAuditStore host bridge
    participant RED as PiiRedactionService
    participant DB as EntityChangeAudit

    APP->>ICE: EF change-tracking capture
    ICE->>RED: redact through central pipeline
    ICE->>DB: persist transactional with the mutation
    Note over ICE,DB: binding at API/Core/Setup/InfrastructureLayerExtensions.cs L416
```

## 8. Sequence-level verdict

| Sequence | Delivery contract | Regulatory gap |
|----------|-------------------|----------------|
| 1 authentication | fail-closed for MFA subset, spool-degraded otherwise | N13 terminal loss |
| 2 API keys | fail-open best-effort | none material |
| 3 payments | **does not exist** | K2 — no unified money trail |
| 4 consent | fail-open audit, dead wiring | N1 live, N2, N4 |
| 5 FERPA↔Learning | **does not exist** | education records unregistered |
| 6 KYC gates | split-brain reads | N7, N9, N11 |
| 7 entity-change | transactional, redacted | none material |
