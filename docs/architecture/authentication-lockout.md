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
