# Quiz: SQL Joins (Week 05)

## Instructions

This quiz tests your understanding of **INNER JOIN**, **LEFT/RIGHT/FULL OUTER JOIN**, **self-joins**, **CROSS JOIN**, **table aliases**, **join conditions**, **filtering with joins**, and **anti-joins/semi-joins**.

---

## PART A: True or False

---

### Question 1

**`JOIN` and `INNER JOIN` are equivalent in SQL.**

- [ ] True
- [ ] False

---

### Question 2

**In an `INNER JOIN`, rows from the left table that have no match in the right table are included with NULL values for the right table columns.**

- [ ] True
- [ ] False

---

### Question 3

**`LEFT JOIN` and `LEFT OUTER JOIN` are the same operation.**

- [ ] True
- [ ] False

---

### Question 4

**In a `CROSS JOIN` between a table with 100 rows and a table with 50 rows, the result will have 150 rows.**

- [ ] True
- [ ] False

---

### Question 5

**When using `LEFT JOIN`, placing a filter condition on the right table in the `WHERE` clause has the same effect as placing it in the `ON` clause.**

- [ ] True
- [ ] False

---

### Question 6

**A self-join requires the table to have a primary key that references itself as a foreign key.**

- [ ] True
- [ ] False

---

### Question 7

**`NATURAL JOIN` automatically joins tables on all columns that have the same name in both tables.**

- [ ] True
- [ ] False

---

### Question 8

**`RIGHT JOIN` can always be rewritten as a `LEFT JOIN` by swapping the table positions.**

- [ ] True
- [ ] False

---

### Question 9

**In a multi-table INNER JOIN, the order of the JOIN clauses affects the final result set.**

- [ ] True
- [ ] False

---

### Question 10

**`FULL OUTER JOIN` returns only rows that have no match in either table.**

- [ ] True
- [ ] False

---

### Question 11

**The `USING` clause can only be used when the join columns have identical names in both tables.**

- [ ] True
- [ ] False

---

### Question 12

**A Cartesian product occurs when you forget the join condition in an INNER JOIN.**

- [ ] True
- [ ] False

---

### Question 13

**When joining tables with aggregations, non-aggregated columns in SELECT must appear in the GROUP BY clause.**

- [ ] True
- [ ] False

---

### Question 14

**`NOT EXISTS` is typically faster than `LEFT JOIN ... WHERE IS NULL` for anti-join patterns.**

- [ ] True
- [ ] False

---

### Question 15

**MySQL natively supports the `FULL OUTER JOIN` syntax.**

- [ ] True
- [ ] False

---

---

## PART B: Multiple Choice

---

### Question 16

**Which query correctly finds all customers and their orders, including customers who have never placed an order?**

- [ ] A.
```sql
SELECT c.name, o.id FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id;
```

- [ ] B.
```sql
SELECT c.name, o.id FROM customers c
RIGHT JOIN orders o ON c.id = o.customer_id;
```

- [ ] C.
```sql
SELECT c.name, o.id FROM orders o
LEFT JOIN customers c ON c.id = o.customer_id;
```
- [ ] D.
```sql
SELECT c.name, o.id FROM customers c
INNER JOIN orders o ON c.id = o.customer_id;
```


---

### Question 17

**What is wrong with this query?**

```sql
SELECT id, name, total_amount
FROM orders
JOIN customers ON customer_id = id;
```

- [ ] A. The `JOIN` keyword should be `INNER JOIN`
- [ ] B. The query is missing a `WHERE` clause
- [ ] C. `id` and `customer_id` are ambiguous - unclear which table they belong to
- [ ] D. Nothing is wrong, this query is valid

---

### Question 18

**Which pattern correctly finds customers who have NEVER placed an order?**

- [ ] A.
```sql
SELECT c.* FROM customers c
RIGHT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;
```
- [ ] B.
```sql
SELECT c.* FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NOT NULL;
```

- [ ] C.
```sql
SELECT c.* FROM customers c
INNER JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;
```

- [ ] D.
```sql
SELECT c.* FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;
```


---

### Question 19

**Given these tables and a `CROSS JOIN`:**

```
Table A: 3 rows
Table B: 4 rows
```

**How many rows will `SELECT * FROM A CROSS JOIN B` return?**

- [ ] A. 3 rows
- [ ] B. 12 rows
- [ ] C. 4 rows
- [ ] D. 7 rows

---

### Question 20

**Which query correctly displays employees with their manager's name using a self-join?**

- [ ] A.
```sql
SELECT e.name AS employee, m.name AS manager
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id;
```

- [ ] B.
```sql
SELECT e.name AS employee, m.name AS manager
FROM employees e
INNER JOIN managers m ON e.manager_id = m.id;
```

