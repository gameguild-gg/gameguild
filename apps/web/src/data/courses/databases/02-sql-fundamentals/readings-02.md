# Week 02 Readings & Quiz Coverage

## Required Readings

Complete the following readings before attempting the Week 02 quizzes:

1. [data-definition-language.md](data-definition-language.md) — DDL statements (CREATE, ALTER, DROP, TRUNCATE)
2. [data-manipulation-language.md](data-manipulation-language.md) — DML statements (INSERT, UPDATE, DELETE)
3. [data-query-language.md](data-query-language.md) — DQL statements (SELECT, FROM, WHERE, ORDER BY, LIMIT)
4. [constraints.md](constraints.md) — Database constraints (PK, FK, NOT NULL, UNIQUE, CHECK, DEFAULT)
5. [idempotency.md](idempotency.md) — Understanding and designing idempotent database operations
6. [dbml-introduction.md](dbml-introduction.md) — Database Markup Language for schema design

---

## Week 02 Quizzes Overview

| Quiz | Questions | Format | Topics Covered |
|------|-----------|--------|----------------|
| [idempotency-quiz.md](quiz/idempotency-quiz.md) | 24 | Categorization | SQL statement classification as idempotent/non-idempotent |
| [idempotency-fix-quiz.md](quiz/idempotency-fix-quiz.md) | 10 | Multiple Choice | Making non-idempotent operations idempotent |
| [ddl-dml-dql-quiz.md](quiz/ddl-dml-dql-quiz.md) | 10 | Translation | Converting between requirements and SQL |
| [constraints-datatypes-quiz.md](quiz/constraints-datatypes-quiz.md) | 25 | T/F + MC | Constraints and data types knowledge |

**Total: 69 questions**

---

## Quiz Coverage Analysis

### Idempotency Quiz (24 questions) — From [idempotency.md](idempotency.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| Idempotent INSERT | INSERT: The Idempotency Problem | E, M, V |
| Non-idempotent INSERT | INSERT: The Idempotency Problem | C, I, N, Q |
| Idempotent UPDATE | UPDATE: Idempotent vs Non-Idempotent | A, K, O, X |
| Non-idempotent UPDATE | UPDATE: Idempotent vs Non-Idempotent | B, L, P, W |
| Idempotent DELETE | DELETE: Naturally Idempotent | D, S |
| Non-idempotent DELETE | DELETE: Naturally Idempotent | (none — DELETE is naturally idempotent) |
| Idempotent DDL | DDL is NOT Idempotent | G, H |
| Non-idempotent DDL | DDL is NOT Idempotent | F, T, U |
| Sequence operations | Why Idempotency Matters | J |

---

### Idempotency Fix Quiz (10 questions) — From [idempotency.md](idempotency.md) & [data-manipulation-language.md](data-manipulation-language.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| ON CONFLICT DO NOTHING | INSERT: The Idempotency Problem | Q1, Q6 |
| ON CONFLICT DO UPDATE | Real-World Patterns | (related Q1) |
| Idempotency keys | Real-World Patterns | Q3 |
| Fixed values vs relative values | UPDATE: Idempotent vs Non-Idempotent | Q7, Q8 |
| Cannot be made idempotent | Why Idempotency Matters | Q2, Q5, Q10 |
| DDL IF EXISTS / IF NOT EXISTS | DDL is NOT Idempotent | Q4 |
| DELETE idempotency | DELETE: Naturally Idempotent | Q9 |

---

### DDL/DML/DQL Translation Quiz (10 questions) — From [data-definition-language.md](data-definition-language.md), [data-manipulation-language.md](data-manipulation-language.md), [data-query-language.md](data-query-language.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| CREATE TABLE syntax | DDL: CREATE TABLE | Q1, Q6 |
| ALTER TABLE operations | DDL: ALTER TABLE | Q2, Q9 |
| SERIAL/BIGSERIAL | DDL: Data Types, ID Types | Q1 |
| Constraints inline syntax | DDL: Constraints | Q1, Q6 |
| UPDATE with SET | DML: UPDATE Statement | Q3 |
| INSERT ... SELECT | DML: INSERT Statement | Q8, Q10 |
| DELETE FROM with WHERE | DML: DELETE Statement | Q5 |
| SELECT with WHERE, ORDER BY, LIMIT | DQL: SELECT, WHERE, ORDER BY, LIMIT | Q4, Q7 |
| Schema creation | DDL: CREATE SCHEMA | Q6 |
| CHECK constraints | DDL: Constraints → CHECK | Q6 |

---

### Constraints & Data Types Quiz (25 questions) — From [constraints.md](constraints.md) & [data-definition-language.md](data-definition-language.md)

