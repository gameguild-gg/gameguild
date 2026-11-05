# Resources.Contents — STRIDE Threat Model

> Module `GameGuild.Resources.Contents` · `develop` @ `ce31d5850c`. Families follow `01-user-flows.md`. Trust boundaries: **TB1** anonymous internet → API (only the current-version route), **TB2** authenticated member → API (JWT; editorial keys checked in code on 5 of 16 versioning routes), **TB3** API → PostgreSQL (`content_versions`, `content_version_reviews`, `document_templates`), **TB4** logs sink (no audit store — there is no TB4).
> Overarching caveat: with 16 of 18 routes 500-ing (R1), the practical attack surface today is `POST drafts` and `v1/document-contracts/*`; the threats below describe the surface as wired, which is what a fix of R1 would re-expose.

## RS1 — Draft authoring (F1)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Draft authored as someone else | **OPEN — R6** | `CreateDraftRequest.CreatedBy` is client-supplied (`Controllers/VersioningController.cs:341` → `Services/ContentDraftService.cs:17`) |
| T | Draft tampering | PARTIAL | only `content:draft` family passes the gate (`:52,74`); entity forbids non-draft edits (`Entities/ContentVersion.cs:117-119`) — but see RS2-E for who else can move the draft |
| R | Deny authorship | **OPEN — R10** | no audit rows; `CreatedBy` spoofable (R6); ILogger only |
| I | Draft body exposure | MITIGATED (design) | reads require `[Authorize]` (`Controllers/VersioningController.cs:89-145`) — but any member qualifies (RS4-I) |
| D | Version-number races | OPEN — R7 | duplicate numbers possible (non-unique index) |
| E | Ungated drafting | MITIGATED | DRAFT gate is the module's best-enforced boundary |

## RS2 — Review workflow (F2)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Reviewer impersonation | OPEN — R6 | reviewer = actor guid or `Guid.Empty` fallback (`Services/ContentReviewPublishingService.cs:15`) |
| T | Illegal state tampering | MITIGATED (entity) | `Approve`/`Reject`/`SubmitForReview` throw on wrong states (`Entities/ContentVersion.cs:150-189`); services map to 400 |
| R | Deny an approval/rejection | **OPEN — R10** | no audit; `ReviewedBy`/`ReviewNotes` on the row are the only record |
| I | Pending-review queue enumeration | OPEN — RS4 | any member lists pending versions (`Controllers/VersioningController.cs:165-182`) |
| D | Review-row flooding | PARTIAL | append-only, unbounded, unguarded `AddReviewAsync` (`Services/ContentReviewPublishingService.cs:113-139`); global rate policy only |
| **E** | **Any member approves/rejects** | **OPEN — R5** | no permission key on submit/approve/reject/reviews (`Controllers/VersioningController.cs:150-212,321-332`) — the editorial control is the attack's shortest path |

## RS3 — Publishing and scheduling (F3)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Publish attributed to empty guid | OPEN — R6 | `GetCurrentUserId` fallback `Guid.Empty` (`Services/ContentReviewPublishingService.cs:15,163`) |
| T | Publish without approval | MITIGATED (entity) | `Publish` requires Approved/Scheduled (`Entities/ContentVersion.cs:193-195`) — except the contract path (RS5-T) |
| T | Schedule tampering | PARTIAL | future-date + Approved guards (`Services/ContentReviewPublishingService.cs:187-194`); schedule/cancel gated (`content:schedule`) |
| R | Deny a publish decision | OPEN — R10 | no audit trail |
| I | Rollback diff leakage | OPEN — RS4 | `compare` returns title/summary/body deltas (`Services/ContentVersionQueryService.cs:97-109`) to any member |
| D | Scheduled publish pile-up | OPEN — R3 | worker never runs; also auto-publish rethrows per version (`:262-266`) |
| E | Any member publishes | OPEN — R5 | publish ungated (`Controllers/VersioningController.cs:217`) |
| — | Cancel-schedule 500s | OPEN — R2 | availability defect on a gated route |

