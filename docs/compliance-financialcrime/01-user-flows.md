# FinancialCrime (AML) user flows — happy path and every edge case

> Module `GameGuild.Compliance.FinancialCrime` + host composition · Documented against `develop` commit `60100459e6`.
> Citation shorthand: `FC:` = `apps/api/Source/Modules/GameGuild.Compliance.FinancialCrime/`, `API:` = `apps/api/Source/GameGuild.API/`, `Risk:` = `apps/api/Source/Modules/GameGuild.Finance.Economy/Risk/`.

## 1. Screening ingestion — sanctions / PEP / adverse-media (DESIGNED; ingress has no caller)

The designed happy path: a screening provider (or an internal connector) delivers a signed screening event; `IngestScreeningAsync` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:86-135`) validates it, detects replays, enforces the contiguous per-subject version ladder, auto-opens an AML case + compliance hold when the outcome is anything but `Approved`, publishes the evidence envelope to the durable compliance ledger, and saves — all inside one `Serializable` transaction.

```mermaid
flowchart TD
    subgraph INGRESS["Screening ingestion - IngestScreeningAsync FC PostgreSqlFinancialCrimeControlPlane.cs"]
        A1["Screening record arrives<br/>provider environment providerEventId<br/>subjectHash version outcome L86"] --> A2{"ValidateScreening<br/>L493-513"}
        A2 -- "unsigned or bad lifetime" --> AX1["ArgumentException rejected<br/>SignatureVerified required L508-512"]
        A2 -- "valid" --> A3["Serializable transaction<br/>PostgreSqlTransactionExecutor L91-92"]
        A3 --> A4{"Replay by provider event<br/>ux provider env providerEventId L94-96"}
        A4 -- "exists" --> A5{"PayloadHash equal<br/>L99"}
        A5 -- "different" --> AX2["FinancialCrimeConflictException<br/>replayed with different evidence L101"]
        A5 -- "equal" --> A6["Return recorded screening + its case<br/>idempotent replay L104-105"]
        A4 -- "absent" --> A7{"Version equals max subject version + 1<br/>L107-113"}
        A7 -- "gap" --> AX3["FinancialCrimeConflictException<br/>versions must be contiguous L112"]
        A7 -- "contiguous" --> A8["ScreeningRow added L115-116"]
        A8 --> A9{"Outcome not Approved<br/>L118"}
        A9 -- "Rejected or NeedsReview or Unavailable" --> A10["OpenCaseAndHoldAsync<br/>case + compliance hold L120-122<br/>see flow 3 opening step"]
        A9 -- "Approved" --> A11["No case - clean screening L123"]
        A10 --> A12["Evidence envelope IngestAsync<br/>kind financial-crime L126<br/>see flow 5"]
        A11 --> A12
        A12 --> A13{"Published or Duplicate<br/>L127-130"}
        A13 -- "other status" --> AX4["FinancialCrimeConflictException<br/>evidence could not be published in sequence L129"]
        A13 -- "ok" --> A14["SaveChanges + return screening result<br/>L132-133"]
    end

    CALLER["CALLER FOR INGEST - NONE<br/>no product source calls IngestScreeningAsync<br/>gap FC1"] -.->|"designed only"| A1
```

### 1a. Edge-case matrix — screening ingestion

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Provider replays the same event id | replay lookup (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:94-96`) | recorded screening returned with its case; no second row |
| 2 | Provider replays with a different payload | payload-hash guard (`:99-102`) | `FinancialCrimeConflictException` — treated as evidence tampering |
| 3 | Out-of-order delivery (version gap) | contiguous-version guard (`:107-113`) | conflict exception; the provider must resend the missing version |
| 4 | Unsigned screening | `ValidateScreening` (`:508-512`) | rejected before any transaction — `SignatureVerified=true` is mandatory |
| 5 | Screening with `ExpiresAt <= IssuedAt` or `NextScreenAt <= IssuedAt` | lifetime validation (`:509`) | rejected |
| 6 | `ReceivedAt` predates `IssuedAt` (future-dated evidence) | validation (`:509`) | rejected |
| 7 | Non-approved outcome for a subject that already has an open case | `OpenCaseAndHoldAsync` reuse (`:402-407`) | existing active case returned; no duplicate case or hold |
| 8 | Evidence ledger rejects the envelope (out-of-sequence version) | ingestion status check (`:127-130`) | whole transaction rolls back — screening row and case never persist without evidence |
| 9 | Who calls this today | search at `60100459e6` | **nobody** — no controller, no worker, no webhook (`06-gaps.md` FC1) |

