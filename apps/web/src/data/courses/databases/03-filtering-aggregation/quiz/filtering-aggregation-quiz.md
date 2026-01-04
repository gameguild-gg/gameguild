# Quiz: Filtering & Aggregation (Week 03)

## Instructions

This quiz tests your understanding of **boolean logic**, **filtering operators**, **pattern matching**, **NULL handling**, **CASE expressions**, and **aggregate functions** in SQL.

---

## PART A: True or False

---

### Question 1

**In SQL's three-valued logic, `NULL = NULL` evaluates to `TRUE`.**

- [ ] True
- [ ] False

---

### Question 2

**The `HAVING` clause can be used without a `GROUP BY` clause in the query.**

- [ ] True
- [ ] False

---

### Question 3

**`COUNT(*)` and `COUNT(column_name)` always return the same result.**

- [ ] True
- [ ] False

---

### Question 4

**The `LIKE` operator in PostgreSQL is case-sensitive by default.**

- [ ] True
- [ ] False

---

### Question 5

**`BETWEEN 10 AND 20` includes both 10 and 20 in the results (inclusive range).**

- [ ] True
- [ ] False

---

### Question 6

**In `WHERE price > 100 OR category = 'Books' AND in_stock = true`, the `AND` is evaluated before the `OR`.**

- [ ] True
- [ ] False

---

### Question 7

**`AVG(column)` includes NULL values in its calculation.**

- [ ] True
- [ ] False

---

### Question 8

**You can use column aliases defined in `SELECT` within the `WHERE` clause of the same query.**

- [ ] True
- [ ] False

---

### Question 9

**`COALESCE(NULL, NULL, 'default')` returns `'default'`.**

- [ ] True
- [ ] False

---

### Question 10

**`NOT IN (1, 2, NULL)` will never return any rows, even if matching values exist.**

- [ ] True
- [ ] False

---

## PART B: Multiple Choice

---

### Question 11

**Which query correctly finds all products with names starting with "Pro" and ending with "Plus"?**

- [ ] A. `SELECT * FROM products WHERE name LIKE 'Pro%Plus';`
- [ ] B. `SELECT * FROM products WHERE name LIKE '%Pro%Plus%';`
- [ ] C. `SELECT * FROM products WHERE name LIKE 'Pro_Plus';`
- [ ] D. `SELECT * FROM products WHERE name = 'Pro*Plus';`

---

### Question 12

**Given a table `orders` with some NULL values in the `shipped_at` column:**

```sql
SELECT COUNT(*), COUNT(shipped_at) FROM orders;
-- Returns: 100, 75
```

**What can you conclude?**

- [ ] A. There are 100 shipped orders and 75 total orders
- [ ] B. There are 100 total orders and 25 have not been shipped yet (NULL shipped_at)
- [ ] C. There are 175 total orders in the table
- [ ] D. The query has an error because you can't use two COUNT functions

---

### Question 13

**Which query correctly finds users who have no phone number recorded?**

- [ ] A. `SELECT * FROM users WHERE phone = NULL;`
- [ ] B. `SELECT * FROM users WHERE phone == NULL;`
- [ ] C. `SELECT * FROM users WHERE phone IS NULL;`
- [ ] D. `SELECT * FROM users WHERE phone = '';`

---

### Question 14

**What does this query return?**

```sql
SELECT 
    category,
    COUNT(*) AS product_count
FROM products
WHERE price > 50
GROUP BY category
HAVING COUNT(*) >= 5
ORDER BY product_count DESC;
```

- [ ] A. All categories with their product counts, filtered to show only products over $50
- [ ] B. Categories that have at least 5 products priced over $50, sorted by count descending
- [ ] C. The top 5 categories by total product count
- [ ] D. Products grouped by category where the average price is over $50

---

### Question 15

**Which statement about `WHERE` vs `HAVING` is correct?**

