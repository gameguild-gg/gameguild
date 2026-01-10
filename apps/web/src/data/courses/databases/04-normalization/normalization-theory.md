# Database Normalization Theory

Normalization is the process of organizing data in a database to reduce redundancy and improve data integrity. It involves decomposing tables into smaller, well-structured tables while maintaining relationships between them.

---

## Why Normalize?

### Problems with Unnormalized Data

Consider this denormalized table storing order information:

| order_id | customer_name | customer_email | product_name | product_price | quantity | order_date |
|----------|---------------|----------------|--------------|---------------|----------|------------|
| 1 | Alice Smith | alice@email.com | Laptop | 999.99 | 1 | 2026-01-15 |
| 2 | Alice Smith | alice@email.com | Mouse | 29.99 | 2 | 2026-01-15 |
| 3 | Bob Jones | bob@email.com | Laptop | 999.99 | 1 | 2026-01-16 |
| 4 | Alice Smith | alice_new@email.com | Keyboard | 79.99 | 1 | 2026-01-17 |

This structure has several problems:

### 1. Update Anomaly
If Alice changes her email, we must update **every row** where she appears. In row 4, someone updated her email but forgot rows 1 and 2 - now we have inconsistent data.

### 2. Insert Anomaly
We cannot add a new product to our catalog without creating an order for it. The product information is tied to order data.

### 3. Delete Anomaly
If we delete Bob's only order (row 3), we lose all information about the fact that Bob is a customer.

### 4. Data Redundancy
Customer names, emails, and product prices are repeated across rows, wasting storage and creating opportunities for inconsistency.

---

## Functional Dependencies

Before understanding normal forms, we need to understand **functional dependencies**.

### Definition

A **functional dependency** (FD) exists when one attribute uniquely determines another:

```
A → B
```

This means: "If you know the value of A, you can determine the value of B."

### Examples

```
student_id → student_name        -- Knowing the ID tells you the name
isbn → book_title               -- An ISBN uniquely identifies a book title
(order_id, product_id) → quantity  -- The combination determines quantity
```

### Types of Dependencies

**Full Functional Dependency:**
The entire key is needed to determine the attribute.
```
(order_id, product_id) → quantity
-- Both order_id AND product_id are needed
```

**Partial Dependency:**
Only part of a composite key determines the attribute.
```
(order_id, product_id) → order_date
-- Only order_id is needed; product_id is irrelevant
```

**Transitive Dependency:**
An attribute depends on another non-key attribute.
```
employee_id → department_id → department_name
-- department_name depends on employee_id through department_id
```

---

## First Normal Form (1NF)

### Rule
A table is in **1NF** if:
1. Each column contains only **atomic** (indivisible) values
2. Each column contains values of a **single type**
3. Each row is **unique** (has a primary key)
4. There are **no repeating groups** or arrays

### Violation Example

**Not in 1NF - Multi-valued column:**

| student_id | name | phone_numbers |
|------------|------|---------------|
| 1 | Alice | 555-1234, 555-5678 |
| 2 | Bob | 555-9999 |

The `phone_numbers` column contains multiple values - not atomic.

**Not in 1NF - Repeating groups:**

| order_id | product1 | qty1 | product2 | qty2 | product3 | qty3 |
|----------|----------|------|----------|------|----------|------|
| 1 | Laptop | 1 | Mouse | 2 | NULL | NULL |

Repeating columns for products violate 1NF.

### Fixed - In 1NF

**Student phones (separate table):**

| student_id | phone_number |
|------------|--------------|
| 1 | 555-1234 |
| 1 | 555-5678 |
| 2 | 555-9999 |

**Order items (separate table):**

| order_id | product_name | quantity |
|----------|--------------|----------|
| 1 | Laptop | 1 |
| 1 | Mouse | 2 |

### 1NF SQL Example

```sql
-- Violates 1NF (conceptually - PostgreSQL arrays)
CREATE TABLE students_bad (
    student_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    phone_numbers TEXT[]  -- Array = multi-valued
);

-- 1NF Compliant
CREATE TABLE students (
    student_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE student_phones (
    student_id INT REFERENCES students(student_id),
    phone_number VARCHAR(20) NOT NULL,
    phone_type VARCHAR(20) DEFAULT 'mobile',
    PRIMARY KEY (student_id, phone_number)
);
```

