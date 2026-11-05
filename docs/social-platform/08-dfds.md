# Social + Platform — Data Flow Diagrams

> Social family (9 sub-modules) + Platform family (13 sub-modules) · Documented against `develop` commit `ce31d585` (citations re-verified at HEAD).
> **L0 context + one L1 per STRIDE family** (the twelve families of [`07-stride-threat-model.md`](07-stride-threat-model.md): C1–C6 Social, P1–P5 Platform). Notation follows the repo standard (`docs/authorization/08-dfds.md` §1): external entities as rectangles, processes as stadiums, stores as cylinders, **trust boundaries as subgraphs**. Mermaid labels carry no parentheses and no colons.
> PII flows are marked **[PII]** on the edges where personal data crosses a boundary. STRIDE cross-reference: each family's threat table lives in 07 §2–§13.

Citation shorthand as in [`01-user-flows.md`](01-user-flows.md).

## 1. Level 0 — context diagram

```mermaid
flowchart LR
    USER["Authenticated member"]
    ANON["Anonymous visitor"]
    ADMIN["Tenant or system admin"]
    MOD["Human moderator - admin role"]
    PROV["AI providers - OpenAI Anthropic Google"]
    OPEN["Openinary - asset CDN plus vLLM classification"]
    CLAM["ClamAV daemon - optional"]

    API(["0.0 GameGuild API - Social plus Platform modules"])

    PG[("PostgreSQL - content profiles scores credits assets")]
    STORE[("Asset binaries - object storage")]
    NOTIF[("Notifications delivery")]
    CDNOUT["CDN edge - HMAC token URLs"]

    USER -->|"posts comments feeds reactions profiles projects sessions - Bearer"| API
    ANON -->|"public post read - profile by handle - share-view - CDN tokens"| API
    ADMIN -->|"group membership - dashboards - jam admin - blog admin"| API
    MOD -->|"moderation queue review - report review"| API
    API -->|"chat generate streams - [PII] prompts"| PROV
    API <-->|"asset bytes upload - classify - transform"| OPEN
    API -->|"file bytes stream scan"| CLAM
    API -->|"content profiles [PII] scores wallets moderation"| PG
    API <-->|"approved binaries read write purge"| STORE
    API -->|"publication announcements - best effort"| NOTIF
    CDNOUT -->|"signed asset bytes to any holder of token"| USER
    API -->|"HMAC path tokens issued"| CDNOUT
```

Trust boundaries on L0:

| ID | Boundary | Crossing flows | Notes |
|---|---|---|---|
| **TB1** | user/browser → web client | all rendering | first-party render escapes by default; only JSON-LD uses `dangerouslySetInnerHTML` with `<` escaping (`Web:lib/seo/jsonld.tsx:235-242`) |
| **TB2** | client → API host | `[Authorize]` / `[AllowAnonymous]` per controller; the middleware chain (`docs/authorization/cross-module-sequences.md` §1) | anonymous in-scope reads: post GET + comments GETs, share/view/statistics, profile `@{handle}` + search, public blog, TestingLab public session list, CDN token routes |
| **TB3** | API → PostgreSQL | content, scores, credits, moderation states | the tamper surface; N10 is the live cross-tenant instance |
| **TB4** | API → moderation lane | virus scan, classification queue | machine trust; fail-closed per-scanner, fail-open per-feature (07 §8 SP2) |
| **TB5** | API → CDN/asset delivery | HMAC-SHA256 path tokens, `[AllowAnonymous]` token-gated | 09 §5 |
| **TB6** | API → AI providers | prompts [PII], streams, usage tokens | DI-default HttpClient (100 s implicit), 07 §12 / 10 §2 |
| **TB7** | tenant A → tenant B | id-addressed reads/writes | N10 class; 09 §6 |

## 2. L1 — C1 post lifecycle and moderation truth

