# Filtering, Pattern Matching, Aggregation and Grouping

- Day 1 (Filtering & Pattern Matching),
- Day 2 (Aggregation & Grouping)

---

## Agenda (2 days)

- Day 1: Boolean logic, operator precedence, `IN` / `NOT IN`, `BETWEEN`, `LIKE`/`ILIKE`, `SIMILAR TO`, regex (`~`, `~*`), escaping wildcards, NULL handling, `CASE`, date filters
- Day 2: Aggregate functions, NULL rules, `GROUP BY`, `HAVING`, DISTINCT aggregates, conditional aggregation, execution order, common patterns, pitfalls (`NOT IN` with NULL, alias scope)

---

# Day 1

## Filtering & Pattern Matching

---

## Three-valued logic (TRUE/FALSE/NULL)

- Comparisons with `NULL` yield `NULL` (unknown)
  - `NULL = NULL` returns `NULL` (not TRUE)
- `IS NULL` / `IS NOT NULL` for checks. returns TRUE/FALSE
  - `NULL IS DISTINCT FROM NULL` returns FALSE
  - `5 IS DISTINCT FROM NULL` returns TRUE

```sql
SELECT NULL = NULL;          -- NULL
SELECT NULL IS NULL;         -- true
SELECT 5 IS DISTINCT FROM NULL; -- true
```

---

## Boolean operators & precedence

- Order: `NOT` then `AND` then `OR`
- Always parenthesize mixed `AND/OR` for intent

```sql
-- AND binds tighter than OR
WHERE price > 100 OR category = 'Books' AND in_stock = true
-- Interpreted as: price > 100 OR (category = 'Books' AND in_stock = true)
-- In this case AND has higher precedence, so it is solved first.

-- Safer, or if unsure, use parentheses:
WHERE (category IN ('Books','Music')) AND price < 20
```

---

## Basic comparisons refresher

- Operators: `=`, `<>` (not equal), `<`, `>`, `<=`, `>=`
- Chain with AND/OR and parentheses

```sql
WHERE price >= 500           -- premium products
WHERE price BETWEEN 20 AND 100
WHERE status IN ('pending','confirmed','processing')
```

---

## IN / NOT IN

- `IN` is cleaner than many ORs
- `NOT IN` + NULL in list means entire predicate becomes NULL (no rows). Prefer `NOT EXISTS` or filter out NULLs.

```sql
-- Safe NOT IN pattern
WHERE id NOT IN (
	SELECT user_id FROM banned_users WHERE user_id IS NOT NULL
)
-- Or
WHERE NOT EXISTS (
	SELECT 1 FROM banned_users b WHERE b.user_id = u.id
)
```

---

## BETWEEN

- Inclusive: `BETWEEN 10 AND 20` includes 10 and 20
- `NOT BETWEEN 10 AND 50` is equivalent to `< 10 OR > 50`
  - Pay attention to what NOT is doing with inclusive ranges!

```sql
WHERE salary BETWEEN 40000 AND 80000
WHERE created_at BETWEEN '2026-01-01' AND '2026-02-01'
```

---

## Pattern matching: LIKE / ILIKE

- `%` matches 0 or more chars;
- `_` matches exactly 1 char
- `LIKE` is case-sensitive, `ILIKE` is case-insensitive
- Exactly 3 chars: `LIKE '___'`
- Starts with "Pro" and ends with "Plus": `LIKE 'Pro%Plus'`

```sql
-- Case-insensitive search
WHERE name ILIKE '%wireless%';

-- Literal underscore
WHERE sku LIKE '%!_%' ESCAPE '!';  -- alt: '%[_]%'
-- this will match any sku containing an underscore character
```

---

## SIMILAR TO & regex

- `SIMILAR TO` combines LIKE + regex: `name SIMILAR TO '(John|Jane)%'`
- POSIX operators:
  - `~` case-sensitive match
  - `~*` case-insensitive match
  - `!~`, `!~*` negated

```sql
WHERE email ~* '^[a-z0-9._%+-]+@gmail\.com$';
```

---

## Escaping wildcards

- Escape `%` or `_` with an escape char

