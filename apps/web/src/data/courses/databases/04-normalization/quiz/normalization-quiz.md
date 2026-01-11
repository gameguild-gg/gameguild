# Week 04 Quiz: Normalization, Entity-Relationships & Indexing

## Instructions

This quiz covers:
- **Part A:** Normal Forms (1NF, 2NF, 3NF, BCNF) - 15 questions
- **Part B:** Entity-Relationships & Schema Design - 15 questions  
- **Part C:** Indexing Fundamentals - 15 questions

**Total: 45 questions**

Time estimate: 45-60 minutes

---

# Part A: Normal Forms & Functional Dependencies

## True or False (Questions 1-8)

### Question 1

**A table with a single-column primary key that is in 1NF is automatically in 2NF.**

- [ ] True
- [ ] False

---

### Question 2

**If a column contains NULL values, the table violates First Normal Form (1NF).**

- [ ] True
- [ ] False

---

### Question 3

**In a table with composite primary key (A, B), if column C depends only on A, this is a partial dependency and violates 2NF.**

- [ ] True
- [ ] False

---

### Question 4

**Third Normal Form (3NF) eliminates all transitive dependencies where a non-key attribute determines another non-key attribute.**

- [ ] True
- [ ] False

---

### Question 5

**BCNF (Boyce-Codd Normal Form) is always achievable without losing information or dependencies.**

- [ ] True
- [ ] False

---

### Question 6

**A table storing `employee_id, employee_name, department_id, department_name` violates 3NF because `department_name` depends on `department_id`, not directly on the primary key.**

- [ ] True
- [ ] False

---

### Question 7

**The functional dependency `A → B` means that knowing the value of B allows you to determine the value of A.**

- [ ] True
- [ ] False

---

### Question 8

**Normalization always improves query performance by reducing data redundancy.**

- [ ] True
- [ ] False

---

## Multiple Choice (Questions 9-15)

### Question 9

**Which normal form violation does this table have?**

| student_id | name | courses |
|------------|------|---------|
| 1 | Alice | Math, Physics, Chemistry |
| 2 | Bob | History, English |

- [ ] A. Violates 1NF - multi-valued attribute
- [ ] B. Violates 2NF - partial dependency
- [ ] C. Violates 3NF - transitive dependency
- [ ] D. No violation - table is in 3NF

---

### Question 10

**Given this table with primary key (order_id, product_id):**

| order_id | product_id | product_name | quantity |
|----------|------------|--------------|----------|
| 1 | 101 | Laptop | 2 |
| 2 | 101 | Laptop | 1 |

**Which statement is correct?**

- [ ] A. Violates 1NF because product_name is repeated
- [ ] B. Violates 2NF because product_name depends only on product_id
- [ ] C. Violates 3NF because of a transitive dependency
- [ ] D. The table is properly normalized to 3NF

---

### Question 11

**Which functional dependency represents a transitive dependency in a table where `employee_id` is the primary key?**

- [ ] A. `employee_id → employee_name`
- [ ] B. `employee_id → department_id`
- [ ] C. `department_id → department_name`
- [ ] D. `employee_id → (employee_name, department_id)`

---

### Question 12

**To convert a 2NF table to 3NF, you must:**

- [ ] A. Remove all multi-valued attributes
- [ ] B. Ensure every non-key attribute is fully dependent on the entire primary key
- [ ] C. Remove transitive dependencies by creating separate tables
- [ ] D. Ensure every determinant is a candidate key

---

### Question 13

**A table has columns: `isbn, title, author_name, author_nationality`. The primary key is `isbn`, and each book has one author. Which statement is correct?**

- [ ] A. The table is in 3NF because all attributes depend on the primary key
- [ ] B. The table violates 3NF because `author_nationality` depends on `author_name`
- [ ] C. The table violates 2NF because of partial dependencies
- [ ] D. The table violates 1NF because author information is repeated

---

### Question 14

**Which of the following is a valid reason to intentionally denormalize a database?**

- [ ] A. To eliminate the need for foreign keys
- [ ] B. To improve write performance on INSERT operations
- [ ] C. To improve read performance for frequently-accessed aggregate data
- [ ] D. To enforce referential integrity more easily

---

### Question 15