```mermaid
flowchart TD
    subgraph CLIENT["TB1-TB2 client"]
        A1["Author"]
        A2["Reader - anonymous allowed on GET"]
    end
    subgraph SOCIALAPI["TB3 Social.Posts"]
        P1(["1.1 CreatePost - author from token L133-137"]) --> P2{"1.2 Media attached L141"}
        P2 -->|"yes"| P3(["1.3 ResolveReadyOwnedInTenantAsync - owned plus ready plus tenant L142-154"])
        P3 -->|"not ready or foreign"| P4(["1.4 400 Media is unavailable"])
        P3 -->|"ready"| P5(["1.5 CreatePostEndpointCommand - raw content L155-161"])
        P2 -->|"no"| P5
        P5 --> P6(["1.6 RebindAssetParentCommand L166-173"])
        P7(["1.7 Visibility check on read - own or Public or Followers-and-following"]) 
        P8(["1.8 Soft delete - DeletedAt L204"])
    end
    subgraph STORES["TB3 stores"]
        D1[("Post rows - no moderation status")]
        D2[("PostComment rows")]
        D3[("PostStatistics counters - anonymous view share inflow SP3")]
    end
    subgraph MODLANE["TB4 - absent for text"]
        X1["NO text moderation queue - TrustSafety disabled Host ApiProductComposition L113-115 - gap N1"]
    end
    A1 -->|"POST content [PII]"| P1
    A2 -->|"GET by id"| P7
    P5 --> D1
    P6 --> D1
    P7 --> D1
    A2 -->|"like pin repost follow"| D3
    X1 -.->|"never invoked"| P5
```

Flows of note: 1.5 carries raw content across TB3 with no sanitization or moderation hop (07 §2 T verdict); 1.3 is the only security gate on the path and it covers media, not text.

## 3. L1 — C2 feed assembly

```mermaid
flowchart TD
    subgraph CLIENT["TB2 client"]
        V1["Viewer - authenticated"]
    end
    subgraph FEEDAPI["TB3 Social.Feed"]
        F1(["2.1 SocialFeedController.GetAsync L60"]) --> F2(["2.2 Load blocks both directions L413-425"])
        F2 --> F3(["2.3 Load active mutes with expiry L72-77"])
        F3 --> F4(["2.4 excludedAuthors union L78"])
        F4 --> F5{"2.5 scope L87"}
        F5 -->|"ForYou"| F6(["2.6 visibility-filtered posts L332-335"])
        F5 -->|"Following"| F7(["2.7 followed authors only L327-329"])
        F5 -->|"Saved"| F8(["2.8 SavedPost join L286-320"])
        F5 -->|"Community"| F9(["2.9 posts skipped L87"])
        F6 --> F10(["2.11 inject TestingSessions Scheduled or Active L100-107 L359-387"])
        F10 --> F11(["2.12 keyset order SortAt desc - take clamp 1-30 L58 L109-114"])
        F11 --> F12(["2.13 FeedProjection.ProjectAsync L440-455"])
        F12 --> F13(["2.14 opaque cursor page L118-121"])
        F14(["2.15 legacy inbox FeedModuleApi L85 - single self-producer N3"])
    end
    subgraph STORES["TB3"]
        E1[("Post + Block + Mute + Follow rows")]
        E2[("FeedItem inbox - write-starved")]
        E3[("TestingSession rows - cross-family read")]
    end
    V1 -->|"GET scope plus cursor"| F1
    F2 --> E1
    F6 --> E1
    F10 --> E3
    F13 -->|"page dto [PII] author handles"| V1
    F14 --> E2
```

Flows of note: there is **no ranking hop anywhere** — 2.11→2.13 is straight visibility-filter + merge + keyset sort; the only manipulation-resistant property is that there is nothing to manipulate yet (07 §3). The exclusion set built at 2.2 has no term over `RepostOfPostId` (07 SP4).

