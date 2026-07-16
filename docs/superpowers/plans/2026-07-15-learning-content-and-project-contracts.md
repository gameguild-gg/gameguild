# Learning Content And Project Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the backend contracts for learning content and ensure the existing Project aggregate is shared by Projects, Testing Lab, Launch Pad, and Store.

**Architecture:** Keep course delivery in Learning.Courses, graded work in Learning.Assessments, and the canonical project aggregate in GameGuild.Projects. Cross-context capabilities use explicit link entities and IDs rather than duplicate project or assessment models. Every slice is developed on a small branch from `develop`, tested, committed, and merged back before the next slice branches.

**Tech Stack:** .NET 10, ASP.NET Core, Entity Framework Core, PostgreSQL, GameGuild.CQRS, FluentValidation, xUnit, FluentAssertions.

## Global Constraints

- Never modify or merge into `main` unless the user explicitly requests it.
- Use `develop` as the sole integration branch.
- Use a dedicated worktree and small branch for each task.
- Name branches after the product domain or capability, not the task number or execution phase.
- Immediately after a verified merge into `develop`, remove the task worktree and delete its local and remote branches.
- Write failing tests before production code.
- Keep persisted enum values explicit and migrations backwards compatible.
- Preserve existing API behavior while adding explicit contracts.

---

### Task 1: Lesson Formats And Tracking

**Branch:** `feat/learning-lesson-contracts`

**Produces:** Explicit Markdown, Lexical, Reveal.js, and Video formats; non-graded lesson invariants; second-level engagement and video event tracking.

**Status:** Complete. Merged as `aee581d10e`; final Courses verification passed `296/296` with zero skips.

- [x] Write failing entity, mapping, handler, and controller tests.
- [x] Add lesson format and interaction event contracts.
- [x] Enforce that lessons cannot carry grading configuration.
- [x] Add fine-grained engagement events and video position/quiz cue event support.
- [x] Add EF configuration and backwards-compatible migration.
- [x] Run Courses tests and API build.
- [x] Commit and merge to `develop`.

### Task 2: Assignment Delivery And Grading Contracts

**Branch:** `feat/learning-assignment-contracts`

**Produces:** Assignment modalities, single-step/continuous rendering, availability, due dates, late policy, and persisted submission payloads.

**Status:** Complete. Initial contracts merged as `1af70012c0`; integrity hardening merged as `1e3856551b`. Final Assessments verification passed `137/137` with zero skips.

- [x] Write failing assessment and submission tests.
- [x] Add explicit submission modality and presentation mode enums.
- [x] Add due-date and availability validation.
- [x] Persist text, file, URL, code, media, project, and structured-answer submissions.
- [x] Add interactive-video assessment cue links.
- [x] Add EF configuration and migration.
- [x] Run Assessments/Courses tests and API build.
- [x] Commit and merge to `develop`.

### Task 3: Discussion, Reflection, And Survey Contracts

**Branch:** `feat/learning-activity-contracts`

**Produces:** Explicit behavior and validation for discussion/forum, reflection, and non-graded survey content.

**Status:** Complete. Contracts merged as `3f208893c0`; policy hardening merged as `25c7a1b628`.

- [x] Write failing content policy tests.
- [x] Add activity-specific settings and response contracts.
- [x] Enforce survey non-grading and type-safe submission behavior.
- [x] Run focused tests and API build.
- [x] Commit and merge to `develop`.

### Task 4: Shared Project Channels

**Branch:** `feat/project-channel-contracts`

**Produces:** One canonical Project that can be exposed in Projects, Testing Lab, Launch Pad, and Store without copying project data.

**Status:** Complete. Channel contracts merged as `42ad0a0569`; lifecycle, tenant isolation, race safety, hard delete, and migration rollback hardening merged as `721be15dc8` after an independent review returned zero findings.

- [x] Write failing integration and model-configuration tests.
- [x] Preserve existing Testing Lab SessionProject and LaunchPlan links.
- [x] Add a Store product-to-project link and channel availability contract.
- [x] Validate project existence and lifecycle rules for every channel.
- [x] Add EF configuration and migration.
- [x] Run Projects, Testing Lab, Launch Pad, Commerce Products, and API tests.
- [x] Commit and merge to `develop`.

### Task 5: Consolidated Verification

**Status:** Complete on 2026-07-16.

- [x] Build the API host with `--warnaserror`: 0 warnings and 0 errors.
- [x] Run all affected module tests with no skips: 2,435 passed across Projects, Testing Lab, Launch Pad, Products, Products Integration, Courses, Assessments, Authorization, Identity Context, and Users.
- [x] Validate migrations and lifecycle races against disposable PostgreSQL databases: 21 passed, 0 failed, 0 skipped.
- [x] Verify `develop` is clean and synchronized with `origin/develop` at `721be15dc8` before the documentation-only close-out commit.
- [x] Remove completed implementation worktrees and delete their local/remote delivery branches after integration.

## Final Verification Evidence

| Scope | Result |
| --- | --- |
| Projects | 158 passed |
| Testing Lab | 90 passed |
| Launch Pad | 24 passed |
| Commerce Products | 554 passed |
| Commerce Products Integration | 10 passed |
| Learning Courses | 296 passed |
| Learning Assessments | 137 passed |
| Identity Authorization | 559 passed |
| Identity Context | 101 passed |
| Identity Users | 506 passed |
| Project-channel PostgreSQL migration/race suite | 21 passed |
| API strict build | 0 warnings, 0 errors |

All listed test runs completed with zero skips. The project-channel corrective review ended with spec compliance `PASS`, code quality `APPROVED`, and zero remaining findings.
