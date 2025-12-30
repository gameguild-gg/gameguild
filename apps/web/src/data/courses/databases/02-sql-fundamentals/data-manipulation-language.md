# DML — Data Manipulation Language

DML (Data Manipulation Language) is the subset of SQL used to manipulate data within database tables. The main operations are INSERT, UPDATE, and DELETE.

[![DML meme](https://programmerhumor.io/wp-content/uploads/2023/03/programmerhumor-io-databases-memes-backend-memes-08a1cd1a18087d8.jpg)](https://programmerhumor.io/)

## INSERT Statement

Adds new rows to a table.

### Basic INSERT

::: example "Insert a single row"

```sql
INSERT INTO users (username, email, created_at)
VALUES ('johndoe', 'john@example.com', CURRENT_TIMESTAMP);
```

:::

### INSERT Multiple Rows

::: example "Insert multiple rows"

```sql
INSERT INTO products (name, price, category)
VALUES 
    ('Laptop', 999.99, 'Electronics'),
    ('Mouse', 29.99, 'Electronics'),
    ('Keyboard', 79.99, 'Electronics');
```

:::

### INSERT with RETURNING

PostgreSQL allows returning the inserted data:

::: example "Insert with returning"

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

:::

### INSERT from SELECT

Copy data from one table to another:

::: example "Insert from select"

```sql
-- Copy active users to archive
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

:::

## UPDATE Statement

Modifies existing rows in a table.

### Basic UPDATE

::: example "Update rows"

```sql
-- Update single row
UPDATE users 
SET email = 'newemail@example.com' 
WHERE id = 1;

-- Update multiple columns
UPDATE products 
SET price = 89.99, updated_at = CURRENT_TIMESTAMP 
WHERE id = 42;

-- Update all rows (dangerous!)
UPDATE products SET is_active = true;
```

:::

::: warning

Always include a WHERE clause with UPDATE unless you intentionally want to update ALL rows!

```sql
-- ⚠️ This updates EVERY product!
UPDATE products SET price = 0;
```

:::

### UPDATE with Expressions

::: example "Update with calculations"

```sql
-- Increase price by 10%
UPDATE products 
SET price = price * 1.10 
WHERE category = 'Electronics';

-- Decrease stock
UPDATE products 
SET stock = stock - 5 
WHERE id = 100;

-- Conditional update with CASE
UPDATE orders 
SET status = CASE 
    WHEN shipped_at IS NOT NULL THEN 'shipped'
    WHEN paid_at IS NOT NULL THEN 'paid'
    ELSE 'pending'
END;
```

:::

### UPDATE with RETURNING

::: example "Update with returning"

```sql
UPDATE products 
SET price = price * 0.9 
WHERE category = 'Clearance'
RETURNING id, name, price;
```

:::

### UPDATE from Another Table

::: example "Update with join"

```sql
-- Update using data from another table
UPDATE products p
SET category_name = c.name
FROM categories c
WHERE p.category_id = c.id;
```

:::

## DELETE Statement

Removes rows from a table.

[![DELETE without WHERE](https://preview.redd.it/ysc1br9icup71.jpg?width=640&crop=smart&auto=webp&s=33f4ff68a749128e3d4b9a5c252d7b511eab545b)](https://reddit.com)

### Basic DELETE

::: example "Delete rows"

```sql
-- Delete specific row
DELETE FROM users WHERE id = 1;

-- Delete with condition
DELETE FROM sessions WHERE expires_at < CURRENT_TIMESTAMP;

-- Delete all rows (dangerous!)
DELETE FROM logs;
```

:::

::: warning

Always include a WHERE clause with DELETE unless you intentionally want to delete ALL rows!

```sql
-- ⚠️ This deletes EVERY user!
DELETE FROM users;
```

:::

### DELETE with RETURNING

::: example "Delete with returning"

```sql
-- Return deleted rows
DELETE FROM expired_tokens 
WHERE created_at < NOW() - INTERVAL '30 days'
RETURNING id, user_id;
```

:::

### DELETE vs TRUNCATE

| Feature | DELETE | TRUNCATE |
|---------|--------|----------|
| WHERE clause | ✅ Yes | ❌ No |
| RETURNING | ✅ Yes | ❌ No |
| Triggers | ✅ Fires | ❌ Doesn't fire |
| Transaction | ✅ Can rollback | ⚠️ Depends on DB |
| Speed | Slower (row by row) | Faster (drops pages) |
| Resets SERIAL/IDENTITY | ❌ No | ✅ Optional |

## Idempotency in DML

Understanding which operations are idempotent is crucial for reliable systems:

### Non-Idempotent Operations

Running these twice produces different results:

```sql
-- ❌ Creates duplicate rows
INSERT INTO logs (message) VALUES ('User logged in');
INSERT INTO logs (message) VALUES ('User logged in');

-- ❌ Counter increases twice  
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
```

### Idempotent Operations

Running these twice produces the same result:

```sql
-- ✅ Same result regardless of how many times executed
UPDATE users SET status = 'active' WHERE id = 1;
UPDATE users SET status = 'active' WHERE id = 1;

-- ✅ Second delete does nothing
DELETE FROM sessions WHERE user_id = 1;
DELETE FROM sessions WHERE user_id = 1;
```

### Making INSERT Idempotent with UPSERT

Use `INSERT ... ON CONFLICT` (UPSERT) to handle duplicates:

::: example "UPSERT patterns"

```sql
-- Insert or do nothing if exists
INSERT INTO users (id, username, email)
VALUES (1, 'johndoe', 'john@example.com')
ON CONFLICT (id) DO NOTHING;

-- Insert or update if exists
INSERT INTO products (sku, name, price)
VALUES ('ABC123', 'Widget', 29.99)
ON CONFLICT (sku) DO UPDATE 
SET name = EXCLUDED.name,
    price = EXCLUDED.price,
    updated_at = CURRENT_TIMESTAMP;

-- Upsert with condition
INSERT INTO inventory (product_id, quantity)
VALUES (1, 100)
ON CONFLICT (product_id) DO UPDATE 
SET quantity = inventory.quantity + EXCLUDED.quantity
WHERE inventory.quantity < 1000;
```

:::

## RETURNING Clause Summary

PostgreSQL's RETURNING clause works with all DML operations:

```sql
-- INSERT
INSERT INTO users (name) VALUES ('John') RETURNING id;

-- UPDATE  
UPDATE users SET status = 'active' WHERE id = 1 RETURNING *;

-- DELETE
DELETE FROM tokens WHERE expired = true RETURNING id, user_id;
```

This is extremely useful for:
- Getting auto-generated IDs
- Confirming what was modified
- Chaining operations in application code

## Practice

1. Insert 3 new products with different categories
2. Update all products in 'Electronics' category to have a 15% discount
3. Delete all orders older than 1 year
4. Create an UPSERT statement for syncing user preferences
5. Write a statement that soft-deletes a user (sets `deleted_at` instead of deleting)
