# SQL Constraints

Constraints are rules enforced by the database to maintain data integrity. They prevent invalid data from being inserted and ensure relationships between tables remain consistent.

[![Constraints meme](https://programmerhumor.io/wp-content/uploads/2023/01/programmerhumor-io-databases-memes-backend-memes-4c2b8ee0f5a98a0.png)](https://programmerhumor.io)

## Why Constraints Matter

Without constraints:
- Duplicate records can exist where they shouldn't
- Required fields can be left empty
- Orphaned records can reference non-existent data
- Invalid data can corrupt your business logic

Constraints act as the **last line of defense** for data quality.

## Overview of Constraints

| Constraint | Purpose |
|------------|---------|
| `PRIMARY KEY` | Uniquely identifies each row |
| `FOREIGN KEY` | Enforces relationships between tables |
| `NOT NULL` | Prevents NULL values |
| `UNIQUE` | Ensures all values are different |
| `CHECK` | Validates values against a condition |
| `DEFAULT` | Sets a value when none is provided |

## PRIMARY KEY (PK)

[![Database Keys](https://dataedo-website.s3.amazonaws.com/cartoon/database_keys.png?1686657387)](https://dataedo.com)

A primary key uniquely identifies each row in a table. It combines:
- `NOT NULL` — cannot be empty
- `UNIQUE` — no duplicates allowed

Each table should have exactly one primary key.

::: example "Primary key examples"

```sql
-- Single column primary key
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL
);

-- Explicit constraint syntax
CREATE TABLE products (
    id UUID NOT NULL,
    name VARCHAR(100) NOT NULL,
    CONSTRAINT pk_products PRIMARY KEY (id)
);

-- Composite primary key (multiple columns)
CREATE TABLE order_items (
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    PRIMARY KEY (order_id, product_id)
);
```

:::

### Primary Key Strategies

| Strategy | Example | Pros | Cons |
|----------|---------|------|------|
| `SERIAL` | 1, 2, 3, ... | Simple, compact, fast | Predictable, not distributed-friendly |
| `UUID` | `550e8400-e29b-...` | Globally unique, secure | Larger storage, slower indexing |
| `IDENTITY` | 1, 2, 3, ... | SQL standard | PostgreSQL 10+ only |
| Natural Key | email, SSN | Meaningful | Can change, privacy concerns |

::: note

Prefer **surrogate keys** (auto-generated IDs) over **natural keys** (business data like email). Natural keys can change and cause cascading updates.

:::

## FOREIGN KEY (FK)

A foreign key creates a link between two tables, ensuring **referential integrity**. It references a primary key (or unique column) in another table.

::: example "Foreign key examples"

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

-- Explicit constraint syntax with name
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INTEGER NOT NULL,
    CONSTRAINT fk_orders_customer 
        FOREIGN KEY (customer_id) REFERENCES customers(id)
);
```

:::

### Referential Actions

What happens when the referenced row is deleted or updated?

| Action | ON DELETE | ON UPDATE |
|--------|-----------|-----------|
| `NO ACTION` | Error if referenced (default) | Error if referenced |
| `RESTRICT` | Same as NO ACTION | Same as NO ACTION |
| `CASCADE` | Delete child rows | Update child FK values |
| `SET NULL` | Set FK to NULL | Set FK to NULL |
| `SET DEFAULT` | Set FK to default value | Set FK to default value |

::: example "Referential actions"

```sql
-- CASCADE: deleting a category deletes all its products
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    category_id INTEGER REFERENCES categories(id) 
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- SET NULL: deleting a user sets their orders' user_id to NULL
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER REFERENCES users(id) 
        ON DELETE SET NULL
);

-- RESTRICT: prevent deleting a category that has products
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    category_id INTEGER REFERENCES categories(id) 
        ON DELETE RESTRICT
);
```

:::

::: warning

Be careful with `CASCADE`! Deleting one row could delete thousands of related rows.

:::

### Self-Referencing Foreign Keys

A table can reference itself:

```sql
-- Employees with managers (who are also employees)
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

## NOT NULL

Prevents a column from accepting NULL values.

::: example "NOT NULL constraint"

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(100) NOT NULL,      -- Required
    phone VARCHAR(20),                 -- Optional (allows NULL)
    created_at TIMESTAMP NOT NULL DEFAULT NOW()
);

-- This will fail:
INSERT INTO users (id) VALUES (1);
-- ERROR: null value in column "email" violates not-null constraint
```

:::

### NULL vs Empty String

```sql
-- These are different!
''     -- Empty string (a value)
NULL   -- Absence of value (unknown)

-- NOT NULL allows empty strings
INSERT INTO users (email) VALUES ('');  -- ✅ Works (but probably not what you want)
```

To prevent empty strings, combine with CHECK:

```sql
email VARCHAR(100) NOT NULL CHECK (email <> '')
```

## UNIQUE

Ensures all values in a column (or combination of columns) are different.

::: example "UNIQUE constraint"

```sql
-- Single column unique
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    email VARCHAR(100) UNIQUE NOT NULL,
    username VARCHAR(50) UNIQUE NOT NULL
);

