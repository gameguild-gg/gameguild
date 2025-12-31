# Outer Joins & Advanced Patterns

While INNER JOIN returns only matching rows, outer joins include non-matching rows from one or both tables. This lesson covers LEFT, RIGHT, and FULL OUTER JOINs, plus advanced patterns like self-joins and CROSS JOINs.

---

## Outer Joins Overview

| Join Type | Returns |
|-----------|---------|
| `INNER JOIN` | Only matching rows |
| `LEFT JOIN` | All rows from left table + matches from right |
| `RIGHT JOIN` | All rows from right table + matches from left |
| `FULL OUTER JOIN` | All rows from both tables |

---

## LEFT JOIN (LEFT OUTER JOIN)

Returns **all rows from the left table** and matching rows from the right table. Non-matching right-side columns are filled with NULL.

### Syntax

```sql
SELECT columns
FROM left_table
LEFT JOIN right_table ON left_table.column = right_table.column;

-- LEFT OUTER JOIN is equivalent
SELECT columns
FROM left_table
LEFT OUTER JOIN right_table ON left_table.column = right_table.column;
```

### Visual Example

```
customers (LEFT)              orders (RIGHT)
+----+-------+               +----+-------------+
| id | name  |               | id | customer_id |
+----+-------+               +----+-------------+
| 1  | Alice |               | 1  | 1           |
| 2  | Bob   |               | 2  | 1           |
| 3  | Carol |               | 3  | 2           |
+----+-------+               +----+-------------+

LEFT JOIN Result:
+----+-------+----------+
| id | name  | order_id |
+----+-------+----------+
| 1  | Alice | 1        |
| 1  | Alice | 2        |
| 2  | Bob   | 3        |
| 3  | Carol | NULL     |  ← Carol has no orders
+----+-------+----------+
```

### Practical Examples

```sql
-- All customers, with their orders (if any)
SELECT 
    c.id,
    c.name,
    o.id AS order_id,
    o.total_amount
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id;

-- All products, with their category (even uncategorized)
SELECT 
    p.name AS product,
    COALESCE(cat.name, 'Uncategorized') AS category
FROM products p
LEFT JOIN categories cat ON p.category_id = cat.id;
```

### Finding Non-Matching Rows

A common pattern: find rows with no match in the other table.

```sql
-- Customers who have never ordered
SELECT c.id, c.name, c.email
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;

-- Products never sold
SELECT p.id, p.name
FROM products p
LEFT JOIN order_items oi ON p.id = oi.product_id
WHERE oi.id IS NULL;

-- Categories with no products
SELECT cat.id, cat.name
FROM categories cat
LEFT JOIN products p ON cat.id = p.category_id
WHERE p.id IS NULL;
```

---

## RIGHT JOIN (RIGHT OUTER JOIN)

Returns **all rows from the right table** and matching rows from the left table. The mirror of LEFT JOIN.

### Syntax

```sql
SELECT columns
FROM left_table
RIGHT JOIN right_table ON left_table.column = right_table.column;
```

### Visual Example

```
orders (LEFT)                 customers (RIGHT)
+----+-------------+         +----+-------+
| id | customer_id |         | id | name  |
+----+-------------+         +----+-------+
| 1  | 1           |         | 1  | Alice |
| 2  | 1           |         | 2  | Bob   |
| 3  | 2           |         | 3  | Carol |
+----+-------------+         +----+-------+

RIGHT JOIN Result:
+----------+----+-------+
| order_id | id | name  |
+----------+----+-------+
| 1        | 1  | Alice |
| 2        | 1  | Alice |
| 3        | 2  | Bob   |
| NULL     | 3  | Carol |  ← Carol has no orders
+----------+----+-------+
```

### RIGHT JOIN vs LEFT JOIN

RIGHT JOIN is rarely used because LEFT JOIN is more intuitive. Any RIGHT JOIN can be rewritten as LEFT JOIN by swapping table positions:

```sql
-- Using RIGHT JOIN
SELECT *
FROM orders o
RIGHT JOIN customers c ON o.customer_id = c.id;

-- Equivalent LEFT JOIN (preferred)
SELECT *
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id;
```

**Best Practice:** Use LEFT JOIN consistently for readability.

---

## FULL OUTER JOIN

Returns **all rows from both tables**. Non-matching rows from either side have NULLs for the other table's columns.