- [ ] A. `WHERE` filters after grouping, `HAVING` filters before grouping
- [ ] B. `WHERE` filters individual rows before grouping, `HAVING` filters groups after aggregation
- [ ] C. `WHERE` and `HAVING` can be used interchangeably
- [ ] D. `HAVING` can only be used with `COUNT(*)`, not other aggregate functions

---

### Question 16

**What is the result of this expression: `NULLIF(100, 100)`?**

- [ ] A. `100`
- [ ] B. `0`
- [ ] C. `NULL`
- [ ] D. An error

---

### Question 17

**Which query correctly calculates the percentage of orders that have been shipped?**

```sql
-- Table: orders (id, status, shipped_at)
-- shipped_at is NULL for unshipped orders
```

- [ ] A. 
```sql
SELECT COUNT(shipped_at) / COUNT(*) * 100 AS pct_shipped FROM orders;
```

- [ ] B.
```sql
SELECT COUNT(shipped_at) * 100.0 / COUNT(*) AS pct_shipped FROM orders;
```

- [ ] C.
```sql
SELECT AVG(shipped_at) * 100 AS pct_shipped FROM orders;
```

- [ ] D.
```sql
SELECT SUM(shipped_at) / COUNT(*) * 100 AS pct_shipped FROM orders;
```

---

### Question 18

**What does `COALESCE(bonus, commission, 0)` return if `bonus` is 500, `commission` is 200?**

- [ ] A. `0`
- [ ] B. `200`
- [ ] C. `500`
- [ ] D. `700`

---

### Question 19

**Which pattern matches strings that have exactly 3 characters?**

- [ ] A. `LIKE '%%%'`
- [ ] B. `LIKE '___'`
- [ ] C. `LIKE '...'`
- [ ] D. `LIKE '[3]'`

---

### Question 20

**Given this query:**

```sql
SELECT 
    department,
    AVG(salary) AS avg_salary
FROM employees
GROUP BY department
HAVING AVG(salary) > 50000;
```

**Why can't we write `HAVING avg_salary > 50000` instead?**

- [ ] A. `HAVING` doesn't support column aliases
- [ ] B. `avg_salary` is a reserved keyword
- [ ] C. The alias `avg_salary` is created in `SELECT`, which executes after `HAVING`
- [ ] D. You must always repeat the aggregate function in `HAVING`

---

### Question 21

**Which query finds duplicate email addresses in the users table?**

- [ ] A.
```sql
SELECT email FROM users WHERE COUNT(email) > 1;
```

- [ ] B.
```sql
SELECT email, COUNT(*) FROM users GROUP BY email HAVING COUNT(*) > 1;
```

- [ ] C.
```sql
SELECT DISTINCT email FROM users WHERE email IS NOT NULL;
```

- [ ] D.
```sql
SELECT email FROM users GROUP BY email WHERE COUNT(*) > 1;
```

---

### Question 22

**What does this CASE expression return when `status` is 'pending'?**

```sql
CASE 
    WHEN status = 'completed' THEN 'Done'
    WHEN status = 'processing' THEN 'In Progress'
    ELSE 'Waiting'
END
```

- [ ] A. `'pending'`
- [ ] B. `NULL`
- [ ] C. `'Waiting'`
- [ ] D. An error because 'pending' is not handled

---

### Question 23

**Which query correctly counts how many unique customers placed orders in January 2026?**

- [ ] A.
```sql
SELECT COUNT(customer_id) FROM orders 
WHERE created_at BETWEEN '2026-01-01' AND '2026-01-31';
```

- [ ] B.
```sql
SELECT COUNT(DISTINCT customer_id) FROM orders 
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

- [ ] C.
```sql
SELECT DISTINCT COUNT(customer_id) FROM orders 
WHERE created_at BETWEEN '2026-01-01' AND '2026-01-31';
```

- [ ] D.
```sql
SELECT SUM(DISTINCT customer_id) FROM orders 
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

---

### Question 24

**What is wrong with this query?**

```sql
SELECT 
    category,
    product_name,
    COUNT(*) AS total
FROM products
GROUP BY category;
```

