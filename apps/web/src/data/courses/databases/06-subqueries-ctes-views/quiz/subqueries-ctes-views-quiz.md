# Quiz: Subqueries, CTEs & Views (Week 06)

## Instructions

This quiz tests your understanding of **subqueries** (scalar, IN, EXISTS, ANY/ALL, correlated), **set operations** (UNION, INTERSECT, EXCEPT), **Common Table Expressions (CTEs)**, **recursive CTEs**, **views**, and **materialized views**.

---

## PART A: True or False

---

### Question 1

**A scalar subquery must return exactly one row and one column.**

- [ ] True
- [ ] False

---

### Question 2

**`NOT IN` and `NOT EXISTS` always produce the same results for the same data.**

- [ ] True
- [ ] False

---

### Question 3

**A correlated subquery executes once for each row in the outer query.**

- [ ] True
- [ ] False

---

### Question 4

**`UNION` removes duplicate rows from the combined result, while `UNION ALL` keeps all rows including duplicates.**

- [ ] True
- [ ] False

---

### Question 5

**CTEs (Common Table Expressions) persist after the query completes and can be used in subsequent queries.**

- [ ] True
- [ ] False

---

### Question 6

**A recursive CTE must have both an anchor member and a recursive member connected by `UNION ALL`.**

- [ ] True
- [ ] False

---

### Question 7

**Regular views store the query result data physically on disk.**

- [ ] True
- [ ] False

---

### Question 8

**All views are updatable — you can always INSERT, UPDATE, or DELETE through a view.**

- [ ] True
- [ ] False

---

### Question 9

**Materialized views automatically refresh whenever the underlying data changes.**

- [ ] True
- [ ] False

---

### Question 10

**`= ANY (subquery)` is equivalent to `IN (subquery)`.**

- [ ] True
- [ ] False

---

### Question 11

**`> ALL (subquery)` returns TRUE if the value is greater than the maximum value in the subquery result.**

- [ ] True
- [ ] False

---

### Question 12

**Derived tables (subqueries in FROM clause) must have an alias.**

- [ ] True
- [ ] False

---

### Question 13

**`EXCEPT` returns rows that appear in the first query but not in the second query.**

- [ ] True
- [ ] False

---

### Question 14

**`WITH CHECK OPTION` on a view prevents inserts/updates that would make the row disappear from the view.**

- [ ] True
- [ ] False

---

### Question 15

**You can create indexes on materialized views to improve query performance.**

- [ ] True
- [ ] False

---

---

## PART B: Multiple Choice

---

### Question 16

**Which query correctly finds all products priced above the average price?**

- [ ] A.
```sql
SELECT * FROM products WHERE price > AVG(price);
```

- [ ] B.
```sql
SELECT * FROM products WHERE price > (SELECT AVG(price) FROM products);
```

- [ ] C.
```sql
SELECT * FROM products HAVING price > AVG(price);
```

- [ ] D.
```sql
SELECT * FROM products GROUP BY id HAVING price > AVG(price);
```

---

### Question 17

**What happens when this query runs if the subquery returns multiple rows?**

```sql
SELECT * FROM products
WHERE price > (SELECT price FROM products WHERE category_id = 1);
```

- [ ] A. It returns all products priced above the maximum price in category 1
- [ ] B. It returns all products priced above the minimum price in category 1
- [ ] C. It returns an error because the scalar subquery returns multiple rows
- [ ] D. It returns no rows

---

### Question 18

**Why does `NOT IN` with NULL values cause problems?**

```sql
SELECT * FROM customers
WHERE id NOT IN (SELECT customer_id FROM orders);
-- If orders.customer_id contains NULL...
```

- [ ] A. NULL values are automatically excluded from the subquery
- [ ] B. Comparison with NULL yields UNKNOWN, making the entire NOT IN return no rows
- [ ] C. The query throws a syntax error
- [ ] D. NULL values are treated as 0

---

### Question 19

