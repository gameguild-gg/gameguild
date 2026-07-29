# Testing Lab Event Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Implement manager-created Testing Lab events with scheduled slots, project applications, controlled approval, tester registration, mandatory feedback, and Learning completion evidence.

**Architecture:** Extend the existing Testing Lab module with a `TestingEvent` aggregate above existing sessions. Commands and queries enforce tenant, actor, approval, capacity, and feedback invariants; thin controllers expose the contracts. The web uses Server Components and server actions through generated `@game-guild/client` modules.

**Tech Stack:** .NET 10, EF Core/PostgreSQL, GameGuild CQRS, Next.js 16, React 19, pnpm, Vitest, Playwright.

## Global Constraints

- Existing projects remain the only source for project applications.
- Project capacity is consumed only after approval and slot assignment.
- In-person slots require campus and room data.
- Rejections require rationale.
- All writes are tenant-scoped and audited.
- Do not introduce Zustand or React Query into this flow.
- Use pnpm and shell scripts only.

---

### Task 1: Event Domain And Persistence

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingEvent.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingEventSlot.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingProjectApplication.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingCommitteeMember.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingApplicationVote.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingFeedbackObligation.cs`
- Modify: `apps/api/Source/Modules/GameGuild.TestingLab/Entities/TestingSession.cs`
- Modify: `apps/api/Source/Modules/GameGuild.TestingLab/Configuration/TestingLabModelConfiguration.cs`
- Modify: `apps/api/Source/GameGuild.API/Database/ApplicationDbContext.cs`
- Create: `apps/api/Source/GameGuild.API/Database/Migrations/20260729101500_AddTestingLabEventWorkflow.cs`
- Test: `apps/api/tests/GameGuild.TestingLab.UnitTests/TestingEventDomainTests.cs`

**Interfaces:**
- Produces: `TestingEvent`, `TestingEventSlot`, `TestingProjectApplication`, `TestingCommitteeMember`, `TestingApplicationVote`, and `TestingFeedbackObligation`.

- [x] Write failing tests for in-person location requirements, application state transitions, rejection rationale, one active assignment, duplicate votes, and feedback completion.
- [x] Run `dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --filter FullyQualifiedName~TestingEventDomainTests`.
- [x] Implement entities with explicit enums and domain methods.
- [x] Register relationships, indexes, tenant filters, and uniqueness constraints.
- [x] Generate and inspect the EF migration.
- [x] Re-run the focused tests and API build.
- [x] Commit domain and persistence changes.

### Task 2: CQRS Event, Slot, And Application Operations

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingEventContracts.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingEventHandlers.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingApplicationContracts.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingApplicationHandlers.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Controllers/TestingEventsController.cs`
- Test: `apps/api/tests/GameGuild.TestingLab.UnitTests/TestingEventHandlerTests.cs`
- Test: `apps/api/tests/GameGuild.API.UnitTests/Database/TestingEventPostgreSqlConcurrencyTests.cs`

**Interfaces:**
- Produces: event and slot CRUD queries/commands; submit, withdraw, review, approve, reject, waitlist, and assign-slot application commands.

- [x] Write failing handler tests for actor ownership, project membership, manager-only decisions, committee majority, tie resolution, and required rationale.
- [x] Write PostgreSQL tests that concurrently approve applications into the last project slot.
- [x] Implement handlers through `IApplicationDbContext`, `IActorContextAccessor`, project authorization, and advisory locking.
- [x] Add thin CQRS controllers with resource permissions and structured errors.
- [x] Run focused unit, PostgreSQL, and controller tests.
- [x] Commit CQRS and API operations.

### Task 3: Tester Capacity, Attendance, And Feedback Obligations

**Files:**
- Modify: `apps/api/Source/Modules/GameGuild.TestingLab/Services/TestingParticipantOperationsService.cs`
- Modify: `apps/api/Source/Modules/GameGuild.TestingLab/Services/TestingFeedbackOperationsService.cs`
- Modify: `apps/api/Source/Modules/GameGuild.TestingLab/Controllers/TestingParticipantsController.cs`
- Modify: `apps/api/Source/Modules/GameGuild.TestingLab/Controllers/TestingFeedbackController.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingParticipationContracts.cs`
- Test: `apps/api/tests/GameGuild.TestingLab.UnitTests/TestingEventParticipationTests.cs`

**Interfaces:**
- Produces: slot registration, waitlist promotion, check-in, project-tested assignment, feedback obligation, and participation-completion operations.

- [x] Write failing tests for in-person capacity, unlimited online capacity, deterministic waitlist promotion, no-show, and incomplete required feedback.
- [x] Implement atomic registration and waitlist movement.
- [x] Create feedback obligations from attendance and assigned projects.
- [x] Complete participation only when configured obligations are fulfilled.
- [x] Run focused tests.
- [x] Commit participation and feedback changes.

