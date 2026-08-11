# Default Tenant Invariant and Testing Lab Calendar Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every standard authenticated user receives an active default-tenant membership before a token is issued, event creation has a valid schedule by default and clear validation feedback, and the Testing Lab dashboard becomes a Google Calendar-style operational calendar.

**Architecture:** Keep default membership repair at the authentication boundary, using the existing tenant command so cancelled memberships reactivate through the same audited workflow as an administrator action. Keep calendar date math in pure web helpers, render the interactive views in a client component, and load real Testing Lab events server-side through the existing query module.

**Tech Stack:** .NET 10, MediatR/CQRS, xUnit/Moq/FluentAssertions, Next.js 16, React 19, TypeScript, Vitest, Testing Library, date-fns, existing GameGuild UI primitives, Lucide React already used by the Testing Lab dashboard.

## Global Constraints

- Default tenant membership is mandatory for standard local, Google ID-token, and refresh-token authentication paths; no feature flag or tenant configuration is introduced.
- An inactive default membership is reactivated with its existing non-empty role; a missing membership uses `Member`.
- Preserve the existing Testing Lab event API and management routes; do not add a calendar dependency or drag-and-drop scheduling.
- Calendar default is Sunday-first Month; supported view values are `day`, `week`, `month`, `year`, `schedule`, and `3days`.
- Use semantic GameGuild design tokens, accessible labels, keyboard-operable controls, and no raw colors.
- Keep existing unrelated untracked workspace files untouched.

---

### Task 1: Repair default memberships before token issuance

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Authentication/Services/DefaultTenantMembershipProvisioner.cs`
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Authentication/Services/LocalAuthService.cs`
- Modify: `apps/api/tests/GameGuild.Identity.Authentication.UnitTests/Services/LocalAuthServiceTests.cs`
- Modify: `apps/api/tests/GameGuild.Identity.Authentication.UnitTests/Services/OAuthAuthServiceTests.cs`

**Interfaces:**
- Consumes: `GetDefaultTenantQuery`, `GetUserMembershipsQuery`, `AddTenantMemberCommand`.
- Produces: `DefaultTenantMembershipProvisioner.EnsureAsync(ISender, Guid, CancellationToken)` which guarantees an active default membership when an active default tenant exists.

- [ ] **Step 1: Write the failing tests**

Add a local-sign-in test whose sender returns an inactive `SystemAdmin` membership for the active default tenant before repair and an active `SystemAdmin` membership after repair. Assert the emitted command preserves `SystemAdmin` and the issued token uses the default tenant. Add the equivalent Google ID-token test and a refresh-token test proving repair happens before tenant resolution.

```csharp
capturedCommand.Should().NotBeNull();
capturedCommand!.Role.Should().Be("SystemAdmin");
capturedTenantId.Should().Be(defaultTenant.Id);
```

- [ ] **Step 2: Verify RED**

Run `dotnet test apps/api/tests/GameGuild.Identity.Authentication.UnitTests/GameGuild.Identity.Authentication.UnitTests.csproj --no-restore -m:1 --filter "FullyQualifiedName~LocalAuthServiceTests|FullyQualifiedName~OAuthAuthServiceTests"`.

Expected: the new tests fail because local sign-in/refresh do not provision and the provisioner returns early for inactive memberships.

- [ ] **Step 3: Implement the repair**

Replace the existence-only early return with an active-membership guard and preserve the inactive membership's role in `AddTenantMemberCommand`.

```csharp
var membership = memberships.Memberships.FirstOrDefault(item => item.TenantId == defaultTenant.Id);
if (membership?.IsActive == true) return;

var role = string.IsNullOrWhiteSpace(membership?.Role) ? MemberRole : membership.Role;
var result = await sender.Send(new AddTenantMemberCommand(defaultTenant.Id, userId, role), cancellationToken);
```

Call `EnsureAsync` in `LocalSignInAsync` and `RefreshTokenAsync` immediately before `ResolveTenantAccessContextAsync`. Keep the existing Google ID-token call before its resolution.