#### Part A: True or False (10 questions)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| PRIMARY KEY (single vs composite) | Constraints: PRIMARY KEY | Q1 |
| UNIQUE with NULL values | Constraints: UNIQUE | Q2 |
| VARCHAR vs CHAR behavior | DDL: Data Types | Q3 |
| Foreign key indexing | Constraints: FOREIGN KEY | Q4 |
| DECIMAL precision limits | DDL: Data Types | Q5 |
| DEFAULT with NOT NULL | Constraints: DEFAULT, NOT NULL | Q6 |
| SERIAL pseudo-type | DDL: ID Types | Q7 |
| CHECK constraint scope | Constraints: CHECK | Q8 |
| TIMESTAMPTZ behavior | DDL: Data Types | Q9 |
| TEXT vs VARCHAR performance | DDL: Data Types | Q10 |

#### Part B: Multiple Choice (15 questions)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| DECIMAL for currency | DDL: Data Types | Q11 |
| DEFAULT value behavior | Constraints: DEFAULT | Q12 |
| VARCHAR length enforcement | DDL: Data Types | Q13 |
| UNIQUE NOT NULL combination | Constraints: UNIQUE, NOT NULL | Q14 |
| Foreign key syntax | Constraints: FOREIGN KEY | Q15 |
| UUID for distributed systems | DDL: ID Types | Q16 |
| BOOLEAN input values | DDL: Data Types | Q17 |
| Composite primary key syntax | Constraints: PRIMARY KEY | Q18 |
| SMALLINT, INT, BIGINT sizes | DDL: Data Types | Q19 |
| TIMESTAMP WITH TIME ZONE | DDL: Data Types | Q20 |
| INT max value | DDL: Data Types | Q21 |
| NOT NULL with PRIMARY KEY | Constraints: NOT NULL, PRIMARY KEY | Q22 |
| CHECK for range validation | Constraints: CHECK | Q23 |
| Constraint interaction validation | All constraint types | Q24 |
| DECIMAL for coordinates | DDL: Data Types | Q25 |

---

## Topic-to-Question Quick Reference

### From [idempotency.md](idempotency.md)

| Topic | Idempotency Quiz | Idempotency Fix Quiz |
|-------|------------------|----------------------|
| Why Idempotency Matters | — | Q2, Q5, Q10 |
| Idempotency in SQL Operations | A-X (all) | Q1-Q10 (all) |
| INSERT: The Idempotency Problem | C, E, I, M, N, Q, V | Q1, Q3, Q6, Q8 |
| UPDATE: Idempotent vs Non-Idempotent | A, B, K, L, O, P, W, X | Q2, Q5, Q7, Q10 |
| DELETE: Naturally Idempotent | D, S | Q9 |
| Real-World Patterns | V | Q1, Q3 |

### From [data-definition-language.md](data-definition-language.md)

| Topic | Idempotency Quiz | DDL/DML/DQL Quiz | Constraints Quiz |
|-------|------------------|------------------|------------------|
| CREATE DATABASE | — | — | — |
| CREATE SCHEMA | — | Q6 | — |
| CREATE TABLE | G | Q1 | Q11-Q25 (many) |
| Data Types | — | Q1 | Q3, Q5, Q9-Q13, Q16-Q21, Q25 |
| Constraints (inline) | — | Q1, Q6 | Q1-Q8, Q14, Q18, Q22-Q24 |
| ID Types (SERIAL, UUID) | — | Q1 | Q7, Q16 |
| ALTER TABLE | T | Q2, Q9 | — |
| DROP | U | — | — |
| TRUNCATE | R | — | — |
| DDL is NOT Idempotent | F, G, H, T, U | Q4 | — |

### From [data-manipulation-language.md](data-manipulation-language.md)

| Topic | Idempotency Quiz | DDL/DML/DQL Quiz |
|-------|------------------|------------------|
| INSERT Statement | C, E, I, M, N, Q, V | Q8, Q10 |
| INSERT with RETURNING | — | Q8 (preview) |
| INSERT from SELECT | — | Q8, Q10 |
| UPDATE Statement | A, B, K, L, O, P, W, X | Q3 |
| DELETE Statement | D, S | Q5 |
| Idempotency in DML | All A-X | — |

### From [data-query-language.md](data-query-language.md)

| Topic | DDL/DML/DQL Quiz |
|-------|------------------|
| SELECT Statement | Q4, Q7 |
| SELECT Columns | Q4, Q7 |
| FROM Clause | Q4, Q7 |
| WHERE Clause | Q4, Q5, Q7 |
| ORDER BY Clause | Q4, Q7 |
| LIMIT and OFFSET | Q4, Q7 |
| DISTINCT | — |
| Column Expressions | — |

### From [constraints.md](constraints.md)

