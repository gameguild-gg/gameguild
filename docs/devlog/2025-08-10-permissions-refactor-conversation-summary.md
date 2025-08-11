Assessment & Improvements
General Settings

Observed: Basic form (labName, description, timezone, durations, boolean toggles). Local state, single save action, minimal validation.
Issues / Opportunities:
Validation: No client-side length/range checks (name length, description max, duration bounds).
UX: Single Save button—no dirty-state indicator, no autosave or optimistic feedback.
Timezone: Plain select? (If currently a text input, needs a canonical TZ dropdown (IANA list) + search).
Accessibility: Labels present; could add aria-live status on save.
Error Handling: Generic toast only; no per-field inline errors.
Types: GeneralSettingsState okay but could narrow timezone to union of known strings or load dynamically.
Performance: All settings loaded with full page load; consider suspense + skeleton.
Testing: No unit test coverage for mapping API → form or submit payload.
Locations

Observed: CRUD dialog with fallback to mock data if API fails; robust form but some duplication.
Issues / Opportunities:
API Error Path: Mixing “demo mode” logic with prod paths—extract adapter/service for clearer separation.
Validation: Just basic numeric bounds; add address length, equipment parsed list, uniqueness of name.
Optimistic Updates: Currently waits; could apply optimistic add/update with rollback on failure.
Repetition: Duplicate save logic in form submit and dialog footer; consolidate into single handler.
Status Enum: Conversions scattered (string ↔ numeric); centralize in a utility.
List Rendering: Large lists could paginate / virtualize later.
Permissions Awareness: UI shows controls even if user lacks create/edit rights (should gate via capability flags).
No filtering/search for locations; add quick filter bar.
Testing: No integration tests for create/update fallback logic.
Roles & Permissions

Observed: Aggregated boolean model unified; create/edit dialog with grouped switches.
Issues / Opportunities:
Switch Count: Many toggles—introduce preset templates (Admin / Manager / Coordinator / Viewer) with “Apply preset” buttons.
Derived Display: permissionTemplates computed server-side; consider computing client-side to reduce payload size if backend cost matters.
Granularity: All permissions are global; no scoped (per location) permission toggles yet.
No dependency logic (e.g., cannot edit/delete without read). Add auto-enforce cascading selection.
Missing audit/meta: Show created/updated timestamps, last modified by.
Duplicate mapping logic (mapAggregatedPermissionsToForm + backend converters) — extract shared schema definition to avoid drift.
No “test role” / impersonation preview.
Validation: Role name uniqueness only enforced by backend; add instant check.
Accessibility: Switch groups need fieldset/legend semantics.
Cross-Cutting Improvements (Prioritized) High

Extract service layer for Locations & Roles (pure functions) to isolate API/dto transforms.
Add field-level validation + schema (zod or valibot) for General + Location + Role forms.
Provide permission-based gating (disable/hide create/edit/delete if user lacks rights).
Add presets + cascading logic in permissions dialog.
Medium

Dirty-state + unsaved changes prompt; optimistic UI for saves.
Introduce loading skeletons & suspense boundaries per section.
Centralize enum/status converters (locations, role ids) in /lib/shared.
Introduce test suite: mapping tests, permission conversion, optimistic rollback.
Low

Pagination / search for locations and collaborators.
Inline editing for descriptions.
Accessibility audit (aria-live, focus management on dialog open/close).
Refactor Targets

Split remaining inline sections (LocationsSettings, RolesSettings) into their own files (already started with General/Collaborators).
Create permissions schema object (single source) that drives:
Form generation (map of categories → permissions)
DTO conversion
Display grouping
Replace magic strings with typed constants or enums.
Data & Performance

Batch initial loads with Promise.all and show aggregated loading state.
Cache read-only lists (locations, roles) via SWR or React cache; invalidate after mutations.
Consider partial hydration (render basic list then enrich with counts).
Telemetry & Observability

Log role changes (audit trail).
Capture location utilization (sessions per location) for later gamification metrics.
Gamification Suggestions (Targeted to Testing Lab)
Core Motivators