- [ ] C.
```sql
SELECT e.name AS employee, e.name AS manager
FROM employees e
WHERE e.manager_id IS NOT NULL;
```

- [ ] D.
```sql
SELECT name AS employee, manager_id AS manager
FROM employees;
```

---

### Question 21

**What is the key difference between these two queries?**

```sql
-- Query 1
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id AND o.status = 'shipped';

-- Query 2
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.status = 'shipped';
```

- [ ] A. Query 1 is invalid syntax; Query 2 is correct
- [ ] B. They are identical in behavior
- [ ] C. Query 2 returns all customers; Query 1 returns only customers with shipped orders
- [ ] D. Query 1 returns all customers; Query 2 returns only customers with shipped orders

---

### Question 22

**Which statement about `FULL OUTER JOIN` is correct?**

- [ ] A. It returns only rows that match in both tables
- [ ] B. It returns all rows from both tables, with NULLs where there's no match
- [ ] C. It returns all rows from the left table and only matching rows from the right
- [ ] D. It returns only rows that don't match in either table

---

### Question 23

**Why is `NATURAL JOIN` generally discouraged?**

- [ ] A. It's slower than explicit joins
- [ ] B. It only works with `LEFT JOIN`
- [ ] C. It automatically matches on ALL columns with the same name, which can cause unexpected results
- [ ] D. It's not supported by PostgreSQL

---

### Question 24

**What does this query return?**

```sql
SELECT p1.name AS product_1, p2.name AS product_2, p1.price
FROM products p1
JOIN products p2 ON p1.price = p2.price AND p1.id < p2.id;
```

- [ ] A. Pairs of different products that have the same price (no duplicates)
- [ ] B. All products paired with themselves
- [ ] C. All products with their prices doubled
- [ ] D. Products where p1.price is less than p2.price

---

### Question 25

**Which query correctly calculates total revenue per customer, including customers with no orders?**

- [ ] A.
```sql
SELECT c.name, SUM(o.total_amount) AS revenue
FROM customers c
INNER JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name;
```

- [ ] B.
```sql
SELECT c.name, SUM(o.total_amount) AS revenue
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.total_amount > 0
GROUP BY c.id, c.name;
```

- [ ] C.
```sql
SELECT c.name, COALESCE(SUM(o.total_amount), 0) AS revenue
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name;
```

- [ ] D.
```sql
SELECT c.name, SUM(total_amount) AS revenue
FROM customers c, orders o
GROUP BY c.name;
```

---

### Question 26

**In the query execution order, when are JOINs processed?**

- [ ] A. After SELECT
- [ ] B. Before WHERE
- [ ] C. After GROUP BY
- [ ] D. After HAVING

---

### Question 27

**What does `EXISTS` do in a semi-join pattern?**

```sql
SELECT c.*
FROM customers c
WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id);
```

- [ ] A. Returns customers who have never placed an order
- [ ] B. Returns all customers with a count of their orders
- [ ] C. Returns the first order for each customer
- [ ] D. Returns customers who have placed at least one order (without duplicates)

---

### Question 28

**Which JOIN type would produce this result from two tables?**

```
employees (left)     departments (right)    Result:
+-------+--------+   +-------+---------+    +-------+--------+---------+
| name  | dept_id|   | id    | name    |    | name  | dept_id| dept    |
+-------+--------+   +-------+---------+    +-------+--------+---------+
| Alice | 1      |   | 1     | Sales   |    | Alice | 1      | Sales   |
| Bob   | 2      |   | 2     | HR      |    | Bob   | 2      | HR      |
| Carol | NULL   |   | 3     | IT      |    | Carol | NULL   | NULL    |
+-------+--------+   +-------+---------+    | NULL  | NULL   | IT      |
                                            +-------+--------+---------+
```

- [ ] A. FULL OUTER JOIN
- [ ] B. INNER JOIN
- [ ] C. LEFT JOIN
- [ ] D. RIGHT JOIN

---

### Question 29

**Which is the correct way to use the `USING` clause?**

- [ ] A. `FROM orders o JOIN customers c USING (o.customer_id = c.customer_id)`
- [ ] B. `FROM orders JOIN customers USING customer_id`
- [ ] C. `FROM orders o JOIN customers c USING (customer_id)`
- [ ] D. `FROM orders o JOIN customers c USING customer_id = customer_id`

---

### Question 30

**What problem does this query have?**

```sql
SELECT * FROM orders, customers;
```

- [ ] A. The syntax is invalid
- [ ] B. It only returns orders without customers
- [ ] C. It returns an empty result set
- [ ] D. It creates a Cartesian product (every order paired with every customer)

---

---

## PART C: SQL Translation

---

### Question 31 - Requirement → SQL

