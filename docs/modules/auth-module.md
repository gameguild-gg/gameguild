# Auth Module

Consolidated summary (replaces multiple auth / token / provider docs).

## Features

- Email/password (BCrypt)
- Google OAuth (ID token validation endpoint)
- GitHub OAuth (extensible provider pattern)
- Web3 challenge/response
- JWT access + refresh (rotation & revocation)
- Email verification / password reset scaffolding
- Multi-tenant claim embedding

## Flows

1. Sign-In → validate credentials → issue tokens
2. Refresh → frontend renews ~30s pre-expiry → rotation persists new pair
3. Google OAuth → frontend sends ID token → backend validates + issues local tokens

## Security

- Refresh tokens stored with revocation metadata
- Cascading revocation on password change
- Short-lived access tokens; configurable refresh lifetime

## Representative Endpoints

- POST `/auth/sign-up`
- POST `/auth/sign-in`
- POST `/auth/refresh`
- POST `/auth/google/id-token`
- POST `/auth/logout`

## Frontend Integration

NextAuth custom JWT callback stores tokens; scheduled refresh before expiry; fallback clears invalid credentials.

## Testing

- Unit: AuthService (registration, login, issuance, revocation)
- Integration: Endpoints, Google ID token, refresh cycle
- Helper: `CreateAuthenticatedTestUserAsync`

## Common Failure Modes

| Issue | Cause | Mitigation |
|-------|-------|-----------|
| AccessDenied (Google) | Wrong endpoint flow | Use ID token path |
| Mid-request expiry | Late refresh | 30s pre-expiry buffer |
| Stale refresh token | Rotation not saved | Ensure DB commit before response |

## Extending Providers

Add validator → map claims → idempotent user creation → reuse issuance pipeline.
