# Schema Patterns & Data Integrity

This lesson covers common schema design patterns for maintaining data integrity, handling deletions gracefully, and tracking changes over time.

---

## Soft Delete Pattern

### The Problem with Hard Deletes

When you delete a row with `DELETE FROM`, it's gone forever (unless you have backups). This causes problems:

1. **Audit trail lost** — No record that the data ever existed
2. **Foreign key issues** — Child records may become orphaned
3. **Accidental deletion** — No easy recovery
4. **Reporting gaps** — Historical reports become inaccurate

### Soft Delete Solution

Instead of deleting rows, mark them as deleted:

```sql
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP DEFAULT NULL  -- NULL = active, timestamp = deleted
);

-- "Delete" a user (soft delete)
UPDATE users SET deleted_at = CURRENT_TIMESTAMP WHERE user_id = 42;

-- Query only active users
SELECT * FROM users WHERE deleted_at IS NULL;

-- Query all users including deleted
SELECT * FROM users;

-- Restore a deleted user
UPDATE users SET deleted_at = NULL WHERE user_id = 42;
```

### Soft Delete Variations

#### Boolean Flag

```sql
ALTER TABLE users ADD COLUMN is_deleted BOOLEAN DEFAULT FALSE;

-- Delete
UPDATE users SET is_deleted = TRUE WHERE user_id = 42;

-- Query active
SELECT * FROM users WHERE is_deleted = FALSE;
```

#### Status Column

```sql
ALTER TABLE users ADD COLUMN status VARCHAR(20) DEFAULT 'active';

-- Possible statuses: 'active', 'suspended', 'deleted', 'archived'
UPDATE users SET status = 'deleted' WHERE user_id = 42;

-- Query active
SELECT * FROM users WHERE status = 'active';
```

### Creating a View for Active Records

```sql
-- Create a view that only shows active users
CREATE VIEW active_users AS
SELECT user_id, email, name, created_at
FROM users
WHERE deleted_at IS NULL;

-- Query the view instead of the table
SELECT * FROM active_users;
```

### Partial Index for Soft Delete

```sql
-- Index only active records for faster queries
CREATE INDEX idx_users_email_active ON users(email) WHERE deleted_at IS NULL;
```

### Handling Unique Constraints with Soft Delete

Problem: If you soft-delete a user with email "alice@example.com", you can't create a new user with that email because it still exists in the table.

**Solution 1: Partial Unique Index**
```sql
-- Only enforce uniqueness on active records
CREATE UNIQUE INDEX idx_users_email_unique_active 
ON users(email) WHERE deleted_at IS NULL;
```

**Solution 2: Include deleted_at in Unique Constraint**
```sql
-- Allow same email if deleted_at is different
ALTER TABLE users DROP CONSTRAINT users_email_key;
ALTER TABLE users ADD CONSTRAINT users_email_unique UNIQUE (email, deleted_at);
```

**Solution 3: Modify Email on Delete**
```sql
-- Append timestamp to email when deleting
UPDATE users 
SET email = email || '_deleted_' || EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::TEXT,
    deleted_at = CURRENT_TIMESTAMP
WHERE user_id = 42;
```

---

## Versioning Patterns

### Row Versioning (Optimistic Locking)

Prevent concurrent updates from overwriting each other:

```sql
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    version INT NOT NULL DEFAULT 1,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Update with version check
UPDATE products
SET name = 'New Name', 
    price = 29.99,
    version = version + 1,
    updated_at = CURRENT_TIMESTAMP
WHERE product_id = 101 AND version = 3;  -- Expected version

-- Check if update succeeded
-- If 0 rows affected, someone else updated first (conflict!)
```

**Application Logic:**
```typescript
// Pseudocode
const product = await db.query('SELECT * FROM products WHERE product_id = $1', [id]);

// User makes changes...

const result = await db.query(`
  UPDATE products SET name = $1, price = $2, version = version + 1
  WHERE product_id = $3 AND version = $4
`, [newName, newPrice, id, product.version]);

if (result.rowCount === 0) {
  throw new Error('Conflict: Product was modified by another user');
}
```

### History Table Pattern

Keep a complete history of all changes:

```sql
-- Current state table
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- History table
CREATE TABLE products_history (
    history_id SERIAL PRIMARY KEY,
    product_id INT NOT NULL,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    changed_by VARCHAR(100),
    operation VARCHAR(10) NOT NULL  -- 'INSERT', 'UPDATE', 'DELETE'
);

-- Trigger to capture history
CREATE OR REPLACE FUNCTION log_product_changes()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO products_history (product_id, name, price, changed_by, operation)
        VALUES (OLD.product_id, OLD.name, OLD.price, current_user, 'DELETE');
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO products_history (product_id, name, price, changed_by, operation)
        VALUES (OLD.product_id, OLD.name, OLD.price, current_user, 'UPDATE');
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO products_history (product_id, name, price, changed_by, operation)
        VALUES (NEW.product_id, NEW.name, NEW.price, current_user, 'INSERT');
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_products_history
AFTER INSERT OR UPDATE OR DELETE ON products
FOR EACH ROW EXECUTE FUNCTION log_product_changes();
```

