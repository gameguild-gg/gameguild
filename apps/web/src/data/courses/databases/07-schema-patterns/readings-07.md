# Week 07 Readings: Schema Patterns, TCL, DCL & ORM

## Overview

Week 07 covers a broad range of topics related to advanced schema design, database security, transaction management, and modern database access patterns. This is one of the largest weeks in terms of content scope.

---

## Reading Assignments

### Required Reading

| Resource                                                                      | Topic                                            | Est. Time |
| ----------------------------------------------------------------------------- | ------------------------------------------------ | --------- |
| [schema-patterns.md](readings-07/schema-patterns)                             | Soft delete, versioning, checksums, audit trails | 25 min    |
| [referential-actions.md](readings-07/referential-actions)                     | CASCADE, RESTRICT, SET NULL, SET DEFAULT         | 20 min    |
| [functions-procedures-triggers.md](readings-07/functions-procedures-triggers) | Stored logic in PostgreSQL                       | 30 min    |
| [transactions.md](readings-07/transactions)                                   | TCL, ACID, isolation levels                      | 30 min    |
| [access-control.md](readings-07/access-control)                               | GRANT, REVOKE, roles, RLS                        | 25 min    |
| [scalability-basics.md](readings-07/scalability-basics)                       | Replication, partitioning, sharding              | 25 min    |
| [orm-query-builders.md](readings-07/orm-query-builders)                       | Drizzle ORM, SQL injection prevention            | 30 min    |

**Total estimated reading time: ~3 hours**

### Supplementary Resources

| Resource                                                                                                                   | Description                   |
| -------------------------------------------------------------------------------------------------------------------------- | ----------------------------- |
| [PostgreSQL Triggers Documentation](https://www.postgresql.org/docs/current/triggers.html)                                 | Official trigger reference    |
| [PostgreSQL Transaction Isolation](https://www.postgresql.org/docs/current/transaction-iso.html)                           | Detailed isolation level docs |
| [PostgreSQL Row Security Policies](https://www.postgresql.org/docs/current/ddl-rowsecurity.html)                           | RLS documentation             |
| [Drizzle ORM Documentation](https://orm.drizzle.team/docs/overview)                                                        | Official Drizzle docs         |
| [OWASP SQL Injection Prevention](https://cheatsheetseries.owasp.org/cheatsheets/SQL_Injection_Prevention_Cheat_Sheet.html) | Security best practices       |

---

## Topic Coverage Matrix

### Schema Patterns → Quiz Questions

| Topic         | Concepts                                           | Quiz Questions |
| ------------- | -------------------------------------------------- | -------------- |
| Soft Delete   | deleted_at, partial indexes, unique constraints    | 1, 2           |
| Versioning    | Optimistic locking, version column, history tables | 3, 6           |
| Checksums     | Data integrity, pgcrypto                           | 5              |
| Audit Trails  | Audit columns, trigger-based logging               | 8, 90          |
| Anti-Patterns | EAV, CSV columns, polymorphic associations         | 4, 7           |

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

## Key Terminology

| Term                | Definition                                                       |
| ------------------- | ---------------------------------------------------------------- |
| Soft Delete         | Marking records as deleted instead of removing them              |
| Optimistic Locking  | Using version numbers to detect concurrent modifications         |
| TCL                 | Transaction Control Language (BEGIN, COMMIT, ROLLBACK)           |
| DCL                 | Data Control Language (GRANT, REVOKE)                            |
| ACID                | Atomicity, Consistency, Isolation, Durability                    |
| Isolation Level     | How much transactions can see of concurrent changes              |
| Dirty Read          | Reading uncommitted data from another transaction                |
| Phantom Read        | New rows appearing in repeated queries                           |
| RLS                 | Row-Level Security - controlling access to individual rows       |
| Sharding            | Distributing data across multiple database servers               |
| Partitioning        | Splitting a table into smaller physical pieces                   |
| Read Replica        | A database copy that handles read queries                        |
| ORM                 | Object Relational Mapping - database abstraction layer           |
| SQL Injection       | Attack that inserts malicious SQL via user input                 |
| Parameterized Query | Query where user input is passed as parameters, not concatenated |

---

## Common Mistakes to Avoid

1. **Using string concatenation in SQL** - Always use parameterized queries
2. **Forgetting to handle soft-deleted records in queries** - Add WHERE deleted_at IS NULL
3. **Using CASCADE without understanding implications** - Can delete more than expected
4. **Choosing wrong isolation level** - Read Committed is usually sufficient
5. **Granting ALL PRIVILEGES** - Violates least privilege principle
6. **Ignoring unique constraints with soft delete** - Need partial indexes
7. **Not using transactions for multi-step operations** - Risk inconsistent state
8. **Returning NEW in AFTER triggers** - Only BEFORE triggers use return values meaningfully
