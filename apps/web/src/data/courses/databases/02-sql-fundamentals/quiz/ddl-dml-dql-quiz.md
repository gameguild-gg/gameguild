# Quiz: DDL, DML, and DQL Translation

## Instructions

This quiz tests your ability to translate between **natural language requirements** and **SQL statements**. Some questions give you a requirement and ask you to select the correct SQL. Others give you SQL and ask what operation it performs.

---

## Question 1 - Requirement → SQL

**Requirement:** The e-commerce team needs to create a new table called `products` with the following columns:
- `product_id`: auto-incrementing integer, primary key
- `name`: text, cannot be null, maximum 200 characters
- `price`: decimal with 10 digits total and 2 decimal places
- `created_at`: timestamp that defaults to the current time

**Which SQL statement correctly implements this requirement?**

- [ ] A.
```sql
CREATE TABLE products (
    product_id INT PRIMARY KEY AUTO_INCREMENT,
    name TEXT(200) NOT NULL,
    price DECIMAL(2, 10),
    created_at TIMESTAMP DEFAULT NOW()
);
```

- [ ] B.
```sql
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

- [ ] C.
```sql
CREATE products TABLE (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(200),
    price DECIMAL(10, 2),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

- [ ] D.
```sql
INSERT INTO products (product_id, name, price, created_at)
VALUES (SERIAL, VARCHAR(200), DECIMAL(10,2), CURRENT_TIMESTAMP);
```

---

## Question 2 - SQL → Description

**Given this SQL statement:**

```sql
ALTER TABLE employees
ADD COLUMN department_id INT REFERENCES departments(id),
DROP COLUMN temp_notes,
ALTER COLUMN salary SET NOT NULL;
```

**What operations does this statement perform?**

- [ ] A. Creates a new table `employees` with three columns: `department_id`, `temp_notes`, and `salary`

- [ ] B. Adds a foreign key column linking to departments, removes an existing column, and makes salary required

- [ ] C. Updates all employee records to set their department, clear their notes, and validate their salary

- [ ] D. Deletes employees without a department and those with null salaries

---

## Question 3 - Requirement → SQL

**Requirement:** The inventory manager needs to add 50 units of product SKU "LAPTOP-PRO-15" to warehouse "WH-EAST". The `inventory` table has columns: `id`, `sku`, `warehouse_code`, `quantity`, `last_updated`.

**Which SQL statement correctly implements this requirement?**

- [ ] A.
```sql
SELECT quantity + 50 FROM inventory
WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';
```

- [ ] B.
```sql
UPDATE inventory
SET quantity = 50, last_updated = NOW()
WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';
```

- [ ] C.
```sql
UPDATE inventory
SET quantity = quantity + 50, last_updated = NOW()
WHERE sku = 'LAPTOP-PRO-15' AND warehouse_code = 'WH-EAST';
```

- [ ] D.
```sql
INSERT INTO inventory (sku, warehouse_code, quantity)
VALUES ('LAPTOP-PRO-15', 'WH-EAST', 50);
```

---

## Question 4 - SQL → Description

**Given this SQL statement:**

```sql
SELECT product_name, price, category
FROM products
WHERE price > 100
  AND category = 'electronics'
ORDER BY price DESC
LIMIT 10;
```

**What does this query retrieve?**

- [ ] A. All electronics products sorted by price

- [ ] B. The 10 most expensive electronics products priced above $100

- [ ] C. Updates the top 10 electronics products to have a price over 100

- [ ] D. Deletes electronics products that cost more than $100

---

## Question 5 - Requirement → SQL

**Requirement:** The HR system needs to permanently remove all employee records from the `employees` table where the `termination_date` is before January 1, 2024, and the `status` is 'inactive'.

**Which SQL statement correctly implements this requirement?**

- [ ] A.
```sql
DELETE employees
WHERE termination_date < '2024-01-01' OR status = 'inactive';
```

- [ ] B.
```sql
DROP FROM employees
WHERE termination_date < '2024-01-01' AND status = 'inactive';
```

- [ ] C.
```sql
DELETE FROM employees
WHERE termination_date < '2024-01-01' AND status = 'inactive';
```

- [ ] D.
```sql
UPDATE employees SET deleted = true
WHERE termination_date < '2024-01-01' AND status = 'inactive';
```

---

## Question 6 - SQL → Description

**Given this SQL statement:**

```sql
CREATE SCHEMA IF NOT EXISTS analytics;

CREATE TABLE analytics.daily_metrics (
    metric_date DATE PRIMARY KEY,
    active_users INT NOT NULL DEFAULT 0,
    revenue DECIMAL(12, 2) NOT NULL DEFAULT 0.00,
    CONSTRAINT positive_users CHECK (active_users >= 0),
    CONSTRAINT positive_revenue CHECK (revenue >= 0)
);
```

**What does this statement accomplish?**

- [ ] A. Creates a database called `analytics` with a table for storing daily user and revenue data with validation rules

- [ ] B. Creates a schema namespace and a table within it for daily metrics, ensuring users and revenue cannot be negative

- [ ] C. Inserts default values into the `daily_metrics` table for each date

- [ ] D. Alters an existing `daily_metrics` table to add constraints for positive values

---

## Question 7 - Requirement → SQL

**Requirement:** The admin needs to retrieve all users who registered after January 1, 2025, showing only their ID, email, and registration date, sorted by registration date with the newest first, limited to 50 results.

**Which SQL statement correctly implements this requirement?**

- [ ] A.
```sql
SELECT id, email, registered_at
FROM users
WHERE registered_at > '2025-01-01'
ORDER BY registered_at DESC
LIMIT 50;
```

- [ ] B.
```sql
SELECT *
FROM users
WHERE registered_at > '2025-01-01'
ORDER BY registered_at
LIMIT 50;
```

- [ ] C.
```sql
UPDATE users
SET registered_at = '2025-01-01'
WHERE id < 50;
```

- [ ] D.
```sql
SELECT id, email, registered_at
FROM users
WHERE registered_at > '2025-01-01'
ORDER BY registered_at ASC;
```

---

## Question 8 - SQL → Description

**Given this SQL statement:**

```sql
INSERT INTO audit_trail (action, table_name, record_id, old_value, new_value, changed_by, changed_at)
SELECT 
    'UPDATE',
    'products',
    p.id,
    p.price::TEXT,
    (p.price * 1.10)::TEXT,
    'system_batch',
    NOW()
FROM products p
WHERE p.category = 'electronics';
```

**What does this statement do?**

- [ ] A. Updates all electronics products to increase their price by 10%

- [ ] B. Logs planned price changes for electronics products to an audit table without modifying the products

- [ ] C. Deletes electronics products and moves them to an audit archive

- [ ] D. Creates a backup of the products table filtered by electronics category

---

## Question 9 - Requirement → SQL

**Requirement:** The database administrator needs to:
1. Add a new column `phone` (VARCHAR 20) to the `customers` table
2. Make the existing `email` column required (NOT NULL)
3. Remove the `fax` column which is no longer used

**Which SQL statements correctly implement ALL requirements?**

- [ ] A.
```sql
ALTER TABLE customers ADD COLUMN phone VARCHAR(20);
ALTER TABLE customers ALTER COLUMN email SET NOT NULL;
ALTER TABLE customers DROP COLUMN fax;
```

- [ ] B.
```sql
INSERT INTO customers (phone) VALUES (VARCHAR(20));
UPDATE customers SET email = NOT NULL;
DELETE FROM customers WHERE fax IS NOT NULL;
```

- [ ] C.
```sql
CREATE COLUMN phone VARCHAR(20) ON customers;
SET email NOT NULL IN customers;
DROP fax FROM customers;
```

- [ ] D.
```sql
ALTER TABLE customers ADD phone;
ALTER TABLE customers MODIFY email NOT NULL;
ALTER TABLE customers REMOVE COLUMN fax;
```

---

## Question 10 - SQL → Description

**Given this SQL statement:**

```sql
INSERT INTO order_history (order_id, status, changed_at, changed_by)
SELECT id, status, updated_at, 'migration_script'
FROM orders
WHERE status = 'completed';
```

**What does this statement do?**

- [ ] A. Updates all completed orders to add history records

- [ ] B. Copies data from completed orders into a history table without modifying the original orders

- [ ] C. Deletes completed orders after backing them up

- [ ] D. Creates a new orders table from the history table

---

## Answer Key (Instructor Only)

| Question | Answer | Category | Explanation |
|:--------:|:------:|:--------:|-------------|
| 1 | **B** | DDL | Uses correct PostgreSQL syntax: `SERIAL` for auto-increment, `VARCHAR(200)` with `NOT NULL`, proper `DECIMAL(10,2)` order (precision, scale), and `CURRENT_TIMESTAMP` |
| 2 | **B** | DDL | `ALTER TABLE` with `ADD COLUMN` (with FK reference), `DROP COLUMN`, and `ALTER COLUMN SET NOT NULL` modifies table structure |
| 3 | **C** | DML | `UPDATE` with `quantity = quantity + 50` adds to existing value. Option B would SET to 50, not add. Option D would create a duplicate row |
| 4 | **B** | DQL | Basic SELECT with WHERE filtering by price and category, ORDER BY DESC, and LIMIT 10 |
| 5 | **C** | DML | `DELETE FROM` with `WHERE` using `AND` for both conditions. Option A uses `OR` (wrong logic), B uses invalid `DROP FROM`, D is soft-delete (not permanent) |
| 6 | **B** | DDL | `CREATE SCHEMA IF NOT EXISTS` creates a namespace, then creates a table within it. `CHECK` constraints validate data but don't create databases |
| 7 | **A** | DQL | Uses specific column selection, correct WHERE with date comparison, ORDER BY DESC for newest first, and LIMIT 50 |
| 8 | **B** | DML | `INSERT INTO ... SELECT` copies calculated data to audit table. The products table is read but not modified. This prepares an audit log before a potential update |
| 9 | **A** | DDL | Uses correct PostgreSQL syntax: `ADD COLUMN`, `ALTER COLUMN SET NOT NULL`, and `DROP COLUMN` |
| 10 | **B** | DML | `INSERT INTO ... SELECT` copies data from one table to another. The original orders table is only read, not modified |
