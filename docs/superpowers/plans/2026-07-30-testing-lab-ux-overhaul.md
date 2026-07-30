# Testing Lab UX Overhaul Implementation Plan

> **Execution:** Use isolated worktrees and branches derived from `develop`. Complete,
> verify, merge, and delete each branch/worktree before starting the next phase.

**Goal:** Preserve the established Testing Lab visual identity while turning the public
and administrative experiences into a coherent, responsive, API-backed workflow for
events, project applications, schedules, testers, feedback, and reporting.

**Architecture:** Route-owned reads remain in Next.js Server Components and use the
generated `@game-guild/client`. Client Components are limited to temporary UI state
such as filters, selection, dialogs, and view modes. Mutations use server actions. No
route-owned data is fetched through client effects, Zustand, or React Query.

## Global Constraints

- Work only from `develop`; do not change `main`.
- Use small branches and isolated worktrees.
- Merge completed branches into `develop`, then delete the branch and worktree.
- Preserve the recovered Testing Lab aesthetic while improving hierarchy and components.
- Use real API data; do not introduce mock counts, fallback users, or fabricated records.
- Prefer slugs for public URLs when the API exposes a stable slug; keep ID compatibility
  until that contract exists.
- Validate desktop, tablet, and mobile layouts.

## Phase 1: Public Experience

**Branch:** `codex/testing-lab-public-sessions-legacy`

- Make `/testing-lab/events` the canonical event directory.
- Keep `/testing-lab/sessions` as a compatibility redirect.
- Fetch directory data in a Server Component and map API DTOs there.
- Keep search, filters, and view selection in a focused client component.
- Preserve cards, rows, and table views with real capacity and schedule data.
- Cover loaded, empty, filtered-empty, access-error, desktop, and mobile states.

## Phase 2: Navigation And Routes

**Branch:** `codex/testing-lab-navigation`

- Reduce primary navigation to Overview, Events, Projects, Participants, Analytics,
  and Settings.
- Move applications, schedule, testers, feedback, and learning into an event workspace.
- Add compatibility redirects for legacy primary routes.
- Fix breadcrumbs, mobile navigation, and nested main landmarks.

## Phase 3: Event Workspace

**Branch:** `codex/testing-lab-event-workspace`

- Add event overview, applications, schedule, testers, feedback, and learning subroutes.
- Provide one event header with lifecycle, schedule, location, capacity, and valid actions.
- Disable invalid mutations for cancelled or completed events.
- Use sheets and dialogs for creation and editing instead of embedded long forms.

## Phase 4: Operations

**Branch:** `codex/testing-lab-operations`

- Complete application review, rationale, committee voting, and slot assignment.
- Complete schedule, registration, waitlist, attendance, and feedback workflows.
- Add filters, search, pagination, selection, bulk actions, confirmations, retries,
  and success feedback.
- Prevent page zero and lifecycle-invalid operations.

## Phase 5: Participants And Query Consistency

**Branch:** `codex/testing-lab-participants`

- Replace UUIDs with resolved names, avatars, projects, and roles.
- Remove People and Feedback N+1 request/session fan-out.
- Fix public/admin tenant and scope consistency for the same event.
- Return actionable structured errors instead of `Unknown error`.

## Phase 6: Analytics

**Branch:** `codex/testing-lab-analytics`

- Add periods, comparisons, drill-down, and export.
- Show applications, approvals, capacity, attendance, feedback completion, and outcomes.
- Keep aggregation on the API/server path and pass bounded data to chart clients.

## Phase 7: Settings

**Branch:** `codex/testing-lab-settings`

- Split settings into General, Locations, and Access.
- Complete location CRUD and capacity rules.
- Complete manager, reviewer, collaborator, role, and permission management.
- Replace long forms with sectioned pages and focused dialogs.

## Phase 8: Quality Closure

**Branch:** `codex/testing-lab-quality`

- Add Playwright journeys for manager, applicant, reviewer, tester, attendance,
  feedback, and reporting.
- Delete every event, project, request, and user fixture created by E2E.
- Cover keyboard operation, focus, accessible names, loading, empty, error, retry,
  and success states.
- Verify mobile behavior without horizontal navigation or clipped actions.
- Measure and remove avoidable waterfalls and N+1 requests.
- Run API, generated-client, web build, typecheck, lint, unit, integration, and E2E gates.
