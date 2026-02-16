# CTEs & Views

Common Table Expressions (CTEs) and Views provide ways to organize complex queries, improve readability, and create reusable query components. This lesson covers CTEs, recursive CTEs, views, and materialized views.

---

## Common Table Expressions (CTEs)

A **CTE** is a temporary named result set defined within a query using the `WITH` clause. It exists only for the duration of the query.

### Basic Syntax

```sql
WITH cte_name AS (
    SELECT ...
)
SELECT * FROM cte_name;
```

### Simple CTE Example

```sql
-- Without CTE (hard to read)
SELECT * FROM (
    SELECT 
        customer_id,
        COUNT(*) AS order_count,
        SUM(total_amount) AS total_spent
    FROM orders
    GROUP BY customer_id
) customer_stats
WHERE order_count > 5;

-- With CTE (cleaner)
WITH customer_stats AS (
    SELECT 
        customer_id,
        COUNT(*) AS order_count,
        SUM(total_amount) AS total_spent
    FROM orders
    GROUP BY customer_id
)
SELECT * FROM customer_stats
WHERE order_count > 5;
```

### Multiple CTEs

Chain multiple CTEs with commas:

```sql
WITH 
-- First CTE: customer order statistics
customer_orders AS (
    SELECT 
        customer_id,
        COUNT(*) AS order_count,
        SUM(total_amount) AS total_spent
    FROM orders
    GROUP BY customer_id
),
-- Second CTE: categorize customers
customer_tiers AS (
    SELECT 
        customer_id,
        order_count,
        total_spent,
        CASE 
            WHEN total_spent >= 10000 THEN 'Platinum'
            WHEN total_spent >= 5000 THEN 'Gold'
            WHEN total_spent >= 1000 THEN 'Silver'
            ELSE 'Bronze'
        END AS tier
    FROM customer_orders
)
-- Main query: join with customer details
SELECT 
    c.name,
    c.email,
    ct.order_count,
    ct.total_spent,
    ct.tier
FROM customers c
JOIN customer_tiers ct ON c.id = ct.customer_id
ORDER BY ct.total_spent DESC;
```

### CTEs Referencing Other CTEs

Later CTEs can reference earlier ones:

```sql
WITH 
daily_sales AS (
    SELECT 
        DATE(created_at) AS sale_date,
        SUM(total_amount) AS daily_total
    FROM orders
    GROUP BY DATE(created_at)
),
weekly_avg AS (
    SELECT AVG(daily_total) AS avg_daily_sales
    FROM daily_sales
)
SELECT 
    ds.sale_date,
    ds.daily_total,
    wa.avg_daily_sales,
    ds.daily_total - wa.avg_daily_sales AS diff_from_avg
FROM daily_sales ds
CROSS JOIN weekly_avg wa
ORDER BY ds.sale_date;
```

---

## CTE vs Subquery

| Aspect | CTE | Subquery |
|--------|-----|----------|
| **Readability** | Named, top-down | Nested, inside-out |
| **Reusability** | Can reference multiple times | Must repeat |
| **Recursion** | Supported | Not supported |
| **Performance** | Usually same | Usually same |

### When to Use CTEs

1. **Complex queries** with multiple steps
2. **Reusing** the same subquery multiple times
3. **Recursive** queries (hierarchies)
4. **Self-documenting** code with named steps

```sql
-- Subquery used twice (redundant)
SELECT * FROM orders
WHERE total_amount > (SELECT AVG(total_amount) FROM orders)
  AND customer_id IN (
      SELECT customer_id FROM orders 
      GROUP BY customer_id 
      HAVING AVG(total_amount) > (SELECT AVG(total_amount) FROM orders)
  );

-- CTE used twice (cleaner)
WITH avg_order AS (
    SELECT AVG(total_amount) AS avg_amount FROM orders
)
SELECT o.* FROM orders o, avg_order a
WHERE o.total_amount > a.avg_amount
  AND o.customer_id IN (
      SELECT customer_id FROM orders, avg_order 
      GROUP BY customer_id, avg_amount
      HAVING AVG(total_amount) > avg_amount
  );
```

---

## Recursive CTEs

Recursive CTEs reference themselves to process hierarchical or graph data.

### Syntax

```sql
WITH RECURSIVE cte_name AS (
    -- Anchor member (base case)
    SELECT ... 
    
    UNION [ALL]
    
    -- Recursive member (references cte_name)
    SELECT ... FROM cte_name WHERE ...
)
SELECT * FROM cte_name;
```