-- Composite unique (combination must be unique)
CREATE TABLE enrollments (
    id SERIAL PRIMARY KEY,
    student_id INTEGER NOT NULL,
    course_id INTEGER NOT NULL,
    UNIQUE (student_id, course_id)  -- Same student can't enroll twice in same course
);

-- Named unique constraint
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    sku VARCHAR(50) NOT NULL,
    CONSTRAINT uq_products_sku UNIQUE (sku)
);
```

:::

### UNIQUE vs PRIMARY KEY

| Feature | PRIMARY KEY | UNIQUE |
|---------|-------------|--------|
| NULL values | ❌ Not allowed | ✅ Allowed (one NULL) |
| Per table | One only | Multiple allowed |
| Creates index | ✅ Always | ✅ Always |
| Identifies row | ✅ Yes | Not necessarily |

::: note

In PostgreSQL, UNIQUE columns can contain one NULL value (since NULL ≠ NULL). Use `UNIQUE NULLS NOT DISTINCT` (PostgreSQL 15+) to treat NULLs as equal.

:::

## CHECK

Validates that values meet a specific condition.

::: example "CHECK constraint"

```sql
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    price DECIMAL(10,2) CHECK (price >= 0),
    quantity INTEGER CHECK (quantity >= 0),
    discount DECIMAL(3,2) CHECK (discount >= 0 AND discount <= 1)
);

-- Named CHECK constraint
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    age INTEGER,
    email VARCHAR(100),
    CONSTRAINT chk_users_age CHECK (age >= 0 AND age <= 150),
    CONSTRAINT chk_users_email CHECK (email LIKE '%@%.%')
);

-- CHECK with multiple columns
CREATE TABLE events (
    id SERIAL PRIMARY KEY,
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    CHECK (end_date >= start_date)
);
```

:::

### Common CHECK Patterns

```sql
-- Positive numbers
CHECK (amount > 0)

-- Non-negative
CHECK (quantity >= 0)

-- Range
CHECK (rating >= 1 AND rating <= 5)

-- Enum-like values
CHECK (status IN ('pending', 'active', 'completed', 'cancelled'))

-- String length
CHECK (LENGTH(code) = 6)

-- Email format (basic)
CHECK (email ~* '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$')

-- Date comparisons
CHECK (end_date > start_date)
```

## DEFAULT

Sets a value automatically when none is provided.

::: example "DEFAULT constraint"

```sql
CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    status VARCHAR(20) DEFAULT 'draft',
    view_count INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_published BOOLEAN DEFAULT false
);

-- Insert without specifying defaults
INSERT INTO posts (title) VALUES ('My First Post');
-- Result: status='draft', view_count=0, created_at=now(), is_published=false

-- UUID default
CREATE TABLE sessions (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id INTEGER NOT NULL
);
```

:::

## Adding and Removing Constraints

### Add Constraint to Existing Table

```sql
-- Add NOT NULL
ALTER TABLE users ALTER COLUMN email SET NOT NULL;

-- Add UNIQUE
ALTER TABLE users ADD CONSTRAINT uq_users_email UNIQUE (email);

-- Add CHECK
ALTER TABLE products ADD CONSTRAINT chk_price CHECK (price >= 0);

-- Add FOREIGN KEY
ALTER TABLE orders ADD CONSTRAINT fk_orders_user 
    FOREIGN KEY (user_id) REFERENCES users(id);

-- Add PRIMARY KEY
ALTER TABLE logs ADD CONSTRAINT pk_logs PRIMARY KEY (id);
```

### Remove Constraint

```sql
-- Drop NOT NULL
ALTER TABLE users ALTER COLUMN email DROP NOT NULL;

-- Drop named constraint
ALTER TABLE users DROP CONSTRAINT uq_users_email;

-- Drop PRIMARY KEY
ALTER TABLE users DROP CONSTRAINT users_pkey;
```

## Constraint Naming Conventions

Use consistent naming for easier maintenance:

| Constraint | Convention | Example |
|------------|------------|---------|
| PRIMARY KEY | `pk_<table>` | `pk_users` |
| FOREIGN KEY | `fk_<table>_<column>` | `fk_orders_user_id` |
| UNIQUE | `uq_<table>_<column>` | `uq_users_email` |
| CHECK | `chk_<table>_<description>` | `chk_products_price_positive` |

## Performance Considerations

- **PRIMARY KEY** and **UNIQUE** automatically create indexes
- **FOREIGN KEY** should have an index on the referencing column
- **CHECK** constraints are evaluated on every INSERT/UPDATE
- Too many constraints can slow down writes

```sql
-- Add index for foreign key performance
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
```

## Practice

1. Create a `students` table with:
   - Auto-generated ID
   - Required name and email
   - Unique email
   - Age between 16 and 100

2. Create a `courses` table and an `enrollments` table with:
   - Proper primary keys
   - Foreign keys to students and courses
   - Prevent duplicate enrollments
   - Grade must be between 0 and 100 (or NULL)

3. What happens if you try to:
   - Insert a student with a duplicate email?
   - Delete a student who has enrollments (with RESTRICT)?
   - Insert an enrollment for a non-existent course?
