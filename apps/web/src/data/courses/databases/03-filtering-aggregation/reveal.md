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

## Date filtering & truncation

- `DATE_TRUNC('month', ts)` returns start of month
- `DATE_TRUNC('week', ts)` returns start of week (Sunday in Postgres)
- `NOW() - INTERVAL '7 days'` returns one week ago
- `AGE('2026-03-15', '2025-01-10')` returns interval (`1 year 2 mons 5 days`)

```sql
WHERE created_at >= NOW() - INTERVAL '7 days'
SELECT DATE_TRUNC('week', created_at) AS week_start FROM orders;
SELECT EXTRACT(MONTH FROM created_at) AS month_num;
SELECT TO_CHAR(created_at, 'Mon') AS month_name;
```

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