Mastery / Progress
Contribution Recognition
Collaboration & Healthy Competition
Transparency of Impact
Mechanics (Prioritized) High-Value

Experience Points (XP): Award testers/managers XP for actions (sessions created, feedback submitted, high-quality feedback rated helpful, requests approved).
Role-based Badges: Dynamic badges that visually augment roles (e.g., “Session Architect” for 50 sessions created).
Streaks: Consecutive days with at least one meaningful contribution (feedback, session participation). Show streak flame icon and multiplier (boost XP).
Quality Score: Weighted score for feedback based on peer upvotes / acceptance / bug reproduction success.
Medium

Location Leaderboards: “Most Active Location” (sessions hosted, unique testers). Encourage balanced usage by rotating seasonal challenges.
Seasonal Challenges / Quests:
Weekly: “Host 3 VR sessions” or “Submit 5 actionable feedback items”.
Monthly: “Improve request approval turnaround to < X hours”.
Contribution Heatmap: Personal calendar of activity; unlocked colors for higher intensity days.
Low / Future

Crafting System: Combine earned tokens (from achievements) to unlock advanced testing tools or early access builds.
Reputation Tiers: Bronze/Silver/Gold for sustained quality score + activity; tiers give soft perks (early feature toggles).
Collaborative Goals: Team progress bar (e.g., “Reach 100 validated feedback items this sprint”) with shared reward (global badge or theme unlock).
Mentor Tokens: Senior contributors can gift limited tokens to novices for exemplary first contributions, accelerating onboarding.
Feedback & Reinforcement

Real-time Toast Enhancements: “+15 XP (Helpful Feedback) • Current Streak: 4 days”.
Profile Progress Ring: Summarize current level progression.
Unlock Animations: Subtle confetti/flare when hitting major milestones (first 10 sessions, first 100 feedback items).
Post-Action Micro Survey (occasionally): “Was this session setup smooth?” feeding improvement loop.
Data Needed to Enable

Event log (action type, actor, timestamp, metadata).
Quality signals (feedback voted helpful, accepted, leads to fix).
Session metrics (duration, participants, follow-up feedback).
Approval latency metrics for requests.
Anti-Gamification Safeguards

Rate limiting XP for repetitive low-value actions (spam feedback).
Weight quality higher than quantity (diminishing returns).
Manual moderation override for fraudulent patterns.
Incremental Rollout Plan

Instrument events + basic XP accumulation (silent phase).
Expose profile XP & simple badges (Founding Tester).
Add streaks + leaderboards.
Introduce quests/challenges.
Add advanced tiers & collaborative goals.

# Testing Lab Roadmap

## 1. Context & Goals
Improve maintainability, UX, permission integrity, and prepare a foundation for gamification that increases contributor engagement and quality.

## 2. Pillars
- Reliability & Consistency
- UX & Accessibility
- Extensibility (schema-driven permissions, modular sections)
- Observability & Metrics
- Engagement (Gamification Layer)

## 3. Phased Delivery

### Phase 1 (Stabilize & Modularize)
- Extract remaining inline sections (Locations, Roles) into dedicated components.
- Centralize permission schema (single source for: form generation + DTO conversion + display).
- Add zod (or valibot) schemas for General, Location, Role forms.
- Field-level validation + inline error states.
- Refactor API/data access into service layer (separate “demo fallback”).
- Permission-based UI gating (disable/hide actions when user lacks rights).

### Phase 2 (UX & Performance Enhancements)
- Dirty-state detection + “unsaved changes” confirm.
- Optimistic updates with rollback (locations, roles).
- Loading skeletons + suspense boundaries per section.
- Search/filter for locations & collaborators.
- Role presets (Admin / Manager / Coordinator / ReadOnly) + cascading permission logic.
- Inline unique-name precheck for roles & locations.
- Accessibility pass (focus traps, fieldset/legend, aria-live for saves).

### Phase 3 (Observability & Security)
- Event logging (role changes, location CRUD, settings updates).
- Audit trail display (who changed what, when).
- Cache + revalidation strategy (SWR or server actions caching).
- Rate limiting / backend validation reinforcement.
- Add integration + unit tests (permission mapping, DTO conversion, optimistic flows).

