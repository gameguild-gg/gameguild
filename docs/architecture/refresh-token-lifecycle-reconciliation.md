# Refresh-token lifecycle and unavailable accounts — #262 / #263

## Reviewed403 contract and client regeneration

The review correctly identified that the new non-owner403 was absent from the
declared revocation contract. A new actual full-application Swagger HTTP test
failed for that omission and now passes after declaring403 on the existing
action. Captured OpenAPI differs solely by that response; all other specification
content is deep-equal (1,297paths/1,656schemas), with no pending EF model changes.
The client was regenerated; only generation metadata changes because its existing
generic error types already support403. Client1,119 tests and typecheck pass.

Fresh reviewed source passes **5,360 focused/core .NET plus1,119 client cases**:
**6,479 distinct local cases**,43 new definitions. Full Authentication2,191,
Authorization1,667 and SharedKernel1,377, actual PostgreSQL9, API architecture/
security/eventing115 and the new HTTP contract fact1 pass. A local complete
OpenAPI attempt ended without terminal TRX and is preserved/excluded; matching-head
CI must supply full integration266 and complete OpenAPI15 before merge.

The initial commit26c1f86bc passed6,549 whole local .NET cases and all applicable
CI checks (2,456 main plus14 repeated OpenAPI). Those earlier receipts remain
historical evidence and are excluded from reviewed-head totals. Pending merge and
the19 original #263 criteria are not completed by this bounded increment.

## Explicit revocation ownership continuation — #263

