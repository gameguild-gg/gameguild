# Aggregation & Grouping

Aggregate functions compute a single result from a set of input values. Combined with `GROUP BY`, they enable powerful data summarization and analysis. This lesson covers SQL's aggregate functions, grouping, filtering groups, and common patterns.

---

## Aggregate Functions Overview

Aggregate functions process multiple rows and return a single value.

| Function        | Description           | NULL Handling                   |
| --------------- | --------------------- | ------------------------------- |
| `COUNT(*)`      | Count all rows        | Counts all rows including NULLs |
| `COUNT(column)` | Count non-NULL values | Ignores NULLs                   |
| `SUM(column)`   | Sum of values         | Ignores NULLs                   |
| `AVG(column)`   | Average of values     | Ignores NULLs                   |
| `MIN(column)`   | Minimum value         | Ignores NULLs                   |
| `MAX(column)`   | Maximum value         | Ignores NULLs                   |

---

## COUNT

### COUNT(\*) - Count All Rows

Counts every row, including those with NULL values.

```sql
-- Total number of orders
SELECT COUNT(*) FROM orders;

-- Total number of products
SELECT COUNT(*) AS total_products FROM products;
```

### COUNT(column) - Count Non-NULL Values

Counts only rows where the specified column is NOT NULL.

```sql
-- Count orders with shipping dates (excludes pending orders)
SELECT COUNT(shipped_at) AS shipped_orders FROM orders;

-- Compare total vs shipped
SELECT
    COUNT(*) AS total_orders,
    COUNT(shipped_at) AS shipped_orders,
    COUNT(*) - COUNT(shipped_at) AS pending_orders
FROM orders;
```

### COUNT(DISTINCT column)

Counts unique non-NULL values.

```sql
-- How many different customers have placed orders?
SELECT COUNT(DISTINCT customer_id) AS unique_customers FROM orders;

-- How many different categories do we sell?
SELECT COUNT(DISTINCT category_id) AS category_count FROM products;

-- Compare total vs unique
SELECT
    COUNT(email) AS total_emails,
    COUNT(DISTINCT email) AS unique_emails
FROM newsletter_subscribers;
```

---

## SUM

Calculates the total of numeric values. Returns NULL if all values are NULL.

```sql
-- Total revenue
SELECT SUM(total_amount) AS total_revenue FROM orders;

-- Total quantity sold
SELECT SUM(quantity) AS items_sold FROM order_items;

-- Sum with calculation
SELECT SUM(quantity * unit_price) AS revenue FROM order_items;

-- Sum of specific subset
SELECT SUM(total_amount) AS january_revenue
FROM orders
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

### Handling NULL in SUM

```sql
-- If all values are NULL, SUM returns NULL
-- Use COALESCE to default to 0
SELECT COALESCE(SUM(bonus), 0) AS total_bonuses FROM employees;
```

---

## AVG

Calculates the arithmetic mean. Only considers non-NULL values.

```sql
-- Average order value
SELECT AVG(total_amount) AS avg_order_value FROM orders;

-- Average with rounding
SELECT ROUND(AVG(price), 2) AS avg_price FROM products;

-- Average rating
SELECT
    AVG(rating) AS avg_rating,
    COUNT(rating) AS review_count
FROM reviews;
```

### AVG vs SUM/COUNT

```sql
-- These are equivalent:
SELECT AVG(price) FROM products;
SELECT SUM(price) / COUNT(price) FROM products;  -- Only non-NULL prices

-- But NOT the same as:
SELECT SUM(price) / COUNT(*) FROM products;  -- Includes NULL rows in count
```

### Average with NULL Consideration

```sql
-- Include NULLs as zeros in average calculation
SELECT AVG(COALESCE(discount, 0)) AS avg_discount FROM products;

-- Or explicitly:
SELECT SUM(COALESCE(discount, 0)) / COUNT(*) AS avg_discount_all FROM products;
```

---

## MIN and MAX

Find the smallest and largest values. Works with numbers, strings, and dates.

### Numeric MIN/MAX

```sql
-- Price range
SELECT
    MIN(price) AS cheapest,
    MAX(price) AS most_expensive,
    MAX(price) - MIN(price) AS price_range
FROM products;

