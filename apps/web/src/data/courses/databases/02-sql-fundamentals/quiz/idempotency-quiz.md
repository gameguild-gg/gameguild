# Quiz: Idempotency in SQL Operations

## Instructions

An operation is **idempotent** if executing it multiple times produces the same database state as executing it once. A **non-idempotent** operation changes the database state differently with each execution.

**Categorize each SQL statement below into the correct group.**

---

## SQL Statements to Classify

```sql
-- A
UPDATE accounts SET balance = 5000.00 WHERE account_id = 101;

-- B
UPDATE accounts SET balance = balance + 500.00 WHERE account_id = 101;

-- C
INSERT INTO audit_log (action, user_id, created_at)
VALUES ('password_change', 42, NOW());

-- D
DELETE FROM expired_sessions WHERE user_id = 88;

-- E
INSERT INTO users (id, email, name)
VALUES (1, 'alice@company.com', 'Alice')
ON CONFLICT (id) DO NOTHING;

-- F
CREATE TABLE reports (id SERIAL PRIMARY KEY, title TEXT);

-- G
CREATE TABLE IF NOT EXISTS reports (id SERIAL PRIMARY KEY, title TEXT);

-- H
DROP TABLE IF EXISTS temp_calculations;

-- I
INSERT INTO orders (customer_id, total, order_date)
VALUES (15, 299.99, CURRENT_TIMESTAMP);

-- J
SELECT nextval('invoice_number_seq');

-- K
UPDATE products SET price = 19.99 WHERE sku = 'WIDGET-001';

-- L
UPDATE products SET stock_count = stock_count - 1 WHERE sku = 'WIDGET-001';

-- M
INSERT INTO inventory (product_id, quantity, warehouse_id)
VALUES (500, 100, 'WH-EAST')
ON CONFLICT (product_id, warehouse_id)
DO UPDATE SET quantity = EXCLUDED.quantity;

-- N
INSERT INTO inventory (product_id, quantity, warehouse_id)
VALUES (500, 100, 'WH-EAST')
ON CONFLICT (product_id, warehouse_id)
DO UPDATE SET quantity = inventory.quantity + EXCLUDED.quantity;

-- O
UPDATE events SET processed_at = '2026-01-15 14:30:00' WHERE event_id = 777;

-- P
UPDATE events SET processed_at = NOW() WHERE event_id = 777;

-- Q
INSERT INTO tokens (user_id, token, expires_at)
VALUES (10, gen_random_uuid(), NOW() + INTERVAL '1 hour');

-- R
TRUNCATE TABLE staging_data;

-- S
DELETE FROM cache_entries WHERE cache_key = 'user:profile:42';

-- T
ALTER TABLE customers ADD COLUMN loyalty_points INT DEFAULT 0;

-- U
DROP TABLE products;

-- V
INSERT INTO payments (idempotency_key, amount, currency, status)
VALUES ('req_abc123xyz', 149.99, 'USD', 'completed')
ON CONFLICT (idempotency_key) DO NOTHING;

-- W
UPDATE counters SET value = value * 2 WHERE counter_name = 'page_views';

-- X
UPDATE settings SET theme = 'dark' WHERE user_id = 5;
```

---

## Bonus Question

For the statements you classified as **non-idempotent**, briefly explain what makes each one non-idempotent and suggest a modification that would make it idempotent (if possible).

---

## Answer Key (Instructor Only)

### Idempotent Group - Answers

| Letter | Statement Description |
|:------:|----------------------|
| A | `UPDATE accounts SET balance = 5000.00` - Sets absolute value, same result every time |
| D | `DELETE FROM expired_sessions WHERE user_id = 88` - Deleting already-deleted rows has no effect |
| E | `INSERT ... ON CONFLICT (id) DO NOTHING` - Skips if row exists |
| G | `CREATE TABLE IF NOT EXISTS reports` - No error if table already exists |
| H | `DROP TABLE IF EXISTS temp_calculations` - No error if table doesn't exist |
| K | `UPDATE products SET price = 19.99` - Sets absolute value |
| M | `INSERT ... ON CONFLICT DO UPDATE SET quantity = EXCLUDED.quantity` - Replaces with same fixed value |
| O | `UPDATE events SET processed_at = '2026-01-15 14:30:00'` - Fixed timestamp, same result every time |
| R | `TRUNCATE TABLE staging_data` - Truncating empty table has same effect as truncating once |
| S | `DELETE FROM cache_entries WHERE cache_key = 'user:profile:42'` - Deleting by specific key is idempotent |
| V | `INSERT ... ON CONFLICT (idempotency_key) DO NOTHING` - Classic idempotency key pattern |
| X | `UPDATE settings SET theme = 'dark'` - Sets absolute value |

### Non-Idempotent Group - Answers

| Letter | Statement Description |
|:------:|----------------------|
| B | `UPDATE accounts SET balance = balance + 500.00` - Increments balance each execution |
| C | `INSERT INTO audit_log ... NOW()` - Creates new row each time with different timestamp |
| F | `CREATE TABLE reports` - Fails on second execution (table already exists) |
| I | `INSERT INTO orders ... CURRENT_TIMESTAMP` - Creates new row with new ID each time |
| J | `SELECT nextval('invoice_number_seq')` - Advances sequence with each call |
| L | `UPDATE products SET stock_count = stock_count - 1` - Decrements each execution |
| N | `INSERT ... ON CONFLICT DO UPDATE SET quantity = inventory.quantity + EXCLUDED.quantity` - Accumulates quantity |
| P | `UPDATE events SET processed_at = NOW()` - Different timestamp on each execution |
| Q | `INSERT INTO tokens ... gen_random_uuid(), NOW()` - New UUID and timestamp each time |
| T | `ALTER TABLE customers ADD COLUMN` - Fails on second execution (column already exists) |
| U | `DROP TABLE products` - Fails on second execution (table no longer exists) |
| W | `UPDATE counters SET value = value * 2` - Doubles value each execution |
