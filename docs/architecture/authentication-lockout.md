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
