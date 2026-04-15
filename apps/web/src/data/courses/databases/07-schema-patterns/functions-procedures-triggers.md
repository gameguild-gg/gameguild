# Functions, Procedures, and Triggers

PostgreSQL allows you to extend the database with custom logic using functions, procedures, and triggers. These run inside the database server, reducing round-trips and ensuring consistent execution.

---

## SQL Functions

### Creating a Simple Function

```sql
-- Function that calculates order total
CREATE FUNCTION calculate_order_total(p_order_id INT)
RETURNS DECIMAL(12, 2)
LANGUAGE SQL
AS $$
    SELECT COALESCE(SUM(quantity * unit_price), 0)
    FROM order_items
    WHERE order_id = p_order_id;
$$;

-- Using the function
SELECT calculate_order_total(1);
SELECT order_id, calculate_order_total(order_id) AS total FROM orders;
```

### Function Syntax

```sql
CREATE [OR REPLACE] FUNCTION function_name(parameter_list)
RETURNS return_type
LANGUAGE language_name
AS $$
    -- function body
$$;
```

### Parameters

```sql
-- Multiple parameters with defaults
CREATE FUNCTION format_price(
    amount DECIMAL,
    currency VARCHAR DEFAULT 'USD',
    decimals INT DEFAULT 2
)
RETURNS VARCHAR
LANGUAGE SQL
AS $$
    SELECT currency || ' ' || ROUND(amount, decimals)::TEXT;
$$;

-- Calling with different arguments
SELECT format_price(99.99);                    -- 'USD 99.99'
SELECT format_price(99.99, 'EUR');             -- 'EUR 99.99'
SELECT format_price(99.99, 'JPY', 0);          -- 'JPY 100'
```

### Return Types

```sql
-- Return a scalar value
CREATE FUNCTION get_user_email(p_user_id INT)
RETURNS VARCHAR
LANGUAGE SQL
AS $$
    SELECT email FROM users WHERE user_id = p_user_id;
$$;

-- Return a table/set of rows
CREATE FUNCTION get_active_users()
RETURNS TABLE(user_id INT, email VARCHAR, name VARCHAR)
LANGUAGE SQL
AS $$
    SELECT user_id, email, name
    FROM users
    WHERE deleted_at IS NULL;
$$;

-- Using table-returning function
SELECT * FROM get_active_users();
SELECT * FROM get_active_users() WHERE name LIKE 'A%';
```

---

## PL/pgSQL Functions

PL/pgSQL is PostgreSQL's procedural language, offering variables, control flow, and more complex logic.

### Basic PL/pgSQL Function

```sql
CREATE OR REPLACE FUNCTION get_customer_status(p_customer_id INT)
RETURNS VARCHAR
LANGUAGE plpgsql
AS $$
DECLARE
    v_order_count INT;
    v_total_spent DECIMAL;
BEGIN
    -- Get customer statistics
    SELECT COUNT(*), COALESCE(SUM(total), 0)
    INTO v_order_count, v_total_spent
    FROM orders
    WHERE customer_id = p_customer_id;
    
    -- Determine status based on spending
    IF v_total_spent >= 10000 THEN
        RETURN 'platinum';
    ELSIF v_total_spent >= 5000 THEN
        RETURN 'gold';
    ELSIF v_total_spent >= 1000 THEN
        RETURN 'silver';
    ELSIF v_order_count > 0 THEN
        RETURN 'bronze';
    ELSE
        RETURN 'new';
    END IF;
END;
$$;
```

### Variables and Control Flow

```sql
CREATE OR REPLACE FUNCTION process_order(p_order_id INT)
RETURNS BOOLEAN
LANGUAGE plpgsql
AS $$
DECLARE
    v_item RECORD;
    v_total DECIMAL := 0;
    v_item_count INT := 0;
BEGIN
    -- Loop through order items
    FOR v_item IN 
        SELECT product_id, quantity, unit_price 
        FROM order_items 
        WHERE order_id = p_order_id
    LOOP
        v_total := v_total + (v_item.quantity * v_item.unit_price);
        v_item_count := v_item_count + 1;
        
        -- Update product stock
        UPDATE products 
        SET stock_quantity = stock_quantity - v_item.quantity
        WHERE product_id = v_item.product_id;
    END LOOP;
    
    -- Check if order had items
    IF v_item_count = 0 THEN
        RAISE EXCEPTION 'Order % has no items', p_order_id;
    END IF;
    
    -- Update order total
    UPDATE orders 
    SET total = v_total, status = 'processed'
    WHERE order_id = p_order_id;
    
    RETURN TRUE;
END;
$$;
```

### Exception Handling

```sql
CREATE OR REPLACE FUNCTION safe_divide(a DECIMAL, b DECIMAL)
RETURNS DECIMAL
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN a / b;
EXCEPTION
    WHEN division_by_zero THEN
        RETURN NULL;
    WHEN OTHERS THEN
        RAISE NOTICE 'Error: %', SQLERRM;
        RETURN NULL;
END;
$$;
```