Production `f3346a891e4887bcf362d0544d23386aeda30ed1` was exercised against actual
migrated PostgreSQL and production signed JWT/HTTP revocation. **Four cases yielded
two failures and two passing controls:** different users in the same or another
tenant received204 instead of403 when submitting the owner's refresh token.
Those two baseline storage assertions follow HTTP status and were not reached.
Own-token204 revocation terminates its linked session; anonymous401 leaves it
unchanged. The initial provisional owner-session-active assertion was corrected
to the existing `LocalAuthService` logout contract; initial source/TRX/logs remain.
[Public baseline](https://github.com/gameguild-gg/gameguild/issues/263#issuecomment-5992442921).

The command now obtains identity from the trusted actor context, requires an
authenticated user subject and compares stored token ownership before delegation.
Command/body userId and administrator roles cannot authorize another user's token.
The host supplies audit IP, and successful own-token logout behavior is preserved.
After that guard, three of the four HTTP cases passed; the owner case exposed a
second gap: token revocation and inactive linked session were persisted, but the
same session's bearer still received200. Its exact source/log/TRX and intermediate
production hashes are retained separately. The assertion remains in the suite.
Session-bound bearer middleware now requires one valid nonempty session claim,
an existing active/unexpired/nonterminated session and matching subject ownership.
Denial clears identity; explicit public endpoints continue anonymously. Legacy
and service tokens without a session claim keep their existing compatibility.
Unit tests cover invalid actor kinds/subjects, spoofed userId, admin roles, missing
tokens, cancellation, stored session failures, malformed/duplicate session claims
and public boundaries. Actual PostgreSQL HTTP tests retain both original denials
and controls plus invalid signed-session cases. **5,359 distinct focused/core
cases pass**, zero failures/skips: Authentication2,191, Authorization1,667,
SharedKernel1,377, PostgreSQL HTTP9 and API architecture/security/eventing115.
Forty-two definitions are new; earlier failed attempts remain evidence. Full API
suites and matching-head gates/merge are pending. Full OpenAPI is deep-equal
(1,297paths/1,656schemas); EF reports no pending model changes. #263 remains OPEN
with all19 criteria unchanged.

## Official bounded #262 acceptance after merge

PR #692 merged into develop at `f3346a891e4887bcf362d0544d23386aeda30ed1` after all
applicable checks for `065c6cf39298fd6f66f678d8735bc4bd058f873b` passed.
[#262 officially CLOSED/COMPLETED](https://github.com/gameguild-gg/gameguild/issues/262#issuecomment-5992219521)
after each preserved owner criterion was mapped to the actual replay/transaction,
signed bearer, stored user-version and forced concurrent endpoint acceptance.
#263 remains OPEN with all 19 original criteria and its remaining bounds below.

| Project | Distinct cases | Receipt provenance |
| --- | ---: | --- |
| Full API integration | 256 | CI on final head, actual PostgreSQL |
| Authentication | 2,158 | CI on final head and local current core; counted once |
| Authorization | 1,667 | Fresh local current core |
| SharedKernel | 1,377 | Fresh local current core |
| Full API unit | 1,049 | Initial head 8c199709d; production C#, APIunit project and compilation inputs byte-identical to final head |

**6,507 distinct combined cases**, including 14 new cases. Actual CI selected
2,414 main cases plus 14 repeated OpenAPI; it did not select full APIunit or
SharedKernel for this changed module surface. Local focused seven, repeated core
and fresh API architecture/security/eventing 115 are subsets excluded from totals.
The final local whole-API repetitions crashed; the sequential integration retry
failed fixture initialization while Docker was unavailable. These receipts are
retained, not counted as complete. Docker local was restored; CI supplied final
whole integration. Current-head required gates, OpenAPI/client consistency,
Codacy/four CodeQL and warning/error-clean builds passed. Full contract remains
identical (1,297 paths / 1,656 schemas), no model changes.

Publication-stage pending statements below remain history and are superseded
only for this bounded #262 acceptance. No complete #263 or unexercised provider/
distributed-cache acceptance is inferred.

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

An otherwise active/unexpired refresh now denies if the existing live-user lookup returns null, before tenant
provisioning, issuance or session mutation. Fallback version/email identity is
removed. Versioned user bearer validation also denies null live-user versions.
Rejected protected requests receive generic 401 and no identity; explicit public
endpoints continue anonymously. Unit tests cover both boundaries, no issuance or
mutations, and retained versionless legacy/service compatibility. Revoked/replaced
replay still requires committed containment before denial, even if the profile is
missing/deleted: its extant token/session rows are revoked/terminated, without
issuance or an invented version/profile update. Two explicit service cases retain
this distinction; moving the availability guard ahead of replay would bypass the
owner's global-containment requirement. The unchanged-state guarantee applies to
otherwise active tokens denied due to unavailable account, not replay containment.

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

At first publication, full Authentication 2,156, Authorization 1,667 and SharedKernel 1,377 pass with
zero failures/skips: 5,200 core cases plus seven focused relational cases. The
55 service/boundary cases are a subset of Authentication and are not added again.
Twelve definitions are new (seven relational, five unit). Full API suites are in
progress at publication and will supersede, not add to, focused subset counts.
Full Release solution and final integration-only build are warning/error clean.
Entire exported OpenAPI equals accepted #691 (1,297 paths / 1,656 schemas); EF
reports no pending model changes. Matching-head CI and merge remain required.

## Review correction and policy clarification

Codacy reported one unused required override parameter in the read synchronizer.
The seam now uses `eventData.CommandSource` to restrict synchronization to LINQ
SELECTs, retaining table/hash filters and all functional assertions; no rule is
suppressed. Seven fresh relational scenarios pass after this correction.
Two additional unit cases explicitly verify committed replay containment for
revoked/replaced tokens without a live profile, while forbidding issuance or a
synthetic profile/version update. The availability check deliberately remains
after replay detection to preserve the owner's global-containment acceptance.

Fresh Authentication 2,158, Authorization 1,667, SharedKernel 1,377 and relational
seven pass: **5,209 focused/core cases**, with **14 new definitions** across this
increment. Full suites and the corrected commit's CI/merge are still required.
First-head complete execution (6,505 distinct cases), quality finding and
intermediate failures remain separate history; focused subsets are never added
again to whole-suite totals.

This does not introduce persisted parent/family IDs, selective-family semantics,
session-specific bearer enforcement, cancellation/issuance-failure acceptance,
lifecycle audit/alerts, scheduled cleanup, metrics or optional absolute/sliding
policy. Those remaining #263 criteria stay open. Cross-instance external-cache
provider acceptance is separate from the PostgreSQL token-version boundary.
Whole suites, contracts and matching-head CI/merge are required before closing #262.
