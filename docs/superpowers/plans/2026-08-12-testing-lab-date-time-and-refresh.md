# Testing Lab Date-Time and Refresh Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace every native Testing Lab date-time control with one accessible shadcn picker and prevent refresh-token rotation from leaving a stale authenticated interface.

**Architecture:** Put the reusable date-time field in `@game-guild/ui`, retain existing `FormData` names and wall-clock values, and convert event schedule coupling to React state. Persist refreshed cookies in the Next auth proxy response and make API rotation idempotent only for a recent same-IP retry whose replacement token is still active.

**Tech Stack:** React 19, TypeScript, shadcn/Radix UI, react-day-picker, Vitest/Testing Library, Next.js 16 proxy, .NET 10, xUnit, Moq, FluentAssertions.

## Global Constraints

- Use no native `datetime-local` input in Testing Lab.
- Preserve the exact `YYYY-MM-DDTHH:mm` `FormData` contract and show `UTC` explicitly.
- Keep `apps/web/src/proxy.ts` limited to auth plus `next-intl` composition and its standard matcher.
- Refresh replay recovery is limited to 30 seconds by default, the same client IP, and an active replacement token.
- Preserve unrelated untracked workspace files.

---

### Task 1: Add the reusable shadcn DateTimePicker

**Files:**
- Create: `packages/infrastructure/ui/src/components/date-time-picker.tsx`
- Create: `apps/web/src/components/testing-lab/date-time-picker.test.tsx`

**Interfaces:**
- Produces: `DateTimePickerProps` with `id`, `name`, `value`, `defaultValue`, `onValueChange`, `required`, `disabled`, `placeholder`, and `timezoneLabel`.
- Produces: a hidden named field containing a literal `YYYY-MM-DDTHH:mm` value.

- [ ] **Step 1: Write failing interaction tests** for exact form value, Apply, Cancel, Clear, required clearing, accessible trigger, and timezone text.
- [ ] **Step 2: Run `pnpm --filter @game-guild/web test -- src/components/testing-lab/date-time-picker.test.tsx`** and verify failure because the component does not exist.
- [ ] **Step 3: Implement the component** from existing `Button`, `Calendar`, `Input`, `Popover`, and `Select` primitives. Keep a draft selection while open and commit only on Apply.
- [ ] **Step 4: Run the focused test** and verify it passes.

### Task 2: Replace all Testing Lab native date-time fields

**Files:**
- Modify: `apps/web/src/components/testing-lab/testing-event-management.tsx`
- Modify: `apps/web/src/components/testing-lab/testing-event-management.test.tsx`
- Modify: `apps/web/src/components/testing-lab/testing-lab-dialogs.tsx`
- Modify: `apps/web/src/components/testing-lab/testing-lab-access-management.tsx`

**Interfaces:**
- Consumes: `DateTimePicker`.
- Preserves: form field names `applicationsOpenAt`, `applicationsCloseAt`, `startsAt`, `endsAt`, `recurrenceEndsAt`, `startDate`, `endDate`, `startTime`, and `endTime`.

- [ ] **Step 1: Add a failing workflow test** proving a start-date change updates the controlled end-date picker and that submitted form data stays chronological.
- [ ] **Step 2: Run the focused Testing Lab tests** and verify the controlled-picker assertion fails.
- [ ] **Step 3: Replace every native input** and move the four event schedule fields to controlled state, preserving edit defaults and dependent-date adjustment.
- [ ] **Step 4: Run focused web tests and typecheck** and verify no Testing Lab workflow renders a native date-time input.

### Task 3: Persist refresh cookies in the Next proxy response

**Files:**
- Modify: `packages/infrastructure/client/src/integrations/next/actions.ts`
- Modify: `packages/infrastructure/client/tests/next/actions-extended.test.ts`
- Modify: `apps/web/src/proxy.ts`

**Interfaces:**
- Produces: `auth(handler)` responses with every refreshed or expired session cookie appended as `Set-Cookie`.
- Consumes: the existing `createMiddleware(routing)` next-intl handler.

- [ ] **Step 1: Add failing tests** for replacement `Set-Cookie` headers and deletion headers when an encrypted session becomes invalid.
- [ ] **Step 2: Run `pnpm --filter @game-guild/client test -- tests/next/actions-extended.test.ts`** and verify the response has no session cookie before the fix.
- [ ] **Step 3: Implement buffered cookie mutations** in the proxy adapter, apply them to the handler response, and compose the web proxy with next-intl in a few lines.
- [ ] **Step 4: Run client tests and typecheck** and verify the headers and types pass.

### Task 4: Make recent refresh rotation retries idempotent

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.Identity.Authentication/Services/LocalAuthService.cs`
- Modify: `apps/api/Tests/GameGuild.Identity.Authentication.UnitTests/Services/LocalAuthServiceTests.cs`

**Interfaces:**
- Consumes: `RefreshToken.ReplacedByToken`, `RevokedAt`, `RevokedByIp`, and `IRefreshTokenRepository.GetByTokenAsync`.
- Produces: recovery only when the old token was rotated within `Jwt:RefreshTokenRotationGraceSeconds`, from the same IP, and its replacement is active.

- [ ] **Step 1: Add failing unit tests** for a recent same-IP retry and for rejection after the grace interval or from another IP.
- [ ] **Step 2: Run the focused LocalAuthService tests** and verify the recent retry is unauthorized before implementation.
- [ ] **Step 3: Implement guarded retry recovery** and reuse tenant resolution/access-token generation while returning the active replacement refresh token.
- [ ] **Step 4: Run focused then complete authentication unit tests** and verify all pass.

### Task 5: Verify, commit, merge, and publish

**Files:**
- Verify all files changed in Tasks 1–4.

- [ ] **Step 1: Run focused web, client, and API tests**, followed by client/web typecheck and the production web build.
- [ ] **Step 2: Run browser smoke checks** for event creation/editing and session continuity when production is available.
- [ ] **Step 3: Commit on `develop`**, push `develop`, merge `develop` into `main`, push `main`, and report exact commit and deployment verification without touching unrelated files.
