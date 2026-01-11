# Quiz: Constraints and Data Types

## Instructions

This quiz covers SQL **constraints** and **data types**. Questions are a mix of **multiple choice** and **true/false**.

---

## PART A: True or False

For each statement, mark **True** or **False**.

---

### Question 1

**A table can have multiple columns defined as `PRIMARY KEY` as long as they are in different statements.**

- [ ] True
- [ ] False

---

### Question 2

**A `UNIQUE` constraint allows multiple `NULL` values in PostgreSQL.**

- [ ] True
- [ ] False

---

### Question 3

**`VARCHAR(100)` and `CHAR(100)` both store exactly 100 characters for every value inserted.**

- [ ] True
- [ ] False

---

### Question 4

**A `FOREIGN KEY` constraint automatically creates an index on the referencing column.**

- [ ] True
- [ ] False

---

### Question 5

**`DECIMAL(5, 2)` can store the value `999.99` but not `1000.00`.**

- [ ] True
- [ ] False

---

### Question 6

**A column with a `DEFAULT` value cannot also have a `NOT NULL` constraint.**

- [ ] True
- [ ] False

---

### Question 7

**`SERIAL` is a true data type in PostgreSQL that stores auto-incrementing values.**

- [ ] True
- [ ] False

---

### Question 8

**A `CHECK` constraint can reference columns from other tables.**

- [ ] True
- [ ] False

---

### Question 9

**`TIMESTAMP WITH TIME ZONE` stores the timezone information alongside the timestamp.**

- [ ] True
- [ ] False

---

### Question 10

**`TEXT` and `VARCHAR` without a length limit have identical performance characteristics in PostgreSQL.**

- [ ] True
- [ ] False

---

## PART B: Multiple Choice

Select the best answer for each question.

---

### Question 11

**Which data type is most appropriate for storing a user's account balance in a financial application?**

- [ ] A. `FLOAT`
- [ ] B. `REAL`
- [ ] C. `DECIMAL(12, 2)`
- [ ] D. `INTEGER`

---

### Question 12

**Given this table definition:**

```sql
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INT NOT NULL REFERENCES customers(id),
    total DECIMAL(10, 2) CHECK (total >= 0),
    status VARCHAR(20) DEFAULT 'pending'
);
```

**Which statement about the constraints is INCORRECT?**

- [ ] A. `id` will auto-increment and must be unique
- [ ] B. `customer_id` must exist in the `customers` table
- [ ] C. `total` can be `NULL` as long as it's not negative
- [ ] D. `status` will be `NULL` if not provided during insert

---

### Question 13

**What happens when you try to insert a string 'Hello World' into a `CHAR(5)` column?**

- [ ] A. It stores 'Hello' (truncated to 5 characters)
- [ ] B. It raises an error because the value is too long
- [ ] C. It stores 'Hello World' and ignores the length limit
- [ ] D. It stores 'Hello' padded with spaces

---

### Question 14

**Which constraint combination ensures that an email column contains unique, non-empty values?**

- [ ] A. `email VARCHAR(255) PRIMARY KEY`
- [ ] B. `email VARCHAR(255) UNIQUE`
- [ ] C. `email VARCHAR(255) UNIQUE NOT NULL`
- [ ] D. `email VARCHAR(255) CHECK (email IS NOT NULL)`

---

### Question 15

**Which of the following is a valid way to define a foreign key relationship when creating a table?**

- [ ] A.

```sql
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INT,
    FOREIGN KEY customer_id REFERENCES customers(id)
);
```

- [ ] B.

```sql
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INT REFERENCES customers(id)
);
```

- [ ] C.

```sql
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INT,
    LINK customer_id TO customers(id)
);
```

- [ ] D.

```sql
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    customer_id INT FOREIGN customers(id)
);
```

---

### Question 16

**Which data type should you use to store a PostgreSQL-generated unique identifier that is safe for distributed systems?**

- [ ] A. `SERIAL`
- [ ] B. `BIGSERIAL`
- [ ] C. `UUID`
- [ ] D. `INT`

---

### Question 17

**Which statement about `BOOLEAN` data type in PostgreSQL is correct?**