```sql
WHERE name LIKE '%\%%' ESCAPE '\';   -- literal percent
WHERE sku  LIKE '%!_%' ESCAPE '!';   -- literal underscore
WHERE name LIKE '%[_]%';             -- bracket escape style
```

---

## NULL handling

- Aggregates ignore NULLs except `COUNT(*)`
- `COALESCE(a,b,'default')` returns first non-NULL
- `NULLIF(a,b)` returns NULL when equal
- `NOT IN (1,2,NULL)` yields no rows

```sql
SELECT COALESCE(NULL, NULL, 'default');  -- 'default'
SELECT NULLIF(100, 100);                 -- NULL
```

---

## String concatenation & fallbacks

- Concatenate with `||`
- Provide defaults with `COALESCE`

```sql
SELECT
	first_name || ' ' || last_name AS full_name,
	COALESCE(phone, 'No phone on file') AS contact_phone
FROM customers;
```

---

## CASE expressions

```sql
CASE
	WHEN status = 'completed' THEN 'Done'
	WHEN status = 'processing' THEN 'In Progress'
	ELSE 'Waiting'
END
```

- Unmatched cases fall to `ELSE` (or NULL if omitted)

---

## Date/Time Data Types

| Type          | Storage  | Description               | Example                    |
| ------------- | -------- | ------------------------- | -------------------------- |
| `DATE`        | 4 bytes  | Date only (no time)       | `'2026-02-05'`             |
| `TIME`        | 8 bytes  | Time only (no date)       | `'14:30:00'`               |
| `TIMESTAMP`   | 8 bytes  | Date + time (no timezone) | `'2026-02-05 14:30:00'`    |
| `TIMESTAMPTZ` | 8 bytes  | Date + time + timezone    | `'2026-02-05 14:30:00-05'` |
| `INTERVAL`    | 16 bytes | Time duration             | `'1 year 2 months 3 days'` |

> **Best Practice:** Use `TIMESTAMPTZ` for real-world events (handles DST automatically)

---

## Getting Current Date/Time

```sql
-- Current date and time
SELECT NOW();                    -- 2026-02-05 14:30:00.123456-05
SELECT CURRENT_TIMESTAMP;        -- same as NOW()
SELECT CURRENT_DATE;             -- 2026-02-05
SELECT CURRENT_TIME;             -- 14:30:00.123456-05

-- Transaction time (same for entire transaction)
SELECT NOW(), pg_sleep(2), NOW();  -- Both NOW() return same value!

-- Wall clock time (changes during transaction)
SELECT clock_timestamp();        -- Actual current time
```

---

## Date Arithmetic with INTERVAL

```sql
-- Add/subtract intervals
SELECT NOW() + INTERVAL '7 days';           -- 1 week from now
SELECT NOW() - INTERVAL '1 month';          -- 1 month ago
SELECT NOW() + INTERVAL '2 hours 30 minutes';

-- Combine intervals
SELECT INTERVAL '1 year' + INTERVAL '6 months';  -- 1 year 6 mons

-- Multiply intervals
SELECT INTERVAL '1 day' * 7;                -- 7 days

-- Subtract dates (returns integer days)
SELECT '2026-02-28'::DATE - '2026-02-01'::DATE;  -- 27

-- Subtract timestamps (returns interval)
SELECT '2026-02-05 15:00'::TIMESTAMP - '2026-02-05 10:30'::TIMESTAMP;
-- Result: 4:30:00
```

---

## INTERVAL Syntax Variations

```sql
-- ISO 8601 format
INTERVAL 'P1Y2M3DT4H5M6S'   -- 1 year, 2 months, 3 days, 4 hours, 5 min, 6 sec

-- PostgreSQL verbose format
INTERVAL '1 year 2 months 3 days 4 hours 5 minutes 6 seconds'

-- Shorthand (single unit)
INTERVAL '30 days'
INTERVAL '6 hours'

-- Using @ symbol (alternative)
@ 1 year 2 months

-- Common patterns
INTERVAL '1 week'            -- 7 days
INTERVAL '1 fortnight'       -- 14 days (yes, really!)
```

---

## DATE_TRUNC - Truncate to Precision