### Raising Exceptions

```sql
CREATE OR REPLACE FUNCTION withdraw(p_account_id INT, p_amount DECIMAL)
RETURNS DECIMAL
LANGUAGE plpgsql
AS $$
DECLARE
    v_balance DECIMAL;
BEGIN
    -- Get current balance
    SELECT balance INTO v_balance
    FROM accounts
    WHERE account_id = p_account_id;
    
    -- Validate
    IF v_balance IS NULL THEN
        RAISE EXCEPTION 'Account % not found', p_account_id;
    END IF;
    
    IF p_amount <= 0 THEN
        RAISE EXCEPTION 'Amount must be positive, got %', p_amount;
    END IF;
    
    IF p_amount > v_balance THEN
        RAISE EXCEPTION 'Insufficient funds: balance=%, requested=%', v_balance, p_amount
            USING ERRCODE = 'insufficient_funds';  -- Custom error code
    END IF;
    
    -- Perform withdrawal
    UPDATE accounts
    SET balance = balance - p_amount
    WHERE account_id = p_account_id
    RETURNING balance INTO v_balance;
    
    RETURN v_balance;
END;
$$;
```

---

## Stored Procedures

Procedures differ from functions in key ways:
- **No return value** (use OUT parameters instead)
- **Can manage transactions** (COMMIT, ROLLBACK)
- **Called with CALL statement**

### Creating a Procedure

```sql
CREATE OR REPLACE PROCEDURE transfer_funds(
    p_from_account INT,
    p_to_account INT,
    p_amount DECIMAL
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_from_balance DECIMAL;
BEGIN
    -- Check source balance
    SELECT balance INTO v_from_balance
    FROM accounts
    WHERE account_id = p_from_account;
    
    IF v_from_balance < p_amount THEN
        RAISE EXCEPTION 'Insufficient funds';
    END IF;
    
    -- Debit source
    UPDATE accounts
    SET balance = balance - p_amount
    WHERE account_id = p_from_account;
    
    -- Credit destination
    UPDATE accounts
    SET balance = balance + p_amount
    WHERE account_id = p_to_account;
    
    -- Can commit within procedure
    COMMIT;
END;
$$;

-- Calling a procedure
CALL transfer_funds(1, 2, 500.00);
```

### Procedures with OUT Parameters

```sql
CREATE OR REPLACE PROCEDURE get_account_info(
    p_account_id INT,
    OUT p_balance DECIMAL,
    OUT p_status VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    SELECT balance, status
    INTO p_balance, p_status
    FROM accounts
    WHERE account_id = p_account_id;
END;
$$;

-- Calling and getting output
CALL get_account_info(1, NULL, NULL);
-- Returns: p_balance = 1000.00, p_status = 'active'
```

### Function vs Procedure

| Feature | Function | Procedure |
|---------|----------|-----------|
| Returns value | Yes (RETURNS) | No (use OUT params) |
| Called with | SELECT, FROM | CALL |
| Transaction control | No | Yes (COMMIT/ROLLBACK) |
| Use in expressions | Yes | No |
| Use in triggers | Yes | No |

---

## Triggers

Triggers automatically execute functions in response to table events (INSERT, UPDATE, DELETE).

### Trigger Basics

```sql
-- Step 1: Create the trigger function
CREATE OR REPLACE FUNCTION update_timestamp()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$;

-- Step 2: Create the trigger
CREATE TRIGGER trg_products_timestamp
BEFORE UPDATE ON products
FOR EACH ROW
EXECUTE FUNCTION update_timestamp();

-- Now every UPDATE on products automatically sets updated_at
UPDATE products SET price = 29.99 WHERE product_id = 1;
```

### Trigger Timing

```sql
-- BEFORE: Modify NEW values before they're written
CREATE TRIGGER trg_before_insert
BEFORE INSERT ON orders
FOR EACH ROW
EXECUTE FUNCTION validate_order();

-- AFTER: React after changes are written
CREATE TRIGGER trg_after_insert
AFTER INSERT ON orders
FOR EACH ROW
EXECUTE FUNCTION send_order_notification();

-- INSTEAD OF: Replace the operation (for views only)
CREATE TRIGGER trg_instead_update
INSTEAD OF UPDATE ON customer_view
FOR EACH ROW
EXECUTE FUNCTION handle_customer_update();
```

### Trigger Events

```sql
-- Single event
CREATE TRIGGER trg_audit_delete
AFTER DELETE ON customers
FOR EACH ROW
EXECUTE FUNCTION log_deletion();

-- Multiple events
CREATE TRIGGER trg_audit_all
AFTER INSERT OR UPDATE OR DELETE ON customers
FOR EACH ROW
EXECUTE FUNCTION log_all_changes();
```

### Trigger Special Variables

In trigger functions:
- `NEW` - The new row (INSERT, UPDATE)
- `OLD` - The old row (UPDATE, DELETE)
- `TG_OP` - Operation: 'INSERT', 'UPDATE', 'DELETE'
- `TG_TABLE_NAME` - Name of the table