### Task 4: Learning Evidence Integration

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingLearningPolicy.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Events/TestingLearningEvidenceCompletedEvent.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning/TestingLab/TestingLabLearningEvidenceHandler.cs`
- Test: `apps/api/tests/GameGuild.TestingLab.UnitTests/TestingLearningPolicyTests.cs`
- Test: `apps/api/tests/GameGuild.Learning.UnitTests/TestingLabLearningEvidenceHandlerTests.cs`

**Interfaces:**
- Produces: event linkage to course/cohort/activity and `TestingLearningEvidenceCompletedEvent`.

- [x] Write failing policy tests for attendance, feedback, project presentation, and combined completion.
- [x] Implement policy evaluation without calculating grades in Testing Lab.
- [x] Publish idempotent completion evidence.
- [x] Consume evidence in Learning and update the linked activity record exactly once.
- [x] Run both module test projects.
- [x] Commit Learning integration.

### Task 5: Generated Client And Dashboard

**Files:**
- Modify generated contracts under: `packages/api-client/src/generated`
- Create: `apps/web/src/lib/testing-lab/events-queries.ts`
- Create: `apps/web/src/lib/testing-lab/events-actions.ts`
- Create: `apps/web/src/components/testing-lab/testing-event-dialogs.tsx`
- Create: `apps/web/src/components/testing-lab/testing-application-review.tsx`
- Create routes under: `apps/web/src/app/[locale]/(dashboard)/dashboard/(testing)/testing-lab/events`
- Modify: `apps/web/src/components/testing-lab/testing-lab-nav.tsx`
- Test: `apps/web/src/lib/testing-lab/events-actions.test.ts`
- Test: `apps/web/src/lib/testing-lab/events-queries.test.ts`

**Interfaces:**
- Consumes: generated Testing Lab event/application modules.
- Produces: manager event directory, event detail, slots, application review, committee, registrations, attendance, and feedback-obligation screens.

- [x] Regenerate the API client and verify no handwritten fetch is introduced.
- [x] Write failing query/action mapping tests.
- [x] Implement manager event CRUD and slot scheduling dialogs.
- [x] Implement application review with approval, rejection rationale, committee votes, and slot assignment.
- [x] Implement filters, search, pagination, selection, bulk review, confirmations, retry, and success states.
- [x] Run frontend unit tests, types, and lint.
- [x] Commit client and manager dashboard.

### Task 6: Public Applicant And Tester Experience

**Files:**
- Modify: `apps/web/src/app/[locale]/(contents)/(testing-lab)/testing-lab/page.tsx`
- Create: `apps/web/src/app/[locale]/(contents)/(testing-lab)/testing-lab/events/[eventId]/page.tsx`
- Create: `apps/web/src/components/testing-lab/testing-project-application.tsx`
- Create: `apps/web/src/components/testing-lab/testing-slot-registration.tsx`
- Create: `apps/web/src/components/testing-lab/testing-feedback-submission.tsx`
- Test: `apps/web/src/components/testing-lab/testing-project-application.test.tsx`
- Test: `apps/web/src/components/testing-lab/testing-slot-registration.test.tsx`
- Test: `apps/web/src/components/testing-lab/testing-feedback-submission.test.tsx`

**Interfaces:**
- Produces: real public event directory, authenticated project application, application tracking, tester slot registration/waitlist, and required feedback submission.

- [x] Write failing component tests for anonymous, applicant, approved, rejected, registered, waitlisted, attended, and feedback-pending states.
- [x] Implement public event and slot views using generated API data.
- [x] Implement project application without reserving capacity.
- [x] Implement tester registration and feedback obligation completion.
- [x] Verify responsive, keyboard, loading, empty, error, and retry states.
- [x] Commit public participant experience.

### Task 7: Complete Verification And Delivery

**Files:**
- Extend: `apps/web/src/lib/__tests__/e2e/testing-lab.e2e.test.ts`
- Create: `apps/web/scripts/testing-lab-browser-e2e.mjs`
- Modify: `apps/web/package.json`
- Update: `docs/superpowers/specs/2026-07-29-testing-lab-event-workflow-design.md`

**Interfaces:**
- Produces: repeatable API integration and Playwright evidence for manager, applicant, committee reviewer, and tester journeys.

- [x] Add API E2E for event creation, slots, project application, review, assignment, capacity, registration, attendance, feedback, reports, settings, roles, and permissions.
- [x] Add Playwright for all dashboard routes and all four user journeys.
- [x] Run Testing Lab unit tests with Coverlet and report the real line, branch, and method metrics.
- [x] Run API build, generated-client build, web typecheck, lint, unit tests, API E2E, and Playwright.
- [x] Inspect desktop and mobile screenshots for clipping, overlap, empty states, and dialogs.
- [x] Commit verification and documentation.
- [x] Merge the feature branch into `develop`, push `develop`, and remove the merged branch and worktree.
## Completion Evidence

Completed on 2026-07-29 in `feat/testing-lab-dashboard-completion` before integration into `develop`.

- API host build: succeeded with 0 warnings and 0 errors.
- Testing Lab unit suite: 157 passed, 0 failed, 0 skipped.
- Learning evidence integration: 6 passed, 0 failed, 0 skipped.
- PostgreSQL concurrency: last-slot approval and 15 project lifecycle race tests passed.
- Frontend Testing Lab suite: 55 passed across 17 files.
- API E2E: 6 passed across the legacy request/session flow and the event workflow.
- Browser E2E: passed with real API data for anonymous, project owner, committee reviewer, manager, and tester journeys, including desktop and mobile viewport checks.
- Generated API client build, web TypeScript, and focused ESLint: passed.
- Real Coverlet result for `GameGuild.TestingLab`: 65.94% line, 41.26% branch, and 45.93% method coverage. This is the measured module baseline, not a 100% claim; remaining uncovered code is concentrated in legacy Testing Lab entities, repositories, GraphQL types, and operations services.