### Syntax

```sql
SELECT columns
FROM table1
FULL OUTER JOIN table2 ON table1.column = table2.column;

-- Or simply:
FULL JOIN table2 ON ...
```

### Visual Example

```
employees                     departments
+----+-------+--------+      +----+-------------+
| id | name  | dept_id|      | id | name        |
+----+-------+--------+      +----+-------------+
| 1  | Alice | 1      |      | 1  | Engineering |
| 2  | Bob   | 1      |      | 2  | Marketing   |
| 3  | Carol | NULL   |      | 3  | Sales       |
+----+-------+--------+      +----+-------------+

FULL OUTER JOIN Result:
+------+-------+---------+-------------+
| e_id | name  | dept_id | dept_name   |
+------+-------+---------+-------------+
| 1    | Alice | 1       | Engineering |
| 2    | Bob   | 1       | Engineering |
| 3    | Carol | NULL    | NULL        |  ← Employee with no department
| NULL | NULL  | NULL    | Marketing   |  ← Department with no employees
| NULL | NULL  | NULL    | Sales       |  ← Department with no employees
+------+-------+---------+-------------+
```

### Practical Examples

```sql
-- All employees and departments, showing gaps
SELECT 
    e.name AS employee,
    d.name AS department
FROM employees e
FULL OUTER JOIN departments d ON e.department_id = d.id;

-- Find orphaned records on both sides
SELECT 
    e.id AS orphan_employee_id,
    d.id AS empty_department_id
FROM employees e
FULL OUTER JOIN departments d ON e.department_id = d.id
WHERE e.department_id IS NULL OR d.id IS NULL;
```

### Simulating FULL OUTER JOIN (MySQL)

MySQL doesn't support FULL OUTER JOIN directly. Use UNION:

```sql
-- MySQL workaround
SELECT e.name, d.name
FROM employees e
LEFT JOIN departments d ON e.department_id = d.id
UNION
SELECT e.name, d.name
FROM employees e
RIGHT JOIN departments d ON e.department_id = d.id;
```

---

## Self-Joins

A **self-join** joins a table to itself. Essential for hierarchical data and comparisons within the same table.

### Syntax

```sql
SELECT columns
FROM table t1
JOIN table t2 ON t1.column = t2.column;
```

### Employee-Manager Hierarchy

```sql
-- Employees with their manager names
SELECT 
    e.name AS employee,
    m.name AS manager
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id;
```

**Data Example:**
```
employees
+----+-------+------------+
| id | name  | manager_id |
+----+-------+------------+
| 1  | CEO   | NULL       |
| 2  | Alice | 1          |
| 3  | Bob   | 2          |
| 4  | Carol | 2          |
+----+-------+------------+

Result:
+----------+---------+
| employee | manager |
+----------+---------+
| CEO      | NULL    |
| Alice    | CEO     |
| Bob      | Alice   |
| Carol    | Alice   |
+----------+---------+
```

### Category Hierarchy

```sql
-- Categories with parent category names
SELECT 
    c.name AS category,
    p.name AS parent_category
FROM categories c
LEFT JOIN categories p ON c.parent_id = p.id;
```

### Comparing Rows Within Same Table

```sql
-- Find products with the same price
SELECT 
    p1.name AS product_1,
    p2.name AS product_2,
    p1.price
FROM products p1
JOIN products p2 ON p1.price = p2.price AND p1.id < p2.id;

-- Find employees hired on the same day
SELECT 
    e1.name AS employee_1,
    e2.name AS employee_2,
    e1.hire_date
FROM employees e1
JOIN employees e2 ON e1.hire_date = e2.hire_date AND e1.id < e2.id;
```

> **Note:** `p1.id < p2.id` prevents duplicates (A,B) and (B,A) and self-matches (A,A).

### Multi-Level Hierarchy

```sql
-- Three levels: Employee → Manager → Director
SELECT 
    e.name AS employee,
    m.name AS manager,
    d.name AS director
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id
LEFT JOIN employees d ON m.manager_id = d.id;
```

---

## CROSS JOIN

Returns the **Cartesian product** — every row from the first table paired with every row from the second table.

### Syntax

```sql
-- Explicit CROSS JOIN
SELECT columns
FROM table1
CROSS JOIN table2;

-- Implicit (comma syntax)
SELECT columns
FROM table1, table2;
```

### Visual Example