Rounds down to the start of the specified unit:

```sql
-- Assuming NOW() = '2026-02-05 14:35:42'
SELECT DATE_TRUNC('year', NOW());    -- 2026-01-01 00:00:00
SELECT DATE_TRUNC('quarter', NOW()); -- 2026-01-01 00:00:00
SELECT DATE_TRUNC('month', NOW());   -- 2026-02-01 00:00:00
SELECT DATE_TRUNC('week', NOW());    -- 2026-02-02 00:00:00 (Monday)
SELECT DATE_TRUNC('day', NOW());     -- 2026-02-05 00:00:00
SELECT DATE_TRUNC('hour', NOW());    -- 2026-02-05 14:00:00
SELECT DATE_TRUNC('minute', NOW());  -- 2026-02-05 14:35:00
```

> **Note:** Week starts on **Monday** in PostgreSQL (ISO 8601 standard)

---

## EXTRACT - Get Date Parts

Pull specific components from a date/timestamp:

```sql
-- Assuming order_date = '2026-02-05 14:35:42'
SELECT EXTRACT(YEAR FROM order_date);      -- 2026
SELECT EXTRACT(MONTH FROM order_date);     -- 2
SELECT EXTRACT(DAY FROM order_date);       -- 5
SELECT EXTRACT(HOUR FROM order_date);      -- 14
SELECT EXTRACT(MINUTE FROM order_date);    -- 35
SELECT EXTRACT(SECOND FROM order_date);    -- 42

-- Useful extras
SELECT EXTRACT(DOW FROM order_date);       -- 4 (0=Sun, 4=Thu)
SELECT EXTRACT(ISODOW FROM order_date);    -- 4 (1=Mon, 4=Thu)
SELECT EXTRACT(DOY FROM order_date);       -- 36 (day of year)
SELECT EXTRACT(WEEK FROM order_date);      -- 6 (ISO week number)
SELECT EXTRACT(QUARTER FROM order_date);   -- 1
SELECT EXTRACT(EPOCH FROM order_date);     -- Unix timestamp (seconds)
```

---

## AGE - Calculate Time Between Dates

```sql
-- AGE(end, start) returns interval
SELECT AGE('2026-03-15', '2025-01-10');
-- Result: 1 year 2 mons 5 days

-- AGE(timestamp) calculates from current date
SELECT AGE(birth_date) FROM users;
-- Result: 25 years 3 mons 12 days (example)

-- Get years as integer
SELECT EXTRACT(YEAR FROM AGE(birth_date)) AS age_years FROM users;
```

---

## TO_CHAR - Formatting Dates

Convert dates to formatted strings:

```sql
SELECT TO_CHAR(NOW(), 'YYYY-MM-DD');         -- 2026-02-05
SELECT TO_CHAR(NOW(), 'Month DD, YYYY');     -- February  05, 2026
SELECT TO_CHAR(NOW(), 'FMMonth DD, YYYY');   -- February 5, 2026 (FM removes padding)
SELECT TO_CHAR(NOW(), 'Day');                -- Thursday
SELECT TO_CHAR(NOW(), 'Dy');                 -- Thu
SELECT TO_CHAR(NOW(), 'HH24:MI:SS');         -- 14:35:42
SELECT TO_CHAR(NOW(), 'HH12:MI AM');         -- 02:35 PM
```

---

## TO_CHAR Format Codes

| Code    | Meaning                | Example  |
| ------- | ---------------------- | -------- |
| `YYYY`  | 4-digit year           | 2026     |
| `YY`    | 2-digit year           | 26       |
| `MM`    | Month number (01-12)   | 02       |
| `Mon`   | Abbreviated month      | Feb      |
| `Month` | Full month (padded)    | February |
| `DD`    | Day of month (01-31)   | 05       |
| `Day`   | Full day name (padded) | Thursday |
| `Dy`    | Abbreviated day        | Thu      |
| `HH24`  | Hour (00-23)           | 14       |
| `HH12`  | Hour (01-12)           | 02       |
| `MI`    | Minutes (00-59)        | 35       |
| `SS`    | Seconds (00-59)        | 42       |
| `AM`    | AM/PM indicator        | PM       |
| `TZ`    | Timezone abbreviation  | EST      |

