# Quiz: Making SQL Operations Idempotent

## Instructions

Each question presents a **non-idempotent SQL statement**. Select the modification that would make the operation **idempotent**, or select **"Cannot be made idempotent"** if no modification is possible while preserving the original intent.

---

## Question 1

**Original Statement:**

```sql
INSERT INTO newsletter_subscribers (email, subscribed_at)
VALUES ('user@example.com', NOW());
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', NOW()) ON CONFLICT (email) DO UPDATE SET subscribed_at = NOW();
  ```
- [ ] B.
  ```sql
  INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', NOW()) ON CONFLICT (email) DO NOTHING;
  ```
- [ ] C.
  ```sql
  INSERT INTO newsletter_subscribers (email, subscribed_at) VALUES ('user@example.com', '2026-01-15 12:00:00');
  ```
- [ ] D. Cannot be made idempotent

---

## Question 2

**Original Statement:**

```sql
UPDATE leaderboard SET score = score + 10 WHERE player_id = 55;
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  UPDATE leaderboard SET score = score + 10 WHERE player_id = 55 AND last_updated < NOW();
  ```
- [ ] B.
  ```sql
  UPDATE leaderboard SET score = 10 WHERE player_id = 55;
  ```
- [ ] C.
  ```sql
  INSERT INTO leaderboard (player_id, score) VALUES (55, 10) ON CONFLICT (player_id) DO UPDATE SET score = leaderboard.score + 10;
  ```
- [ ] D. Cannot be made idempotent

---

## Question 3

**Original Statement:**

```sql
INSERT INTO click_events (user_id, page_url, clicked_at)
VALUES (42, '/products/laptop', NOW());
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  INSERT INTO click_events (user_id, page_url, clicked_at) VALUES (42, '/products/laptop', NOW()) ON CONFLICT DO NOTHING;
  ```
- [ ] B.
  ```sql
  INSERT INTO click_events (event_id, user_id, page_url, clicked_at) VALUES ('evt_abc123', 42, '/products/laptop', NOW()) ON CONFLICT (event_id) DO NOTHING;
  ```
- [ ] C.
  ```sql
  UPDATE click_events SET clicked_at = NOW() WHERE user_id = 42 AND page_url = '/products/laptop';
  ```
- [ ] D. Cannot be made idempotent

---

## Question 4

**Original Statement:**

```sql
ALTER TABLE orders ADD COLUMN shipping_address TEXT;
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  ALTER TABLE orders ADD COLUMN IF NOT EXISTS shipping_address TEXT;
  ```
- [ ] B.
  ```sql
  ALTER TABLE IF EXISTS orders ADD COLUMN shipping_address TEXT;
  ```
- [ ] C.
  ```sql
  DROP COLUMN IF EXISTS shipping_address; ALTER TABLE orders ADD COLUMN shipping_address TEXT;
  ```
- [ ] D. Cannot be made idempotent

---

## Question 5

**Original Statement:**

```sql
UPDATE inventory SET quantity = quantity - 5 WHERE product_id = 101 AND warehouse_id = 'MAIN';
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  UPDATE inventory SET quantity = quantity - 5 WHERE product_id = 101 AND warehouse_id = 'MAIN' AND quantity >= 5;
  ```
- [ ] B.
  ```sql
  UPDATE inventory SET quantity = 95 WHERE product_id = 101 AND warehouse_id = 'MAIN';
  ```
- [ ] C.
  ```sql
  UPDATE inventory SET quantity = GREATEST(quantity - 5, 0) WHERE product_id = 101 AND warehouse_id = 'MAIN';
  ```
- [ ] D. Cannot be made idempotent

---

## Question 6

**Original Statement:**

```sql
INSERT INTO order_items (order_id, product_id, quantity, unit_price)
VALUES (1001, 55, 2, 29.99);
```

