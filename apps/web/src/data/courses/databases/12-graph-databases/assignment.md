# Final Project — Checkpoint 3: Proof of Concept

**Due:** Sunday, 2026/04/05

::: warning

Missing this checkpoint incurs a **5% penalty** on your final project grade. See the [Final Project](../09-break/final-project.md) page for full details.

:::

## Overview

This week your team must demonstrate that your **Docker Compose stack is running** and at least one database is operational with seed data. The goal is to show tangible progress — not a polished application, but clear evidence that the infrastructure works and a core feature functions.

## Deliverables

Provide **both** of the following:

1. **Running Docker Compose** — Your `docker-compose.yml` must successfully bring up all planned database containers. At minimum, one database should have seed data loaded and be accessible.

2. **Core Feature Demo** — Record a **2–5 minute video** or provide a **live URL** showing:
   - `docker-compose up` starting successfully
   - At least one database populated with seed data
   - A basic operation that reads from or writes to the database (API call, CLI command, or web UI)

## What "Proof of Concept" Means

- Docker Compose brings up all containers without errors.
- At least one database is initialized with seed data (via init scripts, migrations, or a seed command).
- A service can connect to the database and perform a basic operation.
- You can describe what works, what is in progress, and what still needs to be done.

::: tip

Do not worry about the full application yet. Focus on getting the infrastructure running: **containers up → database seeded → one service connected**.

:::

## Grading Rubric

| Criterion                      | Points  |
| ------------------------------ | :-----: |
| Docker Compose running         |   30    |
| At least one DB with seed data |   30    |
| Core feature demonstrable      |   20    |
| Progress vs. proposal          |   20    |
| **Total**                      | **100** |

## Submission

Submit the link to your repository and video (or deployed URL) via Canvas.