## 4. L1 — C3 group membership

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        G1["Tenant admin - the ONLY operator N4"]
    end
    subgraph GROUPSAPI["TB3 Social.Groups"]
        H1(["3.1 membership endpoints - TenantAdmin policy plus EnsureCanManageGroupAsync L751-845"]) --> H2{"3.2 JoinSocialGroupCommand - target UserId from body"}
        H2 --> H3(["3.3 group Active check L391-394"])
        H3 --> H4{"3.4 existing membership L397-421"}
        H4 -->|"Active or Pending"| H5(["3.5 idempotent return"])
        H4 -->|"Rejected Removed none"| H6(["3.6 Request or RequestAgain L403-434"])
        H6 --> H7{"3.7 group Visibility"}
        H7 -->|"Public"| H8(["3.8 auto-Active L412-415"])
        H7 -->|"Private InviteOnly"| H9(["3.9 Pending - approve requires Pending L451"])
        H8 --> H10(["3.10 auth token version bump - membership cache L417-420"])
        H11(["3.11 role change - Owner Admin Moderator Member L822"])
        H12(["3.12 group status blind setter L735-748 - D8"])
    end
    subgraph STORES["TB3"]
        I1[("SocialGroup + SocialGroupMember")]
    end
    G1 -->|"join approve reject role leave - target user id in body [PII]"| H1
    H6 --> I1
    H9 --> I1
    H11 --> I1
    H12 --> I1
```

Flows of note: every membership transition enters at 3.1 — there is no member-self-service edge from a non-admin principal (07 §4 E verdicts); 3.12 accepts any from-state.

## 5. L1 — C4 blog publish

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        B1["Primary author"]
        B2["System admin"]
    end
    subgraph BLOGAPI["TB3 Social.Blog"]
        L1(["4.1 CreateBlogPostCommand - Draft"]) --> L2(["4.2 edits plus coauthor management - primary enforced"])
        L2 --> L3(["4.3 PublishBlogPostCommand L231"])
        L3 --> L4{"4.4 already Published L198-201"}
        L4 -->|"no"| L5(["4.5 BlogAssetManifestService.ValidatePublishAssetsAsync L199-203 - every body asset-uri Approved plus clean - fails closed"])
        L5 -->|"violation"| L6(["4.6 publish refused naming asset id"])
        L5 -->|"clean"| L7(["4.7 post.Publish L224"])
        L7 --> L8(["4.8 announcer.AnnounceBlogPublishedAsync L228 - leaves TB3 see doc 10"])
        L9(["4.9 AdminUnpublish or AdminDelete - SystemAdmin policy BlogAdminController L19 L47 L68"])
        L10(["4.11 BlogAuthoringAiService run - credit-bounded see P5"])
    end
    subgraph STORES["TB3"]
        M1[("BlogPost + coauthors")]
        M2[("AssetContent - moderation state read cross-family")]
    end
    B1 -->|"draft body [PII] plus markup"| L1
    L5 --> M2
    L7 --> M1
    B2 --> L9
    L9 --> M1
    L10 --> M1
```

Flows of note: 4.5 is the only publish gate and it is media-only — body **text** has no moderation hop (N1, 07 §5); 4.8 crosses into the announcements bridge (TB3→Notifications, doc 10 §1).

## 6. L1 — C5 reactions and engagement

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        R1["Authenticated reactor"]
        R2["Anonymous - share view statistics SP3"]
    end
    subgraph REACTAPI["TB3 Social.Reactions plus Posts engagement"]
        S1(["5.1 SetReaction or RemoveReaction - actor from context ReactionModuleApi L208-264"]) --> S2(["5.2 ReactionRepository upsert"])
        S3(["5.3 TogglePostLike Pin Repost - endpoint commands PostInteractionsController L31-83"]) --> S4(["5.4 PostEngagementService counters"])
        S5(["5.5 RecordView plus Share - AllowAnonymous L83-84 L96-97 - no dedup N9 SP3"])
        S6(["5.6 CreateRepost - new row RepostOfPostId link Post.cs L50-58 - no exclusion term SP4"])
    end
    subgraph STORES["TB3"]
        T1[("Reaction rows - actor-bound")]
        T2[("PostStatistics - public read-back GET statistics L119-120")]
    end
    R1 --> S1
    R1 --> S3
    R2 --> S5
    S2 --> T1
    S4 --> T2
    S5 --> T2
    S6 --> T2