**What is the primary difference between BCNF and 3NF?**

- [ ] A. BCNF requires atomic values; 3NF does not
- [ ] B. BCNF eliminates partial dependencies; 3NF eliminates transitive dependencies
- [ ] C. BCNF requires every determinant to be a superkey; 3NF allows non-superkey determinants if the dependent is a prime attribute
- [ ] D. There is no difference; BCNF and 3NF are equivalent

---

# Part B: Entity-Relationships & Schema Design

## True or False (Questions 16-22)

### Question 16

**In a 1:N (one-to-many) relationship, the foreign key should be placed in the table on the "many" side.**

- [ ] True
- [ ] False

---

### Question 17

**A junction table (bridge table) is required to implement a many-to-many (M:N) relationship in a relational database.**

- [ ] True
- [ ] False

---

### Question 18

**A self-referencing relationship occurs when a table has a foreign key that references its own primary key.**

- [ ] True
- [ ] False

---

### Question 19

**In a 1:1 relationship, the foreign key must be placed in both tables to enforce the relationship.**

- [ ] True
- [ ] False

---

### Question 20

**A weak entity cannot exist without its identifying (owner) entity and typically uses a composite primary key that includes the owner's key.**

- [ ] True
- [ ] False

---

### Question 21

**In Crow's Foot notation, the symbol `──○<──` represents a mandatory many relationship.**

- [ ] True
- [ ] False

---

### Question 22

**A junction table can only contain the foreign keys from the two related tables and cannot have additional attributes.**

- [ ] True
- [ ] False

---

## Multiple Choice (Questions 23-30)

### Question 23

**Which relationship type exists between Students and Courses if each student can enroll in multiple courses and each course can have multiple students?**

- [ ] A. One-to-One (1:1)
- [ ] B. One-to-Many (1:N)
- [ ] C. Many-to-Many (M:N)
- [ ] D. Self-referencing

---

### Question 24

**Given the requirement: "Each employee belongs to exactly one department, and a department can have many employees." Which SQL correctly implements this?**

- [ ] A. 
```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    department_id INT UNIQUE REFERENCES departments(department_id)
);
```

- [ ] B.
```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    department_id INT REFERENCES departments(department_id)
);
```

- [ ] C.
```sql
CREATE TABLE departments (
    department_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    employee_id INT REFERENCES employees(employee_id)
);
```

- [ ] D.
```sql
CREATE TABLE employee_departments (
    employee_id INT REFERENCES employees(employee_id),
    department_id INT REFERENCES departments(department_id),
    PRIMARY KEY (employee_id, department_id)
);
```

---

### Question 25

**How would you implement a many-to-many relationship between `movies` and `actors` where you also need to store the `role_name` each actor played?**

- [ ] A. Add a `role_name` column to the `movies` table
- [ ] B. Add a `role_name` column to the `actors` table
- [ ] C. Create a junction table with `movie_id`, `actor_id`, and `role_name`
- [ ] D. Store `role_name` as a comma-separated list in either table

---

### Question 26

**For a self-referencing relationship where employees have managers (who are also employees), which implementation is correct?**

- [ ] A.
```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_id INT REFERENCES managers(manager_id)
);
```

- [ ] B.
```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_id INT REFERENCES employees(employee_id)
);
```

- [ ] C.
```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100)
);
CREATE TABLE managers (
    employee_id INT REFERENCES employees(employee_id),
    reports_to INT REFERENCES employees(employee_id)
);
```

- [ ] D.
```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_name VARCHAR(100)
);
```

---

### Question 27

**What is the cardinality of the relationship if "Each order must have exactly one customer, and each customer may have zero or more orders"?**

- [ ] A. 1:1 mandatory on both sides
- [ ] B. 1:N with mandatory participation on the order side, optional on customer side
- [ ] C. M:N with optional participation on both sides
- [ ] D. 1:N with optional participation on both sides

---

### Question 28

**Which table represents a weak entity?**

- [ ] A. `customers` with columns: `customer_id (PK)`, `name`, `email`
- [ ] B. `order_items` with columns: `order_id (PK, FK)`, `line_number (PK)`, `product_id`, `quantity`
- [ ] C. `products` with columns: `product_id (PK)`, `name`, `price`
- [ ] D. `orders` with columns: `order_id (PK)`, `customer_id (FK)`, `order_date`

