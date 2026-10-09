# Quiz Grading Scenario Runner

This local-only runner prepares reproducible quiz grading states through the
same authenticated HTTP contracts used by the product. It never writes to the
database directly and does not add development endpoints to the API.

## Prerequisites

In one terminal, start the stack and keep it running:

```bash
pnpm dev:fast
```

The default addresses are `http://localhost:3000` and
`http://localhost:8080`. The runner rejects remote hosts and automatically uses
the generated client from `dist-fast` when this development mode is active.

In a second terminal, prepare the desired state:

```bash
pnpm grading:scenario prepare \
  --scenario automated-instructor \
  --checkpoint review-ready
```

The result includes disposable credentials and links for the instructor,
learner, SpeedGrader, assessment editor, and learner gradebook. Prepared data
remains available after the process exits.

`prepare` is a one-shot provisioning command; it does not open a browser. Wait
until it prints `Scenario is ready`, then open one of the returned URLs and log
in with the matching persona. Use a private window or a separate browser
profile when switching between instructor and learner sessions.

If provisioning is interrupted, running the same command resumes from the last
persisted checkpoint. A lock owned by a process that no longer exists is
recovered automatically; a live concurrent runner remains protected.

Available scenarios:

- `automated`
- `instructor`
- `automated-instructor`
- `collective-automated-instructor`

Available checkpoints:

- `authoring-ready`
- `test-run-ready`
- `learner-ready`
- `review-ready` (workflows with `InstructorReview` only)
- `release-ready`
- `released`

## Inspect and verify

```bash
pnpm grading:scenario list
pnpm grading:scenario status --scenario automated-instructor
pnpm grading:scenario verify --scenario automated-instructor
```

`status` reports divergence without repairing it. `verify` fails when the
manifest claims a state that the API cannot confirm.

## Browser journey

```bash
pnpm grading:scenario test --scenario automated-instructor --fresh --headed
```

Playwright consumes the same manifest. It uses isolated browser contexts for
the instructor, learner, and outsider and stores local evidence under
`apps/web/test-results/quiz-grading-scenarios/`.

## Reset

```bash
pnpm grading:scenario reset --scenario automated-instructor
```

Reset is explicit. It validates the local host and the ownership marker before
calling public delete/unpublish operations. It does not clear the database,
Docker volumes, unrelated courses, or human accounts. If any delete fails, the
manifest remains so cleanup can be retried safely.
