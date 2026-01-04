# Week 07 Readings: Schema Patterns, TCL, DCL & ORM

## Overview

Week 07 covers a broad range of topics related to advanced schema design, database security, transaction management, and modern database access patterns. This is one of the largest weeks in terms of content scope.

---

## Reading Assignments

### Required Reading

| Resource | Topic | Est. Time |
|----------|-------|-----------|
| [schema-patterns.md](schema-patterns.md) | Soft delete, versioning, checksums, audit trails | 25 min |
| [referential-actions.md](referential-actions.md) | CASCADE, RESTRICT, SET NULL, SET DEFAULT | 20 min |
| [functions-procedures-triggers.md](functions-procedures-triggers.md) | Stored logic in PostgreSQL | 30 min |
| [transactions.md](transactions.md) | TCL, ACID, isolation levels | 30 min |
| [access-control.md](access-control.md) | GRANT, REVOKE, roles, RLS | 25 min |
| [scalability-basics.md](scalability-basics.md) | Replication, partitioning, sharding | 25 min |
| [orm-query-builders.md](orm-query-builders.md) | Drizzle ORM, SQL injection prevention | 30 min |

**Total estimated reading time: ~3 hours**

### Supplementary Resources

| Resource | Description |
|----------|-------------|
| [PostgreSQL Triggers Documentation](https://www.postgresql.org/docs/current/triggers.html) | Official trigger reference |
| [PostgreSQL Transaction Isolation](https://www.postgresql.org/docs/current/transaction-iso.html) | Detailed isolation level docs |
| [PostgreSQL Row Security Policies](https://www.postgresql.org/docs/current/ddl-rowsecurity.html) | RLS documentation |
| [Drizzle ORM Documentation](https://orm.drizzle.team/docs/overview) | Official Drizzle docs |
| [OWASP SQL Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html) | Security best practices |

---

## Topic Coverage Matrix

### Schema Patterns → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| Soft Delete | deleted_at, partial indexes, unique constraints | 1, 2 |
| Versioning | Optimistic locking, version column, history tables | 3, 6 |
| Checksums | Data integrity, pgcrypto | 5 |
| Audit Trails | Audit columns, trigger-based logging | 8, 90 |
| Anti-Patterns | EAV, CSV columns, polymorphic associations | 4, 7 |

### Referential Actions → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| ON DELETE CASCADE | Automatic child deletion | 9, 85 |
| SET NULL / SET DEFAULT | Optional relationships | 10, 12 |
| RESTRICT vs NO ACTION | Immediate vs deferred checking | 11 |
| ON UPDATE CASCADE | Foreign key updates | 13 |
| Combined Actions | ON DELETE + ON UPDATE | 14 |
| Cascading Chains | Multi-table propagation | 15 |
| Circular References | Bidirectional relationships | 16 |

### Functions, Procedures & Triggers → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| Functions vs Procedures | Transaction control, CALL vs SELECT | 17, 20 |
| Return Types | RETURN, SETOF, TRIGGER | 18, 19, 21 |
| Trigger Timing | BEFORE, AFTER | 22, 79 |
| Trigger Variables | NEW, OLD, TG_OP | 23, 27 |
| Trigger Scope | FOR EACH ROW vs STATEMENT | 24 |
| Trigger Syntax | CREATE TRIGGER | 25 |
| Trigger Control | Cancelling operations | 26 |
| PL/pgSQL | Language choice | 28 |

### Transactions (TCL) → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| COMMIT | Persisting changes | 29 |
| ROLLBACK | Undoing changes | 30, 42 |
| SAVEPOINT | Partial rollback | 31 |
| ACID Properties | Atomicity, Consistency, Isolation, Durability | 32, 33, 78 |
| Isolation Levels | READ UNCOMMITTED, READ COMMITTED, REPEATABLE READ, SERIALIZABLE | 34, 35, 37, 87 |
| Concurrency Issues | Dirty reads, non-repeatable reads, phantom reads | 36, 40 |
| Deadlocks | Detection and resolution | 38 |
| Locking | FOR UPDATE, SKIP LOCKED, NOWAIT | 39, 41, 82 |
| Lost Updates | Concurrent modification | 83 |

### Access Control (DCL) → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| GRANT | Giving privileges | 43, 47 |
| REVOKE | Removing privileges | 44 |
| Roles vs Users | Same in PostgreSQL | 45, 48 |
| Schema Privileges | USAGE, CREATE | 46 |
| Row-Level Security | RLS policies | 49, 50, 80 |
| Default Privileges | Future objects | 51, 88 |
| Role Management | DROP ROLE constraints | 52 |
| Column Privileges | Column-level GRANT | 53 |
| Least Privilege | Security principle | 54 |
| Viewing Privileges | information_schema | 84 |

### Scalability → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| Vertical vs Horizontal Scaling | Scale up vs scale out | 55 |
| Read Replicas | Distributing read load | 56, 57, 81 |
| Partitioning | Range, list, hash | 58, 59 |
| Sharding | Data distribution | 60, 61, 64 |
| Connection Pooling | PgBouncer | 62, 63 |
| Optimization Priority | Indexes first | 65 |

### ORM & Query Builders → Quiz Questions

| Topic | Concepts | Quiz Questions |
|-------|----------|----------------|
| Query Builder Benefits | Type safety, injection prevention | 66 |
| ORM Definition | Object Relational Mapping | 67 |
| Drizzle Syntax | eq(), select(), where() | 68, 86 |
| SQL Injection Prevention | Parameterized queries | 69, 70, 71, 73, 74, 75 |
| Drizzle Transactions | db.transaction() | 72 |
| Raw SQL in Drizzle | sql template literal | 76 |
| ORM Trade-offs | Performance vs convenience | 89 |

---

## Learning Objectives

After completing this week's readings, you should be able to:

### Schema Patterns
- [ ] Implement soft delete with proper indexing and constraint handling
- [ ] Design version control patterns for row-level data
- [ ] Create audit trails using triggers and audit tables
- [ ] Identify and avoid common schema anti-patterns

### Referential Actions
- [ ] Choose appropriate ON DELETE/UPDATE actions for relationships
- [ ] Understand the difference between RESTRICT and NO ACTION
- [ ] Design cascading chains safely
- [ ] Handle circular reference scenarios

### Functions, Procedures & Triggers
- [ ] Create SQL and PL/pgSQL functions
- [ ] Understand when to use procedures vs functions
- [ ] Implement triggers for automated data maintenance
- [ ] Use trigger variables (NEW, OLD, TG_OP) effectively

### Transactions (TCL)
- [ ] Use BEGIN, COMMIT, ROLLBACK correctly
- [ ] Implement savepoints for complex transactions
- [ ] Explain all four ACID properties
- [ ] Choose appropriate isolation levels for different scenarios
- [ ] Identify and prevent concurrency issues

### Access Control (DCL)
- [ ] Grant and revoke privileges at table, column, and schema levels
- [ ] Design role hierarchies for team access
- [ ] Implement Row-Level Security for multi-tenant applications
- [ ] Apply the principle of least privilege

### Scalability
- [ ] Distinguish between vertical and horizontal scaling
- [ ] Explain primary-replica replication
- [ ] Design partitioning strategies for large tables
- [ ] Understand when and how to implement sharding
- [ ] Configure connection pooling

### ORM & Query Builders
- [ ] Use Drizzle ORM for type-safe database access
- [ ] Prevent SQL injection in all database code
- [ ] Choose between raw SQL, query builders, and ORMs
- [ ] Handle dynamic identifiers safely

---

## Quiz Preparation

### Topic Distribution in Week 07 Quiz

| Topic | Question Count | Percentage |
|-------|---------------|------------|
| Schema Patterns | 8 | 8.9% |
| Referential Actions | 8 | 8.9% |
| Functions/Procedures/Triggers | 12 | 13.3% |
| Transactions (TCL) | 14 | 15.6% |
| Access Control (DCL) | 12 | 13.3% |
| Scalability | 11 | 12.2% |
| ORM & Query Builders | 11 | 12.2% |
| Mixed/Integration | 14 | 15.6% |
| **Total** | **90** | **100%** |

### High-Priority Topics

Based on question frequency and real-world importance:

1. **Transactions & ACID** (14 questions) — Critical for data integrity
2. **Access Control** (12 questions) — Essential for security
3. **Triggers & Functions** (12 questions) — Key for automation
4. **SQL Injection Prevention** (7 questions) — Security fundamental

### Study Tips

1. **Practice writing triggers** — CREATE TRIGGER syntax appears frequently
2. **Know isolation levels** — Understand what each prevents
3. **Memorize ACID** — Be able to explain each property
4. **GRANT/REVOKE syntax** — Know column-level and schema-level grants
5. **SQL injection patterns** — Recognize vulnerable code

---

## Hands-On Exercises

Complete these exercises before the quiz:

### Exercise 1: Soft Delete with Audit
Create a table with:
- Soft delete (deleted_at column)
- Unique constraint that works with soft delete
- Audit trigger that logs all changes

### Exercise 2: Transaction Practice
Write a transaction that:
- Creates an order
- Adds items
- Uses a savepoint before payment processing
- Rolls back to savepoint if payment fails
- Commits if successful

### Exercise 3: Role-Based Access
Set up:
- Three roles: viewer, editor, admin
- Appropriate privileges for each
- Row-level security for multi-tenant data

### Exercise 4: Drizzle CRUD
Using Drizzle ORM:
- Define a schema with two related tables
- Implement all CRUD operations
- Add a transaction that modifies both tables

---

## Key Terminology

| Term | Definition |
|------|------------|
| Soft Delete | Marking records as deleted instead of removing them |
| Optimistic Locking | Using version numbers to detect concurrent modifications |
| TCL | Transaction Control Language (BEGIN, COMMIT, ROLLBACK) |
| DCL | Data Control Language (GRANT, REVOKE) |
| ACID | Atomicity, Consistency, Isolation, Durability |
| Isolation Level | How much transactions can see of concurrent changes |
| Dirty Read | Reading uncommitted data from another transaction |
| Phantom Read | New rows appearing in repeated queries |
| RLS | Row-Level Security — controlling access to individual rows |
| Sharding | Distributing data across multiple database servers |
| Partitioning | Splitting a table into smaller physical pieces |
| Read Replica | A database copy that handles read queries |
| ORM | Object Relational Mapping — database abstraction layer |
| SQL Injection | Attack that inserts malicious SQL via user input |
| Parameterized Query | Query where user input is passed as parameters, not concatenated |

---

## Common Mistakes to Avoid

1. **Using string concatenation in SQL** — Always use parameterized queries
2. **Forgetting to handle soft-deleted records in queries** — Add WHERE deleted_at IS NULL
3. **Using CASCADE without understanding implications** — Can delete more than expected
4. **Choosing wrong isolation level** — Read Committed is usually sufficient
5. **Granting ALL PRIVILEGES** — Violates least privilege principle
6. **Ignoring unique constraints with soft delete** — Need partial indexes
7. **Not using transactions for multi-step operations** — Risk inconsistent state
8. **Returning NEW in AFTER triggers** — Only BEFORE triggers use return values meaningfully
