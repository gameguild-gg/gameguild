# DQL - Data Query Language

DQL (Data Query Language) is the subset of SQL used to retrieve data from the database. The primary statement is `SELECT`.

## SELECT Statement

The `SELECT` statement is the foundation of data retrieval in SQL.

### Basic Syntax

```sql
SELECT column1, column2, ...
FROM table_name
WHERE condition
ORDER BY column
LIMIT n;
```

## SELECT Columns

::: example "Selecting specific columns"

```sql
-- Select specific columns
SELECT name, email FROM users;

-- Select all columns
SELECT * FROM users;

-- Select with alias
SELECT name AS user_name, email AS user_email FROM users;

-- Select with expressions
SELECT name, price, price * 0.9 AS discounted_price FROM products;
```

:::

::: warning

Avoid using `SELECT *` in production code. Always specify the columns you need for:

- Better performance
- Clearer intent
- Protection against schema changes

:::

## FROM Clause

Specifies the table(s) to query from.

::: example "FROM clause"

```sql
-- Single table
SELECT * FROM products;

-- With schema
SELECT * FROM inventory.products;

-- With alias
SELECT p.name, p.price FROM products p;
```

:::

## WHERE Clause

Filters rows based on conditions.

### Comparison Operators

| Operator     | Description           |
| ------------ | --------------------- |
| `=`          | Equal to              |
| `<>` or `!=` | Not equal to          |
| `<`          | Less than             |
| `>`          | Greater than          |
| `<=`         | Less than or equal    |
| `>=`         | Greater than or equal |

::: example "WHERE with comparisons"

```sql
-- Equality
SELECT * FROM products WHERE category = 'Electronics';

-- Comparison
SELECT * FROM products WHERE price > 100;

-- Not equal
SELECT * FROM users WHERE status <> 'inactive';
```

:::

### Logical Operators

| Operator | Description                         |
| -------- | ----------------------------------- |
| `AND`    | Both conditions must be true        |
| `OR`     | At least one condition must be true |
| `NOT`    | Negates the condition               |

::: example "WHERE with logical operators"

```sql
-- AND
SELECT * FROM products
WHERE category = 'Electronics' AND price < 500;

-- OR
SELECT * FROM products
WHERE category = 'Electronics' OR category = 'Computers';

-- NOT
SELECT * FROM users WHERE NOT is_deleted;

-- Combined
SELECT * FROM products
WHERE (category = 'Electronics' OR category = 'Computers')
  AND price < 1000;
```

:::

### Special Operators

| Operator  | Description                 |
| --------- | --------------------------- |
| `IN`      | Matches any value in a list |
| `BETWEEN` | Within a range (inclusive)  |
| `LIKE`    | Pattern matching            |
| `IS NULL` | Checks for NULL values      |

::: example "Special operators"

```sql
-- IN
SELECT * FROM products
WHERE category IN ('Electronics', 'Computers', 'Phones');

-- BETWEEN
SELECT * FROM orders
WHERE order_date BETWEEN '2024-01-01' AND '2024-12-31';

-- LIKE (% = any characters, _ = single character)
SELECT * FROM users WHERE email LIKE '%@gmail.com';
SELECT * FROM products WHERE sku LIKE 'PROD-___';

-- IS NULL / IS NOT NULL
SELECT * FROM users WHERE deleted_at IS NULL;
SELECT * FROM orders WHERE shipped_at IS NOT NULL;
```

:::

## ORDER BY Clause

Sorts the result set.

::: example "ORDER BY"

```sql
-- Ascending (default)
SELECT * FROM products ORDER BY price;
SELECT * FROM products ORDER BY price ASC;

-- Descending
SELECT * FROM products ORDER BY price DESC;

-- Multiple columns
SELECT * FROM products ORDER BY category ASC, price DESC;

-- By expression
SELECT name, price * quantity AS total
FROM order_items
ORDER BY total DESC;

-- By column position (not recommended)
SELECT name, price FROM products ORDER BY 2 DESC;
```

:::

## LIMIT and OFFSET

Controls the number of rows returned.

::: example "LIMIT and OFFSET"

```sql
-- First 10 rows
SELECT * FROM products LIMIT 10;

-- Skip first 20, get next 10 (pagination)
SELECT * FROM products LIMIT 10 OFFSET 20;

-- PostgreSQL alternative syntax
SELECT * FROM products LIMIT 10 OFFSET 20;
-- is equivalent to
SELECT * FROM products OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY;
```

:::

### Pagination Pattern

```sql
-- Page 1 (items 1-10)
SELECT * FROM products ORDER BY id LIMIT 10 OFFSET 0;

-- Page 2 (items 11-20)
SELECT * FROM products ORDER BY id LIMIT 10 OFFSET 10;

-- Page N (generic formula: OFFSET = (page - 1) * limit)
SELECT * FROM products ORDER BY id LIMIT 10 OFFSET ((N - 1) * 10);
```

## DISTINCT

Removes duplicate rows from results.

::: example "DISTINCT"

```sql
-- Unique categories
SELECT DISTINCT category FROM products;

-- Unique combinations
SELECT DISTINCT category, brand FROM products;
```

:::

## Column Expressions

::: example "Expressions and calculations"

```sql
-- Arithmetic
SELECT name, price, quantity, price * quantity AS total
FROM order_items;

-- String concatenation
SELECT first_name || ' ' || last_name AS full_name FROM users;

-- CASE expression
SELECT name, price,
    CASE
        WHEN price < 10 THEN 'Budget'
        WHEN price < 100 THEN 'Standard'
        ELSE 'Premium'
    END AS price_tier
FROM products;
```

:::

## Query Execution Order

Understanding the logical order of SQL clause evaluation:

1. `FROM` - Source tables
2. `WHERE` - Row filtering
3. `GROUP BY` - Grouping
4. `HAVING` - Group filtering
5. `SELECT` - Column selection and expressions
6. `DISTINCT` - Duplicate removal
7. `ORDER BY` - Sorting
8. `LIMIT/OFFSET` - Row limiting

::: note

This is why you can't use column aliases in WHERE:

```sql
-- ❌ Error: alias not available yet
SELECT price * 0.9 AS discounted FROM products WHERE discounted < 50;

-- ✅ Repeat the expression
SELECT price * 0.9 AS discounted FROM products WHERE price * 0.9 < 50;
```

:::

## Practice

Write queries to:

1. Get all products with price between $10 and $50
2. Find users whose email ends with '@company.com'
3. List the 5 most expensive products
4. Get products in categories 'Books' or 'Music', sorted by name
5. Find all orders from January 2024, newest first
