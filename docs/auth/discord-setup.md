# Discord OAuth Setup and Connected-Accounts QA Runbook

Purpose: register a Discord application in the developer portal, wire its credentials into GameGuild, and walk through the manual QA for Discord sign-in plus Google/Discord account linking.

Date: 2026-08-14

## 1. Discord application setup

1. Go to <https://discord.com/developers/applications> and sign in.
2. Click **New Application**, give it a name, create it.
3. Open the **OAuth2** tab (left sidebar).
4. Copy the **Client ID** and **Client Secret** (click **Reset Secret** if the secret was generated earlier and is no longer visible — resetting invalidates the old one).
5. Under **Redirects**, add every URI your environments use (see below). Each must match the request the app sends **exactly**: the comparison is case-sensitive, scheme- and host-sensitive, port-sensitive, and trailing-slash-sensitive. `127.0.0.1` and `localhost` are different hosts and need separate entries.

Development (web dev server defaults to port 3000; use the 3011 entries only when you run the dev server on port 3011, e.g. when 3000 is taken by another agent/checkout):

```
http://127.0.0.1:3000/api/auth/callback/discord
http://localhost:3000/api/auth/callback/discord
http://127.0.0.1:3011/api/auth/callback/discord
http://localhost:3011/api/auth/callback/discord
http://127.0.0.1:3000/api/auth/link/discord/callback
http://localhost:3000/api/auth/link/discord/callback
http://127.0.0.1:3011/api/auth/link/discord/callback
http://localhost:3011/api/auth/link/discord/callback
```

Production (`<web-domain>` = the public origin the web app is served from):

```
https://<web-domain>/api/auth/callback/discord
https://<web-domain>/api/auth/link/discord/callback
```

What each URI is for:

- `/api/auth/callback/discord` — end of the **sign-in** flow. Served by the shared OAuth catch-all route `apps/web/src/app/api/auth/[...auth]/route.ts`, which builds the redirect URI as `${origin}/api/auth/callback/discord`.
- `/api/auth/link/discord/callback` — end of the **link account** flow (signed-in users connecting Discord in settings). Served by the dedicated route `apps/web/src/app/api/auth/link/discord/callback/route.ts`.

Only these two callback paths exist; Discord's authorize redirect must land on one of them.

## 2. Environment wiring

Discord credentials are **server-side only** — there is no `NEXT_PUBLIC_` variable for Discord (unlike Google).

| Variable | Where | Notes |
| --- | --- | --- |
| `DISCORD_CLIENT_ID` | root `.env` | From the OAuth2 tab. Also gates the web Discord sign-in button's provider registration. |
| `DISCORD_CLIENT_SECRET` | root `.env` | From the OAuth2 tab. |
| `OAuth__Discord__ClientId` | API runtime config | Docker path: set automatically by `compose.yaml` from `DISCORD_CLIENT_ID`. |
| `OAuth__Discord__ClientSecret` | API runtime config | Docker path: set automatically by `compose.yaml` from `DISCORD_CLIENT_SECRET`. |

- **Docker**: `compose.yaml` maps `DISCORD_CLIENT_ID`/`DISCORD_CLIENT_SECRET` to `OAuth__Discord__ClientId`/`OAuth__Discord__ClientSecret` — you only set the two `DISCORD_*` values in `.env`.
- **Local dotnet run**: the `dev:api` script loads the root `.env` via dotenv-cli, so the `OAuth__Discord__*` names are also set automatically from `DISCORD_CLIENT_ID`/`DISCORD_CLIENT_SECRET`. (See the comment block in `.env.example` beside the commented-out `#DISCORD_CLIENT_ID=` entries.)
- Missing credentials are a documented failure: the API returns **503** ("Discord OAuth is not configured" / "External login provider not configured") rather than crashing.

## 3. Platform note

The backend targets **.NET 10 (net10.0)**. There is no `launchSettings.json` in the API project, so a bare `dotnet run` starts in Production and crashes at the migration step (it resolves the production Postgres host). Always set `ASPNETCORE_ENVIRONMENT=Development` for local runs — see the startup commands in section 5.

## 4. Rate-limit note

The Discord OAuth endpoints (authorize + callback for both sign-in and link) are server-to-server calls: the **web server** calls the API, so they all arrive under the web server's egress IP. The API's `Authentication` rate-limit policy is a fixed window of **10 requests/minute**, partitioned by IP for anonymous callers. All auth traffic from one web instance therefore shares that 10/min budget (login attempts included). This is a documented ceiling, not a bug; a dedicated policy for OAuth exchanges has been deferred.

