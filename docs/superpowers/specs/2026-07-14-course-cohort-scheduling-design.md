# Course Cohort Scheduling Design

Date: 2026-07-14
Status: Approved for implementation
Scope: GameGuild professor dashboard, Learning.Cohorts API, cohort-specific scheduling, content release, and assessment timing

## Summary

GameGuild will distinguish the reusable course definition from each concrete delivery of that course:

- A **course** owns canonical modules, lessons, assessments, listing data, and certificate rules.
- A **cohort** (professor-facing label: **class**) owns its students, instructor, enrollment window, capacity, start and end dates, meeting cadence, schedule, and content release timing.
- A **session** (professor-facing label: **class meeting** or **live session**) is one scheduled meeting inside a cohort.

One course may run several cohorts concurrently. For example, a morning cohort and an evening cohort can start in different weeks, meet on different days, and release the same canonical course content at different times.

The approved UX has two levels:

1. A class control center listing every cohort for the course.
2. A cohort workspace for schedule, students, assessments, gradebook, and settings.

The canonical course content is referenced by cohort schedules; it is not duplicated for each cohort.

## Relationship to the Existing Course Design

This specification refines and supersedes the `Classes` ownership described in `2026-05-07-course-authoring-catalog-design.md`:

- `Content` owns canonical modules, lessons, ordering, and instructional assets.
- `Classes` owns cohorts and each cohort's delivery schedule.
- A cohort schedule references canonical content and determines when that content is released.
- A live class meeting is a schedule item inside a cohort, not the cohort itself.

All other source-of-truth, listing, academy, and course-readiness decisions in the earlier specification remain valid.

## Problem Statement

The current implementation conflates a cohort with a single scheduled class meeting:

- `Cohort.StartDate` is mapped to `CourseClass.scheduledAt`.
- The entire interval between `Cohort.StartDate` and `Cohort.EndDate` is mapped to one class duration.
- `Cohort.MeetingSchedule` is treated as a meeting URL even though it can represent broader schedule data.
- The Classes page places a permanent creation form beside the list, making the page read as a session scheduler instead of a cohort manager.
- No persisted cohort-specific mapping exists for lesson release dates, live meetings, assessment availability, or due dates.

This prevents the product from correctly representing multiple cohorts and prevents professors from controlling delivery pace independently for each cohort.

## Goals

- Make multiple cohorts of the same course immediately visible and manageable.
- Keep course content canonical and shared across cohorts.
- Give each cohort an independent calendar and release schedule.
- Generate a schedule from the course structure and cohort cadence.
- Support weekly, meeting-based, and manual content release.
- Let professors preview generated dates before applying them.
- Make conflicts, missing dates, and unpublished content visible before students are affected.
- Preserve enrolled-student data and prevent destructive cohort actions when students exist.
- Keep the experience usable with one cohort, several concurrent cohorts, and completed historical cohorts.

## Non-goals

- Copying or forking course content for each cohort.
- Building video-conference provider integrations in this slice.
- Replacing the existing course content editor.
- Building a full institutional room-booking system.
- Automatically changing historical schedules when canonical content is edited.
- Adding student-facing academy pages beyond the API contract required to enforce availability.

## Approved Terminology

| Domain concept | Professor-facing label | Meaning |
|---|---|---|
| `Program` | Course | Reusable canonical curriculum and listing |
| `Cohort` | Class | One delivery of the course to a specific student group |
| `CohortScheduleItem` with live type | Class meeting / Live session | One dated synchronous meeting inside a class |
| `ProgramContent` | Module / Lesson | Canonical course content referenced by schedules |
| Content availability | Release | When a class can access a course item |

Code and API contracts should use `Cohort` rather than the ambiguous `CourseClass`. UI copy may use `Class` where that language is clearer to professors.

## Information Architecture

### Course navigation

The existing course workspace keeps the `Classes` entry. Selecting it opens the class control center.

### Class control center

Route:

`/dashboard/learning/courses/{course}/classes`

The page contains:

- compact counts for active classes, active students, next meeting, and schedule conflicts
- search by class name or instructor
- filters for status and period
- a secondary `General calendar` view
- a full-width class table
- a `New class` command that opens a side sheet