### Phase 4 (Gamification Foundation)
- Instrument XP events silently (no UI yet).
- Data model: events table (actor, type, weight, metadata).
- Expose profile XP + simple badges (Founding Tester, First Feedback).
- Streak calculation service (daily contribution detection).

### Phase 5 (Engagement Expansion)
- Leaderboards (weekly / monthly: feedback quality, sessions hosted).
- Role-based dynamic badges.
- Quality score (feedback helpfulness, accepted requests).
- Streak UI (flame + multiplier).
- Quests / challenges (weekly + monthly config).
- Contribution heatmap on profile.

### Phase 6 (Advanced Gamification & Economy)
- Collaborative goals (team progress bars).
- Reputation tiers (Bronze/Silver/Gold).
- Mentor tokens (senior validation boost).
- Seasonal rotations & limited badges.
- Optional redeemables (themes, early access flags).

## 4. Backlog (Priority Grouped)

### High
- Permission schema unification
- Form validation (zod)
- Role presets + cascade logic
- API service layer extraction
- Permission-based gating
- Tests for permission mapping
- Dirty-state + confirm-on-close

### Medium
- Optimistic updates + rollback
- Loading skeletons / suspense
- Search & filters (locations, collaborators)
- Unique name debounce validator
- Audit trail storage
- Streak backend logic

### Low
- Pagination / virtualization (large datasets)
- Impersonation / preview role mode
- Location utilization metrics
- Themeable badge components
- Contribution heatmap

## 5. Technical Refactors

| Area | Action |
|------|--------|
| Permissions | Define schema: categories → keys → labels → backend mapping. Autogenerate switches & DTO conversion. |
| Forms | Add schemas + helper (parse + safeTransform). |
| Services | `/services/testing-lab/{locations,roles,settings}.ts` isolates client & fallback. |
| State | Replace scattered `useState` with segmented hooks or context modules. |
| Enums | Centralize `LocationStatus`, permission keys, role presets. |
| Testing | Add Jest/TS tests: mapping, optimistic flows, validation edges. |

## 6. Data & Telemetry Requirements
| Signal | Purpose |
|--------|---------|
| session_created | XP, activity tracking |
| feedback_submitted (+ quality rating) | Quality score & XP weighting |
| role_assigned / role_revoked | Audit & leaderboard |
| request_created/approved | Quest progress |
| streak_tick | Streak engine state |
| location_created | Admin activity metrics |

## 7. Gamification Mechanics (Mapping)
| Mechanic | Data Needed | Notes |
|----------|-------------|-------|
| XP | Weighted event log | Diminishing returns for spam |
| Streak | Daily activity index | Grace period config |
| Badges | Threshold achievements | Cache computed set |
| Leaderboards | Aggregated weekly counts | Separate quality & volume |
| Quests | Config + event aggregation | Stored in DB, versioned |
| Quality Score | Feedback helpfulness, acceptance | Weighted formula |

## 8. Safeguards
- Diminishing XP after N similar actions/day.
- Manual moderation flag to nullify abusive XP.
- Weight quality > quantity.
- Spam detection (rapid duplicate content).

## 9. Success Metrics
| Metric | Target After Rollout |
|--------|----------------------|
| Form Error Rate | -40% vs baseline |
| Time to Configure Role | -30% |
| Avg Feedback Quality Score | +20% |
| Weekly Active Contributors | +25% |
| Session Scheduling Latency | -15% |
| Retention (30-day) | +10% |
| Gamification Opt-out Rate | <5% |

## 10. Risk & Mitigation
| Risk | Mitigation |
|------|------------|
| Permission drift (schema vs backend) | Single schema + tests |
| Over-gamification fatigue | Gradual rollout + opt-out toggle |
| Performance regressions | Profiling + Lighthouse / React Profiler baseline |
| Abusive XP farming | Rate limits + anomaly detection |

