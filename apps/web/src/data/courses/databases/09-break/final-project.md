# Final Project

The goal of this final project is to build a **multi-database application** that combines **3 or more database types** into a single, cohesive system. You will deliver 3 components:

| Deliverable  | Description                                                              | Grade | Composition |
| ------------ | ------------------------------------------------------------------------ | ----- | ----------- |
| Tech Demo    | A working application with 3+ databases orchestrated via Docker Compose. | 50%   | In group    |
| Presentation | A presentation of your project.                                          | 30%   | In group    |
| Writeup      | A writeup of your project.                                               | 20%   | Individual  |

- A Tech Demo of your multi-database system. (In group)
- A Presentation of your project. (In group)
- A Writeup of your project. (Individual)

## Requirements

- Your application must use **at least 3 different database types** (e.g., PostgreSQL + MongoDB + Redis, or Neo4j + Elasticsearch + Kafka + PostgreSQL).
- The entire system must run via a single `docker-compose up` command. A reviewer should be able to clone your repo and have everything running in one shot.
- You may use AI assistant tools (GitHub Copilot, ChatGPT, etc.) to help fill gaps in service development, but you must understand and be able to explain all code.

::: tip

**Bonus points** will be awarded if you deploy your application to a publicly accessible URL. This is not required, but demonstrates DevOps competency.

:::

## Team Size

- **Preferred team size: 3 members.** Teams of 1–2 are allowed but discouraged (less voting power, lonely work). Teams of up to 5 are allowed for higher-complexity topics.
- The complexity ratings (⭐) in the topic suggestions roughly correspond to recommended team size.

## Topic Suggestions

::: tip

You may select one or more topics from the list below, or propose your own — but talk with me first. The star ratings indicate complexity and roughly match the recommended team size.

:::

### Application Domains

| #   | Topic                                         | Suggested Databases                                                                  | Complexity |
| --- | --------------------------------------------- | ------------------------------------------------------------------------------------ | :--------: |
|     | **E-Commerce & Retail**                       |                                                                                      |            |
| 1   | Product Catalog with Search & Recommendations | PostgreSQL (orders), MongoDB (product docs), Elasticsearch (search), Redis (cache)   |  ⭐⭐⭐⭐  |
| 2   | Real-Time Inventory & Order Tracking          | PostgreSQL (orders), Redis (stock counters), Kafka (order events), TimescaleDB       |  ⭐⭐⭐⭐  |
| 3   | Price Comparison & Deal Aggregator            | PostgreSQL (deals), Elasticsearch (search), Redis (cache), MongoDB (scraped data)    |   ⭐⭐⭐   |
|     | **Social & Communication**                    |                                                                                      |            |
| 4   | Social Network with Friend Recommendations    | PostgreSQL (users), Neo4j (social graph), Redis (sessions), MongoDB (posts)          |  ⭐⭐⭐⭐  |
| 5   | Chat Application with Message Search          | PostgreSQL (users), Redis (presence/pub-sub), MongoDB (messages), Elasticsearch      |   ⭐⭐⭐   |
| 6   | Event Platform with Attendee Matching         | PostgreSQL (events), Neo4j (interest graph), Redis (real-time), Elasticsearch        |  ⭐⭐⭐⭐  |
|     | **IoT & Monitoring**                          |                                                                                      |            |
| 7   | Smart Home Dashboard                          | TimescaleDB (sensor data), Redis (state cache), PostgreSQL (config), MongoDB (logs)  |   ⭐⭐⭐   |
| 8   | Server Monitoring & Alerting System           | TimescaleDB (metrics), Elasticsearch (logs), Redis (alerts), PostgreSQL (config)     |   ⭐⭐⭐   |
| 9   | Fleet Tracking & Route Optimization           | TimescaleDB (GPS data), PostgreSQL (vehicles), Redis (real-time), Neo4j (routes)     |  ⭐⭐⭐⭐  |
|     | **Analytics & Business Intelligence**         |                                                                                      |            |
| 10  | Log Analytics Platform                        | Elasticsearch (log search), Kafka (log ingestion), TimescaleDB (metrics), Redis      | ⭐⭐⭐⭐⭐ |
| 11  | Student Performance Analytics                 | PostgreSQL (grades), TimescaleDB (trends), Redis (dashboards), MongoDB (feedback)    |   ⭐⭐⭐   |
|     | **Gaming**                                    |                                                                                      |            |
| 12  | Multiplayer Game Leaderboard & Matchmaking    | PostgreSQL (accounts), Redis (leaderboards/matchmaking), MongoDB (game state)        |   ⭐⭐⭐   |
| 13  | Game Asset Marketplace                        | PostgreSQL (transactions), MongoDB (asset metadata), Elasticsearch (search), Redis   |  ⭐⭐⭐⭐  |
|     | **Healthcare & Finance**                      |                                                                                      |            |
| 14  | Patient Health Record System                  | PostgreSQL (records), MongoDB (documents), Elasticsearch (search), Redis (sessions)  |  ⭐⭐⭐⭐  |
| 15  | Personal Finance Tracker                      | PostgreSQL (transactions), TimescaleDB (trends), Redis (budgets), MongoDB (receipts) |   ⭐⭐⭐   |
| 16  | Fraud Detection Pipeline                      | PostgreSQL (accounts), Kafka (transactions), Neo4j (relationships), Redis (alerts)   | ⭐⭐⭐⭐⭐ |
|     | **Content & Media**                           |                                                                                      |            |
| 17  | Content Management System with AI Search      | PostgreSQL (users), MongoDB (content), Elasticsearch (search), pgvector (semantic)   |  ⭐⭐⭐⭐  |
| 18  | Music/Podcast Recommendation Engine           | PostgreSQL (users), Neo4j (taste graph), pgvector (embeddings), Redis (listening)    |  ⭐⭐⭐⭐  |
| 19  | Recipe Platform with Ingredient Substitutions | PostgreSQL (recipes), Neo4j (ingredient graph), Elasticsearch (search), Redis        |   ⭐⭐⭐   |
| 20  | Knowledge Base / Wiki with Semantic Search    | PostgreSQL (users), MongoDB (articles), pgvector (semantic search), Redis (cache)    |   ⭐⭐⭐   |