The table columns are:

- class name
- instructor
- course period
- recurring meeting pattern
- enrollment count and capacity
- next class meeting
- status

Clicking a row opens that cohort workspace. Row actions are reserved for infrequent operations; opening the row is the primary action.

### Cohort workspace

Base route:

`/dashboard/learning/courses/{course}/classes/{cohort}`

The cohort identity is persistent in the header. A class selector allows switching cohorts without returning to the control center.

Workspace sections:

- `Overview`
- `Schedule & content`
- `Students`
- `Assessments`
- `Gradebook`
- `Settings`

The default section is `Schedule & content` because planning delivery is the primary class-management task.

### Schedule views

`Schedule & content` provides three views over the same persisted schedule:

- `Syllabus`: modules and items grouped into instructional weeks; default view
- `Calendar`: month/week representation for date conflicts and meetings
- `Timeline`: compact chronological list for operations and bulk shifts

Changing views does not create separate schedule data.

## New Class Flow

`New class` opens a side sheet so the control center remains readable.

Required inputs:

- name
- instructor
- timezone
- start date
- expected end date or number of instructional weeks
- capacity, where `0` means unlimited
- meeting days
- meeting start time
- meeting duration
- delivery location or default meeting URL

Optional inputs:

- description
- enrollment opening and closing dates
- holidays and skipped dates
- default content release policy

After creation, the professor enters the cohort workspace and can generate its detailed schedule.

## Schedule Builder

`Edit schedule` opens a side sheet with two explicit stages.

### Stage 1: Rules

The professor configures:

- timezone
- first instructional date
- meeting weekdays and times
- meeting duration
- pacing mode
- skipped dates and holidays
- default release policy
- default assessment due rule

Supported pacing modes:

- one module per week
- one lesson per class meeting
- a fixed number of lessons per week
- custom/manual

Supported release policies:

- release the full module at the start of its instructional week
- release each lesson before its mapped class meeting
- manual release
- immediately available when the cohort opens

### Stage 2: Preview

The system generates a preview from canonical modules, lessons, and assessments. The preview shows:

- instructional week
- referenced course item
- release date
- live meeting date when applicable
- assessment availability and due date
- conflicts and missing configuration

No persisted schedule changes occur until the professor selects `Apply schedule`.

## Manual Schedule Operations

After generation, the professor may:

- edit one item's release date
- edit one live meeting's date, time, location, or URL
- assign or change assessment availability and due dates
- move an item between instructional weeks
- mark an item as asynchronous
- add an exceptional live session that does not map to a lesson
- hide an item from this cohort without deleting canonical content
- reschedule only one item
- shift the selected item and all following items

Bulk shifts always show the affected item count and final date before confirmation.

## Data Model

### Existing `Cohort`

`Cohort` remains the aggregate root for one class delivery. It continues to own:

- course ID
- tenant ID
- name and description
- start and end dates
- capacity and enrollment count
- status and enrollment state
- instructor ID

The ambiguous `MeetingSchedule` string remains readable during migration but is no longer the canonical structured schedule.

### `CohortSchedule`

One schedule policy record belongs to one cohort.

Fields:

- `Id`
- `CohortId`, unique
- `TimezoneId`
- `PacingMode`
- `UnitsPerPeriod`
- `ReleasePolicy`
- `DefaultAssessmentDueOffset`
- `MeetingDays`
- `MeetingStartTime`
- `MeetingDurationMinutes`
- `DefaultLocation`
- `DefaultMeetingUrl`
- timestamps and tenant metadata

Structured meeting days must use a JSON-owned value or normalized child records, not an unvalidated free-form string.

### `CohortScheduleItem`

A schedule item references canonical course data and stores only cohort-specific delivery metadata.

Fields:

- `Id`
- `CohortId`
- `ProgramContentId`, nullable for exceptional sessions
- `AssessmentId`, nullable
- `Type`: content release, live session, assessment window, milestone
- `InstructionalWeek`
- `SortOrder`
- `StartsAt`, nullable
- `EndsAt`, nullable
- `AvailableFrom`, nullable
- `AvailableUntil`, nullable
- `DueAt`, nullable
- `Location`, nullable
- `MeetingUrl`, nullable
- `VisibilityOverride`: inherited, hidden, visible
- `Status`: draft, scheduled, published, completed, cancelled
- optional title override for exceptional sessions
- timestamps and tenant metadata

A schedule item must reference at least one canonical item or have an explicit exceptional-session title.

### Content access rule

Student access to a scheduled content item is derived from:

1. active enrollment in the matching cohort
2. canonical content visibility
3. cohort schedule visibility override
4. `AvailableFrom` and `AvailableUntil`

The canonical `ProgramContent.Visibility` remains the broad course-level rule. Cohort schedule items supply the class-specific release window.

## API Design

New endpoints use the project's CQRS dispatcher and handlers. Controllers only validate transport concerns and dispatch commands or queries.

### Cohort control center

- existing `GET /api/cohorts/course/{courseId}`
  - extends its response with operational list data including next meeting and conflict count
- existing `POST /api/cohorts`
  - extended to accept structured initial class configuration
- existing cohort lifecycle endpoints remain available

### Cohort schedule

- `GET /api/cohorts/{cohortId}/schedule`
- `POST /api/cohorts/{cohortId}/schedule/preview`
- `PUT /api/cohorts/{cohortId}/schedule`
- `PATCH /api/cohorts/{cohortId}/schedule/items/{itemId}`
- `POST /api/cohorts/{cohortId}/schedule/items/{itemId}/shift`
- `POST /api/cohorts/{cohortId}/schedule/items`
- `DELETE /api/cohorts/{cohortId}/schedule/items/{itemId}`
- `GET /api/courses/{courseId}/cohorts/calendar?from={date}&to={date}`

`preview` is side-effect free. `PUT schedule` applies the approved preview atomically.

### Student delivery

- `GET /api/cohorts/{cohortId}/available-content`
  - returns only content available to the authenticated enrolled student

Authorization checks require course-management permission for professor operations and cohort enrollment for student delivery queries.

## Frontend Contracts

Replace the misleading `CourseClass` mapping with explicit models:

- `CourseCohortSummary`
- `CourseCohortDetail`
- `CohortSchedule`
- `CohortScheduleItem`
- `CohortSchedulePreview`
- `CohortScheduleConflict`

The current code that derives a session duration from the cohort start/end interval must be removed.

The generated API client is the transport source of truth. Handwritten mapping remains limited to view models and presentation labels.

## Conflict Detection

The preview and persisted schedule expose conflicts for:

- meeting outside the cohort period
- duplicate meetings at the same time for the same instructor
- release date after assessment due date
- assessment due before its referenced lesson is available
- content with no generated date
- skipped date or holiday collision
- end date exceeded after a bulk shift
- canonical content removed after being scheduled
- published schedule item missing required meeting location or URL

Conflicts are blocking or advisory. Blocking conflicts prevent schedule publication; advisory warnings allow explicit confirmation.

## Editing Canonical Content With Active Cohorts

Because cohorts reference shared course content:

- changing lesson copy updates the lesson for all cohorts
- adding new content leaves existing cohort schedules unchanged and surfaces an `Unscheduled content` warning
- reordering course content does not silently reorder published cohort schedules
- deleting content referenced by an active cohort is blocked until the schedule references are resolved
- removing content from a draft cohort schedule is allowed without deleting canonical content

## Lifecycle and Destructive Actions

- A cohort with no enrollments can be deleted after confirmation.
- A cohort with enrollments cannot be deleted.
- A scheduled cohort can be cancelled.
- An active cohort can be closed for enrollment without cancelling delivery.
- A completed cohort remains read-only except for permitted corrections and exports.
- Historical schedules and grades remain accessible after completion.

## Responsive Behavior

- Desktop: full operational table and two-pane schedule builder preview.
- Tablet: reduced class columns, horizontal calendar navigation, single-pane side sheet.
- Mobile: class cards replace the wide table; schedule defaults to timeline; calendar is secondary.
- The current cohort selector remains visible at every viewport size.

