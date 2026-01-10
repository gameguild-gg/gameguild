# Indexing Fundamentals

Indexes are data structures that improve the speed of data retrieval operations on database tables. They work similarly to an index in a book - instead of reading every page to find a topic, you look it up in the index and go directly to the relevant page.

---

## Why Indexes Matter

### Without an Index

When you query a table without an index, the database must perform a **full table scan** - reading every row to find matches.

```sql
SELECT * FROM customers WHERE email = 'alice@example.com';
```

With 1 million customers, this query must check all 1 million rows.

### With an Index

An index on `email` creates a sorted structure that allows the database to find the row in **O(log n)** time instead of **O(n)**.

```sql
CREATE INDEX idx_customers_email ON customers(email);
```

Now the same query might only need to examine ~20 rows (log₂ of 1 million ≈ 20).

### Performance Impact

| Operation | Without Index | With Index |
|-----------|---------------|------------|
| SELECT by indexed column | O(n) - scan all rows | O(log n) - tree lookup |
| INSERT | O(1) - just append | O(log n) - update index too |
| UPDATE indexed column | O(n) + O(1) | O(log n) + O(log n) |
| DELETE | O(n) + O(1) | O(log n) + O(log n) |

> **Trade-off:** Indexes speed up reads but slow down writes. Every INSERT, UPDATE, or DELETE must also update the index.

---

## How Indexes Work

### B-Tree Indexes (Default)

Most databases use **B-Tree** (Balanced Tree) indexes by default. They maintain sorted data and allow searches, insertions, and deletions in O(log n) time.

```
                    [50]
                   /    \
              [25]        [75]
             /    \      /    \
          [10] [30]   [60]  [90]
          /  \   \     /     /  \
         [5][15][35] [55]  [80][95]
```

B-Trees are excellent for:
- Equality comparisons (`=`)
- Range queries (`<`, `>`, `BETWEEN`)
- Prefix matching (`LIKE 'abc%'`)
- Ordering (`ORDER BY`)

### Index Storage

