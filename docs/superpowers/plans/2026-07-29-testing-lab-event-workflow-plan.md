# Testing Lab Event Workflow Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

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

- [ ] Write failing tests for in-person location requirements, application state transitions, rejection rationale, one active assignment, duplicate votes, and feedback completion.
- [ ] Run `dotnet test apps/api/tests/GameGuild.TestingLab.UnitTests/GameGuild.TestingLab.UnitTests.csproj --filter FullyQualifiedName~TestingEventDomainTests`.
- [ ] Implement entities with explicit enums and domain methods.
- [ ] Register relationships, indexes, tenant filters, and uniqueness constraints.
- [ ] Generate and inspect the EF migration.
- [ ] Re-run the focused tests and API build.
- [ ] Commit domain and persistence changes.

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

- [ ] Write failing handler tests for actor ownership, project membership, manager-only decisions, committee majority, tie resolution, and required rationale.
- [ ] Write PostgreSQL tests that concurrently approve applications into the last project slot.
- [ ] Implement handlers through `IApplicationDbContext`, `IActorContextAccessor`, project authorization, and advisory locking.
- [ ] Add thin CQRS controllers with resource permissions and structured errors.
- [ ] Run focused unit, PostgreSQL, and controller tests.
- [ ] Commit CQRS and API operations.

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

- [ ] Write failing tests for in-person capacity, unlimited online capacity, deterministic waitlist promotion, no-show, and incomplete required feedback.
- [ ] Implement atomic registration and waitlist movement.
- [ ] Create feedback obligations from attendance and assigned projects.
- [ ] Complete participation only when configured obligations are fulfilled.
- [ ] Run focused tests.
- [ ] Commit participation and feedback changes.

### Task 4: Learning Evidence Integration

**Files:**
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/EventsWorkflow/TestingLearningPolicy.cs`
- Create: `apps/api/Source/Modules/GameGuild.TestingLab/Events/TestingLearningEvidenceCompletedEvent.cs`
- Create: `apps/api/Source/Modules/GameGuild.Learning/TestingLab/TestingLabLearningEvidenceHandler.cs`
- Test: `apps/api/tests/GameGuild.TestingLab.UnitTests/TestingLearningPolicyTests.cs`
- Test: `apps/api/tests/GameGuild.Learning.UnitTests/TestingLabLearningEvidenceHandlerTests.cs`

**Interfaces:**
- Produces: event linkage to course/cohort/activity and `TestingLearningEvidenceCompletedEvent`.

- [ ] Write failing policy tests for attendance, feedback, project presentation, and combined completion.
- [ ] Implement policy evaluation without calculating grades in Testing Lab.
- [ ] Publish idempotent completion evidence.
- [ ] Consume evidence in Learning and update the linked activity record exactly once.
- [ ] Run both module test projects.
- [ ] Commit Learning integration.

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

- [ ] Regenerate the API client and verify no handwritten fetch is introduced.
- [ ] Write failing query/action mapping tests.
- [ ] Implement manager event CRUD and slot scheduling dialogs.
- [ ] Implement application review with approval, rejection rationale, committee votes, and slot assignment.
- [ ] Implement filters, search, pagination, selection, bulk review, confirmations, retry, and success states.
- [ ] Run frontend unit tests, types, and lint.
- [ ] Commit client and manager dashboard.

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

- [ ] Write failing component tests for anonymous, applicant, approved, rejected, registered, waitlisted, attended, and feedback-pending states.
- [ ] Implement public event and slot views using generated API data.
- [ ] Implement project application without reserving capacity.
- [ ] Implement tester registration and feedback obligation completion.
- [ ] Verify responsive, keyboard, loading, empty, error, and retry states.
- [ ] Commit public participant experience.

### Task 7: Complete Verification And Delivery

**Files:**
- Extend: `apps/web/src/lib/__tests__/e2e/testing-lab.e2e.test.ts`
- Create: `apps/web/scripts/testing-lab-browser-e2e.mjs`
- Modify: `apps/web/package.json`
- Update: `docs/superpowers/specs/2026-07-29-testing-lab-event-workflow-design.md`

**Interfaces:**
- Produces: repeatable API integration and Playwright evidence for manager, applicant, committee reviewer, and tester journeys.

- [ ] Add API E2E for event creation, slots, project application, review, assignment, capacity, registration, attendance, feedback, reports, settings, roles, and permissions.
- [ ] Add Playwright for all dashboard routes and all four user journeys.
- [ ] Run Testing Lab unit tests with Coverlet and report the real line, branch, and method metrics.
- [ ] Run API build, generated-client build, web typecheck, lint, unit tests, API E2E, and Playwright.
- [ ] Inspect desktop and mobile screenshots for clipping, overlap, empty states, and dialogs.
- [ ] Commit verification and documentation.
- [ ] Merge the feature branch into `develop`, push `develop`, and remove the merged branch and worktree.