```
colors              sizes
+-------+           +-------+
| name  |           | name  |
+-------+           +-------+
| Red   |           | S     |
| Blue  |           | M     |
+-------+           | L     |
                    +-------+

CROSS JOIN Result (2 × 3 = 6 rows):
+-------+------+
| color | size |
+-------+------+
| Red   | S    |
| Red   | M    |
| Red   | L    |
| Blue  | S    |
| Blue  | M    |
| Blue  | L    |
+-------+------+
```

### Use Cases

**1. Generate All Combinations:**
```sql
-- All product variants (color × size)
SELECT 
    c.name AS color,
    s.name AS size,
    CONCAT(p.name, ' - ', c.name, ' ', s.name) AS variant_name
FROM products p
CROSS JOIN colors c
CROSS JOIN sizes s;
```

**2. Calendar/Time Series Generation:**
```sql
-- All combinations of years and months
SELECT y.year, m.month
FROM 
    (SELECT generate_series(2020, 2026) AS year) y
CROSS JOIN 
    (SELECT generate_series(1, 12) AS month) m;
```

**3. Comparison Matrix:**
```sql
-- Compare each product with every other product
SELECT 
    p1.name AS product_1,
    p2.name AS product_2,
    p1.price - p2.price AS price_difference
FROM products p1
CROSS JOIN products p2
WHERE p1.id != p2.id;
```

### CROSS JOIN Warning

⚠️ **Be careful with large tables!** 
- 1,000 × 1,000 = 1,000,000 rows
- 10,000 × 10,000 = 100,000,000 rows

---

## Join Visualization Mental Models

### Venn Diagram Model

```
INNER JOIN:      LEFT JOIN:       RIGHT JOIN:      FULL OUTER:
    ┌───┐            ┌───┐            ┌───┐            ┌───┐
  ┌─┤███├─┐        ┌─┤███├─┐        ┌─┤███├─┐        ┌─┤███├─┐
  │ └───┘ │        │█└───┘ │        │ └───┘█│        │█└───┘█│
  │   A   │        │   A   │        │   A   │        │   A   │
  └───────┘        └───────┘        └───────┘        └───────┘
     ∩ B              + B              + B              ∪ B
```

### Row Matching Model

Think of joins as matching rows:

| Join Type | Left Unmatched | Both Matched | Right Unmatched |
|-----------|----------------|--------------|-----------------|
| INNER | ❌ | ✅ | ❌ |
| LEFT | ✅ (+ NULLs) | ✅ | ❌ |
| RIGHT | ❌ | ✅ | ✅ (+ NULLs) |
| FULL OUTER | ✅ (+ NULLs) | ✅ | ✅ (+ NULLs) |

---

## Filtering with Outer Joins

### WHERE vs ON — Critical Difference!

For outer joins, WHERE and ON behave differently:

```sql
-- ON: Filter BEFORE joining (preserves left rows)
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id AND o.status = 'completed';
-- Returns ALL customers; orders show only if completed

-- WHERE: Filter AFTER joining (removes non-matching rows)
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.status = 'completed';
-- Returns only customers WITH completed orders (like INNER JOIN!)
```

### Correct Patterns

```sql
-- All customers, but only show their 2026 orders
SELECT c.name, o.id, o.created_at
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id 
                   AND o.created_at >= '2026-01-01';

-- Only customers who ordered in 2026
SELECT c.name, o.id, o.created_at
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.created_at >= '2026-01-01';

-- All customers, show 2026 orders, filter by customer country
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id 
                   AND o.created_at >= '2026-01-01'
WHERE c.country = 'USA';  -- Filter on LEFT table is OK in WHERE
```

---

## Combining Multiple Join Types

You can mix join types in a single query:

```sql
-- All products with category (inner) and optional reviews (left)
SELECT 
    p.name AS product,
    c.name AS category,
    r.rating
FROM products p
INNER JOIN categories c ON p.category_id = c.id      -- Must have category
LEFT JOIN reviews r ON p.id = r.product_id;          -- Reviews optional

-- Complex multi-table query
SELECT 
    c.name AS customer,
    o.id AS order_id,
    p.name AS product,
    w.name AS warehouse
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id           -- All customers
LEFT JOIN order_items oi ON o.id = oi.order_id       -- Order items if order exists
LEFT JOIN products p ON oi.product_id = p.id         -- Product if item exists  
LEFT JOIN inventory i ON p.id = i.product_id         -- Inventory if product exists
LEFT JOIN warehouses w ON i.warehouse_id = w.id;     -- Warehouse if inventory exists
```