- [ ] A. `COUNT(*)` cannot be used with `GROUP BY`
- [ ] B. `product_name` is in SELECT but not in GROUP BY or an aggregate function
- [ ] C. `category` should be in the `HAVING` clause
- [ ] D. Nothing is wrong, this query is valid

---

### Question 25

**Which operator should you use for case-insensitive pattern matching in PostgreSQL?**

- [ ] A. `LIKE`
- [ ] B. `SIMILAR TO`
- [ ] C. `ILIKE`
- [ ] D. `LOWER LIKE`

---

## PART C: SQL Translation

---

### Question 26 — Requirement → SQL

**Requirement:** Find all employees whose salary is between $40,000 and $80,000 (inclusive), work in either the 'Engineering' or 'Marketing' department, and have a non-null manager_id.

**Which query is correct?**

- [ ] A.
```sql
SELECT * FROM employees
WHERE salary BETWEEN 40000 AND 80000
  AND department IN ('Engineering', 'Marketing')
  AND manager_id IS NOT NULL;
```

- [ ] B.
```sql
SELECT * FROM employees
WHERE salary >= 40000 OR salary <= 80000
  AND department = 'Engineering' OR department = 'Marketing'
  AND manager_id != NULL;
```

- [ ] C.
```sql
SELECT * FROM employees
WHERE salary BETWEEN 40000 AND 80000
  OR department IN ('Engineering', 'Marketing')
  OR manager_id IS NOT NULL;
```

- [ ] D.
```sql
SELECT * FROM employees
WHERE salary > 40000 AND salary < 80000
  AND department IN ('Engineering', 'Marketing')
  AND manager_id IS NOT NULL;
```

---

### Question 27 — SQL → Description

**What does this query do?**

```sql
SELECT 
    CASE 
        WHEN age < 18 THEN 'Minor'
        WHEN age < 65 THEN 'Adult'
        ELSE 'Senior'
    END AS age_group,
    COUNT(*) AS user_count,
    AVG(purchase_total) AS avg_spending
FROM users
WHERE is_active = true
GROUP BY 
    CASE 
        WHEN age < 18 THEN 'Minor'
        WHEN age < 65 THEN 'Adult'
        ELSE 'Senior'
    END
HAVING COUNT(*) > 100;
```

- [ ] A. Counts all users grouped by exact age, showing only ages with more than 100 users

- [ ] B. Categorizes active users into age groups, counts each group, calculates average spending, and only shows groups with more than 100 users

- [ ] C. Finds the top 100 users by spending in each age category

- [ ] D. Updates user age groups based on their purchase history

---

### Question 28 — Requirement → SQL

**Requirement:** Calculate the total revenue, number of orders, and average order value for each month in 2025, but only show months where total revenue exceeded $10,000.

**Which query is correct?**

- [ ] A.
```sql
SELECT 
    EXTRACT(MONTH FROM order_date) AS month,
    SUM(amount) AS total_revenue,
    COUNT(*) AS order_count,
    AVG(amount) AS avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025 AND SUM(amount) > 10000
GROUP BY EXTRACT(MONTH FROM order_date);
```

- [ ] B.
```sql
SELECT 
    EXTRACT(MONTH FROM order_date) AS month,
    SUM(amount) AS total_revenue,
    COUNT(*) AS order_count,
    AVG(amount) AS avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025
GROUP BY EXTRACT(MONTH FROM order_date)
HAVING SUM(amount) > 10000;
```

- [ ] C.
```sql
SELECT 
    EXTRACT(MONTH FROM order_date) AS month,
    SUM(amount) AS total_revenue,
    COUNT(*) AS order_count,
    AVG(amount) AS avg_order_value
FROM orders
GROUP BY EXTRACT(MONTH FROM order_date)
HAVING EXTRACT(YEAR FROM order_date) = 2025 AND SUM(amount) > 10000;
```

