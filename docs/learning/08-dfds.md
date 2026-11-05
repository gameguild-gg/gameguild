# Learning — Data-Flow Diagrams

> Learning family modules (`GameGuild.Learning.*`) · `develop` @ `ea4d1f91ce` (post-#1226 / post-#1275).
> L0 context + one L1 per major family (catalog/enrollment, assessment, certification, LTI). **Student PII is marked `[PII]`** on every flow that carries it — under FERPA these are education records (see the FERPA cross-ref in [`07-stride-threat-model.md`](07-stride-threat-model.md); the FERPA↔Learning handoff does not exist — `docs/compliance/10-cross-module-sequences.md:85-102`).
> Trust boundaries: **TB1** anonymous internet → API · **TB2** authenticated actor → API (JWT → `ActorContext`) · **TB3** external LTI platform ↔ API (the only external system; signed launches in, AGS scores out) · **TB4** API → PostgreSQL `public` schema · **TB5** mutation services → compliance `AuditLog` (same transaction, fail-closed) · **TB6** API → EVM blockchain node (PII-free digests only).
> Mermaid labels contain no parentheses and no colons (repo rendering constraint).

## L0 — Context

```mermaid
flowchart TD
    subgraph TB1["TB1 - anonymous internet"]
        VIS["Visitor<br/>catalog browser"]
        VER["Certificate verifier<br/>employer public"]
        LTIP["External LTI platform TB3<br/>Moodle Canvas class"]
    end
    subgraph TB2["TB2 - authenticated actors"]
        LEA["Learner"]
        AUT["Author - course Edit"]
        STA["Staff - Review or system admin"]
        ADM["System admin<br/>LTI deployments"]
    end
    API["GameGuild API<br/>Learning family - 31 controllers 12 sub-modules"]
    subgraph TB4["TB4 - PostgreSQL public schema"]
        DB[("Learning tables ~80<br/>programs program_users<br/>learning_enrollments<br/>Assessments runtime 17 tables<br/>certificates cohorts")]
    end
    subgraph TB5["TB5 - compliance store"]
        AUD[("AuditLog rows<br/>Learning dot asterisk")]
    end
    subgraph TB6["TB6 - EVM node"]
        CHAIN[("Blockchain anchor records<br/>SHA-256 digests only - PII-free")]
    end
    NOTIF["Notifications module<br/>in-app learner notices"]
    COM["Commerce module<br/>checkout entitlements"]
    TL["TestingLab module<br/>in-proc evidence events"]
    WALLET["Economy AiCredits wallet<br/>authoring AI budget"]
    VIS -->|"public catalog slug reads<br/>no PII out"| API
    VER -->|"verify by number anonymous<br/>verdict + PII-free anchor evidence"| API
    LTIP -->|"OIDC login + signed id_token<br/>name email claims [PII]"| API
    API -->|"AGS score passback<br/>score out only"| LTIP
    LEA -->|"enroll take assessments<br/>payloads answers [PII]"| API
    AUT -->|"author publish content<br/>AI runs"| API
    STA -->|"grade release override<br/>gradebook [PII]"| API
    ADM -->|"LTI deployment + line-item admin"| API
    API --> DB
    API -->|"audited mutations fail-closed"| AUD
    API -->|"anchor + verify digests"| CHAIN
    API -->|"grade-result-released"| NOTIF
    API -->|"monetized enrollment gate"| COM
    TL -->|"evidence completed<br/>ids only no session data"| API
    API -->|"quote reserve settle"| WALLET
```

## L1-A — Catalog & enrollment

Covers `ProgramCrudController` public/self-enroll surfaces, `EnrollmentModuleApi`, the cohort guard, and the commerce checkout handoff. The #1226 N6 ownership gates and the #1275 cohort guard are drawn at their enforcement points.

```mermaid
flowchart TD
    subgraph TB1a["TB1 anonymous"]
        VIS2["Visitor"]
    end
    subgraph TB2a["TB2 learner"]
        LEA2["Learner"]
        STA2["System admin"]
    end
    VIS2 -->|"GET courses-public GET slug"| PCR["ProgramCrudController<br/>published + public forced<br/>ProgramReadService L29-56"]
    LEA2 -->|"POST id-self-enroll<br/>userId from auth context"| PCR
    PCR -->|"unpublished or non-public -> 404<br/>window closed -> 409<br/>monetized -> 402"| LEA2
    PCR -->|"AddUserToProgramEndpointCommand"| PES["ProgramEnrollmentService"]
    LEA2 -->|"POST api-learning-enrollments<br/>body UserId [PII]"| EMC["EnrollmentModuleApi<br/>owner gate L445-450"]
    EMC -->|"CohortId present<br/>reserve seat first L149-160"| GUARD["ICohortEnrollmentGuard<br/>CanEnroll + increment<br/>fail-closed when unconfigured"]
    GUARD -->|"same transaction as enrollment row"| ENT[("learning_enrollments TB4 [PII]")]
    EMC -->|"owner-scoped progress status writes L474-505"| ENT
    PES -->|"program_users row"| PU[("program_users TB4 [PII]")]
    LEA2 -->|"checkout"| COM2["Commerce checkout TB2"]
    COM2 -->|"complete-course-checkout<br/>EntitlementId EnrollmentIds idempotent"| PCR
    EMC -->|"UserEnrolled audit same tx L163-170"| AUD2[("AuditLog TB5")]
    STA2 -->|"cross-user support reads<br/>IsSystemAdmin only L383-386"| EMC
```

PII on this diagram: `program_users` and `learning_enrollments` rows (userId, course, status, progress) are education records; the body `UserId` on enroll is identity data — now owner-gated post-#1275.

## L1-B — Assessment taking & grading

Covers the executable-definition runtime, the QuizAdapter redaction boundary, review stages, and the grade-release outbox. The answer-key redaction is the trust boundary **inside** the API between authoring material and learner delivery.

```mermaid
flowchart TD
    subgraph TB2b["TB2 actors"]
        LEA3["Learner - Learn capability"]
        INS["Instructor - StaffReview"]
    end
    AC["AssessmentsController<br/>Learn capability gates L485 524 1307 1576"]
    RT["AssessmentGradingRuntimeService<br/>start policy one attempt L1631-1639<br/>server timer L1643-1670"]
    RED["QuizDeliveryGenerator RedactEntry<br/>ANSWER-KEY BOUNDARY<br/>allowlist per item type L11-60"]
    SUB[("AssessmentSubmissions<br/>draft payload answers [PII] TB4")]
    REV["Review stages<br/>quiz deterministic instructor self peer AI"]
    GB["Gradebook projection<br/>per enrollment [PII]"]
    OBX["AcademicOutbox<br/>grade-result-released<br/>DLQ after 5 attempts L94 202-213"]
    LEA3 -->|"start attempt autosave submit<br/>answers [PII]"| AC
    AC --> RT
    RT -->|"redacted delivery only"| RED
    RED -->|"no correct-answer material"| LEA3
    RT --> SUB
    RT --> REV
    INS -->|"save review override regrade<br/>StaffReview gated"| REV
    REV --> GB
    GB --> OBX
    OBX -->|"AssessmentGraded notice"| NOTIF3["Notifications"]
    OBX -->|"AGS score passback<br/>final released only"| LTIP3["External LTI platform TB3"]
    INS -->|"gradebook read<br/>staff view or own enrollment L915-941"| GB
    LEA3 -->|"own released results only"| GB
```

PII: submission payloads (the student's answers), drafts, and the gradebook projection are education records. The `RED` node is the anti-cheating control for answer leakage (STRIDE SL4); the server-side timer at `RT` is the anti-timing-attack control.

## L1-C — Certification & EVM anchoring

Covers the eligibility gate (post-#1226 N1), audited issuance, and the two-layer verification model. The digest canonicalizer is the privacy boundary at TB6.

```mermaid
flowchart TD
    subgraph TB2c["TB2 actors"]
        LEA4["Learner recipient"]
        STA3["Staff - issue Create perm"]
        VER4["Public verifier TB1"]
    end
    ISS["CertificateService IssueCertificateAsync<br/>ELIGIBILITY GATE L52-99<br/>enrollment exists + belongs to recipient<br/>+ belongs to course + Completed"]
    CERT[("certificates TB4<br/>recipient name [PII]")]
    EPROD["CertificateEvmEvidenceProducer<br/>stages anchor-requested event"]
    EVM["BlockchainCertificateAnchoringAdapter<br/>canonicalizer SHA-256<br/>recipient name -> digest L16-18 79-87<br/>PII NEVER CROSSES"]
    CONSUMER["CertificateEvmPublicationConsumer<br/>durable journal + retry"]
    CHAIN2[("EVM chain records TB6<br/>digests tx ids only")]
    ISS -->|"audited tx fail-closed L143-148"| AUD4[("AuditLog TB5")]
    ISS --> CERT
    ISS --> EPROD
    EPROD --> CONSUMER
    CONSUMER --> EVM
    EVM --> CHAIN2
    ISS -->|"anchor best-effort never blocks issuance L566"| EVM
    STA3 -->|"POST issue - Create permission L138"| ISS
    LEA4 -->|"GET my - self scope L55-68<br/>GET id - recipient scope L73-89"| CERT
    VER4 -->|"GET verify number anonymous L96-98"| VRFY["VerifyCertificateAsync<br/>unknown number -> invalid + audited"]
    VER4 -->|"GET verify number anchor anonymous L120-122"| AVC["Anchor verification<br/>recompute digest then corroborate L92-144<br/>fail-closed Unreachable"]
    VRFY --> CERT
    AVC --> CERT
    AVC --> CHAIN2
    VRFY -->|"verdict only - no recipient PII"| VER4
    AVC -->|"digests tx block ts - PII-free"| VER4
```

PII: the certificate row holds the recipient name locally `[PII]`; **nothing PII crosses TB6** — the canonicalizer reduces the name to a SHA-256 digest before the adapter sees it. Verification responses to anonymous verifiers carry verdicts and PII-free evidence only.

## L1-D — LTI 1.3 (external platform trust boundary)

Covers the three anonymous protocol endpoints, the state/nonce fence, session minting, and outbound AGS. **TB3 is the hard trust boundary**: the platform is authenticated by its JWKS-signed `id_token`, not by network position.

```mermaid
flowchart TD
    subgraph TB3b["TB3 - external LTI platform - UNTRUSTED"]
        PLAT["Platform<br/>Moodle Canvas class"]
        JWKS["Platform JWKS endpoint"]
    end
    subgraph TB1b["TB1 - protocol endpoints anonymous by necessity"]
        LOGIN["POST lti-login L72<br/>iss client_id deployment_id must match L88-92<br/>login_hint [PII]"]
        LAUNCH["POST lti-launch L133<br/>id_token must be in body L137-140"]
    end
    subgraph APIB["API - TB2 side"]
        ST["LtiLaunchStateStore<br/>state + nonce SINGLE-USE L74-83"]
        CMD["LaunchLtiCommand<br/>RS256 vs platform JWKS L68-71<br/>sub nonce claims required L74-78"]
        MAP["LtiUserMapping<br/>by sub then email L86-101<br/>NO JIT provisioning"]
        JWT["IJwtTokenService<br/>GenerateAccessTokenAsync L103-107<br/>TokenVersion honored"]
        AGS["ILtiScorePassback<br/>AgsScoreService outbound only"]
        ADMR["Deployment + line-item admin<br/>IsSystemAdmin inline L192 215"]
    end
    LEA5["Learner browser<br/>platform iframe"]
    PLAT -->|"OIDC login init<br/>login_hint [PII]"| LOGIN
    LOGIN -->|"302 to platform auth<br/>https absolute credential-free L265-282"| PLAT
    PLAT -->|"signed id_token<br/>name email claims [PII]"| LAUNCH
    LAUNCH --> ST
    ST -->|"TryConsume - replay rejected"| CMD
    JWKS -->|"keys"| CMD
    CMD --> MAP
    MAP --> JWT
    JWT -->|"gg_session cookie SameSite None L176-184<br/>iframe-required accepted risk"| LEA5
    AGS -->|"score passback final released only"| PLAT
    OBX3["AcademicOutbox<br/>grade-result-released"] --> AGS
    ADMR --> PLAT
```

PII: the launch carries platform-asserted `login_hint`, `sub`, and typically `name`/`email` claims `[PII]`; the local mapping consumes them but **never writes a new account** (no JIT). Outbound AGS carries scores keyed by platform user ids — grade data is education record on the Learning side and leaves through this boundary only after release.

## Data-store sensitivity summary

| Store | PII / education record | Exposure surface | Controls |
|---|---|---|---|
| `program_users`, `learning_enrollments` | userId + course + status + progress | owner-scoped reads/writes; roster staff-gated (post-#1275) | SL2 gates; FERPA gate absent (N5) |
| `AssessmentSubmissions` + drafts | student answers | Learn capability + owner checks | redacted delivery, immutable post-submit |
| Gradebook projection | grades per enrollment | instructor `StaffReview` or own enrollment (`AssessmentsController.cs:915-941`) | SL5; FERPA gate absent |
| `certificates` | recipient name | recipient-scoped by-id; anonymous number-verify verdicts only | SL6 gates; PII-free anchoring |
| LTI launch claims | name/email from platform | transient; mapping only | no JIT; single-use state/nonce |
| TestingLab receipts | ids only (userId) | internal | no session content crosses (SL8) |
| EVM chain records | **none by construction** | public chain | SHA-256 canonicalizer at TB6 |
