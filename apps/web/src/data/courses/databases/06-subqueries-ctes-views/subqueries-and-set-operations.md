# Subqueries & Set Operations

Subqueries (also called nested queries or inner queries) are queries embedded within another query. They enable complex data retrieval that would otherwise require multiple queries or application logic. Set operations combine results from multiple queries.

---

## What is a Subquery?

A subquery is a SELECT statement nested inside another SQL statement.

```sql
-- Main query (outer query)
SELECT * FROM products
WHERE price > (
    -- Subquery (inner query)
    SELECT AVG(price) FROM products
);
```

Subqueries can appear in:
- `SELECT` clause (scalar subqueries)
- `FROM` clause (derived tables)
- `WHERE` clause (filtering)
- `HAVING` clause (group filtering)

---

## Scalar Subqueries

A **scalar subquery** returns exactly **one value** (one row, one column). It can be used anywhere a single value is expected.

### In SELECT Clause

```sql
-- Add average price to each product row
SELECT 
    name,
    price,
    (SELECT AVG(price) FROM products) AS avg_price,
    price - (SELECT AVG(price) FROM products) AS diff_from_avg
FROM products;

-- Count related items
SELECT 
    c.name AS category,
    (SELECT COUNT(*) FROM products p WHERE p.category_id = c.id) AS product_count
FROM categories c;
```

### In WHERE Clause

```sql
-- Products priced above average
SELECT name, price
FROM products
WHERE price > (SELECT AVG(price) FROM products);

-- Most recent order
SELECT * FROM orders
WHERE created_at = (SELECT MAX(created_at) FROM orders);

-- Customer with highest total spending
SELECT * FROM customers
WHERE id = (
    SELECT customer_id 
    FROM orders 
    GROUP BY customer_id 
    ORDER BY SUM(total_amount) DESC 
    LIMIT 1
);
```

### Scalar Subquery Errors

Scalar subqueries **must** return exactly one value:

```sql
-- ERROR: Subquery returns multiple rows
SELECT * FROM products
WHERE price > (SELECT price FROM products WHERE category_id = 1);

-- CORRECT: Use aggregate or LIMIT
SELECT * FROM products
WHERE price > (SELECT MAX(price) FROM products WHERE category_id = 1);
```

---

## Subqueries in WHERE with IN

`IN` checks if a value matches **any value** in a list or subquery result.

### Basic IN Subquery

```sql
-- Products in categories that have 'Electronics' in their name
SELECT * FROM products
WHERE category_id IN (
    SELECT id FROM categories WHERE name LIKE '%Electronics%'
);

-- Customers who have placed orders
SELECT * FROM customers
WHERE id IN (
    SELECT DISTINCT customer_id FROM orders
);

-- Products ordered in January 2026
SELECT * FROM products
WHERE id IN (
    SELECT DISTINCT product_id 
    FROM order_items oi
    JOIN orders o ON oi.order_id = o.id
    WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
);
```

### NOT IN

```sql
-- Customers who have never ordered
SELECT * FROM customers
WHERE id NOT IN (
    SELECT DISTINCT customer_id FROM orders WHERE customer_id IS NOT NULL
);

-- Products never ordered
SELECT * FROM products
WHERE id NOT IN (
    SELECT DISTINCT product_id FROM order_items WHERE product_id IS NOT NULL
);
```

⚠️ **Warning:** `NOT IN` with NULL values returns no rows!

```sql
-- If subquery returns (1, 2, NULL):
-- NOT IN (1, 2, NULL) is always UNKNOWN, returning no rows!

-- Safe pattern: exclude NULLs
WHERE id NOT IN (SELECT col FROM table WHERE col IS NOT NULL)

-- Or use NOT EXISTS instead (handles NULLs correctly)
```

---

## Subqueries with EXISTS

`EXISTS` returns TRUE if the subquery returns **any rows** at all. It's often more efficient than `IN` for large datasets.

