# Learning Content And Project Contracts Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the backend contracts for learning content and ensure the existing Project aggregate is shared by Projects, Testing Lab, Launch Pad, and Store.

**Architecture:** Keep course delivery in Learning.Courses, graded work in Learning.Assessments, and the canonical project aggregate in GameGuild.Projects. Cross-context capabilities use explicit link entities and IDs rather than duplicate project or assessment models. Every slice is developed on a small branch from `develop`, tested, committed, and merged back before the next slice branches.

**Tech Stack:** .NET 10, ASP.NET Core, Entity Framework Core, PostgreSQL, GameGuild.CQRS, FluentValidation, xUnit, FluentAssertions.

## Global Constraints

- Never modify or merge into `main` unless the user explicitly requests it.
- Use `develop` as the sole integration branch.
- Use a dedicated worktree and small branch for each task.
- Write failing tests before production code.
- Keep persisted enum values explicit and migrations backwards compatible.
- Preserve existing API behavior while adding explicit contracts.

---

### Task 1: Lesson Formats And Tracking

**Branch:** `feat/learning-lesson-contracts`

**Produces:** Explicit Markdown, Lexical, Reveal.js, and Video formats; non-graded lesson invariants; second-level engagement and video event tracking.

- [ ] Write failing entity, mapping, handler, and controller tests.
- [ ] Add lesson format and interaction event contracts.
- [ ] Enforce that lessons cannot carry grading configuration.
- [ ] Add fine-grained engagement events and video position/quiz cue event support.
- [ ] Add EF configuration and backwards-compatible migration.
- [ ] Run Courses tests and API build.
- [ ] Commit and merge to `develop`.

### Task 2: Assignment Delivery And Grading Contracts

**Branch:** `feat/learning-assignment-contracts`

**Produces:** Assignment modalities, single-step/continuous rendering, availability, due dates, late policy, and persisted submission payloads.

- [ ] Write failing assessment and submission tests.
- [ ] Add explicit submission modality and presentation mode enums.
- [ ] Add due-date and availability validation.
- [ ] Persist text, file, URL, code, media, project, and structured-answer submissions.
- [ ] Add interactive-video assessment cue links.
- [ ] Add EF configuration and migration.
- [ ] Run Assessments/Courses tests and API build.
- [ ] Commit and merge to `develop`.

### Task 3: Discussion, Reflection, And Survey Contracts

**Branch:** `feat/learning-activity-contracts`

**Produces:** Explicit behavior and validation for discussion/forum, reflection, and non-graded survey content.

- [ ] Write failing content policy tests.
- [ ] Add activity-specific settings and response contracts.
- [ ] Enforce survey non-grading and type-safe submission behavior.
- [ ] Run focused tests and API build.
- [ ] Commit and merge to `develop`.

### Task 4: Shared Project Channels

**Branch:** `feat/project-channel-contracts`

**Produces:** One canonical Project that can be exposed in Projects, Testing Lab, Launch Pad, and Store without copying project data.

- [ ] Write failing integration and model-configuration tests.
- [ ] Preserve existing Testing Lab SessionProject and LaunchPlan links.
- [ ] Add a Store product-to-project link and channel availability contract.
- [ ] Validate project existence and lifecycle rules for every channel.
- [ ] Add EF configuration and migration.
- [ ] Run Projects, Testing Lab, Launch Pad, Commerce Products, and API tests.
- [ ] Commit and merge to `develop`.

### Task 5: Consolidated Verification

- [ ] Build the API host with warnings treated according to repository policy.
- [ ] Run all affected module tests with no skips.
- [ ] Validate migrations against a disposable PostgreSQL database.
- [ ] Verify `develop` is clean and synchronized with origin.
- [ ] Remove completed temporary worktrees after integration.