## 5. Manual QA walkthrough

Requires: real Discord account(s), a Discord application from section 1, credentials in `.env`, Postgres running. Sign-in with Discord needs one Discord account with a **verified** email that no GameGuild account uses yet (new-user case), one with a verified email matching an **existing** GameGuild account (auto-link case), and one with an **unverified** email matching an existing account (rejection case) — the last two can be the same existing GameGuild account. The Google link steps additionally need `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET` in `.env`.

### 5.1 Start the stack

1. Postgres: `docker-compose up -d adminer` (or reuse the long-running `game-guild-postgres` container).
2. API: `ASPNETCORE_ENVIRONMENT=Development dotnet run --project apps/api/Source/GameGuild.API/GameGuild.API.csproj` (add `--urls http://localhost:8080` to pin the port the web app expects).
3. Web: `pnpm --filter @game-guild/web dev` (port 3000). If 3000 is taken, `PORT=3011 pnpm --filter @game-guild/web dev` — and remember port 3011's redirect URIs must also be registered (section 1).
4. Expected: API logs show migrations applied; web ready on its port.

### 5.2 Sign-in scenarios

1. **Brand-new user.** Open `http://127.0.0.1:3000/sign-in`, click **Continue with Discord**, authorize in Discord's consent screen.
   Expected: redirected back to `/api/auth/callback/discord`, a session is created, you land on the locale-prefixed dashboard. The user appears in the DB as an OAuth user (no password), with an `externallogin` row `provider=discord`.
2. **Existing verified-email account auto-link.** Pre-create (or use) a GameGuild account whose email equals the Discord account's **verified** email. Sign out, sign in with that Discord account.
   Expected: sign-in succeeds, lands on dashboard, and the account now has a `discord` external login attached (visible later in Connected Accounts) — no duplicate user created.
3. **Unverified Discord email collision.** Use a Discord account whose email is **unverified** but matches an existing GameGuild account. Attempt sign-in.
   Expected: rejected — you are redirected to the auth error page (`/auth-error?error=...`, localized in `en-US`/`pt-BR`) instead of the dashboard; no external login row is written.

Locale note for QA: the web middleware persists a `NEXT_LOCALE` cookie — visiting `/pt-BR/...` first makes later bare-path pages render pt-BR. Use a fresh browser context per locale scenario.

### 5.3 Settings: link and unlink

Sign in first with the seeded dev admin (`admin@game-guild.com` / `Admin123!`) or any credentials account, then open **Dashboard → Settings → Account** (`/dashboard/settings/account`). The **Connected Accounts** card shows one row each for Google and Discord with Link/Unlink actions.

1. **Link Discord.** Click Link on the Discord row, authorize in Discord.
   Expected: back on the settings page with a success state (`?linked=discord`); the Discord row now shows linked.
2. **409 conflict.** In a second browser profile, sign in as a *different* GameGuild account and link the **same** Discord identity.
   Expected: the link is refused; settings page shows the conflict message (`?error=conflict`); the first account keeps the link.
3. **Unlink Discord.** Back on the first account, click Unlink on the Discord row.
   Expected: row returns to not-linked; success feedback.
4. **Unlink Google / link Google.** With `GOOGLE_CLIENT_ID`/`GOOGLE_CLIENT_SECRET` set: click Link on the Google row, complete the Google prompt — expected: linked, and the session is **not** re-issued (you stay signed in as the same user). Unlink — expected: not-linked again.
5. **Last-method refusal.** On an account with **no password** (e.g. one created via Discord sign-in in 5.2.1) and exactly one linked provider, unlink that last provider.
   Expected: refused with a "cannot remove the last sign-in method" error (400 → UI message); the provider stays linked. Linking two providers and removing one still succeeds — the guard only fires on the last one with no password set.

### 5.4 State-cookie tamper (CSRF proof)

1. Start a Discord sign-in from `/sign-in` but **stop at Discord's consent screen** (do not click Authorize yet).
2. Open devtools → Application → Cookies and edit `__gg-oauth-state-discord` (sign-in flow) or `__gg-oauth-link-state-discord` (link flow) — change any character in the value.
3. Now click Authorize so Discord redirects to the callback with the original `state`.
   Expected: callback rejects the mismatched/tampered cookie — sign-in flow redirects to the auth error page with `state_mismatch`; link flow redirects to settings with the error state — and the state cookie is deleted (single-use).

Setup credentials are placeholders in this doc; never commit real client IDs/secrets beyond what the Discord portal treats as public (the client ID alone is not secret, but keep the secret out of the repo).