-- Order value extremes
SELECT
    MIN(total_amount) AS smallest_order,
    MAX(total_amount) AS largest_order
FROM orders;
```

### Date MIN/MAX

```sql
-- First and last order dates
SELECT
    MIN(created_at) AS first_order,
    MAX(created_at) AS latest_order
FROM orders;

-- Customer's first purchase
SELECT
    customer_id,
    MIN(created_at) AS first_purchase
FROM orders
GROUP BY customer_id;
```

### String MIN/MAX

Returns alphabetically first/last values.

```sql
-- Alphabetically first and last product names
SELECT
    MIN(name) AS first_alphabetically,
    MAX(name) AS last_alphabetically
FROM products;
```

---

## GROUP BY

`GROUP BY` divides rows into groups and applies aggregate functions to each group.

### Basic Grouping

```sql
-- Count orders per status
SELECT
    status,
    COUNT(*) AS order_count
FROM orders
GROUP BY status;

-- Revenue per category
SELECT
    category_id,
    SUM(price * stock_quantity) AS inventory_value
FROM products
GROUP BY category_id;
```

### Multiple Group Columns

```sql
-- Orders by year and month
SELECT
    EXTRACT(YEAR FROM created_at) AS year,
    EXTRACT(MONTH FROM created_at) AS month,
    COUNT(*) AS order_count,
    SUM(total_amount) AS revenue
FROM orders
GROUP BY
    EXTRACT(YEAR FROM created_at),
    EXTRACT(MONTH FROM created_at)
ORDER BY year, month;

-- Orders by status and payment method
SELECT
    status,
    payment_method,
    COUNT(*) AS order_count
FROM orders
GROUP BY status, payment_method
ORDER BY status, order_count DESC;
```

### GROUP BY with Expressions

```sql
-- Group by price tier
SELECT
    CASE
        WHEN price < 10 THEN 'Budget'
        WHEN price < 50 THEN 'Mid-Range'
        WHEN price < 100 THEN 'Premium'
        ELSE 'Luxury'
    END AS price_tier,
    COUNT(*) AS product_count,
    AVG(price) AS avg_price
FROM products
GROUP BY
    CASE
        WHEN price < 10 THEN 'Budget'
        WHEN price < 50 THEN 'Mid-Range'
        WHEN price < 100 THEN 'Premium'
        ELSE 'Luxury'
    END;

-- Group by date (ignoring time)
SELECT
    DATE(created_at) AS order_date,
    COUNT(*) AS order_count
FROM orders
GROUP BY DATE(created_at)
ORDER BY order_date;
```

---

## HAVING

`HAVING` filters groups after aggregation. It's like `WHERE` but for groups.

| Clause   | Filters         | When Evaluated  |
| -------- | --------------- | --------------- |
| `WHERE`  | Individual rows | Before grouping |
| `HAVING` | Groups          | After grouping  |

### Basic HAVING

```sql
-- Categories with more than 10 products
SELECT
    category_id,
    COUNT(*) AS product_count
FROM products
GROUP BY category_id
HAVING COUNT(*) > 10;

-- Customers who spent more than $1000
SELECT
    customer_id,
    SUM(total_amount) AS total_spent
FROM orders
GROUP BY customer_id
HAVING SUM(total_amount) > 1000
ORDER BY total_spent DESC;
```

### WHERE + HAVING Together

```sql
-- Active products by category, only categories with 5+ products
SELECT
    category_id,
    COUNT(*) AS active_products,
    AVG(price) AS avg_price
FROM products
WHERE is_active = true            -- Filter rows first
GROUP BY category_id
HAVING COUNT(*) >= 5              -- Filter groups after
ORDER BY active_products DESC;
```

### HAVING with Multiple Conditions

```sql
-- High-value categories with good inventory
SELECT
    category_id,
    COUNT(*) AS product_count,
    SUM(price) AS total_value,
    AVG(stock_quantity) AS avg_stock
FROM products
GROUP BY category_id
HAVING
    COUNT(*) >= 5
    AND SUM(price) > 500
    AND AVG(stock_quantity) > 10;
```

### Common HAVING Patterns

```sql
-- Find duplicates
SELECT
    email,
    COUNT(*) AS occurrences