### Basic EXISTS

```sql
-- Customers who have placed at least one order
SELECT * FROM customers c
WHERE EXISTS (
    SELECT 1 FROM orders o WHERE o.customer_id = c.id
);

-- Categories that have products
SELECT * FROM categories cat
WHERE EXISTS (
    SELECT 1 FROM products p WHERE p.category_id = cat.id
);
```

### NOT EXISTS

```sql
-- Customers who have never ordered
SELECT * FROM customers c
WHERE NOT EXISTS (
    SELECT 1 FROM orders o WHERE o.customer_id = c.id
);

-- Products not in any order
SELECT * FROM products p
WHERE NOT EXISTS (
    SELECT 1 FROM order_items oi WHERE oi.product_id = p.id
);
```

### EXISTS vs IN

| Aspect | IN | EXISTS |
|--------|-----|--------|
| **Returns** | List of values | TRUE/FALSE |
| **NULL handling** | Problematic with NOT IN | Handles NULLs correctly |
| **Performance** | Better for small subquery results | Better for large outer tables |
| **Readability** | More intuitive for simple cases | Better for correlated queries |

```sql
-- Equivalent queries:

-- Using IN
SELECT * FROM customers
WHERE id IN (SELECT customer_id FROM orders);

-- Using EXISTS (often faster)
SELECT * FROM customers c
WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id);
```

---

## ANY and ALL

`ANY` and `ALL` compare a value to a set of values from a subquery.

### ANY (SOME)

Returns TRUE if comparison is true for **at least one** value.

```sql
-- Products more expensive than ANY product in category 1
-- (i.e., more expensive than the cheapest in category 1)
SELECT * FROM products
WHERE price > ANY (SELECT price FROM products WHERE category_id = 1);

-- Equivalent to:
SELECT * FROM products
WHERE price > (SELECT MIN(price) FROM products WHERE category_id = 1);
```

### ALL

Returns TRUE if comparison is true for **all** values.

```sql
-- Products more expensive than ALL products in category 1
-- (i.e., more expensive than the most expensive in category 1)
SELECT * FROM products
WHERE price > ALL (SELECT price FROM products WHERE category_id = 1);

-- Equivalent to:
SELECT * FROM products
WHERE price > (SELECT MAX(price) FROM products WHERE category_id = 1);
```

### ANY/ALL Comparison Table

| Expression | Equivalent |
|------------|------------|
| `> ANY (subquery)` | `> MIN(subquery)` |
| `< ANY (subquery)` | `< MAX(subquery)` |
| `= ANY (subquery)` | `IN (subquery)` |
| `> ALL (subquery)` | `> MAX(subquery)` |
| `< ALL (subquery)` | `< MIN(subquery)` |
| `<> ALL (subquery)` | `NOT IN (subquery)` |

---

## Correlated vs Non-Correlated Subqueries

### Non-Correlated Subquery

Executes **once**, independently of the outer query.

```sql
-- Non-correlated: subquery runs once
SELECT * FROM products
WHERE price > (SELECT AVG(price) FROM products);
--              ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
--              This runs once, returns a single value
```

### Correlated Subquery

References columns from the outer query. Executes **once per row** of the outer query.

```sql
-- Correlated: subquery runs for each product row
SELECT * FROM products p1
WHERE price > (
    SELECT AVG(price) 
    FROM products p2 
    WHERE p2.category_id = p1.category_id  -- References outer query!
);
-- Find products priced above their category's average
```

### Correlated Subquery Examples

```sql
-- Products that are the most expensive in their category
SELECT * FROM products p1
WHERE price = (
    SELECT MAX(price) 
    FROM products p2 
    WHERE p2.category_id = p1.category_id
);

-- Orders with above-average amount for that customer
SELECT * FROM orders o1
WHERE total_amount > (
    SELECT AVG(total_amount)
    FROM orders o2
    WHERE o2.customer_id = o1.customer_id
);

-- Employees who earn more than their department average
SELECT * FROM employees e1
WHERE salary > (
    SELECT AVG(salary)
    FROM employees e2
    WHERE e2.department_id = e1.department_id
);
```