---

### Question 29

**To implement a 1:1 relationship between `users` and `profiles` (each user has at most one profile), which approach ensures the constraint?**

- [ ] A. Add `profile_id` as a foreign key in `users` table (no UNIQUE constraint)
- [ ] B. Add `user_id` as a foreign key with a UNIQUE constraint in `profiles` table
- [ ] C. Create a junction table `user_profiles` with both foreign keys
- [ ] D. Store all profile data directly in the `users` table

---

### Question 30

**In a hierarchical category structure (categories can have subcategories), how should the relationship be modeled?**

- [ ] A. Create separate tables for each level: `categories`, `subcategories`, `sub_subcategories`
- [ ] B. Use a self-referencing foreign key: `parent_id` referencing `category_id` in the same table
- [ ] C. Store the hierarchy as a comma-separated path: `path = "Electronics,Phones,Smartphones"`
- [ ] D. Use a many-to-many junction table between categories

---

# Part C: Indexing Fundamentals

## True or False (Questions 31-38)

### Question 31

**PostgreSQL automatically creates an index for PRIMARY KEY constraints.**

- [ ] True
- [ ] False

---

### Question 32

**PostgreSQL automatically creates an index for FOREIGN KEY columns.**

- [ ] True
- [ ] False

---

### Question 33

**A composite index on columns (A, B, C) can efficiently support a query that filters only on column B.**

- [ ] True
- [ ] False

---

### Question 34

**Creating more indexes always improves overall database performance.**

- [ ] True
- [ ] False

---

### Question 35

**A partial index only indexes rows that match a specified condition, resulting in a smaller and faster index.**

- [ ] True
- [ ] False

---

### Question 36

**Hash indexes in PostgreSQL are suitable for range queries like `WHERE price > 100`.**

- [ ] True
- [ ] False

---

### Question 37

**An index-only scan occurs when all columns needed by a query are available in the index, allowing the database to skip reading the actual table.**

- [ ] True
- [ ] False

---

### Question 38

**The B-Tree index type is the default index type in PostgreSQL and works well for equality and range queries.**

- [ ] True
- [ ] False

---

## Multiple Choice (Questions 39-45)

### Question 39

**Which columns are the best candidates for indexing?**

- [ ] A. Columns that are rarely used in WHERE clauses
- [ ] B. Boolean columns with only TRUE/FALSE values
- [ ] C. Foreign key columns used in JOIN operations
- [ ] D. Columns that change frequently with every update

---

### Question 40

**Given a composite index `CREATE INDEX idx ON orders(customer_id, order_date)`, which query will use the index most efficiently?**

- [ ] A. `SELECT * FROM orders WHERE order_date = '2026-01-15'`
- [ ] B. `SELECT * FROM orders WHERE customer_id = 5 AND order_date > '2026-01-01'`
- [ ] C. `SELECT * FROM orders WHERE order_date BETWEEN '2026-01-01' AND '2026-01-31'`
- [ ] D. `SELECT * FROM orders ORDER BY order_date`

---

### Question 41

**What does `EXPLAIN ANALYZE` show that `EXPLAIN` alone does not?**

- [ ] A. The SQL query execution plan
- [ ] B. Actual execution time and row counts from running the query
- [ ] C. The indexes available on the table
- [ ] D. The table schema definition

---

### Question 42

**Which index type is most appropriate for full-text search on a large text column?**

- [ ] A. B-Tree
- [ ] B. Hash
- [ ] C. GIN (Generalized Inverted Index)
- [ ] D. BRIN (Block Range Index)

---

### Question 43

**Which statement about index maintenance is correct?**

- [ ] A. Indexes never need to be rebuilt or maintained after creation
- [ ] B. Indexes can become bloated after many updates/deletes and may need REINDEX
- [ ] C. Dropping and recreating an index is the only way to update index statistics
- [ ] D. Index statistics are automatically updated after every INSERT

---

### Question 44

**To create an index that allows case-insensitive email lookups, which approach is correct?**