| Topic | Constraints Quiz (Part A) | Constraints Quiz (Part B) |
|-------|---------------------------|---------------------------|
| Why Constraints Matter | — | — |
| PRIMARY KEY | Q1 | Q18, Q22 |
| FOREIGN KEY | Q4 | Q15 |
| NOT NULL | Q6 | Q14, Q22 |
| UNIQUE | Q2 | Q14 |
| CHECK | Q8 | Q23 |
| DEFAULT | Q6 | Q12 |
| Constraint Naming Conventions | — | — |
| Performance Considerations | Q4 | — |

---

## Learning Objectives Alignment

After completing the readings and quizzes, students should be able to:

### DDL (Data Definition Language)
- ✅ Write CREATE TABLE statements with proper syntax — **DDL Quiz: Q1, Q6**
- ✅ Use ALTER TABLE to modify existing structures — **DDL Quiz: Q2, Q9**
- ✅ Choose appropriate data types for different use cases — **Constraints Quiz: Q3, Q5, Q9-Q13, Q16-Q21, Q25**
- ✅ Understand SERIAL vs BIGSERIAL vs UUID for IDs — **Constraints Quiz: Q7, Q16**
- ✅ Apply IF EXISTS / IF NOT EXISTS for idempotent DDL — **Idempotency Quiz: G, H; Fix Quiz: Q4**

### DML (Data Manipulation Language)
- ✅ Write INSERT, UPDATE, DELETE statements correctly — **DDL Quiz: Q3, Q5, Q8, Q10**
- ✅ Use INSERT ... SELECT for data migration — **DDL Quiz: Q8, Q10**
- ✅ Identify idempotent vs non-idempotent DML — **Idempotency Quiz: All A-X**
- ✅ Apply ON CONFLICT for idempotent inserts — **Idempotency Quiz: E, M, V; Fix Quiz: Q1, Q6**
- ✅ Use absolute values instead of relative for idempotent updates — **Idempotency Quiz: A, K, O, X; Fix Quiz: Q7, Q8**

### DQL (Data Query Language)
- ✅ Write SELECT queries with WHERE, ORDER BY, LIMIT — **DDL Quiz: Q4, Q7**
- ✅ Interpret query results correctly — **DDL Quiz: Q4**

### Constraints
- ✅ Apply PRIMARY KEY correctly (single and composite) — **Constraints Quiz: Q1, Q18, Q22**
- ✅ Implement FOREIGN KEY relationships — **Constraints Quiz: Q4, Q15**
- ✅ Use NOT NULL appropriately — **Constraints Quiz: Q6, Q14, Q22**
- ✅ Apply UNIQUE constraints — **Constraints Quiz: Q2, Q14**
- ✅ Write CHECK constraints for validation — **Constraints Quiz: Q8, Q23**
- ✅ Set DEFAULT values correctly — **Constraints Quiz: Q6, Q12**
- ✅ Understand constraint interactions — **Constraints Quiz: Q24**

### Idempotency
- ✅ Define and explain idempotency — **Idempotency Quiz: All**
- ✅ Classify SQL statements as idempotent or non-idempotent — **Idempotency Quiz: All A-X**
- ✅ Convert non-idempotent operations to idempotent ones — **Fix Quiz: Q1-Q10**
- ✅ Recognize when operations cannot be made idempotent — **Fix Quiz: Q2, Q5, Q10**
- ✅ Apply idempotency keys in real-world scenarios — **Idempotency Quiz: V; Fix Quiz: Q3**

---

## Study Tips

1. **Start with DQL** — Understanding SELECT helps with reading INSERT...SELECT and UPDATE...FROM patterns
2. **Practice DDL syntax** — CREATE TABLE syntax is foundational for all other operations
3. **Focus on idempotency patterns** — The ON CONFLICT clause and absolute vs relative values are key concepts
4. **Know your data types** — Especially DECIMAL vs FLOAT for money, and TIMESTAMPTZ for global apps
5. **Understand constraint interactions** — How DEFAULT + NOT NULL work together, why PK implies NOT NULL

---

## Time Estimates

| Reading | Estimated Time |
|---------|----------------|
| data-definition-language.md | 20-25 min |
| data-manipulation-language.md | 20-25 min |
| data-query-language.md | 15-20 min |
| constraints.md | 25-30 min |
| idempotency.md | 15-20 min |
| dbml-introduction.md | 20-25 min |
| **Total Readings** | **~2 hours** |

| Quiz | Estimated Time |
|------|----------------|
| idempotency-quiz.md | 15-20 min |
| idempotency-fix-quiz.md | 15-20 min |
| ddl-dml-dql-quiz.md | 20-25 min |
| constraints-datatypes-quiz.md | 25-30 min |
| **Total Quizzes** | **~75-95 min** |