- [ ] D.
```sql
SELECT 
    EXTRACT(MONTH FROM order_date) AS month,
    total_revenue,
    order_count,
    avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025
HAVING total_revenue > 10000;
```

---

### Question 29 — SQL → Description

**What does this query find?**

```sql
SELECT product_id, product_name
FROM products
WHERE product_id NOT IN (
    SELECT DISTINCT product_id 
    FROM order_items 
    WHERE product_id IS NOT NULL
);
```

- [ ] A. Products that have been ordered at least once
- [ ] B. Products that have never been ordered
- [ ] C. All distinct products from order_items
- [ ] D. Products with NULL product_id values

---

### Question 30 — Requirement → SQL

**Requirement:** Find customers who have placed more than 5 orders AND have spent a total of more than $500, showing their name, order count, and total spent, sorted by total spent descending.

**Which query is correct?**

- [ ] A.
```sql
SELECT 
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total) AS total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
WHERE COUNT(o.id) > 5 AND SUM(o.total) > 500
GROUP BY c.id, c.name
ORDER BY total_spent DESC;
```

- [ ] B.
```sql
SELECT 
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total) AS total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name
HAVING COUNT(o.id) > 5 AND SUM(o.total) > 500
ORDER BY total_spent DESC;
```

- [ ] C.
```sql
SELECT 
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total) AS total_spent
FROM customers c, orders o
GROUP BY c.id, c.name
HAVING order_count > 5 AND total_spent > 500
ORDER BY total_spent DESC;
```

- [ ] D.
```sql
SELECT 
    c.name,
    order_count,
    total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
WHERE order_count > 5 AND total_spent > 500
ORDER BY total_spent DESC;
```

---

## PART D: Additional True or False

---

### Question 31

**`NOT (TRUE AND FALSE)` evaluates to `TRUE`.**

- [ ] True
- [ ] False

---

### Question 32

**`price NOT BETWEEN 10 AND 50` is equivalent to `price < 10 OR price > 50`.**

- [ ] True
- [ ] False

---

### Question 33

**In PostgreSQL, `'John' SIMILAR TO 'J%'` returns `TRUE`.**

- [ ] True
- [ ] False

---

### Question 34

**`NULL IS DISTINCT FROM NULL` returns `TRUE`.**

- [ ] True
- [ ] False

---

### Question 35

**`SUM(column)` returns `0` when all values in the column are `NULL`.**

- [ ] True
- [ ] False

---

### Question 36

**`MIN()` and `MAX()` can be used on string columns to find alphabetically first/last values.**

- [ ] True
- [ ] False

---

### Question 37

**`DATE_TRUNC('month', '2026-03-15 14:30:00')` returns `'2026-03-01 00:00:00'`.**

- [ ] True
- [ ] False

---

### Question 38

**The expression `NOW() - INTERVAL '7 days'` returns a date exactly one week ago.**

- [ ] True
- [ ] False

---

### Question 39

**In `WHERE name NOT LIKE '%test%'`, rows where `name` is `NULL` will be included in the results.**

- [ ] True
- [ ] False

---

### Question 40

**`COUNT(CASE WHEN status = 'active' THEN 1 END)` counts only rows where status is 'active'.**

- [ ] True
- [ ] False

---

## PART E: Additional Multiple Choice

---

### Question 41

**Which expression correctly searches for product names containing a literal underscore character?**

- [ ] A. `WHERE name LIKE '%_%'`
- [ ] B. `WHERE name LIKE '%\__%' ESCAPE '\'`
- [ ] C. `WHERE name LIKE '%!_%' ESCAPE '!'`
- [ ] D. `WHERE name LIKE '%[_]%'`

---

### Question 42

**What does the regex operator `~*` do in PostgreSQL?**

- [ ] A. Matches the pattern case-sensitively
- [ ] B. Matches the pattern case-insensitively
- [ ] C. Negates the regex match
- [ ] D. Performs a partial match only

---

### Question 43

**Given these values in column `score`: 10, 20, NULL, 30, NULL. What does `SUM(score)` return?**