### Employee Hierarchy Example

```sql
-- Table structure
-- employees(id, name, manager_id)

WITH RECURSIVE org_chart AS (
    -- Anchor: top-level employees (no manager)
    SELECT 
        id, 
        name, 
        manager_id,
        1 AS level,
        name AS path
    FROM employees
    WHERE manager_id IS NULL
    
    UNION ALL
    
    -- Recursive: employees who report to someone in org_chart
    SELECT 
        e.id,
        e.name,
        e.manager_id,
        oc.level + 1,
        oc.path || ' > ' || e.name
    FROM employees e
    JOIN org_chart oc ON e.manager_id = oc.id
)
SELECT * FROM org_chart
ORDER BY path;
```

**Result:**
```
id | name    | manager_id | level | path
---+---------+------------+-------+------------------------
1  | CEO     | NULL       | 1     | CEO
2  | CTO     | 1          | 2     | CEO > CTO
4  | DevLead | 2          | 3     | CEO > CTO > DevLead
5  | Dev1    | 4          | 4     | CEO > CTO > DevLead > Dev1
3  | CFO     | 1          | 2     | CEO > CFO
```

### Category Hierarchy

```sql
WITH RECURSIVE category_tree AS (
    -- Root categories
    SELECT 
        id, 
        name, 
        parent_id,
        1 AS depth,
        ARRAY[id] AS path
    FROM categories
    WHERE parent_id IS NULL
    
    UNION ALL
    
    -- Child categories
    SELECT 
        c.id,
        c.name,
        c.parent_id,
        ct.depth + 1,
        ct.path || c.id
    FROM categories c
    JOIN category_tree ct ON c.parent_id = ct.id
)
SELECT 
    id,
    REPEAT('  ', depth - 1) || name AS indented_name,
    depth
FROM category_tree
ORDER BY path;
```

### Generating Series (Number Sequence)

```sql
-- Generate numbers 1 to 10
WITH RECURSIVE numbers AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM numbers WHERE n < 10
)
SELECT n FROM numbers;

-- Generate date series
WITH RECURSIVE dates AS (
    SELECT DATE '2026-01-01' AS dt
    UNION ALL
    SELECT dt + INTERVAL '1 day' FROM dates WHERE dt < '2026-01-31'
)
SELECT dt FROM dates;
```

### Finding All Descendants

```sql
-- All subordinates of employee ID 2
WITH RECURSIVE subordinates AS (
    SELECT id, name, manager_id
    FROM employees
    WHERE manager_id = 2
    
    UNION ALL
    
    SELECT e.id, e.name, e.manager_id
    FROM employees e
    JOIN subordinates s ON e.manager_id = s.id
)
SELECT * FROM subordinates;
```

### Preventing Infinite Loops

Add a depth limit or cycle detection:

```sql
WITH RECURSIVE tree AS (
    SELECT id, parent_id, 1 AS depth
    FROM nodes
    WHERE id = 1
    
    UNION ALL
    
    SELECT n.id, n.parent_id, t.depth + 1
    FROM nodes n
    JOIN tree t ON n.parent_id = t.id
    WHERE t.depth < 100  -- Depth limit
)
SELECT * FROM tree;
```

PostgreSQL has built-in cycle detection:

```sql
WITH RECURSIVE tree AS (
    SELECT id, parent_id, ARRAY[id] AS path, false AS cycle
    FROM nodes WHERE id = 1
    
    UNION ALL
    
    SELECT n.id, n.parent_id, t.path || n.id, n.id = ANY(t.path)
    FROM nodes n
    JOIN tree t ON n.parent_id = t.id
    WHERE NOT t.cycle
)
SELECT * FROM tree WHERE NOT cycle;
```

---

## Views

A **view** is a stored query that acts like a virtual table. It doesn't store data itself-it runs the underlying query when accessed.

### Creating Views

```sql
CREATE VIEW view_name AS
SELECT ...;

-- Example: Active products view
CREATE VIEW active_products AS
SELECT id, name, price, category_id
FROM products
WHERE is_active = true AND stock_quantity > 0;

-- Use like a table
SELECT * FROM active_products WHERE price < 50;
```

### View Benefits