### Temporal Tables (Valid Time)

Track when data was valid in the real world:

```sql
CREATE TABLE employee_salaries (
    employee_id INT NOT NULL,
    salary DECIMAL(10, 2) NOT NULL,
    valid_from DATE NOT NULL,
    valid_to DATE,  -- NULL means currently valid
    PRIMARY KEY (employee_id, valid_from)
);

-- Insert salary history
INSERT INTO employee_salaries VALUES
(1, 50000, '2024-01-01', '2024-12-31'),
(1, 55000, '2025-01-01', '2025-12-31'),
(1, 60000, '2026-01-01', NULL);

-- Get current salary
SELECT salary FROM employee_salaries
WHERE employee_id = 1 AND valid_to IS NULL;

-- Get salary at a specific date
SELECT salary FROM employee_salaries
WHERE employee_id = 1 
  AND valid_from <= '2025-06-15' 
  AND (valid_to IS NULL OR valid_to >= '2025-06-15');
```

---

## Checksum Pattern

### Data Integrity Verification

Use checksums to detect data corruption or tampering:

```sql
CREATE TABLE financial_transactions (
    transaction_id SERIAL PRIMARY KEY,
    account_id INT NOT NULL,
    amount DECIMAL(12, 2) NOT NULL,
    transaction_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    description TEXT,
    checksum VARCHAR(64) NOT NULL
);

-- Calculate checksum on insert
-- In PostgreSQL, use pgcrypto extension
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Insert with checksum
INSERT INTO financial_transactions (account_id, amount, description, checksum)
VALUES (
    1001, 
    500.00, 
    'Payment received',
    encode(digest(
        1001::TEXT || 500.00::TEXT || 'Payment received', 
        'sha256'
    ), 'hex')
);

-- Verify integrity
SELECT *,
    CASE WHEN checksum = encode(digest(
        account_id::TEXT || amount::TEXT || description, 
        'sha256'
    ), 'hex') 
    THEN 'VALID' ELSE 'CORRUPTED' END AS integrity_status
FROM financial_transactions;
```

### Row Signature Pattern

Sign entire rows for audit compliance:

```sql
CREATE TABLE audit_records (
    record_id SERIAL PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    record_data JSONB NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100) NOT NULL,
    signature VARCHAR(128) NOT NULL
);
```

---

## Audit Trail Patterns

### Audit Columns

Add standard audit columns to every table:

```sql
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INT NOT NULL,
    total DECIMAL(10, 2) NOT NULL,
    -- Audit columns
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by VARCHAR(100) NOT NULL DEFAULT current_user,
    updated_at TIMESTAMP,
    updated_by VARCHAR(100)
);

-- Auto-update audit columns
CREATE OR REPLACE FUNCTION update_audit_columns()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    NEW.updated_by = current_user;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_orders_audit
BEFORE UPDATE ON orders
FOR EACH ROW EXECUTE FUNCTION update_audit_columns();
```

### Centralized Audit Log

One table to track all changes across the database:

```sql
CREATE TABLE audit_log (
    log_id BIGSERIAL PRIMARY KEY,
    table_name VARCHAR(100) NOT NULL,
    record_id TEXT NOT NULL,
    operation VARCHAR(10) NOT NULL,  -- INSERT, UPDATE, DELETE
    old_values JSONB,
    new_values JSONB,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    changed_by VARCHAR(100) DEFAULT current_user,
    client_ip INET,
    session_id TEXT
);

-- Generic audit trigger function
CREATE OR REPLACE FUNCTION audit_trigger_func()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit_log (table_name, record_id, operation, old_values)
        VALUES (TG_TABLE_NAME, OLD.id::TEXT, 'DELETE', to_jsonb(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_log (table_name, record_id, operation, old_values, new_values)
        VALUES (TG_TABLE_NAME, NEW.id::TEXT, 'UPDATE', to_jsonb(OLD), to_jsonb(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log (table_name, record_id, operation, new_values)
        VALUES (TG_TABLE_NAME, NEW.id::TEXT, 'INSERT', to_jsonb(NEW));
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- Apply to a table
CREATE TRIGGER trg_customers_audit
AFTER INSERT OR UPDATE OR DELETE ON customers
FOR EACH ROW EXECUTE FUNCTION audit_trigger_func();
```

---

## Common Schema Anti-Patterns

### 1. Entity-Attribute-Value (EAV)