**Complexity key:** ⭐⭐⭐ moderate (3-person team) · ⭐⭐⭐⭐ challenging (4-person team) · ⭐⭐⭐⭐⭐ advanced (5-person team)

::: note

Once you select your topic, fill and check the signup spreadsheet to ensure your topic is not already taken.

:::

## Tech Demo

Your tech demo is a working multi-database application system. The entire stack must be orchestrated by Docker Compose so that a reviewer can run everything with a single command:

```bash
git clone <your-repo>
cd <your-repo>
docker-compose up
```

### Requirements

- All databases and services defined in `docker-compose.yml`
- A `README.md` with setup instructions, architecture overview, and which databases are used and why
- Seed data or initialization scripts so the system is functional immediately after startup
- A web UI, CLI, or API that demonstrates the system's capabilities

::: warning

Your system must work out of the box. If a reviewer has to manually install dependencies, configure environment variables beyond what's documented, or run multiple commands to get things running, points will be deducted.

:::

## Writeup

Your writeup should include the following:

- At least 3 references (books, articles, documentation, or websites)
- A description of your multi-database architecture and why each database was chosen
- How data flows between the different database systems
- Challenges encountered and how they were solved
- Length should be between **600 and 3000 words**
- Your writeup should reference your tech demo with screenshots or diagrams
- Use diagrams to explain your architecture. Suggestions: [draw.io](https://draw.io/), [Mermaid](https://mermaid.live/), [Excalidraw](https://excalidraw.com/)

::: info

If you want to publish your class work publicly (e.g., on Medium, LinkedIn, Reddit, or a personal blog), you need to sign the [FERPA waiver](https://gameguild.gg/ferpa-waiver). Publishing is **encouraged** but not required — it builds your professional portfolio.

:::

## Presentation

- The goal of the presentation is to teach the class about your chosen domain and database architecture
- Your audience is the class, so target the content level appropriately
- Duration: **10 minutes** (strict) + **5 minutes** Q&A
- You may use any presentation tool you like, but I recommend [Google Slides](https://www.google.com/slides/) for simplicity and multi-user editing
- This is a formal presentation — be prepared to answer questions from the audience
- A **live demo** of your Docker Compose system is required during the presentation. Have a backup recording in case of technical issues

## Submission

- The presentation slides (link)
- The link for the writeup (Medium, LinkedIn, Google Doc, etc.)
- The link to the code repository (GitHub)
- The deployed URL (if applicable, for bonus points)

## Late Submission Policy

Late submissions will incur a penalty of **1% deduction per day** up to a maximum of **25% of the total grade**. For example, a submission that is 1 week (7 days) late will receive a 7% penalty.

## Milestones & Checkpoints

| Week | Date       | Milestone                         | Deliverable                                                                                                                                                     | Status       |
| ---- | ---------- | --------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------ |
| 10   | 2026/03/22 | **Proposal**                      | Team composition + project proposal in slideshow format. Must include: chosen topic, team members & roles, scope description, and which databases you will use. | Due Sunday   |
| 11   | 2026/03/29 | **Architecture Design**           | System architecture diagram showing all database components, data flow between them, and service boundaries. Submit as a link or PDF.                           | Due Sunday   |
| 12   | 2026/04/05 | **Proof of Concept**              | Docker Compose running with at least one database operational and seed data loaded. Core service demonstrable.                                                  | Due Sunday   |
| 13   | 2026/04/12 | **Testing Session 1**             | In-class peer testing. Bring your system for classmates to test. Collect feedback and identify issues.                                                          | In class     |
| 14   | 2026/04/19 | **Feature Freeze**                | No new features after this week. Second testing session in class. Focus on bug fixes, polish, and documentation only.                                           | In class     |
| 15   | 2026/04/26 | **Peer Evaluation & Code Freeze** | Teams exchange repos for code review. Code freeze by Wednesday 2026/04/22. Writeup draft due.                                                                   | Due Sunday   |
| 16   | 2026/04/30 | **Final Presentations**           | 10-minute presentation + 5-minute Q&A. Live demo required. Final repository, writeup, and peer evaluation due.                                                  | Due Thursday |

::: warning

Missing any checkpoint will result in a **grade of 0** for that checkpoint. Stay on track — these checkpoints exist to prevent last-minute scrambles.

:::
