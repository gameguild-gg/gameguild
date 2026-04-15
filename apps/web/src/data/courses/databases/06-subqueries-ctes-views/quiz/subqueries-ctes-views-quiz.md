# Quiz: Subqueries, CTEs & Views (Week 06)

## Instructions

This quiz tests your understanding of **subqueries** (scalar, IN, EXISTS, ANY/ALL, correlated), **set operations** (UNION, INTERSECT, EXCEPT), **Common Table Expressions (CTEs)**, **recursive CTEs**, **views**, and **materialized views**.

**Total: 46 questions**

Time estimate: 45-60 minutes

---

# PART A: True or False

---

!!! quiz
{
"title": "Scalar Subquery Return Value",
"question": "A scalar subquery must return exactly one row and one column.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "NOT IN vs NOT EXISTS",
"question": "`NOT IN` and `NOT EXISTS` always produce the same results for the same data.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Correlated Subquery Execution",
"question": "A correlated subquery executes once for each row in the outer query.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "UNION vs UNION ALL Duplicates",
"question": "`UNION` removes duplicate rows from the combined result, while `UNION ALL` keeps all rows including duplicates.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "CTE Persistence",
"question": "CTEs (Common Table Expressions) persist after the query completes and can be used in subsequent queries.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Recursive CTE Structure",
"question": "A recursive CTE must have both an anchor member and a recursive member connected by `UNION ALL`.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "View Physical Storage",
"question": "Regular views store the query result data physically on disk.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "All Views Updatable",
"question": "All views are updatable - you can always INSERT, UPDATE, or DELETE through a view.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Materialized View Auto-Refresh",
"question": "Materialized views automatically refresh whenever the underlying data changes.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "= ANY Equivalence",
"question": "`= ANY (subquery)` is equivalent to `IN (subquery)`.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "> ALL Semantics",
"question": "`> ALL (subquery)` returns TRUE if the value is greater than the maximum value in the subquery result.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Derived Table Alias",
"question": "Derived tables (subqueries in FROM clause) must have an alias.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "EXCEPT Semantics",
"question": "`EXCEPT` returns rows that appear in the first query but not in the second query.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "WITH CHECK OPTION",
"question": "`WITH CHECK OPTION` on a view prevents inserts/updates that would make the row disappear from the view.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Indexes on Materialized Views",
"question": "You can create indexes on materialized views to improve query performance.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

# PART B: Multiple Choice

---

**Which query correctly finds all products priced above the average price?**

Option A:

```sql
SELECT * FROM products HAVING price > AVG(price);
```

Option B:

```sql
SELECT * FROM products GROUP BY id HAVING price > AVG(price);
```

Option C:

```sql
SELECT * FROM products WHERE price > AVG(price);
```

Option D:

```sql
SELECT * FROM products WHERE price > (SELECT AVG(price) FROM products);
```

!!! quiz
{
"title": "Scalar Subquery for Average",
"question": "Which query correctly finds all products priced above the average price?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

**What happens when this query runs if the subquery returns multiple rows?**

```sql
SELECT * FROM products
WHERE price > (SELECT price FROM products WHERE category_id = 1);
```

!!! quiz
{
"title": "Scalar Subquery Multiple Rows",
"question": "What happens when the query above runs if the subquery returns multiple rows?",
"options": ["It returns all products priced above the maximum price in category 1", "It returns no rows", "It returns all products priced above the minimum price in category 1", "It returns an error because the scalar subquery returns multiple rows"],
"answers": ["It returns an error because the scalar subquery returns multiple rows"]
}
!!!

---

**Consider this query where `orders.customer_id` may contain NULL values:**

```sql
SELECT * FROM customers
WHERE id NOT IN (SELECT customer_id FROM orders);
```

!!! quiz
{
"title": "NOT IN with NULL Problem",
"question": "Why does `NOT IN` with NULL values cause problems in the query above?",
"options": ["NULL values are automatically excluded from the subquery", "NULL values are treated as 0", "Comparison with NULL yields UNKNOWN, making the entire NOT IN return no rows", "The query throws a syntax error"],
"answers": ["Comparison with NULL yields UNKNOWN, making the entire NOT IN return no rows"]
}
!!!

---

**Which is the safer alternative to `NOT IN` when NULLs may be present?**

Option A:

```sql
WHERE id <> ANY (SELECT col FROM t)
```

Option B:

```sql
WHERE NOT EXISTS (SELECT 1 FROM t WHERE t.fk = outer.id)
```

Option C:

```sql
WHERE id NOT IN (SELECT col FROM t)
```

Option D:

```sql
WHERE id != ALL (SELECT col FROM t)
```

!!! quiz
{
"title": "Safe Alternative to NOT IN",
"question": "Which is the safer alternative to `NOT IN` when NULLs may be present?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

!!! quiz
{
"title": "Correlated vs Non-Correlated",
"question": "What is the difference between a correlated and non-correlated subquery?",
"options": ["There is no difference; they are the same", "Correlated subqueries reference the outer query and run once per row; non-correlated run once independently", "Correlated subqueries are faster than non-correlated", "Correlated subqueries use JOINs; non-correlated use WHERE"],
"answers": ["Correlated subqueries reference the outer query and run once per row; non-correlated run once independently"]
}
!!!

---

**What does `INTERSECT` return?**

```sql
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;
```

!!! quiz
{
"title": "INTERSECT Result",
"question": "What does the `INTERSECT` query above return?",
"options": ["Emails that appear in employees but not in customers", "All emails from customers and employees combined", "Emails that appear in both customers AND employees", "Emails that appear in customers but not in employees"],
"answers": ["Emails that appear in both customers AND employees"]
}
!!!

---

!!! quiz
{
"title": "Fastest Set Operation",
"question": "Which set operation is fastest because it doesn't need to check for duplicates?",
"options": ["INTERSECT", "EXCEPT", "UNION", "UNION ALL"],
"answers": ["UNION ALL"]
}
!!!

---

!!! quiz
{
"title": "CTE Purpose",
"question": "What is the purpose of a CTE (Common Table Expression)?",
"options": ["To create a temporary named result set that improves query readability and can be referenced multiple times", "To permanently store query results in the database", "To replace all JOINs in a query", "To automatically optimize query performance"],
"answers": ["To create a temporary named result set that improves query readability and can be referenced multiple times"]
}
!!!

---

**Which CTE syntax is correct for defining multiple CTEs?**

Option A:

```sql
WITH cte1 AS (...) WITH cte2 AS (...) SELECT ...
```

Option B:

```sql
WITH cte1 AS (...); WITH cte2 AS (...); SELECT ...
```

Option C:

```sql
WITH cte1 AS (...), cte2 AS (...) SELECT ...
```

Option D:

```sql
WITH (cte1 AS (...) AND cte2 AS (...)) SELECT ...
```

!!! quiz
{
"title": "Multiple CTE Syntax",
"question": "Which CTE syntax is correct for defining multiple CTEs?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

!!! quiz
{
"title": "Anchor Member Definition",
"question": "In a recursive CTE, what is the \"anchor member\"?",
"options": ["The ORDER BY clause", "The part that references the CTE itself", "The final SELECT statement", "The base case that provides initial rows and doesn't reference the CTE"],
"answers": ["The base case that provides initial rows and doesn't reference the CTE"]
}
!!!

---

**What does this recursive CTE produce?**

```sql
WITH RECURSIVE nums AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM nums WHERE n < 5
)
SELECT n FROM nums;
```

!!! quiz
{
"title": "Recursive CTE Number Sequence",
"question": "What does the recursive CTE above produce?",
"options": ["An infinite loop", "The number 1 repeated 5 times", "The numbers 1, 2, 3, 4, 5", "An error because recursion is not allowed"],
"answers": ["The numbers 1, 2, 3, 4, 5"]
}
!!!

---

!!! quiz
{
"title": "Preventing Infinite Recursion",
"question": "How do you prevent infinite loops in recursive CTEs?",
"options": ["Use UNION instead of UNION ALL", "Add a WHERE condition that eventually becomes false (depth limit)", "Recursive CTEs automatically stop after 100 iterations", "You cannot prevent infinite loops; they always run forever"],
"answers": ["Add a WHERE condition that eventually becomes false (depth limit)"]
}
!!!

---

!!! quiz
{
"title": "View Behavior",
"question": "Which statement about views is correct?",
"options": ["Views store data physically like tables", "Views cannot be used with JOINs", "Views must contain aggregate functions", "Views run their underlying query each time they are accessed"],
"answers": ["Views run their underlying query each time they are accessed"]
}
!!!

---

!!! quiz
{
"title": "Updatable View Requirements",
"question": "What makes a view \"updatable\" (allowing INSERT/UPDATE/DELETE)?",
"options": ["Being based on a single table without DISTINCT, GROUP BY, aggregates, or subqueries", "Using WITH CHECK OPTION", "Creating an index on the view", "Explicitly declaring it with CREATE UPDATABLE VIEW"],
"answers": ["Being based on a single table without DISTINCT, GROUP BY, aggregates, or subqueries"]
}
!!!

---

!!! quiz
{
"title": "View vs Materialized View",
"question": "What is the key difference between a view and a materialized view?",
"options": ["Views can only SELECT; materialized views can INSERT", "Materialized views store data physically and require refresh; views execute the query each time", "Materialized views are automatically updated; views require manual refresh", "There is no difference; they are the same"],
"answers": ["Materialized views store data physically and require refresh; views execute the query each time"]
}
!!!

---

!!! quiz
{
"title": "When to Use Materialized Views",
"question": "When should you use a materialized view instead of a regular view?",
"options": ["When the underlying query is expensive and data freshness can be slightly delayed", "When you need real-time data accuracy", "When the underlying query is simple", "When you need to hide columns for security"],
"answers": ["When the underlying query is expensive and data freshness can be slightly delayed"]
}
!!!

---

!!! quiz
{
"title": "Updating Materialized View Data",
"question": "How do you update data in a materialized view?",
"options": ["Delete and recreate the materialized view", "Data updates automatically when underlying tables change", "Use REFRESH MATERIALIZED VIEW to rebuild the data", "Use UPDATE statement directly on the materialized view"],
"answers": ["Use REFRESH MATERIALIZED VIEW to rebuild the data"]
}
!!!

---

!!! quiz
{
"title": "REFRESH CONCURRENTLY",
"question": "What does `REFRESH MATERIALIZED VIEW CONCURRENTLY` do differently?",
"options": ["Allows reads during refresh (doesn't lock the view)", "Schedules the refresh for later", "Refreshes faster by skipping some rows", "Refreshes multiple views at once"],
"answers": ["Allows reads during refresh (doesn't lock the view)"]
}
!!!

---

# PART C: SQL Translation

---

**Requirement:** Find customers who have placed at least one order (use EXISTS).

Option A:

```sql
SELECT * FROM customers c
WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id);
```

Option B:

```sql
SELECT * FROM customers c
WHERE EXISTS (SELECT * FROM orders o);
```

Option C:

```sql
SELECT * FROM customers c
WHERE IN (SELECT customer_id FROM orders);
```

Option D:

```sql
SELECT * FROM customers c
WHERE o.customer_id EXISTS (SELECT 1 FROM orders o);
```

!!! quiz
{
"title": "EXISTS for Customers with Orders",
"question": "Which query correctly finds customers who have placed at least one order using EXISTS?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What does this query find?**

```sql
SELECT * FROM products
WHERE price > ALL (SELECT price FROM products WHERE category_id = 5);
```

!!! quiz
{
"title": "> ALL Query Description",
"question": "What does the query above find?",
"options": ["Products priced above at least one product in category 5", "Products in category 5 that are the most expensive", "Products priced above ALL products in category 5 (more expensive than the most expensive in category 5)", "Products priced above the average price in category 5"],
"answers": ["Products priced above ALL products in category 5 (more expensive than the most expensive in category 5)"]
}
!!!

---

**Requirement:** Combine customer emails and employee emails into one list, removing duplicates.

Option A:

```sql
SELECT email FROM customers
UNION ALL
SELECT email FROM employees;
```

Option B:

```sql
SELECT email FROM customers
EXCEPT
SELECT email FROM employees;
```

Option C:

```sql
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;
```

Option D:

```sql
SELECT email FROM customers
UNION
SELECT email FROM employees;
```

!!! quiz
{
"title": "UNION for Combined Email List",
"question": "Which query correctly combines customer and employee emails into one list, removing duplicates?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

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

!!! quiz
{
"title": "CTE with JOIN and Filter",
"question": "What does the query above produce?",
"options": ["The top 10 categories by product count", "Categories with more than 10 products, showing count and average price, sorted by average price descending", "Products grouped by category with prices above average", "All categories with their product counts"],
"answers": ["Categories with more than 10 products, showing count and average price, sorted by average price descending"]
}
!!!

---

**Requirement:** Find products that have been ordered in both January AND February 2026.

Option A:

```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at BETWEEN '2026-01-01' AND '2026-02-28';
```

Option B:

```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
EXCEPT
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

Option C:

```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
INTERSECT
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

Option D:

```sql
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
UNION ALL
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

!!! quiz
{
"title": "INTERSECT for Both Months",
"question": "Which query correctly finds products ordered in both January AND February 2026?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

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

!!! quiz
{
"title": "Recursive CTE Hierarchy",
"question": "What hierarchy does the recursive CTE above traverse?",
"options": ["Employee-manager hierarchy starting from top-level employees (no manager)", "Date series for reporting", "Products by category hierarchy", "Customer order history"],
"answers": ["Employee-manager hierarchy starting from top-level employees (no manager)"]
}
!!!

---

**Requirement:** Create a view showing only active products with their category names, hiding the internal flags and timestamps.

Option A:

```sql
CREATE VIEW active_products AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

Option B:

```sql
CREATE TABLE active_products_view AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

Option C:

```sql
CREATE MATERIALIZED VIEW active_products AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

Option D:

```sql
INSERT INTO VIEW active_products AS
SELECT p.id, p.name, p.price, c.name AS category
FROM products p
JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true;
```

!!! quiz
{
"title": "CREATE VIEW for Active Products",
"question": "Which query correctly creates a view showing only active products with their category names?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What does this correlated subquery find?**

```sql
SELECT * FROM products p1
WHERE price = (
    SELECT MAX(price)
    FROM products p2
    WHERE p2.category_id = p1.category_id
);
```

!!! quiz
{
"title": "Correlated Subquery MAX per Category",
"question": "What does the correlated subquery above find?",
"options": ["All products priced above average", "The single most expensive product overall", "Products with prices equal to the minimum in their category", "The most expensive product in each category"],
"answers": ["The most expensive product in each category"]
}
!!!

---

**Requirement:** Find all customer emails that are NOT in the employees table (customers only, not employees).

Option A:

```sql
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;
```

Option B:

```sql
SELECT email FROM customers
EXCEPT
SELECT email FROM employees;
```

Option C:

```sql
SELECT email FROM customers
UNION
SELECT email FROM employees;
```

Option D:

```sql
SELECT email FROM customers
UNION ALL
SELECT email FROM employees;
```

!!! quiz
{
"title": "EXCEPT for Customer-Only Emails",
"question": "Which query correctly finds customer emails that are NOT in the employees table?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

**What happens when you query this view after the underlying data changes?**

```sql
CREATE VIEW recent_orders AS
SELECT * FROM orders WHERE created_at >= CURRENT_DATE - INTERVAL '30 days';

-- Later...
SELECT * FROM recent_orders;
```

!!! quiz
{
"title": "View Fresh Data on Access",
"question": "What happens when you query the view above after the underlying data changes?",
"options": ["Returns the same data as when the view was created (cached)", "Runs the query fresh, showing current orders from the last 30 days", "Requires REFRESH before returning data", "Returns an error because the data has changed"],
"answers": ["Runs the query fresh, showing current orders from the last 30 days"]
}
!!!

---

**Requirement:** Create a materialized view for monthly sales totals that can be refreshed without blocking reads.

Option A:

```sql
CREATE MATERIALIZED VIEW monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);

CREATE UNIQUE INDEX ON monthly_sales (month);
REFRESH MATERIALIZED VIEW CONCURRENTLY monthly_sales;
```

Option B:

```sql
CREATE MATERIALIZED VIEW CONCURRENT monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);
```

Option C:

```sql
CREATE VIEW monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);

REFRESH VIEW CONCURRENTLY monthly_sales;
```

Option D:

```sql
CREATE MATERIALIZED VIEW monthly_sales AS
SELECT DATE_TRUNC('month', order_date) AS month, SUM(amount) AS total
FROM orders GROUP BY DATE_TRUNC('month', order_date);

REFRESH MATERIALIZED VIEW monthly_sales;
```

!!! quiz
{
"title": "Materialized View with CONCURRENTLY",
"question": "Which steps correctly create a materialized view that can be refreshed without blocking reads?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What problem does this query solve?**

```sql
SELECT * FROM orders o1
WHERE total_amount > (
    SELECT AVG(total_amount)
    FROM orders o2
    WHERE o2.customer_id = o1.customer_id
);
```

!!! quiz
{
"title": "Correlated Subquery Per-Customer Average",
"question": "What problem does the query above solve?",
"options": ["Finds each customer's first order", "Finds the maximum order for each customer", "Finds orders where the amount is above that customer's own average order value", "Finds orders above the global average"],
"answers": ["Finds orders where the amount is above that customer's own average order value"]
}
!!!

---

**Given this SQL statement:**

```sql
WITH monthly_sales AS (
    SELECT
        DATE_TRUNC('month', order_date) AS month,
        SUM(total_amount) AS revenue
    FROM orders
    WHERE order_date >= '2025-01-01'
    GROUP BY DATE_TRUNC('month', order_date)
)
SELECT
    month,
    revenue,
    revenue - LAG(revenue) OVER (ORDER BY month) AS month_over_month_change,
    ROUND(100.0 * (revenue - LAG(revenue) OVER (ORDER BY month)) / LAG(revenue) OVER (ORDER BY month), 2) AS pct_change
FROM monthly_sales
ORDER BY month;
```

!!! quiz
{
"title": "CTE with LAG Window Function",
"question": "What does the query above produce?",
"options": ["A running total of all orders placed since January 2025", "Monthly revenue totals for 2025+ with the difference and percentage change compared to the previous month", "A list of all orders from 2025 grouped by customer", "The total revenue for each product category in 2025"],
"answers": ["Monthly revenue totals for 2025+ with the difference and percentage change compared to the previous month"]
}
!!!