**Anti-pattern:**
```sql
CREATE TABLE entity_attributes (
    entity_id INT,
    attribute_name VARCHAR(100),
    attribute_value TEXT
);

-- Storing: user_id=1, name="Alice", email="alice@mail.com"
INSERT INTO entity_attributes VALUES
(1, 'name', 'Alice'),
(1, 'email', 'alice@mail.com'),
(1, 'age', '30');
```

**Problems:**
- No type safety (everything is TEXT)
- Can't enforce NOT NULL or constraints
- Queries are complex and slow
- No foreign key relationships

**Better:** Use proper normalized tables, or JSONB for truly dynamic attributes.

### 2. Comma-Separated Values

**Anti-pattern:**
```sql
CREATE TABLE users (
    user_id INT PRIMARY KEY,
    name VARCHAR(100),
    roles TEXT  -- 'admin,editor,viewer'
);
```

**Problems:**
- Violates 1NF
- Can't enforce referential integrity
- Queries are complex (`LIKE '%admin%'`)
- Can't index effectively

**Better:** Use a junction table:
```sql
CREATE TABLE user_roles (
    user_id INT REFERENCES users(user_id),
    role_id INT REFERENCES roles(role_id),
    PRIMARY KEY (user_id, role_id)
);
```

### 3. Polymorphic Associations

**Anti-pattern:**
```sql
CREATE TABLE comments (
    comment_id SERIAL PRIMARY KEY,
    body TEXT,
    commentable_type VARCHAR(50),  -- 'post', 'photo', 'video'
    commentable_id INT
);
```

**Problems:**
- Can't enforce foreign keys
- No referential integrity
- Complex queries

**Better:** Use separate tables or inheritance:
```sql
-- Separate junction tables
CREATE TABLE post_comments (
    comment_id INT PRIMARY KEY REFERENCES comments(comment_id),
    post_id INT REFERENCES posts(post_id)
);

CREATE TABLE photo_comments (
    comment_id INT PRIMARY KEY REFERENCES comments(comment_id),
    photo_id INT REFERENCES photos(photo_id)
);
```

### 4. One True Lookup Table (OTLT)

**Anti-pattern:**
```sql
CREATE TABLE lookups (
    lookup_id SERIAL PRIMARY KEY,
    category VARCHAR(50),  -- 'country', 'status', 'color'
    code VARCHAR(50),
    description VARCHAR(200)
);
```

**Problems:**
- Can't have foreign keys to specific categories
- No type-specific constraints
- Queries require filtering by category

**Better:** Separate lookup tables:
```sql
CREATE TABLE countries (
    country_code CHAR(2) PRIMARY KEY,
    name VARCHAR(100)
);

CREATE TABLE order_statuses (
    status_code VARCHAR(20) PRIMARY KEY,
    description VARCHAR(100)
);
```

---

## Best Practices Summary

| Pattern | Use When | Implementation |
|---------|----------|----------------|
| **Soft Delete** | Need to recover data, maintain history | `deleted_at` timestamp column |
| **Row Versioning** | Concurrent updates possible | `version` integer column |
| **History Table** | Full audit trail required | Separate `_history` table with trigger |
| **Temporal Tables** | Need to query "as of" a date | `valid_from`/`valid_to` date range |
| **Checksum** | Data integrity verification required | SHA256 hash of key columns |
| **Audit Columns** | Basic change tracking | `created_at/by`, `updated_at/by` |
| **Audit Log** | Centralized change tracking | Single log table with JSONB |

---

## Practice

### Exercise 1: Implement Soft Delete

Add soft delete capability to an existing `products` table:
1. Add the appropriate column(s)
2. Create a view for active products
3. Create a partial index for the unique constraint
4. Write the "delete" and "restore" queries

### Exercise 2: History Table

Design a history tracking system for an `employee` table that tracks:
- All changes to name, email, department
- Who made the change
- When the change was made
- What the old values were

### Exercise 3: Identify Anti-Patterns

Review this schema and identify the anti-patterns:

```sql
CREATE TABLE items (
    id INT PRIMARY KEY,
    type VARCHAR(50),
    attributes TEXT,  -- JSON string
    tags VARCHAR(500),  -- comma-separated
    parent_type VARCHAR(50),
    parent_id INT
);
```

---

## Key Takeaways

1. **Soft deletes preserve data** — Use `deleted_at` timestamp instead of `DELETE`
2. **Versioning prevents conflicts** — Increment version on update, check before saving
3. **History tables provide audit trails** — Use triggers to capture changes automatically
4. **Temporal tables track time** — Use date ranges for "as of" queries
5. **Checksums verify integrity** — Hash important data to detect tampering
6. **Avoid common anti-patterns** — EAV, comma-separated values, polymorphic associations
7. **Standard audit columns** — Add `created_at`, `created_by`, `updated_at`, `updated_by` to all tables