---

## Anti-Joins and Semi-Joins

### Anti-Join (NOT EXISTS / LEFT JOIN + NULL)

Find rows with **no match** in another table.

```sql
-- Using LEFT JOIN + IS NULL
SELECT c.*
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;

-- Using NOT EXISTS (often faster)
SELECT c.*
FROM customers c
WHERE NOT EXISTS (
    SELECT 1 FROM orders o WHERE o.customer_id = c.id
);

-- Using NOT IN (watch for NULLs!)
SELECT c.*
FROM customers c
WHERE c.id NOT IN (SELECT customer_id FROM orders WHERE customer_id IS NOT NULL);
```

### Semi-Join (EXISTS)

Find rows that **have a match** without duplicating for multiple matches.

```sql
-- Customers who have placed at least one order
SELECT c.*
FROM customers c
WHERE EXISTS (
    SELECT 1 FROM orders o WHERE o.customer_id = c.id
);

-- Products that have been ordered
SELECT p.*
FROM products p
WHERE EXISTS (
    SELECT 1 FROM order_items oi WHERE oi.product_id = p.id
);
```

---

## Performance Considerations

### Index Your Join Columns

```sql
-- These columns should be indexed:
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);
```

### Choose the Right Join Type

```sql
-- If you only need matching rows, use INNER JOIN
-- Don't use LEFT JOIN "just in case"

-- INNER JOIN (more efficient if you need only matches)
SELECT * FROM orders o JOIN customers c ON o.customer_id = c.id;

-- LEFT JOIN (only if you need unmatched rows too)
SELECT * FROM orders o LEFT JOIN customers c ON o.customer_id = c.id;
```

### Limit Early When Possible

```sql
-- Filter before joining when possible
SELECT c.name, o.total
FROM (
    SELECT * FROM orders WHERE created_at >= '2026-01-01'
) o
JOIN customers c ON o.customer_id = c.id;
```

---

## Practice Exercises

### Exercise 1: Outer Joins
1. List all customers with their orders (include customers with no orders)
2. Find products that have never been ordered
3. Show all departments with employee counts (including empty departments)

### Exercise 2: Self-Joins
1. Display employees with their manager's name
2. Find products with the same price
3. Show categories and their parent categories (3 levels deep)

### Exercise 3: Mixed Joins
1. All customers, their orders, and order items (all levels optional)
2. Products with categories (required) and reviews (optional)
3. Find customers who have ordered but never reviewed a product

### Exercise 4: Anti-Joins
1. Categories with no products
2. Customers who haven't ordered in 2026
3. Products not in any warehouse inventory

---

## Key Takeaways

1. **LEFT JOIN** keeps all rows from the left table
2. **RIGHT JOIN** is just LEFT JOIN with swapped tables—use LEFT JOIN for consistency
3. **FULL OUTER JOIN** keeps all rows from both tables
4. **Self-joins** connect a table to itself for hierarchies and comparisons
5. **CROSS JOIN** produces all combinations (Cartesian product)
6. **WHERE vs ON matters** for outer joins—ON filters before joining, WHERE after
7. **Anti-joins** (LEFT JOIN + IS NULL or NOT EXISTS) find non-matching rows

---

## Quick Reference

```sql
-- LEFT JOIN: All from left, matches from right
SELECT * FROM a LEFT JOIN b ON a.id = b.a_id;

-- RIGHT JOIN: All from right, matches from left
SELECT * FROM a RIGHT JOIN b ON a.id = b.a_id;

-- FULL OUTER JOIN: All from both
SELECT * FROM a FULL OUTER JOIN b ON a.id = b.a_id;

-- Self-join: Table joined to itself
SELECT * FROM employees e JOIN employees m ON e.manager_id = m.id;

-- CROSS JOIN: All combinations
SELECT * FROM a CROSS JOIN b;

-- Anti-join: No match exists
SELECT * FROM a LEFT JOIN b ON a.id = b.a_id WHERE b.a_id IS NULL;

-- Semi-join: Match exists (no duplicates)
SELECT * FROM a WHERE EXISTS (SELECT 1 FROM b WHERE b.a_id = a.id);
```