- [ ] A. `CREATE INDEX idx_email ON users(email COLLATE "en_US.utf8")`
- [ ] B. `CREATE INDEX idx_email ON users(LOWER(email))`
- [ ] C. `CREATE UNIQUE INDEX idx_email ON users(email)`
- [ ] D. `CREATE INDEX idx_email ON users(email) WHERE email IS NOT NULL`

---

### Question 45

**When creating an index on a production table with heavy traffic, which command prevents blocking writes?**

- [ ] A. `CREATE INDEX idx ON orders(order_date)`
- [ ] B. `CREATE INDEX CONCURRENTLY idx ON orders(order_date)`
- [ ] C. `CREATE INDEX FAST idx ON orders(order_date)`
- [ ] D. `CREATE INDEX NOWAIT idx ON orders(order_date)`

---

# Bonus Section: Integrated Scenarios

## Question 46 (Bonus)

**You have the following denormalized table:**

| order_id | order_date | customer_name | customer_email | product_name | product_price | quantity |
|----------|------------|---------------|----------------|--------------|---------------|----------|
| 1 | 2026-01-15 | Alice | alice@mail.com | Laptop | 999.99 | 1 |
| 1 | 2026-01-15 | Alice | alice@mail.com | Mouse | 29.99 | 2 |
| 2 | 2026-01-16 | Bob | bob@mail.com | Laptop | 999.99 | 1 |

**After normalizing to 3NF, how many tables will you have (minimum)?**

- [ ] A. 2 tables
- [ ] B. 3 tables
- [ ] C. 4 tables
- [ ] D. 5 tables

---

## Question 47 (Bonus)

**After normalizing the table from Question 46, which indexes would you recommend creating? (Select the BEST answer)**

- [ ] A. Primary key indexes only (automatic)
- [ ] B. Primary keys + indexes on all foreign key columns
- [ ] C. Indexes on every column for maximum performance
- [ ] D. No indexes needed for normalized tables

---

## Question 48 (Bonus)

**A query joining 4 normalized tables runs slowly. The `EXPLAIN ANALYZE` shows sequential scans on all tables. What is the FIRST step to improve performance?**

- [ ] A. Denormalize all tables into one
- [ ] B. Add indexes on the foreign key columns used in JOIN conditions
- [ ] C. Rewrite the query to use subqueries instead of JOINs
- [ ] D. Increase the server's RAM

---

---

# Answer Key (Instructor Only)

## Part A: Normal Forms & Functional Dependencies

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 1 | **True** | 2NF violations require partial dependencies on composite keys. Single-column PKs can't have partial dependencies. |
| 2 | **False** | NULL values do not violate 1NF. 1NF requires atomic values and no repeating groups, not the absence of NULLs. |
| 3 | **True** | This is the definition of a partial dependency-a non-key attribute depending on only part of a composite key. |
| 4 | **True** | 3NF specifically targets transitive dependencies where non-key → non-key. |
| 5 | **False** | Achieving BCNF may require sacrificing some functional dependencies. Lossless decomposition is always possible, but dependency preservation is not guaranteed. |
| 6 | **True** | `department_name` depends on `department_id`, not directly on `employee_id`. This is a classic transitive dependency. |
| 7 | **False** | `A → B` means knowing A determines B, not the other way around. |
| 8 | **False** | Normalization can hurt query performance by requiring more JOINs. It improves data integrity, not necessarily performance. |
| 9 | **A** | The `courses` column contains multiple values (comma-separated list), violating 1NF's atomicity requirement. |
| 10 | **B** | `product_name` depends only on `product_id`, not on the full composite key `(order_id, product_id)`. This is a partial dependency violating 2NF. |
| 11 | **C** | `department_id → department_name` is a transitive dependency because `department_id` is a non-key attribute. |
| 12 | **C** | 3NF is achieved by removing transitive dependencies, creating separate tables for the dependent attributes. |
| 13 | **B** | `author_nationality` depends on `author_name`, not directly on `isbn`. This is a transitive dependency violating 3NF. |
| 14 | **C** | Denormalization is typically done to improve read performance, especially for aggregates and frequently-accessed data. It generally hurts write performance. |
| 15 | **C** | BCNF requires every determinant to be a superkey. 3NF allows exceptions when the dependent attribute is part of a candidate key (prime attribute). |