1. **Simplification** - Hide complex joins and logic
2. **Security** - Expose only certain columns/rows
3. **Abstraction** - Shield applications from schema changes
4. **Reusability** - Define once, use everywhere

### Complex View Example

```sql
CREATE VIEW order_details AS
SELECT 
    o.id AS order_id,
    o.created_at AS order_date,
    c.name AS customer_name,
    c.email AS customer_email,
    p.name AS product_name,
    oi.quantity,
    oi.unit_price,
    (oi.quantity * oi.unit_price) AS line_total,
    o.total_amount AS order_total,
    o.status
FROM orders o
JOIN customers c ON o.customer_id = c.id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id;

-- Now use simply:
SELECT * FROM order_details WHERE status = 'pending';
SELECT customer_name, SUM(line_total) FROM order_details GROUP BY customer_name;
```

### Replacing Views

```sql
-- CREATE OR REPLACE overwrites existing view
CREATE OR REPLACE VIEW active_products AS
SELECT id, name, price, category_id, stock_quantity  -- Added column
FROM products
WHERE is_active = true AND stock_quantity > 0;
```

### Dropping Views

```sql
DROP VIEW view_name;
DROP VIEW IF EXISTS view_name;
DROP VIEW view_name CASCADE;  -- Drop dependent objects too
```

---

## Updatable Views

Some views can be updated (INSERT, UPDATE, DELETE), modifying the underlying table.

### Requirements for Updatable Views

- Based on a single table
- No DISTINCT, GROUP BY, HAVING, UNION
- No aggregate functions
- No subqueries in SELECT
- Includes all NOT NULL columns without defaults

### Updatable View Example

```sql
CREATE VIEW california_customers AS
SELECT id, name, email, phone
FROM customers
WHERE state = 'CA';

-- These work:
UPDATE california_customers SET phone = '555-0100' WHERE id = 1;
INSERT INTO california_customers (id, name, email, state) 
VALUES (100, 'New Customer', 'new@email.com', 'CA');

-- Row disappears from view if state changes:
UPDATE california_customers SET state = 'NY' WHERE id = 1;  -- Gone from view!
```

### WITH CHECK OPTION

Prevents updates that would make rows disappear from the view:

```sql
CREATE VIEW california_customers AS
SELECT id, name, email, phone, state
FROM customers
WHERE state = 'CA'
WITH CHECK OPTION;

-- This now fails:
UPDATE california_customers SET state = 'NY' WHERE id = 1;
-- ERROR: new row violates check option for view
```

---

## Materialized Views

A **materialized view** stores the query result physically. Unlike regular views, it contains actual data.

### Creating Materialized Views

```sql
CREATE MATERIALIZED VIEW mv_name AS
SELECT ...;

-- Example: Sales summary (expensive to compute)
CREATE MATERIALIZED VIEW sales_summary AS
SELECT 
    DATE_TRUNC('month', o.created_at) AS month,
    c.name AS category,
    COUNT(DISTINCT o.id) AS order_count,
    SUM(oi.quantity) AS units_sold,
    SUM(oi.quantity * oi.unit_price) AS revenue
FROM orders o
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
JOIN categories c ON p.category_id = c.id
WHERE o.status = 'completed'
GROUP BY DATE_TRUNC('month', o.created_at), c.name;
```

### Refreshing Materialized Views

Materialized views become stale. Refresh to update:

```sql
-- Full refresh (rebuilds entire view)
REFRESH MATERIALIZED VIEW sales_summary;

-- Concurrent refresh (doesn't lock reads, requires unique index)
CREATE UNIQUE INDEX ON sales_summary (month, category);
REFRESH MATERIALIZED VIEW CONCURRENTLY sales_summary;
```

### Refresh Strategies

| Strategy | When to Use |
|----------|-------------|
| **Manual** | Ad-hoc reporting, after batch loads |
| **Scheduled** | Regular intervals (cron job, pg_cron) |
| **Trigger-based** | After source table changes |
| **On-demand** | Before critical queries |

```sql
-- Scheduled refresh with pg_cron (PostgreSQL extension)
SELECT cron.schedule('refresh-sales', '0 * * * *',  -- Every hour
    'REFRESH MATERIALIZED VIEW CONCURRENTLY sales_summary');
```

### Materialized View vs Regular View