> **Tip:** Prefix with `FM` to remove padding: `FMMonth` → "February" instead of "February "

---

## Parsing Strings to Dates

```sql
-- TO_DATE: string → DATE
SELECT TO_DATE('05-02-2026', 'DD-MM-YYYY');     -- 2026-02-05

-- TO_TIMESTAMP: string → TIMESTAMP
SELECT TO_TIMESTAMP('2026/02/05 14:30', 'YYYY/MM/DD HH24:MI');

-- Casting (requires ISO format YYYY-MM-DD)
SELECT '2026-02-05'::DATE;
SELECT '2026-02-05 14:30:00'::TIMESTAMP;
```

---

## Common Date Filtering Patterns

```sql
-- Last 7 days
WHERE created_at >= NOW() - INTERVAL '7 days'

-- This month
WHERE DATE_TRUNC('month', created_at) = DATE_TRUNC('month', NOW())

-- Last month (half-open range - more efficient with indexes!)
WHERE created_at >= DATE_TRUNC('month', NOW() - INTERVAL '1 month')
  AND created_at <  DATE_TRUNC('month', NOW())

-- Specific year
WHERE EXTRACT(YEAR FROM order_date) = 2026

-- Weekdays only (Monday=1 through Friday=5)
WHERE EXTRACT(ISODOW FROM order_date) BETWEEN 1 AND 5

-- Business hours
WHERE EXTRACT(HOUR FROM created_at) BETWEEN 9 AND 17
```

---

## Date Filtering Best Practices

❌ **Avoid functions on indexed columns:**

```sql
-- Bad: Can't use index on created_at
WHERE EXTRACT(YEAR FROM created_at) = 2026
WHERE DATE_TRUNC('day', created_at) = '2026-02-05'
```

✅ **Use range comparisons instead:**

```sql
-- Good: Uses index efficiently
WHERE created_at >= '2026-01-01' AND created_at < '2027-01-01'
WHERE created_at >= '2026-02-05' AND created_at < '2026-02-06'
```

> Half-open ranges `[start, end)` avoid boundary issues with timestamps

---

# Day 2 - Aggregation & Grouping

---

## Aggregate functions

- `COUNT(*)` counts rows (NULLs included); `COUNT(col)` skips NULLs
- `SUM`, `AVG`, `MIN`, `MAX` ignore NULLs
- Strings work with `MIN`/`MAX` alphabetically

```sql
SELECT COUNT(*) AS total, COUNT(shipped_at) AS shipped FROM orders;
SELECT SUM(score) FROM t; -- NULLs ignored, all NULL returns NULL (use COALESCE)
```

---

## Rounding & formatting

- `ROUND(num, 2)` for currency-ish outputs
- Guard all-NULL aggregates with `COALESCE`

```sql
SELECT
	ROUND(AVG(total_amount), 2) AS average_order_value,
	COALESCE(SUM(bonus), 0)     AS total_bonus
FROM orders;
```

---

## GROUP BY rules

- All non-aggregated columns must appear in `GROUP BY`
- `COUNT(*)` works with `GROUP BY`

```sql
SELECT category, COUNT(*) AS total
FROM products
GROUP BY category;  -- valid
```

---

## GROUP BY with multiple keys

```sql
-- Orders by status and payment method
SELECT
	status,
	payment_method,
	COUNT(*) AS order_count,
	SUM(total_amount) AS revenue
FROM orders
GROUP BY status, payment_method
HAVING SUM(total_amount) > 5000
ORDER BY revenue DESC;
```

- Remember every selected non-aggregate column must be grouped.

---

## HAVING vs WHERE

- `WHERE` filters rows before grouping
- `HAVING` filters groups after aggregation
- Alias from SELECT not available in HAVING in Postgres

```sql
-- Categories with >=5 products over $50
SELECT category, COUNT(*) AS product_count
FROM products
WHERE price > 50
GROUP BY category
HAVING COUNT(*) >= 5
ORDER BY product_count DESC;
```

---

## Execution order cheat sheet

