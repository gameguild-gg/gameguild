# Referential Actions (CASCADE, RESTRICT, SET NULL)

Referential actions define what happens to related records when a parent record is updated or deleted. They are specified on foreign key constraints.

---

## The Problem

When you have related tables, deleting or updating a parent record creates a dilemma:

```sql
CREATE TABLE departments (
    dept_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    dept_id INT REFERENCES departments(dept_id)
);

-- Insert sample data
INSERT INTO departments VALUES (1, 'Engineering'), (2, 'Marketing');
INSERT INTO employees VALUES (1, 'Alice', 1), (2, 'Bob', 1), (3, 'Carol', 2);

-- What happens when we try to delete Engineering?
DELETE FROM departments WHERE dept_id = 1;
-- ERROR: update or delete on table "departments" violates foreign key constraint
```

Referential actions tell the database how to handle these situations automatically.

---

## ON DELETE Actions

### NO ACTION (Default)

Prevents deletion if child records exist. The check is deferred to the end of the statement.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON DELETE NO ACTION
);

-- Attempting to delete a referenced department:
DELETE FROM departments WHERE dept_id = 1;
-- ERROR: violates foreign key constraint
```

### RESTRICT

Similar to NO ACTION, but the check is immediate. In PostgreSQL, there's no practical difference unless you're using deferred constraints.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON DELETE RESTRICT
);
```

### CASCADE

Automatically deletes all child records when the parent is deleted.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON DELETE CASCADE
);

-- Delete department
DELETE FROM departments WHERE dept_id = 1;
-- Employees Alice and Bob are automatically deleted
```

**Use Cases:**
- Order → Order Items (delete order, delete its items)
- Post → Comments (delete post, delete its comments)
- User → User Settings (delete user, delete their settings)

**⚠️ Danger:** Cascading deletes can be destructive. Be careful with data you might need.

### SET NULL

Sets the foreign key column to NULL when the parent is deleted.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON DELETE SET NULL
);

-- Delete department
DELETE FROM departments WHERE dept_id = 1;
-- Alice and Bob now have dept_id = NULL
```

**Requirements:** The FK column must allow NULL values.

**Use Cases:**
- Product → Category (delete category, products become uncategorized)
- Employee → Manager (delete manager, employees have no manager)

### SET DEFAULT

Sets the foreign key column to its DEFAULT value when the parent is deleted.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT DEFAULT 0 REFERENCES departments(dept_id) ON DELETE SET DEFAULT
);

-- Requires a default department to exist
INSERT INTO departments VALUES (0, 'Unassigned');

-- Delete department
DELETE FROM departments WHERE dept_id = 1;
-- Alice and Bob now have dept_id = 0 (Unassigned)
```

**Requirements:** 
- The FK column must have a DEFAULT value
- The default value must reference a valid parent record

---

## ON UPDATE Actions

The same actions are available for updates to the parent's primary key.

### NO ACTION / RESTRICT (Default)

Prevents updating the primary key if child records reference it.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON UPDATE RESTRICT
);

-- Attempting to update a referenced department's ID:
UPDATE departments SET dept_id = 100 WHERE dept_id = 1;
-- ERROR: violates foreign key constraint
```

### CASCADE

Automatically updates child records when the parent's key changes.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON UPDATE CASCADE
);

-- Update department ID
UPDATE departments SET dept_id = 100 WHERE dept_id = 1;
-- Alice and Bob now have dept_id = 100 (automatically updated)
```

**Use Cases:**
- When using natural keys that might change
- Code tables where codes are occasionally updated

### SET NULL / SET DEFAULT

Same behavior as ON DELETE - sets the FK to NULL or its default value.

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON UPDATE SET NULL
);
```

---

## Combining ON DELETE and ON UPDATE

You can specify different actions for delete and update:

```sql
CREATE TABLE order_items (
    item_id SERIAL PRIMARY KEY,
    order_id INT REFERENCES orders(order_id) 
        ON DELETE CASCADE    -- Delete items when order is deleted
        ON UPDATE CASCADE,   -- Update FK if order_id changes
    product_id INT REFERENCES products(product_id) 
        ON DELETE RESTRICT   -- Can't delete product with order history
        ON UPDATE CASCADE    -- Update FK if product_id changes
);
```

---

## Complete Syntax

```sql
CREATE TABLE child_table (
    id SERIAL PRIMARY KEY,
    parent_id INT,
    CONSTRAINT fk_parent
        FOREIGN KEY (parent_id)
        REFERENCES parent_table(id)
        ON DELETE CASCADE
        ON UPDATE CASCADE
);

-- Or inline syntax
CREATE TABLE child_table (
    id SERIAL PRIMARY KEY,
    parent_id INT REFERENCES parent_table(id) ON DELETE CASCADE ON UPDATE CASCADE
);
```

---

## Decision Matrix

| Scenario | ON DELETE | ON UPDATE |
|----------|-----------|-----------|
| Child data meaningless without parent | CASCADE | CASCADE |
| Child can exist independently | SET NULL | CASCADE |
| Child should use a default fallback | SET DEFAULT | CASCADE |
| Deletion should require manual cleanup | RESTRICT | CASCADE |
| Using surrogate keys (auto-increment) | Any | Not relevant |
| Using natural keys that may change | Any | CASCADE |

