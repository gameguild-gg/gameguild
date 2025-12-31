# Advanced WHERE & Pattern Matching

The `WHERE` clause is your primary tool for filtering data in SQL. This lesson covers advanced filtering techniques including boolean logic, pattern matching, range queries, NULL handling, and conditional expressions.

![WHERE meme](https://i.programmerhumor.io/2023/02/programmerhumor-io-databases-memes-programming-memes-5b64f9619e9f8f9.jpg)

![Filtering meme](https://i.programmerhumor.io/2021/06/programmerhumor-io-databases-memes-backend-memes-baf0e59b14adbdf.jpg)

---

## Boolean Logic

SQL uses three-valued logic: **TRUE**, **FALSE**, and **NULL** (unknown). Boolean operators combine conditions to create complex filters.

### AND Operator

Returns TRUE only when **all** conditions are TRUE.

```sql
-- Find active premium users
SELECT * FROM users
WHERE is_active = true AND subscription_tier = 'premium';

-- Multiple AND conditions
SELECT * FROM products
WHERE category = 'Electronics'
  AND price > 100
  AND stock_quantity > 0;
```

**Truth Table for AND:**

| A | B | A AND B |
|---|---|---------|
| TRUE | TRUE | TRUE |
| TRUE | FALSE | FALSE |
| FALSE | TRUE | FALSE |
| FALSE | FALSE | FALSE |
| TRUE | NULL | NULL |
| NULL | TRUE | NULL |
| FALSE | NULL | FALSE |
| NULL | FALSE | FALSE |
| NULL | NULL | NULL |

### OR Operator

Returns TRUE when **any** condition is TRUE.

```sql
-- Find users who are admins or moderators
SELECT * FROM users
WHERE role = 'admin' OR role = 'moderator';

-- Find products on sale or low stock
SELECT * FROM products
WHERE discount_percent > 0 OR stock_quantity < 10;
```

**Truth Table for OR:**

| A | B | A OR B |
|---|---|--------|
| TRUE | TRUE | TRUE |
| TRUE | FALSE | TRUE |
| FALSE | TRUE | TRUE |
| FALSE | FALSE | FALSE |
| TRUE | NULL | TRUE |
| NULL | TRUE | TRUE |
| FALSE | NULL | NULL |
| NULL | FALSE | NULL |
| NULL | NULL | NULL |

### NOT Operator

Negates a condition.

```sql
-- Find all non-admin users
SELECT * FROM users
WHERE NOT role = 'admin';

-- Equivalent to:
SELECT * FROM users
WHERE role != 'admin';  -- or role <> 'admin'

-- NOT with other operators
SELECT * FROM products
WHERE NOT (price > 100 AND category = 'Electronics');
```

**Truth Table for NOT:**

| A | NOT A |
|---|-------|
| TRUE | FALSE |
| FALSE | TRUE |
| NULL | NULL |

### Operator Precedence

SQL evaluates operators in this order (highest to lowest):

1. `NOT`
2. `AND`
3. `OR`

**Use parentheses to control evaluation order!**

```sql
-- Without parentheses (AND evaluated first)
SELECT * FROM products
WHERE category = 'Books' OR category = 'Music' AND price < 20;
-- Interpreted as: Books OR (Music AND price < 20)

-- With parentheses (explicit grouping)
SELECT * FROM products
WHERE (category = 'Books' OR category = 'Music') AND price < 20;
-- Interpreted as: (Books OR Music) AND price < 20
```

⚠️ **Always use parentheses when mixing AND and OR** to make your intent clear.

---

## The IN Operator

`IN` checks if a value matches any value in a list. It's cleaner than multiple OR conditions.

```sql
-- Instead of:
SELECT * FROM users
WHERE role = 'admin' OR role = 'moderator' OR role = 'editor';

-- Use IN:
SELECT * FROM users
WHERE role IN ('admin', 'moderator', 'editor');
```

### NOT IN

```sql
-- Exclude certain statuses
SELECT * FROM orders
WHERE status NOT IN ('cancelled', 'refunded');
```

### IN with Subqueries

```sql
-- Find users who have placed orders
SELECT * FROM users
WHERE id IN (SELECT DISTINCT user_id FROM orders);

-- Find products never ordered
SELECT * FROM products
WHERE id NOT IN (
    SELECT DISTINCT product_id 
    FROM order_items 
    WHERE product_id IS NOT NULL
);
```

⚠️ **Warning:** `NOT IN` with NULL values can cause unexpected results:
```sql
-- If subquery returns: (1, 2, NULL)
-- NOT IN (1, 2, NULL) is always NULL (unknown), returning no rows!
-- Use NOT EXISTS instead when NULLs are possible
```

---

## The BETWEEN Operator

`BETWEEN` checks if a value is within an **inclusive** range.

```sql
-- Price between $10 and $50 (inclusive)
SELECT * FROM products
WHERE price BETWEEN 10 AND 50;

-- Equivalent to:
SELECT * FROM products
WHERE price >= 10 AND price <= 50;
```

### Date Ranges

```sql
-- Orders from January 2026
SELECT * FROM orders
WHERE created_at BETWEEN '2026-01-01' AND '2026-01-31';

-- Be careful with timestamps!
-- '2026-01-31' means '2026-01-31 00:00:00'
-- Use explicit time or next day:
SELECT * FROM orders
WHERE created_at BETWEEN '2026-01-01' AND '2026-02-01';
-- Or:
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

### NOT BETWEEN

```sql
-- Products outside the $10-$50 range
SELECT * FROM products
WHERE price NOT BETWEEN 10 AND 50;
```

---

## Pattern Matching with LIKE

`LIKE` performs pattern matching on strings using wildcards.

### Wildcards

| Wildcard | Meaning | Example |
|----------|---------|---------|
| `%` | Zero or more characters | `'Jo%'` matches "Jo", "John", "Jones" |
| `_` | Exactly one character | `'J_n'` matches "Jon", "Jan", "Jen" |

### Examples

```sql
-- Names starting with 'A'
SELECT * FROM users WHERE first_name LIKE 'A%';

-- Names ending with 'son'
SELECT * FROM users WHERE last_name LIKE '%son';

-- Names containing 'ann'
SELECT * FROM users WHERE first_name LIKE '%ann%';

-- Exactly 5-character names
SELECT * FROM users WHERE first_name LIKE '_____';

-- Second character is 'a'
SELECT * FROM users WHERE first_name LIKE '_a%';

-- Email from specific domain
SELECT * FROM users WHERE email LIKE '%@gmail.com';
```

### Case Sensitivity

`LIKE` is **case-sensitive** in PostgreSQL!

```sql
-- Case-sensitive (won't match 'JOHN' or 'john')
SELECT * FROM users WHERE first_name LIKE 'John%';

-- Case-insensitive with ILIKE (PostgreSQL)
SELECT * FROM users WHERE first_name ILIKE 'john%';

-- Case-insensitive with LOWER()
SELECT * FROM users WHERE LOWER(first_name) LIKE 'john%';
```

### Escaping Wildcards

To search for literal `%` or `_` characters:

```sql
-- Find values containing literal '%'
SELECT * FROM discounts WHERE description LIKE '%\%%' ESCAPE '\';

-- Or use a different escape character
SELECT * FROM products WHERE sku LIKE '%!_%' ESCAPE '!';
```

### NOT LIKE

```sql
-- Users not from Gmail
SELECT * FROM users WHERE email NOT LIKE '%@gmail.com';
```

---

## SIMILAR TO and Regular Expressions

PostgreSQL supports more powerful pattern matching:

### SIMILAR TO (SQL Standard)

Combines LIKE and regex:

```sql
-- Match 'cat' or 'dog'
SELECT * FROM pets WHERE species SIMILAR TO '(cat|dog)';

-- Match any digit
SELECT * FROM products WHERE sku SIMILAR TO '[0-9]%';
```

### POSIX Regular Expressions

```sql
-- ~ is case-sensitive match
SELECT * FROM users WHERE email ~ '^[a-z]+@';

-- ~* is case-insensitive match
SELECT * FROM users WHERE first_name ~* '^(john|jane)$';

-- !~ is negated match
SELECT * FROM users WHERE email !~ '@gmail\.com$';
```

| Operator | Description |
|----------|-------------|
| `~` | Matches regex (case-sensitive) |
| `~*` | Matches regex (case-insensitive) |
| `!~` | Does not match (case-sensitive) |
| `!~*` | Does not match (case-insensitive) |

---

## NULL Handling

NULL represents **unknown** or **missing** data. It's not equal to anything, not even itself!

### IS NULL / IS NOT NULL

```sql
-- Find users without phone numbers
SELECT * FROM users WHERE phone IS NULL;

-- Find users with phone numbers
SELECT * FROM users WHERE phone IS NOT NULL;

-- WRONG - this never matches!
SELECT * FROM users WHERE phone = NULL;  -- Always returns no rows
```

### NULL in Comparisons

Any comparison with NULL returns NULL (unknown):

```sql
-- These all return NULL, not TRUE or FALSE:
SELECT NULL = NULL;      -- NULL
SELECT NULL != NULL;     -- NULL
SELECT NULL > 5;         -- NULL
SELECT 5 + NULL;         -- NULL
```

### COALESCE

Returns the first non-NULL value:

```sql
-- Display 'N/A' if phone is NULL
SELECT first_name, COALESCE(phone, 'N/A') AS phone
FROM users;

-- Use fallback values
SELECT COALESCE(nickname, first_name, 'Anonymous') AS display_name
FROM users;
```

### NULLIF

Returns NULL if two values are equal:

```sql
-- Avoid division by zero
SELECT total / NULLIF(count, 0) AS average
FROM stats;

-- Convert empty strings to NULL
SELECT NULLIF(middle_name, '') AS middle_name
FROM users;
```

### NULL-safe Comparison

```sql
-- IS DISTINCT FROM (treats NULLs as equal)
SELECT * FROM users
WHERE phone IS DISTINCT FROM '555-0100';

-- IS NOT DISTINCT FROM
SELECT * FROM users  
WHERE phone IS NOT DISTINCT FROM NULL;  -- Same as IS NULL
```

---

## CASE Expressions

`CASE` provides conditional logic within queries. It's like if/else for SQL.

### Simple CASE

Compare one expression to multiple values:

```sql
SELECT 
    order_id,
    status,
    CASE status
        WHEN 'pending' THEN 'Awaiting Payment'
        WHEN 'paid' THEN 'Processing'
        WHEN 'shipped' THEN 'On the Way'
        WHEN 'delivered' THEN 'Complete'
        ELSE 'Unknown'
    END AS status_label
FROM orders;
```

### Searched CASE

Use any boolean expressions:

```sql
SELECT 
    product_name,
    price,
    CASE
        WHEN price < 10 THEN 'Budget'
        WHEN price < 50 THEN 'Mid-Range'
        WHEN price < 100 THEN 'Premium'
        ELSE 'Luxury'
    END AS price_tier
FROM products;
```

### CASE in ORDER BY

```sql
-- Custom sort order
SELECT * FROM orders
ORDER BY 
    CASE status
        WHEN 'pending' THEN 1
        WHEN 'processing' THEN 2
        WHEN 'shipped' THEN 3
        WHEN 'delivered' THEN 4
        ELSE 5
    END;
```

### CASE in WHERE

```sql
-- Conditional filtering
SELECT * FROM products
WHERE 
    CASE 
        WHEN category = 'Electronics' THEN price > 100
        WHEN category = 'Books' THEN price > 20
        ELSE price > 50
    END;
```

### CASE for Aggregation

```sql
-- Count by category
SELECT 
    COUNT(CASE WHEN status = 'active' THEN 1 END) AS active_count,
    COUNT(CASE WHEN status = 'inactive' THEN 1 END) AS inactive_count,
    COUNT(CASE WHEN status = 'pending' THEN 1 END) AS pending_count
FROM users;
```

---

## Date/Time Functions

### Current Date and Time

```sql
SELECT 
    CURRENT_DATE,           -- 2026-01-26
    CURRENT_TIME,           -- 14:30:00.123456-05:00
    CURRENT_TIMESTAMP,      -- 2026-01-26 14:30:00.123456-05:00
    NOW(),                  -- Same as CURRENT_TIMESTAMP
    LOCALTIME,              -- Time without timezone
    LOCALTIMESTAMP;         -- Timestamp without timezone
```

### Extracting Date Parts

```sql
SELECT
    created_at,
    EXTRACT(YEAR FROM created_at) AS year,
    EXTRACT(MONTH FROM created_at) AS month,
    EXTRACT(DAY FROM created_at) AS day,
    EXTRACT(HOUR FROM created_at) AS hour,
    EXTRACT(DOW FROM created_at) AS day_of_week,  -- 0=Sunday
    EXTRACT(DOY FROM created_at) AS day_of_year
FROM orders;

-- DATE_PART is equivalent
SELECT DATE_PART('month', created_at) FROM orders;
```

### Date Truncation

```sql
SELECT
    DATE_TRUNC('year', created_at) AS year_start,
    DATE_TRUNC('month', created_at) AS month_start,
    DATE_TRUNC('week', created_at) AS week_start,
    DATE_TRUNC('day', created_at) AS day_start,
    DATE_TRUNC('hour', created_at) AS hour_start
FROM orders;
```

### Date Arithmetic

```sql
-- Add/subtract intervals
SELECT 
    NOW() + INTERVAL '1 day' AS tomorrow,
    NOW() - INTERVAL '1 week' AS last_week,
    NOW() + INTERVAL '3 months' AS three_months_later,
    NOW() - INTERVAL '2 hours 30 minutes' AS earlier;

-- Age between dates
SELECT AGE(NOW(), created_at) AS account_age FROM users;

-- Days between dates
SELECT created_at::date - '2026-01-01'::date AS days_since_jan1
FROM orders;
```

### Filtering by Date

```sql
-- Orders from today
SELECT * FROM orders
WHERE DATE(created_at) = CURRENT_DATE;

-- Orders from this month
SELECT * FROM orders
WHERE DATE_TRUNC('month', created_at) = DATE_TRUNC('month', CURRENT_DATE);

-- Orders from last 7 days
SELECT * FROM orders
WHERE created_at >= NOW() - INTERVAL '7 days';

-- Orders from specific year
SELECT * FROM orders
WHERE EXTRACT(YEAR FROM created_at) = 2026;
```

### Date Formatting

```sql
SELECT 
    TO_CHAR(created_at, 'YYYY-MM-DD') AS iso_date,
    TO_CHAR(created_at, 'Month DD, YYYY') AS long_date,
    TO_CHAR(created_at, 'HH12:MI AM') AS time_12h,
    TO_CHAR(created_at, 'Day') AS weekday_name
FROM orders;
```

---

## Combining Techniques

Real-world queries often combine multiple filtering techniques:

```sql
-- Complex product search
SELECT 
    p.name,
    p.price,
    c.name AS category,
    CASE 
        WHEN p.stock_quantity = 0 THEN 'Out of Stock'
        WHEN p.stock_quantity < 10 THEN 'Low Stock'
        ELSE 'In Stock'
    END AS availability
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE 
    p.is_active = true
    AND p.price BETWEEN 20 AND 100
    AND c.name IN ('Electronics', 'Books', 'Games')
    AND p.name ILIKE '%wireless%'
    AND p.created_at >= NOW() - INTERVAL '1 year'
    AND p.deleted_at IS NULL
ORDER BY 
    CASE WHEN p.stock_quantity > 0 THEN 0 ELSE 1 END,
    p.price;
```

---

## Practice Exercises

### Exercise 1: Boolean Logic
Write queries to find:
1. Products that are either in 'Electronics' category AND cost over $50, OR in 'Books' category AND cost under $20
2. Users who signed up in 2025 AND have NOT made any purchases
3. Orders that are NOT (cancelled OR refunded) AND were placed in the last 30 days

### Exercise 2: Pattern Matching
Find:
1. All users with Gmail addresses (case-insensitive)
2. Products with SKUs starting with "ELEC-" followed by exactly 4 digits
3. Users whose names contain exactly two words (first and last name with single space)

### Exercise 3: NULL Handling
1. Find all orders where the shipping address is not set
2. Display users with their phone numbers, showing "No phone" for NULLs
3. Find products where the description is either NULL or empty string

### Exercise 4: CASE Expressions
1. Create a query that labels orders as 'New' (< 7 days), 'Recent' (7-30 days), or 'Old' (> 30 days)
2. Count how many products are in each price tier (Budget/Mid-Range/Premium/Luxury)
3. Sort users by subscription tier: 'enterprise' first, then 'premium', then 'free'

### Exercise 5: Date Filtering
1. Find all orders placed on a Monday
2. Find users who signed up in the same month as they made their first purchase
3. Calculate the average order value per month for the last 12 months

---

## Key Takeaways

1. **Use parentheses** when combining AND and OR to control precedence
2. **IN** is cleaner than multiple OR conditions
3. **BETWEEN** is inclusive on both ends
4. **LIKE** is case-sensitive; use **ILIKE** for case-insensitive matching
5. **NULL requires special handling** — use IS NULL, not = NULL
6. **COALESCE** handles NULL values gracefully
7. **CASE** provides conditional logic within queries
8. **Date functions** are essential for time-based filtering

---

## Quick Reference

```sql
-- Boolean
WHERE a AND b              -- Both true
WHERE a OR b               -- Either true
WHERE NOT a                -- Negation

-- Lists
WHERE x IN (1, 2, 3)       -- Match any
WHERE x NOT IN (1, 2, 3)   -- Match none

-- Ranges
WHERE x BETWEEN 1 AND 10   -- Inclusive range

-- Pattern Matching
WHERE x LIKE 'A%'          -- Starts with A
WHERE x ILIKE '%test%'     -- Contains test (case-insensitive)

-- NULL
WHERE x IS NULL            -- Is NULL
WHERE x IS NOT NULL        -- Is not NULL
COALESCE(x, 'default')     -- First non-NULL

-- CASE
CASE WHEN x > 10 THEN 'high' ELSE 'low' END

-- Dates
WHERE created_at >= NOW() - INTERVAL '7 days'
EXTRACT(MONTH FROM date_column)
DATE_TRUNC('month', timestamp_column)
```