```sql
CREATE OR REPLACE FUNCTION audit_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN
        INSERT INTO audit_log (table_name, operation, old_data)
        VALUES (TG_TABLE_NAME, 'DELETE', to_jsonb(OLD));
        RETURN OLD;
    ELSIF TG_OP = 'UPDATE' THEN
        INSERT INTO audit_log (table_name, operation, old_data, new_data)
        VALUES (TG_TABLE_NAME, 'UPDATE', to_jsonb(OLD), to_jsonb(NEW));
        RETURN NEW;
    ELSIF TG_OP = 'INSERT' THEN
        INSERT INTO audit_log (table_name, operation, new_data)
        VALUES (TG_TABLE_NAME, 'INSERT', to_jsonb(NEW));
        RETURN NEW;
    END IF;
END;
$$;
```

### FOR EACH ROW vs FOR EACH STATEMENT

```sql
-- FOR EACH ROW: Fires once per affected row
CREATE TRIGGER trg_row_level
AFTER INSERT ON orders
FOR EACH ROW
EXECUTE FUNCTION process_single_order();

-- FOR EACH STATEMENT: Fires once per SQL statement
CREATE TRIGGER trg_statement_level
AFTER INSERT ON orders
FOR EACH STATEMENT
EXECUTE FUNCTION log_bulk_insert();
```

### Conditional Triggers (WHEN clause)

```sql
-- Only fire trigger when condition is met
CREATE TRIGGER trg_high_value_order
AFTER INSERT ON orders
FOR EACH ROW
WHEN (NEW.total > 1000)
EXECUTE FUNCTION notify_high_value_order();
```

### Common Trigger Use Cases

**1. Auto-populate timestamps:**
```sql
CREATE OR REPLACE FUNCTION set_timestamps()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        NEW.created_at = CURRENT_TIMESTAMP;
    END IF;
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;
```

**2. Maintain denormalized data:**
```sql
CREATE OR REPLACE FUNCTION update_order_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE customers SET order_count = order_count + 1 
        WHERE customer_id = NEW.customer_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE customers SET order_count = order_count - 1 
        WHERE customer_id = OLD.customer_id;
    END IF;
    RETURN NULL;  -- AFTER trigger, return ignored
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_order_count
AFTER INSERT OR DELETE ON orders
FOR EACH ROW
EXECUTE FUNCTION update_order_count();
```

**3. Enforce complex business rules:**
```sql
CREATE OR REPLACE FUNCTION check_inventory()
RETURNS TRIGGER AS $$
DECLARE
    v_stock INT;
BEGIN
    SELECT stock_quantity INTO v_stock
    FROM products WHERE product_id = NEW.product_id;
    
    IF v_stock < NEW.quantity THEN
        RAISE EXCEPTION 'Insufficient stock for product %', NEW.product_id;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_inventory
BEFORE INSERT ON order_items
FOR EACH ROW
EXECUTE FUNCTION check_inventory();
```

---

## Managing Functions and Triggers

### View Existing

```sql
-- List functions
SELECT proname, prosrc 
FROM pg_proc 
WHERE pronamespace = 'public'::regnamespace;

-- List triggers
SELECT tgname, tgrelid::regclass, tgenabled
FROM pg_trigger
WHERE NOT tgisinternal;
```

### Drop

```sql
-- Drop function
DROP FUNCTION IF EXISTS calculate_order_total(INT);

-- Drop procedure
DROP PROCEDURE IF EXISTS transfer_funds(INT, INT, DECIMAL);

-- Drop trigger
DROP TRIGGER IF EXISTS trg_products_timestamp ON products;
```

### Disable/Enable Triggers

```sql
-- Disable single trigger
ALTER TABLE products DISABLE TRIGGER trg_products_timestamp;

-- Disable all triggers on table
ALTER TABLE products DISABLE TRIGGER ALL;

-- Re-enable
ALTER TABLE products ENABLE TRIGGER trg_products_timestamp;
ALTER TABLE products ENABLE TRIGGER ALL;
```

---

## Practice

### Exercise 1: Create a Function

Write a function `get_product_revenue(product_id INT)` that returns the total revenue generated by a product across all orders.

### Exercise 2: Create a Trigger

Create a trigger that:
1. Automatically generates an `order_number` (like 'ORD-2026-00001') on INSERT
2. The number should auto-increment per year

### Exercise 3: Create an Audit Trigger

Create a reusable audit trigger that logs all changes to any table to a central `audit_log` table, capturing:
- Table name
- Operation type
- Old and new values as JSONB
- Timestamp
- User who made the change

---

## Key Takeaways

1. **Functions** return values and can be used in SELECT statements
2. **Procedures** use CALL and can control transactions
3. **Triggers** execute automatically on table events
4. **BEFORE triggers** can modify data before it's written
5. **AFTER triggers** react to changes after they're committed
6. **NEW** contains the new row; **OLD** contains the old row
7. **Use triggers sparingly** - they add hidden complexity
8. **Document triggers well** - their automatic nature can surprise developers
