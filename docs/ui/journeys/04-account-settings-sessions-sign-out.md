# Journey 4 — Account and settings: preferences, sessions, sign-out

Part of the [UI journeys pack](./README.md). Audited against `develop` (`630eb8c6d7`).

The settings hub is `/[locale]/workspace/settings/*` (7 tabs), plus the sign-out
surfaces scattered across shells (workspace menu, public menu, learning shell).
This journey also records the UI impact of the #1287 self-authorization fix: the
preferences/profile saves below hit self-path user endpoints that previously
403'd without a granted permission.

## Entry points

| Entry | Route / component | file:line |
| --- | --- | --- |
| Settings hub | `/[locale]/workspace/settings` | `apps/web/src/app/[locale]/(dashboards)/workspace/settings/page.tsx` |
| Settings nav | `SettingsNav` (7 tabs) | `apps/web/src/components/settings/settings-nav.tsx:9-15` |
| Profile | `ProfileForm` | `apps/web/src/app/[locale]/(dashboards)/workspace/settings/profile/page.tsx:8`, `apps/web/src/components/settings/profile-form.tsx` |
| Account | `ConnectedAccountsCard` + `SessionStateCard` + `PasswordCard` | `apps/web/src/app/[locale]/(dashboards)/workspace/settings/account/page.tsx:131-137` |
| Appearance | `AppearanceForm` | `apps/web/src/components/settings/appearance-form.tsx:54` |
| Localization | `LocalizationForm` | `apps/web/src/components/settings/localization-form.tsx:90` |
| Notifications | `NotificationPreferences` | `apps/web/src/app/[locale]/(dashboards)/workspace/settings/notifications/page.tsx:9` |
| Security (sessions + MFA) | `SessionsManager` | `apps/web/src/app/[locale]/(dashboards)/workspace/settings/security/page.tsx:26`, `apps/web/src/components/settings/sessions-manager.tsx:95` |
| Sessions actions | `lib/auth/sessions-actions.ts` | `apps/web/src/lib/auth/sessions-actions.ts:86` (loader), `:105`-`:152` (mutations) |
| Sign-out, workspace | `WorkspaceUserMenu` | `apps/web/src/components/console/workspace-user-menu.tsx:37` |
| Sign-out, public | `PublicAccountMenu` | `apps/web/src/components/app/public-account-menu.tsx:13`, `:59` |
| Sign-out, learning | `LearningShell` | `apps/web/src/components/learning/learning-shell.tsx:171` |

## Step-by-step click-path

### 1. Enter the hub

Every settings page is server-guarded: unauthenticated visits redirect to
`/sign-in?callbackUrl=<page>` (e.g. `account/page.tsx:66-70`,
`notifications/page.tsx:24-30`). `SettingsNav` renders the seven tabs with
`aria-current` active state (`settings-nav.tsx:23-41`).

### 2. Preferences

- **Appearance** — theme choice → `updateThemePreferenceAction`
  (`appearance-form.tsx:54`) → `PATCH /v1/users/{userId}/preferences`
  (`apps/web/src/lib/user-settings/actions.ts:84-101`, raw `client.request`) plus a
  client-side theme-sync initializer for instant feedback.
- **Localization / Privacy / Accessibility** — same `patchCategoryPreferences`
  pipeline (`actions.ts:380/390/400` → `PATCH /v1/users/{userId}/preferences`
  category payloads).
- **Notifications** — channel/category/per-type toggles, digest frequency and
  quiet hours call `updatePreferenceFlagsAction`, `updateMutedTypesAction`,
  `updateDigestFrequencyAction`, `updateQuietHoursAction`
  (`apps/web/src/components/settings/notifications/notification-preferences.tsx:124-166`),
  backed by the Notifications-module preference endpoints loaded server-side on
  the page (`notifications/page.tsx:7-17`).
- **Profile** — display name, bio, handle, links → `updateProfileAction`
  (`actions.ts:140-153`) with URL validation for link fields
  (`actions.ts:71-78`); the page pre-fills from the users profile plus the
  community/social profile with a graceful `.catch(() => null)` when the social
  read fails (`profile/page.tsx:15-17`).

