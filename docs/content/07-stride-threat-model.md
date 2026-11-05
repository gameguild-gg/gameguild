# Content.Pages — STRIDE Threat Model

> Module `GameGuild.Content.Pages` · `develop` @ `ce31d5850c`. Families follow the flows of `01-user-flows.md`. Per family: Spoofing / Tampering / Repudiation / Information disclosure / Denial of service / Elevation of privilege, each with a verdict (MITIGATED / OPEN-see-gap / ACCEPTED-by-design) and citation. Trust boundaries: **TB1** anonymous internet → API (global rate limit + anonymous allowlist), **TB2** authenticated member → API (JWT, no per-route policy), **TB3** API → PostgreSQL (`public` schema), **TB4** API → compliance `AuditLog` (same transaction).

## S1 — Page lifecycle (F1: create/edit/publish/unpublish/delete)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Publish attributed to a spoofed user | MITIGATED | publisher id taken from `ActorContext.SubjectIdAsGuid`, 401 when absent (`Controllers/PageController.cs:173-176`) |
| T | Page body/SEO/JSON-LD tampering | OPEN (authz shape) | body edits require EDIT gate (`:137`) but creation/deletion don't — C1; JSON-LD/CustomData are free JSON strings (`Entities/Page.cs:127-138`) stored as-is |
| R | Deny a publish/unpublish | MITIGATED | `Content.Pages.PagePublished/Unpublished` audit with actor id in-transaction (`Services/PageService.cs:143-147,157-161`); fail-closed (`AuditedMutationExtensions.cs:62`) |
| I | Draft leakage via lifecycle surface | MITIGATED on by-id/by-slug | draft → 404 without `content:*` (`Controllers/PageController.cs:65-68,80`) — but see S3 for the section bypass (C4) |
| D | Slug-unique hot rows | ACCEPTED | unique index contention only on duplicate slugs (`Configuration/PageConfiguration.cs:11`) |
| **E** | **Member publishes/deletes platform pages** | **OPEN — C1 (High)** | publish/unpublish/delete/create carry `[Authorize]` only (`Controllers/PageController.cs:119,157,170,189`) |

## S2 — Public consumption (F2: by-slug, sitemap, OG)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Crawler impersonation | ACCEPTED | surface is intentionally public; registry entries L98-99, L102 (`API:/Security/AnonymousEndpointRegistry.cs`) |
| T | Cache poisoning via OG | LOW/OPEN | 5-min `ResponseCache` keyed by query strings (`Controllers/OpenGraphController.cs:23`) — a published page's OG is trusted input rendered into `<head>`; `CanonicalUrl`/`OgImageUrl` are operator-controlled URL strings (stored XSS is a frontend-escaping concern) |
| R | — | n/a | reads are not audited (by design) |
| **I** | **Deleted or draft content served** | **OPEN — C2** | deleted pages still resolvable (no query filter — `Services/PageService.cs:15-45`, `OpenGraphService.cs:14-26`); sitemap lists soft-deleted pages (`Controllers/PageController.cs:100-116`) |
| D | Sitemap/catalog cost | MITIGATED-ish | sitemap hard-capped at 5000 (`:107`); catalog paginated `take 50` default; global rate policy only (no per-route limit) — heavy `q` search scans unindexed (`Services/ContentResourceService.cs:34-41`), ACCEPTED at current scale |
| E | — | n/a | no privilege boundary crossed by reads |

## S3 — Section authoring (F3)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | — | n/a | editor identity only from actor context |
| T | Block JSON tampering on drafts | OPEN | section writes on unpublished pages are ungated (C1/C4 — `Controllers/PageController.cs:212-280`); `Data` JSONB unrestricted (`Entities/PageSection.cs:93`) |
| R | Reorder/delete attribution | MITIGATED | `PageSectionsReordered`/`PageSectionDeleted` audits with actor (`Services/PageService.cs:227-231,248-252`) |
| **I** | **Draft-page content enumeration** | **OPEN — C4** | `GET …/sections` returns draft-page blocks to any member (`Controllers/PageController.cs:204-209` — no status check) |
| D | Reorder O(n²) match loop | ACCEPTED | `sections.FirstOrDefault` per id (`Services/PageService.cs:242-246`) — page-sized n |
| E | Member escalates to page-authoring | OPEN — C1 | see S1-E |

