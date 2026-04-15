# Final Project — Checkpoint 2: Architecture Design

**Due:** Sunday, 2026/03/29

::: warning

Missing this checkpoint will result in a **grade of 0** for this checkpoint. See the [Final Project](../09-break/final-project.md) page for full details.

:::

## Overview

This week you will design and document the architecture of your multi-database system. The deliverable is a **system diagram** showing every major component, data flow between databases, and how services communicate.

## Deliverables

Submit a document or diagram that includes:

1. **System Architecture Diagram** — A visual representation of your entire stack. Show all databases, services, and how they connect. Must clearly indicate which database type each component uses. Recommended tools:
   - [Mermaid](https://mermaid.live/)
   - [draw.io](https://draw.io/)
   - [Excalidraw](https://excalidraw.com/)

2. **Component Breakdown** — For each service and database, write 2–3 sentences describing its purpose, what data it stores/processes, and why that specific database type was chosen.

3. **Data Flow** — Show how data flows between your databases and services. Include: API endpoints, message queues (if applicable), synchronization strategies, and any data transformation steps.

4. **Docker Compose Plan** — A draft `docker-compose.yml` or a list of all services, images, ports, and volumes you plan to use.

5. **Feasibility Notes** — Identify the riskiest part of your implementation (e.g., data sync between PostgreSQL and Elasticsearch) and describe how you plan to address it. What will you prototype first?

::: tip

If you are unsure about your architecture, look at the database decision matrix from Week 01 and consider the CAP theorem trade-offs discussed in Week 11.

:::

## Grading Rubric

| Criterion            | Points  |
| -------------------- | :-----: |
| System diagram       |   30    |
| Component breakdown  |   25    |
| Data flow            |   25    |
| Feasibility analysis |   20    |
| **Total**            | **100** |

## Submission

Submit as a link (shared Mermaid/draw.io/Google Doc) or PDF via Canvas.
