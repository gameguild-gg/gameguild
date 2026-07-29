# Testing Lab Event Workflow Design

## Purpose

Testing Lab lets community members submit existing GameGuild projects for structured online or in-person testing events. Managers publish events and schedules before project owners apply. A project consumes event capacity only after approval and slot assignment.

## Domain Model

### TestingEvent

The manager-owned aggregate defines the event name, description, mode, submission window, event period, status, tester feedback policy, optional capacity for online events, approval mode, and optional Learning integration.

### TestingEventSlot

A slot defines when testing happens and where. In-person slots reference a campus and room and enforce tester and project limits. Online slots may use a meeting URL and may have unlimited capacity. Slot capacity is consumed only by approved project applications and confirmed tester registrations.

### TestingProjectApplication

An application connects an existing project and optional project version to an event. Applicants may provide preferred availability, but they do not choose or reserve a slot. States are Pending, UnderReview, Approved, Rejected, Waitlisted, and Withdrawn. Approval assigns a slot atomically; rejection requires feedback.

### TestingReviewCommittee

An event can use ManagerOnly or Committee approval. Committee members submit immutable votes with comments. Committee decisions use a simple majority, with the event manager resolving ties. Every decision records actor, timestamp, outcome, and rationale.

### TesterRegistration

Testers register for an event slot. In-person registration enforces the room capacity and moves overflow to a waitlist. Online slots support optional or unlimited capacity. Registration tracks confirmation, check-in, attendance, completion, and no-show state.

### FeedbackRequirement

Events can require every attended tester to submit feedback for every project they tested. Participation remains incomplete while required feedback is missing. Managers can see outstanding obligations and project-level response coverage.

### Learning Integration

An event can reference a course, cohort, and learning activity. Completion policies can require attendance, submitted feedback, an accepted project presentation, or a configured combination. Testing Lab publishes completion evidence; Learning remains responsible for calculating grades.

## Workflows

### Manager

Create event, configure locations and slots, open applications, appoint a committee when needed, review applications, approve or reject with rationale, assign approved projects to slots, open tester registration, record attendance, monitor feedback obligations, and close the event with operational reports.

### Project Owner

Choose an existing project, apply to an event, provide availability, follow review status, receive the decision and assigned slot, present the project, and review collected feedback.

### Tester

Choose an event slot, register or join its waitlist, attend, test assigned projects, submit required feedback, and receive participation completion.

## API And Application Architecture

The backend uses the existing modular CQRS conventions. Controllers dispatch commands and queries through the GameGuild mediator. The web dashboard uses Server Components and server actions through `@game-guild/client` generated Testing Lab modules. Local client state is limited to dialogs, temporary filters, and selection.

The existing `TestingSession` remains the operational session record and gains an event-slot relationship. Existing project-backed requests are migrated into applications without inventing projects or duplicating project ownership.

## Invariants

- An application never consumes project capacity before approval.
- An approved application must have exactly one active slot assignment.
- Slot project and tester capacities are enforced atomically.
- Rejection requires a non-empty rationale.
- Only the manager can issue the final ManagerOnly decision.
- Committee members cannot vote twice on the same application.
- In-person slots require campus and room data.
- Required feedback blocks participation completion.
- All reads and writes are tenant-scoped and audited.

## Verification

Backend unit and PostgreSQL integration tests cover invariants, concurrent approvals, slot capacity, committee voting, waitlist promotion, feedback obligations, Learning evidence, assembly discovery, schema repair, and project lifecycle locking. Frontend tests cover server actions, query mapping, error/retry/success states, filters, pagination, and bulk operations. Playwright uses real API data and distinct accounts for the complete manager, project-owner, tester, and committee journeys across desktop and mobile viewports.

The completed verification baseline is:

- 157 Testing Lab unit tests passed with no skips.
- 6 Learning evidence tests passed with no skips.
- 55 focused frontend tests passed across 17 files.
- 6 API E2E tests passed across request/session and event workflows.
- The complete Testing Lab browser E2E passed without route failures, browser exceptions, or viewport overflow.
- Real Testing Lab Coverlet coverage is 65.94% line, 41.26% branch, and 45.93% method. This remains an explicit legacy unit-coverage debt and is not represented as 100%.
