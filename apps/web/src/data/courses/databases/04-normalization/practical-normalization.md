# Practical Normalization & Denormalization

Normalization theory provides the foundation for good database design, but real-world applications require pragmatic decisions. This lesson covers when to normalize, when to denormalize, and how to analyze existing schemas.

---

## The Normalization Trade-off

### Benefits of Normalization (3NF)

| Benefit | Explanation |
|---------|-------------|
| **Data Integrity** | No update anomalies; change data in one place |
| **Storage Efficiency** | No redundant data; smaller database size |
| **Flexibility** | Easier to extend schema without affecting existing tables |
| **Consistency** | Single source of truth for each piece of data |

### Costs of Normalization

| Cost | Explanation |
|------|-------------|
| **More JOINs** | Queries need to combine multiple tables |
| **Query Complexity** | Simple questions require complex SQL |
| **Performance Overhead** | JOINs have computational cost |
| **Harder to Understand** | Data spread across many tables |

### The Sweet Spot

Most applications aim for **Third Normal Form (3NF)** as the default, then selectively denormalize for performance-critical queries.

```
Under-normalized ←────────── 3NF ──────────→ Over-normalized
(redundancy,               (balance)        (too many joins,
 anomalies)                                  complexity)
```

---

## When to Denormalize

### Read-Heavy Workloads

If your application reads data 100x more than it writes, the cost of JOINs may outweigh the benefits of normalization.

**Example:** A product catalog page that shows product name, category name, and manufacturer name.

**Normalized (3 JOINs):**
```sql
SELECT p.name, c.name AS category, m.name AS manufacturer
FROM products p
JOIN categories c ON p.category_id = c.category_id
JOIN manufacturers m ON p.manufacturer_id = m.manufacturer_id
WHERE p.product_id = 101;
```

**Denormalized (no JOINs):**
```sql
SELECT name, category_name, manufacturer_name
FROM products
WHERE product_id = 101;
```

### Reporting and Analytics

OLAP (Online Analytical Processing) workloads often use denormalized **star schemas** or **snowflake schemas** for fast aggregations.

### Caching Computed Values

Instead of computing aggregates every time, store them:

```sql
-- Normalized: must calculate order total every time
SELECT SUM(quantity * unit_price) AS total
FROM order_items
WHERE order_id = 1;

-- Denormalized: store the pre-computed total
SELECT total FROM orders WHERE order_id = 1;
```

### Historical Data Snapshots

When you need to know what data looked like at a point in time:

```sql
-- Store the price at order time, not just a reference
CREATE TABLE order_items (
    order_id INT,
    product_id INT,
    quantity INT,
    unit_price DECIMAL(10, 2),  -- Snapshot, not current price
    product_name VARCHAR(200),  -- Snapshot, products might be renamed
    PRIMARY KEY (order_id, product_id)
);
```

---

## Denormalization Techniques

### 1. Redundant Columns

Store frequently-accessed data from related tables directly:

```sql
-- Normalized
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INT REFERENCES customers(customer_id),
    order_date DATE
);

-- Denormalized: add customer_name for display
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INT REFERENCES customers(customer_id),
    customer_name VARCHAR(100),  -- Redundant copy
    order_date DATE
);
```

**Maintenance:** Use triggers or application logic to keep in sync:

```sql
CREATE OR REPLACE FUNCTION sync_customer_name()
RETURNS TRIGGER AS $$
BEGIN
    SELECT name INTO NEW.customer_name 
    FROM customers 
    WHERE customer_id = NEW.customer_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER orders_customer_sync
BEFORE INSERT OR UPDATE ON orders
FOR EACH ROW EXECUTE FUNCTION sync_customer_name();
```

### 2. Pre-Computed Aggregates

Store calculated values that are expensive to compute:

```sql
-- Add aggregate columns to parent tables
ALTER TABLE customers ADD COLUMN order_count INT DEFAULT 0;
ALTER TABLE customers ADD COLUMN total_spent DECIMAL(12, 2) DEFAULT 0;

-- Update via trigger or scheduled job
CREATE OR REPLACE FUNCTION update_customer_stats()
RETURNS TRIGGER AS $$
BEGIN
    UPDATE customers
    SET order_count = (SELECT COUNT(*) FROM orders WHERE customer_id = NEW.customer_id),
        total_spent = (SELECT COALESCE(SUM(total), 0) FROM orders WHERE customer_id = NEW.customer_id)
    WHERE customer_id = NEW.customer_id;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

### 3. Summary Tables

Create separate tables for aggregated data:

```sql
-- Daily sales summary
CREATE TABLE daily_sales_summary (
    summary_date DATE PRIMARY KEY,
    total_orders INT,
    total_revenue DECIMAL(12, 2),
    average_order_value DECIMAL(10, 2),
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Populate via scheduled job or trigger
INSERT INTO daily_sales_summary (summary_date, total_orders, total_revenue, average_order_value)
SELECT 
    order_date,
    COUNT(*),
    SUM(total),
    AVG(total)
FROM orders
WHERE order_date = CURRENT_DATE - INTERVAL '1 day'
GROUP BY order_date
ON CONFLICT (summary_date) DO UPDATE SET
    total_orders = EXCLUDED.total_orders,
    total_revenue = EXCLUDED.total_revenue,
    average_order_value = EXCLUDED.average_order_value,
    updated_at = CURRENT_TIMESTAMP;
```

### 4. Materialized Views

PostgreSQL's materialized views store query results physically:

```sql
-- Create materialized view
CREATE MATERIALIZED VIEW product_sales_summary AS
SELECT 
    p.product_id,
    p.name,
    COUNT(DISTINCT o.order_id) AS order_count,
    SUM(oi.quantity) AS units_sold,
    SUM(oi.quantity * oi.unit_price) AS total_revenue
FROM products p
LEFT JOIN order_items oi ON p.product_id = oi.product_id
LEFT JOIN orders o ON oi.order_id = o.order_id
GROUP BY p.product_id, p.name;

-- Create index on materialized view
CREATE INDEX idx_product_sales_revenue ON product_sales_summary(total_revenue DESC);

-- Refresh the view (full refresh)
REFRESH MATERIALIZED VIEW product_sales_summary;

-- Concurrent refresh (doesn't lock reads)
REFRESH MATERIALIZED VIEW CONCURRENTLY product_sales_summary;
```

**Materialized View vs Regular View:**

| Feature | Regular View | Materialized View |
|---------|--------------|-------------------|
| Storage | No storage (query runs each time) | Stores query results |
| Performance | Same as underlying query | Fast (pre-computed) |
| Freshness | Always current | Stale until refreshed |
| Indexes | Cannot index | Can create indexes |
| Use Case | Simple abstraction | Performance optimization |

---

## Real-World Schema Analysis

### Analyzing Existing Schemas

When you encounter an existing database, analyze it systematically:

#### Step 1: List Tables and Relationships

```sql
-- List all tables
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';

-- Find foreign key relationships
SELECT
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table,
    ccu.column_name AS foreign_column
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY';
```

#### Step 2: Check for Normalization Violations

Look for red flags:

```sql
-- Multi-valued columns (arrays or comma-separated)
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'your_table' AND data_type LIKE '%ARRAY%';

-- Repeated column patterns (column1, column2, column3)
SELECT column_name 
FROM information_schema.columns 
WHERE table_name = 'your_table' 
AND column_name ~ '(1|2|3|_1|_2|_3)$';
```

#### Step 3: Check for Missing Indexes

```sql
-- Foreign keys without indexes
SELECT 
    tc.table_name,
    kcu.column_name
FROM information_schema.table_constraints tc
JOIN information_schema.key_column_usage kcu 
    ON tc.constraint_name = kcu.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
AND NOT EXISTS (
    SELECT 1 FROM pg_indexes 
    WHERE tablename = tc.table_name 
    AND indexdef LIKE '%' || kcu.column_name || '%'
);
```

### Sample Schema Review: E-Commerce

Let's analyze a typical e-commerce schema:

```sql
-- Schema with some issues
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_name VARCHAR(100),
    customer_email VARCHAR(255),
    customer_address TEXT,
    product_list TEXT,  -- "Widget:2:$10, Gadget:1:$25"
    subtotal DECIMAL(10, 2),
    tax DECIMAL(10, 2),
    shipping DECIMAL(10, 2),
    total DECIMAL(10, 2)
);
```

**Issues Identified:**

| Problem | Normal Form Violated | Fix |
|---------|---------------------|-----|
| `product_list` is multi-valued | 1NF | Create `order_items` junction table |
| Customer data repeated | 3NF | Create `customers` table |
| Customer address might include city/zip | 1NF | Separate address components |
| `total` can be computed | — | Consider if denormalization is intentional |

**Normalized Version:**

```sql
CREATE TABLE customers (
    customer_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE
);

CREATE TABLE addresses (
    address_id SERIAL PRIMARY KEY,
    customer_id INT REFERENCES customers(customer_id),
    street VARCHAR(255),
    city VARCHAR(100),
    state VARCHAR(50),
    postal_code VARCHAR(20),
    country VARCHAR(50) DEFAULT 'USA'
);

CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INT NOT NULL REFERENCES customers(customer_id),
    shipping_address_id INT REFERENCES addresses(address_id),
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    subtotal DECIMAL(10, 2),
    tax DECIMAL(10, 2),
    shipping DECIMAL(10, 2),
    total DECIMAL(10, 2)  -- Intentionally denormalized for fast access
);