- [ ] **Step 4: Verify GREEN and commit**

Run the command from Step 2. Then stage only the four Task 1 files and commit with `fix(auth): enforce active default tenant membership`.

### Task 2: Make Testing Lab event creation valid by default

**Files:**
- Modify: `apps/web/src/components/testing-lab/testing-event-management.tsx`
- Modify: `apps/web/src/components/testing-lab/testing-event-management.test.tsx`
- Modify: `apps/web/src/lib/testing-lab/events-actions.ts`
- Modify: `apps/web/src/lib/testing-lab/events-actions.test.ts`

**Interfaces:**
- Consumes: `createTestingEvent(FormData)` and the existing `CreateTestingEventDialog` sheet.
- Produces: a fresh event drawer whose four schedule values are chronological, whose edits preserve chronological ordering, and whose error surface never exposes only `TestingLab.Validation`.

- [ ] **Step 1: Write the failing tests**

Replace the current expectation that changing the event start mirrors the same value into the end. Assert a new event drawer supplies all four schedule fields in increasing order and moving the start produces an end at least one hour later. Add an action test where a failed API result only contains `message: "TestingLab.Validation"` and assert human-readable schedule copy.

```tsx
expect(new Date(endsAt.value).getTime()).toBeGreaterThan(new Date(startsAt.value).getTime());
expect(result).toEqual({ success: false, error: 'Check the application window and event schedule.' });
```

- [ ] **Step 2: Verify RED**

Run `pnpm --filter @game-guild/web test -- src/components/testing-lab/testing-event-management.test.tsx src/lib/testing-lab/events-actions.test.ts`.

Expected: the start/end test fails because both values are equal and the generic-code fallback is not translated.

- [ ] **Step 3: Implement chronological defaults and meaningful fallback**

When the drawer opens, seed a schedule where applications open at the next local hour, close one day later, the event starts one day after close, and ends two hours later. When an edited field passes a later field, move dependent fields forward while keeping an event duration of at least one hour. Retain API `detail` when present; when the only message is `TestingLab.Validation`, return `Check the application window and event schedule.`.

- [ ] **Step 4: Verify GREEN and commit**

Run the command from Step 2. Then stage only the four Task 2 files and commit with `fix(testing-lab): make event scheduling valid by default`.

### Task 3: Add pure calendar range and event-placement helpers

**Files:**
- Create: `apps/web/src/lib/testing-lab/calendar.ts`
- Create: `apps/web/src/lib/testing-lab/calendar.test.ts`

**Interfaces:**
- Consumes: `TestingLabTestingEventProjection` from `@game-guild/client`.
- Produces: `CalendarView`, `parseCalendarView`, `calendarRange`, `shiftCalendarAnchor`, `calendarEventSegments`, and `calendarRangeLabel`.

- [ ] **Step 1: Write the failing tests**

Use literal UTC fixtures for a single-day event, a multi-day event, and an unscheduled event. Assert Sunday-first month grid ranges, invalid view parsing defaults to `month`, weekend hiding removes Saturday/Sunday segments, and month navigation moves August to September 2026.

```ts
expect(parseCalendarView('invalid')).toBe('month');
expect(calendarRange(new Date('2026-08-10T12:00:00Z'), 'month', true).days).toHaveLength(42);
expect(shiftCalendarAnchor(new Date('2026-08-10T12:00:00Z'), 'month', 1).toISOString()).toContain('2026-09');
```

- [ ] **Step 2: Verify RED**

Run `pnpm --filter @game-guild/web test -- src/lib/testing-lab/calendar.test.ts`.

Expected: the test fails because the calendar module does not exist.

- [ ] **Step 3: Implement deterministic helpers**

Use `date-fns` for boundaries and local labels. Keep helpers pure; map invalid or missing `startsAt` events to `unscheduled` instead of discarding them. Sort segments by start time then title.

- [ ] **Step 4: Verify GREEN and commit**

Run the command from Step 2. Then stage the helper and test and commit with `feat(testing-lab): add calendar data helpers`.

