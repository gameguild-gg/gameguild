# Authentication account lockout

The API applies both the existing per-client authentication rate limit and an account-level lockout to local
email/password sign-in (`POST /v1/auth/sign-in`). The account policy is configured in the `AuthenticationSecurity`
section:

```json
{
  "AuthenticationSecurity": {
    "MaxFailedAttemptsPerHour": 5,
    "AccountLockoutDurationMinutes": 30
  }
}
```

The API counts failed attempts for the normalized email over the preceding hour. Once the configured threshold is
reached, local sign-in is rejected until the lockout duration from the latest failure has elapsed. Attempt records
are read from the shared database, so lockout state is consistent across API instances and survives process restarts.
The response is the same generic `401` used for invalid credentials and includes `Cache-Control: no-store`; it does not
disclose whether the account exists or is locked.

The policy is applied before the local sign-in action. The authentication attempt table must therefore be available
for the local sign-in route; a database read error fails the request instead of bypassing the lockout check. Other
authentication schemes continue to use their endpoint's configured authentication rate-limit policy.

On PostgreSQL, the filter also acquires a session-level advisory lock derived from the normalized email and holds it
from the failure-count query through completion of the local sign-in action. This serializes the check and the
persisted attempt across API instances without replacing the command's normal transaction. If another request for
the same email is already being processed, the new request receives the same generic `401` and does not run the
credential check; the client may retry. The lock is released before the database connection returns to its pool.

Non-relational providers use striped in-process locks for tests and single-process development. An unsupported
relational provider fails closed instead of silently providing only per-process protection.

## Progressive delay (issue #1084)

Before credential verification, the local sign-in handler applies an exponential backoff keyed on the
normalized account identifier — never the source IP — so distributed attacks against one account are
throttled by the account itself. With the defaults (`ProgressiveDelayBaseSeconds: 1`,
`ProgressiveDelayMaxSeconds: 8`) the schedule is 1s, 2s, 4s, 8s, saturating at 8s while recent failures
remain in the one-hour window. The counter reads the shared authentication attempt table, so the delay is
consistent across API instances; counting failures skip the delay (fail-open) because the backoff must not
become an availability problem. `EnableProgressiveDelay: false` restores the pre-existing behavior.

## Turnstile challenge after repeated failures (issue #1082)

The same admission path counts failed attempts **per source IP** over the one-hour window. After
`TurnstileRequiredAfterFailedAttempts` (default 3) failures, password sign-in must present a Cloudflare
Turnstile token (`turnstileToken` on the sign-in request), verified server-side against the siteverify
endpoint. A missing or rejected token is denied with the same generic `401` carrying the stable
`captcha_required` marker in the detail, which the web sign-in form matches to reveal the challenge widget.
The web widget renders after three local failures or on the marker, whichever comes first. An empty
`TurnstileSecretKey` disables the challenge (development default); while configured, an explicit Cloudflare
rejection fails closed and a siteverify outage fails open so a Cloudflare incident cannot lock every user out.

## Lockout owner notification (issue #1083)

When the lockout engages, the account owner is notified through the durable-event notification bridge: InApp
always, and Email when their delivery preferences allow it (Urgent priority). The message states the reason
(the failed-attempt threshold), the unlock time in UTC with a relative estimate, and a support contact
(`Authentication:SecurityNotifications:SupportContactEmail`, optional). The alert fires only while the lockout
is fresh — within one minute of the threshold-crossing failure — so a sustained attack during the lockout does
not email the owner on every denied attempt. Unknown and deleted accounts have no owner to notify; their
lockouts still deny sign-in.