### Performance Consideration

Correlated subqueries can be slow for large tables because they execute once per outer row. Consider rewriting with JOINs:

```sql
-- Correlated subquery (slower)
SELECT * FROM products p
WHERE price = (
    SELECT MAX(price) FROM products WHERE category_id = p.category_id
);

-- Rewritten with JOIN (often faster)
SELECT p.*
FROM products p
JOIN (
    SELECT category_id, MAX(price) AS max_price
    FROM products
    GROUP BY category_id
) cat_max ON p.category_id = cat_max.category_id 
         AND p.price = cat_max.max_price;
```

---

## Subqueries in FROM (Derived Tables)

A subquery in the FROM clause creates a temporary table (derived table or inline view).

```sql
-- Calculate statistics, then filter
SELECT * FROM (
    SELECT 
        category_id,
        COUNT(*) AS product_count,
        AVG(price) AS avg_price
    FROM products
    GROUP BY category_id
) AS category_stats
WHERE product_count > 10;

-- Join with derived table
SELECT 
    c.name,
    stats.product_count,
    stats.avg_price
FROM categories c
JOIN (
    SELECT 
        category_id,
        COUNT(*) AS product_count,
        AVG(price) AS avg_price
    FROM products
    GROUP BY category_id
) AS stats ON c.id = stats.category_id;
```

> **Note:** Derived tables **must** have an alias in most databases.

---

## Set Operations

Set operations combine results from multiple SELECT statements.

### Requirements

- Same number of columns
- Compatible data types
- Column names come from the first query

### UNION

Combines results and **removes duplicates**.

```sql
-- All people (customers and employees)
SELECT name, email FROM customers
UNION
SELECT name, email FROM employees;

-- All product IDs from orders in Jan or Feb
SELECT product_id FROM order_items WHERE order_id IN 
    (SELECT id FROM orders WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01')
UNION
SELECT product_id FROM order_items WHERE order_id IN 
    (SELECT id FROM orders WHERE created_at >= '2026-02-01' AND created_at < '2026-03-01');
```

### UNION ALL

Combines results and **keeps duplicates** (faster than UNION).

```sql
-- All transactions (with possible duplicates)
SELECT amount, 'credit' AS type FROM credits
UNION ALL
SELECT amount, 'debit' AS type FROM debits;

-- Combine logs from multiple tables
SELECT timestamp, message, 'app' AS source FROM app_logs
UNION ALL
SELECT timestamp, message, 'system' AS source FROM system_logs
ORDER BY timestamp DESC;
```

### When to Use UNION vs UNION ALL

| Use UNION when... | Use UNION ALL when... |
|-------------------|----------------------|
| You need unique results | You want all rows (duplicates OK) |
| Combining overlapping data | Combining non-overlapping data |
| Correctness over performance | Performance is critical |

### INTERSECT

Returns rows that appear in **both** queries.

```sql
-- Customers who are also employees
SELECT email FROM customers
INTERSECT
SELECT email FROM employees;

-- Products ordered in both January AND February
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
INTERSECT
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

### EXCEPT (MINUS in Oracle)

Returns rows from the first query that are **not in** the second query.

```sql
-- Customers who are not employees
SELECT email FROM customers
EXCEPT
SELECT email FROM employees;