## 2. Transaction-monitoring signal → case (DESIGNED; ingress has no caller)

`RecordSignalAsync` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:137-174`) is the designed hook for transaction-monitoring scores (velocity, structuring patterns, anomaly detectors): a signal carries an idempotency key hashed to `RequestHash`, and any signal always opens (or reuses) a case + hold.

```mermaid
flowchart TD
    subgraph SIGNAL["Transaction signal - RecordSignalAsync L137-174"]
        S1["FinancialCrimeTransactionSignal<br/>tenantId subjectHash operationFingerprint<br/>signalType score evidenceHash idempotencyKey L28-31"] --> S2{"ValidateSignal<br/>L515-532"}
        S2 -- "score out of 0..1000000<br/>or hold not after observed" --> SX1["ArgumentException L528-531"]
        S2 -- "valid" --> S3["RequestHash = SHA256 idempotencyKey<br/>L142"]
        S3 --> S4["Serializable transaction L143-145"]
        S4 --> S5{"Replay by RequestHash<br/>unique index L146-147"}
        S5 -- "exists" --> S6{"Same fingerprint and evidence<br/>L150-152"}
        S6 -- "different" --> SX2["FinancialCrimeConflictException<br/>idempotency key replayed with different inputs L152"]
        S6 -- "same" --> S7["Return existing case - idempotent L155-157"]
        S5 -- "absent" --> S8["OpenCaseAndHoldAsync<br/>reason = signalType hold expires at HoldExpiresAt L166-168"]
        S8 --> S9["SignalRow linked to case L169-171"]
        S9 --> S10["SaveChanges return case L171-172"]
    end

    PRODUCER["CALLER FOR SIGNALS - NONE<br/>no monitoring producer exists<br/>gap FC1"] -.->|"designed only"| S1
```

### 2a. Edge-case matrix — transaction signal

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Same idempotency key sent twice with identical inputs | request-hash replay (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:146-158`) | existing case returned; one signal row |
| 2 | Same idempotency key, different fingerprint/evidence | input-match guard (`:150-152`) | conflict exception — key reuse across different operations is a bug/tamper signal |
| 3 | Two different signals for the same subject with an open case | `OpenCaseAndHoldAsync` reuse (`:402-407`) | both signals attach to the same case |
| 4 | Signal arrives after the hold expired but case still open | no state guard on the case in this path (`:166-168`) | case reused; hold reactivation is not attempted (hold TTL was set by the first opener) |
| 5 | Who calls this today | search at `60100459e6` | **nobody** (`06-gaps.md` FC1) |

## 3. AML case review — the LIVE admin flow

The only reachable flow: an analyst with `economy:operate:compliance` lists cases, opens details, self-assigns, and decides. Route base `api/v{v}/admin/economy/compliance` (`API:/Core/Controllers/EconomyComplianceAdministrationController.cs:36`).