FROM users
GROUP BY email
HAVING COUNT(*) > 1;

-- Products never ordered (using subquery)
SELECT id, name
FROM products
WHERE id NOT IN (
    SELECT DISTINCT product_id
    FROM order_items
    WHERE product_id IS NOT NULL
);
```

---

## DISTINCT in Aggregates

Use `DISTINCT` inside aggregate functions to count/sum unique values only.

```sql
-- Total orders vs unique customers who ordered
SELECT
    COUNT(*) AS total_orders,
    COUNT(DISTINCT customer_id) AS unique_customers
FROM orders;

-- Average orders per customer
SELECT
    COUNT(*) * 1.0 / COUNT(DISTINCT customer_id) AS avg_orders_per_customer
FROM orders;

-- Unique products per category
SELECT
    category_id,
    COUNT(DISTINCT name) AS unique_product_names,
    COUNT(*) AS total_products
FROM products
GROUP BY category_id;
```

---

## Query Execution Order

Understanding execution order helps write correct queries:

```
1. FROM        -- Source table(s)
2. WHERE       -- Filter rows
3. GROUP BY    -- Create groups
4. HAVING      -- Filter groups
5. SELECT      -- Compute expressions and aggregates
6. DISTINCT    -- Remove duplicates
7. ORDER BY    -- Sort results
8. LIMIT       -- Limit output
```

### Why This Matters

```sql
-- WRONG: Can't use alias in WHERE (SELECT runs after WHERE)
SELECT
    customer_id,
    SUM(total_amount) AS total_spent
FROM orders
WHERE total_spent > 1000  -- ERROR: column "total_spent" does not exist
GROUP BY customer_id;

-- CORRECT: Use HAVING for aggregate conditions
SELECT
    customer_id,
    SUM(total_amount) AS total_spent
FROM orders
GROUP BY customer_id
HAVING SUM(total_amount) > 1000;

-- CORRECT: Can use alias in ORDER BY (runs after SELECT)
SELECT
    customer_id,
    SUM(total_amount) AS total_spent
FROM orders
GROUP BY customer_id
ORDER BY total_spent DESC;
```

---

## Common Aggregate Patterns

### Top N per Group

```sql
-- Top 3 customers by spending
SELECT
    customer_id,
    SUM(total_amount) AS total_spent
FROM orders
GROUP BY customer_id
ORDER BY total_spent DESC
LIMIT 3;
```

### Running Totals (Window Function Preview)

```sql
-- Daily revenue with running total
SELECT
    DATE(created_at) AS order_date,
    SUM(total_amount) AS daily_revenue,
    SUM(SUM(total_amount)) OVER (ORDER BY DATE(created_at)) AS running_total
FROM orders
GROUP BY DATE(created_at)
ORDER BY order_date;
```

### Percentage of Total

```sql
-- Category revenue as percentage of total
SELECT
    category_id,
    SUM(price) AS category_total,
    ROUND(
        SUM(price) * 100.0 / (SELECT SUM(price) FROM products),
        2
    ) AS percentage
FROM products
GROUP BY category_id
ORDER BY percentage DESC;
```

### Pivot-like Aggregation

```sql
-- Count orders by status (columns instead of rows)
SELECT
    COUNT(CASE WHEN status = 'pending' THEN 1 END) AS pending,
    COUNT(CASE WHEN status = 'shipped' THEN 1 END) AS shipped,
    COUNT(CASE WHEN status = 'delivered' THEN 1 END) AS delivered,
    COUNT(CASE WHEN status = 'cancelled' THEN 1 END) AS cancelled
FROM orders;

-- Monthly breakdown
SELECT
    EXTRACT(YEAR FROM created_at) AS year,
    SUM(CASE WHEN EXTRACT(MONTH FROM created_at) = 1 THEN total_amount ELSE 0 END) AS jan,
    SUM(CASE WHEN EXTRACT(MONTH FROM created_at) = 2 THEN total_amount ELSE 0 END) AS feb,
    SUM(CASE WHEN EXTRACT(MONTH FROM created_at) = 3 THEN total_amount ELSE 0 END) AS mar
    -- ... continue for other months
