# Week 02 Readings & Quiz Coverage

## Required Readings

Complete the following readings before attempting the Week 02 quizzes:

1. [data-definition-language](./sql-fundamentals/data-definition-language) - DDL statements (CREATE, ALTER, DROP, TRUNCATE)
2. [data-manipulation-language](./sql-fundamentals/data-manipulation-language) - DML statements (INSERT, UPDATE, DELETE)
3. [data-query-language](./sql-fundamentals/data-query-language) - DQL statements (SELECT, FROM, WHERE, ORDER BY, LIMIT)
4. [constraints](./sql-fundamentals/constraints) - Database constraints (PK, FK, NOT NULL, UNIQUE, CHECK, DEFAULT)
5. [idempotency](./sql-fundamentals/idempotency) - Understanding and designing idempotent database operations
6. [dbml-introduction](./sql-fundamentals/dbml-introduction) - Database Markup Language for schema design

---

## Week 02 Quizzes Overview

| Quiz                                                                           | Questions | Format          | Topics Covered                                            |
| ------------------------------------------------------------------------------ | --------- | --------------- | --------------------------------------------------------- |
| [idempotency](./sql-fundamentals/quizzes/idempotency-quiz)                     | 24        | Categorization  | SQL statement classification as idempotent/non-idempotent |
| [idempotency-fix](./sql-fundamentals/quizzes/idempotency-fix-quiz)             | 10        | Multiple Choice | Making non-idempotent operations idempotent               |
| [ddl-dml-dql](./sql-fundamentals/quizzes/ddl-dml-dql-quiz)                     | 10        | Translation     | Converting between requirements and SQL                   |
| [constraints-datatypes](./sql-fundamentals/quizzes/constraints-datatypes-quiz) | 25        | T/F + MC        | Constraints and data types knowledge                      |

---

## Study Tips

1. **Start with DQL** - Understanding SELECT helps with reading INSERT...SELECT and UPDATE...FROM patterns
2. **Practice DDL syntax** - CREATE TABLE syntax is foundational for all other operations
3. **Focus on idempotency patterns** - The ON CONFLICT clause and absolute vs relative values are key concepts
4. **Know your data types** - Especially DECIMAL vs FLOAT for money, and TIMESTAMPTZ for global apps
5. **Understand constraint interactions** - How DEFAULT + NOT NULL work together, why PK implies NOT NULL
