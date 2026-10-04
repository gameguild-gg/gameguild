# Compliance evidence packaging

Issue: [#178](https://github.com/gameguild-gg/gameguild/issues/178).

## Implementation status

The backend and generated client are implemented locally on
`feature/issue-178-compliance-packaging-20261003`. They provide tenant administrator
uploads, versioned document reviews, PostgreSQL collectors, an initial ISO 27001
Annex A evidence template, signed ZIP packages and audited downloads. This branch
has not been merged or deployed. The complete feature remains open.

Remaining work includes SOC2 Type I/II, GDPR/DPIA, HIPAA, PCI DSS and FedRAMP
templates with their actual scope and source versions; the remaining ISO ISMS/SoA
requirements; framework-specific reviewer formats; and controlled delivery to
named auditors. Enum values alone do not establish support for those frameworks.

| Original acceptance criterion | Current evidence | Remaining work |
| --- | --- | --- |
| SOC2 Type I/II templates | No catalog entry yet | Separate point-in-time and period templates; full reviewed criteria mappings |
| ISO 27001 mapping and collection | All 93 Annex A identifiers; actual tenant-filtered collection; per-control reviewed assessments | Mandatory ISMS requirements, applicability and complete scope acceptance |
| GDPR documentation and DPIA | Generic reviewed document upload and field validation | Versioned GDPR requirements and structured DPIA documentation |
| HIPAA, PCI DSS, FedRAMP packages | Shared packaging primitives | Actual framework templates, baseline/scope validation and regulatory references |
| Automatic collection by control | Repeatable-read database snapshot and collection inventory | Verify the maps for every additional framework |
| Quality and regulatory alignment | Hash, revision, review, framework version and per-control assessment checks | Framework-specific completeness and document requirements |
| Gaps and deficiencies | Missing sources/documents, truncation and control deficiencies in signed reports | Full-framework acceptance after catalogs are complete |
| Timeline coverage | UTC intervals, source timestamps/counts/dates and document validity checked | Framework-specific point/period acceptance |
| Standardized review formats | Signed common ZIP, JSON/CSV indexes and control cross references | Framework-specific reviewer artifacts |
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
- PostgreSQL/HTTP/OpenAPI selection: 8 passed, including database update guards,
  concurrent reviews, metadata lists, unknown keys, missing private keys, interval
  predecessor checks, omitted oversized entries, both routes and schema contracts.
- Packaging table/column inventory against the clean migrated PostgreSQL database:
  1 passed. This is scoped to the two new tables; the separate failed global
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
