# Default Tenant Invariant and Testing Lab Calendar

## Context

The default GameGuild tenant is mandatory for every active user. The current authentication flow violates that invariant when a user has an inactive or cancelled membership: provisioning treats any matching membership as sufficient, while JWT tenant resolution only accepts active memberships. The result is a successfully authenticated user without tenant claims or effective access.

The Testing Lab dashboard currently spends its main content area on large Operations cards. The desired replacement is a Google Calendar-style operational calendar backed by the existing Testing Lab events API.

## Selected Approach

Use the existing tenant commands, Testing Lab event projections, GameGuild UI primitives, and application routing. Build the calendar interaction in the web application instead of adding a large calendar dependency.

Alternatives considered:

1. A month-only grid would be smaller but would not satisfy the requested Google Calendar view selector.
2. A third-party full-calendar package would accelerate the first render but add substantial styling, accessibility, and bundle integration work.
3. A focused GameGuild calendar using the existing API and design tokens supports the required modes without a second visual system. This is the selected approach.

## Default Tenant Invariant

Before issuing an authenticated user token, the system ensures an active membership in the active tenant marked `IsDefault`.

- If no membership exists, create an active membership with the `Member` role.
- If an inactive membership exists, reactivate it and clear leave/cancellation state.
- Preserve the existing role when reactivating so an assigned administrator is not silently downgraded.
- If an active membership exists, leave it unchanged.
- The behavior is mandatory and has no feature flag or tenant-level configuration.
- Apply the invariant to established login/token paths, not only new account registration, so existing users are repaired on their next successful authentication.
- Keep explicit tenant selection behavior after the default membership has been ensured.

Default-tenant membership is the baseline access boundary. Revoking a user's ability to authenticate must use user/account controls rather than leaving a mandatory default membership inactive.

## Testing Lab Dashboard Layout

Keep the existing Testing Lab page header, access warnings, and summary metrics. Replace the large Operations card grid with:

1. A compact operations menu bar containing icon buttons for Events, Projects, Participants, Analytics, and Settings. Every button retains its current route, accessible name, tooltip, focus state, and selected/hover feedback.
2. A calendar toolbar with Today, previous/next period, current range label, Create event, and a view menu.
3. A responsive calendar surface displaying real Testing Lab events.

The view menu matches the requested Google Calendar model:

- Day
- Week
- Month (default)
- Year
- Schedule
- 3 days
- Show weekends toggle

The selected view and anchor date are represented in URL search parameters so navigation is refresh-safe and shareable. `Month` is used when the URL contains no valid view.

## Calendar Data and Interaction

The server page loads Testing Lab metrics, dashboard data, and up to 100 Testing Lab events in parallel. Calendar entries use the existing event projection fields: `id`, `name`, `status`, `mode`, `startsAt`, `endsAt`, `slotCount`, and `applicationCount`.

- Selecting an event navigates to its existing management workspace.
- Create event reuses `CreateTestingEventDialog`.
- Events without a valid start date appear in Schedule as unscheduled instead of being silently discarded.
- Multi-day events span the applicable days.
- Overlapping timed events are ordered consistently by start time and title.
- Status is communicated with label/icon plus semantic styling, never color alone.
- Empty ranges show a clear empty state without hiding navigation or creation controls.
- API failures continue through `TestingLabAccessIssues` while the remaining dashboard content stays usable.

## Responsive Behavior

- Desktop shows the complete calendar grid and labelled operation buttons where space allows.
- Narrow layouts keep the toolbar horizontally usable, collapse operation labels while preserving tooltips and accessible names, and use an agenda-oriented presentation for views whose time grid cannot fit without content overflow.
- The page must remain horizontally scroll-free at 390px; only an intentionally scrollable calendar time region may own horizontal scrolling.

## Components

- `TestingLabOperationsBar`: route-backed compact navigation.
- `TestingLabCalendar`: client-side view selection and period navigation.
- Pure calendar helpers: view parsing, date-range calculation, event-to-day grouping, and range labels.
- Focused view renderers for Month, Week/3 days/Day, Year, and Schedule.

Pure date calculations remain independent of React so they can be exhaustively unit-tested.

## Testing

Backend tests cover:

- missing default membership is created;
- active default membership is unchanged;
- inactive default membership is reactivated;
- elevated existing role is preserved;
- local sign-in and Google ID-token sign-in ensure membership before tenant claims are resolved.

Frontend tests cover:

- Operations is rendered as route-backed icon controls rather than cards;
- Month is the default view;
- all requested Google Calendar views are selectable;
- previous, next, and Today navigation calculate the correct range;
- weekends can be hidden and restored;
- events are placed on the correct dates and link to management;
- unscheduled and empty states remain accessible;
- the dashboard passes focused accessibility and responsive browser checks.

## Non-goals

- Google Calendar synchronization or import/export.
- Personal calendars, tasks, invitations, or attendee decline controls.
- Drag-and-drop rescheduling in this iteration.
- Replacing the existing Testing Lab event creation and management workflows.
