# Final Project — Checkpoint 6: Peer Evaluation & Code Freeze

**Code Freeze:** Wednesday, 2026/04/22
**Due:** Sunday, 2026/04/26

::: danger

**Code Freeze** — Your project code must be pushed to your repository no later than **Wednesday 2026/04/22**. After this date, only documentation changes are allowed.

:::

::: warning

Missing this checkpoint incurs a **5% penalty** on your final project grade. See the [Final Project](../09-break/final-project.md) page for full details.

:::

## Overview

This week teams **exchange projects** for structured code review and final testing. You will also submit your **writeup draft**.

## Peer Evaluation

### As a Reviewer

- You will be assigned another team's project.
- **Infrastructure Review** — Clone their repo and run `docker-compose up`. Does it work out of the box? Are all 3+ databases present and functional?
- **Code Review** — Read through their service implementations. Look for: code organization, database usage patterns, documentation quality, and potential issues.
- **System Test** — Test the application's core features. Does data flow between databases as described in their architecture?
- Fill out the **peer evaluation rubric** (provided in class) with constructive feedback.

### As a Team Being Reviewed

- Ensure your code repository is accessible to reviewers.
- Include a `README.md` explaining:
  - How to run the project (`docker-compose up`)
  - Architecture overview with diagram
  - Which databases are used and why
  - How to interact with the system (API endpoints, web UI, CLI)

## Deliverables

1. **Peer evaluation feedback** — Completed rubric for the team you reviewed (due Sunday).
2. **Writeup draft** — A draft of your individual writeup (600–3000 words). See the [Final Project writeup requirements](../09-break/final-project.md#writeup). This draft does not need to be final, but should be substantially complete.
3. **Code freeze** — Final code pushed to repository by Wednesday 2026/04/22.

## Grading Rubric

| Criterion              | Points  |
| ---------------------- | :-----: |
| Peer review quality    |   40    |
| Writeup draft          |   40    |
| Code freeze compliance |   20    |
| **Total**              | **100** |

## Submission

- Peer evaluation: submit via Canvas by Sunday.
- Writeup draft: submit as a link (Google Doc, Medium draft, etc.) by Sunday.
- Code: pushed to your repository by Wednesday.