**Requirement:** Show all products with their category names. Products must have a category, but also show the supplier name if available (products may not have a supplier assigned).

**Which query is correct?**

- [ ] A.
```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
LEFT JOIN suppliers s ON p.supplier_id = s.id;
```

- [ ] B.
```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
INNER JOIN suppliers s ON p.supplier_id = s.id;
```
- [ ] C.
```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
INNER JOIN categories c ON p.category_id = c.id
INNER JOIN suppliers s ON p.supplier_id = s.id;
```

- [ ] D.
```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
INNER JOIN categories c ON p.category_id = c.id
LEFT JOIN suppliers s ON p.supplier_id = s.id;
```


---

### Question 32 - SQL → Description

**What does this query find?**

```sql
SELECT c.name AS category
FROM categories c
LEFT JOIN products p ON c.id = p.category_id
WHERE p.id IS NULL;
```

- [ ] A. Categories that have no products (empty categories)
- [ ] B. All categories with their products
- [ ] C. Categories that have at least one product
- [ ] D. Products that have no category assigned

---

### Question 33 - Requirement → SQL

**Requirement:** Find all pairs of employees who were hired on the same date (don't include an employee paired with themselves, and don't show duplicate pairs like Alice-Bob and Bob-Alice).

**Which query is correct?**

- [ ] A.
```sql
SELECT e1.name, e2.name, e1.hire_date
FROM employees e1
CROSS JOIN employees e2
WHERE e1.hire_date = e2.hire_date;
```

- [ ] B.
```sql
SELECT e1.name, e2.name, e1.hire_date
FROM employees e1
JOIN employees e2 ON e1.hire_date = e2.hire_date AND e1.id != e2.id;
```

- [ ] C.
```sql
SELECT e1.name, e2.name, e1.hire_date
FROM employees e1
JOIN employees e2 ON e1.hire_date = e2.hire_date AND e1.id < e2.id;
```

- [ ] D.
```sql
SELECT name, hire_date
FROM employees
GROUP BY hire_date
HAVING COUNT(*) > 1;
```

---

### Question 34 - SQL → Description

**What does this query calculate?**

```sql
SELECT 
    cat.name AS category,
    COUNT(DISTINCT o.id) AS orders,
    SUM(oi.quantity * oi.unit_price) AS revenue
FROM categories cat
JOIN products p ON cat.id = p.category_id
JOIN order_items oi ON p.id = oi.product_id
JOIN orders o ON oi.order_id = o.id
WHERE o.status = 'completed'
GROUP BY cat.id, cat.name
ORDER BY revenue DESC;
```

- [ ] A. The number of products per category
- [ ] B. Number of orders and total revenue per category from completed orders
- [ ] C. Revenue per product from completed orders
- [ ] D. Average order value per category

---

### Question 35 - Requirement → SQL

**Requirement:** Generate a report showing all combinations of product colors and sizes for inventory planning.

**Which query is correct?**

- [ ] A.
```sql
SELECT c.name AS color, s.name AS size
FROM colors c
CROSS JOIN sizes s;
```

- [ ] B.
```sql
SELECT c.name AS color, s.name AS size
FROM colors c
INNER JOIN sizes s ON c.id = s.id;
```

- [ ] C.
```sql
SELECT c.name AS color, s.name AS size
FROM colors c
LEFT JOIN sizes s ON 1=1;
```

- [ ] D.
```sql
SELECT c.name AS color, s.name AS size
FROM colors c
FULL OUTER JOIN sizes s ON c.id = s.id;
```

---

### Question 36 - SQL → Description

**What does this query do?**

```sql
SELECT c.*
FROM customers c
WHERE NOT EXISTS (
    SELECT 1 FROM orders o 
    WHERE o.customer_id = c.id 
    AND o.created_at >= '2026-01-01'
);
```

- [ ] A. Finds customers who placed orders in 2026
- [ ] B. Finds customers who have never placed any order
- [ ] C. Counts orders per customer in 2026
- [ ] D. Finds customers who have NOT placed any orders in 2026 (but may have ordered before)

---

### Question 37 - Requirement → SQL

**Requirement:** Show order details including customer name, ordered product names, and quantities. Only include orders that have at least one order item.

**Which query is correct?**

- [ ] A.
```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM orders o
LEFT JOIN customers c ON o.customer_id = c.id
LEFT JOIN order_items oi ON o.id = oi.order_id
LEFT JOIN products p ON oi.product_id = p.id;
```

- [ ] B.
```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM orders o
JOIN customers c ON o.customer_id = c.id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id;
```

- [ ] C.
```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM customers c
CROSS JOIN products p
CROSS JOIN order_items oi;
```