## Part B: Entity-Relationships & Schema Design

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 16 | **True** | In 1:N relationships, the FK goes in the "many" side to allow multiple rows to reference the same "one" row. |
| 17 | **True** | Relational databases cannot directly represent M:N relationships; they require a junction table with FKs to both entities. |
| 18 | **True** | This is the definition of a self-referencing relationship (e.g., employees with managers). |
| 19 | **False** | A 1:1 relationship only needs a foreign key in ONE of the tables (with a UNIQUE constraint to enforce 1:1). |
| 20 | **True** | Weak entities depend on their owner and use composite keys including the owner's key (e.g., `order_items` depending on `orders`). |
| 21 | **False** | `──○<──` represents an **optional** many relationship. The circle (○) means optional; a line (│) means mandatory. |
| 22 | **False** | Junction tables can and often do have additional attributes (e.g., `quantity` in `order_items`, `enrolled_at` in `enrollments`). |
| 23 | **C** | Students ↔ Courses is a classic M:N relationship requiring a junction table (enrollments). |
| 24 | **B** | This correctly implements 1:N with the FK in the "many" side (employees). Option A wrongly adds UNIQUE which would make it 1:1. |
| 25 | **C** | Junction tables can have additional attributes like `role_name` to describe the relationship. |
| 26 | **B** | Self-referencing FK: `manager_id` references `employees(employee_id)` in the same table. |
| 27 | **B** | Orders MUST have a customer (mandatory), but customers MAY have zero or more orders (optional on customer side). |
| 28 | **B** | `order_items` is a weak entity-it cannot exist without an order and has a composite PK including the order's FK. |
| 29 | **B** | The UNIQUE constraint on the FK ensures that each user has at most one profile (1:1 relationship). |
| 30 | **B** | Self-referencing FK (`parent_id`) is the standard way to model hierarchical data in a single table. |

## Part C: Indexing Fundamentals

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 31 | **True** | PostgreSQL automatically creates a unique index for PRIMARY KEY constraints. |
| 32 | **False** | PostgreSQL does NOT automatically index foreign key columns. You should create them manually for JOIN performance. |
| 33 | **False** | Composite index (A, B, C) efficiently supports queries on A, (A, B), or (A, B, C), but NOT on B alone or C alone. |
| 34 | **False** | Indexes speed up reads but slow down writes (INSERT, UPDATE, DELETE). Too many indexes hurt write performance. |
| 35 | **True** | Partial indexes (with WHERE clause) only index matching rows, reducing size and improving performance for filtered queries. |
| 36 | **False** | Hash indexes only support equality comparisons (=). B-Tree indexes are needed for range queries. |
| 37 | **True** | Index-only scans are possible when all required columns are in the index, avoiding table access (heap fetch). |
| 38 | **True** | B-Tree is the default and most versatile index type in PostgreSQL, supporting =, <, >, BETWEEN, and ORDER BY. |
| 39 | **C** | Foreign keys used in JOINs are excellent index candidates-high selectivity and frequently used in queries. |
| 40 | **B** | The composite index (customer_id, order_date) is most efficient when querying on customer_id first, then order_date. |
| 41 | **B** | EXPLAIN shows the plan; EXPLAIN ANALYZE actually runs the query and reports real execution times and row counts. |
| 42 | **C** | GIN (Generalized Inverted Index) is designed for full-text search and array/JSONB containment queries. |
| 43 | **B** | Indexes can become bloated after many DML operations. REINDEX or VACUUM can help reclaim space. |
| 44 | **B** | Expression index on `LOWER(email)` allows efficient case-insensitive lookups with `WHERE LOWER(email) = '...'`. |
| 45 | **B** | `CREATE INDEX CONCURRENTLY` builds the index without blocking concurrent writes (though it takes longer). |

## Bonus Section

| Q | Answer | Explanation |
|:-:|:------:|-------------|
| 46 | **C** | Minimum 4 tables: `customers`, `products`, `orders`, `order_items` (junction table for orders-products). |
| 47 | **B** | Primary keys get automatic indexes. You should also add indexes on foreign keys (`customer_id`, `product_id` in relevant tables). |
| 48 | **B** | First step is always to add indexes on JOIN columns. Denormalization is a last resort after indexing is optimized. |
