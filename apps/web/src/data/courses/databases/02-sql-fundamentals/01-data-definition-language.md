# DDL - Data Definition Language

DDL (Data Definition Language) is the subset of SQL used to define and manage the structure of database objects such as databases, schemas, tables, and constraints.

[![DDL meme](https://i.imgflip.com/2/1bij.jpg)](https://imgflip.com/i/1bij)

## Core DDL Statements

| Statement | Purpose |
|-----------|---------|
| `CREATE`  | Creates a new database object |
| `ALTER`   | Modifies an existing object |
| `DROP`    | Deletes an object |
| `TRUNCATE`| Removes all data from a table (but keeps structure) |

## CREATE DATABASE

Creates a new database instance.

::: example "Create a database"

```sql
CREATE DATABASE ecommerce;
```

:::

## CREATE SCHEMA

Schemas are logical containers within a database to organize objects (tables, views, etc.).

::: example "Create a schema"

```sql
CREATE SCHEMA inventory;
```

:::

## CREATE TABLE

Defines a new table with its columns and constraints.

::: example "Create a table"

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

:::

### Data Types

Common PostgreSQL data types:

| Category | Types |
|----------|-------|
| Numeric | `INTEGER`, `BIGINT`, `SERIAL`, `DECIMAL`, `NUMERIC`, `FLOAT`, `REAL` |
| Text | `CHAR(n)`, `VARCHAR(n)`, `TEXT` |
| Boolean | `BOOLEAN` |
| Date/Time | `DATE`, `TIME`, `TIMESTAMP`, `INTERVAL` |
| Binary | `BYTEA` |
| JSON | `JSON`, `JSONB` |
| UUID | `UUID` |

### Constraints

Constraints enforce rules on data to maintain integrity:

| Constraint | Description |
|------------|-------------|
| `PRIMARY KEY` | Uniquely identifies each row |
| `FOREIGN KEY` | References a primary key in another table |
| `NOT NULL` | Column cannot contain NULL values |
| `UNIQUE` | All values in column must be different |
| `CHECK` | Values must satisfy a condition |
| `DEFAULT` | Sets a default value if none provided |

::: example "Table with constraints"

```sql
CREATE TABLE products (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    price DECIMAL(10, 2) NOT NULL CHECK (price >= 0),
    category_id INTEGER REFERENCES categories(id),
    sku VARCHAR(50) UNIQUE,
    is_active BOOLEAN DEFAULT true
);
```

:::

### ID Types

Different strategies for primary key generation:

| Type | Description | Pros | Cons |
|------|-------------|------|------|
| `SERIAL` | Auto-incrementing integer | Simple, compact | Predictable, not distributed-friendly |
| `BIGSERIAL` | Auto-incrementing big integer | Larger range | Same as SERIAL |
| `UUID` | Universally unique identifier | Globally unique, distributed-friendly | Larger storage, less readable |
| `IDENTITY` | SQL standard auto-increment | Standard compliant | PostgreSQL 10+ only |

::: example "Different ID strategies"

```sql
-- SERIAL (legacy)
CREATE TABLE orders_v1 (
    id SERIAL PRIMARY KEY
);

-- IDENTITY (modern standard)
CREATE TABLE orders_v2 (
    id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY
);

-- UUID
CREATE TABLE orders_v3 (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid()
);
```

:::

## ALTER TABLE

Modifies an existing table structure.

::: example "Alter table examples"

```sql
-- Add a column
ALTER TABLE users ADD COLUMN phone VARCHAR(20);

-- Drop a column
ALTER TABLE users DROP COLUMN phone;

-- Rename a column
ALTER TABLE users RENAME COLUMN username TO user_name;

-- Change data type
ALTER TABLE users ALTER COLUMN email TYPE VARCHAR(150);

-- Add a constraint
ALTER TABLE users ADD CONSTRAINT email_unique UNIQUE (email);

-- Drop a constraint
ALTER TABLE users DROP CONSTRAINT email_unique;
```

:::

## DROP

[![Drop Table](https://dataedo-website.s3.amazonaws.com/cartoon/drop_table_or_column.png?1686657380)](https://dataedo.com)

Removes database objects permanently.

::: warning

DROP is a destructive operation. Always backup your data before dropping objects in production!

:::

::: example "Drop examples"

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

:::

## TRUNCATE

Removes all rows from a table but keeps the table structure.

::: example "Truncate"

```sql
-- Remove all data
TRUNCATE TABLE logs;

-- Reset identity/serial counter
TRUNCATE TABLE orders RESTART IDENTITY;

-- Cascade to dependent tables
TRUNCATE TABLE users CASCADE;
```

:::

## DDL is NOT Idempotent

Most DDL operations are **not idempotent** - running them twice will cause an error:

```sql
CREATE TABLE users (...);  -- ✅ Works
CREATE TABLE users (...);  -- ❌ Error: table already exists
```

Use `IF EXISTS` / `IF NOT EXISTS` to make them safer:

```sql
CREATE TABLE IF NOT EXISTS users (...);  -- ✅ Safe
DROP TABLE IF EXISTS users;              -- ✅ Safe
```

## Practice

Try creating a schema for an e-commerce system with these tables:
- `customers` (id, name, email, created_at)
- `products` (id, name, description, price, stock)
- `orders` (id, customer_id, order_date, status)
- `order_items` (id, order_id, product_id, quantity, unit_price)

Think about:
- Which ID strategy to use?
- What constraints are needed?
- What are the relationships between tables?