## S4 — Content-resource catalog (F4)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | — | n/a | — |
| T | View-count integrity | OPEN — C7 | anonymous write-on-read (`Services/ContentResourceService.cs:158-165`) inflates engagement metrics; unaudited |
| R | Resource mutations | MITIGATED | Created/Updated/Deleted/Published audits (`:82-86,120-124,134-138,150-154`) |
| I | In-review/draft leakage | MITIGATED | forced-Published list + by-id 404 (`Controllers/ContentResourceController.cs:51-54,73-77`) |
| D | Write-on-read doubles DB writes per public GET | OPEN (perf/DoS) | awaited second `SaveChangesAsync` per request (`Controllers/ContentResourceController.cs:95-101`) — also turns count-write failures into 500s on a public read |
| E | Member publishes/deletes catalog entries | OPEN — C1 | publish/delete ungated (`:142-171`) |

## S5 — Course-proposal intake (F5)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Fake proposer identity | ACCEPTED | open pipeline (issue #105); email unverified; dedupe absent by design |
| T | Status tampering | OPEN — C8 | any member can PUT any status transition (`Controllers/CourseProposalsController.cs:80-112` — vocabulary-validated, transition-free) |
| R | Submission/review trail | MITIGATED | Created/Updated (with PreviousStatus)/Deleted audits, shape-only for PII (`Services/CourseProposalService.cs:27-34,132-137,152-157`) |
| **I** | Proposer PII readable by any member | OPEN — C8 | CRM list/get `[Authorize]`-only, search covers `ProposerEmail` (`Services/CourseProposalService.cs:55-88`) |
| D | Proposal spam | PARTIAL | global rate policy only; slug suffix prevents collisions (`:194`); 4000-char caps (`Entities/CourseProposal.cs:50-65`) |
| E | — | OPEN — C1 family | delete/review ungated |

## S6 — Marketing-lead intake (F6)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| Sp | Forged lead attribution | ACCEPTED | anonymous by design; source validated (`Controllers/MarketingLeadController.cs:77-80`) |
| T | Schema-valid but hostile payloads | PARTIAL | length caps (`Entities/MarketingLead.cs`), controller rules; `Referrer`/`UserAgent`/`PagePath` stored raw (reflected into any CRM UI at its own risk) |
| R | Capture trail | MITIGATED | shape-only audit, no submitted values (`Services/MarketingLeadService.cs:29-35`) |
| **I** | **PII retention without erasure** | **OPEN — C6** | name/email/company/message stored indefinitely; no DELETE/update route (`Controllers/MarketingLeadController.cs`); CRM list itself is `[Authorize]`-only (I3: any member can read all leads, `:15-31`, search over Email `Services/MarketingLeadService.cs:81-86`) |
| D | Lead spam / table flooding | PARTIAL | global rate limit + validation only; no dedupe (`Configuration/MarketingLeadConfiguration.cs:31` non-unique email index) |
| E | — | n/a | intake is intentionally public |

## S7 — Audit trail (cross-cutting)

| Threat | Vector | Verdict | Evidence / gap |
|---|---|---|---|
| T | Audit bypass | MITIGATED | sink requires the owning transaction and fails closed (`Audit:/Services/CentralModuleAuditEventSinks.cs:35-41`); rollback coupling tested (`ContentPagesMutationAuditTests.cs:112,125`) |
| R | Non-repudiation | MITIGATED | actor/tenant backfilled from ambient context, never payload-only (`:30-33`) |
| I | PII into audit | MITIGATED | shape-only discipline for proposal/lead events; page events carry slug/type/locale (`04-event-inventory.md` §1) |
| D | Audit write amplification | ACCEPTED | one extra insert per mutation, same transaction |
| E | — | n/a | — |

## Prioritized mitigations

1. C1 — gate all mutating routes with the existing `CanEditContent`/`content:write` pattern (smallest diff, closes S1-E/S3-E/S4-E/S5-E).
2. C2 — soft-delete query filters on `Page`/`ContentResource` (closes S2-I takedown gap).
3. C4 — parent-status check on the section sub-resource (closes S3-I).
4. C6 — lead erasure/retention route + a permission key on the CRM reads (closes S6-I).
5. C7 — move view counting off the read path (closes S4-T/D residual).