- [ ] A. `NULL`
- [ ] B. `60`
- [ ] C. `12` (average)
- [ ] D. `0`

---

### Question 44

**Which query correctly finds the earliest and latest order dates?**

- [ ] A. `SELECT FIRST(order_date), LAST(order_date) FROM orders;`
- [ ] B. `SELECT MIN(order_date), MAX(order_date) FROM orders;`
- [ ] C. `SELECT EARLIEST(order_date), LATEST(order_date) FROM orders;`
- [ ] D. `SELECT order_date[0], order_date[-1] FROM orders;`

---

### Question 45

**What does this query return?**

```sql
SELECT 
    5 IS DISTINCT FROM NULL,
    NULL IS DISTINCT FROM NULL,
    5 IS NOT DISTINCT FROM 5;
```

- [ ] A. `TRUE, TRUE, TRUE`
- [ ] B. `TRUE, FALSE, TRUE`
- [ ] C. `NULL, NULL, TRUE`
- [ ] D. `TRUE, TRUE, FALSE`

---

### Question 46

**Which query correctly truncates timestamps to the start of each week?**

- [ ] A. `SELECT WEEK(created_at) FROM orders;`
- [ ] B. `SELECT DATE_TRUNC('week', created_at) FROM orders;`
- [ ] C. `SELECT EXTRACT(WEEK FROM created_at) FROM orders;`
- [ ] D. `SELECT TRUNCATE(created_at, 'week') FROM orders;`

---

### Question 47

**What does `AGE('2026-03-15', '2025-01-10')` return in PostgreSQL?**

- [ ] A. The number of days between the dates
- [ ] B. An interval representing the difference (e.g., '1 year 2 mons 5 days')
- [ ] C. The second date subtracted from the first as a timestamp
- [ ] D. An error because AGE requires timestamps

---

### Question 48

**Which query correctly counts active vs inactive products per category?**

- [ ] A.
```sql
SELECT category_id,
    COUNT(is_active = true) AS active,
    COUNT(is_active = false) AS inactive
FROM products GROUP BY category_id;
```

- [ ] B.
```sql
SELECT category_id,
    COUNT(CASE WHEN is_active = true THEN 1 END) AS active,
    COUNT(CASE WHEN is_active = false THEN 1 END) AS inactive
FROM products GROUP BY category_id;
```

- [ ] C.
```sql
SELECT category_id,
    SUM(is_active) AS active,
    SUM(NOT is_active) AS inactive
FROM products GROUP BY category_id;
```

- [ ] D.
```sql
SELECT category_id,
    COUNT(is_active) AS active,
    COUNT(NOT is_active) AS inactive
FROM products GROUP BY category_id;
```

---

### Question 49

**What does this PostgreSQL-specific query do?**

```sql
SELECT 
    category_id,
    COUNT(*) FILTER (WHERE price > 100) AS expensive_count,
    AVG(price) FILTER (WHERE in_stock = true) AS avg_available_price
FROM products
GROUP BY category_id;
```

- [ ] A. Filters the entire result set to only expensive, in-stock products
- [ ] B. Counts expensive products and averages prices of in-stock products separately per category
- [ ] C. Returns an error because FILTER is not valid SQL
- [ ] D. Creates two separate result sets and combines them

---

### Question 50

**Which pattern matches email addresses from any `.edu` domain using PostgreSQL regex?**

- [ ] A. `WHERE email LIKE '%@%.edu'`
- [ ] B. `WHERE email ~ '@.*\.edu$'`
- [ ] C. `WHERE email SIMILAR TO '%@%.edu'`
- [ ] D. `WHERE email ~* '@[a-z]+\.edu$'`

---

### Question 51

**What is the result of `NOT (NULL OR FALSE)`?**

- [ ] A. `TRUE`
- [ ] B. `FALSE`
- [ ] C. `NULL`
- [ ] D. An error

---

### Question 52

**Which query finds all orders placed in the last 30 days?**