-- Products ordered in January but NOT in February
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-01-01' AND o.created_at < '2026-02-01'
EXCEPT
SELECT product_id FROM order_items oi
JOIN orders o ON oi.order_id = o.id
WHERE o.created_at >= '2026-02-01' AND o.created_at < '2026-03-01';
```

### Set Operations with ORDER BY

ORDER BY applies to the **final result** and goes at the end:

```sql
SELECT name, email FROM customers
UNION
SELECT name, email FROM employees
ORDER BY name;  -- Sorts the combined result
```

---

## Subquery Best Practices

### 1. Use CTEs for Readability

Complex nested subqueries are hard to read. Consider CTEs (next lesson):

```sql
-- Hard to read
SELECT * FROM (
    SELECT * FROM (
        SELECT customer_id, SUM(total) AS total
        FROM orders GROUP BY customer_id
    ) sub1 WHERE total > 1000
) sub2 WHERE ...

-- Better with CTE (covered in next lesson)
WITH customer_totals AS (
    SELECT customer_id, SUM(total) AS total
    FROM orders GROUP BY customer_id
)
SELECT * FROM customer_totals WHERE total > 1000;
```

### 2. Prefer EXISTS Over IN for Large Datasets

```sql
-- Potentially slower
SELECT * FROM customers
WHERE id IN (SELECT customer_id FROM orders);

-- Often faster
SELECT * FROM customers c
WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id);
```

### 3. Avoid Correlated Subqueries When Possible

```sql
-- Slow: correlated subquery runs per row
SELECT *, (SELECT COUNT(*) FROM orders WHERE customer_id = c.id) AS order_count
FROM customers c;

-- Faster: single aggregation with join
SELECT c.*, COALESCE(o.order_count, 0) AS order_count
FROM customers c
LEFT JOIN (
    SELECT customer_id, COUNT(*) AS order_count
    FROM orders GROUP BY customer_id
) o ON c.id = o.customer_id;
```

---

## Practice Exercises

### Exercise 1: Scalar Subqueries
1. Find all products priced above the average price
2. For each product, show its price and the category average price
3. Find the customer who placed the most recent order

### Exercise 2: IN and EXISTS
1. Find customers who have ordered products from category 'Electronics'
2. Find products that have never been ordered (use both NOT IN and NOT EXISTS)
3. Find categories where all products are priced above $50

### Exercise 3: Correlated Subqueries
1. Find products that are the cheapest in their category
2. Find orders with total above the customer's average order value
3. Find employees who earn more than their manager

### Exercise 4: Set Operations
1. Get a list of all emails (customers and newsletter subscribers), without duplicates
2. Find customers who have ordered but never left a review
3. Find products ordered in Q1 but not in Q2

---

## Key Takeaways

1. **Scalar subqueries** return one value and can go in SELECT or WHERE
2. **IN** checks membership in a list; watch for NULL issues with NOT IN
3. **EXISTS** checks for row existence; handles NULLs correctly
4. **ANY/ALL** compare against all values in a set
5. **Correlated subqueries** reference outer query; run once per row
6. **UNION** removes duplicates; **UNION ALL** keeps them
7. **INTERSECT** finds common rows; **EXCEPT** finds differences

---

## Quick Reference

```sql
-- Scalar subquery
SELECT * FROM t WHERE col > (SELECT AVG(col) FROM t);

-- IN subquery
SELECT * FROM t WHERE id IN (SELECT id FROM other);

-- EXISTS
SELECT * FROM t1 WHERE EXISTS (SELECT 1 FROM t2 WHERE t2.fk = t1.id);

-- NOT EXISTS
SELECT * FROM t1 WHERE NOT EXISTS (SELECT 1 FROM t2 WHERE t2.fk = t1.id);

-- Correlated subquery
SELECT * FROM t1 WHERE col = (SELECT MAX(col) FROM t1 t2 WHERE t2.group = t1.group);

-- Set operations
SELECT a FROM t1 UNION SELECT a FROM t2;      -- Remove duplicates
SELECT a FROM t1 UNION ALL SELECT a FROM t2;  -- Keep duplicates
SELECT a FROM t1 INTERSECT SELECT a FROM t2;  -- In both
SELECT a FROM t1 EXCEPT SELECT a FROM t2;     -- In first, not second
```