An index stores:
1. The indexed column value(s)
2. A pointer to the actual row (typically the row's physical location or primary key)

```
Index: idx_customers_email

| email               | row_pointer |
|---------------------|-------------|
| alice@example.com   | row_42      |
| bob@example.com     | row_17      |
| carol@example.com   | row_103     |
| ...                 | ...         |
```

---

## Creating Indexes

### Basic Syntax

```sql
CREATE INDEX index_name ON table_name (column_name);
```

### Single-Column Index

```sql
-- Index on email for fast lookups
CREATE INDEX idx_customers_email ON customers(email);

-- Index on order date for date range queries
CREATE INDEX idx_orders_date ON orders(order_date);
```

### Multi-Column (Composite) Index

```sql
-- Index on multiple columns
CREATE INDEX idx_orders_customer_date ON orders(customer_id, order_date);
```

**Column order matters!** A composite index on `(A, B, C)` can be used for:
- Queries on `A`
- Queries on `A` and `B`
- Queries on `A`, `B`, and `C`

But **NOT** efficiently for:
- Queries on `B` alone
- Queries on `C` alone
- Queries on `B` and `C`

Think of it like a phone book sorted by (Last Name, First Name) - you can look up "Smith" quickly, or "Smith, John", but not all "Johns".

### Unique Index

Enforces uniqueness on the indexed columns:

```sql
CREATE UNIQUE INDEX idx_users_email ON users(email);
```

> **Note:** A `UNIQUE` constraint automatically creates a unique index.

### Partial Index

Indexes only rows that match a condition:

```sql
-- Index only active users
CREATE INDEX idx_users_active ON users(email) WHERE status = 'active';

-- Index only recent orders
CREATE INDEX idx_orders_recent ON orders(order_date) 
WHERE order_date > '2025-01-01';
```

Partial indexes are smaller and faster when queries match the condition.

### Expression Index

Indexes the result of an expression:

```sql
-- Index on lowercase email for case-insensitive lookups
CREATE INDEX idx_users_email_lower ON users(LOWER(email));

-- Now this query uses the index:
SELECT * FROM users WHERE LOWER(email) = 'alice@example.com';
```

---

## Index Types in PostgreSQL

### B-Tree (Default)

Best for: Equality and range queries

```sql
CREATE INDEX idx_orders_total ON orders(total);  -- B-Tree by default

-- Used by:
SELECT * FROM orders WHERE total > 100;
SELECT * FROM orders WHERE total BETWEEN 50 AND 150;
SELECT * FROM orders ORDER BY total;
```

### Hash

Best for: Equality comparisons only (not range queries)

```sql
CREATE INDEX idx_users_email_hash ON users USING HASH (email);

-- Used by:
SELECT * FROM users WHERE email = 'alice@example.com';

-- NOT used by (B-Tree is better):
SELECT * FROM users WHERE email LIKE 'a%';
```

### GiST (Generalized Search Tree)

Best for: Geometric data, full-text search, range types

```sql
-- For geographic data
CREATE INDEX idx_locations_coords ON locations USING GIST (coordinates);

-- For text search
CREATE INDEX idx_articles_content ON articles USING GIST (to_tsvector('english', content));
```

### GIN (Generalized Inverted Index)

Best for: Arrays, JSONB, full-text search with many distinct values

```sql
-- For JSONB columns
CREATE INDEX idx_products_attributes ON products USING GIN (attributes);

-- For array columns
CREATE INDEX idx_posts_tags ON posts USING GIN (tags);

-- For full-text search
CREATE INDEX idx_articles_fts ON articles USING GIN (to_tsvector('english', content));
```

### BRIN (Block Range Index)

Best for: Very large tables with naturally ordered data (like timestamps)

```sql
-- For time-series data
CREATE INDEX idx_events_timestamp ON events USING BRIN (created_at);
```

BRIN indexes are much smaller than B-Tree but only effective when data is physically ordered.

---

## Automatic Indexes

PostgreSQL automatically creates indexes for:

1. **PRIMARY KEY** - Unique B-Tree index
2. **UNIQUE constraints** - Unique B-Tree index

```sql
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,  -- Auto-creates unique index
    email VARCHAR(255) UNIQUE    -- Auto-creates unique index
);

-- These are equivalent to:
-- CREATE UNIQUE INDEX users_pkey ON users(user_id);
-- CREATE UNIQUE INDEX users_email_key ON users(email);
```

**Note:** Foreign keys do NOT automatically get indexes in PostgreSQL. You should create them manually:

```sql
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INT REFERENCES customers(customer_id)
);

-- Add index on foreign key (recommended!)
CREATE INDEX idx_orders_customer ON orders(customer_id);
```

---

## Viewing and Managing Indexes

### List Indexes

```sql
-- List all indexes on a table
SELECT indexname, indexdef
FROM pg_indexes
WHERE tablename = 'orders';

-- Using \d in psql
\d orders
```

### Drop an Index

```sql
DROP INDEX index_name;

-- Drop if exists (idempotent)
DROP INDEX IF EXISTS index_name;
```

### Rename an Index

```sql
ALTER INDEX old_name RENAME TO new_name;
```

### Rebuild an Index

```sql
REINDEX INDEX index_name;
REINDEX TABLE table_name;
```

---

## When to Create Indexes

### Good Candidates for Indexing

| Column Type | Reason |
|-------------|--------|
| Primary keys | Automatic, used for JOINs |
| Foreign keys | Speed up JOINs and constraint checks |
| Columns in WHERE clauses | Speed up filtering |
| Columns in JOIN conditions | Speed up joins |
| Columns in ORDER BY | Speed up sorting |
| Columns in GROUP BY | Speed up grouping |
| Columns with high selectivity | More benefit from indexing |

**High selectivity** = Many distinct values (like email, user_id)
**Low selectivity** = Few distinct values (like status, gender)

### Poor Candidates for Indexing

| Situation | Reason |
|-----------|--------|
| Small tables (< 1000 rows) | Full scan is fast enough |
| Columns rarely used in queries | Wasted space and maintenance |
| Columns with low selectivity | Index barely helps |
| Tables with heavy write loads | Index maintenance overhead |
| Columns that change frequently | Constant index updates |

### The 5% Rule

An index is typically beneficial when a query selects **less than 5-15%** of the table's rows. For larger result sets, a full table scan may be faster.

---

## Index-Only Scans (Covering Indexes)

When all columns needed by a query are in the index, PostgreSQL can answer the query using **only the index** - never touching the table.

```sql
-- Create index including both columns
CREATE INDEX idx_orders_customer_total ON orders(customer_id, total);

-- This query can be answered from the index alone
SELECT customer_id, total FROM orders WHERE customer_id = 5;
```

### INCLUDE Clause (PostgreSQL 11+)

Add non-key columns to an index for covering queries:

```sql
CREATE INDEX idx_orders_covering ON orders(customer_id) 
INCLUDE (order_date, total);

-- Now this query uses an index-only scan:
SELECT customer_id, order_date, total FROM orders WHERE customer_id = 5;
```

---

## Analyzing Query Performance

### EXPLAIN

Shows the query execution plan:

```sql
EXPLAIN SELECT * FROM orders WHERE customer_id = 5;
```

Output:
```
Index Scan using idx_orders_customer on orders  (cost=0.29..8.30 rows=1 width=40)
  Index Cond: (customer_id = 5)
```

### EXPLAIN ANALYZE

Actually runs the query and shows real timing:

```sql
EXPLAIN ANALYZE SELECT * FROM orders WHERE customer_id = 5;
```

Output:
```
Index Scan using idx_orders_customer on orders  
  (cost=0.29..8.30 rows=1 width=40) 
  (actual time=0.015..0.016 rows=1 loops=1)
Planning Time: 0.123 ms
Execution Time: 0.042 ms
```

### Key EXPLAIN Terms

| Term | Meaning |
|------|---------|
| **Seq Scan** | Full table scan (no index used) |
| **Index Scan** | Using an index, then fetching rows |
| **Index Only Scan** | Using only the index (covering) |
| **Bitmap Index Scan** | Building a bitmap of matching rows |
| **Nested Loop** | For each row in outer, scan inner |
| **Hash Join** | Build hash table, probe with other |
| **Merge Join** | Both sides sorted, merge together |
| **cost** | Estimated startup and total cost |
| **rows** | Estimated number of rows |
| **actual time** | Real execution time (with ANALYZE) |

---

## Index Maintenance

### Statistics and ANALYZE

PostgreSQL uses statistics to choose the best query plan. Update them after major data changes:

```sql
ANALYZE table_name;
ANALYZE;  -- All tables in database
```

### Bloat

Indexes can become bloated after many updates/deletes. Rebuild periodically:

```sql
REINDEX INDEX index_name;

-- Or use VACUUM for general maintenance
VACUUM ANALYZE table_name;
```

### Concurrent Index Creation

Creating an index locks the table. For production systems, use:

```sql
CREATE INDEX CONCURRENTLY idx_orders_date ON orders(order_date);
```

This takes longer but doesn't block writes.

---

## Common Indexing Patterns

### Pattern 1: Foreign Key Indexes

Always index foreign keys for JOIN performance:

```sql
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INT NOT NULL REFERENCES customers(customer_id),
    product_id INT NOT NULL REFERENCES products(product_id)
);

-- Add these indexes
CREATE INDEX idx_orders_customer ON orders(customer_id);
CREATE INDEX idx_orders_product ON orders(product_id);
```

### Pattern 2: Composite Index for Common Filters

```sql
-- If you often query by customer and date range:
SELECT * FROM orders 
WHERE customer_id = 5 AND order_date BETWEEN '2026-01-01' AND '2026-01-31';

-- Create composite index
CREATE INDEX idx_orders_customer_date ON orders(customer_id, order_date);
```

### Pattern 3: Partial Index for Soft Delete

```sql
-- If you frequently query only active records:
SELECT * FROM users WHERE deleted_at IS NULL;

-- Create partial index
CREATE INDEX idx_users_active ON users(email) WHERE deleted_at IS NULL;
```

### Pattern 4: Expression Index for Computed Values

```sql
-- If you often search by year:
SELECT * FROM orders WHERE EXTRACT(YEAR FROM order_date) = 2026;

-- Create expression index
CREATE INDEX idx_orders_year ON orders(EXTRACT(YEAR FROM order_date));
```

### Pattern 5: GIN for JSONB Queries

```sql
-- For JSONB data
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    data JSONB
);

CREATE INDEX idx_products_data ON products USING GIN (data);

-- Now this query uses the index:
SELECT * FROM products WHERE data @> '{"category": "electronics"}';
```

---

## Index Naming Conventions

Use consistent naming for maintainability:

| Pattern | Example |
|---------|---------|
| `idx_table_column` | `idx_orders_customer_id` |
| `idx_table_col1_col2` | `idx_orders_customer_date` |
| `idx_table_purpose` | `idx_orders_active_only` |
| `uidx_table_column` | `uidx_users_email` (unique) |

---

## Practice

### Exercise 1: Index Analysis

Given this table and query pattern, what indexes would you create?

```sql
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(200),
    category VARCHAR(50),
    price DECIMAL(10, 2),
    stock_quantity INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Common queries:
-- 1. Find products by category
-- 2. Find products by category with price range
-- 3. Find products ordered by created_at DESC
-- 4. Find products where stock_quantity < 10
```

### Exercise 2: Explain Analysis

Run EXPLAIN ANALYZE on queries before and after adding indexes. What changes do you observe?

### Exercise 3: Index Trade-offs

For a table with 10 million rows and heavy INSERT traffic (1000 inserts/second), discuss:
1. How many indexes is too many?
2. When would you consider NOT indexing a frequently-queried column?

---

## Key Takeaways

1. **Indexes speed up reads but slow down writes** - use them strategically
2. **B-Tree is the default** and works for most use cases (equality, ranges, ordering)
3. **Foreign keys need manual indexes** in PostgreSQL
4. **Composite index column order matters** - put high-selectivity columns first
5. **Partial indexes** reduce size for filtered queries
6. **Use EXPLAIN ANALYZE** to verify index usage
7. **Covering indexes** enable index-only scans
8. **Maintain indexes** with VACUUM, ANALYZE, and occasional REINDEX
9. **Name indexes consistently** for easier maintenance
10. **Don't over-index** - each index has storage and write overhead
