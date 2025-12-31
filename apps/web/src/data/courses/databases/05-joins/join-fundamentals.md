# Join Fundamentals

Joins combine rows from two or more tables based on related columns. They are fundamental to working with relational databases, allowing you to query normalized data across multiple tables.

![Joins and Subqueries meme](https://i.programmerhumor.io/2024/07/programmerhumor-io-databases-memes-programming-memes-d6df758c4d35dca.png)

---

## Why Joins?

In a normalized database, data is split across multiple tables to:
- Eliminate redundancy
- Ensure data integrity
- Enable efficient updates

**Without joins**, you'd need separate queries and manual data combination:
```sql
-- Separate queries (inefficient)
SELECT * FROM orders WHERE id = 1;
SELECT * FROM customers WHERE id = 42;  -- Then manually match
```

**With joins**, you combine related data in a single query:
```sql
-- Single query with join
SELECT o.*, c.name AS customer_name
FROM orders o
JOIN customers c ON o.customer_id = c.id
WHERE o.id = 1;
```

---

## INNER JOIN

`INNER JOIN` returns only rows that have matching values in **both** tables.

### Basic Syntax

```sql
SELECT columns
FROM table1
INNER JOIN table2 ON table1.column = table2.column;
```

The `INNER` keyword is optional—`JOIN` alone means `INNER JOIN`:

```sql
SELECT columns
FROM table1
JOIN table2 ON table1.column = table2.column;
```

### Simple Example

```sql
-- Get orders with customer names
SELECT 
    orders.id AS order_id,
    orders.total_amount,
    customers.name AS customer_name,
    customers.email
FROM orders
INNER JOIN customers ON orders.customer_id = customers.id;
```

**Visual Representation:**

```
customers                    orders
+----+-------+              +----+-------------+--------+
| id | name  |              | id | customer_id | amount |
+----+-------+              +----+-------------+--------+
| 1  | Alice |              | 1  | 1           | 100    |
| 2  | Bob   |              | 2  | 1           | 150    |
| 3  | Carol |              | 3  | 2           | 200    |
+----+-------+              +----+-------------+--------+

INNER JOIN Result:
+----------+--------+-------+
| order_id | amount | name  |
+----------+--------+-------+
| 1        | 100    | Alice |
| 2        | 150    | Alice |
| 3        | 200    | Bob   |
+----------+--------+-------+
```

Note: Carol has no orders, so she doesn't appear in the result.

---

## Table Aliases

Aliases make queries shorter and more readable. Use them consistently.

### Without Aliases (Verbose)

```sql
SELECT 
    orders.id,
    orders.total_amount,
    customers.name
FROM orders
INNER JOIN customers ON orders.customer_id = customers.id;
```

### With Aliases (Cleaner)

```sql
SELECT 
    o.id,
    o.total_amount,
    c.name
FROM orders o
JOIN customers c ON o.customer_id = c.id;
```

### Alias Best Practices

| Table | Common Alias |
|-------|--------------|
| `customers` | `c` |
| `orders` | `o` |
| `order_items` | `oi` |
| `products` | `p` |
| `categories` | `cat` |
| `users` | `u` |

```sql
-- Descriptive aliases for complex queries
SELECT ...
FROM order_items oi
JOIN orders ord ON oi.order_id = ord.id
JOIN products prod ON oi.product_id = prod.id
JOIN categories cat ON prod.category_id = cat.id;
```

---

## Join Conditions

The `ON` clause specifies how tables are related.

### Equality Condition (Most Common)

```sql
-- Match on foreign key
SELECT *
FROM orders o
JOIN customers c ON o.customer_id = c.id;
```

### Multiple Conditions

```sql
-- Join with additional conditions
SELECT *
FROM prices p
JOIN products prod ON p.product_id = prod.id 
                   AND p.region = 'US'
                   AND p.effective_date <= CURRENT_DATE;
```

### Non-Equality Conditions

```sql
-- Range-based join
SELECT 
    e.name AS employee,
    s.grade AS salary_grade
FROM employees e
JOIN salary_grades s ON e.salary BETWEEN s.min_salary AND s.max_salary;

-- Comparison join
SELECT 
    e1.name AS employee,
    e2.name AS senior_colleague
FROM employees e1
JOIN employees e2 ON e1.hire_date > e2.hire_date;
```

---

## Multi-Table Joins

Chain multiple joins to combine data from several tables.

### Three-Table Join

```sql
-- Orders with customer and product details
SELECT 
    o.id AS order_id,
    c.name AS customer,
    p.name AS product,
    oi.quantity,
    oi.unit_price
FROM orders o
JOIN customers c ON o.customer_id = c.id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id;
```

### Join Order

The order of joins typically doesn't affect the result for INNER JOINs, but:
- Start with the main table
- Join related tables in logical order
- Consider readability

```sql
-- Logical flow: orders → items → products → categories
SELECT 
    o.id,
    p.name AS product,
    cat.name AS category
FROM orders o
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
JOIN categories cat ON p.category_id = cat.id
WHERE o.created_at >= '2026-01-01';
```

### Complex Multi-Table Example

```sql
-- Full order details with all related information
SELECT 
    o.id AS order_id,
    o.created_at AS order_date,
    c.name AS customer_name,
    c.email AS customer_email,
    a.city AS shipping_city,
    p.name AS product_name,
    cat.name AS category,
    oi.quantity,
    oi.unit_price,
    (oi.quantity * oi.unit_price) AS line_total
FROM orders o
JOIN customers c ON o.customer_id = c.id
JOIN addresses a ON o.shipping_address_id = a.id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
JOIN categories cat ON p.category_id = cat.id
WHERE o.status = 'completed'
ORDER BY o.created_at DESC;
```

---

## Joining on Multiple Columns

Some relationships require matching on multiple columns.

### Composite Key Join

```sql
-- Price depends on both product and region
SELECT 
    p.name AS product,
    pr.region,
    pr.price
FROM products p
JOIN product_prices pr ON p.id = pr.product_id 
                       AND p.version = pr.product_version;
```

### Natural Relationships

```sql
-- Enrollment requires both student and course
SELECT 
    s.name AS student,
    c.title AS course,
    e.grade
FROM enrollments e
JOIN students s ON e.student_id = s.id
JOIN courses c ON e.course_id = c.id;
```

---

## USING Clause

When join columns have the **same name** in both tables, use `USING` for cleaner syntax.

### Standard ON Clause

```sql
SELECT *
FROM orders o
JOIN customers c ON o.customer_id = c.customer_id;
```

### Equivalent USING Clause

```sql
SELECT *
FROM orders o
JOIN customers c USING (customer_id);
```

### Multiple Columns with USING

```sql
SELECT *
FROM order_items oi
JOIN inventory i USING (product_id, warehouse_id);
```

> **Note:** `USING` only works when column names match exactly in both tables.

---

## NATURAL JOIN

`NATURAL JOIN` automatically joins on all columns with matching names. **Use with caution!**

```sql
-- Joins on ALL columns with same names
SELECT *
FROM orders
NATURAL JOIN customers;
```

### Why to Avoid NATURAL JOIN

```sql
-- If both tables have 'id', 'created_at', 'updated_at' columns:
-- NATURAL JOIN will try to match on ALL of them!

-- This probably won't work as expected:
SELECT * FROM orders NATURAL JOIN customers;
-- Tries to match: id = id, created_at = created_at, etc.
```

**Best Practice:** Always use explicit `ON` or `USING` clauses for clarity and correctness.

---

## Filtering Joined Data

### WHERE vs ON for Filtering

For `INNER JOIN`, filtering in `WHERE` or `ON` produces the same result:

```sql
-- Filter in ON
SELECT o.id, c.name
FROM orders o
JOIN customers c ON o.customer_id = c.id AND c.country = 'USA';

-- Filter in WHERE (equivalent for INNER JOIN)
SELECT o.id, c.name
FROM orders o
JOIN customers c ON o.customer_id = c.id
WHERE c.country = 'USA';
```

**Best Practice:** 
- Use `ON` for join conditions (how tables relate)
- Use `WHERE` for filtering results (what data you want)

```sql
-- Clear separation of concerns
SELECT o.id, c.name, p.name AS product
FROM orders o
JOIN customers c ON o.customer_id = c.id          -- Relationship
JOIN order_items oi ON o.id = oi.order_id         -- Relationship
JOIN products p ON oi.product_id = p.id           -- Relationship
WHERE o.status = 'completed'                       -- Filter
  AND c.country = 'USA'                            -- Filter
  AND o.created_at >= '2026-01-01';                -- Filter
```

---

## Joins with Aggregations

Combine joins with aggregate functions for powerful analysis.

### Basic Aggregation with Join

```sql
-- Total spent per customer
SELECT 
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total_amount) AS total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name
ORDER BY total_spent DESC;
```

### Multi-Level Aggregation

```sql
-- Revenue by category
SELECT 
    cat.name AS category,
    COUNT(DISTINCT o.id) AS order_count,
    SUM(oi.quantity) AS units_sold,
    SUM(oi.quantity * oi.unit_price) AS revenue
FROM categories cat
JOIN products p ON cat.id = p.category_id
JOIN order_items oi ON p.id = oi.product_id
JOIN orders o ON oi.order_id = o.id
WHERE o.status = 'completed'
GROUP BY cat.id, cat.name
ORDER BY revenue DESC;
```

---

## Common Join Mistakes

### 1. Missing Join Condition (Cartesian Product)

```sql
-- WRONG: Creates every combination of rows!
SELECT * FROM orders, customers;
-- If orders has 100 rows and customers has 50, result has 5000 rows!

-- CORRECT: Use proper join
SELECT * FROM orders o JOIN customers c ON o.customer_id = c.id;
```

### 2. Ambiguous Column Names

```sql
-- WRONG: Which 'id' and 'name'?
SELECT id, name FROM orders JOIN customers ON customer_id = id;

-- CORRECT: Use table aliases
SELECT o.id, c.name FROM orders o JOIN customers c ON o.customer_id = c.id;
```

### 3. Joining Wrong Columns

```sql
-- WRONG: Comparing primary key to primary key
SELECT * FROM orders o JOIN customers c ON o.id = c.id;

-- CORRECT: Compare foreign key to primary key
SELECT * FROM orders o JOIN customers c ON o.customer_id = c.id;
```

### 4. Forgetting About NULL Foreign Keys

```sql
-- Orders with NULL customer_id won't appear in INNER JOIN
-- If that's not desired, use LEFT JOIN instead
SELECT * FROM orders o LEFT JOIN customers c ON o.customer_id = c.id;
```

---

## Query Execution Order with Joins

```
1. FROM + JOINs  -- Tables combined first
2. WHERE         -- Filter rows
3. GROUP BY      -- Create groups
4. HAVING        -- Filter groups
5. SELECT        -- Compute expressions
6. DISTINCT      -- Remove duplicates
7. ORDER BY      -- Sort
8. LIMIT         -- Limit results
```

---

## Practice Exercises

### Exercise 1: Basic Joins
1. List all orders with customer names
2. Show products with their category names
3. Display order items with product names and prices

### Exercise 2: Multi-Table Joins
1. Show all orders with customer name, product names, and quantities
2. List employees with their department and manager names
3. Display invoices with customer, product, and payment information

### Exercise 3: Joins with Aggregation
1. Find total revenue per customer
2. Count products per category
3. Calculate average order value by customer country

### Exercise 4: Complex Queries
Write a query showing:
- Customer name and email
- Number of orders placed
- Total amount spent
- Most recent order date
- For customers who have placed at least one order
- Sorted by total spent descending

---

## Key Takeaways

1. **INNER JOIN** returns only matching rows from both tables
2. **Use aliases** to keep queries readable
3. **ON clause** defines the relationship between tables
4. **Chain joins** to combine data from multiple tables
5. **Use WHERE** for filtering, **ON** for join conditions
6. **Avoid NATURAL JOIN** — be explicit about join columns
7. **Watch for Cartesian products** from missing join conditions

---

## Quick Reference

```sql
-- Basic INNER JOIN
SELECT columns
FROM table1 t1
JOIN table2 t2 ON t1.foreign_key = t2.primary_key;

-- Multi-table join
SELECT columns
FROM table1 t1
JOIN table2 t2 ON t1.fk = t2.pk
JOIN table3 t3 ON t2.fk = t3.pk;

-- With filtering and aggregation
SELECT 
    t1.name,
    COUNT(t2.id) AS count,
    SUM(t2.amount) AS total
FROM table1 t1
JOIN table2 t2 ON t1.id = t2.fk
WHERE t2.status = 'active'
GROUP BY t1.id, t1.name
HAVING COUNT(t2.id) > 5
ORDER BY total DESC;
```