- [ ] A. It can only store `TRUE` or `FALSE`, not `NULL`
- [ ] B. It accepts values like `'yes'`, `'no'`, `'1'`, `'0'`, `'t'`, `'f'` as valid boolean inputs
- [ ] C. It is stored as a string internally
- [ ] D. PostgreSQL does not support a native `BOOLEAN` type

---

### Question 18

**Which statement correctly defines a composite primary key?**

- [ ] A.

```sql
CREATE TABLE order_items (
    order_id INT PRIMARY KEY,
    product_id INT PRIMARY KEY,
    quantity INT
);
```

- [ ] B.

```sql
CREATE TABLE order_items (
    order_id INT,
    product_id INT,
    quantity INT,
    PRIMARY KEY (order_id, product_id)
);
```

- [ ] C.

```sql
CREATE TABLE order_items (
    order_id INT UNIQUE,
    product_id INT UNIQUE,
    quantity INT
);
```

- [ ] D.

```sql
CREATE TABLE order_items (
    order_id INT NOT NULL,
    product_id INT NOT NULL,
    quantity INT,
    UNIQUE (order_id, product_id)
);
```

---

### Question 19

**What is the difference between `SMALLINT`, `INT`, and `BIGINT`?**

- [ ] A. They store the same range but with different performance characteristics
- [ ] B. `SMALLINT` uses 2 bytes, `INT` uses 4 bytes, `BIGINT` uses 8 bytes, each with increasing value ranges
- [ ] C. They are aliases for the same underlying data type
- [ ] D. `BIGINT` is for decimal numbers, `INT` and `SMALLINT` are for whole numbers only

---

### Question 20

**You need to store timestamps that will be used across multiple timezones. Users in New York and Tokyo should see the correct local time. Which data type should you use?**

- [ ] A. `TIMESTAMP`
- [ ] B. `TIMESTAMP WITH TIME ZONE`
- [ ] C. `DATE`
- [ ] D. `TIME`

---

### Question 21

**What is the maximum value that can be stored in an `INT` (4-byte signed integer)?**

- [ ] A. 32,767
- [ ] B. 2,147,483,647
- [ ] C. 9,223,372,036,854,775,807
- [ ] D. 4,294,967,295

---

### Question 22

**Which statement about `NOT NULL` constraints is correct?**

- [ ] A. `NOT NULL` is implied for `PRIMARY KEY` columns
- [ ] B. `NOT NULL` cannot be added to an existing column with `ALTER TABLE`
- [ ] C. `NOT NULL` prevents empty strings (`''`) from being inserted
- [ ] D. `NOT NULL` is automatically applied to all `FOREIGN KEY` columns

---

### Question 23

**You want to ensure that a `discount_percent` column only accepts values between 0 and 100. Which constraint should you use?**

- [ ] A. `discount_percent INT UNIQUE`
- [ ] B. `discount_percent INT NOT NULL`
- [ ] C. `discount_percent INT CHECK (discount_percent >= 0 AND discount_percent <= 100)`
- [ ] D. `discount_percent INT DEFAULT 0`

---

### Question 24