- [ ] A. `SELECT * FROM orders WHERE order_date > DATE_SUB(NOW(), 30);`
- [ ] B. `SELECT * FROM orders WHERE order_date >= NOW() - INTERVAL '30 days';`
- [ ] C. `SELECT * FROM orders WHERE order_date BETWEEN NOW() - 30 AND NOW();`
- [ ] D. `SELECT * FROM orders WHERE DATEDIFF(NOW(), order_date) <= 30;`

---

### Question 53

**Given a products table, what does this query calculate?**

```sql
SELECT 
    MAX(price) - MIN(price) AS price_range,
    MAX(created_at) - MIN(created_at) AS time_span
FROM products;
```

- [ ] A. The difference between the highest and lowest prices, and the interval between newest and oldest products
- [ ] B. The total price and total time of all products
- [ ] C. An error because you cannot subtract dates
- [ ] D. The average price range and time span per product

---

### Question 54

**Which statement about `NOT LIKE` with NULL values is correct?**

- [ ] A. `NULL NOT LIKE '%test%'` returns `TRUE`
- [ ] B. `NULL NOT LIKE '%test%'` returns `FALSE`
- [ ] C. `NULL NOT LIKE '%test%'` returns `NULL`
- [ ] D. `NULL NOT LIKE '%test%'` causes an error

---

### Question 55

**What does `TO_CHAR(created_at, 'Day')` return for a date that falls on Monday?**

- [ ] A. `1`
- [ ] B. `'Mon'`
- [ ] C. `'Monday   '` (padded with spaces)
- [ ] D. `'MONDAY'`

---

---

## Answer Key (Instructor Only)