### Common Patterns

| Parent → Child | ON DELETE | Reason |
|----------------|-----------|--------|
| Order → Order Items | CASCADE | Items belong to the order |
| User → Posts | SET NULL | Keep posts, mark author as deleted |
| Category → Products | SET NULL | Keep products, mark as uncategorized |
| Department → Employees | RESTRICT | Must reassign employees first |
| Invoice → Line Items | CASCADE | Line items belong to invoice |
| Account → Transactions | RESTRICT | Can't delete account with transactions |

---

## Cascading Chains

CASCADE can trigger through multiple levels:

```sql
CREATE TABLE grandparent (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100)
);

CREATE TABLE parent (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    grandparent_id INT REFERENCES grandparent(id) ON DELETE CASCADE
);

CREATE TABLE child (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    parent_id INT REFERENCES parent(id) ON DELETE CASCADE
);

-- Deleting a grandparent cascades to parents, which cascades to children
DELETE FROM grandparent WHERE id = 1;
-- All related parents AND children are deleted
```

**⚠️ Warning:** Deep cascade chains can delete more data than expected. Always review your schema's cascade paths.

---

## Self-Referencing with CASCADE

Be careful with self-referencing tables:

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_id INT REFERENCES employees(emp_id) ON DELETE CASCADE
);

-- If you delete a manager, all their reports are deleted
-- And if those reports were managers, their reports are deleted too!
```

**Safer approach:**

```sql
CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_id INT REFERENCES employees(emp_id) ON DELETE SET NULL
);

-- Deleting a manager leaves employees without a manager (needs reassignment)
```

---

## Circular References

Be careful with circular foreign keys:

```sql
-- This creates problems
CREATE TABLE departments (
    dept_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    head_emp_id INT  -- Will reference employees
);

CREATE TABLE employees (
    emp_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id) ON DELETE CASCADE
);

-- Now try to add the FK to departments
ALTER TABLE departments 
ADD CONSTRAINT fk_head_emp 
FOREIGN KEY (head_emp_id) REFERENCES employees(emp_id) ON DELETE SET NULL;

-- With ON DELETE CASCADE on both, deleting could cause infinite loops
-- PostgreSQL detects and prevents this, but it's still a design smell
```

**Better design:** Use a separate junction table or avoid circular dependencies.

---

## Viewing Existing Constraints

```sql
-- List all foreign key constraints
SELECT
    tc.table_name,
    kcu.column_name,
    ccu.table_name AS foreign_table,
    ccu.column_name AS foreign_column,
    rc.update_rule,
    rc.delete_rule
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
JOIN information_schema.referential_constraints AS rc
    ON rc.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
ORDER BY tc.table_name;
```

---

## Modifying Referential Actions

You cannot modify a constraint in place. You must drop and recreate:

```sql
-- Drop the existing constraint
ALTER TABLE employees DROP CONSTRAINT employees_dept_id_fkey;

-- Add with new referential action
ALTER TABLE employees 
ADD CONSTRAINT employees_dept_id_fkey 
FOREIGN KEY (dept_id) REFERENCES departments(dept_id) ON DELETE SET NULL;
```

---

## Practice

### Exercise 1: Choose the Right Action

For each scenario, choose the appropriate ON DELETE action:

1. A `shopping_cart` has many `cart_items`. When the cart is deleted, items should be deleted.
2. A `team` has many `players`. When a team is deleted, players should remain but not belong to any team.
3. An `author` has many `books`. Books must always have an author, so authors with books cannot be deleted.
4. A `product` has an optional `supplier`. When a supplier is deleted, products should point to a default supplier.

### Exercise 2: Design Review

Review this schema and identify potential issues with the CASCADE chain:

```sql
CREATE TABLE organizations (id SERIAL PRIMARY KEY, name VARCHAR(100));
CREATE TABLE departments (id SERIAL PRIMARY KEY, org_id INT REFERENCES organizations(id) ON DELETE CASCADE);
CREATE TABLE teams (id SERIAL PRIMARY KEY, dept_id INT REFERENCES departments(id) ON DELETE CASCADE);
CREATE TABLE employees (id SERIAL PRIMARY KEY, team_id INT REFERENCES teams(id) ON DELETE CASCADE);
CREATE TABLE tasks (id SERIAL PRIMARY KEY, emp_id INT REFERENCES employees(id) ON DELETE CASCADE);
CREATE TABLE time_entries (id SERIAL PRIMARY KEY, task_id INT REFERENCES tasks(id) ON DELETE CASCADE);
```

What happens when you delete an organization?

---

## Key Takeaways

1. **RESTRICT/NO ACTION** - Prevent deletion if children exist (safest default)
2. **CASCADE** - Delete children when parent is deleted (for owned data)
3. **SET NULL** - Orphan children by setting FK to NULL (for independent data)
4. **SET DEFAULT** - Use a fallback value (requires default value and valid reference)
5. **ON UPDATE CASCADE** - Essential when using natural keys that may change
6. **Review cascade chains** - Understand how deep deletes propagate
7. **Be careful with self-references** - CASCADE on manager_id can delete entire org trees