```

## 7. L1 — C6 profile exposure [PII]

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        U1["Profile owner - authenticated"]
        U2["Anonymous visitor - handle known or guessed"]
    end
    subgraph PROFILEAPI["TB3 Social.Profiles"]
        V1(["6.1 Upsert privacy skills portfolio - IsActor or OwnsProfile guards L47 L59 L73 L87 L105 L122 L139"]) --> V2[("6.2 SocialProfile store")]
        V3(["6.3 GET by handle - AllowAnonymous L34-37 - repository filter is handle plus not-deleted ONLY Repositories L52-58 - SP1"])
        V4(["6.4 GET by user id - Authorize L19-21 - same unfiltered read L47-50 - SP1"])
        V5(["6.5 Search - AllowAnonymous L39-42 - Public-only filter L60-63"])
        V6(["6.6 ToDto - all fields incl Location WebsiteUrl SocialLinksJson Entities L180-205"])
    end
    V2 --> V6
    V3 --> V6
    V4 --> V6
    V6 -->|"full dto [PII] - Location TimeZone WebsiteUrl SocialLinksJson Bio"| U2
    V5 -->|"public profiles only"| U2
    U1 -->|"self-disclosed PII write [PII]"| V1
```

Flows of note: the [PII] edge 6.6→anonymous visitor is the SP1 leak — the privacy switch (`ProfileVisibility`, `ShowActivity/ShowPortfolio/ShowSkills`) is stored but the by-handle and by-user read predicates never consult it; only 6.5 (search) does.

## 8. L1 — P1 asset upload → moderation

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        W1["Uploader - Authorize - 100 MB limit AssetsController L29"]
        W2["Moderator - RequireAdminRole AssetsAdminController L20"]
    end
    subgraph ASSETSAPI["TB3-TB4 GameGuild.Assets"]
        X1(["7.1 UploadAssetCommand L60-66"]) --> X2[("7.2 AssetContent row - VirusScan Pending Moderation Pending L124")]
        X3(["7.3 VirusScanBackgroundService polls"]) --> X4{"7.4 VirusScanService - local policy plus optional ClamAv"}
        X4 -->|"Enabled false - clean pass SP2"| X5(["7.5 VirusScanStatus Clean"])
        X4 -->|"infected"| X6(["7.6 Infected quarantined"])
        X4 -->|"ClamAV outage - FailClosedWhenScannerUnavailable true L95"| X7(["7.7 rejected SCANNER_UNAVAILABLE"])
        X5 --> X8(["7.8 ModerationClassificationWorker - batch 10 - exits when disabled L15-40 N13"])
        X8 --> X9(["7.9 CAS claim TryBeginModeration Processor L47-50"])
        X9 --> X10{"7.10 image and Clean L37-45"}
        X10 -->|"non-image"| X11(["7.11 HandleNonImageAsync - policy without classifier"])
        X10 -->|"image"| X12(["7.12 classifier.ClassifyAsync - default UnresolvedModerationClassifier fails closed"])
        X12 --> X13(["7.13 Openinary vLLM lane - leaves TB4 see doc 10 §3"])
        X12 --> X14(["7.14 ModerationDecisionPolicy - hard-reject labels - unknown labels NeedsReview"])
        X14 -->|"auto approve"| X15(["7.15 Approved - variants prewarmed"])
        X14 -->|"reject"| X16(["7.16 Rejected - binaries purged"])
        X14 -->|"uncertain"| X17(["7.17 NeedsReview"])
        X17 --> X18(["7.18 admin review-moderation AssetsAdminController L313"])
        X19(["7.19 report path - ReportStatus machine AssetsController L714 Admin L78"])
    end
    subgraph STORES["TB4-TB5"]
        Y1[("AssetContent moderation states")]
        Y2[("binary store - Openinary or legacy")]
    end
    W1 -->|"file bytes"| X1
    X2 --> Y1
    X1 --> Y2
    X8 --> Y1
    X15 --> Y2
    X16 -.->|"purge"| Y2
    W2 --> X18
    X18 --> Y1
    W2 --> X19
