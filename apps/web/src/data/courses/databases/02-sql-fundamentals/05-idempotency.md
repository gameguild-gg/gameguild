# Idempotency

Idempotency is a fundamental concept in computer science and databases. An operation is **idempotent** if performing it multiple times produces the same result as performing it once.

$$f(f(x)) = f(x)$$

[![Idempotency meme](https://media.beehiiv.com/cdn-cgi/image/fit=scale-down,format=auto,onerror=redirect,quality=80/uploads/asset/file/a34ee0f8-2e26-4d34-a7bd-69effda29cd7/0123xcnkaljdfioquoer.jpg?t=1749255663)](https://imgflip.com)

[![Idempotency meme](https://media.beehiiv.com/cdn-cgi/image/fit=scale-down,format=auto,onerror=redirect,quality=80/uploads/asset/file/c8ec9fcb-840f-43a1-baaa-2c8781680cd8/1234jkdlafjkldjfkla.jpg?t=1749255703)](https://imgflip.com)



## Why Idempotency Matters

In real-world systems, operations can fail and need to be retried:

- Network timeouts
- Server crashes mid-operation
- Duplicate requests from users clicking twice
- Message queue redelivery
- Distributed system retries

If your operations are idempotent, retrying them is **safe**. If not, you might end up with:
- Duplicate records
- Incorrect counts
- Corrupted data
- Overcharged customers

## Idempotency in SQL Operations

### DDL Operations

| Operation | Idempotent? | Notes |
|-----------|-------------|-------|
| `CREATE TABLE` | ❌ No | Fails if table exists |
| `CREATE TABLE IF NOT EXISTS` | ✅ Yes | Safe to retry |
| `DROP TABLE` | ❌ No | Fails if table doesn't exist |
| `DROP TABLE IF EXISTS` | ✅ Yes | Safe to retry |
| `ALTER TABLE ADD COLUMN` | ❌ No | Fails if column exists |

::: example "Making DDL idempotent"

```sql
-- ❌ Non-idempotent: fails on second run
CREATE TABLE users (id SERIAL PRIMARY KEY);

-- ✅ Idempotent: safe to run multiple times
CREATE TABLE IF NOT EXISTS users (id SERIAL PRIMARY KEY);

-- ❌ Non-idempotent
DROP TABLE users;

-- ✅ Idempotent
DROP TABLE IF EXISTS users;
```

:::

### DQL Operations (SELECT)

All SELECT queries are naturally **idempotent** - they only read data, never modify it.

```sql
-- ✅ Always idempotent
SELECT * FROM users WHERE id = 1;
SELECT COUNT(*) FROM orders;
```

### DML Operations

This is where idempotency becomes critical:

| Operation | Idempotent? | Notes |
|-----------|-------------|-------|
| `INSERT` | ❌ No | Creates duplicate rows |
| `INSERT ... ON CONFLICT DO NOTHING` | ✅ Yes | UPSERT pattern |
| `INSERT ... ON CONFLICT DO UPDATE` | ✅ Yes | UPSERT pattern |
| `UPDATE ... SET x = value` | ✅ Yes | Same value each time |
| `UPDATE ... SET x = x + 1` | ❌ No | Increments each time |
| `DELETE WHERE ...` | ✅ Yes | Second delete does nothing |

## INSERT: The Idempotency Problem

INSERT is the most problematic operation for idempotency:

```sql
-- ❌ Running this twice creates TWO rows
INSERT INTO logs (message, created_at) 
VALUES ('User logged in', NOW());

INSERT INTO logs (message, created_at) 
VALUES ('User logged in', NOW());

-- Result: 2 duplicate log entries!
```

### Solution 1: UPSERT with ON CONFLICT

PostgreSQL's `INSERT ... ON CONFLICT` (UPSERT) makes inserts idempotent:

::: example "UPSERT - Do Nothing"

```sql
-- If user with this email exists, do nothing
INSERT INTO users (email, name)
VALUES ('john@example.com', 'John Doe')
ON CONFLICT (email) DO NOTHING;

-- Running twice: only one row created ✅
```

:::

::: example "UPSERT - Do Update"

```sql
-- If product with this SKU exists, update it
INSERT INTO products (sku, name, price, updated_at)
VALUES ('WIDGET-001', 'Super Widget', 29.99, NOW())
ON CONFLICT (sku) DO UPDATE SET
    name = EXCLUDED.name,
    price = EXCLUDED.price,
    updated_at = EXCLUDED.updated_at;

-- Running twice: creates or updates, same final state ✅
```

:::

::: note

`EXCLUDED` refers to the row that was proposed for insertion but conflicted.

:::

### Solution 2: Idempotency Keys

For operations without natural unique keys, use an **idempotency key**:

```sql
CREATE TABLE payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    idempotency_key UUID UNIQUE NOT NULL,  -- Client-provided
    amount DECIMAL(10,2) NOT NULL,
    status VARCHAR(20) NOT NULL,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Client generates a unique key for each intended payment
INSERT INTO payments (idempotency_key, amount, status)
VALUES ('550e8400-e29b-41d4-a716-446655440000', 99.99, 'pending')
ON CONFLICT (idempotency_key) DO NOTHING;

-- Retry with same key: no duplicate payment ✅
```

## UPDATE: Idempotent vs Non-Idempotent

### Idempotent Updates (Absolute Values)

Setting a column to a specific value is idempotent:

```sql
-- ✅ Idempotent: same result regardless of repetitions
UPDATE users SET status = 'active' WHERE id = 1;
UPDATE users SET status = 'active' WHERE id = 1;
UPDATE users SET status = 'active' WHERE id = 1;
-- Result: status = 'active' (always the same)

-- ✅ Idempotent: setting to a fixed value
UPDATE products SET price = 29.99 WHERE sku = 'WIDGET-001';
```

### Non-Idempotent Updates (Relative Values)

Operations that depend on current state are NOT idempotent:

```sql
-- ❌ Non-idempotent: increments each time
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
-- Result: view_count increased by 3!

-- ❌ Non-idempotent: appends each time
UPDATE posts SET tags = tags || ARRAY['new-tag'] WHERE id = 1;
```

### Making Relative Updates Idempotent

Use conditions or tracking tables:

::: example "Conditional increment"

```sql
-- Track which events have been processed
CREATE TABLE processed_events (
    event_id UUID PRIMARY KEY,
    processed_at TIMESTAMP DEFAULT NOW()
);

-- Only increment if event not already processed
INSERT INTO processed_events (event_id) 
VALUES ('event-123')
ON CONFLICT (event_id) DO NOTHING;

-- Check if insert happened (row count = 1)
-- If yes, perform the increment
UPDATE products SET view_count = view_count + 1 WHERE id = 1;
```

:::

## DELETE: Naturally Idempotent

DELETE operations are naturally idempotent:

```sql
-- ✅ Idempotent: second delete finds nothing to delete
DELETE FROM sessions WHERE user_id = 1;
DELETE FROM sessions WHERE user_id = 1;
DELETE FROM sessions WHERE user_id = 1;
-- Result: all sessions for user 1 are deleted (same result)

-- ✅ Idempotent: row either exists and gets deleted, or doesn't
DELETE FROM users WHERE id = 42;
```

## Real-World Patterns

### Pattern 1: API Idempotency Keys

REST APIs often require clients to send an idempotency key:

```
POST /api/payments
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000

{
  "amount": 99.99,
  "currency": "USD"
}
```

The server stores this key and rejects duplicates.

### Pattern 2: Event Processing

When processing events from a queue:

```sql
-- Store processed event IDs
CREATE TABLE processed_events (
    event_id VARCHAR(100) PRIMARY KEY,
    processed_at TIMESTAMP DEFAULT NOW()
);

-- Before processing, try to insert
INSERT INTO processed_events (event_id)
VALUES ('evt_abc123')
ON CONFLICT (event_id) DO NOTHING
RETURNING event_id;

-- If RETURNING gives a result, process the event
-- If empty, event was already processed - skip it
```

### Pattern 3: State Machine Updates

Use state machines with valid transitions:

```sql
-- Only transition from 'pending' to 'completed'
UPDATE orders 
SET status = 'completed', completed_at = NOW()
WHERE id = 1 AND status = 'pending';

-- Running twice: second run affects 0 rows (safe!) ✅
```

## Summary

| Make it Idempotent | How |
|--------------------|-----|
| CREATE/DROP | Add `IF [NOT] EXISTS` |
| INSERT | Use `ON CONFLICT DO NOTHING/UPDATE` |
| UPDATE (increment) | Track processed operations |
| UPDATE (set value) | Already idempotent! |
| DELETE | Already idempotent! |

## Practice

1. Convert this non-idempotent INSERT to idempotent:
   ```sql
   INSERT INTO subscribers (email) VALUES ('user@example.com');
   ```

2. How would you make this counter update idempotent?
   ```sql
   UPDATE articles SET likes = likes + 1 WHERE id = 5;
   ```

3. Design an idempotent payment processing system that handles retries safely.

4. What's wrong with this "idempotent" update?
   ```sql
   UPDATE users SET last_login = NOW() WHERE id = 1;
   ```
