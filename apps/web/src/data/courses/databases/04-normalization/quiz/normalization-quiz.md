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

---

!!! quiz
{
"title": "Question 1: Single-Column PK and 2NF",
"question": "A table with a single-column primary key that is in 1NF is automatically in 2NF.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 2: NULL and 1NF",
"question": "If a column contains NULL values, the table violates First Normal Form (1NF).",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 3: Partial Dependency",
"question": "In a table with composite primary key (A, B), if column C depends only on A, this is a partial dependency and violates 2NF.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 4: 3NF and Transitive Dependencies",
"question": "Third Normal Form (3NF) eliminates all transitive dependencies where a non-key attribute determines another non-key attribute.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 5: BCNF Achievability",
"question": "BCNF (Boyce-Codd Normal Form) is always achievable without losing information or dependencies.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 6: Employee-Department 3NF Violation",
"question": "A table storing employee_id, employee_name, department_id, department_name violates 3NF because department_name depends on department_id, not directly on the primary key.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 7: Functional Dependency Direction",
"question": "The functional dependency A → B means that knowing the value of B allows you to determine the value of A.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 8: Normalization and Performance",
"question": "Normalization always improves query performance by reducing data redundancy.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

## Multiple Choice (Questions 9-15)

---

Consider this table:

| student_id | name  | courses                  |
| ---------- | ----- | ------------------------ |
| 1          | Alice | Math, Physics, Chemistry |
| 2          | Bob   | History, English         |

!!! quiz
{
"title": "Question 9: Normal Form Violation",
"question": "Which normal form violation does this table have?",
"options": ["Violates 1NF - multi-valued attribute", "Violates 2NF - partial dependency", "Violates 3NF - transitive dependency", "No violation - table is in 3NF"],
"answers": ["Violates 1NF - multi-valued attribute"]
}
!!!

---

Consider this table with primary key (order_id, product_id):

| order_id | product_id | product_name | quantity |
| -------- | ---------- | ------------ | -------- |
| 1        | 101        | Laptop       | 2        |
| 2        | 101        | Laptop       | 1        |

!!! quiz
{
"title": "Question 10: Composite Key Violation",
"question": "Which statement is correct about this table?",
"options": ["Violates 1NF because product_name is repeated", "Violates 2NF because product_name depends only on product_id", "Violates 3NF because of a transitive dependency", "The table is properly normalized to 3NF"],
"answers": ["Violates 2NF because product_name depends only on product_id"]
}
!!!

---

!!! quiz
{
"title": "Question 11: Transitive Dependency Identification",
"question": "Which functional dependency represents a transitive dependency in a table where employee_id is the primary key?",
"options": ["employee_id → employee_name", "employee_id → department_id", "department_id → department_name", "employee_id → (employee_name, department_id)"],
"answers": ["department_id → department_name"]
}
!!!

---

!!! quiz
{
"title": "Question 12: Converting 2NF to 3NF",
"question": "To convert a 2NF table to 3NF, you must:",
"options": ["Remove all multi-valued attributes", "Ensure every non-key attribute is fully dependent on the entire primary key", "Remove transitive dependencies by creating separate tables", "Ensure every determinant is a candidate key"],
"answers": ["Remove transitive dependencies by creating separate tables"]
}
!!!

---

!!! quiz
{
"title": "Question 13: Book-Author Table Analysis",
"question": "A table has columns: isbn, title, author_name, author_nationality. The primary key is isbn, and each book has one author. Which statement is correct?",
"options": ["The table is in 3NF because all attributes depend on the primary key", "The table violates 3NF because author_nationality depends on author_name", "The table violates 2NF because of partial dependencies", "The table violates 1NF because author information is repeated"],
"answers": ["The table violates 3NF because author_nationality depends on author_name"]
}
!!!

---

!!! quiz
{
"title": "Question 14: Denormalization Reasons",
"question": "Which of the following is a valid reason to intentionally denormalize a database?",
"options": ["To eliminate the need for foreign keys", "To improve write performance on INSERT operations", "To improve read performance for frequently-accessed aggregate data", "To enforce referential integrity more easily"],
"answers": ["To improve read performance for frequently-accessed aggregate data"]
}
!!!

---

!!! quiz
{
"title": "Question 15: BCNF vs 3NF",
"question": "What is the primary difference between BCNF and 3NF?",
"options": ["BCNF requires atomic values; 3NF does not", "BCNF eliminates partial dependencies; 3NF eliminates transitive dependencies", "BCNF requires every determinant to be a superkey; 3NF allows non-superkey determinants if the dependent is a prime attribute", "There is no difference; BCNF and 3NF are equivalent"],
"answers": ["BCNF requires every determinant to be a superkey; 3NF allows non-superkey determinants if the dependent is a prime attribute"]
}
!!!

---

# Part B: Entity-Relationships & Schema Design

## True or False (Questions 16-22)

---

!!! quiz
{
"title": "Question 16: Foreign Key Placement in 1:N",
"question": "In a 1:N (one-to-many) relationship, the foreign key should be placed in the table on the 'many' side.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 17: Junction Tables for M:N",
"question": "A junction table (bridge table) is required to implement a many-to-many (M:N) relationship in a relational database.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 18: Self-Referencing Relationship",
"question": "A self-referencing relationship occurs when a table has a foreign key that references its own primary key.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 19: 1:1 Foreign Key Placement",
"question": "In a 1:1 relationship, the foreign key must be placed in both tables to enforce the relationship.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 20: Weak Entity Definition",
"question": "A weak entity cannot exist without its identifying (owner) entity and typically uses a composite primary key that includes the owner's key.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 21: Crow's Foot Notation",
"question": "In Crow's Foot notation, the symbol ──○<── represents a mandatory many relationship.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 22: Junction Table Attributes",
"question": "A junction table can only contain the foreign keys from the two related tables and cannot have additional attributes.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

## Multiple Choice (Questions 23-30)

---

!!! quiz
{
"title": "Question 23: Student-Course Relationship",
"question": "Which relationship type exists between Students and Courses if each student can enroll in multiple courses and each course can have multiple students?",
"options": ["One-to-One (1:1)", "One-to-Many (1:N)", "Many-to-Many (M:N)", "Self-referencing"],
"answers": ["Many-to-Many (M:N)"]
}
!!!

---

**Requirement:** "Each employee belongs to exactly one department, and a department can have many employees."

Option A:

```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    department_id INT UNIQUE REFERENCES departments(department_id)
);
```

Option B:

```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    department_id INT REFERENCES departments(department_id)
);
```

Option C:

```sql
CREATE TABLE departments (
    department_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    employee_id INT REFERENCES employees(employee_id)
);
```

Option D:

```sql
CREATE TABLE employee_departments (
    employee_id INT REFERENCES employees(employee_id),
    department_id INT REFERENCES departments(department_id),
    PRIMARY KEY (employee_id, department_id)
);
```

!!! quiz
{
"title": "Question 24: Employee-Department Implementation",
"question": "Which SQL correctly implements this requirement?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

!!! quiz
{
"title": "Question 25: M:N with Additional Attributes",
"question": "How would you implement a many-to-many relationship between movies and actors where you also need to store the role_name each actor played?",
"options": ["Add a role_name column to the movies table", "Add a role_name column to the actors table", "Create a junction table with movie_id, actor_id, and role_name", "Store role_name as a comma-separated list in either table"],
"answers": ["Create a junction table with movie_id, actor_id, and role_name"]
}
!!!

---

**Requirement:** Employees have managers (who are also employees).

Option A:

```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_id INT REFERENCES managers(manager_id)
);
```

Option B:

```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_id INT REFERENCES employees(employee_id)
);
```

Option C:

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

Option D:

```sql
CREATE TABLE employees (
    employee_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    manager_name VARCHAR(100)
);
```

!!! quiz
{
"title": "Question 26: Self-Referencing Implementation",
"question": "Which implementation is correct for a self-referencing relationship?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

!!! quiz
{
"title": "Question 27: Order-Customer Cardinality",
"question": "What is the cardinality of the relationship if 'Each order must have exactly one customer, and each customer may have zero or more orders'?",
"options": ["1:1 mandatory on both sides", "1:N with mandatory participation on the order side, optional on customer side", "M:N with optional participation on both sides", "1:N with optional participation on both sides"],
"answers": ["1:N with mandatory participation on the order side, optional on customer side"]
}
!!!

---

!!! quiz
{
"title": "Question 28: Weak Entity Identification",
"question": "Which table represents a weak entity?",
"options": ["customers with columns: customer_id (PK), name, email", "order_items with columns: order_id (PK, FK), line_number (PK), product_id, quantity", "products with columns: product_id (PK), name, price", "orders with columns: order_id (PK), customer_id (FK), order_date"],
"answers": ["order_items with columns: order_id (PK, FK), line_number (PK), product_id, quantity"]
}
!!!

---

!!! quiz
{
"title": "Question 29: 1:1 Implementation",
"question": "To implement a 1:1 relationship between users and profiles (each user has at most one profile), which approach ensures the constraint?",
"options": ["Add profile_id as a foreign key in users table (no UNIQUE constraint)", "Add user_id as a foreign key with a UNIQUE constraint in profiles table", "Create a junction table user_profiles with both foreign keys", "Store all profile data directly in the users table"],
"answers": ["Add user_id as a foreign key with a UNIQUE constraint in profiles table"]
}
!!!

---

!!! quiz
{
"title": "Question 30: Hierarchical Category Structure",
"question": "In a hierarchical category structure (categories can have subcategories), how should the relationship be modeled?",
"options": ["Create separate tables for each level: categories, subcategories, sub_subcategories", "Use a self-referencing foreign key: parent_id referencing category_id in the same table", "Store the hierarchy as a comma-separated path: path = 'Electronics,Phones,Smartphones'", "Use a many-to-many junction table between categories"],
"answers": ["Use a self-referencing foreign key: parent_id referencing category_id in the same table"]
}
!!!

---

# Part C: Indexing Fundamentals

## True or False (Questions 31-38)

---

!!! quiz
{
"title": "Question 31: Primary Key Index",
"question": "PostgreSQL automatically creates an index for PRIMARY KEY constraints.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 32: Foreign Key Index",
"question": "PostgreSQL automatically creates an index for FOREIGN KEY columns.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 33: Composite Index Usage",
"question": "A composite index on columns (A, B, C) can efficiently support a query that filters only on column B.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 34: More Indexes Better Performance",
"question": "Creating more indexes always improves overall database performance.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 35: Partial Index",
"question": "A partial index only indexes rows that match a specified condition, resulting in a smaller and faster index.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 36: Hash Index Range Queries",
"question": "Hash indexes in PostgreSQL are suitable for range queries like WHERE price > 100.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 37: Index-Only Scan",
"question": "An index-only scan occurs when all columns needed by a query are available in the index, allowing the database to skip reading the actual table.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 38: B-Tree Default",
"question": "The B-Tree index type is the default index type in PostgreSQL and works well for equality and range queries.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

## Multiple Choice (Questions 39-45)

---

!!! quiz
{
"title": "Question 39: Index Candidates",
"question": "Which columns are the best candidates for indexing?",
"options": ["Columns that are rarely used in WHERE clauses", "Boolean columns with only TRUE/FALSE values", "Foreign key columns used in JOIN operations", "Columns that change frequently with every update"],
"answers": ["Foreign key columns used in JOIN operations"]
}
!!!

---

Consider this composite index:

```sql
CREATE INDEX idx ON orders(customer_id, order_date)
```

!!! quiz
{
"title": "Question 40: Composite Index Efficiency",
"question": "Which query will use this index most efficiently?",
"options": ["SELECT * FROM orders WHERE order_date = '2026-01-15'", "SELECT * FROM orders WHERE customer_id = 5 AND order_date > '2026-01-01'", "SELECT * FROM orders WHERE order_date BETWEEN '2026-01-01' AND '2026-01-31'", "SELECT * FROM orders ORDER BY order_date"],
"answers": ["SELECT * FROM orders WHERE customer_id = 5 AND order_date > '2026-01-01'"]
}
!!!

---

!!! quiz
{
"title": "Question 41: EXPLAIN vs EXPLAIN ANALYZE",
"question": "What does EXPLAIN ANALYZE show that EXPLAIN alone does not?",
"options": ["The SQL query execution plan", "Actual execution time and row counts from running the query", "The indexes available on the table", "The table schema definition"],
"answers": ["Actual execution time and row counts from running the query"]
}
!!!

---

!!! quiz
{
"title": "Question 42: Full-Text Search Index",
"question": "Which index type is most appropriate for full-text search on a large text column?",
"options": ["B-Tree", "Hash", "GIN (Generalized Inverted Index)", "BRIN (Block Range Index)"],
"answers": ["GIN (Generalized Inverted Index)"]
}
!!!

---

!!! quiz
{
"title": "Question 43: Index Maintenance",
"question": "Which statement about index maintenance is correct?",
"options": ["Indexes never need to be rebuilt or maintained after creation", "Indexes can become bloated after many updates/deletes and may need REINDEX", "Dropping and recreating an index is the only way to update index statistics", "Index statistics are automatically updated after every INSERT"],
"answers": ["Indexes can become bloated after many updates/deletes and may need REINDEX"]
}
!!!

---

!!! quiz
{
"title": "Question 44: Case-Insensitive Index",
"question": "To create an index that allows case-insensitive email lookups, which approach is correct?",
"options": ["CREATE INDEX idx_email ON users(email COLLATE 'en_US.utf8')", "CREATE INDEX idx_email ON users(LOWER(email))", "CREATE UNIQUE INDEX idx_email ON users(email)", "CREATE INDEX idx_email ON users(email) WHERE email IS NOT NULL"],
"answers": ["CREATE INDEX idx_email ON users(LOWER(email))"]
}
!!!

---

!!! quiz
{
"title": "Question 45: Non-Blocking Index Creation",
"question": "When creating an index on a production table with heavy traffic, which command prevents blocking writes?",
"options": ["CREATE INDEX idx ON orders(order_date)", "CREATE INDEX CONCURRENTLY idx ON orders(order_date)", "CREATE INDEX FAST idx ON orders(order_date)", "CREATE INDEX NOWAIT idx ON orders(order_date)"],
"answers": ["CREATE INDEX CONCURRENTLY idx ON orders(order_date)"]
}
!!!

---

# Bonus Section: Integrated Scenarios

---

Consider this denormalized table:

| order_id | order_date | customer_name | customer_email | product_name | product_price | quantity |
| -------- | ---------- | ------------- | -------------- | ------------ | ------------- | -------- |
| 1        | 2026-01-15 | Alice         | alice@mail.com | Laptop       | 999.99        | 1        |
| 1        | 2026-01-15 | Alice         | alice@mail.com | Mouse        | 29.99         | 2        |
| 2        | 2026-01-16 | Bob           | bob@mail.com   | Laptop       | 999.99        | 1        |

!!! quiz
{
"title": "Question 46 (Bonus): Normalization Table Count",
"question": "After normalizing this table to 3NF, how many tables will you have (minimum)?",
"options": ["2 tables", "3 tables", "4 tables", "5 tables"],
"answers": ["4 tables"]
}
!!!

---

!!! quiz
{
"title": "Question 47 (Bonus): Recommended Indexes",
"question": "After normalizing the table from Question 46, which indexes would you recommend creating? (Select the BEST answer)",
"options": ["Primary key indexes only (automatic)", "Primary keys + indexes on all foreign key columns", "Indexes on every column for maximum performance", "No indexes needed for normalized tables"],
"answers": ["Primary keys + indexes on all foreign key columns"]
}
!!!

---

!!! quiz
{
"title": "Question 48 (Bonus): Slow JOIN Query",
"question": "A query joining 4 normalized tables runs slowly. The EXPLAIN ANALYZE shows sequential scans on all tables. What is the FIRST step to improve performance?",
"options": ["Denormalize all tables into one", "Add indexes on the foreign key columns used in JOIN conditions", "Rewrite the query to use subqueries instead of JOINs", "Increase the server's RAM"],
"answers": ["Add indexes on the foreign key columns used in JOIN conditions"]
}
!!!

---

**Requirement:** The database administrator needs to:

1. Add a unique constraint on the `email` column in the `users` table
2. Create an index on the `last_login` column for faster queries
3. Rename the column `user_name` to `username`

Option A:

```sql
ALTER TABLE users ADD CONSTRAINT unique_email UNIQUE (email);
CREATE INDEX idx_users_last_login ON users (last_login);
ALTER TABLE users RENAME COLUMN user_name TO username;
```

Option B:

```sql
UPDATE users SET email = UNIQUE(email);
INSERT INDEX idx_users_last_login ON users (last_login);
UPDATE users SET user_name = 'username';
```

Option C:

```sql
CREATE UNIQUE email ON users;
CREATE INDEX last_login ON users;
RENAME user_name TO username IN users;
```

Option D:

```sql
ALTER TABLE users ADD UNIQUE (email);
ALTER TABLE users ADD INDEX (last_login);
ALTER TABLE users CHANGE user_name username;
```

!!! quiz
{
"title": "Question 49 (Bonus): Creating Indexes",
"question": "Which SQL statements correctly implement ALL requirements?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!