CREATE TABLE order_items (
    order_id INT REFERENCES orders(order_id) ON DELETE CASCADE,
    product_id INT REFERENCES products(product_id),
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10, 2) NOT NULL,
    PRIMARY KEY (order_id, product_id)
);
```

---

## Industry Sample Schemas

### MySQL Sample Databases

MySQL provides several sample databases for learning:

1. **Sakila** — DVD rental store
   - Customers, Films, Rentals, Inventory
   - Great example of a normalized schema

2. **World** — Geographic data
   - Countries, Cities, Languages
   - Simple 3NF design

3. **Employees** — HR data
   - Employees, Departments, Salaries, Titles
   - Historical data with date ranges

### Microsoft Sample Databases

1. **AdventureWorks** — Manufacturing and sales
   - Comprehensive ERP-style schema
   - Product lifecycle, sales, HR

2. **Northwind** — Classic trading company
   - Customers, Orders, Products, Suppliers
   - Simple e-commerce model

3. **Contoso** — Retail analytics
   - OLAP-style star schema
   - Designed for reporting

### PostgreSQL Samples

1. **dvdrental** — Based on Sakila
2. **pagila** — PostgreSQL port of Sakila

**Loading Sample Data:**
```bash
# Download dvdrental
curl -O https://www.postgresqltutorial.com/wp-content/uploads/2019/05/dvdrental.zip

# Restore to PostgreSQL
pg_restore -U postgres -d dvdrental dvdrental.tar
```

---

## Schema Evolution

### Adding Columns

```sql
-- Add column with default (safe)
ALTER TABLE customers ADD COLUMN loyalty_points INT DEFAULT 0;

-- Add NOT NULL column (must provide default)
ALTER TABLE customers ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'active';
```

### Splitting Tables (Normalizing)

When you discover a normalization violation in production:

```sql
-- Step 1: Create new normalized table
CREATE TABLE categories (
    category_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
);

-- Step 2: Populate from existing data
INSERT INTO categories (name)
SELECT DISTINCT category_name FROM products;

-- Step 3: Add foreign key column
ALTER TABLE products ADD COLUMN category_id INT;

-- Step 4: Backfill foreign key
UPDATE products p
SET category_id = c.category_id
FROM categories c
WHERE p.category_name = c.name;

-- Step 5: Add constraint and drop old column
ALTER TABLE products 
    ALTER COLUMN category_id SET NOT NULL,
    ADD CONSTRAINT fk_products_category 
        FOREIGN KEY (category_id) REFERENCES categories(category_id);

ALTER TABLE products DROP COLUMN category_name;
```

### Merging Tables (Denormalizing)

When you need to denormalize for performance:

```sql
-- Add redundant column
ALTER TABLE orders ADD COLUMN customer_name VARCHAR(100);

-- Backfill data
UPDATE orders o
SET customer_name = c.name
FROM customers c
WHERE o.customer_id = c.customer_id;