**Which is the safer alternative to `NOT IN` when NULLs may be present?**

- [ ] A.
```sql
WHERE id NOT IN (SELECT col FROM t)
```

- [ ] B.
```sql
WHERE id != ALL (SELECT col FROM t)
```

- [ ] C.
```sql
WHERE NOT EXISTS (SELECT 1 FROM t WHERE t.fk = outer.id)
```

- [ ] D.
```sql
WHERE id <> ANY (SELECT col FROM t)
```

---

### Question 20

**What is the difference between a correlated and non-correlated subquery?**

- [ ] A. Correlated subqueries use JOINs; non-correlated use WHERE
- [ ] B. Correlated subqueries reference the outer query and run once per row; non-correlated run once independently
- [ ] C. Correlated subqueries are faster than non-correlated
- [ ] D. There is no difference; they are the same

---

### Question 21

**What does `INTERSECT` return?**

```sql
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;
```

- [ ] A. All emails from customers and employees combined
- [ ] B. Emails that appear in customers but not in employees
- [ ] C. Emails that appear in both customers AND employees
- [ ] D. Emails that appear in employees but not in customers

---

### Question 22

**Which set operation is fastest because it doesn't need to check for duplicates?**

- [ ] A. UNION
- [ ] B. UNION ALL
- [ ] C. INTERSECT
- [ ] D. EXCEPT

---

### Question 23

**What is the purpose of a CTE (Common Table Expression)?**

- [ ] A. To permanently store query results in the database
- [ ] B. To create a temporary named result set that improves query readability and can be referenced multiple times
- [ ] C. To replace all JOINs in a query
- [ ] D. To automatically optimize query performance

---

### Question 24

**Which CTE syntax is correct for defining multiple CTEs?**

- [ ] A.
```sql
WITH cte1 AS (...) WITH cte2 AS (...) SELECT ...
```

- [ ] B.
```sql
WITH cte1 AS (...), cte2 AS (...) SELECT ...
```

- [ ] C.
```sql
WITH (cte1 AS (...) AND cte2 AS (...)) SELECT ...
```

- [ ] D.
```sql
WITH cte1 AS (...); WITH cte2 AS (...); SELECT ...
```

---

### Question 25

**In a recursive CTE, what is the "anchor member"?**

- [ ] A. The part that references the CTE itself
- [ ] B. The base case that provides initial rows and doesn't reference the CTE
- [ ] C. The ORDER BY clause
- [ ] D. The final SELECT statement

---

### Question 26

**What does this recursive CTE produce?**

```sql
WITH RECURSIVE nums AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM nums WHERE n < 5
)
SELECT n FROM nums;
```

- [ ] A. The number 1 repeated 5 times
- [ ] B. The numbers 1, 2, 3, 4, 5
- [ ] C. An infinite loop
- [ ] D. An error because recursion is not allowed

---

### Question 27

**How do you prevent infinite loops in recursive CTEs?**

- [ ] A. Use UNION instead of UNION ALL
- [ ] B. Add a WHERE condition that eventually becomes false (depth limit)
- [ ] C. Recursive CTEs automatically stop after 100 iterations
- [ ] D. You cannot prevent infinite loops; they always run forever

---

### Question 28

**Which statement about views is correct?**

- [ ] A. Views store data physically like tables
- [ ] B. Views run their underlying query each time they are accessed
- [ ] C. Views cannot be used with JOINs
- [ ] D. Views must contain aggregate functions

---

### Question 29

**What makes a view "updatable" (allowing INSERT/UPDATE/DELETE)?**

- [ ] A. Using WITH CHECK OPTION
- [ ] B. Being based on a single table without DISTINCT, GROUP BY, aggregates, or subqueries
- [ ] C. Creating an index on the view
- [ ] D. Explicitly declaring it with CREATE UPDATABLE VIEW

---

### Question 30

**What is the key difference between a view and a materialized view?**