## Error and Recovery States

- Failed list loads show retry without hiding the course identity.
- Failed schedule previews preserve entered rules.
- Failed applies do not mutate the visible persisted schedule.
- Concurrent schedule edits return a version conflict and offer reload before overwrite.
- Empty classes show one clear `Create first class` action.
- Empty schedules show `Build schedule`; they do not render a permanent form.
- Partial API failures identify the affected section and preserve other loaded cohort data.

## Migration

1. Add schedule tables and explicit enum values.
2. Preserve all existing cohort records.
3. Treat existing `MeetingSchedule` as legacy data:
   - an HTTP value becomes the default meeting URL
   - other values remain available as a migration note for manual review
4. Do not generate schedule items automatically during the database migration.
5. Existing cohorts without schedules appear with `Schedule not configured` and can use the builder.
6. Keep compatibility reads until every deployed environment has completed the migration.

## Testing Strategy

### Backend unit tests

- cohort schedule validation and lifecycle
- schedule generation for each pacing mode
- weekly module release
- lesson-per-meeting release
- holidays and skipped dates
- assessment availability and due-date generation
- conflict severity classification
- single-item and following-item shifts
- deletion protection for referenced content and enrolled cohorts
- student content availability calculation

### Backend integration tests

- create multiple cohorts for one course with independent policies
- preview remains side-effect free
- schedule apply is atomic
- one cohort schedule never changes another cohort
- tenant and course permissions are enforced
- enrolled students see only released content for their cohort
- legacy cohort data remains readable

### Frontend tests

- class control center renders multiple statuses and independent dates
- filters and search operate without changing cohort data
- new class form is a side sheet
- cohort selector changes workspace context
- syllabus, calendar, and timeline use the same schedule
- schedule preview shows generated releases and conflicts
- unsaved rules survive a failed preview
- bulk shift confirmation shows scope and resulting dates
- completed cohorts are read-only
- mobile cards and timeline do not overflow

### Browser E2E

1. Create a course with modules, lessons, and an assessment.
2. Create a morning cohort starting in one week.
3. Generate a weekly schedule and apply it.
4. Create an evening cohort starting in a different week.
5. Generate a meeting-based schedule and apply it.
6. Verify the schedules and content release dates differ.
7. Enroll different students in each cohort.
8. Verify each student sees only content released for their cohort.
9. Shift one cohort schedule and verify the other remains unchanged.
10. Complete a cohort and verify historical data remains visible and protected.

## Implementation Sequence

1. Correct frontend terminology and remove the cohort-to-single-session mapping.
2. Add schedule domain entities, configurations, migration, CQRS handlers, and tests.
3. Add schedule preview, apply, item editing, and calendar APIs.
4. Regenerate the API client and add frontend query/action adapters.
5. Replace the Classes page with the approved control center and creation side sheet.
6. Build the cohort workspace with the persistent selector and section routes.
7. Build the Syllabus, Calendar, and Timeline schedule views.
8. Add schedule builder preview, conflict handling, and bulk rescheduling.
9. Enforce cohort-specific content availability in student delivery APIs.
10. Add full component, integration, responsive, and browser E2E coverage.

## Acceptance Criteria

- A course can have multiple independently scheduled cohorts.
- Professors can identify and switch cohorts without ambiguity.
- Cohort schedules reference shared course content without duplicating it.
- Each cohort can start on different dates and meet on different days and times.
- Each cohort can release modules, lessons, and assessments on its own schedule.
- Generated schedules can be previewed before persistence.
- Conflicts are visible and blocking conflicts prevent publication.
- Students receive only content available to their enrolled cohort.
- Changing one cohort schedule never changes another cohort.
- The dashboard works without horizontal clipping on supported desktop, tablet, and mobile viewports.
- Unit, integration, frontend, and E2E tests cover the approved professor and student flows.

## Approved Product Decision

The primary UX is the class control center followed by a dedicated cohort workspace. A general multi-cohort calendar is secondary. The default cohort workspace view is Syllabus because it best communicates content sequence, instructional weeks, release timing, meetings, and assessments together.