- [ ] D.
```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM customers c
RIGHT JOIN orders o ON c.id = o.customer_id
RIGHT JOIN order_items oi ON o.id = oi.order_id;
```

---

### Question 38 - SQL → Description

**What happens with this query?**

```sql
SELECT e.name, d.department_name
FROM employees e
LEFT JOIN departments d ON e.department_id = d.id
WHERE d.location = 'New York';
```

- [ ] A. Returns all employees, showing department only for those in New York
- [ ] B. Returns all employees and all departments in New York
- [ ] C. Returns only employees whose department is in New York (like an INNER JOIN)
- [ ] D. Returns an error because you can't filter on a LEFT JOINed table

---

### Question 39 - Requirement → SQL

**Requirement:** Find the total number of unique products ordered and total quantity sold per customer, but only for customers who have ordered more than 5 different products.

**Which query is correct?**

- [ ] A.
```sql
SELECT c.name, COUNT(DISTINCT p.id) AS products, SUM(oi.quantity) AS total_qty
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
GROUP BY c.id, c.name
HAVING COUNT(DISTINCT p.id) > 5;
```

- [ ] B.
```sql
SELECT c.name, COUNT(p.id) AS products, SUM(oi.quantity) AS total_qty
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
GROUP BY c.id, c.name
WHERE COUNT(DISTINCT p.id) > 5;
```

- [ ] C.
```sql
SELECT c.name, COUNT(DISTINCT p.id) AS products, SUM(oi.quantity) AS total_qty
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
LEFT JOIN order_items oi ON o.id = oi.order_id
LEFT JOIN products p ON oi.product_id = p.id
GROUP BY c.id, c.name
HAVING products > 5;
```

- [ ] D.
```sql
SELECT c.name, products, total_qty
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
WHERE DISTINCT products > 5;
```

---

### Question 40 - SQL → Description

**What type of join pattern is this?**

```sql
SELECT p.*
FROM products p
WHERE EXISTS (
    SELECT 1 FROM order_items oi WHERE oi.product_id = p.id
);
```

- [ ] A. Anti-join (finds rows with NO match)
- [ ] B. Cross join (all combinations)
- [ ] C. Self-join (table joined to itself)
- [ ] D. Semi-join (finds rows with at least one match, no duplicates)

---

---

## PART D: Additional Questions

---

### Question 41

**Join conditions can only use equality comparisons (`=`).**

- [ ] True
- [ ] False

---

### Question 42

**Which query correctly assigns employees to salary grades based on a range?**

```sql
-- salary_grades table: id, grade_name, min_salary, max_salary
```

- [ ] A.
```sql
SELECT e.name, g.grade_name
FROM employees e
JOIN salary_grades g ON e.salary = g.min_salary;
```

- [ ] B.
```sql
SELECT e.name, g.grade_name
FROM employees e
CROSS JOIN salary_grades g
WHERE e.salary > g.min_salary;
```

- [ ] C.
```sql
SELECT e.name, g.grade_name
FROM employees e
LEFT JOIN salary_grades g ON e.salary = g.grade_name;
```
- [ ] D.
```sql
SELECT e.name, g.grade_name
FROM employees e
JOIN salary_grades g ON e.salary BETWEEN g.min_salary AND g.max_salary;
```


---

### Question 43

**What type of join condition is used in this query?**

```sql
SELECT 
    e.name AS employee,
    s.name AS senior
FROM employees e
JOIN employees s ON e.hire_date > s.hire_date;
```

- [ ] A. Non-equality join (comparison-based)
- [ ] B. Equality join
- [ ] C. Natural join
- [ ] D. Cross join

---

### Question 44

**Creating indexes on foreign key columns used in JOIN conditions improves query performance.**

- [ ] True
- [ ] False

---

### Question 45

**Which columns should be indexed to optimize this query?**

```sql
SELECT c.name, o.total_amount, p.name AS product
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
WHERE o.status = 'completed';
```

- [ ] A. Only `customers.id` (the primary key)
- [ ] B. Only `orders.status` since it's in the WHERE clause
- [ ] C. `orders.customer_id`, `order_items.order_id`, `order_items.product_id`, and `orders.status`
- [ ] D. No indexes are needed; JOINs are automatically optimized

---

### Question 46

**Which query uses a range-based join to find products priced within a discount tier?**

```sql
-- discount_tiers: id, tier_name, min_price, max_price, discount_pct
```

- [ ] A.
```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
JOIN discount_tiers d ON p.id = d.id;
```

- [ ] B.
```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
JOIN discount_tiers d ON p.price >= d.min_price AND p.price <= d.max_price;
```

- [ ] C.
```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
CROSS JOIN discount_tiers d;
```

- [ ] D.
```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
LEFT JOIN discount_tiers d ON p.price = d.min_price;
```
