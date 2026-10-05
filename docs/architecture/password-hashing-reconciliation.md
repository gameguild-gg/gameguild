# Password hashing and policy reconciliation — #254 and #255

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

The initial focused 100-case run is history, not additional coverage. On the
isolated password branch before incorporating PR #686, **5,248 distinct local
cases** passed: authentication 2,091 (31 new), authorization 1,667, SharedKernel
1,371, selected API architecture/security/bearer 106 and PostgreSQL/HTTP 13 (all
new). Repeated runs are not added. The configured full Release solution build
had zero warnings/errors; the final extra scoring test's project then rebuilt
warning/error clean and the full authentication suite passed again.

Actual signup persists different salted hashes for equal passwords; short hashes
carry configured cost 10 and long hashes preserve their entire input. Login
accepts a correct legacy password, rejects incorrect/malformed hashes and rejects
an alternate long suffix. Authenticated change preserves state on a wrong current
password, writes the new full-length hash and retains history. Real reset-token
validation writes the new hash and rejects replay. Ambiguous legacy long input
requires recovery; changing only its suffix cannot evade history rejection.

The complete pre-MFA OpenAPI remains unchanged at 1,296 paths / 1,654 schemas.
Rebase onto the merged MFA increment, combined-revision verification, client
reconciliation, exact-head PR gates and merge remain pending. #254/#255 remain OPEN.
Configured external email delivery is separate
acceptance under #223/#253 and is not established by a synthetic reset-token fixture.

## #255 policy provenance and coverage

The original #255 body was empty; its title requests strength and complexity
rules. Its preserved source review requires current weak/strong/common/length/
sequence checks and enforcement at writing boundaries. Existing freshly executed
tests cover missing uppercase/lowercase/digit/special, common-password rejection,
empty/short input, valid strength and shared/legacy configuration precedence.
The new independent score control uses random distinct character classes and
known added sequence/repetition patterns: expected scores are 100/90/90/80.
This is a heuristic and never a measured entropy or crack-time guarantee.

New HTTP cases reject weak passwords at signup, authenticated change and reset,
preserving the original stored hash. Weak reset rejection does not consume the
real reset token. A configured minimum of 14 rejects length 13 and accepts 14
when composition requirements are disabled; the host's resolved configuration
is checked. These are configured test-host observations, not inspection or
certification of production policy or compromised-credential feeds.