---

## Second Normal Form (2NF)

### Rule
A table is in **2NF** if:
1. It is in 1NF
2. Every non-key attribute is **fully functionally dependent** on the **entire** primary key

2NF addresses **partial dependencies** - where non-key attributes depend on only *part* of a composite key.

> **Note:** 2NF only applies to tables with composite (multi-column) primary keys. A table with a single-column primary key that is in 1NF is automatically in 2NF.

### Violation Example

| order_id | product_id | product_name | product_price | quantity |
|----------|------------|--------------|---------------|----------|
| 1 | 101 | Laptop | 999.99 | 1 |
| 1 | 102 | Mouse | 29.99 | 2 |
| 2 | 101 | Laptop | 999.99 | 1 |

**Primary Key:** `(order_id, product_id)`

**Dependencies:**
- `(order_id, product_id) → quantity` ✅ Full dependency
- `product_id → product_name` ❌ Partial dependency (only part of PK)
- `product_id → product_price` ❌ Partial dependency

`product_name` and `product_price` depend only on `product_id`, not on the full composite key.

### Fixed - In 2NF

Decompose into two tables:

**products table:**
| product_id | product_name | product_price |
|------------|--------------|---------------|
| 101 | Laptop | 999.99 |
| 102 | Mouse | 29.99 |

**order_items table:**
| order_id | product_id | quantity |
|----------|------------|----------|
| 1 | 101 | 1 |
| 1 | 102 | 2 |
| 2 | 101 | 1 |

### 2NF SQL Example

```sql
-- 2NF Compliant
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    product_name VARCHAR(200) NOT NULL,
    product_price DECIMAL(10, 2) NOT NULL
);

CREATE TABLE order_items (
    order_id INT REFERENCES orders(order_id),
    product_id INT REFERENCES products(product_id),
    quantity INT NOT NULL CHECK (quantity > 0),
    PRIMARY KEY (order_id, product_id)
);
```

---

## Third Normal Form (3NF)

### Rule
A table is in **3NF** if:
1. It is in 2NF
2. There are **no transitive dependencies** - every non-key attribute depends **only** on the primary key, not on other non-key attributes

### Violation Example

| employee_id | employee_name | department_id | department_name | department_budget |
|-------------|---------------|---------------|-----------------|-------------------|
| 1 | Alice | 10 | Engineering | 500000 |
| 2 | Bob | 10 | Engineering | 500000 |
| 3 | Carol | 20 | Marketing | 300000 |

**Primary Key:** `employee_id`

**Dependencies:**
- `employee_id → employee_name` ✅ Direct dependency on PK
- `employee_id → department_id` ✅ Direct dependency on PK
- `department_id → department_name` ❌ Transitive dependency
- `department_id → department_budget` ❌ Transitive dependency

`department_name` and `department_budget` depend on `department_id`, not directly on `employee_id`.

### Fixed - In 3NF

**departments table:**
| department_id | department_name | department_budget |
|---------------|-----------------|-------------------|
| 10 | Engineering | 500000 |
| 20 | Marketing | 300000 |

**employees table:**
| employee_id | employee_name | department_id |
|-------------|---------------|---------------|
| 1 | Alice | 10 |
| 2 | Bob | 10 |
| 3 | Carol | 20 |

### 3NF SQL Example

```sql
CREATE TABLE departments (
    department_id SERIAL PRIMARY KEY,
    department_name VARCHAR(100) NOT NULL UNIQUE,
    department_budget DECIMAL(12, 2) NOT NULL DEFAULT 0
);

CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    employee_name VARCHAR(100) NOT NULL,
    department_id INT REFERENCES departments(department_id)
);
```

---

## Boyce-Codd Normal Form (BCNF)

### Rule
A table is in **BCNF** if:
1. It is in 3NF
2. For every functional dependency `X → Y`, X must be a **superkey** (a candidate key or superset of one)

BCNF is a stricter version of 3NF that handles edge cases where 3NF allows anomalies.

### When BCNF Differs from 3NF

BCNF violations occur when:
- A table has multiple overlapping candidate keys
- A non-key attribute determines part of a candidate key

### Violation Example

Consider a table tracking which professors teach which subjects in which semesters:

| student_id | subject | professor |
|------------|---------|-----------|
| 1 | Math | Dr. Smith |
| 1 | Physics | Dr. Jones |
| 2 | Math | Dr. Smith |
| 2 | Physics | Dr. Brown |

**Assumptions:**
- Each professor teaches only ONE subject
- A subject can be taught by multiple professors
- A student takes one subject from one professor

**Candidate Key:** `(student_id, subject)` or `(student_id, professor)`

**Dependencies:**
- `professor → subject` (each professor teaches one subject)

This violates BCNF because `professor` is not a superkey, yet it determines `subject`.

### Fixed - In BCNF

**professor_subjects table:**
| professor | subject |
|-----------|---------|
| Dr. Smith | Math |
| Dr. Jones | Physics |
| Dr. Brown | Physics |

**student_professors table:**
| student_id | professor |
|------------|-----------|
| 1 | Dr. Smith |
| 1 | Dr. Jones |
| 2 | Dr. Smith |
| 2 | Dr. Brown |

Now we can derive which subject a student takes by joining the tables.

### BCNF SQL Example

```sql
CREATE TABLE professor_subjects (
    professor VARCHAR(100) PRIMARY KEY,
    subject VARCHAR(100) NOT NULL
);

CREATE TABLE student_professors (
    student_id INT REFERENCES students(student_id),
    professor VARCHAR(100) REFERENCES professor_subjects(professor),
    PRIMARY KEY (student_id, professor)
);

-- To find student subjects:
SELECT sp.student_id, ps.subject
FROM student_professors sp
JOIN professor_subjects ps ON sp.professor = ps.professor;
```

---

## Normal Form Summary

| Normal Form | Requirement | Eliminates |
|-------------|-------------|------------|
| **1NF** | Atomic values, no repeating groups | Multi-valued attributes |
| **2NF** | 1NF + no partial dependencies | Redundancy from composite keys |
| **3NF** | 2NF + no transitive dependencies | Redundancy from non-key dependencies |
| **BCNF** | Every determinant is a candidate key | Remaining anomalies from overlapping keys |

### The Ladder

```
Unnormalized
     ↓
   1NF  (atomic values)
     ↓
   2NF  (remove partial dependencies)
     ↓
   3NF  (remove transitive dependencies)
     ↓
  BCNF  (every determinant is a superkey)
```

### Quick Checks

| Question | If Yes, Violates |
|----------|------------------|
| Does any column contain lists, arrays, or comma-separated values? | 1NF |
| Does a non-key column depend on only *part* of the primary key? | 2NF |
| Does a non-key column depend on another non-key column? | 3NF |
| Does any non-superkey determine another attribute? | BCNF |

---

## Normalization Process: Step-by-Step

### Example: Normalizing an Order Spreadsheet

**Original Denormalized Data:**

| order_id | order_date | customer_name | customer_email | customer_city | items |
|----------|------------|---------------|----------------|---------------|-------|
| 1 | 2026-01-15 | Alice | alice@mail.com | NYC | Laptop:1:999, Mouse:2:30 |
| 2 | 2026-01-16 | Bob | bob@mail.com | LA | Keyboard:1:80 |

### Step 1: Convert to 1NF

Remove multi-valued `items` column:

| order_id | order_date | customer_name | customer_email | customer_city | product | qty | price |
|----------|------------|---------------|----------------|---------------|---------|-----|-------|
| 1 | 2026-01-15 | Alice | alice@mail.com | NYC | Laptop | 1 | 999 |
| 1 | 2026-01-15 | Alice | alice@mail.com | NYC | Mouse | 2 | 30 |
| 2 | 2026-01-16 | Bob | bob@mail.com | LA | Keyboard | 1 | 80 |

**PK:** `(order_id, product)`

### Step 2: Convert to 2NF

Identify partial dependencies:
- `order_id → order_date, customer_name, customer_email, customer_city`
- `product → price` (assuming fixed pricing)

Extract to separate tables:

**orders:**
| order_id | order_date | customer_name | customer_email | customer_city |
|----------|------------|---------------|----------------|---------------|
| 1 | 2026-01-15 | Alice | alice@mail.com | NYC |
| 2 | 2026-01-16 | Bob | bob@mail.com | LA |

**products:**
| product | price |
|---------|-------|
| Laptop | 999 |
| Mouse | 30 |
| Keyboard | 80 |