FROM orders
GROUP BY EXTRACT(YEAR FROM created_at);
```

### Conditional Aggregation

```sql
-- Compare active vs inactive products
SELECT
    category_id,
    COUNT(*) AS total,
    COUNT(*) FILTER (WHERE is_active = true) AS active,
    COUNT(*) FILTER (WHERE is_active = false) AS inactive,
    AVG(price) FILTER (WHERE is_active = true) AS avg_active_price
FROM products
GROUP BY category_id;
```

> **Note:** `FILTER (WHERE ...)` is PostgreSQL-specific. For other databases, use `CASE WHEN`:
>
> ```sql
> COUNT(CASE WHEN is_active = true THEN 1 END) AS active
> ```

---

## Statistical Aggregates (PostgreSQL)

PostgreSQL provides additional statistical functions:

```sql
SELECT
    -- Basic stats
    COUNT(*) AS n,
    AVG(price) AS mean,

    -- Variance and standard deviation
    VAR_POP(price) AS variance_population,
    VAR_SAMP(price) AS variance_sample,
    STDDEV_POP(price) AS stddev_population,
    STDDEV_SAMP(price) AS stddev_sample,

    -- Percentiles
    PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY price) AS median,
    PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY price) AS q1,
    PERCENTILE_CONT(0.75) WITHIN GROUP (ORDER BY price) AS q3,

    -- Mode
    MODE() WITHIN GROUP (ORDER BY category_id) AS most_common_category
FROM products;
```

---

## Array Aggregates (PostgreSQL)

Collect values into arrays:

```sql
-- List all product names per category
SELECT
    category_id,
    ARRAY_AGG(name ORDER BY name) AS product_names,
    ARRAY_AGG(DISTINCT price ORDER BY price) AS unique_prices
FROM products
GROUP BY category_id;

-- Concatenate as string
SELECT
    category_id,
    STRING_AGG(name, ', ' ORDER BY name) AS product_names
FROM products
GROUP BY category_id;
```

---

## Practice Exercises

### Exercise 1: Basic Aggregation

Write queries to find:

1. Total number of products, total inventory value, and average price
2. Minimum and maximum order dates
3. Count of unique categories that have products

### Exercise 2: GROUP BY

1. Count products per category
2. Calculate average order value per customer
3. Find total revenue by year and month

### Exercise 3: HAVING

1. Find customers with more than 5 orders
2. Find categories where average product price exceeds $50
3. Find duplicate email addresses in the users table

### Exercise 4: Combined Analysis

Write a single query that shows for each category:

- Category name
- Number of products
- Number of active products
- Average price
- Total inventory value
- Only include categories with at least 3 products
- Sort by total inventory value descending

### Exercise 5: Business Questions

1. What percentage of orders are in each status?
2. Which day of the week has the most orders?
3. What is the average time between a customer's first and second order?

---

## Key Takeaways

1. **COUNT(\*)** counts all rows; **COUNT(column)** counts non-NULLs
2. **Aggregates ignore NULLs** (except COUNT(\*))
3. **GROUP BY** creates groups; all non-aggregated columns must be in GROUP BY
4. **WHERE** filters rows before grouping; **HAVING** filters groups after
5. **DISTINCT** in aggregates counts/sums unique values only
6. Use **COALESCE** to handle NULL in calculations
7. **Query execution order** matters for understanding what you can reference where

---

## Quick Reference

```sql
-- Counting
COUNT(*)                    -- All rows
COUNT(column)              -- Non-NULL values
COUNT(DISTINCT column)     -- Unique non-NULL values

-- Math aggregates
SUM(column)                -- Total
AVG(column)                -- Average
MIN(column)                -- Minimum
MAX(column)                -- Maximum

-- Grouping
GROUP BY column1, column2  -- Create groups
HAVING condition           -- Filter groups

-- Patterns
COALESCE(SUM(x), 0)        -- Handle NULL result
ROUND(AVG(x), 2)           -- Round average
COUNT(*) FILTER (WHERE x)  -- Conditional count (PostgreSQL)

-- Example structure
SELECT
    group_column,
    COUNT(*) AS count,
    SUM(value) AS total
FROM table
WHERE row_condition        -- Filter rows first
GROUP BY group_column
HAVING COUNT(*) > 5        -- Filter groups after
ORDER BY total DESC
LIMIT 10;
```
