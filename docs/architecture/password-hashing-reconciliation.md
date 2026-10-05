# Password hashing reconciliation — #254

## Provenance and baseline

The original description was empty. The original title requests password hashing
and verification; the historical closeout also claimed configurable BCrypt cost
and upgrade support. The independent source review reopened #254. Its full history
and #255's separate policy requirements remain preserved.

[Executed baseline](https://github.com/gameguild-gg/gameguild/issues/254#issuecomment-5987292416)
at `11b41bf063181ab821368351be7afe3cff98c53a` failed eight assertions: configured
cost and upgrade decisions, malformed-hash classification, distinct suffixes after
72 UTF-8 bytes and four pre-cancelled operations. Three positive controls passed.
Both collision inputs met the existing 128-character policy. The executable linked
the real source; it did not call HTTP, persistence or external providers.

## Decisions

- Preserve BCrypt for passwords at or below 72 UTF-8 bytes. Read validated
  `BCryptWorkFactor` from shared presentation policy, then the two legacy policy
  locations. Default remains 12; supported generation range is 10–16. Existing
  supported lower-cost hashes still verify and are flagged for upgrade. Detection
  does not claim that login automatically rewrites hashes.
- Longer new passwords use salted PBKDF2-HMAC-SHA256, 600,000 iterations, 128-bit
  random salt and 256-bit output. The stored modular format carries the algorithm,
  iteration count, salt and hash. It uses the entire UTF-8 input and constant-time
  comparison, with strict format and parameter validation. No pepper or external
  key configuration is introduced. See the [OWASP password storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).
- A legacy BCrypt hash cannot establish an original suffix beyond its byte limit.
  Long input against that format fails authentication and requires password reset
  to a full-length hash. The historical suffix cannot be restored from the hash.
  A valid 72-byte prefix can still match a legacy hash; without historical length
  metadata it is indistinguishable from an originally 72-byte password. This is
  an explicit legacy limitation, not proof that historical passwords were full
  length. No production data inspection or forced account migration is claimed.
- Password history conservatively rejects long candidates matching the truncated
  legacy prefix, while authentication rejects those long inputs. This avoids
  weakening existing reuse protection during recovery. New full-length hashes
  distinguish their entire input.
- Reject malformed/unsupported hashes without performing attacker-chosen enormous
  work. Async operations check cancellation before and after their synchronous
  computation; cancellation cannot interrupt an already running BCL KDF.
- Keep existing user storage, routes and numeric/password-policy wire contracts.
  Typed configuration exposes the additive cost option. The hasher is not a
  password-policy evaluator during hashing; writing callers retain their policy
  validation, history checks, original-hash guard and session handling.

## Verification status

Initial focused hashing/password-handler selection passed 100/100 and its Release
build passed with zero warnings/errors. A further legacy-history regression was
added afterwards and must be run before accepting the increment.

Fresh complete suites, actual signup/login/change/reset storage and HTTP tests,
OpenAPI/client reconciliation, full solution build, exact-head PR gates and merge
remain pending. #254 remains OPEN. Configured external email delivery is separate
acceptance under #223/#253 and is not established by a synthetic reset-token fixture.