**order_items:**
| order_id | product | qty |
|----------|---------|-----|
| 1 | Laptop | 1 |
| 1 | Mouse | 2 |
| 2 | Keyboard | 1 |

### Step 3: Convert to 3NF

In the orders table, identify transitive dependencies:
- If `customer_email → customer_name, customer_city` (email determines customer info)

Extract customers:

**customers:**
| customer_id | customer_name | customer_email | customer_city |
|-------------|---------------|----------------|---------------|
| 1 | Alice | alice@mail.com | NYC |
| 2 | Bob | bob@mail.com | LA |

**orders (updated):**
| order_id | order_date | customer_id |
|----------|------------|-------------|
| 1 | 2026-01-15 | 1 |
| 2 | 2026-01-16 | 2 |

### Final 3NF Schema

```sql
CREATE TABLE customers (
    customer_id SERIAL PRIMARY KEY,
    customer_name VARCHAR(100) NOT NULL,
    customer_email VARCHAR(255) NOT NULL UNIQUE,
    customer_city VARCHAR(100)
);

CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    product_name VARCHAR(200) NOT NULL UNIQUE,
    price DECIMAL(10, 2) NOT NULL
);

CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    order_date DATE NOT NULL DEFAULT CURRENT_DATE,
    customer_id INT NOT NULL REFERENCES customers(customer_id)
);

CREATE TABLE order_items (
    order_id INT REFERENCES orders(order_id),
    product_id INT REFERENCES products(product_id),
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10, 2) NOT NULL,  -- snapshot of price at order time
    PRIMARY KEY (order_id, product_id)
);
```

> **Note:** We store `unit_price` in `order_items` because product prices may change, but we need to remember what the customer actually paid.

---

## Practice

### Exercise 1: Identify Normal Form Violations

For each table, identify which normal form it violates and why:

**Table A:**
| id | name | skills |
|----|------|--------|
| 1 | Alice | Python, SQL, Docker |
| 2 | Bob | Java, SQL |

**Table B:**
| order_id | line_num | product_name | product_category | quantity |
|----------|----------|--------------|------------------|----------|
| 1 | 1 | Laptop | Electronics | 1 |
| 1 | 2 | Mouse | Electronics | 2 |
| 2 | 1 | Laptop | Electronics | 3 |

**Table C:**
| emp_id | name | dept_id | dept_name | manager_id | manager_name |
|--------|------|---------|-----------|------------|--------------|
| 1 | Alice | 10 | Sales | 5 | Carol |
| 2 | Bob | 10 | Sales | 5 | Carol |

### Exercise 2: Normalize This Schema

Given this spreadsheet data, design a fully normalized (3NF) schema:

| invoice_id | invoice_date | client_name | client_address | service1 | hours1 | rate1 | service2 | hours2 | rate2 |
|------------|--------------|-------------|----------------|----------|--------|-------|----------|--------|-------|
| INV-001 | 2026-01-15 | Acme Corp | 123 Main St | Consulting | 8 | 150 | Development | 16 | 200 |
| INV-002 | 2026-01-16 | Acme Corp | 123 Main St | Consulting | 4 | 150 | NULL | NULL | NULL |
| INV-003 | 2026-01-17 | Beta LLC | 456 Oak Ave | Development | 20 | 200 | NULL | NULL | NULL |

### Exercise 3: Identify Functional Dependencies

List all functional dependencies in this table:

| student_id | course_id | instructor_id | instructor_name | grade | semester |
|------------|-----------|---------------|-----------------|-------|----------|
| S001 | CSI300 | I01 | Prof Smith | A | Spring26 |
| S001 | CSI281 | I02 | Prof Jones | B+ | Fall25 |
| S002 | CSI300 | I01 | Prof Smith | B | Spring26 |

---

## Key Takeaways

1. **Normalization reduces redundancy** but may require more JOINs to retrieve data
2. **1NF** ensures atomic values - no lists or repeating groups
3. **2NF** eliminates partial dependencies on composite keys
4. **3NF** eliminates transitive dependencies through non-key attributes
5. **BCNF** ensures every determinant is a candidate key
6. **Most applications aim for 3NF** as a good balance between integrity and performance
7. **Denormalization is sometimes necessary** for read performance (covered in practical-normalization.md)