```

Flows of note: 7.4's clean-pass branch (SP2) and 7.8's config-off exit (N13) are the two silent fail-open/freeze branches of an otherwise fail-closed pipeline (07 §8).

## 9. L1 — P2 game-jam lifecycle and scoring

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        J1["Organizer or participant - Authorize only"]
    end
    subgraph JAMAPI["TB3 GameGuild.GameJams"]
        K1(["8.1 CreateJamCommand L364"]) --> K2(["8.2 status inferred at creation L197"])
        K3(["8.3 SetJamStatusCommand - blind write any-to-any L215-227 N6"])
        K4(["8.4 SubmitJamEntryCommand - UserId from body L381 N5 - jam Status never read L229-249 N6"])
        K5(["8.5 ScoreJamSubmissionCommand - JudgeUserId from body L391 N5 - score clamped L281"])
        K6(["8.6 CreateJamProjectCommand - bridge into Projects ProjectsController L392"])
    end
    subgraph STORES["TB3"]
        N1[("Jam + JamSubmission + JamScore")]
    end
    J1 -->|"create set-status submit score - identities partially body-asserted"| K1
    K3 --> N1
    K4 --> N1
    K5 --> N1
    K6 --> N1
```

## 10. L1 — P3 project lifecycle and data exfiltration

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        Q1["Creator or collaborator"]
        Q2["Invitee"]
        Q3["System admin - analytics dashboards N10"]
    end
    subgraph PROJAPI["TB3 GameGuild.Projects + Analytics"]
        AA1(["9.1 create Draft L356 - validator present"]) --> AA2(["9.2 versions Draft to ReadyForTesting to Released to Archived - entity-enforced ordering"])
        AA3(["9.3 PublishProjectCommand - Publish permission L322 - NO from-state guard L311-341 N14"])
        AA3 --> AA4(["9.4 AnnouncePublicationCommand - leaves TB3 see doc 10 §1"])
        AA5(["9.5 invitations - token accept with expiry plus email match L705-756"])
        AA6(["9.6 collaborator CRUD - CanGrantPermissionsAsync"])
        AA7(["9.7 LaunchPadProjectLifecycleParticipant - close soft-deletes plans"])
        AA8(["9.8 dashboard read update by id - repository predicate id plus not-deleted ONLY AnalyticsRepositories L103-105 - Authorize class-level N10 - TB7 breach"])
    end
    subgraph STORES["TB3"]
        AB1[("Project + ProjectVersion + Collaborator + Invitation")]
        AB2[("Dashboard + Widget rows")]
    end
    Q1 -->|"project content [PII] versions"| AA1
    Q2 -->|"accept token"| AA5
    AA1 --> AB1
    AA3 --> AB1
    AA5 --> AB1
    Q3 -->|"GET PUT dashboard by id - any tenant"| AA8
    AA8 --> AB2
```

Flows of note: 9.8 is the live TB7 breach — every flow into it is id-addressed with no tenant predicate (07 §10 I verdict; N10).

## 11. L1 — P4 TestingLab session

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        Z1["Manager - RequireTestingLabPermission"]
        Z2["Participant"]
        Z3["Anonymous - public session list"]
    end
    subgraph TLAPI["TB3 GameGuild.TestingLab"]
        AC1(["10.1 CreateTestingSession - permission attribute L61 - validator present"]) --> AC2(["10.2 session created Scheduled - window inherited from event slots"])
        AC3(["10.3 register or waitlist - actor from context 401 otherwise TestingParticipantsController L85-130"])
        AC4(["10.4 attendance marks - manager permission L212"])
        AC5(["10.5 link session project L241"])
        AC6(["10.6 event workflow Draft to ApplicationsOpen to Scheduled to Active to Completed - committee votes actor-verified L559-600"])
        AC7(["10.7 analytics read plus export L14 L25"])
        AC8(["10.8 public session list - anonymous L136"])
    end
    subgraph STORES["TB3"]
        AD1[("TestingEvent + Session + Registration + Waitlist")]
    end
    Z1 --> AC1
    Z2 -->|"registration notes [PII]"| AC3
    Z1 --> AC4
    Z3 --> AC8
    AC1 --> AD1
    AC3 --> AD1
    AC6 --> AD1
```

