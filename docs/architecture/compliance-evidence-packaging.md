# Compliance evidence packaging

Issue: [#178](https://github.com/gameguild-gg/gameguild/issues/178).

## Implementation status

The backend and generated client implementation is tracked in
[PR #674](https://github.com/gameguild-gg/gameguild/pull/674). It provides tenant administrator
uploads, versioned document reviews, PostgreSQL collectors, an initial ISO 27001
Annex A evidence template, signed ZIP packages and audited downloads. This implementation
has local verification evidence; production deployment remains unverified. The
complete feature remains open.

The increment in [PR #675](https://github.com/gameguild-gg/gameguild/pull/675)
adds separate ISO ISMS/SoA and GDPR/DPIA profiles, native document validation and
signed reviewer indexes. The legacy Annex A profile and
previously signed v1 archives retain their original definition and trust contract.

Remaining work includes SOC2 Type I/II, HIPAA, PCI DSS and FedRAMP templates with
their actual scope and source versions; combined full-framework acceptance,
including a licensed-standard review of the ISO clause groups; reviewer formats
for the remaining frameworks; and controlled delivery to named auditors. Enum
values alone do not establish support for those frameworks.

| Original acceptance criterion | Current evidence | Remaining work |
| --- | --- | --- |
| SOC2 Type I/II templates | No catalog entry yet | Separate point-in-time and period templates; full reviewed criteria mappings |
| ISO 27001 mapping and collection | Versioned 2022+Amd1:2024 profile: 7 management clause groups and 93 Annex A identifiers, reviewed SoA decisions/custom controls, actual tenant-filtered collection | Review clause groups against the licensed standard and accept the complete organisation scope |
| GDPR documentation and DPIA | 43 organisation-facing article mappings; processing scope/roles/records, screened activities, actual DPIA contents, prior-consultation and timeline validation | Final organisational applicability and functional acceptance with the other framework/delivery requirements |
| HIPAA, PCI DSS, FedRAMP packages | Shared packaging primitives | Actual framework templates, baseline/scope validation and regulatory references |
| Automatic collection by control | Repeatable-read database snapshot and collection inventory | Verify the maps for every additional framework |
| Quality and regulatory alignment | Hash/revision/review/version checks; structured ISO SoA and GDPR scope/DPIA checks | Completeness rules for the remaining frameworks and final human regulatory assessment |
| Gaps and deficiencies | Missing sources/documents, truncation and control deficiencies in signed reports | Full-framework acceptance after catalogs are complete |
| Timeline coverage | UTC intervals, source timestamps/counts/dates and document validity checked | Framework-specific point/period acceptance |
| Standardized review formats | Signed common ZIP; ISO SoA/management CSVs; GDPR DPIA/processing-record CSVs with source revision references | Reviewer artifacts for the remaining frameworks |
| Digital signatures and tamper evidence | Trusted-key ECDSA seal and every payload's SHA-256; verified PostgreSQL/HTTP download | Final combined acceptance and merge |

Automated auditor delivery is also part of the original proposed solution and is
still pending. Administrator downloads do not satisfy that delivery workflow.

## API and persistence

Every endpoint requires an authenticated tenant administrator with an actual
tenant and subject identifier. Acting user and tenant come from the server actor
context. Supplying authority fields in the JSON body cannot override that context.
Upload, review and preparation use distinct guarded CQRS commands and durable
operation events. Document bytes are not included in operation event metadata.

Both `/v1/audit/compliance-packaging` and the requested
`/api/audit/compliance-packaging` expose:

- `GET /templates`, `POST /documents`, `GET /documents`, `GET /documents/{id}`.
- `POST /documents/{id}/review` with the expected document revision.
- `POST /`, `GET /`, `GET /{id}`, `GET /{id}/verification`, `GET /{id}/download`.

List routes are bounded to 100 items per page and project metadata without
selecting document or artifact bytes. Unknown or other-tenant identifiers return
404. Concurrent reviews allow one winner and return 409 for stale revisions.
Downloads re-verify the stored checksum, complete archive, signed manifest and
persisted summary; an untrusted signing key causes a 409 Problem Details response,
never an artifact download. Missing private signing keys cause preparation to
return 503 without persisting a package.

Migration `20261003235346_ComplianceEvidencePackaging` adds the two owned tables,
content/period/identity constraints and update guards. Uploaded bytes and mapping
metadata cannot be changed; reviews must advance the revision. Sealed packages
cannot be updated. DELETE is not blocked by a permanent retention rule; no delete
endpoint is exposed. Any future lawful removal workflow requires explicit
authorization and audit records.

## Evidence quality

`ComplianceEvidenceValidationEngine` evaluates an explicit framework template and
the exact requested document revisions. It reports missing sources or documents,
unapproved reviews, content hash changes, mismatched framework versions, declared
deficiencies, malformed or ambiguous JSON, source row count and timestamp
inconsistencies, and incomplete period coverage.

JSON document fields come from the uploaded bytes. A caller cannot fill a missing
JSON assessment field by declaring it in metadata. PDF and text fields remain human
reviewer declarations, identified as such in the validation assumptions. Approval
and document-to-control mapping are persisted by the authenticated service before
package preparation. Each mapped control assessment includes an owner,
implementation description, effectiveness evidence and satisfactory assessment.
A deficient control affects its own index entry; global document deficiencies
affect all its mappings.

An exclusion requires a reviewed, mapped `applicability` document with framework
version, satisfactory assessment and rationale. Excluded controls appear as
`ReviewedExclusion` in the index. Other controls have `EvidenceCollected` or
`EvidenceGap` status. Readiness means that evidence can be reviewed by an auditor;
it does not mean that a control is compliant or effective.

## Versioned native profiles

`GET /templates` returns independent copies of three catalog definitions:

| Profile identifier | Version | Evidence scope |
| --- | --- | --- |
| `iso27001-2022-evidence-v1` | `ISO/IEC27001:2022` | Original 93 Annex A reference mappings; unchanged for existing captures |
| `iso27001-2022-isms-evidence-v2` | `ISO/IEC27001:2022+Amd1:2024` | Management clause groups `ISMS.4`–`ISMS.10`, plus all 93 Annex A mappings |
| `gdpr-2016-679-evidence-v1` | `Regulation(EU)2016/679` | Organisation-facing articles 3, 5–39, 44–49 and 89 |

The ISO management mappings are clause groups, not a claim that every normative
subclause has been independently verified. Use the licensed standard and human
review for that assessment. [ISO's standard entry](https://www.iso.org/standard/27001)
and [2024 amendment](https://www.iso.org/standard/88435.html) supply version provenance.
The committee's [SoA auditing practices note](https://committee.iso.org/files/live/sites/jtc1sc27/files/resources/ISO-IECJTC1-SC27-WG1_N3298_Auditing%20Practices%20Note%20-%20SoA.pdf)
is educational guidance, not an additional normative standard.

Native document types require actual JSON; PDF/text validation-field declarations
cannot substitute for a structured SoA, scope, register, screening or DPIA. Other
reviewed declarations, such as legal-basis and national-law assessments, remain
human assessments. Each document carries the exact `frameworkVersion`, an owner
and its review status. Catalog required fields can be inspected through the API.
Capture one scope/register/SoA revision per native type; separate DPIA and prior
consultation documents can be supplied for different activities. Existing limits
of 100 documents and 32 MiB captured content still apply. Large organisations can
select explicit subsets; a subset package does not establish organisation-wide
coverage.

### ISO context and Statement of Applicability

The new profile requires context, leadership, planning, support, operation,
performance and improvement evidence. Management clauses cannot be excluded.
Context includes a `climateRelevanceAssessment` object with a boolean `relevant`
decision and reviewed `rationale`/`interestedPartyRequirements` declarations.

The `iso-soa` document contains:

- `scope`, `soaRevision`, and one to 500 `necessaryControls` with unique identifiers.
- For each necessary control: `id`, `description`, `inclusionJustification`,
  `implementationStatus` (`implemented`, `planned`, or `not-implemented`) and
  `annexAReferences`. An empty reference array is valid for a custom control.
  Implemented controls require `effectivenessEvidence`.
- `annexADecisions`, an object with all 93 Annex A identifiers as keys. Each value
  has a boolean `applicable`, its inclusion or exclusion justification, and
  `necessaryControlIds` resolving to the documented controls. Applicable entries
  require at least one necessary control; excluded entries may refer to a custom
  replacement or use an empty array.

For Annex A exclusions, the request must reference the same reviewed SoA revision
and the same exclusion reason. A false SoA decision without the matching requested
exclusion, or an exclusion of an applicable entry, remains a scope gap. Planned
necessary controls can be recorded and their document quality approved; they
remain `ControlImplementationPending` gaps in the package.

### GDPR processing scope, DPIA and consultation

The profile references [Regulation (EU) 2016/679](https://eur-lex.europa.eu/eli/reg/2016/679/ojv)
and [Commission guidance for organisations](https://commission.europa.eu/law/law-topic/data-protection/information-business-and-organisations/obligations_en).
Institutional and supervisory-authority chapters are not modelled as tenant
processing duties. Optional duties and national rules require reviewed
applicability evidence; the profile does not decide whether a legal exemption
applies or treat proposed legal amendments as enacted rules.

`gdpr-accountability.processingScope` identifies up to 200 unique `activityIds`,
the `controller`/`processor` roles, and a scope `rationale`. The processing register
uses `processingActivities` with unique activity/role pairs. Controller entries
require contact, purposes, subject/data/recipient categories, transfers, retention
and security declarations. Processor entries require processor contact,
controllers, processing categories, transfers and security declarations. Records
must agree with scope roles. The reviewed Article 30 exclusion workflow can record
a human-justified derogation; no employee-count exemption is automatically inferred.

Every scoped activity requires one reviewed `gdpr-dpia-screening.screenings` entry
with `activityId`, boolean `dpiaRequired`, `rationale`,
`supervisoryAuthorityListsReview` and `reviewTriggers`. A positive screening requires
one approved, period-covering `gdpr-dpia` document for that activity, with actual
processing description/purposes, necessity and proportionality, risks to people,
mitigations, residual-risk decision, consultation declarations and review triggers.
Missing, unapproved or expired DPIAs remain gaps. Generic exclusions cannot bypass
Articles 3, 35 or 36; conditional applicability is resolved through the scope and
screening documents.

`residualRiskDecision` contains `level` (`low`, `medium`, `high`) and `rationale`.
`consultation` records `dpoAdvice` and `dataSubjectViews`, including a reviewed
reason when a consultation is inapplicable. `assessmentTiming` records UTC
`assessedAtUtc`, `effectiveAtUtc` and `basis` (`initial-processing`, `material-change`
or `periodic-review`). Initial/change assessments must predate the declared
processing/change. Periodic reviews retain `originalAssessmentAtUtc` and
`originalAssessmentReference`; a later review cannot erase a historical timing gap.

Residual high risk requires reviewed prior-consultation evidence for the same
activity: authority, submission reference/date, status and completed outcome
reference/date. Completion must follow submission and precede the declared
processing/change. A pending consultation can be documented and reviewed, but
cannot make the package ready. These checks verify declarations and their
consistency; an auditor still evaluates their accuracy, legal sufficiency and
the effectiveness of mitigation.

Document approval validates that document's quality without requiring unrelated
package documents to already exist. Package preparation separately evaluates the
full requested scope, conditional dependencies, period and collected observations.

Event datasets use a JSON array. Every row contains an `observedAtUtc` string in
UTC, for example:

```json
[
  { "observedAtUtc": "2026-09-01T09:30:00Z", "action": "ExampleOperation" }
]
```

Counts, first/last timestamps and observed dates must match these actual bytes.
Event timestamps must fall inside the inclusive requested interval. Retention
configuration snapshots are checked separately because configuration can predate
the period. Operational, authentication, authorization and integrity streams
conservatively report every UTC date without an observation. An empty incident
dataset means that no incidents were observed, and does not prove that every
incident was detected. Collection errors and truncation remain evidence gaps.

## Artifact format and trust

`ComplianceArtifactBuilder` freezes the capture, validates it, and writes a ZIP
with the following structure:

```text
manifest.json
seal.json
request.json
template.json
validation.json
index.json
index.csv
evidence/collection.json
evidence/<kind>.json
documents/<document-id>/metadata.json
documents/<document-id>/content.<json|pdf|txt>
review/iso27001/statement-of-applicability.csv   # new ISO profile only
review/iso27001/management-evidence.csv         # new ISO profile only
review/gdpr/dpia-index.csv                      # GDPR profile only
review/gdpr/processing-records.csv              # GDPR profile only
```

The manifest records package, tenant and preparing actor, capture time, framework
template snapshot, period, validation report and each payload's SHA-256 hash and
byte length. Evidence paths are generated by the server. Document names never
become ZIP paths. CSV fields are escaped and protected against formula evaluation.

The seal uses the existing `ICryptographicSigningService` with externally
provisioned ECDSA keys. The signed UTF-8 statement is:

```text
compliance-evidence-v1
ECDSA-SHA256-P1363
<configured-key-id>
<sha256-of-exact-manifest-bytes>
```

Verification binds the expected tenant and package, checks the signature against
the verifier's configured keys, and checks every payload hash and length. A public
key supplied by an archive is not a trust anchor. Missing private keys cause
generation to fail. Packages with evidence gaps can be signed for review; signature
validity and evidence readiness are separate results.

Native reviewer CSVs include source metadata paths, control/activity identifiers,
declared decisions and gap codes. The ISO SoA index includes custom necessary
controls. The GDPR DPIA index preserves screening, assessment and consultation
references, including missing dependencies. These files are hashed in the sealed
manifest and required by verification for their profile. Removing or changing one
invalidates the artifact. Native CSV additions do not modify the legacy profile or
require new files in already-signed v1 archives.

The verifier rejects missing or extra files, duplicate paths (including case
variants), traversal and absolute paths, ambiguous JSON metadata, mismatched
templates/reports, unsupported formats and seals, unknown signing keys and tampered
contents. Cancellation propagates instead of returning success.

The actual PostgreSQL collector omits personal snapshots, descriptions, metadata,
user/session/resource identifiers, IP addresses and user agents from event datasets.
It checks trusted signatures and canonical hashes for integrity entries in the
requested interval and its immediate predecessor, without claiming to have checked
the earlier full chain. Oversized, missing, disconnected or invalid entries remain
explicit gaps. Retention simulation configuration is identified as an assumption;
it is never presented as an enforced retention policy.

New audit events normalize their timestamps to PostgreSQL's microsecond precision
before signing. Legacy events signed at 100 ns precision can recover only the
original digit within the stored microsecond, still requiring the original content
hash, chain hash and trusted signature. Whole-microsecond timestamp changes,
content changes and unknown keys fail verification. No historical signatures or
hashes are rewritten.

## Resource limits

- At most 100 unique documents of 1 MiB each.
- At most six unique datasets of 4 MiB each and 50,000 rows per dataset.
- At most 32 MiB of captured document/dataset content.
- At most 1,500 controls and a five-year UTC audit period.
- At most 220 ZIP entries; every entry is bounded at 4 MiB.
- At most 40 MiB compressed archive and total decompressed content.
- JSON nesting is bounded at 32 levels and duplicate properties are rejected.

The input limits are checked before cloning capture bytes. Each source and document
is parsed once per validation, even when many controls map to it. Decompression is
bounded while reading actual bytes, independently of the ZIP's declared lengths.

## Schema verification and pre-existing drift

`node scripts/devops/generate-dbml.mjs --check` passed against the current EF model
with 434 tables. Regenerating `docs/schema.dbml` also reconciled documentation that
predated already-merged entities; that larger generated diff does not represent
additional database changes in this feature. Its migration adds only the two
packaging tables.

A separate clean-database inventory comparison found a pre-existing missing
`game_jam_scores.ProjectJamSubmissionId` column and database tables created by
explicit SQL outside the EF model (economy operation records and the project
status migration review). That global comparison failed and does not establish
whole-database parity. These findings are outside this feature's migration; its
table/column inventory check is scoped explicitly to the two packaging tables.

## Verification

Focused tests live in `CompliancePackagingCoreTests`,
`ComplianceEvidencePackagingServiceTests`, `AuditChainEvidenceVerifierTests` and
`CompliancePackagingPostgreSqlHttpTests`. They exercise real ECDSA signing, artifact
inspection/tampering, identity/key trust, input limits, per-control quality, gaps,
period coverage, exclusions, cancellation and real PostgreSQL persistence/HTTP.

Local verification on 2026-10-03:

- `dotnet build apps/api/GameGuild.sln --no-restore -p:TreatWarningsAsErrors=true -v:quiet`: passed, zero warnings/errors.
- Canonical audit suite: 263 passed, including packaging core/service and legacy timestamp verification cases.
- Authentication suite: 1,852 passed.
- Authorization suite: 1,667 passed.
- API filter `Architecture|Security|OpenApi`: 143 passed.
- PostgreSQL/HTTP/OpenAPI selection: 9 passed, including the new table/column
  inventory, database update guards,
  concurrent reviews, metadata lists, unknown keys, missing private keys, interval
  predecessor checks, omitted oversized entries, both routes and schema contracts.
- The inventory check is scoped to the two new tables; the separate failed global
  comparison is documented above.
- Client suite: 1,110 passed, including ZIP generation and binary byte preservation
  through an actual local HTTP server and the regenerated authenticated module.
- Total: 5,044 passing tests; no skipped or failing tests in these final runs.

Verification reproduced an existing loss of signed timestamp precision after a
PostgreSQL round trip before fixing it. The client tests also reproduced ZIP
responses being generated as `void` and decoded as text without an attachment
header. The fix generates `Blob` results, an explicit binary response mode and the
appropriate Accept header. Authorization errors keep the existing structured
error contract. These local checks do not satisfy the remaining framework/delivery
criteria or prove production readiness.