- [ ] A. Views can only SELECT; materialized views can INSERT
- [ ] B. Materialized views store data physically and require refresh; views execute the query each time
- [ ] C. Materialized views are automatically updated; views require manual refresh
- [ ] D. There is no difference; they are the same

---

### Question 31

**When should you use a materialized view instead of a regular view?**

- [ ] A. When the underlying query is simple
- [ ] B. When you need real-time data accuracy
- [ ] C. When the underlying query is expensive and data freshness can be slightly delayed
- [ ] D. When you need to hide columns for security

---

### Question 32

**How do you update data in a materialized view?**

- [ ] A. Use UPDATE statement directly on the materialized view
- [ ] B. Use REFRESH MATERIALIZED VIEW to rebuild the data
- [ ] C. Data updates automatically when underlying tables change
- [ ] D. Delete and recreate the materialized view

---

### Question 33

**What does `REFRESH MATERIALIZED VIEW CONCURRENTLY` do differently?**

- [ ] A. Refreshes faster by skipping some rows
- [ ] B. Allows reads during refresh (doesn't lock the view)
- [ ] C. Refreshes multiple views at once
- [ ] D. Schedules the refresh for later

---

---

## PART C: SQL Translation

---

### Question 34 — Requirement → SQL

**Requirement:** Find customers who have placed at least one order (use EXISTS).

**Which query is correct?**

- [ ] A.
```sql
SELECT * FROM customers c
WHERE EXISTS (SELECT * FROM orders o);
```

- [ ] B.
```sql
SELECT * FROM customers c
WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id);
```

- [ ] C.
```sql
SELECT * FROM customers c
WHERE IN (SELECT customer_id FROM orders);
```

- [ ] D.
```sql
SELECT * FROM customers c
WHERE o.customer_id EXISTS (SELECT 1 FROM orders o);
```

---

### Question 35 — SQL → Description

**What does this query find?**

```sql
SELECT * FROM products
WHERE price > ALL (SELECT price FROM products WHERE category_id = 5);
```

- [ ] A. Products priced above the average price in category 5
- [ ] B. Products priced above at least one product in category 5
- [ ] C. Products priced above ALL products in category 5 (more expensive than the most expensive in category 5)
- [ ] D. Products in category 5 that are the most expensive

---

### Question 36 — Requirement → SQL

**Requirement:** Combine customer emails and employee emails into one list, removing duplicates.

**Which query is correct?**

- [ ] A.
```sql
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;
```

- [ ] B.
```sql
SELECT email FROM customers
EXCEPT
SELECT email FROM employees;
```

- [ ] C.
```sql
SELECT email FROM customers
UNION
SELECT email FROM employees;
```

- [ ] D.
```sql
SELECT email FROM customers
UNION ALL
SELECT email FROM employees;
```

---

### Question 37 — SQL → Description

**What does this query produce?**

```sql
WITH category_stats AS (
    SELECT 
        category_id,
        COUNT(*) AS product_count,
        AVG(price) AS avg_price
    FROM products
    GROUP BY category_id
)
SELECT c.name, cs.product_count, cs.avg_price
FROM categories c
JOIN category_stats cs ON c.id = cs.category_id
WHERE cs.product_count > 10
ORDER BY cs.avg_price DESC;
```

- [ ] A. All categories with their product counts
- [ ] B. Categories with more than 10 products, showing count and average price, sorted by average price descending
- [ ] C. The top 10 categories by product count
- [ ] D. Products grouped by category with prices above average

---

### Question 38 — Requirement → SQL

**Requirement:** Find products that have been ordered in both January AND February 2026.

**Which query is correct?**

- [ ] A.
```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at BETWEEN '2026-01-01' AND '2026-02-28';
```

- [ ] B.
```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
INTERSECT
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

- [ ] C.
```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
EXCEPT
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

- [ ] D.
```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
UNION ALL
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

---

### Question 39 — SQL → Description

**What hierarchy does this recursive CTE traverse?**

```sql
WITH RECURSIVE org_chart AS (
    SELECT id, name, manager_id, 1 AS level
    FROM employees
    WHERE manager_id IS NULL
    
    UNION ALL
    
    SELECT e.id, e.name, e.manager_id, oc.level + 1
    FROM employees e
    JOIN org_chart oc ON e.manager_id = oc.id
)
SELECT * FROM org_chart ORDER BY level;
```

- [ ] A. Products by category hierarchy
- [ ] B. Employee-manager hierarchy starting from top-level employees (no manager)
- [ ] C. Customer order history
- [ ] D. Date series for reporting

---

### Question 40 — Requirement → SQL

**Requirement:** Create a view showing only active products with their category names, hiding the internal flags and timestamps.

**Which is correct?**

- [ ] A.
```sql
CREATE TABLE active_products_view AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

- [ ] B.
```sql
CREATE VIEW active_products AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

- [ ] C.
```sql
CREATE MATERIALIZED VIEW active_products AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

- [ ] D.
```sql
INSERT INTO VIEW active_products AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

---

### Question 41 — SQL → Description

**What does this correlated subquery find?**

```sql
SELECT * FROM products p1
WHERE price = (
    SELECT MAX(price) 
    FROM products p2 
    WHERE p2.category_id = p1.category_id
);
```

- [ ] A. The single most expensive product overall
- [ ] B. The most expensive product in each category
- [ ] C. All products priced above average
- [ ] D. Products with prices equal to the minimum in their category

---

### Question 42 — Requirement → SQL

**Requirement:** Find all customer emails that are NOT in the employees table (customers only, not employees).

**Which query is correct?**

- [ ] A.
```sql
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;
```

- [ ] B.
```sql
SELECT email FROM customers
UNION
SELECT email FROM employees;
```

- [ ] C.
```sql
SELECT email FROM customers
EXCEPT
SELECT email FROM employees;
```

- [ ] D.
```sql
SELECT email FROM customers
UNION ALL
SELECT email FROM employees;
```

---

### Question 43 — SQL → Description

**What happens when you query this view after the underlying data changes?**

```sql
CREATE VIEW recent_orders AS
SELECT * FROM orders WHERE created_at >= CURRENT_DATE - INTERVAL '30 days';

-- Later...
SELECT * FROM recent_orders;
```

- [ ] A. Returns the same data as when the view was created (cached)
- [ ] B. Runs the query fresh, showing current orders from the last 30 days
- [ ] C. Returns an error because the data has changed
- [ ] D. Requires REFRESH before returning data

---

### Question 44 — Requirement → SQL

**Requirement:** Create a materialized view for monthly sales totals that can be refreshed without blocking reads.

**Which steps are correct?**

- [ ] A.
```sql
CREATE MATERIALIZED VIEW monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);

