# Quiz: SQL Joins (Week 05)

## Instructions

This quiz tests your understanding of **INNER JOIN**, **LEFT/RIGHT/FULL OUTER JOIN**, **self-joins**, **CROSS JOIN**, **table aliases**, **join conditions**, **filtering with joins**, and **anti-joins/semi-joins**.

**Total: 46 questions**

Time estimate: 45-60 minutes

---

# PART A: True or False

---

!!! quiz
{
"title": "JOIN vs INNER JOIN",
"question": "`JOIN` and `INNER JOIN` are equivalent in SQL.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "INNER JOIN and NULLs",
"question": "In an `INNER JOIN`, rows from the left table that have no match in the right table are included with NULL values for the right table columns.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "LEFT JOIN vs LEFT OUTER JOIN",
"question": "`LEFT JOIN` and `LEFT OUTER JOIN` are the same operation.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "CROSS JOIN Row Count",
"question": "In a `CROSS JOIN` between a table with 100 rows and a table with 50 rows, the result will have 150 rows.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "LEFT JOIN WHERE vs ON",
"question": "When using `LEFT JOIN`, placing a filter condition on the right table in the `WHERE` clause has the same effect as placing it in the `ON` clause.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Self-Join Requirements",
"question": "A self-join requires the table to have a primary key that references itself as a foreign key.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "NATURAL JOIN Behavior",
"question": "`NATURAL JOIN` automatically joins tables on all columns that have the same name in both tables.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "RIGHT JOIN Rewrite",
"question": "`RIGHT JOIN` can always be rewritten as a `LEFT JOIN` by swapping the table positions.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "INNER JOIN Order",
"question": "In a multi-table INNER JOIN, the order of the JOIN clauses affects the final result set.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "FULL OUTER JOIN Behavior",
"question": "`FULL OUTER JOIN` returns only rows that have no match in either table.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "USING Clause Requirement",
"question": "The `USING` clause can only be used when the join columns have identical names in both tables.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Cartesian Product",
"question": "A Cartesian product occurs when you forget the join condition in an INNER JOIN.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "GROUP BY with JOINs",
"question": "When joining tables with aggregations, non-aggregated columns in SELECT must appear in the GROUP BY clause.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "NOT EXISTS Performance",
"question": "`NOT EXISTS` is typically faster than `LEFT JOIN ... WHERE IS NULL` for anti-join patterns.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "MySQL FULL OUTER JOIN",
"question": "MySQL natively supports the `FULL OUTER JOIN` syntax.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

# PART B: Multiple Choice

---

**Which query correctly finds all customers and their orders, including customers who have never placed an order?**

Option A:

```sql
SELECT c.name, o.id FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id;
```

Option B:

```sql
SELECT c.name, o.id FROM customers c
RIGHT JOIN orders o ON c.id = o.customer_id;
```

Option C:

```sql
SELECT c.name, o.id FROM orders o
LEFT JOIN customers c ON c.id = o.customer_id;
```

Option D:

```sql
SELECT c.name, o.id FROM customers c
INNER JOIN orders o ON c.id = o.customer_id;
```

!!! quiz
{
"title": "LEFT JOIN for Optional Matches",
"question": "Which query correctly finds all customers and their orders, including customers who have never placed an order?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What is wrong with this query?**

```sql
SELECT id, name, total_amount
FROM orders
JOIN customers ON customer_id = id;
```

!!! quiz
{
"title": "Ambiguous Column References",
"question": "What is wrong with the query above?",
"options": ["The JOIN keyword should be INNER JOIN", "The query is missing a WHERE clause", "id and customer_id are ambiguous - unclear which table they belong to", "Nothing is wrong, this query is valid"],
"answers": ["id and customer_id are ambiguous - unclear which table they belong to"]
}
!!!

---

**Which pattern correctly finds customers who have NEVER placed an order?**

Option A:

```sql
SELECT c.* FROM customers c
RIGHT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;
```

Option B:

```sql
SELECT c.* FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NOT NULL;
```

Option C:

```sql
SELECT c.* FROM customers c
INNER JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;
```

Option D:

```sql
SELECT c.* FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.id IS NULL;
```

!!! quiz
{
"title": "Anti-Join Pattern",
"question": "Which pattern correctly finds customers who have NEVER placed an order?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

**Given these tables and a `CROSS JOIN`:**

```
Table A: 3 rows
Table B: 4 rows
```

!!! quiz
{
"title": "CROSS JOIN Row Count",
"question": "How many rows will `SELECT * FROM A CROSS JOIN B` return?",
"options": ["3 rows", "12 rows", "4 rows", "7 rows"],
"answers": ["12 rows"]
}
!!!

---

**Which query correctly displays employees with their manager's name using a self-join?**

Option A:

```sql
SELECT e.name AS employee, m.name AS manager
FROM employees e
LEFT JOIN employees m ON e.manager_id = m.id;
```

Option B:

```sql
SELECT e.name AS employee, m.name AS manager
FROM employees e
INNER JOIN managers m ON e.manager_id = m.id;
```

Option C:

```sql
SELECT e.name AS employee, e.name AS manager
FROM employees e
WHERE e.manager_id IS NOT NULL;
```

Option D:

```sql
SELECT name AS employee, manager_id AS manager
FROM employees;
```

!!! quiz
{
"title": "Self-Join for Manager",
"question": "Which query correctly displays employees with their manager's name using a self-join?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What is the key difference between these two queries?**

Query 1:

```sql
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id AND o.status = 'shipped';
```

Query 2:

```sql
SELECT c.name, o.id
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.status = 'shipped';
```

!!! quiz
{
"title": "ON vs WHERE in LEFT JOIN",
"question": "What is the key difference between the two queries above?",
"options": ["Query 1 is invalid syntax; Query 2 is correct", "They are identical in behavior", "Query 2 returns all customers; Query 1 returns only customers with shipped orders", "Query 1 returns all customers; Query 2 returns only customers with shipped orders"],
"answers": ["Query 1 returns all customers; Query 2 returns only customers with shipped orders"]
}
!!!

---

!!! quiz
{
"title": "FULL OUTER JOIN Definition",
"question": "Which statement about `FULL OUTER JOIN` is correct?",
"options": ["It returns only rows that match in both tables", "It returns all rows from both tables, with NULLs where there is no match", "It returns all rows from the left table and only matching rows from the right", "It returns only rows that do not match in either table"],
"answers": ["It returns all rows from both tables, with NULLs where there is no match"]
}
!!!

---

!!! quiz
{
"title": "NATURAL JOIN Discouraged",
"question": "Why is `NATURAL JOIN` generally discouraged?",
"options": ["It is slower than explicit joins", "It only works with LEFT JOIN", "It automatically matches on ALL columns with the same name, which can cause unexpected results", "It is not supported by PostgreSQL"],
"answers": ["It automatically matches on ALL columns with the same name, which can cause unexpected results"]
}
!!!

---

**What does this query return?**

```sql
SELECT p1.name AS product_1, p2.name AS product_2, p1.price
FROM products p1
JOIN products p2 ON p1.price = p2.price AND p1.id < p2.id;
```

!!! quiz
{
"title": "Self-Join with Price Matching",
"question": "What does the query above return?",
"options": ["Pairs of different products that have the same price (no duplicates)", "All products paired with themselves", "All products with their prices doubled", "Products where p1.price is less than p2.price"],
"answers": ["Pairs of different products that have the same price (no duplicates)"]
}
!!!

---

**Which query correctly calculates total revenue per customer, including customers with no orders?**

Option A:

```sql
SELECT c.name, SUM(o.total_amount) AS revenue
FROM customers c
INNER JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name;
```

Option B:

```sql
SELECT c.name, SUM(o.total_amount) AS revenue
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
WHERE o.total_amount > 0
GROUP BY c.id, c.name;
```

Option C:

```sql
SELECT c.name, COALESCE(SUM(o.total_amount), 0) AS revenue
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name;
```

Option D:

```sql
SELECT c.name, SUM(total_amount) AS revenue
FROM customers c, orders o
GROUP BY c.name;
```

!!! quiz
{
"title": "Revenue with LEFT JOIN and COALESCE",
"question": "Which query correctly calculates total revenue per customer, including customers with no orders?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

!!! quiz
{
"title": "Query Execution Order",
"question": "In the query execution order, when are JOINs processed?",
"options": ["After SELECT", "Before WHERE", "After GROUP BY", "After HAVING"],
"answers": ["Before WHERE"]
}
!!!

---

**What does `EXISTS` do in the following semi-join pattern?**

```sql
SELECT c.*
FROM customers c
WHERE EXISTS (SELECT 1 FROM orders o WHERE o.customer_id = c.id);
```

!!! quiz
{
"title": "EXISTS Semi-Join",
"question": "What does `EXISTS` do in the query above?",
"options": ["Returns customers who have never placed an order", "Returns all customers with a count of their orders", "Returns the first order for each customer", "Returns customers who have placed at least one order (without duplicates)"],
"answers": ["Returns customers who have placed at least one order (without duplicates)"]
}
!!!

---

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

!!! quiz
{
"title": "Identify JOIN Type from Result",
"question": "Which JOIN type would produce the result shown above?",
"options": ["FULL OUTER JOIN", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN"],
"answers": ["FULL OUTER JOIN"]
}
!!!

---

!!! quiz
{
"title": "USING Clause Syntax",
"question": "Which is the correct way to use the `USING` clause?",
"options": ["FROM orders o JOIN customers c USING (o.customer_id = c.customer_id)", "FROM orders JOIN customers USING customer_id", "FROM orders o JOIN customers c USING (customer_id)", "FROM orders o JOIN customers c USING customer_id = customer_id"],
"answers": ["FROM orders o JOIN customers c USING (customer_id)"]
}
!!!

---

**What problem does this query have?**

```sql
SELECT * FROM orders, customers;
```

!!! quiz
{
"title": "Comma Join Without Condition",
"question": "What problem does the query above have?",
"options": ["The syntax is invalid", "It only returns orders without customers", "It returns an empty result set", "It creates a Cartesian product (every order paired with every customer)"],
"answers": ["It creates a Cartesian product (every order paired with every customer)"]
}
!!!

---

# PART C: SQL Translation

---

**Requirement:** Show all products with their category names. Products must have a category, but also show the supplier name if available (products may not have a supplier assigned).

Option A:

```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
LEFT JOIN suppliers s ON p.supplier_id = s.id;
```

Option B:

```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
INNER JOIN suppliers s ON p.supplier_id = s.id;
```

Option C:

```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
INNER JOIN categories c ON p.category_id = c.id
INNER JOIN suppliers s ON p.supplier_id = s.id;
```

Option D:

```sql
SELECT p.name, c.name AS category, s.name AS supplier
FROM products p
INNER JOIN categories c ON p.category_id = c.id
LEFT JOIN suppliers s ON p.supplier_id = s.id;
```

!!! quiz
{
"title": "Mixed JOIN Types",
"question": "Which query correctly shows all products with their category names (required) and supplier name (optional)?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

**What does this query find?**

```sql
SELECT c.name AS category
FROM categories c
LEFT JOIN products p ON c.id = p.category_id
WHERE p.id IS NULL;
```

!!! quiz
{
"title": "LEFT JOIN Anti-Pattern for Empty Categories",
"question": "What does the query above find?",
"options": ["Categories that have no products (empty categories)", "All categories with their products", "Categories that have at least one product", "Products that have no category assigned"],
"answers": ["Categories that have no products (empty categories)"]
}
!!!

---

**Requirement:** Find all pairs of employees who were hired on the same date (don't include an employee paired with themselves, and don't show duplicate pairs like Alice-Bob and Bob-Alice).

Option A:

```sql
SELECT e1.name, e2.name, e1.hire_date
FROM employees e1
CROSS JOIN employees e2
WHERE e1.hire_date = e2.hire_date;
```

Option B:

```sql
SELECT e1.name, e2.name, e1.hire_date
FROM employees e1
JOIN employees e2 ON e1.hire_date = e2.hire_date AND e1.id != e2.id;
```

Option C:

```sql
SELECT e1.name, e2.name, e1.hire_date
FROM employees e1
JOIN employees e2 ON e1.hire_date = e2.hire_date AND e1.id < e2.id;
```

Option D:

```sql
SELECT name, hire_date
FROM employees
GROUP BY hire_date
HAVING COUNT(*) > 1;
```

!!! quiz
{
"title": "Self-Join for Same Hire Date Pairs",
"question": "Which query correctly finds unique pairs of employees hired on the same date?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

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

!!! quiz
{
"title": "Multi-Table JOIN with Aggregation",
"question": "What does the query above calculate?",
"options": ["The number of products per category", "Number of orders and total revenue per category from completed orders", "Revenue per product from completed orders", "Average order value per category"],
"answers": ["Number of orders and total revenue per category from completed orders"]
}
!!!

---

**Requirement:** Generate a report showing all combinations of product colors and sizes for inventory planning.

Option A:

```sql
SELECT c.name AS color, s.name AS size
FROM colors c
CROSS JOIN sizes s;
```

Option B:

```sql
SELECT c.name AS color, s.name AS size
FROM colors c
INNER JOIN sizes s ON c.id = s.id;
```

Option C:

```sql
SELECT c.name AS color, s.name AS size
FROM colors c
LEFT JOIN sizes s ON 1=1;
```

Option D:

```sql
SELECT c.name AS color, s.name AS size
FROM colors c
FULL OUTER JOIN sizes s ON c.id = s.id;
```

!!! quiz
{
"title": "CROSS JOIN for Combinations",
"question": "Which query correctly generates all combinations of product colors and sizes?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

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

!!! quiz
{
"title": "NOT EXISTS with Date Filter",
"question": "What does the query above do?",
"options": ["Finds customers who placed orders in 2026", "Finds customers who have never placed any order", "Counts orders per customer in 2026", "Finds customers who have NOT placed any orders in 2026 (but may have ordered before)"],
"answers": ["Finds customers who have NOT placed any orders in 2026 (but may have ordered before)"]
}
!!!

---

**Requirement:** Show order details including customer name, ordered product names, and quantities. Only include orders that have at least one order item.

Option A:

```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM orders o
LEFT JOIN customers c ON o.customer_id = c.id
LEFT JOIN order_items oi ON o.id = oi.order_id
LEFT JOIN products p ON oi.product_id = p.id;
```

Option B:

```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM orders o
JOIN customers c ON o.customer_id = c.id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id;
```

Option C:

```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM customers c
CROSS JOIN products p
CROSS JOIN order_items oi;
```

Option D:

```sql
SELECT c.name AS customer, p.name AS product, oi.quantity
FROM customers c
RIGHT JOIN orders o ON c.id = o.customer_id
RIGHT JOIN order_items oi ON o.id = oi.order_id;
```

!!! quiz
{
"title": "INNER JOIN for Required Relationships",
"question": "Which query correctly shows order details, only including orders that have at least one order item?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

**What happens with this query?**

```sql
SELECT e.name, d.department_name
FROM employees e
LEFT JOIN departments d ON e.department_id = d.id
WHERE d.location = 'New York';
```

!!! quiz
{
"title": "WHERE Nullifying LEFT JOIN",
"question": "What happens with the query above?",
"options": ["Returns all employees, showing department only for those in New York", "Returns all employees and all departments in New York", "Returns only employees whose department is in New York (like an INNER JOIN)", "Returns an error because you cannot filter on a LEFT JOINed table"],
"answers": ["Returns only employees whose department is in New York (like an INNER JOIN)"]
}
!!!

---

**Requirement:** Find the total number of unique products ordered and total quantity sold per customer, but only for customers who have ordered more than 5 different products.

Option A:

```sql
SELECT c.name, COUNT(DISTINCT p.id) AS products, SUM(oi.quantity) AS total_qty
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
GROUP BY c.id, c.name
HAVING COUNT(DISTINCT p.id) > 5;
```

Option B:

```sql
SELECT c.name, COUNT(p.id) AS products, SUM(oi.quantity) AS total_qty
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
GROUP BY c.id, c.name
WHERE COUNT(DISTINCT p.id) > 5;
```

Option C:

```sql
SELECT c.name, COUNT(DISTINCT p.id) AS products, SUM(oi.quantity) AS total_qty
FROM customers c
LEFT JOIN orders o ON c.id = o.customer_id
LEFT JOIN order_items oi ON o.id = oi.order_id
LEFT JOIN products p ON oi.product_id = p.id
GROUP BY c.id, c.name
HAVING products > 5;
```

Option D:

```sql
SELECT c.name, products, total_qty
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
WHERE DISTINCT products > 5;
```

!!! quiz
{
"title": "HAVING with COUNT DISTINCT",
"question": "Which query correctly finds customers who have ordered more than 5 different products?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What type of join pattern is this?**

```sql
SELECT p.*
FROM products p
WHERE EXISTS (
    SELECT 1 FROM order_items oi WHERE oi.product_id = p.id
);
```

!!! quiz
{
"title": "Semi-Join Pattern",
"question": "What type of join pattern is the query above?",
"options": ["Anti-join (finds rows with NO match)", "Cross join (all combinations)", "Self-join (table joined to itself)", "Semi-join (finds rows with at least one match, no duplicates)"],
"answers": ["Semi-join (finds rows with at least one match, no duplicates)"]
}
!!!

---

# PART D: Additional Questions

---

!!! quiz
{
"title": "Equality-Only Join Conditions",
"question": "Join conditions can only use equality comparisons (`=`).",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

**Which query correctly assigns employees to salary grades based on a range?**

```sql
-- salary_grades table: id, grade_name, min_salary, max_salary
```

Option A:

```sql
SELECT e.name, g.grade_name
FROM employees e
JOIN salary_grades g ON e.salary = g.min_salary;
```

Option B:

```sql
SELECT e.name, g.grade_name
FROM employees e
CROSS JOIN salary_grades g
WHERE e.salary > g.min_salary;
```

Option C:

```sql
SELECT e.name, g.grade_name
FROM employees e
LEFT JOIN salary_grades g ON e.salary = g.grade_name;
```

Option D:

```sql
SELECT e.name, g.grade_name
FROM employees e
JOIN salary_grades g ON e.salary BETWEEN g.min_salary AND g.max_salary;
```

!!! quiz
{
"title": "Range-Based Join",
"question": "Which query correctly assigns employees to salary grades based on a range?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

**What type of join condition is used in this query?**

```sql
SELECT
    e.name AS employee,
    s.name AS senior
FROM employees e
JOIN employees s ON e.hire_date > s.hire_date;
```

!!! quiz
{
"title": "Non-Equality Join Condition",
"question": "What type of join condition is used in the query above?",
"options": ["Non-equality join (comparison-based)", "Equality join", "Natural join", "Cross join"],
"answers": ["Non-equality join (comparison-based)"]
}
!!!

---

!!! quiz
{
"title": "Indexes on Foreign Keys",
"question": "Creating indexes on foreign key columns used in JOIN conditions improves query performance.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

**Which columns should be indexed to optimize this query?**

```sql
SELECT c.name, o.total_amount, p.name AS product
FROM customers c
JOIN orders o ON c.id = o.customer_id
JOIN order_items oi ON o.id = oi.order_id
JOIN products p ON oi.product_id = p.id
WHERE o.status = 'completed';
```

!!! quiz
{
"title": "Index Selection for JOINs",
"question": "Which columns should be indexed to optimize the query above?",
"options": ["Only customers.id (the primary key)", "Only orders.status since it is in the WHERE clause", "orders.customer_id, order_items.order_id, order_items.product_id, and orders.status", "No indexes are needed; JOINs are automatically optimized"],
"answers": ["orders.customer_id, order_items.order_id, order_items.product_id, and orders.status"]
}
!!!

---

**Which query uses a range-based join to find products priced within a discount tier?**

```sql
-- discount_tiers: id, tier_name, min_price, max_price, discount_pct
```

Option A:

```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
JOIN discount_tiers d ON p.id = d.id;
```

Option B:

```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
JOIN discount_tiers d ON p.price >= d.min_price AND p.price <= d.max_price;
```

Option C:

```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
CROSS JOIN discount_tiers d;
```

Option D:

```sql
SELECT p.name, d.tier_name, d.discount_pct
FROM products p
LEFT JOIN discount_tiers d ON p.price = d.min_price;
```

!!! quiz
{
"title": "Range-Based Join for Discount Tiers",
"question": "Which query uses a range-based join to find products priced within a discount tier?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!