```mermaid
flowchart LR
	A[FROM] --> B[WHERE]
	B --> C[GROUP BY]
	C --> D[HAVING]
	D --> E[SELECT]
	E --> F[DISTINCT]
	F --> G[ORDER BY]
	G --> H[LIMIT]
```

Use this to reason about alias visibility and filter placement.

---

## DISTINCT in aggregates

```sql
SELECT COUNT(DISTINCT customer_id) AS unique_customers
FROM orders
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

- `COUNT(DISTINCT col)` skips NULLs
- January 2026 unique customers: COUNT DISTINCT with half-open date

---

## Conditional aggregation

- Postgres `FILTER` or `CASE WHEN`

```sql
SELECT category_id,
	COUNT(*) AS total,
	COUNT(*) FILTER (WHERE is_active) AS active,
	COUNT(*) FILTER (WHERE NOT is_active) AS inactive
FROM products
GROUP BY category_id;
-- CASE alternative (portable):
COUNT(CASE WHEN is_active THEN 1 END) AS active
```

---

## Percentage / ratios

```sql
-- % shipped orders
SELECT COUNT(shipped_at) * 100.0 / COUNT(*) AS pct_shipped
FROM orders;
```

- Multiply before divide with `100.0` to force numeric division

---

## Finding duplicates

```sql
SELECT email, COUNT(*) AS occurrences
FROM users
GROUP BY email
HAVING COUNT(*) > 1;
```

---

## Unordered / never-ordered items

```sql
SELECT p.id, p.name
FROM products p
WHERE p.id NOT IN (
	SELECT DISTINCT product_id
	FROM order_items
	WHERE product_id IS NOT NULL
);
-- Or safer with NOT EXISTS
```

---

## Revenue by month with HAVING

```sql
SELECT
	EXTRACT(MONTH FROM order_date) AS month,
	SUM(amount) AS total_revenue,
	COUNT(*) AS order_count,
	AVG(amount) AS avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025
GROUP BY EXTRACT(MONTH FROM order_date)
HAVING SUM(amount) > 10000
ORDER BY month;
```

---

## Customer spend & order count

```sql
SELECT
	customer_id,
	COUNT(*) AS order_count,
	SUM(total) AS total_spent
FROM orders
GROUP BY customer_id
HAVING COUNT(*) > 5 AND SUM(total) > 500
ORDER BY total_spent DESC;
```

---

## Active users by age bucket

```sql
SELECT
	CASE
		WHEN age < 18 THEN 'under_18'
		WHEN age BETWEEN 18 AND 34 THEN '18_34'
		WHEN age BETWEEN 35 AND 54 THEN '35_54'
		ELSE '55_plus'
	END AS age_group,
	COUNT(*) AS users,
	AVG(purchase_total) AS avg_spending
FROM users
WHERE is_active = true
GROUP BY age_group
HAVING COUNT(*) > 100;
```

---

## CASE-insensitive literal underscore

Two valid patterns:

- `WHERE name LIKE '%!_%' ESCAPE '!'`
- `WHERE name LIKE '%[_]%'`

---

## IS DISTINCT FROM / NULL-safe equality

```sql
SELECT
	5 IS DISTINCT FROM NULL,  -- true
	5 IS NOT DISTINCT FROM 5; -- true
```

---

## Putting it together - requirement practice

```sql
-- Salary band, departments, non-null manager
SELECT * FROM employees
WHERE salary BETWEEN 40000 AND 80000
	AND department IN ('Engineering','Marketing')
	AND manager_id IS NOT NULL;
```

---

# Wrap up

- Filter smartly: handle NULLs, set operator precedence, escape wildcards
- Aggregate wisely: pick correct aggregates, place filters in WHERE vs HAVING, mind alias scope
- PostgreSQL niceties: `ILIKE`, regex (`~*`), `FILTER`, `IS DISTINCT FROM`, `DATE_TRUNC`

---

# Micro challenge (optional warm up)

Write a query that returns, for the last 30 days:

- total orders
- % shipped
- top 3 categories by revenue (name + revenue)
  Hint: CTE for last-30-days orders, aggregate, ORDER BY revenue DESC LIMIT 3.