```mermaid
flowchart TD
    subgraph ADMIN["AML case management - EconomyComplianceAdministrationController"]
        C0["GET financial-crime cases<br/>EconomyComplianceAdministrationController.cs L46"] --> C1{"TryActor<br/>OperateCompliance permission L232-243"}
        C1 -- "no permission" --> CX1["403 Forbid"]
        C1 -- "ok" --> C2["financialCrime ReadCasesAsync<br/>tenant state take L322-353"]
        C2 --> C3["200 case list ordered by UpdatedAt desc"]
        C4["GET financial-crime cases caseId<br/>L58"] --> C5["ReadCaseDetailsAsync<br/>case + events + decisions + regulatory refs L355-396"]
        C5 -- "not found" --> CX2["404 L71-74"]
        C6["POST cases caseId assignment<br/>L77 body ExpectedVersion"] --> C7["AssignFinancialCrimeCaseEndpointCommand<br/>EconomyComplianceEndpointCommands.cs L14-15"]
        C7 --> C8["EconomyComplianceEndpointCommandHandler<br/>L51 L61-64 to AssignCaseAsync"]
        C8 --> C9{"Case not Closed<br/>version equals expectedVersion<br/>FC L185-188"}
        C9 -- "stale or closed" --> CX3["409 FinancialCrimeConflictException L97-100"]
        C9 -- "ok" --> C10["State Assigned version++<br/>case event Assigned appended L190-194"]
        C11["POST cases caseId decisions<br/>L103 body outcome reasonCode evidenceHash expiresAt"] --> C12["Controller reads case details<br/>for SubjectHash L115-121"]
        C12 --> C13["DecideFinancialCrimeCaseEndpointCommand<br/>L131-133"]
        C13 --> C14["DecideCaseAsync FC L200-253"]
        C14 --> C15{"Assigned to this decider<br/>case open at expected version<br/>decision version contiguous L210-219"}
        C15 -- "no" --> CX4["409 conflict L140-143"]
        C15 -- "yes" --> C16{"Outcome Approved<br/>L225"}
        C16 -- "yes" --> C17["Case Closed ClosedAt set<br/>hold ReleasedAsync L226-229"]
        C16 -- "Rejected or NeedsReview" --> C18["Case NeedsReview<br/>hold stays L232-233"]
        C17 --> C19["Decision evidence envelope published<br/>provider internal-financial-crime L236-247"]
        C18 --> C19
        C19 --> C20["201 decision returned L134"]
        C21["POST cases caseId regulatory-references<br/>L146 body Kind SAR or STR JurisdictionCode ReferenceHash"] --> C22["RecordRegulatoryReferenceEndpointCommand<br/>L157-159"]
        C22 --> C23{"Kind is SAR or STR<br/>FC L291-294"}
        C23 -- "other" --> CX5["ArgumentOutOfRangeException"]
        C23 -- "ok" --> C24["Reference row - hash only no narrative<br/>L298-304"]
        C24 --> C25["204 No Content"]
    end
```

### 3a. Edge-case matrix — case management

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Analyst without `OperateCompliance` | `TryActor` gate (`API:/Core/Controllers/EconomyComplianceAdministrationController.cs:232-243`) | 403 on every endpoint, reads included |
| 2 | Concurrent assignment with stale `ExpectedVersion` | optimistic-version guard (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:185-188`) | 409 — no lost update |
| 3 | Decision by an analyst the case is not assigned to | assignee guard (`:210-213`) | 409 — decisions require self-assignment first |
| 4 | Decision version skipping | contiguous decision versions (`:215-219`) | 409 conflict |
| 5 | Second Approved decision after close | closed-state guard (`:185, :210`) | 409 — closed cases accept nothing |
| 6 | Decision evidence ledger rejects the envelope | status check (`:245-248`) | whole decision transaction rolls back — case state and decision stay untouched |
| 7 | Regulatory reference with a kind other than SAR/STR | kind whitelist (`:291-294`) | rejected — the only two report kinds the schema understands |
| 8 | SAR reference content itself | hash-only storage (`:298-304`) | only `ReferenceHash` is stored; the narrative lives with the FIU, never here |
| 9 | Case with zero rows today | `ReadCasesAsync` (`:322-353`) | 200 `[]` — because nothing can open a case (FC1) |
| 10 | Rate limiting on these endpoints | none on the controller (`:35-44`) | no policy — `06-gaps.md` FC7 (mirrors compliance N8) |

## 4. Decision consumption at the money path (DESIGNED; no caller)

An `Approved` case decision is a scarce, single-use, windowed release: `ConsumeDecisionAsync` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:255-284`) burns it exactly once for one operation fingerprint inside `[IssuedAt, ExpiresAt)`.

