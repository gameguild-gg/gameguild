# Refresh-token lifecycle and unavailable accounts — #262 / #263

## Preserved scope

#262 retains its original empty body and owner clarification: replay invalidates
the family and global sessions, advances user version, rejects prior access tokens,
and only one concurrent rotation may claim a token. #263 retains all 19 criteria;
duplicate #264 closure does not prove implementation. Merged #690 and #691 are
bounded increments, recorded in the existing refresh and bearer reconciliation
documents. #262 remains open until integrated acceptance and merge are verified.

## Executed baseline

Production `c4ffdc4797c058b0544ad071e7b8764d896f7e86` was exercised with actual
migrated PostgreSQL, the production signing/validation/pipeline and HTTP endpoints.
Six cases produced **two failures / four passes / zero skips**: a soft-deleted
account's previously issued bearer and refresh each returned 200 instead of 401.
The failure assertions stopped at HTTP status; later unchanged-storage assertions
were not executed and are not claimed as baseline evidence.

Passing controls perform two successive refresh rotations for TTLs 1, 7 and 30
days, verifying original auth_time, session reuse, random token bytes, hash-only
storage, predecessor invalidation/replacement, configured expiry, later bearer
consumption and replay containment. A tenant without membership returns 403 and
leaves token/session unchanged. An initial expectation of 401 for that tenant was
corrected to the existing 403 contract; all state/denial assertions and original
attempt receipts were retained.

[Public baseline](https://github.com/gameguild-gg/gameguild/issues/263#issuecomment-5990979868).
Sources, initial/corrected TRX, logs and hashes are retained under
`artifacts/test-results/issue-263-lifecycle-20261005/`.

## Correction and acceptance boundaries

Refresh now denies if the existing live-user lookup returns null, before tenant
provisioning, issuance or session mutation. Fallback version/email identity is
removed. Versioned user bearer validation also denies null live-user versions.
Rejected protected requests receive generic 401 and no identity; explicit public
endpoints continue anonymously. Unit tests cover both boundaries, no issuance or
mutations, and retained versionless legacy/service compatibility.

The original six relational scenarios pass after correction. A further endpoint
test synchronizes two actual SELECT results so both requests observe the same
active token before rotation; the interceptor only delays reads, without replacing
the database, repository, command, JWT or authentication. Acceptance requires one
successful claim/response, one denial, committed global containment, increased
version and rejection of the winner's now-compromised bearer, with an unrelated
user unaffected. All seven scenarios pass against actual migrated PostgreSQL.
The first synchronization attempt also counted session SELECTs and failed its
arrival-count assertion; the interceptor was narrowed to refresh-token SELECTs,
without changing any response/containment assertion, and the attempt was retained.

Full Authentication 2,156, Authorization 1,667 and SharedKernel 1,377 pass with
zero failures/skips: 5,200 core cases plus seven focused relational cases. The
55 service/boundary cases are a subset of Authentication and are not added again.
Twelve definitions are new (seven relational, five unit). Full API suites are in
progress at publication and will supersede, not add to, focused subset counts.
Full Release solution and final integration-only build are warning/error clean.
Entire exported OpenAPI equals accepted #691 (1,297 paths / 1,656 schemas); EF
reports no pending model changes. Matching-head CI and merge remain required.

This does not introduce persisted parent/family IDs, selective-family semantics,
session-specific bearer enforcement, cancellation/issuance-failure acceptance,
lifecycle audit/alerts, scheduled cleanup, metrics or optional absolute/sliding
policy. Those remaining #263 criteria stay open. Cross-instance external-cache
provider acceptance is separate from the PostgreSQL token-version boundary.
Whole suites, contracts and matching-head CI/merge are required before closing #262.
