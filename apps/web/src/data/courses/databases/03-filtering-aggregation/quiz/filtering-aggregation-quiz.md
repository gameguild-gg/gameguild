# Quiz: Filtering & Aggregation (Week 03)

## Instructions

This quiz tests your understanding of **boolean logic**, **filtering operators**, **pattern matching**, **NULL handling**, **CASE expressions**, and **aggregate functions** in SQL.

---

## PART A: True or False

---

!!! quiz
{
"title": "Question 1: NULL Comparison",
"question": "In SQL's three-valued logic, NULL = NULL evaluates to TRUE.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 2: HAVING Without GROUP BY",
"question": "The HAVING clause can be used without a GROUP BY clause in the query.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 3: COUNT Variations",
"question": "COUNT(*) and COUNT(column_name) always return the same result.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 4: LIKE Case Sensitivity",
"question": "The LIKE operator in PostgreSQL is case-sensitive by default.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 5: BETWEEN Inclusivity",
"question": "BETWEEN 10 AND 20 includes both 10 and 20 in the results (inclusive range).",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

Consider this WHERE clause: `WHERE price > 100 OR category = 'Books' AND in_stock = true`

!!! quiz
{
"title": "Question 6: Operator Precedence",
"question": "In the WHERE clause above, the AND is evaluated before the OR.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 7: AVG and NULL",
"question": "AVG(column) includes NULL values in its calculation.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 8: Column Aliases in WHERE",
"question": "You can use column aliases defined in SELECT within the WHERE clause of the same query.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 9: COALESCE Function",
"question": "COALESCE(NULL, NULL, 'default') returns 'default'.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 10: NOT IN with NULL",
"question": "NOT IN (1, 2, NULL) will never return any rows, even if matching values exist.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

## PART B: Multiple Choice

---

Consider these LIKE patterns for finding products:

- A: `SELECT * FROM products WHERE name LIKE 'Pro%Plus';`
- B: `SELECT * FROM products WHERE name LIKE '%Pro%Plus%';`
- C: `SELECT * FROM products WHERE name LIKE 'Pro_Plus';`
- D: `SELECT * FROM products WHERE name = 'Pro*Plus';`

!!! quiz
{
"title": "Question 11: LIKE Pattern Matching",
"question": "Which query correctly finds all products with names starting with 'Pro' and ending with 'Plus'?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

Given a table `orders` with some NULL values in the `shipped_at` column:

```sql
SELECT COUNT(*), COUNT(shipped_at) FROM orders;
-- Returns: 100, 75
```

!!! quiz
{
"title": "Question 12: COUNT Behavior",
"question": "What can you conclude from the query result above?",
"options": ["There are 100 shipped orders and 75 total orders", "There are 100 total orders and 25 have not been shipped yet (NULL shipped_at)", "There are 175 total orders in the table", "The query has an error because you can't use two COUNT functions"],
"answers": ["There are 100 total orders and 25 have not been shipped yet (NULL shipped_at)"]
}
!!!

---

Consider these queries to find users with no phone number:

- A: `SELECT * FROM users WHERE phone = NULL;`
- B: `SELECT * FROM users WHERE phone == NULL;`
- C: `SELECT * FROM users WHERE phone IS NULL;`
- D: `SELECT * FROM users WHERE phone = '';`

!!! quiz
{
"title": "Question 13: NULL Checking",
"question": "Which query correctly finds users who have no phone number recorded?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

Consider this query:

```sql
SELECT
    category,
    COUNT(*) AS product_count
FROM products
WHERE price > 50
GROUP BY category
HAVING COUNT(*) >= 5
ORDER BY product_count DESC;
```

!!! quiz
{
"title": "Question 14: GROUP BY and HAVING",
"question": "What does this query return?",
"options": ["All categories with their product counts, filtered to show only products over $50", "Categories that have at least 5 products priced over $50, sorted by count descending", "The top 5 categories by total product count", "Products grouped by category where the average price is over $50"],
"answers": ["Categories that have at least 5 products priced over $50, sorted by count descending"]
}
!!!

---

!!! quiz
{
"title": "Question 15: WHERE vs HAVING",
"question": "Which statement about WHERE vs HAVING is correct?",
"options": ["WHERE filters after grouping, HAVING filters before grouping", "WHERE filters individual rows before grouping, HAVING filters groups after aggregation", "WHERE and HAVING can be used interchangeably", "HAVING can only be used with COUNT(*), not other aggregate functions"],
"answers": ["WHERE filters individual rows before grouping, HAVING filters groups after aggregation"]
}
!!!

---

!!! quiz
{
"title": "Question 16: NULLIF Function",
"question": "What is the result of this expression: NULLIF(100, 100)?",
"options": ["100", "0", "NULL", "An error"],
"answers": ["NULL"]
}
!!!

---

Table: orders (id, status, shipped_at) where shipped_at is NULL for unshipped orders.

Consider these queries to calculate the percentage of shipped orders:

- A: `SELECT COUNT(shipped_at) / COUNT(*) * 100 AS pct_shipped FROM orders;`
- B: `SELECT COUNT(shipped_at) * 100.0 / COUNT(*) AS pct_shipped FROM orders;`
- C: `SELECT AVG(shipped_at) * 100 AS pct_shipped FROM orders;`
- D: `SELECT SUM(shipped_at) / COUNT(*) * 100 AS pct_shipped FROM orders;`

!!! quiz
{
"title": "Question 17: Percentage Calculation",
"question": "Which query correctly calculates the percentage of orders that have been shipped?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

!!! quiz
{
"title": "Question 18: COALESCE Return Value",
"question": "What does COALESCE(bonus, commission, 0) return if bonus is 500, commission is 200?",
"options": ["0", "200", "500", "700"],
"answers": ["500"]
}
!!!

---

Consider these LIKE patterns:

- A: `LIKE '%%%'`
- B: `LIKE '___'`
- C: `LIKE '...'`
- D: `LIKE '[3]'`

!!! quiz
{
"title": "Question 19: Pattern Matching Length",
"question": "Which pattern matches strings that have exactly 3 characters?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

Given this query:

```sql
SELECT
    department,
    AVG(salary) AS avg_salary
FROM employees
GROUP BY department
HAVING AVG(salary) > 50000;
```

!!! quiz
{
"title": "Question 20: Alias in HAVING",
"question": "Why can't we write HAVING avg_salary > 50000 instead?",
"options": ["HAVING doesn't support column aliases", "avg_salary is a reserved keyword", "The alias avg_salary is created in SELECT, which executes after HAVING", "You must always repeat the aggregate function in HAVING"],
"answers": ["The alias avg_salary is created in SELECT, which executes after HAVING"]
}
!!!

---

Consider these queries to find duplicate email addresses:

Option A:

```sql
SELECT email FROM users WHERE COUNT(email) > 1;
```

Option B:

```sql
SELECT email, COUNT(*) FROM users GROUP BY email HAVING COUNT(*) > 1;
```

Option C:

```sql
SELECT DISTINCT email FROM users WHERE email IS NOT NULL;
```

Option D:

```sql
SELECT email FROM users GROUP BY email WHERE COUNT(*) > 1;
```

!!! quiz
{
"title": "Question 21: Finding Duplicates",
"question": "Which query finds duplicate email addresses in the users table?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

Consider this CASE expression when `status` is 'pending':

```sql
CASE
    WHEN status = 'completed' THEN 'Done'
    WHEN status = 'processing' THEN 'In Progress'
    ELSE 'Waiting'
END
```

!!! quiz
{
"title": "Question 22: CASE Expression",
"question": "What does this CASE expression return when status is 'pending'?",
"options": ["'pending'", "NULL", "'Waiting'", "An error because 'pending' is not handled"],
"answers": ["'Waiting'"]
}
!!!

---

Consider these queries to count unique customers in January 2026:

Option A:

```sql
SELECT COUNT(customer_id) FROM orders
WHERE created_at BETWEEN '2026-01-01' AND '2026-01-31';
```

Option B:

```sql
SELECT COUNT(DISTINCT customer_id) FROM orders
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

Option C:

```sql
SELECT DISTINCT COUNT(customer_id) FROM orders
WHERE created_at BETWEEN '2026-01-01' AND '2026-01-31';
```

Option D:

```sql
SELECT SUM(DISTINCT customer_id) FROM orders
WHERE created_at >= '2026-01-01' AND created_at < '2026-02-01';
```

!!! quiz
{
"title": "Question 23: COUNT DISTINCT",
"question": "Which query correctly counts how many unique customers placed orders in January 2026?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

### Question 24

Consider this query:

```sql
SELECT
    category,
    product_name,
    COUNT(*) AS total
FROM products
GROUP BY category;
```

!!! quiz
{
"title": "Question 24: GROUP BY Validation",
"question": "What is wrong with this query?",
"options": ["COUNT(*) cannot be used with GROUP BY", "product_name is in SELECT but not in GROUP BY or an aggregate function", "category should be in the HAVING clause", "Nothing is wrong, this query is valid"],
"answers": ["product_name is in SELECT but not in GROUP BY or an aggregate function"]
}
!!!

---

!!! quiz
{
"title": "Question 25: Case-Insensitive Matching",
"question": "Which operator should you use for case-insensitive pattern matching in PostgreSQL?",
"options": ["LIKE", "SIMILAR TO", "ILIKE", "LOWER LIKE"],
"answers": ["ILIKE"]
}
!!!

---

## PART C: SQL Translation

---

### Question 26 - Requirement → SQL

**Requirement:** Find all employees whose salary is between $40,000 and $80,000 (inclusive), work in either the 'Engineering' or 'Marketing' department, and have a non-null manager_id.

Option A:

```sql
SELECT * FROM employees
WHERE salary BETWEEN 40000 AND 80000
  AND department IN ('Engineering', 'Marketing')
  AND manager_id IS NOT NULL;
```

Option B:

```sql
SELECT * FROM employees
WHERE salary >= 40000 OR salary <= 80000
  AND department = 'Engineering' OR department = 'Marketing'
  AND manager_id != NULL;
```

Option C:

```sql
SELECT * FROM employees
WHERE salary BETWEEN 40000 AND 80000
  OR department IN ('Engineering', 'Marketing')
  OR manager_id IS NOT NULL;
```

Option D:

```sql
SELECT * FROM employees
WHERE salary > 40000 AND salary < 80000
  AND department IN ('Engineering', 'Marketing')
  AND manager_id IS NOT NULL;
```

!!! quiz
{
"title": "Question 26: Filtering Employees",
"question": "Which query correctly implements the requirement?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

### Question 27 - SQL → Description

Consider this query:

```sql
SELECT
    CASE
        WHEN age < 18 THEN 'Minor'
        WHEN age < 65 THEN 'Adult'
        ELSE 'Senior'
    END AS age_group,
    COUNT(*) AS user_count,
    AVG(purchase_total) AS avg_spending
FROM users
WHERE is_active = true
GROUP BY
    CASE
        WHEN age < 18 THEN 'Minor'
        WHEN age < 65 THEN 'Adult'
        ELSE 'Senior'
    END
HAVING COUNT(*) > 100;
```

!!! quiz
{
"title": "Question 27: Query Description",
"question": "What does this query do?",
"options": ["Counts all users grouped by exact age, showing only ages with more than 100 users", "Categorizes active users into age groups, counts each group, calculates average spending, and only shows groups with more than 100 users", "Finds the top 100 users by spending in each age category", "Updates user age groups based on their purchase history"],
"answers": ["Categorizes active users into age groups, counts each group, calculates average spending, and only shows groups with more than 100 users"]
}
!!!

---

### Question 28 - Requirement → SQL

**Requirement:** Calculate the total revenue, number of orders, and average order value for each month in 2025, but only show months where total revenue exceeded $10,000.

Option A:

```sql
SELECT
    EXTRACT(MONTH FROM order_date) AS month,
    SUM(amount) AS total_revenue,
    COUNT(*) AS order_count,
    AVG(amount) AS avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025 AND SUM(amount) > 10000
GROUP BY EXTRACT(MONTH FROM order_date);
```

Option B:

```sql
SELECT
    EXTRACT(MONTH FROM order_date) AS month,
    SUM(amount) AS total_revenue,
    COUNT(*) AS order_count,
    AVG(amount) AS avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025
GROUP BY EXTRACT(MONTH FROM order_date)
HAVING SUM(amount) > 10000;
```

Option C:

```sql
SELECT
    EXTRACT(MONTH FROM order_date) AS month,
    SUM(amount) AS total_revenue,
    COUNT(*) AS order_count,
    AVG(amount) AS avg_order_value
FROM orders
GROUP BY EXTRACT(MONTH FROM order_date)
HAVING EXTRACT(YEAR FROM order_date) = 2025 AND SUM(amount) > 10000;
```

Option D:

```sql
SELECT
    EXTRACT(MONTH FROM order_date) AS month,
    total_revenue,
    order_count,
    avg_order_value
FROM orders
WHERE EXTRACT(YEAR FROM order_date) = 2025
HAVING total_revenue > 10000;
```

!!! quiz
{
"title": "Question 28: Monthly Revenue",
"question": "Which query correctly implements the requirement?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

### Question 29 - SQL → Description

Consider this query:

```sql
SELECT product_id, product_name
FROM products
WHERE product_id NOT IN (
    SELECT DISTINCT product_id
    FROM order_items
    WHERE product_id IS NOT NULL
);
```

!!! quiz
{
"title": "Question 29: Subquery Analysis",
"question": "What does this query find?",
"options": ["Products that have been ordered at least once", "Products that have never been ordered", "All distinct products from order_items", "Products with NULL product_id values"],
"answers": ["Products that have never been ordered"]
}
!!!

---

### Question 30 - Requirement → SQL

**Requirement:** Find customers who have placed more than 5 orders AND have spent a total of more than $500, showing their name, order count, and total spent, sorted by total spent descending.

Option A:

```sql
SELECT
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total) AS total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
WHERE COUNT(o.id) > 5 AND SUM(o.total) > 500
GROUP BY c.id, c.name
ORDER BY total_spent DESC;
```

Option B:

```sql
SELECT
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total) AS total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
GROUP BY c.id, c.name
HAVING COUNT(o.id) > 5 AND SUM(o.total) > 500
ORDER BY total_spent DESC;
```

Option C:

```sql
SELECT
    c.name,
    COUNT(o.id) AS order_count,
    SUM(o.total) AS total_spent
FROM customers c, orders o
GROUP BY c.id, c.name
HAVING order_count > 5 AND total_spent > 500
ORDER BY total_spent DESC;
```

Option D:

```sql
SELECT
    c.name,
    order_count,
    total_spent
FROM customers c
JOIN orders o ON c.id = o.customer_id
WHERE order_count > 5 AND total_spent > 500
ORDER BY total_spent DESC;
```

!!! quiz
{
"title": "Question 30: Customer Aggregation",
"question": "Which query correctly implements the requirement?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

## PART D: Additional True or False

---

!!! quiz
{
"title": "Question 31: Boolean Logic",
"question": "NOT (TRUE AND FALSE) evaluates to TRUE.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 32: NOT BETWEEN",
"question": "price NOT BETWEEN 10 AND 50 is equivalent to price < 10 OR price > 50.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 33: SIMILAR TO",
"question": "In PostgreSQL, 'John' SIMILAR TO 'J%' returns TRUE.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 34: IS DISTINCT FROM",
"question": "NULL IS DISTINCT FROM NULL returns TRUE.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 35: SUM with NULLs",
"question": "SUM(column) returns 0 when all values in the column are NULL.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 36: MIN/MAX on Strings",
"question": "MIN() and MAX() can be used on string columns to find alphabetically first/last values.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 37: DATE_TRUNC",
"question": "DATE_TRUNC('month', '2026-03-15 14:30:00') returns '2026-03-01 00:00:00'.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 38: INTERVAL Arithmetic",
"question": "The expression NOW() - INTERVAL '7 days' returns a date exactly one week ago.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

!!! quiz
{
"title": "Question 39: NOT LIKE with NULL",
"question": "In WHERE name NOT LIKE '%test%', rows where name is NULL will be included in the results.",
"options": ["True", "False"],
"answers": ["False"]
}
!!!

---

!!! quiz
{
"title": "Question 40: COUNT with CASE",
"question": "COUNT(CASE WHEN status = 'active' THEN 1 END) counts only rows where status is 'active'.",
"options": ["True", "False"],
"answers": ["True"]
}
!!!

---

## PART E: Additional Multiple Choice

---

Consider these expressions to search for product names containing a literal underscore:

- A: `WHERE name LIKE '%_%'`
- B: `WHERE name LIKE '%\__%' ESCAPE '\'`
- C: `WHERE name LIKE '%!_%' ESCAPE '!'`
- D: `WHERE name LIKE '%[_]%'`

!!! quiz
{
"title": "Question 41: Escaping Special Characters",
"question": "Which expression correctly searches for product names containing a literal underscore character?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

!!! quiz
{
"title": "Question 42: Regex Operator ~_",
"question": "What does the regex operator ~_ do in PostgreSQL?",
"options": ["Matches the pattern case-sensitively", "Matches the pattern case-insensitively", "Negates the regex match", "Performs a partial match only"],
"answers": ["Matches the pattern case-insensitively"]
}
!!!

---

Given these values in column `score`: 10, 20, NULL, 30, NULL.

!!! quiz
{
"title": "Question 43: SUM with NULL Values",
"question": "What does SUM(score) return?",
"options": ["NULL", "60", "12 (average)", "0"],
"answers": ["60"]
}
!!!

---

Consider these queries to find earliest and latest order dates:

- A: `SELECT FIRST(order_date), LAST(order_date) FROM orders;`
- B: `SELECT MIN(order_date), MAX(order_date) FROM orders;`
- C: `SELECT EARLIEST(order_date), LATEST(order_date) FROM orders;`
- D: `SELECT order_date[0], order_date[-1] FROM orders;`

!!! quiz
{
"title": "Question 44: Min/Max Dates",
"question": "Which query correctly finds the earliest and latest order dates?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

Consider this query:

```sql
SELECT
    5 IS DISTINCT FROM NULL,
    NULL IS DISTINCT FROM NULL,
    5 IS NOT DISTINCT FROM 5;
```

!!! quiz
{
"title": "Question 45: IS DISTINCT FROM Results",
"question": "What does this query return?",
"options": ["TRUE, TRUE, TRUE", "TRUE, FALSE, TRUE", "NULL, NULL, TRUE", "TRUE, TRUE, FALSE"],
"answers": ["TRUE, FALSE, TRUE"]
}
!!!

---

Consider these queries to truncate timestamps:

- A: `SELECT WEEK(created_at) FROM orders;`
- B: `SELECT DATE_TRUNC('week', created_at) FROM orders;`
- C: `SELECT EXTRACT(WEEK FROM created_at) FROM orders;`
- D: `SELECT TRUNCATE(created_at, 'week') FROM orders;`

!!! quiz
{
"title": "Question 46: DATE_TRUNC",
"question": "Which query correctly truncates timestamps to the start of each week?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

!!! quiz
{
"title": "Question 47: AGE Function",
"question": "What does AGE('2026-03-15', '2025-01-10') return in PostgreSQL?",
"options": ["The number of days between the dates", "An interval representing the difference (e.g., '1 year 2 mons 5 days')", "The second date subtracted from the first as a timestamp", "An error because AGE requires timestamps"],
"answers": ["An interval representing the difference (e.g., '1 year 2 mons 5 days')"]
}
!!!

---

### Question 48

Consider these queries to count active vs inactive products per category:

Option A:

```sql
SELECT category_id,
    COUNT(is_active = true) AS active,
    COUNT(is_active = false) AS inactive
FROM products GROUP BY category_id;
```

Option B:

```sql
SELECT category_id,
    COUNT(CASE WHEN is_active = true THEN 1 END) AS active,
    COUNT(CASE WHEN is_active = false THEN 1 END) AS inactive
FROM products GROUP BY category_id;
```

Option C:

```sql
SELECT category_id,
    SUM(is_active) AS active,
    SUM(NOT is_active) AS inactive
FROM products GROUP BY category_id;
```

Option D:

```sql
SELECT category_id,
    COUNT(is_active) AS active,
    COUNT(NOT is_active) AS inactive
FROM products GROUP BY category_id;
```

!!! quiz
{
"title": "Question 48: Conditional Counting",
"question": "Which query correctly counts active vs inactive products per category?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

### Question 49

Consider this PostgreSQL-specific query:

```sql
SELECT
    category_id,
    COUNT(*) FILTER (WHERE price > 100) AS expensive_count,
    AVG(price) FILTER (WHERE in_stock = true) AS avg_available_price
FROM products
GROUP BY category_id;
```

!!! quiz
{
"title": "Question 49: FILTER Clause",
"question": "What does this PostgreSQL-specific query do?",
"options": ["Filters the entire result set to only expensive, in-stock products", "Counts expensive products and averages prices of in-stock products separately per category", "Returns an error because FILTER is not valid SQL", "Creates two separate result sets and combines them"],
"answers": ["Counts expensive products and averages prices of in-stock products separately per category"]
}
!!!

---

Consider these patterns to match email addresses from any `.edu` domain:

- A: `WHERE email LIKE '%@%.edu'`
- B: `WHERE email ~ '@.*\.edu$'`
- C: `WHERE email SIMILAR TO '%@%.edu'`
- D: `WHERE email ~* '@[a-z]+\.edu$'`

!!! quiz
{
"title": "Question 50: Regex Email Matching",
"question": "Which pattern matches email addresses from any .edu domain using PostgreSQL regex?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

!!! quiz
{
"title": "Question 51: Boolean Logic with NULL",
"question": "What is the result of NOT (NULL OR FALSE)?",
"options": ["TRUE", "FALSE", "NULL", "An error"],
"answers": ["NULL"]
}
!!!

---

Consider these queries to find orders placed in the last 30 days:

- A: `SELECT * FROM orders WHERE order_date > DATE_SUB(NOW(), 30);`
- B: `SELECT * FROM orders WHERE order_date >= NOW() - INTERVAL '30 days';`
- C: `SELECT * FROM orders WHERE order_date BETWEEN NOW() - 30 AND NOW();`
- D: `SELECT * FROM orders WHERE DATEDIFF(NOW(), order_date) <= 30;`

!!! quiz
{
"title": "Question 52: Date Arithmetic",
"question": "Which query finds all orders placed in the last 30 days?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

Consider this query:

```sql
SELECT
    MAX(price) - MIN(price) AS price_range,
    MAX(created_at) - MIN(created_at) AS time_span
FROM products;
```

!!! quiz
{
"title": "Question 53: Aggregate Arithmetic",
"question": "What does this query calculate?",
"options": ["The difference between the highest and lowest prices, and the interval between newest and oldest products", "The total price and total time of all products", "An error because you cannot subtract dates", "The average price range and time span per product"],
"answers": ["The difference between the highest and lowest prices, and the interval between newest and oldest products"]
}
!!!

---

!!! quiz
{
"title": "Question 54: NOT LIKE with NULL",
"question": "Which statement about NOT LIKE with NULL values is correct?",
"options": ["NULL NOT LIKE '%test%' returns TRUE", "NULL NOT LIKE '%test%' returns FALSE", "NULL NOT LIKE '%test%' returns NULL", "NULL NOT LIKE '%test%' causes an error"],
"answers": ["NULL NOT LIKE '%test%' returns NULL"]
}
!!!

---

!!! quiz
{
"title": "Question 55: TO_CHAR Day Format",
"question": "What does TO_CHAR(created_at, 'Day') return for a date that falls on Monday?",
"options": ["1", "'Mon'", "'Monday ' (padded with spaces)", "'MONDAY'"],
"answers": ["'Monday ' (padded with spaces)"]
}
!!!