-- Create trigger to maintain consistency
CREATE OR REPLACE FUNCTION sync_order_customer_name()
RETURNS TRIGGER AS $$
BEGIN
    -- On order insert/update
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        SELECT name INTO NEW.customer_name 
        FROM customers 
        WHERE customer_id = NEW.customer_id;
        RETURN NEW;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_orders_customer_name
BEFORE INSERT OR UPDATE OF customer_id ON orders
FOR EACH ROW EXECUTE FUNCTION sync_order_customer_name();

-- Also handle customer name changes
CREATE OR REPLACE FUNCTION sync_orders_on_customer_update()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.name <> NEW.name THEN
        UPDATE orders SET customer_name = NEW.name 
        WHERE customer_id = NEW.customer_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_customer_name_sync
AFTER UPDATE OF name ON customers
FOR EACH ROW EXECUTE FUNCTION sync_orders_on_customer_update();
```

---

## Performance Considerations

### Measuring Query Performance

```sql
-- Enable timing
\timing on

-- Explain analyze to see actual performance
EXPLAIN ANALYZE 
SELECT c.name, COUNT(o.order_id) 
FROM customers c
LEFT JOIN orders o ON c.customer_id = o.customer_id
GROUP BY c.customer_id;
```

### When Normalization Hurts

Signs that you may need to denormalize:

1. **Query time is too slow** despite proper indexing
2. **High JOIN count** (5+ tables) in common queries
3. **Read/write ratio is very high** (100:1 or more)
4. **Aggregate calculations** are repeated frequently

### When Denormalization Hurts

Signs that you've over-denormalized:

1. **Data inconsistencies** appearing between redundant copies
2. **Complex update logic** to maintain synchronization
3. **Write performance degraded** due to trigger overhead
4. **Storage bloat** from redundant data

---

## Best Practices Summary

### Design Phase

1. **Start with 3NF** — normalize first, denormalize with purpose
2. **Document intentional denormalization** — note why and how it's maintained
3. **Use ERD tools** — visualize before implementing (dbdiagram.io, MySQL Workbench)
4. **Review sample schemas** — learn from proven designs

### Implementation Phase

1. **Add indexes on foreign keys** — PostgreSQL doesn't do this automatically
2. **Use constraints** — enforce data integrity at the database level
3. **Consider materialized views** — for complex reporting queries
4. **Plan for schema evolution** — make additive changes when possible

### Maintenance Phase

1. **Monitor query performance** — use EXPLAIN ANALYZE regularly
2. **Review slow queries** — identify denormalization opportunities
3. **Maintain materialized views** — set up refresh schedules
4. **Validate data consistency** — check redundant data periodically

---

## Practice

### Exercise 1: Analyze and Normalize

Given this denormalized table, identify issues and design a normalized schema:

```sql
CREATE TABLE employee_projects (
    emp_id INT,
    emp_name VARCHAR(100),
    emp_department VARCHAR(50),
    emp_salary DECIMAL(10,2),
    project_ids TEXT,  -- "P1,P2,P3"
    project_names TEXT,  -- "Website,API,Mobile"
    manager_name VARCHAR(100),
    manager_email VARCHAR(255)
);
```

### Exercise 2: Denormalization Decision

You have a normalized e-commerce schema. The product detail page needs to show:
- Product name and description
- Category name
- Manufacturer name
- Average review rating
- Number of reviews
- Current stock level

The page loads 10,000 times per minute. Propose a denormalization strategy.

### Exercise 3: Materialized View

Create a materialized view for a "Top 10 Products This Month" report that shows:
- Product name
- Total units sold
- Total revenue
- Number of unique customers

Include the refresh strategy.

---

## Key Takeaways

1. **3NF is the default target** — provides good balance of integrity and performance
2. **Denormalize strategically** — for read-heavy, well-defined query patterns
3. **Document denormalization** — track what, why, and how it's maintained
4. **Use materialized views** — for expensive read queries without write complexity
5. **Maintain redundant data carefully** — triggers or application logic must keep data in sync
6. **Measure before optimizing** — use EXPLAIN ANALYZE to identify real bottlenecks
7. **Study existing schemas** — Sakila, AdventureWorks, and others are valuable learning resources
8. **Plan for evolution** — schemas change; design for additive migrations