**Given this table, which INSERT statement will succeed?**

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) NOT NULL,
    age INT CHECK (age >= 18),
    role VARCHAR(20) DEFAULT 'user'
);
```

- [ ] A. `INSERT INTO users (username, email) VALUES ('alice', 'alice@test.com');`
- [ ] B. `INSERT INTO users (username, email, age) VALUES ('bob', 'bob@test.com', 16);`
- [ ] C. `INSERT INTO users (email, age) VALUES ('charlie@test.com', 25);`
- [ ] D. `INSERT INTO users (username, email, age) VALUES (NULL, 'dan@test.com', 30);`

---

### Question 25

**Which data type combination is most appropriate for a table storing geographic coordinates (latitude/longitude)?**

- [ ] A. `latitude VARCHAR(20), longitude VARCHAR(20)`
- [ ] B. `latitude INT, longitude INT`
- [ ] C. `latitude DECIMAL(9, 6), longitude DECIMAL(9, 6)`
- [ ] D. `latitude FLOAT, longitude FLOAT`

---

## Answer Key (Instructor Only)

### Part A: True or False

|  Q  |  Answer   | Explanation                                                                                                                                    |
| :-: | :-------: | ---------------------------------------------------------------------------------------------------------------------------------------------- |
|  1  | **False** | A table can only have ONE primary key. Multiple columns can form a composite PK, but you cannot define separate PKs.                           |
|  2  | **True**  | In PostgreSQL, `UNIQUE` allows multiple `NULL` values because `NULL` is considered distinct from other `NULL`s. (SQL standard behavior varies) |
|  3  | **False** | `VARCHAR(100)` stores variable length up to 100 chars. `CHAR(100)` pads shorter values with spaces to exactly 100 chars.                       |
|  4  | **False** | Foreign keys do NOT automatically create indexes. You should manually create indexes on FK columns for performance.                            |
|  5  | **True**  | `DECIMAL(5, 2)` means 5 total digits with 2 after decimal = max 999.99. 1000.00 needs 6 total digits.                                          |
|  6  | **False** | `DEFAULT` and `NOT NULL` work together perfectly. If no value provided, the default is used, satisfying NOT NULL.                              |
|  7  | **False** | `SERIAL` is a pseudo-type/shorthand that creates an `INTEGER` column with a sequence and default. It's not a true data type.                   |
|  8  | **False** | `CHECK` constraints can only reference columns within the same row of the same table. Use triggers for cross-table validation.                 |
|  9  | **False** | `TIMESTAMPTZ` converts and stores timestamps in UTC. It does NOT store the original timezone, just the UTC value.                              |
| 10  | **True**  | In PostgreSQL, `TEXT` and unbounded `VARCHAR` are stored identically with no performance difference.                                           |

### Part B: Multiple Choice

|  Q  | Answer | Explanation                                                                                                                                                                      |
| :-: | :----: | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 11  | **C**  | `DECIMAL(12, 2)` provides exact precision for currency. `FLOAT`/`REAL` have rounding errors.                                                                                     |
| 12  | **D**  | Incorrect! `status` has `DEFAULT 'pending'`, so it will be 'pending' not `NULL` if omitted.                                                                                      |
| 13  | **B**  | PostgreSQL raises an error for strings exceeding the defined length. It does NOT silently truncate.                                                                              |
| 14  | **C**  | `UNIQUE NOT NULL` ensures both uniqueness and that the value exists. `UNIQUE` alone allows NULLs.                                                                                |
| 15  | **B**  | Inline `REFERENCES` syntax is valid. Option A has incorrect syntax (missing parentheses around column).                                                                          |
| 16  | **C**  | `UUID` is designed for distributed systems. `SERIAL`/`BIGSERIAL` can have conflicts across multiple servers.                                                                     |
| 17  | **B**  | PostgreSQL BOOLEAN accepts many string representations including 'yes', 'no', '1', '0', 't', 'f', 'true', 'false'.                                                               |
| 18  | **B**  | Composite PKs use `PRIMARY KEY (col1, col2)` at table level. Option A has invalid syntax (two PKs).                                                                              |
| 19  | **B**  | `SMALLINT` = 2 bytes (-32,768 to 32,767), `INT` = 4 bytes (~±2.1 billion), `BIGINT` = 8 bytes (~±9.2 quintillion).                                                               |
| 20  | **B**  | `TIMESTAMP WITH TIME ZONE` (TIMESTAMPTZ) handles timezone conversion for global applications.                                                                                    |
| 21  | **B**  | 4-byte signed INT range: -2,147,483,648 to 2,147,483,647. Option A is SMALLINT, C is BIGINT.                                                                                     |
| 22  | **A**  | Primary key columns are implicitly NOT NULL. You don't need to specify both.                                                                                                     |
| 23  | **C**  | `CHECK` constraint with comparison operators validates the value range. Other options don't enforce 0-100 range.                                                                 |
| 24  | **A**  | Only A succeeds: `age` can be NULL (CHECK only validates non-NULL values), `role` defaults to 'user'. B fails CHECK, C fails NOT NULL on username, D fails NOT NULL on username. |
| 25  | **C**  | `DECIMAL(9, 6)` provides 6 decimal places (needed for ~0.1m accuracy) with 3 digits for the integer part (enough for ±180). FLOAT works but has precision issues.                |
