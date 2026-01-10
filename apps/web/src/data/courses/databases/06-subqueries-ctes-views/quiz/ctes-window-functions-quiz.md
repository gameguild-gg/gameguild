# Quiz: CTEs & Window Functions (Week 06)

## Instructions

This quiz tests your understanding of **Common Table Expressions (CTEs)** and **window functions** in SQL.

---

## Moved from Week 02 DDL/DML/DQL Quiz

### Question 1 - SQL → Description (CTE + Window Functions)

**Given this SQL statement:**

```sql
WITH monthly_sales AS (
    SELECT 
        DATE_TRUNC('month', order_date) AS month,
        SUM(total_amount) AS revenue
    FROM orders
    WHERE order_date >= '2025-01-01'
    GROUP BY DATE_TRUNC('month', order_date)
)
SELECT 
    month,
    revenue,
    revenue - LAG(revenue) OVER (ORDER BY month) AS month_over_month_change,
    ROUND(100.0 * (revenue - LAG(revenue) OVER (ORDER BY month)) / LAG(revenue) OVER (ORDER BY month), 2) AS pct_change
FROM monthly_sales
ORDER BY month;
```

**What does this query produce?**

- [ ] A. A list of all orders from 2025 grouped by customer

- [ ] B. Monthly revenue totals for 2025+ with the difference and percentage change compared to the previous month

- [ ] C. The total revenue for each product category in 2025

- [ ] D. A running total of all orders placed since January 2025

---

## Answer Key (Instructor Only)

| Question | Answer | Explanation |
|:--------:|:------:|-------------|
| 1 | **B** | The CTE calculates monthly totals, then `LAG()` window function computes month-over-month changes and percentage differences |
