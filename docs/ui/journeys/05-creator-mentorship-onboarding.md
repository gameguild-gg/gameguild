# Journey 5 — Creator and mentorship onboarding (issue #1159)

Part of the [UI journeys pack](./README.md). Audited against `develop` (`630eb8c6d7`).

The #1159 feature shipped two onboarding funnels: a public **content-creator
application wizard** (issue #105, three steps → course proposal) and the
**mentorship program** (issue #102: public directory + booking for advisees,
workspace dashboard for mentors). Both submit through **raw-fetch transports**
(`lib/content-creators/actions.ts`, `lib/mentorship/actions.ts`) rather than the
generated client — see Gaps.

## Entry points

| Entry | Route / component | file:line |
| --- | --- | --- |
| Creator process page | `/[locale]/content-creators` | `apps/web/src/app/[locale]/(public)/content-creators/page.tsx:37` |
| Apply wizard page | `/[locale]/content-creators/apply` | `apps/web/src/app/[locale]/(public)/content-creators/apply/page.tsx:10` |
| Application wizard | `ContentCreatorApplicationWizard` | `apps/web/src/components/content-creators/content-creator-application-wizard.tsx:65` |
| Wizard submit action | `submitContentCreatorApplicationAction` | `apps/web/src/lib/content-creators/actions.ts:69` |
| Mentorship directory | `/[locale]/mentorship` | `apps/web/src/app/[locale]/(public)/mentorship/page.tsx:20` |
| Mentor profile + booking | `/[locale]/mentorship/[profileId]` | `apps/web/src/app/[locale]/(public)/mentorship/[profileId]/page.tsx:28` |
| Booking form | `MentorBookingForm` | `apps/web/src/components/mentorship/mentor-booking-form.tsx:34` |
| Mentor workspace | `/[locale]/workspace/mentorship` | `apps/web/src/app/[locale]/(dashboards)/workspace/mentorship/page.tsx:28` |
| Profile manager | `MentorProfileManager` | `apps/web/src/components/mentorship/mentor-profile-manager.tsx:31` |
| Sessions panel | `MentorshipSessionsPanel` | `apps/web/src/components/mentorship/mentorship-sessions-panel.tsx:34` |
| Mentorship actions | `lib/mentorship/actions.ts` | `apps/web/src/lib/mentorship/actions.ts:194`-`:361` |

## Step-by-step click-path

### 1. Discover and start the creator application

`/content-creators` explains the four-step program (Apply → Creator profile →
First course → Grow, `content-creators/page.tsx:7-33`) and links to
`/content-creators/apply` (`apply/page.tsx:10-16`). The page is fully static — no
API calls until submit.

### 2. The three-step wizard

`ContentCreatorApplicationWizard` (`content-creator-application-wizard.tsx:65`)
tracks `step` 1→3 with a progress bar labeled "Application step N of 3"
(`:121`):

- **Step 1 — Application** (`:140-203`): name, email, content area select
  (6 fixed focus areas, `:17-24`), optional portfolio URL, motivation textarea.
- **Step 2 — Creator profile** (`:205-262`): creator handle, optional links,
  bio, preferred-format chip checkboxes (Written courses / Video lessons / Live
  workshops / Assignments and critiques, `:26`).
- **Step 3 — First course setup** (`:264-295`): working title, audience, outline
  sketch.

**Continue** validates the *current* step synchronously
(`validateContentCreatorApplicationStep`, `:88-96`; the shared validator lives in
`lib/content-creators/validation.ts` because a `"use server"` module may only
export async functions — `actions.ts:14-18`). **Back** is free navigation
(`:312-318`).

### 3. Submit → course proposal

**Submit application** (`:98-114`) → `submitContentCreatorApplicationAction`
(`actions.ts:69-133`), which re-validates all three steps server-side
(`:75-80`), packs the structured answers into `metadata` JSON (`:82-94`) and
raw-fetches `POST /v1/content/course-proposals` (`:100-113`) with title, pitch
(motivation + outline), audience, proposer name/email. Problem-details errors are
extracted field-by-field (`getFirstProblemDetailError`, `:31-67`). Success shows
the `role="status"` feedback line and resets the wizard to step 1 (`:105-112` in
the component). The proposal then follows the public proposal pipeline
(`(public)/proposals/[slug]` renders outcomes) — approval itself is off-UI
(editorial review).

### 4. Find and book a mentor (advisee side)

- `/mentorship` server-renders the directory: `listMentors({ skill, search })` →
  raw `GET /v1/mentorship/mentors` with query (`mentorship/actions.ts:194-198`).
  The skill filter is a plain GET `<form>` (`mentorship/page.tsx:50-56`) —
  no-JS friendly. Roles map Mentor/Advisor/Matchmaker (`:7-11`).
- A mentor card opens `/mentorship/[profileId]`: `getMentor` →
  `GET /v1/mentorship/mentors/{profileId}` (`actions.ts:202-206`, `notFound()`
  when absent, `[profileId]/page.tsx:29-31`), plus availability
  (`GET …/availability`, `actions.ts:212-216`) and the viewer's session state to
  gate booking (`[profileId]/page.tsx:34-38`).
- `MentorBookingForm` (`mentor-booking-form.tsx:34`): pick an open slot
  (defaults to the first), type topic and notes, submit →
  `bookMentorshipSessionAction` → raw `POST /v1/mentorship/sessions`
  (`actions.ts:333-341`). The submit button is disabled when signed out
  (`:159`); zero slots renders a muted empty state (`:72`).

### 5. Mentor onboarding (workspace side)

`/workspace/mentorship` (`workspace/mentorship/page.tsx:28`) guards the session
(sign-in prompt when anonymous, `:29-36`) and loads the dashboard + own sessions
in parallel (`getMentorDashboardAction` → `GET /v1/mentorship/profile/dashboard`
`actions.ts:226-232`; `listMyMentorshipSessionsAction` → `GET /v1/mentorship/sessions`
`:314-321`), rendering four stat cards and two panels.

`MentorProfileManager` (`mentor-profile-manager.tsx:31`) is the mentor's
onboarding form — display name, headline, bio, **program role** select
(`:171-184`), timezone, **pricing** select Free/Paid (`:198-208`), a
free-contribution reason shown for the free model (`:228`), **scheduling mode**
(direct slots vs request, options at `:24-27`), intro-call checkbox + minutes
(`:253`), and comma-separated skills (`:241`):

| Click | Handler | API call (raw fetch) |
| --- | --- | --- |
| Save profile | `:67` (`upsertMentorProfileAction`) | `POST /v1/mentorship/profile` (`actions.ts:245-260`) |
| Publish / unpublish | `:87` (`setMentorProfileStatusAction`) | `POST /v1/mentorship/profile/status` (`actions.ts:273-285`, statuses 0/1/2) |
| Publish availability slot | `:97-109` (client date/time + end-after-start validation) | `POST /v1/mentorship/profile/availability` (`actions.ts:289-300`) |
| Remove a slot | per-row | `DELETE /v1/mentorship/profile/availability/{slotId}` (`actions.ts:302-309`) |

`MentorshipSessionsPanel` (`mentorship-sessions-panel.tsx:34`) lists upcoming and
past sessions with **Cancel** (`:47-58` → `POST /v1/mentorship/sessions/{id}/cancel`,
`actions.ts:350-358`) and **Complete** (`:60-70` → `POST …/complete`,
`actions.ts:361-369`).

## Error states shown by the UI

| State | Component | Where | Shape |
| --- | --- | --- | --- |
| Step validation (wizard) | wizard | `content-creator-application-wizard.tsx:88-96` | `role="status"` feedback line, error color (`:297-309`) |
| Submit failed (wizard) | wizard | `:105-107` | same feedback line with the action's error |
| Submit succeeded | wizard | `:110-112` | success feedback + full reset to step 1 |
| Mentor load failure | directory/profile pages | `listMentors`/`getMentor` degrade; `[profileId]/page.tsx:29-31` | `notFound()` (404 page) — a 500 upstream also renders as not-found |
| Booking without slot | `MentorBookingForm` | `:54-57` | feedback "Choose an availability slot first." |
| Booking failed / signed out | `MentorBookingForm` | `:159` disabled; `run()` pattern | feedback line `role="status"` (`:145-158`) |
| Profile save / status / slot failures | `MentorProfileManager` | `:52-58`, `:97-105` | shared `role="status"` feedback (`:336`) |
| Slot validation | `MentorProfileManager` | `:97-105` | feedback for missing date/time and end-before-start |
| Session cancel/complete failure | `MentorshipSessionsPanel` | `:47-70` | feedback line |
| Not signed in (workspace) | mentorship page | `workspace/mentorship/page.tsx:29-36` | inline sign-in prompt card |
| Loading | wizard + panels | `isPending` everywhere | disabled buttons with "Submitting…" copy |

## Accessibility notes

- The wizard's step indicator is `aria-label="Application step N of 3"`
  (`:121`) with a visual progress bar; each step's fields use `Label htmlFor`.
- The preferred-format chips are real checkboxes visually hidden inside styled
  labels (`:250-255`) — keyboard operable and announced as checkboxes, with the
  selected state conveyed by styling (`:243-248`) — **and** by the checkbox's
  checked state, since the input itself carries it.
- All mentor/booking feedback lines use `role="status"` (announced without
  interrupting), errors share the same region colored destructively
  (`mentor-profile-manager.tsx:336`, `mentor-booking-form.tsx:147`,
  `mentorship-sessions-panel.tsx` same pattern).
- The directory search is a native form GET — works without JS and with screen
  readers; the icon is `aria-hidden` (`mentorship/page.tsx:54`).
- Booking slot selection is a radio-style list defaulting to the first slot —
  keyboard users must tab through; no explicit fieldset legend on the slot group
  (minor).
- The workspace stat cards are text-first (description + number), no
  color-only meaning.

## Edge cases

- **Anonymous applicants** — the wizard is public and takes name/email in-form;
  it does **not** create an account or require sign-in (proposer identity is
  self-declared, `actions.ts:108-111`). Duplicate applications by the same email
  are a server-side concern.
- **Signed-out booking** — the mentor profile page renders the form but disables
  submit (`mentor-booking-form.tsx:159`); no redirect to sign-in is offered from
  the form itself.
- **Offline / API down** — every action funnels to the shared feedback line with
  the fetch error message; the directory page renders from server data, so a
  failed `listMentors` yields an empty section rather than an error banner.
- **Session expiry mid-management** — authenticated mentorship calls attach the
  bearer via `getToken()` (`actions.ts:151-160`); expiry returns the API's 401
  which surfaces as the generic failure feedback, no re-auth redirect.
- **Stale availability** — booking a slot another advisee took fails
  server-side; the user sees the error but the slot list does not auto-refresh
  (manual reload needed).
- **Wizard state loss** — step state is component-local; a refresh mid-wizard
  resets to step 1 with empty fields (no persistence, by design for a public
  form).

## Gaps

**Known:**

- **F3-class shadow transports** — creator submit (`actions.ts:100`) and every
  mentorship call (`actions.ts:194-369`) hand-fetch their endpoints. The
  mentorship endpoints *are* in the generated client
  (`endpoints.gen.ts:26098+`), so this is exactly the F3 "parallel transport"
  pattern the matrix tracks.

**NEW:**

- **The api-contract-matrix predates this module.** Its audit (2026-11-02) has
  no `mentorship` row and F3's file list omits `lib/mentorship/actions.ts` and
  `lib/content-creators/actions.ts` — 12+ raw-fetch call sites invisible to the
  matrix's counts. The matrix should be re-run with #1159's surface included.
- **No applicant status surface.** After submitting, the applicant has no page
  to check their proposal's state — `/proposals/[slug]` exists but nothing links
  a submitted application to it (no returned slug is stored or shown beyond the
  generic success line).
- **Mentor "unpublish/pause" semantics are implicit.** The status buttons pass
  numeric statuses 0/1/2 (`mentor-profile-manager.tsx:277-283`) labeled only by
  copy; there is no confirmation for the destructive-ish unpublish, unlike the
  repo's AlertDialog convention elsewhere.
- **Booking does not refresh availability** after a failure (stale-slot case) —
  the user must reload to see the slot disappear.
- **`getMentor` failures render as 404** (`[profileId]/page.tsx:29-31`) — a
  transient API outage is indistinguishable from a deleted mentor; the
  destructive-alert fallback convention would fit here.
- **Course-proposal spam posture** — the public wizard has no Turnstile or rate
  limit on the client (unlike password reset, Journey 2); every other public
  POST in the auth journeys is gated. Server-side limits, if any, are not
  mirrored by any client feedback.

## Flow diagram

```mermaid
flowchart TD
    A["Visitor opens slash content-creators"] --> B["Reads the four step program explainer"]
    B --> C["Clicks apply and opens the wizard"]
    C --> D["Step 1 application fields"]
    D --> E{"Step validation passes"}
    E -- "no" --> F["Feedback line with the field error"]
    F --> D
    E -- "yes" --> G["Step 2 creator profile fields"]
    G --> H["Step 3 first course fields"]
    H --> I["Submit application"]
    I --> J[["POST /v1/content/course-proposals raw fetch with structured metadata"]]
    J -- "fail" --> K["Feedback line with problem detail error"]
    J -- "ok" --> L["Success feedback and wizard resets"]
    L -.-> M["No applicant status page exists gap"]
    N["Advisee opens slash mentorship"] --> O[["GET /v1/mentorship/mentors with skill filter"]]
    O --> P["Advisee opens a mentor profile"]
    P --> Q[["GET /v1/mentorship/mentors profileId and availability"]]
    Q --> R["MentorBookingForm slot topic notes"]
    R --> S{"Signed in and slot chosen"}
    S -- "no" --> T["Submit disabled or choose-slot feedback"]
    S -- "yes" --> U[["POST /v1/mentorship/sessions raw fetch"]]
    U -- "fail" --> V["Feedback line and stale slot list"]
    U -- "ok" --> W["Booking confirmed feedback"]
    X["Mentor opens workspace mentorship"] --> Y[["GET /v1/mentorship/profile/dashboard and sessions"]]
    Y --> Z["Dashboard cards and sessions panel"]
    Z --> AA["Mentor fills profile manager form"]
    AA --> AB[["POST /v1/mentorship/profile upsert"]]
    AA --> AC[["POST /v1/mentorship/profile/status publish or unpublish"]]
    AA --> AD[["POST /v1/mentorship/profile/availability publish or delete slot"]]
    Z --> AE["Cancel or complete a session"]
    AE --> AF[["POST /v1/mentorship/sessions id cancel or complete"]]