### Task 4: Render the Google Calendar-style Testing Lab workspace

**Files:**
- Create: `apps/web/src/components/testing-lab/testing-lab-calendar.tsx`
- Create: `apps/web/src/components/testing-lab/testing-lab-calendar.test.tsx`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/(testing)/testing-lab/page.tsx`
- Modify: `apps/web/src/app/[locale]/(dashboard)/dashboard/(testing)/testing-lab/page.test.tsx`

**Interfaces:**
- Consumes: calendar helper exports, `getTestingEventsDirectory`, `CreateTestingEventDialog`, `Link`, and Testing Lab event projections.
- Produces: a route-backed operations menu bar and interactive calendar views linked to `/dashboard/testing-lab/events/:eventId`.

- [ ] **Step 1: Write the failing dashboard and component tests**

Assert the dashboard no longer renders Operations card descriptions, requests `getTestingEventsDirectory({ skip: 0, take: 100 })`, and renders an accessible operations navigation. Assert Month is selected by default, the view menu exposes Day/Week/Month/Year/Schedule/3 days, the weekend toggle changes the grid, navigation changes the range label, and an event links to management.

```tsx
expect(screen.getByRole('navigation', { name: 'Testing Lab operations' })).toBeInTheDocument();
expect(screen.getByRole('button', { name: 'Month view' })).toHaveAttribute('aria-pressed', 'true');
expect(screen.getByRole('link', { name: /August campus playtest/i })).toHaveAttribute('href', '/dashboard/testing-lab/events/event-1');
```

- [ ] **Step 2: Verify RED**

Run `pnpm --filter @game-guild/web test -- "src/app/[locale]/(dashboard)/dashboard/(testing)/testing-lab/page.test.tsx" src/components/testing-lab/testing-lab-calendar.test.tsx`.

Expected: tests fail because the page still renders Operations cards and no calendar component exists.

- [ ] **Step 3: Implement the workspace**

Load events in parallel with existing dashboard and analytics requests. Replace the Operations card grid with compact labelled icon navigation using the existing Testing Lab Lucide icon set and GameGuild tooltip/button primitives. Implement Today, previous/next, a range label, New event, a Month-default view menu, and a Show weekends checkbox. Render every selected view from helper output; mobile uses a compact agenda list for dense time views.

- [ ] **Step 4: Verify GREEN, lint, and build**

Run the Task 2–4 tests, then run `pnpm --filter @game-guild/web lint -- src/components/testing-lab/testing-lab-calendar.tsx src/lib/testing-lab/calendar.ts "src/app/[locale]/(dashboard)/dashboard/(testing)/testing-lab/page.tsx"` and `pnpm --filter @game-guild/web build`.

- [ ] **Step 5: Commit the workspace**

Stage the two component files, two helper files, and dashboard page/test. Commit with `feat(testing-lab): add operations calendar workspace`.

### Task 5: Final verification and branch handoff

**Files:**
- Verify all files changed in Tasks 1–4.

- [ ] **Step 1: Run focused verification**

Run `dotnet test apps/api/tests/GameGuild.Identity.Authentication.UnitTests/GameGuild.Identity.Authentication.UnitTests.csproj --no-restore -m:1` and `pnpm --filter @game-guild/web test -- src/lib/testing-lab/calendar.test.ts src/lib/testing-lab/events-actions.test.ts src/components/testing-lab/testing-event-management.test.tsx src/components/testing-lab/testing-lab-calendar.test.tsx "src/app/[locale]/(dashboard)/dashboard/(testing)/testing-lab/page.test.tsx"`.

- [ ] **Step 2: Inspect the rendered dashboard**

Start the existing web app and verify Month, Week, Year, Schedule, weekend toggle, calendar navigation, New event defaults, and management navigation. If an authenticated browser session is unavailable, record the limitation instead of claiming live visual verification.

- [ ] **Step 3: Review and hand off**

Run `git diff origin/develop...HEAD --check` and `git status --short`. Report verification results, commits, and the exact merge/push state without altering unrelated untracked files.