## 11. Quick Wins (Start Immediately)
- Extract remaining sections (Locations, Roles).
- Build permission schema + generator.
- Add zod validation for all forms.
- Add role presets & cascade logic.
- Introduce optimistic location create/update.
- Implement XP event emitter (no UI).
- Add audit log write on role CRUD.

## 12. Sample Permission Schema Sketch
```ts
const permissionSchema = {
  sessions: {
    label: 'Testing Sessions',
    permissions: [
      { key: 'createSession', backend: 'canCreateSessions', label: 'Create', dependsOn: ['readSession'] },
      { key: 'editSession', backend: 'canEditSessions', label: 'Edit', dependsOn: ['readSession'] },
      { key: 'deleteSession', backend: 'canDeleteSessions', label: 'Delete', dependsOn: ['readSession'] },
      { key: 'readSession', backend: 'canViewSessions', label: 'View' },
    ],
  },
  // ...locations, feedback, requests, participants
} as const;
```

## 13. Roadmap Task Marking: General Settings & Locations

Explicit mapping of the Observed / Issues items for General Settings and Locations into the phased roadmap with status checkboxes.

Legend: [ ] Not Started · [~] In Progress · [x] Complete

### 13.1 General Settings Tasks
| Task | Phase | Status | Notes |
|------|-------|--------|-------|
| Client-side validation (labName length, description max, duration bounds) | 1 | [ ] | Via zod schema (ties to Phase 1 validation) |
| Inline per-field error messaging | 1 | [ ] | Replace generic toast-only approach |
| Timezone: searchable IANA dropdown | 2 | [ ] | Use generated list (e.g. @vvo/tzdb) |
| Dirty-state indicator | 2 | [ ] | Complements confirm-on-close already listed |
| Autosave / optimistic save (debounced) | 2 | [ ] | After stable validation foundation |
| aria-live save status region | 1 | [ ] | Accessibility enhancement |
| Narrow timezone type to loaded IDs | 1 | [ ] | Improves type safety |
| Suspense + skeleton for initial load | 2 | [ ] | Per-section boundary |
| Unit tests: API→form mapping & submit payload | 3 | [ ] | Added to Phase 3 test expansion |

### 13.2 Locations Tasks
| Task | Phase | Status | Notes |
|------|-------|--------|-------|
| Extract service layer (API vs demo fallback) | 1 | [ ] | Clarifies separation of concerns |
| Consolidate duplicate save logic | 1 | [ ] | Single submission pathway |
| Validation: name uniqueness (debounced) | 2 | [ ] | Shared with roles unique validator infra |
| Validation: address length & bounds | 1 | [ ] | Part of zod schema extension |
| Validation: equipment list parsing & uniqueness | 1 | [ ] | Normalize array + trim/lowercase compare |
| Centralize status enum converters | 1 | [ ] | Shared enum/util module |
| Permission-based UI gating | 1 | [ ] | Consumes unified permission schema |
| Optimistic create/update + rollback | 2 | [ ] | Mutation helper abstraction |
| Search / quick filter bar | 2 | [ ] | Client-side; server later if needed |
| Pagination / virtualization readiness | 4 | [ ] | Low priority scaling item |
| Tests: optimistic flows & fallback adapter | 3 | [ ] | Ensure rollback correctness |

### 13.3 Phase Integration Summary
| Phase | Newly Catalogued Items |
|-------|-----------------------|
| 1 | General validation & inline errors, aria-live, timezone type narrowing, locations service extraction, save consolidation, address/equipment validation, status enum centralization, permission gating |
| 2 | Dirty-state indicator, autosave, timezone dropdown, optimistic location CRUD, search/filter, name uniqueness validator |
| 3 | Mapping & optimistic/fallback tests |
| 4 | Pagination / virtualization readiness |

### 13.4 Immediate Board Additions (Phase 1)
- General: zod validation + inline errors
- General: timezone type narrowing
- Locations: service layer extraction
- Locations: consolidate save logic
- Locations: extended validation (address, equipment)
- Locations: status enum centralization
- Locations: permission gating

These tasks are now explicitly tracked without altering previously stated priorities.