| Aspect | View | Materialized View |
|--------|------|-------------------|
| **Storage** | No data stored | Stores query result |
| **Speed** | Runs query each time | Fast (precomputed) |
| **Freshness** | Always current | May be stale |
| **Updates** | N/A | Requires REFRESH |
| **Indexes** | Cannot create | Can create indexes |
| **Use case** | Simple queries, security | Complex aggregations, reporting |

### Indexing Materialized Views

```sql
-- Create indexes for faster queries
CREATE INDEX idx_sales_summary_month ON sales_summary (month);
CREATE INDEX idx_sales_summary_category ON sales_summary (category);

-- Query uses index
SELECT * FROM sales_summary WHERE month = '2026-01-01';
```

---

## View Security

### Column-Level Security

```sql
-- Hide sensitive columns
CREATE VIEW public_employees AS
SELECT id, name, department, hire_date
FROM employees;
-- Excludes: salary, ssn, home_address
```

### Row-Level Security

```sql
-- Only active, public products
CREATE VIEW public_products AS
SELECT id, name, description, price
FROM products
WHERE is_active = true AND is_public = true;
```

### Schema-Based Access

```sql
-- Create views in a separate schema
CREATE SCHEMA api;

CREATE VIEW api.customers AS
SELECT id, name, email FROM internal.customers;

-- Grant access to views only
GRANT SELECT ON api.customers TO app_user;
-- Don't grant access to internal.customers
```

---

## Performance Considerations

### Views Don't Cache

Regular views execute the full query each time:

```sql
-- This runs the complex join every time
SELECT * FROM order_details WHERE customer_name = 'Alice';
```

### Materialized Views for Performance

Use materialized views for:
- Complex aggregations
- Expensive joins
- Reports and dashboards
- Search result caching

```sql
-- Fast: reads from stored data
SELECT * FROM sales_summary WHERE month = '2026-01-01';
```

### Query Optimization Through Views

Views can simplify but don't optimize:

```sql
-- The view
CREATE VIEW big_orders AS
SELECT * FROM orders WHERE total_amount > 1000;

-- These are equivalent in performance:
SELECT * FROM big_orders WHERE status = 'pending';
SELECT * FROM orders WHERE total_amount > 1000 AND status = 'pending';
```

---

## Practice Exercises

### Exercise 1: CTEs
1. Write a CTE to find customers with above-average order values
2. Use multiple CTEs to: (a) calculate category totals, (b) rank categories, (c) show top 5
3. Refactor a complex nested subquery into CTEs

### Exercise 2: Recursive CTEs
1. Display an employee hierarchy with indentation showing levels
2. Find all ancestors of a given category
3. Generate a calendar of dates for a given month

### Exercise 3: Views
1. Create a view showing order details with customer and product info
2. Create an updatable view for "VIP customers" (spent > $10,000)
3. Create a view that hides salary information from an employees table

### Exercise 4: Materialized Views
1. Create a materialized view for monthly sales by category
2. Add appropriate indexes to the materialized view
3. Write a refresh schedule strategy for a real-time dashboard vs. weekly reports

---

## Key Takeaways

1. **CTEs** improve readability with named, reusable query blocks
2. **Multiple CTEs** can reference each other in sequence
3. **Recursive CTEs** handle hierarchies and graph traversal
4. **Views** are virtual tables that run queries on access
5. **Updatable views** allow DML on simple views
6. **WITH CHECK OPTION** enforces view criteria on updates
7. **Materialized views** store results for fast access
8. **REFRESH** updates materialized view data

---

## Quick Reference

```sql
-- CTE
WITH cte_name AS (SELECT ...)
SELECT * FROM cte_name;

-- Multiple CTEs
WITH cte1 AS (...), cte2 AS (...) 
SELECT * FROM cte1 JOIN cte2 ...;

-- Recursive CTE
WITH RECURSIVE cte AS (
    SELECT ... -- anchor
    UNION ALL
    SELECT ... FROM cte WHERE ... -- recursive
)
SELECT * FROM cte;

-- View
CREATE VIEW view_name AS SELECT ...;
CREATE OR REPLACE VIEW view_name AS SELECT ...;
DROP VIEW view_name;

-- Updatable view with check
CREATE VIEW v AS SELECT ... WHERE condition
WITH CHECK OPTION;

-- Materialized view
CREATE MATERIALIZED VIEW mv AS SELECT ...;
REFRESH MATERIALIZED VIEW mv;
REFRESH MATERIALIZED VIEW CONCURRENTLY mv;
DROP MATERIALIZED VIEW mv;
```
