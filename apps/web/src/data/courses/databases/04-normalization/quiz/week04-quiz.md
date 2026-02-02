# Week 04 Quiz: Normalization, Entity-Relationships & Indexing

## Instructions

This quiz covers the core concepts from Week 04:

- Normal Forms and Functional Dependencies
- Entity-Relationship Design
- Indexing Fundamentals

**Total: 20 questions**

Time estimate: 20-30 minutes

---

## Question 1

**A relation with a single-attribute primary key that satisfies 1NF will also satisfy 2NF.**

- [x] True
- [ ] False

<!--
EXPLANATION: Second Normal Form violations occur when there are partial dependencies - meaning a non-key attribute depends on only part of a composite key. With a single-attribute primary key, there's no way to have a "part" of the key, so partial dependencies are impossible. Therefore, any 1NF table with a single-column PK automatically satisfies 2NF.
-->

---

## Question 2

**A table that contains NULL values in some columns violates First Normal Form.**

- [ ] True
- [x] False

<!--
EXPLANATION: First Normal Form requires that all attribute values be atomic (no repeating groups or multi-valued attributes). NULL values are perfectly acceptable in 1NF - they simply represent missing or unknown data. The confusion often arises because some textbooks mention "no missing values" as a 1NF requirement, but the formal definition only prohibits non-atomic values.
-->

---

## Question 3

**Consider a table with composite primary key (X, Y). If attribute Z depends only on X, this represents a partial dependency.**

- [x] True
- [ ] False

<!--
EXPLANATION: A partial dependency occurs when a non-key attribute depends on only a portion of a composite primary key rather than the entire key. Since Z depends only on X (not on the full key X,Y), this is the textbook definition of a partial dependency, which violates Second Normal Form.
-->

---

## Question 4

**Normalizing a database to higher normal forms always results in better query performance.**

- [ ] True
- [x] False

<!--
EXPLANATION: Normalization improves data integrity and reduces redundancy, but it can actually hurt query performance. Highly normalized databases require more JOIN operations to retrieve related data, which can be slower than reading from a single denormalized table. This is why strategic denormalization is sometimes used in read-heavy systems.
-->

---

## Question 5

**What violation does this table exhibit?**

| employee_id | full_name  | skills            |
| ----------- | ---------- | ----------------- |
| 101         | John Smith | Python, SQL, Java |
| 102         | Jane Doe   | JavaScript, React |

- [x] A. Violates 1NF due to non-atomic values
- [ ] B. Violates 2NF due to partial dependency
- [ ] C. Violates 3NF due to transitive dependency
- [ ] D. The table is properly normalized

<!--
EXPLANATION: The `skills` column contains multiple values in a single cell (comma-separated list). First Normal Form requires all attributes to be atomic - each cell should contain exactly one value. The correct design would use a separate `employee_skills` junction table.
-->

---

## Question 6

**Given this table with composite primary key (invoice_id, item_id):**

| invoice_id | item_id | item_description | qty |
| ---------- | ------- | ---------------- | --- |
| 1001       | 55      | Keyboard         | 3   |
| 1002       | 55      | Keyboard         | 1   |

**Which statement accurately describes this table?**

- [ ] A. Violates 1NF because item_description appears multiple times
- [x] B. Violates 2NF because item_description depends only on item_id
- [ ] C. Violates 3NF due to transitive dependency
- [ ] D. The table meets all requirements for 3NF

<!--
EXPLANATION: The attribute `item_description` is functionally determined by `item_id` alone, not by the full composite key (invoice_id, item_id). This is a partial dependency - you only need to know the item_id to determine its description. Partial dependencies violate Second Normal Form.
-->

---

## Question 7

**In a table where `student_id` is the primary key, which dependency represents a transitive dependency?**

- [ ] A. student_id → student_name
- [ ] B. student_id → advisor_id
- [x] C. advisor_id → advisor_email
- [ ] D. student_id → (student_name, advisor_id)