### Part A: True or False

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 1 | **False** | `NULL = NULL` returns `NULL` (unknown), not `TRUE`. Use `IS NULL` or `IS NOT DISTINCT FROM` |
| 2 | **True** | `HAVING` can be used without `GROUP BY` to filter the entire result set as a single group |
| 3 | **False** | `COUNT(*)` counts all rows; `COUNT(column)` counts only non-NULL values in that column |
| 4 | **True** | PostgreSQL's `LIKE` is case-sensitive. Use `ILIKE` for case-insensitive matching |
| 5 | **True** | `BETWEEN` is inclusive on both ends |
| 6 | **True** | `AND` has higher precedence than `OR`, so `AND` is evaluated first |
| 7 | **False** | `AVG()` ignores NULL values entirely (doesn't include them in sum or count) |
| 8 | **False** | `SELECT` is processed after `WHERE`, so aliases aren't available in `WHERE` |
| 9 | **True** | `COALESCE` returns the first non-NULL value, which is `'default'` |
| 10 | **True** | Any comparison with NULL in `NOT IN` returns NULL/unknown, causing no rows to match |

### Part B: Multiple Choice

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 11 | **A** | `'Pro%Plus'` matches "Pro" + any characters + "Plus" at the end |
| 12 | **B** | `COUNT(*)` = 100 total rows; `COUNT(shipped_at)` = 75 non-NULL values means 25 are NULL |
| 13 | **C** | `IS NULL` is the correct way to check for NULL values; `= NULL` doesn't work |
| 14 | **B** | WHERE filters rows (price > 50), GROUP BY groups, HAVING filters groups (count >= 5) |
| 15 | **B** | WHERE filters rows before grouping; HAVING filters groups after aggregation |
| 16 | **C** | `NULLIF(a, b)` returns NULL if a equals b; since 100 = 100, it returns NULL |
| 17 | **B** | Must use `100.0` to avoid integer division truncation; `COUNT(shipped_at)/COUNT(*)` gives ratio |
| 18 | **C** | `COALESCE` returns the first non-NULL value, which is `bonus` (500) |
| 19 | **B** | `_` matches exactly one character; `___` matches exactly three |
| 20 | **C** | Due to SQL execution order, `SELECT` runs after `HAVING`, so aliases aren't available |
| 21 | **B** | GROUP BY email, then HAVING COUNT(*) > 1 finds duplicates |
| 22 | **C** | 'pending' doesn't match any WHEN condition, so ELSE returns 'Waiting' |
| 23 | **B** | `COUNT(DISTINCT customer_id)` counts unique customers; proper date range with `< '2026-02-01'` |
| 24 | **B** | Non-aggregated columns in SELECT must appear in GROUP BY |
| 25 | **C** | `ILIKE` is PostgreSQL's case-insensitive version of `LIKE` |

### Part C: SQL Translation

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 26 | **A** | Uses `BETWEEN`, `IN`, and `IS NOT NULL` correctly with `AND` logic |
| 27 | **B** | Groups active users by age category, counts them, calculates avg spending, filters by count > 100 |
| 28 | **B** | `WHERE` filters to 2025, `GROUP BY` month, `HAVING` filters aggregated revenue > 10000 |
| 29 | **B** | `NOT IN (subquery)` finds products whose IDs are not in order_items = never ordered |
| 30 | **B** | Uses `JOIN`, `GROUP BY`, and `HAVING` with aggregate conditions; can use alias in `ORDER BY` |

### Part D: Additional True or False

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 31 | **True** | `TRUE AND FALSE` = `FALSE`; `NOT FALSE` = `TRUE` |
| 32 | **True** | `NOT BETWEEN` excludes the range, meaning values less than 10 or greater than 50 |
| 33 | **True** | `SIMILAR TO` uses SQL regex; `J%` matches "J" followed by any characters |
| 34 | **False** | `IS DISTINCT FROM` treats NULLs as equal, so `NULL IS DISTINCT FROM NULL` = `FALSE` |
| 35 | **False** | `SUM()` returns `NULL` (not 0) when all values are NULL. Use `COALESCE(SUM(col), 0)` |
| 36 | **True** | `MIN()` returns alphabetically first, `MAX()` returns alphabetically last string |
| 37 | **True** | `DATE_TRUNC('month', ...)` truncates to the first day of the month at midnight |
| 38 | **True** | `INTERVAL` arithmetic works with `NOW()` to calculate relative timestamps |
| 39 | **False** | `NOT LIKE` with NULL returns NULL (unknown), so NULL rows are excluded |
| 40 | **True** | CASE returns 1 only for 'active' rows; COUNT ignores NULL (non-matching rows) |

### Part E: Additional Multiple Choice

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 41 | **C** | `ESCAPE '!'` defines `!` as escape char, so `!_` matches literal underscore |
| 42 | **B** | `~*` is PostgreSQL's case-insensitive regex match operator |
| 43 | **B** | `SUM()` ignores NULLs: 10 + 20 + 30 = 60 |
| 44 | **B** | `MIN()` and `MAX()` work on dates to find earliest/latest |
| 45 | **B** | 5 is distinct from NULL (TRUE), NULL equals NULL in IS DISTINCT (FALSE), 5 equals 5 (TRUE) |
| 46 | **B** | `DATE_TRUNC('week', timestamp)` truncates to the start of the week |
| 47 | **B** | `AGE()` returns an interval type showing years, months, and days difference |
| 48 | **B** | `CASE WHEN ... THEN 1 END` returns 1 or NULL; `COUNT` counts the 1s only |
| 49 | **B** | `FILTER (WHERE ...)` applies different conditions to each aggregate function |
| 50 | **B** | `~ '@.*\.edu$'` uses regex: @ + any chars + literal .edu at end (case-sensitive) |
| 51 | **C** | `NULL OR FALSE` = `NULL`; `NOT NULL` = `NULL` |
| 52 | **B** | PostgreSQL uses `INTERVAL '30 days'` syntax for date arithmetic |
| 53 | **A** | `MAX - MIN` gives range for numbers; date subtraction gives an interval |
| 54 | **C** | Any comparison (including NOT LIKE) with NULL returns NULL |
| 55 | **C** | `TO_CHAR` with 'Day' returns full weekday name, padded to 9 characters |