**#1287 UI impact:** all of these write to self paths
(`/v1/users/{userId}/preferences`, own-profile endpoints). Before the
`SelfOrPermission` fix (commit `6b8a27b863`, #1287 U5) a plain authenticated user
without an explicitly granted permission could 403 on their *own* record; with
it, these saves succeed for the account owner, which is the only reason the
preferences tabs are usable at all.

### 3. Account page

`/workspace/settings/account` server-loads three things in parallel
(`account/page.tsx:91-127`):

1. `getAuthSessionState` via the generated `AuthModule` (`:97-99`) — rendered by
   `SessionStateCard` (`apps/web/src/components/auth/session-state-card.tsx:18-49`)
   with live refresh and an explicit error block (`:34`).
2. `HEAD /v1/auth/external-logins` via `requestRaw` (`:101-104`), parsing linked
   providers and scope consents from response headers — the `ConnectedAccountsCard`
   link/unlink surface (issue #250 consent display).
3. `PasswordCard` (`apps/web/src/components/password-card.tsx:31`).

**Password change** (`password-card.tsx:37-93`): current + new + confirm, optional
"revoke other sessions" checkbox (default checked, `:145`) →
`changePasswordAction` → generated `postAuthPasswordChange` (matrix: called from
`lib/auth/password-change-action.ts`). This is the settings-side instance of the
#1164/#1231 re-verification pattern: the current password is the proof, the
server bumps `TokenVersion`, and the card refreshes the session via
`useSession().update()` (`:77`) — it deliberately never re-signs-in. Errors map
to toasts: `wrongCurrent` / `weakPassword` / generic (`:81-92`); a stale action
bundle (409) reloads once instead of toasting (`:62-68`, issue #1035).

### 4. Sessions list + revoke

`/workspace/settings/security` loads sessions, the current-session security
analysis and trusted devices in parallel through generated modules
(`sessions-actions.ts:86-102`), each wrapped in the silent `safeLoad` degrade
(issue #864 pattern). `SessionsManager` (`sessions-manager.tsx:95`) renders:

| Click | Handler | API call |
| --- | --- | --- |
| Rename a session | `sessions-manager.tsx:120-130` | `patchAuthSessionsName` (`sessions-actions.ts:105-110`) |
| Terminate one session | `:262-265` (aria-labeled per row) | `deleteAuthSessions` (`:113-118`) |
| Terminate others | `:277-279` | `postAuthSessionsTerminateOthers` (`:121-126`) |
| Terminate all | `:285` behind an `AlertDialog` confirm (`:105`) | `postAuthSessionsTerminateAll` (`:129-134`) |
| Trust current device | trusted-devices card | `postAuthTrustedDevices` (`:137-144`) |
| Revoke trusted device | per-row | `deleteAuthTrustedDevices` (`:147-152`) |

Every mutation `revalidatePath`s the security page (`:108/116/124/131/142/150`),
so the list re-derives from server truth. The current session is badged
(`current-session-badge`, `:190`); empty lists render muted copy
(`:299-300`).

### 5. Sign-out

The workspace avatar menu (`WorkspaceUserMenu`) calls
`useAuth().signOut({ redirect: false })` then routes to `/sign-in`
(`workspace-user-menu.tsx:47-54`); the public menu and learning shell do the same
shape (`public-account-menu.tsx:59`, `learning-shell.tsx:171`). The sign-out
**server action** (`packages/infrastructure/client/src/integrations/next/actions.ts:329-368`):

1. Reads the encrypted session cookie, decodes the refresh token.
2. Fire-and-forgets `POST /v1/auth/tokens:revoke` with a swallowed `.catch(() => {})`
   (`actions.ts:347-353`).
3. Deletes the session cookie.

That is issue **#1196**: the dedicated `POST /v1/auth/sign-out` endpoint added by
#1168 is never called by the web client — sign-out is best-effort revocation plus
cookie clearing. See Gaps.

## Error states shown by the UI

| State | Component | Where | Shape |
| --- | --- | --- | --- |
| Unauthenticated page visit | each settings page guard | e.g. `account/page.tsx:66-70` | redirect to sign-in with callbackUrl |
| Preferences save failed | forms via `ActionResult` | `actions.ts:91-96` | inline/toast error per form |
| Wrong current password | `PasswordCard` | `password-card.tsx:82-83` | `toast.error` with `errors.wrongCurrent` |
| Mismatch/weak password | `PasswordCard` | `:46-49`, `:141` | inline `FieldError` + `aria-invalid` |
| Session-state refresh failed | `SessionStateCard` | `session-state-card.tsx:34` | destructive-text block with testid |
| Session action failed | `SessionsManager` | `:112-114` | `toast.error(actionFailed)` — the status (`unauthorized`/`notFound`/`error`, `sessions-actions.ts:47-56`) is collapsed into one generic toast |
| Link/unlink OAuth banners | `ConnectedAccountsCard` | `account/page.tsx:123-129` | banner kinds incl. conflict/last-method/state-mismatch |
| Loader degraded | security page `safeLoad` | `sessions-actions.ts:71-80` | **silent** empty render (pre-#1217 convention) |
| Loading | forms | e.g. `password-card.tsx:152-155` | disabled submit + spinner |
| Empty | sessions/devices lists | `sessions-manager.tsx:299-300` | muted copy |

## Accessibility notes

- `SettingsNav` is a real `<nav aria-label>` with link semantics and active-state
  styling (`settings-nav.tsx:23`).
- Session rows: rename gets an `sr-only` label per session id
  (`sessions-manager.tsx:206-208`); terminate buttons carry per-name
  `aria-label`s (`:263`); destructive bulk actions sit behind an `AlertDialog`
  confirm (`:105`, `:285`).
- `PasswordCard` labels all three inputs with `FieldLabel htmlFor`, hints in
  `FieldDescription`, mismatch surfaced with `aria-invalid` and cleared on input
  (`password-card.tsx:115-141`).
- The revoke-sessions checkbox is a labeled checkbox (`:144-149`), keyboard
  toggleable.
- `SessionStateCard` uses a definition list for fields (`:39`) — name/value pairs
  are announced cleanly.
- Toasts (sonner) announce results; they are the *only* feedback for session
  mutations, so screen-reader users get the outcome without a DOM change in the
  row (acceptable but minimal).

## Edge cases

- **Session expiry mid-settings** — page guards redirect at navigation; an
  in-flight mutation returns `unauthorized` status which the UI collapses into the
  generic failure toast (no re-auth redirect from the action itself).
- **Terminating the current session** — allowed via terminate-all (dialog
  confirmed); the cookie clears server-side and the next navigation bounces to
  sign-in. Terminating *others* keeps this session alive.
- **Password change bumps TokenVersion** — other sessions die server-side; the
  checkbox merely makes it explicit (`password-card.tsx:145`), and
  `useSession().update()` (`:77`) refreshes this browser's claims without a
  re-sign-in (the #1164-era fix: no `signIn` from the card, see header comment
  `:26-30`).
- **Offline** — all saves fail into toasts/inline errors; no optimistic writes.
- **Race between two tabs** — `expectedVersion` on cart-style endpoints is absent
  here; preference patches are last-write-wins per category.

## Gaps

**Known:**

- **#1196 (open)** — the client never calls `POST /v1/auth/sign-out`
  (`actions.ts:347-353` fire-and-forgets `tokens:revoke` instead, errors
  swallowed). Revocation can silently fail, leaving a live refresh token with a
  cleared cookie.
- **Matrix dead cluster** — `sessions:refresh` (`postAuthSessionsRefresh`) has no
  UI; `patchAuthTrustedDevicesName` exists in the client but no row rename UI
  calls it (matrix trusted-devices row).

**NEW:**

- **`safeLoad` hides real outages on the security page.** A failed sessions or
  MFA load renders an empty list indistinguishable from "no sessions"
  (`sessions-actions.ts:71-80`) — the pre-#1217 anti-pattern, one convention fix
  away from the destructive-alert fallback.
- **Session action errors lose their status.** `unauthorized` vs `notFound` vs
  `error` all toast the same generic string (`sessions-manager.tsx:112-114`) — a
  user whose session expired mid-action gets "Action failed" with no path back to
  sign-in.
- **Sign-out gives no feedback on revocation failure** by design of the swallowed
  catch (`actions.ts:351`); combined with #1196 the UI cannot even detect it.
- **Trusted-device rename has no UI** although the endpoint is generated
  (`patchAuthTrustedDevicesName`) — a naming dead end: devices can be trusted and
  revoked but never labeled from this page.
- **Two MFA action modules coexist** (`lib/auth/mfa-actions.ts` and
  `lib/user-settings/mfa-actions.ts`) — the user-settings copy is imported by
  nothing on this page set; drift risk documented in
  [Journey 2](./02-mfa-enrollment-and-recovery.md).

## Flow diagram

```mermaid
flowchart TD
    A["User opens workspace settings"] --> B{"Authenticated"}
    B -- "no" --> C["Redirect to sign-in with callbackUrl"]
    B -- "yes" --> D["SettingsNav with seven tabs"]
    D --> E["Appearance or Localization or Privacy or Accessibility form"]
    E --> F[["PATCH /v1/users/userId/preferences via patchPreferences"]]
    F -- "fail" --> G["Inline or toast error"]
    F -- "ok" --> H["Preference saved and page revalidated"]
    D --> I["Notifications tab"]
    I --> J[["Notification preference actions for flags types digest quiet hours"]]
    D --> K["Account tab"]
    K --> L[["getAuthSessionState via generated AuthModule"]]
    K --> M[["HEAD /v1/auth/external-logins parses linked providers header"]]
    K --> N["PasswordCard submit with current password"]
    N --> O[["postAuthPasswordChange generated call"]]
    O -- "wrongCurrent" --> P["Toast wrong current password"]
    O -- "ok" --> Q["TokenVersion bumped and useSession update refresh"]
    D --> R["Security tab"]
    R --> S[["getAuthSessions and analyze-security and getAuthTrustedDevices in parallel"]]
    S -- "degraded" --> T["Silent empty lists"]
    S -- "ok" --> U["SessionsManager renders rows"]
    U --> V[["patchAuthSessionsName on rename"]]
    U --> W[["deleteAuthSessions on terminate one"]]
    U --> X[["postAuthSessionsTerminateOthers or TerminateAll behind confirm dialog"]]
    U --> Y[["postAuthTrustedDevices or deleteAuthTrustedDevices"]]
    Z["User clicks sign out in any shell menu"] --> AA["useAuth signOut with redirect false"]
    AA --> AB[["POST /v1/auth/tokens revoke fire and forget actions.ts 347"]]
    AB --> AC["Session cookie deleted then route to sign-in"]
    AD["Note the newer POST /v1/auth/sign-out endpoint is never called issue 1196"] -.-> AB
