# Databases

This course will introduce students to database design, Structured Query Language (SQL), normalization, and relational database theory. Traditional relational databases will be contrasted with NoSQL paradigm databases including document-oriented, key-value store, and graph. Students will gain hands-on experience writing database applications.. [Course Catalog](https://classlist.champlain.edu/show/course/number/CSI_300)

## Requirements

- [Data Structures and Algorithms](https://classlist.champlain.edu/show/course/number/CSI_281) or [Data Structures and Algorithms with Python](https://classlist.champlain.edu/show/course/number/CSI_270)

### Textbook

- No textbook is required for this course. All materials will be provided by the instructor.

## Learning Outcomes and Competencies

Using the [Bloom's Taxonomy](https://cft.vanderbilt.edu/guides-sub-pages/blooms-taxonomy/) and the [Champlain College Competency Framework](https://competencies.champlain.edu/), the following learning outcomes have been developed for this course:

### Objective Outcomes

By the end of this course, students will be able to:

1. **Identify** database paradigms and **justify** technology selection for specific use cases. _(Analysis)_
2. **Design** normalized schemas using functional dependencies and normal forms. _(Analysis)_
3. **Construct** SQL queries using joins, subqueries, CTEs, views, and aggregations. _(Technology Literacy)_
4. **Implement** database solutions using Docker and Drizzle ORM. _(Technology Literacy)_
5. **Analyze** database systems by examining components and their relationships. _(Technology Literacy)_
6. **Compare** raw SQL, query builders, and ORMs, defending appropriate choices for different contexts. _(Analysis)_
7. **Develop** queries for non-relational databases (MongoDB, Redis, Neo4j, Elasticsearch). _(Integration)_
8. **Collaborate** in teams to deliver a multi-database application addressing a real-world problem. _(Collaboration)_
9. **Formulate** questions about data requirements that guide architecture decisions. _(Inquiry)_
10. **Synthesize** multiple database paradigms into a cohesive capstone project. _(Integration)_

### Assessment Outcomes

| Outcome                       | Assessment Method                     | Weeks  |
| ----------------------------- | ------------------------------------- | ------ |
| Database paradigm selection   | Quiz 1, Final Project Proposal        | 1, 10  |
| Normalization & schema design | Quiz 6, Assignment 6                  | 6      |
| SQL query construction        | Quizzes 2-5, Assignments 2-5, Midterm | 2-5, 8 |
| DevOps & tooling              | Assignment 1, Assignment 7            | 1, 7   |
| Systems analysis              | Quizzes 8-12, Final Project           | 10-14  |
| Tool trade-off analysis       | Quiz 7, Midterm                       | 7, 8   |
| Non-relational databases      | Quizzes 8-12, Final Project           | 10-14  |
| Team collaboration            | Final Project, Peer Evaluations       | 10-16  |
| Requirements inquiry          | Final Project Proposal, Checkpoints   | 10-13  |
| Multi-paradigm synthesis      | Final Project                         | 15-16  |

### Champlain Competencies Addressed

| Competency              | Course Coverage                                                                    | Primary Assessments                 |
| ----------------------- | ---------------------------------------------------------------------------------- | ----------------------------------- |
| **Analysis**            | Database paradigm evaluation, normalization methodology, tool selection trade-offs | Quizzes, Midterm, Assignments       |
| **Technology Literacy** | Docker, SQL, Drizzle ORM, NoSQL databases, systems thinking                        | All Assignments, Final Project      |
| **Collaboration**       | Team-based final project with defined roles and accountability                     | Final Project, Peer Evaluations     |
| **Integration**         | Multi-database architecture, combining paradigms for real-world solutions          | Final Project                       |
| **Inquiry**             | Requirements analysis, architecture decisions, problem formulation                 | Final Project Proposal, Checkpoints |

## Phylosophy

This course takes a top-down, DevOps-first approach to database management. Students learn by doing. Every concept is reinforced through hands-on coding assignments automatically tested via GitHub Actions. The course emphasizes:

- _Infrastructure as Code_: All databases run in Docker containers
- _Continuous Integration_: Assignments auto-graded via GitHub Actions
- _Real-World Practices_: Version control, testing, and deployment pipelines
- _Breadth of Knowledge_: SQL foundations + modern database paradigms

## Course Overview

| Phase                | Weeks | Focus                      | Assessment                                          |
| -------------------- | ----- | -------------------------- | --------------------------------------------------- |
| Foundations          | 1-8   | SQL & Relational Databases | Weekly Quizzes + Coding Assignments + Final Project |
| Polyglot Persistence | 10-14 | NoSQL & Specialized DBs    | Weekly Quizzes + Final Project                      |
| Final Project        | 15-16 | Final Project              | Project Deliverables                                |

## Schedule for Spring 2026

::: warning

This is a work in progress, and the schedule is subject to change. Every change will be communicated in class. Use this github repo as the source of truth for the schedule and materials. The materials provided in canvas are just a copy for archiving purposes and might be outdated.

:::

## Holiday and Important Dates

No Classes MLK Day: Monday, 1/19 | Spring Break 3/9-13 | No Classes: Friday 4/10

## Week-by-Week Course Plan

---

### WEEK 01 — The Database Landscape & DevOps Setup

**2026/01/12 – 2026/01/16**

**Monday 2026/01/12** — Introduction & Database Taxonomy

- Course overview, expectations, GitHub repo setup
- The Database Zoo: relational, document, key-value, wide-column, graph, time series, search engines, vector, event streaming
- Decision framework: when to use what

**Thursday 2026/01/15** — Docker & Container-Based Development

- Running databases in containers, Docker Compose
- Lab: Clone repo, `docker-compose up` with PostgreSQL + MongoDB + Redis
- TypeScript + Node.js environment setup

**Quiz 1** (2026/01/15): Database types & use cases
**Assignment 1** (due 2026/01/18): Docker setup + container commands

---

### WEEK 02 — SQL Fundamentals: DDL & Basic Queries

**2026/01/19 – 2026/01/23**

**Monday 2026/01/19** — NO CLASS (Martin Luther King Jr. Day)

**Thursday 2026/01/22** — Data Definition Language & Basic SELECT

- Relational model fundamentals
- CREATE TABLE, data types, constraints (PK, FK, NOT NULL, UNIQUE, CHECK)
- ALTER TABLE, DROP TABLE
- SELECT, FROM, WHERE, ORDER BY, LIMIT
- Seed database introduction: e-commerce schema

**Quiz 2** (2026/01/22): DDL syntax & constraints
**Assignment 2** (due 2026/01/25): Create tables + SELECT queries

---

### WEEK 03 — SQL: Filtering & Aggregation

**2026/01/26 – 2026/01/30**

**Monday 2026/01/26** — Advanced WHERE & Pattern Matching

- Boolean logic (AND, OR, NOT), LIKE, IN, BETWEEN
- NULL handling, CASE expressions, date/time functions

**Thursday 2026/01/29** — Aggregation & Grouping

- COUNT, SUM, AVG, MIN, MAX
- GROUP BY, HAVING, DISTINCT in aggregates

**Quiz 3** (2026/01/29): Filtering & aggregation
**Assignment 3** (due 2026/02/01): Complex filters and aggregations

---

### WEEK 04 — SQL: Joins

**2026/02/02 – 2026/02/06**

**Monday 2026/02/02** — Join Fundamentals

- INNER JOIN mechanics, join conditions, multi-table joins, table aliases

**Thursday 2026/02/05** — Outer Joins & Advanced Patterns

- LEFT/RIGHT/FULL OUTER JOIN, self-joins, CROSS JOIN
- Join visualization and mental models

**Quiz 4** (2026/02/05): Join types & behavior
**Assignment 4** (due 2026/02/08): Multi-table queries

---

### WEEK 05 — SQL: Subqueries, CTEs & Views

**2026/02/09 – 2026/02/13**

**Monday 2026/02/09** — Subqueries & Set Operations

- Scalar subqueries, subqueries in WHERE (IN, EXISTS, ANY, ALL)
- Correlated vs non-correlated subqueries
- UNION, INTERSECT, EXCEPT

**Thursday 2026/02/12** — CTEs & Views

- Common Table Expressions (WITH clause), recursive CTEs
- Views: CREATE VIEW, use cases, updatable views
- Materialized Views: CREATE MATERIALIZED VIEW, REFRESH strategies, performance trade-offs

**Quiz 5** (2026/02/12): Subqueries, CTEs & Views
**Assignment 5** (due 2026/02/15): Analytical queries + create views

---

### WEEK 06 — Database Design: Normalization

**2026/02/16 – 2026/02/20**

**Monday 2026/02/16** — Normalization Theory

- Functional dependencies
- 1NF, 2NF, 3NF, BCNF
- Insert, update, delete anomalies

**Thursday 2026/02/19** — Practical Normalization

- Denormalization trade-offs (when to use materialized views)
- Real-world schema analysis
- Activity: normalize a problematic spreadsheet

**Quiz 6** (2026/02/19): Normal forms & dependencies
**Assignment 6** (due 2026/02/22): Design normalized schema + DDL

---

### WEEK 07 — Database Design: Relationships, Stored Logic & Query Builders

**2026/02/23 – 2026/02/27**

**Monday 2026/02/23** — ER Diagrams & Relationships

- Entity-Relationship modeling, cardinality (1:1, 1:N, M:N)
- Junction tables, UML notation
- Translating ER diagrams to DDL, CASCADE options

**Thursday 2026/02/26** — Functions, Procedures & Drizzle ORM

- SQL Functions: CREATE FUNCTION, parameters, return types
- Stored Procedures: CREATE PROCEDURE, CALL, control flow
- Triggers: CREATE TRIGGER, BEFORE/AFTER, use cases
- Indexing fundamentals
- Drizzle ORM: schema definition, type-safe queries, migrations
- Raw SQL vs query builders: trade-offs and when to use each

**Quiz 7** (2026/02/26): ER modeling, stored logic & query builders
**Assignment 7** (due 2026/03/01): ER diagram + schema + functions + Drizzle queries

---

### WEEK 08 — Midterm

**2026/03/02 – 2026/03/06**

**Monday 2026/03/02** — Midterm Review

- Recap of Weeks 01–07
- Q&A session
- **Final Project Introduction**

**Thursday 2026/03/05** — Midterm Exam

- Covers Weeks 01–07

---

### WEEK 09 — SPRING BREAK

**2026/03/09 – 2026/03/13**

No classes or assignments.

---

### WEEK 10 — Document Databases: MongoDB

**2026/03/16 – 2026/03/20**

**Monday 2026/03/16** — MongoDB Fundamentals

- Document model vs relational, JSON/BSON
- Schema design patterns (embedding vs referencing)
- When to choose document DBs

**Thursday 2026/03/19** — MongoDB CRUD & Aggregation

- insertOne/Many, find with projections, query operators
- Update operators ($set, $push, $pull), aggregation pipeline
- Drizzle + MongoDB (drizzle-orm/mongodb)

**Quiz 8** (2026/03/19): Document DB concepts
**Final Project** (due 2026/03/22): Team formation + proposal

---

### WEEK 11 — Key-Value & Wide-Column Stores

**2026/03/23 – 2026/03/27**

**Monday 2026/03/23** — Key-Value Stores: Redis

- Redis data structures (strings, lists, sets, hashes, sorted sets)
- Use cases: caching, sessions, rate limiting, leaderboards
- TTL, expiration, Pub/Sub
- ioredis client for TypeScript

**Thursday 2026/03/26** — Wide-Column Stores: Cassandra

- Column families, partition keys, clustering columns
- CAP theorem, eventual consistency, CQL basics

**Quiz 9** (2026/03/26): Key-Value & Wide-Column
**Final Project** (due 2026/03/29): Architecture design

---

### WEEK 12 — Graph Databases: Neo4j

**2026/03/30 – 2026/04/03**

**Monday 2026/03/30** — Graph Database Fundamentals

- Nodes, relationships, properties
- When graphs beat relational
- Use cases: social networks, recommendations, fraud detection

**Thursday 2026/04/02** — Cypher Query Language

- MATCH, CREATE, WHERE, RETURN
- Path queries, variable-length patterns, MERGE
- neo4j-driver for TypeScript

**Quiz 10** (2026/04/02): Graph concepts & Cypher
**Final Project** (due 2026/04/05): Checkpoint #1

---

### WEEK 13 — Time Series & Search Engines

**2026/04/06 – 2026/04/10**

**Monday 2026/04/06** — Time Series Databases

- TimescaleDB: hypertables, retention policies, continuous aggregates
- Downsampling, IoT/monitoring use cases
- TimescaleDB with Drizzle (PostgreSQL extension)

**Thursday 2026/04/09** — Search Engines: Elasticsearch

- Inverted indices, analyzers, tokenizers
- Documents, indices, mappings, basic queries (match, term, bool)
- @elastic/elasticsearch client

**Quiz 11** (2026/04/09): Time Series & Search
**Final Project** (due 2026/04/12): Checkpoint #2

---

### WEEK 14 — Vector Databases & Event Streaming

**2026/04/13 – 2026/04/17**

**Monday 2026/04/13** — Vector Databases

- Embeddings, pgvector, similarity search (cosine, euclidean)
- Indexing (IVFFlat, HNSW), RAG architecture
- pgvector with Drizzle (PostgreSQL extension)

**Thursday 2026/04/16** — Event Streaming: Kafka

- Topics, partitions, producers/consumers
- Consumer groups, stream processing concepts
- kafkajs client

**Quiz 12** (2026/04/16): Vector DBs & Streaming
**Final Project** (due 2026/04/19): Feature freeze

---

### WEEK 15 — Final Project Work

**2026/04/20 – 2026/04/24**

**Monday 2026/04/20** — Project work session, instructor consultations

**Thursday 2026/04/23** — Demo dry runs, peer feedback, documentation review

**Final Project** (due 2026/04/26): Code complete + documentation

---

### WEEK 16 — Final Presentations

**2026/04/27 – 2026/05/01**

**Monday 2026/04/27** — Presentations (Group A)

**Thursday 2026/04/30** — Presentations (Group B) + Course wrap-up

**Final Project** (due 2026/04/30): Final submission
**Peer Evaluations** (due 2026/04/30): Team evaluations

---

## Tech Stack

**Language**: TypeScript (Node.js), SQL
**Query Builder**: Drizzle ORM, and other native clients
**Databases**: PostgreSQL, MongoDB, Redis, Neo4j, TimescaleDB, Elasticsearch, Kafka
**Containerization**: Docker, Docker Compose
**CI/CD**: GitHub Actions
**Testing**: Vitest / Jest

---

## Assessment Summary

**Weeks 01–07**: 7 Quizzes + 7 Coding Assignments
**Week 08**: Midterm Exam
**Week 09**: Spring Break
**Weeks 10–14**: 5 Quizzes + Project Milestones
**Weeks 15–16**: Project completion and presentations

**Totals**: 12 Quizzes • 7 Coding Assignments • 1 Midterm • 7 Project Milestones

**Gradings:**

| Component                  | Weight |
| -------------------------- | ------ |
| Attendance & Participation | 10%    |
| Quizzes                    | 20%    |
| Coding Assignments         | 25%    |
| Midterm Exam               | 20%    |
| Final Project              | 25%    |
