# SQL Fundamentals

Week 02: DDL, DML, DQL, Constraints, Idempotency & DBML

---

## Roadmap

- DDL: Data Definition Language
- DML: Data Manipulation Language
- DQL: Data Query Language
- Constraints
- Idempotency
- DBML: Database Markup Language

---

## Week 02 Quizzes

| Quiz                     | Questions | Format          |
| ------------------------ | --------- | --------------- |
| Idempotency              | 24        | Categorization  |
| Idempotency Fix          | 10        | Multiple Choice |
| DDL/DML/DQL              | 10        | Translation     |
| Constraints & Data Types | 25        | T/F + MC        |

---

## Study Tips

1. Start with DQL (SELECT helps understand INSERT...SELECT)
2. Practice CREATE TABLE syntax
3. Focus on ON CONFLICT and absolute vs relative values
4. Know DECIMAL vs FLOAT for money
5. Understand DEFAULT + NOT NULL interactions

---

# DDL

Data Definition Language

---

## What is DDL?

DDL defines and manages database structure:

- Databases
- Schemas
- Tables
- Constraints

[![DDL meme](https://i.imgflip.com/2/1bij.jpg)](https://imgflip.com/i/1bij)

---

## Core DDL Statements

| Statement  | Purpose                            |
| ---------- | ---------------------------------- |
| `CREATE`   | Creates a new database object      |
| `ALTER`    | Modifies an existing object        |
| `DROP`     | Deletes an object                  |
| `TRUNCATE` | Removes all data (keeps structure) |

---

## CREATE DATABASE & SCHEMA

```sql
-- Create a database
CREATE DATABASE ecommerce;

-- Create a schema (logical container)
CREATE SCHEMA inventory;
```

---

## CREATE TABLE

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

---

## Common Data Types

| Category  | Types                                             |
| --------- | ------------------------------------------------- |
| Numeric   | `INTEGER`, `BIGINT`, `SERIAL`, `DECIMAL`, `FLOAT` |
| Text      | `CHAR(n)`, `VARCHAR(n)`, `TEXT`                   |
| Boolean   | `BOOLEAN`                                         |
| Date/Time | `DATE`, `TIME`, `TIMESTAMP`, `INTERVAL`           |
| Special   | `UUID`, `JSON`, `JSONB`, `BYTEA`                  |

---

## ID Strategies

| Type        | Pros            | Cons                                  |
| ----------- | --------------- | ------------------------------------- |
| `SERIAL`    | Simple, compact | Predictable, not distributed-friendly |
| `BIGSERIAL` | Larger range    | Same as SERIAL                        |
| `UUID`      | Globally unique | Larger storage, less readable         |
| `IDENTITY`  | SQL standard    | PostgreSQL 10+ only                   |

---

## ID Examples

```sql
-- SERIAL (legacy)
CREATE TABLE orders_v1 (id SERIAL PRIMARY KEY);

-- IDENTITY (modern standard)
CREATE TABLE orders_v2 (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY
);

-- UUID
CREATE TABLE orders_v3 (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid()
);
```

---

## ALTER TABLE

```sql
-- Add a column
ALTER TABLE users ADD COLUMN phone VARCHAR(20);

-- Drop a column
ALTER TABLE users DROP COLUMN phone;

-- Rename a column
ALTER TABLE users RENAME COLUMN username TO user_name;

-- Change data type
ALTER TABLE users ALTER COLUMN email TYPE VARCHAR(150);

-- Add/Drop constraint
ALTER TABLE users ADD CONSTRAINT email_unique UNIQUE (email);
ALTER TABLE users DROP CONSTRAINT email_unique;
```

---

## DROP

[![Drop Table](https://dataedo-website.s3.amazonaws.com/cartoon/drop_table_or_column.png?1686657380)](https://dataedo.com)

⚠️ DROP is destructive! Always backup first!

---

## DROP Examples

```sql
-- Drop a table
DROP TABLE users;

-- Drop only if exists (prevents error)
DROP TABLE IF EXISTS users;

-- Drop with cascade (removes dependent objects)
DROP TABLE users CASCADE;

-- Drop a database
DROP DATABASE ecommerce;
```

---

## TRUNCATE

```sql
-- Remove all data
TRUNCATE TABLE logs;

-- Reset identity/serial counter
TRUNCATE TABLE orders RESTART IDENTITY;

-- Cascade to dependent tables
TRUNCATE TABLE users CASCADE;
```

---

## DDL is NOT Idempotent

```sql
CREATE TABLE users (...);  -- ✅ Works
CREATE TABLE users (...);  -- ❌ Error: table already exists
```

Make it safe:

```sql
CREATE TABLE IF NOT EXISTS users (...);  -- ✅ Safe
DROP TABLE IF EXISTS users;              -- ✅ Safe
```

---

# DML

Data Manipulation Language

---

## What is DML?

DML manipulates data within tables:

- `INSERT` - Add new rows
- `UPDATE` - Modify existing rows
- `DELETE` - Remove rows

---

## INSERT: Basic

```sql
-- Single row
INSERT INTO users (username, email, created_at)
VALUES ('johndoe', 'john@example.com', CURRENT_TIMESTAMP);

-- Multiple rows
INSERT INTO products (name, price, category)
VALUES
    ('Laptop', 999.99, 'Electronics'),
    ('Mouse', 29.99, 'Electronics'),
    ('Keyboard', 79.99, 'Electronics');
```

---

## INSERT with RETURNING

```sql
-- Return the generated ID
INSERT INTO users (username, email)
VALUES ('janedoe', 'jane@example.com')
RETURNING id;

-- Return multiple columns
INSERT INTO orders (customer_id, total)
VALUES (1, 150.00)
RETURNING id, created_at;

-- Return entire row
INSERT INTO products (name, price)
VALUES ('Tablet', 499.99)
RETURNING *;
```

---

## INSERT from SELECT

```sql
-- Copy data between tables
INSERT INTO archived_users (id, username, email)
SELECT id, username, email
FROM users
WHERE is_active = false;

-- Insert with transformation
INSERT INTO order_summary (order_id, item_count, total)
SELECT order_id, COUNT(*), SUM(price * quantity)
FROM order_items
GROUP BY order_id;
```

---

## UPDATE: Basic

```sql
-- Update single row
UPDATE users
SET email = 'newemail@example.com'
WHERE id = 1;

-- Update multiple columns
UPDATE products
SET price = 89.99, updated_at = CURRENT_TIMESTAMP
WHERE id = 42;
```

⚠️ Always include WHERE unless updating ALL rows!

---

## UPDATE with Expressions

```sql
-- Increase price by 10%
UPDATE products
SET price = price * 1.10
WHERE category = 'Electronics';

-- Conditional update with CASE
UPDATE orders
SET status = CASE
    WHEN shipped_at IS NOT NULL THEN 'shipped'
    WHEN paid_at IS NOT NULL THEN 'paid'
    ELSE 'pending'
END;
```

---

## UPDATE from Another Table

```sql
UPDATE products p
SET category_name = c.name
FROM categories c
WHERE p.category_id = c.id;
```

---

## DELETE

[![DELETE without WHERE](https://preview.redd.it/ysc1br9icup71.jpg?width=640&crop=smart&auto=webp&s=33f4ff68a749128e3d4b9a5c252d7b511eab545b)](https://reddit.com)

---

## DELETE Examples

```sql
-- Delete specific row
DELETE FROM users WHERE id = 1;

-- Delete with condition
DELETE FROM sessions WHERE expires_at < CURRENT_TIMESTAMP;

-- Delete with RETURNING
DELETE FROM expired_tokens
WHERE created_at < NOW() - INTERVAL '30 days'
RETURNING id, user_id;
```

⚠️ Always include WHERE unless deleting ALL rows!

---

## DELETE vs TRUNCATE

| Feature       | DELETE   | TRUNCATE        |
| ------------- | -------- | --------------- |
| WHERE clause  | ✅ Yes   | ❌ No           |
| RETURNING     | ✅ Yes   | ❌ No           |
| Triggers      | ✅ Fires | ❌ Doesn't fire |
| Speed         | Slower   | Faster          |
| Resets SERIAL | ❌ No    | ✅ Optional     |

---

# DQL

Data Query Language

---

## SELECT Statement

```sql
SELECT column1, column2, ... -- Specify columns
FROM table_name -- Source table
WHERE condition -- filtering
ORDER BY column -- Sorting
LIMIT n -- how many rows we want
OFFSET m; -- skip rows for pagination
```

The foundation of data retrieval in SQL.

---

## SELECT Columns

```sql
-- Specific columns
SELECT name, email FROM users;

-- All columns (avoid in production!)
SELECT * FROM users;

-- With alias
SELECT name AS user_name, email AS user_email FROM users;

-- With expressions
SELECT name, price, price * 0.9 AS discounted_price FROM products;
```

---

## WHERE: Comparison Operators

| Operator     | Description                |
| ------------ | -------------------------- |
| `=`          | Equal to                   |
| `<>` or `!=` | Not equal to               |
| `<`, `>`     | Less/Greater than          |
| `<=`, `>=`   | Less/Greater than or equal |

```sql
SELECT * FROM products WHERE price > 100;
SELECT * FROM users WHERE status <> 'inactive';
```

---

## WHERE: Logical Operators

| Operator | Description                         |
| -------- | ----------------------------------- |
| `AND`    | Both conditions must be true        |
| `OR`     | At least one condition must be true |
| `NOT`    | Negates the condition               |

```sql
SELECT * FROM products
WHERE (category = 'Electronics' OR category = 'Computers')
  AND price < 1000;
```

---

## WHERE: Special Operators

```sql
-- IN
SELECT * FROM products
WHERE category IN ('Electronics', 'Computers', 'Phones');

-- BETWEEN (inclusive)
SELECT * FROM orders
WHERE order_date BETWEEN '2024-01-01' AND '2024-12-31';

-- LIKE (% = any chars, _ = single char)
SELECT * FROM users WHERE email LIKE '%@gmail.com';

-- IS NULL / IS NOT NULL
SELECT * FROM users WHERE deleted_at IS NULL;
```

---

## ORDER BY

```sql
-- Ascending (default)
SELECT * FROM products ORDER BY price ASC;

-- Descending
SELECT * FROM products ORDER BY price DESC;

-- Multiple columns
SELECT * FROM products ORDER BY category ASC, price DESC;

-- By expression
SELECT name, price * quantity AS total
FROM order_items
ORDER BY total DESC;
```

---

## LIMIT and OFFSET

```sql
-- First 10 rows
SELECT * FROM products LIMIT 10;

-- Skip first 20, get next 10 (pagination)
SELECT * FROM products LIMIT 10 OFFSET 20;
```

### Pagination Formula

```sql
-- Page N: OFFSET = (page - 1) * limit
SELECT * FROM products ORDER BY id LIMIT 10 OFFSET ((N - 1) * 10);
```

---

## DISTINCT

```sql
-- Unique categories
SELECT DISTINCT category FROM products;

-- Unique combinations
SELECT DISTINCT category, brand FROM products;
```

---

## Query Execution Order

1. `FROM` - Source tables
2. `WHERE` - Row filtering
3. `GROUP BY` - Grouping
4. `HAVING` - Group filtering
5. `SELECT` - Column selection
6. `DISTINCT` - Duplicate removal
7. `ORDER BY` - Sorting
8. `LIMIT/OFFSET` - Row limiting

---

## Alias Limitation

```sql
-- ❌ Error: alias not available in WHERE
SELECT price * 0.9 AS discounted
FROM products
WHERE discounted < 50;

-- ✅ Repeat the expression
SELECT price * 0.9 AS discounted
FROM products
WHERE price * 0.9 < 50;
```

---

# Constraints

---

## Why Constraints Matter

Without constraints:

- Duplicate records can exist
- Required fields can be empty
- Orphaned records can reference non-existent data
- Invalid data corrupts business logic

**Constraints = last line of defense for data quality**

---

## Constraint Overview

| Constraint    | Purpose                          |
| ------------- | -------------------------------- |
| `PRIMARY KEY` | Uniquely identifies each row     |
| `FOREIGN KEY` | Enforces relationships           |
| `NOT NULL`    | Prevents NULL values             |
| `UNIQUE`      | Ensures all values are different |
| `CHECK`       | Validates against a condition    |
| `DEFAULT`     | Sets value when none provided    |

---

## PRIMARY KEY

[![Database Keys](https://dataedo-website.s3.amazonaws.com/cartoon/database_keys.png?1686657387)](https://dataedo.com)

---

## PRIMARY KEY Examples

```sql
-- Single column
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL
);

-- Composite primary key
CREATE TABLE order_items (
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    PRIMARY KEY (order_id, product_id)
);
```

---

## FOREIGN KEY

```sql
-- Create parent table first
CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL
);

-- Create child table with foreign key
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    category_id INTEGER REFERENCES categories(id)
);
```

---

## Referential Actions

| Action        | ON DELETE         | ON UPDATE         |
| ------------- | ----------------- | ----------------- |
| `NO ACTION`   | Error (default)   | Error             |
| `RESTRICT`    | Same as NO ACTION | Same              |
| `CASCADE`     | Delete child rows | Update FK values  |
| `SET NULL`    | Set FK to NULL    | Set FK to NULL    |
| `SET DEFAULT` | Set FK to default | Set FK to default |

---

## Referential Actions Example

```sql
-- CASCADE: deleting category deletes products
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    category_id INTEGER REFERENCES categories(id)
        ON DELETE CASCADE ON UPDATE CASCADE
);

-- SET NULL: deleting user nullifies orders
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id)
        ON DELETE SET NULL
);
```

⚠️ Be careful with CASCADE!

---

## Self-Referencing FK

```sql
-- Employees with managers
CREATE TABLE employees (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    manager_id INTEGER REFERENCES employees(id)
);

-- Categories with subcategories
CREATE TABLE categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL,
    parent_id INTEGER REFERENCES categories(id)
);
```

---

## NOT NULL

[![NULL vs Empty String](https://preview.redd.it/we-support-all-3-database-types-and-this-constantly-is-an-v0-f2hv3twxwb1a1.jpg?width=320&crop=smart&auto=webp&s=80db294ca98006b9b582544dca14177df54a6fd6)](https://reddit.com)

---

## NOT NULL vs Empty String

```sql
''     -- Empty string (a value)
NULL   -- Absence of value (unknown)

-- NOT NULL allows empty strings!
INSERT INTO users (email) VALUES ('');  -- ✅ Works

-- Prevent empty strings with CHECK:
email VARCHAR(100) NOT NULL CHECK (email <> '')
```

---

## UNIQUE

```sql
-- Single column unique
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(100) UNIQUE NOT NULL
);

-- Composite unique
CREATE TABLE enrollments (
    id SERIAL PRIMARY KEY,
    student_id INTEGER NOT NULL,
    course_id INTEGER NOT NULL,
    UNIQUE (student_id, course_id)
);
```

Note: UNIQUE allows one NULL (use `UNIQUE NULLS NOT DISTINCT` in PG 15+)

---

## CHECK

```sql
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    price DECIMAL(10,2) CHECK (price >= 0),
    discount DECIMAL(3,2) CHECK (discount >= 0 AND discount <= 1)
);

-- Multi-column CHECK
CREATE TABLE events (
    id SERIAL PRIMARY KEY,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    CHECK (end_date >= start_date)
);
```

---

## Common CHECK Patterns

```sql
-- Positive numbers
CHECK (amount > 0)

-- Range
CHECK (rating >= 1 AND rating <= 5)

-- Enum-like values
CHECK (status IN ('pending', 'active', 'completed'))

-- String length
CHECK (LENGTH(code) = 6)

-- Email format (basic)
CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')
```

---

## DEFAULT

```sql
CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    status VARCHAR(20) DEFAULT 'draft',
    view_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_published BOOLEAN DEFAULT false
);

-- UUID default
CREATE TABLE sessions (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id INTEGER NOT NULL
);
```

---

## Constraint Naming Conventions

| Constraint  | Convention            | Example                       |
| ----------- | --------------------- | ----------------------------- |
| PRIMARY KEY | `pk_<table>`          | `pk_users`                    |
| FOREIGN KEY | `fk_<table>_<column>` | `fk_orders_user_id`           |
| UNIQUE      | `uq_<table>_<column>` | `uq_users_email`              |
| CHECK       | `chk_<table>_<desc>`  | `chk_products_price_positive` |

---

# Idempotency

---

## What is Idempotency?

An operation is **idempotent** if performing it multiple times produces the same result as performing it once.

$$f(f(x)) = f(x)$$

---

## Why Idempotency Matters

Operations can fail and need retries:

- Network timeouts
- Server crashes mid-operation
- Users clicking twice
- Message queue redelivery

If idempotent → **safe retries**

If not → duplicates, wrong counts, overcharges

---

## DDL Idempotency

| Operation                    | Idempotent? |
| ---------------------------- | ----------- |
| `CREATE TABLE`               | ❌ No       |
| `CREATE TABLE IF NOT EXISTS` | ✅ Yes      |
| `DROP TABLE`                 | ❌ No       |
| `DROP TABLE IF EXISTS`       | ✅ Yes      |
| `ALTER TABLE ADD COLUMN`     | ❌ No       |

---

## DQL Idempotency

All SELECT queries are naturally **idempotent**:

```sql
-- ✅ Always idempotent (read-only)
SELECT * FROM users WHERE id = 1;
SELECT COUNT(*) FROM orders;
```

---

## DML Idempotency

| Operation                           | Idempotent? |
| ----------------------------------- | ----------- |
| `INSERT`                            | ❌ No       |
| `INSERT ... ON CONFLICT DO NOTHING` | ✅ Yes      |
| `INSERT ... ON CONFLICT DO UPDATE`  | ✅ Yes      |
| `UPDATE SET x = value`              | ✅ Yes      |
| `UPDATE SET x = x + 1`              | ❌ No       |
| `DELETE WHERE ...`                  | ✅ Yes      |

---

## INSERT: The Problem

```sql
-- ❌ Running twice creates TWO rows
INSERT INTO logs (message, created_at)
VALUES ('User logged in', NOW());

INSERT INTO logs (message, created_at)
VALUES ('User logged in', NOW());

-- Result: 2 duplicate log entries!
```

---

## Solution: UPSERT

```sql
-- DO NOTHING if exists
INSERT INTO users (email, name)
VALUES ('john@example.com', 'John Doe')
ON CONFLICT (email) DO NOTHING;

-- DO UPDATE if exists
INSERT INTO products (sku, name, price)
VALUES ('WIDGET-001', 'Super Widget', 29.99)
ON CONFLICT (sku) DO UPDATE SET
    name = EXCLUDED.name,
    price = EXCLUDED.price;
```

`EXCLUDED` = the row that was proposed for insertion

---

## Idempotency Keys

For operations without natural unique keys:

```sql
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key UUID UNIQUE NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL
);

INSERT INTO payments (idempotency_key, amount, status)
VALUES ('550e8400-e29b-41d4-a716-446655440000', 99.99, 'pending')
ON CONFLICT (idempotency_key) DO NOTHING;
```

---

## UPDATE: Idempotent vs Not

```sql
-- ✅ Idempotent: absolute value
UPDATE users SET status = 'active' WHERE id = 1;
UPDATE users SET status = 'active' WHERE id = 1;
-- Result: status = 'active' (same)

-- ❌ Non-idempotent: relative value
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
-- Result: view_count increased by 2!
```

---

## DELETE: Naturally Idempotent

```sql
-- ✅ Second delete finds nothing
DELETE FROM sessions WHERE user_id = 1;
DELETE FROM sessions WHERE user_id = 1;
-- Result: all sessions deleted (same result)
```

---

## Real-World Patterns

1. **API Idempotency Keys** - Client sends unique key per request
2. **Event Processing** - Track processed event IDs
3. **State Machine Updates** - Gate on current state

```sql
-- Only transition from 'pending' to 'completed'
UPDATE orders
SET status = 'completed'
WHERE id = 1 AND status = 'pending';
-- Running twice: second run affects 0 rows ✅
```

---

## Idempotency Summary

| Make it Idempotent | How                                 |
| ------------------ | ----------------------------------- |
| CREATE/DROP        | Add `IF [NOT] EXISTS`               |
| INSERT             | Use `ON CONFLICT DO NOTHING/UPDATE` |
| UPDATE (increment) | Track processed operations          |
| UPDATE (set value) | Already idempotent!                 |
| DELETE             | Already idempotent!                 |

---

# DBML

Database Markup Language

---

## Why DBML?

| Challenge with DDL       | DBML Solution                  |
| ------------------------ | ------------------------------ |
| Verbose syntax           | Concise, readable              |
| No visual representation | Integrates with diagram tools  |
| Database-specific        | Generates SQL for multiple DBs |
| Hard to collaborate      | Human-readable, git-friendly   |

---

## DBML vs SQL DDL

**SQL:**

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**DBML:**

```dbml
Table users {
  id serial [pk]
  username varchar(50) [not null, unique]
  email varchar(255) [not null]
  created_at timestamp [default: `now()`]
}
```

---

## DBML Column Constraints

```dbml
Table users {
  id serial [pk]                        // Primary key
  email varchar(255) [unique, not null] // Multiple constraints
  role varchar(20) [default: 'user']    // Default value
  age integer [note: 'Must be 18+']     // Documentation
}
```

---

## DBML Relationships

| Symbol | Meaning      | Relationship               |
| ------ | ------------ | -------------------------- |
| `>`    | Many-to-One  | Many of this → One of that |
| `<`    | One-to-Many  | One of this → Many of that |
| `-`    | One-to-One   | One of this → One of that  |
| `<>`   | Many-to-Many | Many ↔ Many                |

---

## DBML Relationship Examples

```dbml
// Many-to-One (inline)
Table posts {
  id serial [pk]
  author_id integer [ref: > users.id]
}

// Standalone reference
Ref: posts.author_id > users.id

// With referential actions
Ref: posts.author_id > users.id [delete: cascade]
```

---

## DBML Indexes & Enums

```dbml
Enum order_status {
  pending
  processing
  shipped
  delivered
}

Table orders {
  id serial [pk]
  status order_status [default: 'pending']

  indexes {
    status
    (user_id, status) [unique]
  }
}
```

---

## DBML Tools

- **dbdiagram.io** - Free online editor with visualization
- **@dbml/cli** - Convert DBML ↔ SQL
- **VS Code Extension** - Syntax highlighting & preview

```bash
npm install -g @dbml/cli
dbml2sql schema.dbml --postgres -o schema.sql
sql2dbml schema.sql --postgres -o schema.dbml
```

---

## DBML Workflow

1. **Design** - Write DBML, visualize in dbdiagram.io
2. **Review** - Share with team, iterate
3. **Implement** - Export to SQL, apply migrations
4. **Document** - Keep DBML in version control

---

# Quiz Prep

---

## Key Concepts to Review

- DDL: CREATE, ALTER, DROP, TRUNCATE + IF [NOT] EXISTS
- DML: INSERT, UPDATE, DELETE + RETURNING + UPSERT
- DQL: SELECT, WHERE, ORDER BY, LIMIT/OFFSET
- Constraints: PK, FK, NOT NULL, UNIQUE, CHECK, DEFAULT
- Idempotency: Absolute vs relative, ON CONFLICT patterns
- Data types: DECIMAL for money, TIMESTAMPTZ for global time

---

## Common Pitfalls

- UPDATE/DELETE without WHERE
- SELECT \* in production
- Foreign keys without indexes
- Relative updates (x = x + 1) assuming idempotency
- Using FLOAT for money (use DECIMAL!)
- NOW() in non-idempotent contexts

---

## Practice Exercises

1. Create an e-commerce schema with proper constraints
2. Write idempotent UPSERT for user preferences
3. Classify SQL statements as idempotent/non-idempotent
4. Convert requirements to SQL (and vice versa)
5. Design DBML for a library system

---

# Questions?

Good luck on the quizzes! 🎯
