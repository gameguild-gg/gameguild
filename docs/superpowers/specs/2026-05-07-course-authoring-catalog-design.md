# Course Authoring and Catalog Design

Date: 2026-05-07
Status: Approved for implementation handoff
Scope: GameGuild web dashboard course editor, public course catalog, future academy runtime contract

## Summary

GameGuild will use one canonical course record that feeds three surfaces:

1. The authenticated dashboard authoring workspace.
2. The public catalog and storefront experience at `/courses`.
3. A future academy microfrontend that delivers the enrolled learning experience.

The dashboard remains the source of truth. The catalog is a public merchandising layer over the same course. The academy is a delivery layer over the same course. This avoids split records, duplicate publishing flows, and long-term drift between authoring and storefront data.

## Product Decisions Already Approved

### Source of truth

- One course record exists per course.
- The dashboard edits that record.
- The catalog reads the public and commercial slice of that record.
- The academy reads the delivery slice of that record.

### Surface responsibilities

- Dashboard: create, structure, launch, operate, and retire courses.
- Catalog: present courses as a store page, including teaser and enrollment states.
- Academy: consume the course once learners are enrolled and the course is live.

### Release model

The release model must support all of these cases:

- teaser-first
- enrollment-open-later
- academy-start-later
- same-day launch for teaser, enrollment, and academy start

The system therefore needs separate dates instead of one publish toggle.

### Dashboard shape

The course workspace should evolve from a simple editor into a structured authoring system with a clear sequence and a danger zone at the end.

## What Is Already Defined In Code

### Public catalog surface

- `apps/web/src/app/[locale]/(contents)/(learning)/courses/page.tsx`
  - Public `/courses` page already exists.
  - It already calls `getCourses()` and renders a marketing-forward catalog page.

- `apps/web/src/lib/courses/services/course.service.ts`
  - Public course fetch layer already exists.
  - `getCourses()` returns a mapped public list.
  - `getCourseBySlug()` returns a public detail model.
  - Failure already degrades safely to an empty catalog list.

- `packages/features/courses/src/components/course-catalog.tsx`
  - Reusable headless course catalog shell already exists.
  - It can be used as a shared base for public listing UIs.

### Dashboard authoring surface

- `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/new/page.tsx`
  - A working create-course flow already exists.
  - It currently captures basics, details, and settings.
  - It already calls `createCourse()` and `updateCourse()`.

- `apps/web/src/app/[locale]/(dashboard)/dashboard/(learning)/learning/courses/[course]/`
  - Route structure already exists for the expanded workspace:
    - `page.tsx`
    - `overview/`
    - `settings/`
    - `listing/`
    - `content/`
    - `classes/`
    - `assessments/`
    - `certificates/`
    - `students/`
    - `analytics/`
    - `support/`

### Authenticated course data layer

- `apps/web/src/lib/learning/actions.ts`
  - Already defines server actions for:
    - `createCourse`
    - `updateCourse`
    - `publishCourse`
    - `unpublishCourse`
    - `addContent`
    - `updateContent`
    - `deleteContent`
    - `reorderContent`

- `apps/web/src/lib/learning/queries/course.ts`
  - Already defines authenticated read queries for:
    - `getCourse`
    - `getCourseAnalytics`
    - `getCourseContent`
    - `getContentItem`
    - `getCourseStudents`

- `apps/web/src/lib/learning/types.ts`
  - Already contains richer frontend course types than the current UI fully uses.
  - It already suggests future support for delivery mode, pricing model, features, analytics, and operations.

## Problem Statement

Today the project has the pieces of a catalog and the pieces of a dashboard course editor, but the product model is still too flat.

Current gaps:

- the dashboard is route-rich but not yet organized around a clear authoring lifecycle
- launch timing is not modeled strongly enough for teaser, enrollment, and academy start to vary independently
- the catalog and the future academy need a cleaner contract with the shared course record
- the existing publish actions are too coarse for the planned release model

## Goals

- Keep one canonical course record.
- Make the dashboard the definitive editing surface.
- Let public catalog visibility be driven by release configuration rather than ad hoc conditions.
- Prepare the course model for a separate academy microfrontend without re-modeling later.
- Preserve and reuse the current routes and data layer where possible.

## Non-goals

- Building the academy microfrontend now.
- Rewriting the public catalog from scratch.
- Replacing the existing server actions wholesale unless the current contract blocks the new release model.
- Introducing a second authoring model or separate “store product” record.

## Information Architecture

The dashboard course workspace should be presented as a guided but persistent authoring system.

### Workspace sections

1. Basics
2. Structure
3. Launch
4. Operate
5. Advanced
6. Danger Zone

### Page ownership

#### Overview

Purpose:

- control tower for the course
- status summary
- launch readiness
- operational alerts

Should show:

- title, status, visibility summary
- storefront state
- academy state
- readiness checklist
- enrollment counts and headline metrics
- missing configuration warnings

#### Settings

Purpose:

- canonical course identity and shared metadata

Owns:

- title
- subtitle or short summary
- slug
- description
- category
- difficulty
- estimated hours
- thumbnail
- showcase video
- base visibility intent
- course-level feature flags that affect all surfaces

#### Listing

Purpose:

- public storefront behavior

Owns:

- public teaser copy
- catalog card presentation fields
- long-form selling copy
- seo-facing metadata if applicable
- call-to-action mode
- teaser visibility
- enrollment visibility
- launch schedule

#### Content

Purpose:

- curriculum structure container

Owns:

- high-level content tree
- modules, sections, ordering
- top-level learning assets

#### Classes

Purpose:

- delivery units inside the curriculum

Owns:

- lessons
- live or scheduled classes if supported
- sequencing and gating metadata

#### Assessments

Purpose:

- learner evaluation layer

Owns:

- quizzes
- submissions
- grading configuration
- completion requirements

#### Certificates

Purpose:

- post-completion credential rules

Owns:

- certificate eligibility rules
- certificate metadata and templates

#### Students

Purpose:

- enrollment roster and learner management

#### Analytics

Purpose:

- performance and engagement metrics

#### Support

Purpose:

- instructor or team operations around the course

#### Danger Zone

Purpose:

- destructive or high-risk actions

Owns:

- archive
- unpublish hard actions
- duplicate if needed
- delete if allowed

## Shared Course Model

The course model should be treated as three layers living on one record.

### 1. Shared core layer

Used by all surfaces.

Fields:

- id
- slug
- title
- subtitle
- description
- category
- difficulty
- estimatedHours
- thumbnail
- showcaseVideoUrl
- status
- version
- creator metadata
- timestamps

### 2. Storefront layer

Used by the public catalog.

Fields:

- listing headline
- teaser summary
- public selling copy
- enrollment CTA label or mode
- pricing or offer configuration when added
- teaserAt
- enrollmentOpensAt
- enrollmentClosesAt
- public seo or share metadata where applicable

### 3. Academy layer

Used by the academy runtime.

Fields:

- academyStartsAt
- academyEndsAt if needed
- curriculum structure
- class items
- assessments
- completion requirements
- certificate rules
- learner operations metadata

## Derived States

These states should not be edited directly when they can be computed from dates and readiness.

### Storefront state

- `hidden`
- `teaser`
- `enrollment-open`
- `enrollment-closed`

### Academy state

- `hidden`
- `scheduled`
- `live`
- `ended`

### Overall readiness state

- `incomplete`
- `storefront-ready`
- `academy-ready`
- `live`

## Release Rules

The release system is date-driven.

### Launch fields

- `teaserAt` optional
- `enrollmentOpensAt` optional but required for enrollable catalog state
- `academyStartsAt` optional but required for academy delivery
- `enrollmentClosesAt` optional
- `academyEndsAt` optional

### Valid scenarios

#### Scenario A: teaser first

- `teaserAt` set
- `enrollmentOpensAt` later
- `academyStartsAt` later

#### Scenario B: teaser and enrollment same day

- `teaserAt == enrollmentOpensAt`
- `academyStartsAt` later

#### Scenario C: same-day full launch

- `teaserAt == enrollmentOpensAt == academyStartsAt`

#### Scenario D: no teaser, direct enrollment opening

- `teaserAt` omitted
- `enrollmentOpensAt` set
- `academyStartsAt` same day or later

## Validation Rules

- `slug` must remain unique.
- `title` is required.
- `settings` must contain the minimum shared identity before any public launch state is allowed.
- `teaserAt`, if present, must not be after `enrollmentOpensAt` when both exist.
- `academyStartsAt` must not be earlier than `enrollmentOpensAt` when both exist.
- `enrollmentClosesAt` must not be earlier than `enrollmentOpensAt`.
- `academyEndsAt` must not be earlier than `academyStartsAt`.
- storefront-ready requires minimum listing content.
- academy-ready requires minimum curriculum structure and delivery configuration.

## UX Rules

### Overview page

Must answer these questions immediately:

- is this course still a draft?
- is it visible publicly?
- can learners enroll?
- has the academy started yet?
- what is blocking launch?

### Settings page

Must stay focused on stable, canonical data instead of mixing launch and content concerns.

### Listing page

Must behave as the launch control surface for public visibility and enrollment timing.

### Content and classes pages

Must evolve toward a curriculum builder instead of a flat content CRUD form.

### Danger Zone

Must be visually isolated and last in the navigation hierarchy.

## Contract Between Surfaces

### Dashboard to catalog

The catalog should consume only the fields necessary for public discovery and merchandising.

### Dashboard to academy

The academy should consume only published and delivery-ready data.

### Catalog to academy

The catalog may link into the academy, but it must not become the delivery source of truth.

## Recommended Implementation Order

### Phase 1: stabilize the shared course contract

- align `settings`, `listing`, and overview with the approved model
- add explicit launch fields and derived launch states
- stop treating publish as the only meaningful public state

### Phase 2: make overview and listing authoritative

- add launch-readiness summary to overview
- move public visibility and release scheduling into listing
- keep existing route structure, but make ownership clear

### Phase 3: align content and academy preparation

- make content and classes reflect the academy layer model
- formalize completion and certificate dependencies

### Phase 4: operational layer

- connect students, analytics, and support to the new readiness and launch states

## First Implementation Slice

The first slice should focus on the parts that unblock the whole model.

Recommended first slice:

1. Strengthen the course detail shape used by dashboard pages.
2. Add launch configuration fields and derived states.
3. Upgrade `overview` to expose readiness and state.
4. Upgrade `listing` to own teaser, enrollment, and academy schedule.

This gives the project a real source-of-truth launch model before deeper curriculum work begins.

## Risks

- Current backend DTOs may not expose enough fields for the new release model.
- Existing `publishCourse` and `unpublishCourse` actions may be too coarse and may need to become wrappers around more explicit state changes.
- The public catalog currently assumes a simpler public model than the planned one.

## Open Technical Questions

- Which launch fields already exist in backend DTOs versus needing API expansion?
- Should “academy not yet started” still allow enrolled learners to see a waiting room state?
- How much pricing and commerce belongs in this course record versus a future commercial abstraction?

## Immediate Next Step

Create an implementation plan for the first slice:

- course shared contract alignment
- overview readiness panel
- listing launch controls
- derived storefront and academy states