_Assume `order_items` has an auto-incrementing `id` primary key._

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES (1001, 55, 2, 29.99) ON CONFLICT DO NOTHING;
  ```
- [ ] B.
  ```sql
  INSERT INTO order_items (order_id, product_id, quantity, unit_price) VALUES (1001, 55, 2, 29.99) ON CONFLICT (order_id, product_id) DO NOTHING;
  ```
- [ ] C.
  ```sql
  MERGE INTO order_items USING (VALUES (1001, 55, 2, 29.99)) AS src ON order_items.order_id = src.order_id;
  ```
- [ ] D. Cannot be made idempotent

---

## Question 7

**Original Statement:**

```sql
UPDATE user_sessions SET last_active = NOW() WHERE session_id = 'sess_xyz789';
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  UPDATE user_sessions SET last_active = NOW() WHERE session_id = 'sess_xyz789' AND last_active < NOW();
  ```
- [ ] B.
  ```sql
  UPDATE user_sessions SET last_active = '2026-01-15 14:30:00' WHERE session_id = 'sess_xyz789';
  ```
- [ ] C.
  ```sql
  INSERT INTO user_sessions (session_id, last_active) VALUES ('sess_xyz789', NOW()) ON CONFLICT (session_id) DO UPDATE SET last_active = NOW();
  ```
- [ ] D. Cannot be made idempotent

---

## Question 8

**Original Statement:**

```sql
INSERT INTO api_requests (request_id, endpoint, response_code, logged_at)
VALUES (gen_random_uuid(), '/api/users', 200, NOW());
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES (gen_random_uuid(), '/api/users', 200, NOW()) ON CONFLICT (request_id) DO NOTHING;
  ```
- [ ] B.
  ```sql
  INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES ('fixed-uuid-value', '/api/users', 200, NOW()) ON CONFLICT (request_id) DO NOTHING;
  ```
- [ ] C.
  ```sql
  INSERT INTO api_requests (request_id, endpoint, response_code, logged_at) VALUES ('fixed-uuid-value', '/api/users', 200, '2026-01-15 12:00:00') ON CONFLICT (request_id) DO NOTHING;
  ```
- [ ] D. Cannot be made idempotent

---

## Question 9

**Original Statement:**

```sql
DELETE FROM temp_files WHERE created_at < NOW() - INTERVAL '24 hours';
```

**Which modification makes this idempotent?**

- [ ] A. This statement is already idempotent
- [ ] B.
  ```sql
  DELETE FROM temp_files WHERE created_at < '2026-01-14 12:00:00';
  ```
- [ ] C.
  ```sql
  TRUNCATE TABLE temp_files;
  ```
- [ ] D. Cannot be made idempotent

---

## Question 10

**Original Statement:**

```sql
UPDATE account_balance
SET balance = balance * 1.05,
    last_interest_date = CURRENT_DATE
WHERE account_type = 'savings';
```

**Which modification makes this idempotent?**

- [ ] A.
  ```sql
  UPDATE account_balance SET balance = balance * 1.05, last_interest_date = CURRENT_DATE WHERE account_type = 'savings' AND last_interest_date < CURRENT_DATE;
  ```
- [ ] B.
  ```sql
  UPDATE account_balance SET balance = balance * 1.05, last_interest_date = CURRENT_DATE WHERE account_type = 'savings' AND last_interest_date != CURRENT_DATE;
  ```
- [ ] C.
  ```sql
  UPDATE account_balance SET last_interest_date = CURRENT_DATE WHERE account_type = 'savings';
  ```
- [ ] D. Cannot be made idempotent

---

## Answer Key (Instructor Only)

| Question | Answer | Explanation                                                                                                                                                                                                                                                                                                                         |
| :------: | :----: | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
|    1     | **B**  | `ON CONFLICT (email) DO NOTHING` ensures duplicate emails are ignored. Option A updates `subscribed_at` to a new `NOW()` each time, which is non-idempotent.                                                                                                                                                                        |
|    2     | **D**  | Cannot be made idempotent while preserving the intent of _adding_ 10 points. Option B changes the intent to _setting_ the score to 10. The original goal of incrementing cannot be made idempotent without external tracking.                                                                                                       |
|    3     | **B**  | Adding a client-generated unique `event_id` with `ON CONFLICT DO NOTHING` makes it idempotent. Option A fails because there's no unique constraint on the natural key.                                                                                                                                                              |
|    4     | **A**  | `ADD COLUMN IF NOT EXISTS` is the PostgreSQL-standard way to make DDL idempotent (PostgreSQL 9.6+).                                                                                                                                                                                                                                 |
|    5     | **D**  | Cannot be made idempotent while preserving the intent of _decrementing_ by 5. Option B changes intent to _setting_ a fixed value.                                                                                                                                                                                                   |
|    6     | **B**  | Adding a composite unique constraint on `(order_id, product_id)` with `ON CONFLICT DO NOTHING` prevents duplicates. Option A fails because `ON CONFLICT` without specifying columns doesn't work as expected.                                                                                                                       |
|    7     | **B**  | Using a fixed timestamp makes it idempotent. Options A and C still use `NOW()`, which produces different values on each execution.                                                                                                                                                                                                  |
|    8     | **C**  | Both the UUID and timestamp must be fixed values. Option A still generates random UUID each time. Option B still uses `NOW()`.                                                                                                                                                                                                      |
|    9     | **A**  | DELETE with a WHERE clause is already idempotent—deleting already-deleted rows has no effect, and the rows matching the condition will be deleted on first run.                                                                                                                                                                     |
|    10    | **D**  | Cannot be made idempotent while preserving the intent of _multiplying_ balance. Options A and B add a guard, but if executed exactly once per day they still work—however running twice on the same day would only apply once, changing the intended behavior. The core operation of `balance * 1.05` is inherently non-idempotent. |