## RS4 — Version reads (cross-family)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | — | n/a | reads carry no identity decisions |
| T | — | n/a | read-only |
| R | — | n/a | — |
| **I** | **Cross-tenant / cross-entity IDOR** | **OPEN** | every read keys on `(entityType, entityId)` with **no tenant predicate and no ownership check** (`Services/ContentVersionQueryService.cs:13-75`; global filter is only `!IsDeleted`, `Configuration/ContentVersionEntityConfiguration.cs:34`) — any member enumerates any entity's full history incl. bodies, reviews and diffs |
| **I** | **Anonymous full-body read** | **OPEN — R9** | `GetCurrentVersion` is `[AllowAnonymous]` (`Controllers/VersioningController.cs:121-122`) while the allowlist claims it "exposes only the version" (`API:/Security/AnonymousEndpointRegistry.cs:44`); returns `Body`/`Metadata` of the published version |
| D | History scans | PARTIAL | indexes on `(EntityId, EntityType)` (`Configuration/…:38-39`) but history is unpaginated (`GetVersionHistoryAsync` returns all) |
| E | — | n/a | — |

## RS5 — Contract generation (F4)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Generation as another user | MITIGATED | `createdBy` from actor context, 401 when absent (`Controllers/ContractGenerationController.cs:26-34`) |
| **T** | **Template/variable injection into rendered contracts** | **OPEN** | placeholder substitution is plain regex string replacement (`Services/DocumentTemplateService.cs:160-170`); variable *values* are attacker-controllable strings interpolated verbatim into the rendered body — any markup/code the downstream consumer honors is injected; `Metadata` snapshots the variables into the version row (`Services/ContractGenerationService.cs:57-66`) |
| T | Review bypass on publish flag | OPEN — R8 | submit→approve→publish same actor synchronously (`:93-95`) |
| R | Deny generation | OPEN — R10 | no audit; response + ILogger only |
| I | Template disclosure via diffs | PARTIAL | generated versions are normal versions — readable per RS4-I |
| D | Bulk generation cost | PARTIAL | unbounded list, sequential loop (`Commands/GenerateContractCommands.cs:50-89`); global rate policy only |
| E | Ungated generation | OPEN — R5 family | `[Authorize]` only; no content permission on either contract route |

## RS6 — Audit absence (cross-cutting)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| **R** | **Every editorial action is deniable** | **OPEN — R10** | no sink port, no `AuditLog` rows, bare `SaveChangesAsync` (`04-event-inventory.md` §1) — a malicious reviewer/publisher leaves only the row's own columns |
| T | Log-only trail can be dropped | ACCEPTED-risk | ILogger is operational telemetry, not evidence |
| I | Logger payloads | MITIGATED | lines carry version ids/numbers, no user PII (`04-event-inventory.md` §4) |

## Prioritized mitigations

1. R1 — replace explicit `!IsDeleted` predicates with mapped/`DeletedAt == null` predicates and clear the 16 baseline entries (restores the surface everything else assumes).
2. R5 — gate submit/approve/reject/publish/reviews (new `content:review` key or reuse `content:edit`) — closes RS2-E/RS3-E.
3. R6 — take `CreatedBy` from the actor; reject non-guid subjects instead of `Guid.Empty` — closes RS1-Sp/RS3-Sp.
4. R9 — fix the allowlist justification or rescope the anonymous current-version route (body vs "only the version") — closes RS4-I-anon.
5. R2 + R3 — add the entity `CancelSchedule` transition and a scheduled-publish worker (or drop the schedule feature) — closes RS3 availability.
6. R10 — port the Content.Pages required-audit pattern — closes RS6.
7. RS5-T — treat rendered contracts as untrusted output (escape at consumers) and document the variable-value trust boundary on the generate contract.