<!--
EXPLANATION: A transitive dependency occurs when a non-key attribute determines another non-key attribute. Here, `advisor_id` is a non-key attribute (it depends on student_id), and `advisor_email` depends on `advisor_id`. This chain (student_id → advisor_id → advisor_email) creates a transitive dependency that violates Third Normal Form.
-->

---

## Question 8

**What is the key distinction between BCNF and 3NF?**

- [ ] A. BCNF requires all values to be atomic while 3NF does not
- [ ] B. BCNF addresses partial dependencies while 3NF addresses transitive dependencies
- [x] C. In BCNF, every determinant must be a superkey; 3NF permits exceptions when the dependent attribute is prime
- [ ] D. BCNF and 3NF are equivalent in all cases

<!--
EXPLANATION: Both 3NF and BCNF address similar issues, but BCNF is stricter. In 3NF, a non-trivial functional dependency X → A is allowed if A is a prime attribute (part of some candidate key), even if X is not a superkey. BCNF removes this exception - every determinant must be a superkey, no exceptions.
-->

---

## Question 9

**When implementing a one-to-many (1:N) relationship, the foreign key should be placed in the table representing the "one" side.**

- [ ] True
- [x] False

<!--
EXPLANATION: In a 1:N relationship, the foreign key belongs in the "many" side table. This allows multiple rows in the "many" table to reference the same row in the "one" table. Placing the FK in the "one" side would require storing multiple values (violating 1NF) or creating redundant rows.
-->

---

## Question 10

**A bridge table (junction table) is necessary to represent a many-to-many relationship in a relational database.**

- [x] True
- [ ] False

<!--
EXPLANATION: Relational databases cannot directly express M:N relationships between two tables. You need an intermediate junction/bridge table that contains foreign keys referencing both related tables. Each row in the junction table represents one instance of the relationship between the two entities.
-->

---

## Question 11

**In a one-to-one (1:1) relationship, foreign keys must exist in both participating tables.**

- [ ] True
- [x] False

<!--
EXPLANATION: A 1:1 relationship only requires a foreign key in ONE of the tables, combined with a UNIQUE constraint to enforce that each referenced row has at most one referencing row. Placing FKs in both tables would be redundant and could create circular dependency issues.
-->

---

## Question 12

**In Crow's Foot notation, the symbol `──○<──` indicates a mandatory relationship on the "many" side.**

- [ ] True
- [x] False

<!--
EXPLANATION: In Crow's Foot notation, the circle (○) represents optional participation (zero allowed), while a vertical bar (|) represents mandatory participation (at least one required). The crow's foot (<) indicates "many". So `──○<──` means "optional many" (zero or more), not mandatory.
-->

---

## Question 13

**What type of relationship exists between Authors and Books if an author can write multiple books and a book can have multiple authors?**

- [ ] A. One-to-One (1:1)
- [ ] B. One-to-Many (1:N)
- [x] C. Many-to-Many (M:N)
- [ ] D. Recursive relationship

<!--
EXPLANATION: Since each author can be associated with multiple books AND each book can be associated with multiple authors, this is a classic Many-to-Many (M:N) relationship. Implementation requires a junction table (like `book_authors`) containing foreign keys to both tables.
-->

---

## Question 14

**An employee management system requires: "Every employee works in exactly one department, and departments can have many employees." Which implementation is correct?**

- [ ] A.

```sql
CREATE TABLE staff (
    staff_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT UNIQUE REFERENCES departments(dept_id)
);
```

- [x] B.

```sql
CREATE TABLE staff (
    staff_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    dept_id INT REFERENCES departments(dept_id)
);
```

- [ ] C.

```sql
CREATE TABLE departments (
    dept_id SERIAL PRIMARY KEY,
    name VARCHAR(100),
    staff_id INT REFERENCES staff(staff_id)
);
```

- [ ] D.

```sql
CREATE TABLE staff_departments (
    staff_id INT REFERENCES staff(staff_id),
    dept_id INT REFERENCES departments(dept_id),
    PRIMARY KEY (staff_id, dept_id)
);
```