```mermaid
flowchart TD
    subgraph CONSUME["Decision consumption - ConsumeDecisionAsync L255-284"]
        D1["ConsumeDecisionAsync<br/>tenantId decisionId operationFingerprint consumedAt"] --> D2["Serializable transaction L265-267"]
        D2 --> D3{"Decision exists in tenant<br/>L268-270"}
        D3 -- "no" --> DX1["KeyNotFoundException"]
        D3 -- "yes" --> D4{"Approved only<br/>IssuedAt less than now less than ExpiresAt<br/>never consumed before L271-274"}
        D4 -- "any check fails" --> DX2["FinancialCrimeConflictException not consumable L274"]
        D4 -- "ok" --> D5["Consumption row with operation fingerprint<br/>unique single index L277-281"]
        D5 --> D6["SaveChanges - decision burned L282"]
    end

    CALLER2["CALLER FOR CONSUMPTION - NONE<br/>no value-movement path burns decisions today<br/>gap FC1"] -.->|"designed only"| D1
```

### 4a. Edge-case matrix — decision consumption

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Replay of the same consumption | consumed-before guard + unique index (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:271-272`, `FC:/FinancialCrimePersistence.cs:194`) | conflict — one release per decision |
| 2 | Consumption at/beyond `ExpiresAt` | window guard (`:271`) | conflict — expired approvals cannot release anything |
| 3 | Non-approved decision consumed | outcome guard (`:271`) | conflict by design — only `Approved` is consumable |
| 4 | Who calls this today | search at `60100459e6` | **nobody** — the hold-release consumption leg of the design is dormant (`06-gaps.md` FC1) |

## 5. Evidence publication → risk-gate read side (LIVE read side)

Every screening ingestion and every case decision publishes a `ComplianceEvidenceEnvelope` with `EvidenceKind = "financial-crime"` through `IComplianceEvidenceStore` (`FC:/PostgreSqlFinancialCrimeControlPlane.cs:126, 236-247`); the money path reads the latest row of that kind through `PostgreSqlFinancialCrimeRiskInputSource` (`FC:/FinancialCrimeModule.cs:43-62`).

```mermaid
flowchart TD
    subgraph READSIDE["Risk input read side - PostgreSqlFinancialCrimeRiskInputSource FC FinancialCrimeModule.cs"]
        R1["Protected operation requests risk decision<br/>Risk PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs L182-191"] --> R2{"Tenant-scoped read<br/>ReadAsync tenantId subject L43"}
        R2 -- "tenantless overload used" --> RX1["Always Unavailable<br/>financial-crime-tenant-context-required L34-41"]
        R2 -- "tenant present" --> R3["ReadLatestAsync kind financial-crime<br/>L51-52"]
        R3 -- "no row ever ingested" --> RX2["Unavailable financial-crime-evidence-unavailable<br/>version 1 expires in 1 minute L53-54 L64-70"]
        R3 -- "row exists" --> R4{"SignatureVerified and<br/>EvidenceHash present L61"}
        R4 -- "no" --> RX3["IsAuditable false - evidence unusable"]
        R4 -- "yes" --> R5["Map Approved Allow Rejected Deny<br/>NeedsReview Review else Unavailable L72-78"]
        R5 --> R6["Assess in issuer L81-87<br/>Unavailable or stale - ComplianceUnavailable reject<br/>Deny - Denied reject"]
        RX2 --> R6
        RX3 --> R6
        RX1 --> R6
    end
```

### 5a. Edge-case matrix — read side

| # | Edge case | Code path | Outcome |
|---|-----------|-----------|---------|
| 1 | Evidence exists but expired | freshness check (`Risk:/EconomyProtectedRiskDecisionIssuerSupport.cs:22-38`) | `ComplianceStale` — deny (fail-closed) |
| 2 | Evidence result `NeedsReview` | assess (`:41-49`) | `Review` outcome → risk review case submitted (`Risk:/PostgreSqlEconomyProtectedOperationRiskDecisionIssuer.cs:145-150`) |
| 3 | Evidence `Allow` but signature unverified | `IsAuditable` flag (`FC:/FinancialCrimeModule.cs:61`) | evidence inadmissible — `RequireFreshAllow` throws (`Risk:/ExternalRiskEvidence.cs:67-71`) on conversion paths |
| 4 | Evidence row today | table state | none — ingestion has no caller (FC1), so every subject is `Unavailable` |