REFRESH MATERIALIZED VIEW monthly_sales;
```

- [ ] B.
```sql
CREATE MATERIALIZED VIEW monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);

CREATE UNIQUE INDEX ON monthly_sales (month);
REFRESH MATERIALIZED VIEW CONCURRENTLY monthly_sales;
```

- [ ] C.
```sql
CREATE VIEW monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);

REFRESH VIEW CONCURRENTLY monthly_sales;
```

- [ ] D.
```sql
CREATE MATERIALIZED VIEW CONCURRENT monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);
```

---

### Question 45 — SQL → Description

**What problem does this query solve?**

```sql
SELECT * FROM orders o1
WHERE total_amount > (
    SELECT AVG(total_amount)
    FROM orders o2
    WHERE o2.customer_id = o1.customer_id
);
```

- [ ] A. Finds orders above the global average
- [ ] B. Finds each customer's first order
- [ ] C. Finds orders where the amount is above that customer's own average order value
- [ ] D. Finds the maximum order for each customer

---

---

## Answer Key (Instructor Only)

### Part A: True or False

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 1 | **True** | Scalar subqueries must return exactly one value (one row, one column) |
| 2 | **False** | `NOT IN` fails when NULLs are present; `NOT EXISTS` handles NULLs correctly |
| 3 | **True** | Correlated subqueries reference outer query columns and run once per outer row |
| 4 | **True** | UNION removes duplicates (slower); UNION ALL keeps all rows (faster) |
| 5 | **False** | CTEs exist only for the duration of the single query; they don't persist |
| 6 | **True** | Recursive CTEs need an anchor (base case) and recursive member connected by UNION [ALL] |
| 7 | **False** | Regular views don't store data; they run the query each time accessed |
| 8 | **False** | Only simple views based on single tables without aggregates/DISTINCT/etc. are updatable |
| 9 | **False** | Materialized views require manual REFRESH to update their data |
| 10 | **True** | `= ANY (subquery)` checks if value equals any value in the list, same as IN |
| 11 | **True** | `> ALL` means greater than every value, which is the maximum |
| 12 | **True** | Derived tables (subqueries in FROM) must have an alias in most databases |
| 13 | **True** | EXCEPT returns rows from first query that don't appear in second query |
| 14 | **True** | WITH CHECK OPTION prevents changes that would make the row invisible to the view |
| 15 | **True** | Unlike regular views, materialized views can have indexes for better performance |

### Part B: Multiple Choice

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 16 | **B** | Can't use aggregate in WHERE without subquery; scalar subquery calculates avg |
| 17 | **C** | Scalar subquery position requires single value; multiple rows cause error |
| 18 | **B** | Any comparison with NULL = UNKNOWN; NOT IN (1,2,NULL) always UNKNOWN |
| 19 | **C** | NOT EXISTS handles NULLs correctly because it checks for row existence |
| 20 | **B** | Correlated references outer query, runs per row; non-correlated runs once |
| 21 | **C** | INTERSECT returns rows appearing in both result sets |
| 22 | **B** | UNION ALL skips duplicate elimination, making it fastest |
| 23 | **B** | CTEs are temporary named result sets for readability and reuse within one query |
| 24 | **B** | Multiple CTEs separated by commas after single WITH keyword |
| 25 | **B** | Anchor member provides starting rows without self-reference |
| 26 | **B** | Starts at 1, adds 1 each iteration while n < 5, producing 1,2,3,4,5 |
| 27 | **B** | Add termination condition in WHERE clause (depth limit) |
| 28 | **B** | Views execute their underlying query each time accessed |
| 29 | **B** | Simple single-table views without aggregates/DISTINCT/GROUP BY are updatable |
| 30 | **B** | Materialized views store data physically; regular views are virtual |
| 31 | **C** | Use materialized views for expensive queries where slight staleness is acceptable |
| 32 | **B** | REFRESH MATERIALIZED VIEW rebuilds the stored data |
| 33 | **B** | CONCURRENTLY allows reads during refresh (requires unique index) |

### Part C: SQL Translation

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 34 | **B** | EXISTS with correlated condition checking if orders exist for each customer |
| 35 | **C** | `> ALL` means greater than maximum value in the subquery |
| 36 | **C** | UNION combines both sets and removes duplicates |
| 37 | **B** | CTE calculates stats, main query filters >10 products, sorts by avg price |
| 38 | **B** | INTERSECT finds products appearing in BOTH January AND February |
| 39 | **B** | Starts from employees with no manager (NULL), recursively finds reports |
| 40 | **B** | CREATE VIEW creates a virtual table; CREATE TABLE would copy data |
| 41 | **B** | Correlated subquery finds MAX price per category, returns those products |
| 42 | **C** | EXCEPT returns rows in first set that aren't in second set |
| 43 | **B** | Regular views run fresh each time; no caching or refresh needed |
| 44 | **B** | CONCURRENTLY requires unique index; must create MV first, then index, then refresh |
| 45 | **C** | Correlated subquery calculates per-customer average, filters orders above it |