Flows of note: 10.3 resolves identity from the actor context — the correct pattern P2 lacks (07 §11); session rows flow out to the feed at C2/2.10.

## 12. L1 — P5 AI credit consumption

```mermaid
flowchart TD
    subgraph CLIENT["TB2"]
        AI1["Blog author"]
        AI2["Any authenticated user - generic chat"]
    end
    subgraph AIAPI["TB3-TB6 Blog authoring + AI + AiCredits"]
        AJ1(["11.1 entitlement read - GetBalanceAsync L87"]) --> AJ2(["11.2 QuoteAsync L176"])
        AJ2 --> AJ3{"11.3 balance sufficient"}
        AJ3 -->|"no"| AJ4(["11.4 InsufficientAiCredits"])
        AJ3 -->|"yes"| AJ5(["11.5 ReserveAsync L221 - wallet owns transaction - SELECT FOR UPDATE AiCreditWallet L323-345"])
        AJ5 --> AJ6(["11.6 AiOrchestrator GenerateForActorStreamingWithReservedQuota - TB6 to providers"])
        AJ6 -->|"success"| AJ7(["11.7 SettleAsync actual usage L495"])
        AJ6 -->|"cancel or failure"| AJ8(["11.8 ReleaseAsync idempotency-keyed L577 L600"])
        AJ9(["11.9 generic chat generate - quota-only AiOrchestrator L331-337 - NoOp billing recorder default ServiceCollectionExtensions L30 - N12 SP5"])
        AJ10(["11.10 token quota consume plus refund L345 L362 L375"])
    end
    subgraph STORES["TB3-TB6"]
        AK1[("economy wallets + credit ledger rows")]
        AK2[("Resources quota counters")]
        AK3[("AI execution billing - NoOp sink unless recorder registered")]
    end
    AI1 -->|"prompt [PII]"| AJ1
    AI2 -->|"prompt [PII] - pays no credits"| AJ9
    AJ5 --> AK1
    AJ7 --> AK1
    AJ8 --> AK1
    AJ9 --> AK2
    AJ9 -.->|"NoOp by default"| AK3
    AJ6 <-->|"prompts and streams cross TB6"| AK3
```

Flows of note: two regimes share one wallet boundary — the authoring lane (11.1–11.8) is credit-bounded and transaction-joined; the generic lane (11.9) exits TB3 having touched only quota counters (07 §12, N12/SP5).

## 13. Flow-to-family-to-threat index

| L1 | STRIDE family (07 §) | Highest-severity open item |
|---|---|---|
| §2 | C1 post lifecycle | N1 (no text moderation) |
| §3 | C2 feed assembly | SP3/SP4 (counter pollution, repost laundering) |
| §4 | C3 group membership | N4 (admin-only + blind status setter) |
| §5 | C4 blog publish | N1 (text) — asset gate covered |
| §6 | C5 reactions | N9 extension SP3 |
| §7 | C6 profile exposure | **SP1 [PII] anonymous read of Private profiles** |
| §8 | P1 asset moderation | SP2 (scan-disabled clean pass) + N13 |
| §9 | P2 jam | N5/N6 |
| §10 | P3 project | N10 (cross-tenant dashboards) + N14 |
| §11 | P4 TestingLab | accepted-role-trust items only |
| §12 | P5 AI credits | N12 + SP5 |
