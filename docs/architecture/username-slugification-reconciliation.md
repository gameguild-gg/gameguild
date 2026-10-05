# Username slugification reconciliation — issue #222

## Requirement provenance

[Issue #222](https://github.com/gameguild-gg/gameguild/issues/222) has the title
“Automatic Username Slugification” and an empty original body. The criteria below
are engineering interpretations of that title and the current creation paths;
they are not a historical owner-approved checklist. Provider-specific OAuth,
wallet authentication and existing-account renaming remain separate requirements.

The executable baseline intercepted the real `LocalAuthService.LocalSignUpAsync`
at `IUserRepository.AddAsync`: request username `Matheus Martins` became the
display name, while `User.Username` was null. The probe intentionally stopped
before persistence and bypassed the HTTP command validator, which then rejected
spaces. The baseline factory contract suite independently failed 19/20 cases;
only preservation of an existing handle passed.

## Criteria and implementation map

| Criterion | Implementation | Verification |
| --- | --- | --- |
| New password, OAuth and administrative users receive a usable handle | `User.CreateWithPassword`, `CreateOAuthUser` and `Create` call `UsernameSlug`; repository also covers newly added object-initialized users | `UsernameAssignmentContractTests`, `UserRepositoryTests`, `UserUsernamePostgreSqlTests` |
| Automatic normalization is deterministic and bounded | Lowercase ASCII, accent removal, separators to hyphens, repeated hyphens collapsed, edge separators trimmed; compatible interior dots/underscores retained; generated names without ASCII use `user`; persisted maximum 256 | Factory cases, canonical-input validator tests, length/collision cases |
| Chosen signup usernames are actually assigned and validated | Signup passes the canonical handle as the fourth factory argument; both service and command validator require a 3–50 character canonical result and preserve the input maximum of 50 | `LocalAuthServiceTests`, `LocalSignUpCommandValidatorTests` |
| Explicitly chosen handles are not silently replaced on collision | Case-insensitive availability includes persisted and staged users; collision raises the existing `RequestValidationException` with a Username error | Repository unit cases and a real PostgreSQL explicit-write race |
| Generated collisions remain unique across creation paths and batches | Readable base when available; otherwise a bounded user-ID suffix within the existing 256 character storage limit; at most eight suffix candidates after the base | Existing/deleted/legacy-case, unsaved batch, stored suffix reservation and maximum-length cases |
| Concurrent writes respect the existing unique constraint | PostgreSQL `23505` for `IX_Users_Username` alone permits one generated-handle save retry; unrelated database failures propagate | Real PostgreSQL generated race, explicit race, second collision, email-constraint failure and ambient transaction/savepoint cases |
| Retry preserves versions and durable events | Restore tracked entity versions before retry; reuse the application context's existing outbox EventId deduplication; clear events only after successful save | Persisted user version 1, unrelated modified user's version 2, exactly one outbox event, no loser/event write on failure |
| Existing accounts and interfaces remain compatible | No migration/backfill, display-name-driven renaming, endpoint/DTO/JWT signature change; old case variants and null handles remain readable | Existing-handle/null update cases, full authorization/authentication/API suites, OpenAPI/client comparison |
| Signup works through the current anonymous HTTP endpoint | Actual host, controller, CQRS validator/handler, authentication facade/service and PostgreSQL persistence; canonical username returned in the existing response | `UsernameSignUpPostgreSqlHttpTests`: three successful normalization cases, three invalid canonical inputs and one chosen-handle collision |
| The allocation hint does not alter persistence or JSON contracts | Existing nullable 256 character unique/unfiltered username column; private creation hint excluded from model and serialization | `UsernameModelContractTests` exercises the actual application model and serialized new user |

## Policy details

The slug is separate from `Name`. Input `MátHeus Martíns` yields
`matheus-martins`, while the display name is preserved. `User.Name_1` yields
`user.name_1`, retaining characters supported by the existing web signup form.
Explicit inputs without a usable slug, malformed Unicode or control characters
are rejected; only automatically generated names use the `user` fallback.

Deleted rows reserve their usernames because the existing unique index includes
them. Allocation also sees unsaved batch members. Existing stored handles are
neither normalized on reads nor changed by display-name updates. Case-insensitive
reservation is consistent with the current username lookup contract; newly
assigned handles are canonical lowercase.

The generated/chosen distinction is an in-memory creation hint, not a public DTO
or database column. The PostgreSQL constraint remains the final concurrency guard.
Allocation is bounded to eight suffix candidates after the base; save retry is bounded to one. An
exhausted candidate sequence or a second username collision returns validation
failure and does not certify successful registration. Other persistence errors,
including the email unique constraint, retain their original failure path.

Concrete repository overloads preserve existing calls with omitted cancellation
tokens while the token-taking signatures and `IUserRepository` contract remain
available. The scan follow-up also uses execution-generated synthetic HTTP-test
passwords, a read-only maximum-length property, and explicit fixture parameter/
brace handling. These corrections do not change the handle allocation policy.

## Validation status

Issue #222 remains open until merge and recorded acceptance. The complete solution
Release build has zero warnings and errors. Local Users unit tests pass 714/714,
Authentication 2,023/2,023, Authorization 1,667/1,667, SharedKernel 1,371/1,371,
and Users integration 25/25, including eight new PostgreSQL constraint/race cases.
The complete API unit suite passes 1,048/1,048 and complete API integration suite
137/137 on the implementation revision. Following the scan corrections, Users
714/714, Authentication 2,023/2,023, all eight new PostgreSQL race cases and the
21-case full-application signup/OpenAPI selection pass again. The additional
actual-model/JSON contract case passes 1/1: 66 cases were added in total. These
local runs cover 6,986 distinct cases across the implementation and scan-follow-up
revisions; repeated focused cases are not added to that count. Matching-head CI
must validate the final revision before official closure.

The complete v1 comparison retains 1,296 paths and 1,654 schemas. The sole document
change is the `Identity_Users_User.username` description, updated to state the new
creation behavior; no wire structure changed. Client generation updates only the
corresponding TypeScript documentation comment and generator metadata. No external
provider credentials or delivery acceptance are claimed by these username tests.