<!--
EXPLANATION: This is a 1:N relationship where the FK goes in the "many" side (staff table). The first option incorrectly adds UNIQUE, which would convert it to 1:1. The third option puts the FK on the wrong side. The fourth option creates a junction table, which is for M:N relationships.
-->

---

## Question 15

**Which table structure represents a weak entity?**

- [ ] A. `clients` with: `client_id (PK)`, `name`, `phone`
- [x] B. `invoice_lines` with: `invoice_id (PK, FK)`, `line_num (PK)`, `product_id`, `amount`
- [ ] C. `inventory` with: `product_id (PK)`, `name`, `stock_qty`
- [ ] D. `purchases` with: `purchase_id (PK)`, `client_id (FK)`, `purchase_date`

<!--
EXPLANATION: A weak entity cannot be uniquely identified by its own attributes alone - it depends on a parent (owner) entity. The `invoice_lines` table has a composite PK that includes the parent's key (`invoice_id`), meaning a line item cannot exist or be identified without its parent invoice. This is the defining characteristic of a weak entity.
-->

---

## Question 16

**PostgreSQL automatically creates an index when you define a FOREIGN KEY constraint.**

- [ ] True
- [x] False

<!--
EXPLANATION: PostgreSQL automatically creates indexes for PRIMARY KEY and UNIQUE constraints, but NOT for FOREIGN KEY constraints. This is a common source of performance issues - developers should manually create indexes on FK columns, especially when those columns are used in JOIN operations.
-->

---

## Question 17

**A composite index on columns (X, Y, Z) can efficiently support queries that filter only on column Y.**

- [ ] True
- [x] False

<!--
EXPLANATION: Composite indexes follow a leftmost prefix rule. An index on (X, Y, Z) efficiently supports queries filtering on X, (X, Y), or (X, Y, Z). It cannot efficiently support queries that skip the leftmost columns - filtering only on Y or Z would require a full index scan rather than an index seek.
-->

---

## Question 18

**Adding more indexes to a table will always improve overall database performance.**

- [ ] True
- [x] False

<!--
EXPLANATION: While indexes speed up read operations (SELECT), they slow down write operations (INSERT, UPDATE, DELETE) because the database must maintain each index. Too many indexes can significantly degrade write performance and consume storage space. Index strategy requires balancing read vs. write workloads.
-->

---

## Question 19

**Given a composite index `CREATE INDEX idx ON sales(region_id, sale_date)`, which query benefits most from this index?**

- [ ] A. `SELECT * FROM sales WHERE sale_date = '2026-01-20'`
- [x] B. `SELECT * FROM sales WHERE region_id = 10 AND sale_date > '2026-01-01'`
- [ ] C. `SELECT * FROM sales WHERE sale_date BETWEEN '2026-01-01' AND '2026-01-31'`
- [ ] D. `SELECT * FROM sales ORDER BY sale_date DESC`

<!--
EXPLANATION: The composite index (region_id, sale_date) is most effective when queries filter on region_id first (the leftmost column), optionally followed by sale_date. The second option filters on both columns in the correct order. The other options filter or sort only on sale_date, which is not the leading column.
-->

---

## Question 20

**When building an index on a heavily-used production table, which command avoids blocking concurrent write operations?**

- [ ] A. `CREATE INDEX idx ON transactions(created_at)`
- [x] B. `CREATE INDEX CONCURRENTLY idx ON transactions(created_at)`
- [ ] C. `CREATE INDEX PARALLEL idx ON transactions(created_at)`
- [ ] D. `CREATE INDEX ASYNC idx ON transactions(created_at)`

<!--
EXPLANATION: Standard CREATE INDEX acquires an exclusive lock that blocks writes until complete. CREATE INDEX CONCURRENTLY builds the index in the background, allowing concurrent INSERT, UPDATE, and DELETE operations. This takes longer but avoids downtime on production systems.
-->
