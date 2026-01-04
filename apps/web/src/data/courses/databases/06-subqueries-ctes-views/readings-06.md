# Week 06 Readings & Quiz Coverage

## Required Readings

1. [Subqueries & Set Operations](subqueries-and-set-operations.md)
2. [CTEs & Views](ctes-and-views.md)

---

## Quiz Coverage Analysis

### From [subqueries-and-set-operations.md](subqueries-and-set-operations.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| What is a Subquery? | What is a Subquery? | Implicit |
| Scalar Subqueries (one row, one column) | Scalar Subqueries | Q1, Q16, Q17 |
| Scalar Subquery in SELECT | In SELECT Clause | Q16 |
| Scalar Subquery in WHERE | In WHERE Clause | Q16, Q35 |
| Scalar Subquery Errors (multiple rows) | Scalar Subquery Errors | Q17 |
| IN Subquery | Subqueries in WHERE with IN | Q10 |
| Basic IN Subquery | Basic IN Subquery | Q10 |
| NOT IN | NOT IN | Q18 |
| NOT IN with NULL problem | Warning box | Q2, Q18, Q19 |
| EXISTS | Subqueries with EXISTS | Q34 |
| Basic EXISTS | Basic EXISTS | Q34 |
| NOT EXISTS | NOT EXISTS | Q19 |
| EXISTS vs IN Comparison | EXISTS vs IN table | Q19 |
| ANY (SOME) | ANY (SOME) | Q10 |
| = ANY = IN | ANY/ALL Comparison Table | Q10 |
| ALL | ALL | Q11, Q35 |
| > ALL = > MAX | ANY/ALL Comparison Table | Q11, Q35 |
| Non-Correlated Subquery | Non-Correlated Subquery | Q20 |
| Correlated Subquery | Correlated Subquery | Q3, Q20 |
| Correlated Subquery Examples | Correlated Subquery Examples | Q41, Q45 |
| Performance Consideration | Performance Consideration | Q20 |
| Derived Tables (FROM clause) | Subqueries in FROM | Q12 |
| Derived Table Alias Requirement | Note about aliases | Q12 |
| UNION | UNION | Q4, Q36 |
| UNION removes duplicates | UNION description | Q4, Q36 |
| UNION ALL | UNION ALL | Q4, Q22 |
| UNION ALL keeps duplicates | UNION ALL description | Q4, Q22 |
| UNION vs UNION ALL Performance | When to Use table | Q22 |
| INTERSECT | INTERSECT | Q21, Q38 |
| EXCEPT | EXCEPT | Q13, Q42 |
| Set Operations Requirements | Requirements | Implicit |
| Set Operations with ORDER BY | Set Operations with ORDER BY | Implicit |
| Subquery Best Practices | Subquery Best Practices | Q19, Q20 |

### From [ctes-and-views.md](ctes-and-views.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| CTE Definition | Common Table Expressions (CTEs) | Q23 |
| CTE Basic Syntax (WITH clause) | Basic Syntax | Q23, Q37 |
| Simple CTE Example | Simple CTE Example | Q37 |
| CTE Temporary Nature | CTE description | Q5 |
| Multiple CTEs (comma-separated) | Multiple CTEs | Q24 |
| CTEs Referencing Other CTEs | CTEs Referencing Other CTEs | Q37 |
| CTE vs Subquery Comparison | CTE vs Subquery table | Q23 |
| When to Use CTEs | When to Use CTEs | Q23 |
| Recursive CTE Syntax | Recursive CTEs | Q6 |
| WITH RECURSIVE | Syntax | Q6 |
| Anchor Member (base case) | Anchor member | Q25 |
| Recursive Member | Recursive member | Q6, Q26 |
| UNION ALL in Recursive CTE | Syntax | Q6 |
| Employee Hierarchy Example | Employee Hierarchy Example | Q39 |
| Category Hierarchy | Category Hierarchy | Implicit |
| Generating Series (numbers) | Generating Series | Q26 |
| Generating Date Series | Generate date series | Implicit |
| Finding All Descendants | Finding All Descendants | Q39 |
| Preventing Infinite Loops | Preventing Infinite Loops | Q27 |
| Depth Limit | Depth limit | Q27 |
| PostgreSQL Cycle Detection | PostgreSQL 14+ cycle detection | Implicit |
| CREATE VIEW Syntax | Creating Views | Q40 |
| View Definition | Views description | Q28 |
| View Benefits | View Benefits | Q28, Q40 |
| Complex View Example | Complex View Example | Q40 |
| CREATE OR REPLACE VIEW | Replacing Views | Implicit |
| DROP VIEW | Dropping Views | Implicit |
| Views Execute Query Each Time | Views Don't Cache | Q7, Q28, Q43 |
| Updatable Views | Updatable Views | Q8, Q29 |
| Updatable View Requirements | Requirements for Updatable Views | Q29 |
| Updatable View Example | Updatable View Example | Implicit |
| WITH CHECK OPTION | WITH CHECK OPTION | Q14 |
| Materialized View Definition | Materialized Views | Q30, Q31 |
| Materialized View Stores Data | Materialized view description | Q30 |
| CREATE MATERIALIZED VIEW | Creating Materialized Views | Q44 |
| REFRESH MATERIALIZED VIEW | Refreshing Materialized Views | Q9, Q32 |
| Manual Refresh | Refresh Strategies table | Q32 |
| REFRESH CONCURRENTLY | Concurrent refresh | Q33, Q44 |
| Unique Index for CONCURRENTLY | Concurrent refresh note | Q44 |
| Refresh Strategies | Refresh Strategies table | Q31 |
| Materialized View vs View | Materialized View vs Regular View table | Q30, Q31 |
| Indexing Materialized Views | Indexing Materialized Views | Q15, Q44 |
| View Security (Column-Level) | Column-Level Security | Q40 |
| View Security (Row-Level) | Row-Level Security | Q40 |
| Schema-Based Access | Schema-Based Access | Implicit |
| Performance Considerations | Performance Considerations | Q31 |

---

## Topic-to-Question Quick Reference

| Question | Topic(s) Covered |
|:--------:|------------------|
| Q1 | Scalar subquery must return exactly one value |
| Q2 | NOT IN vs NOT EXISTS with NULLs |
| Q3 | Correlated subquery runs once per outer row |
| Q4 | UNION removes duplicates; UNION ALL keeps them |
| Q5 | CTEs exist only for duration of query |
| Q6 | Recursive CTE requires anchor + recursive member |
| Q7 | Regular views don't store data physically |
| Q8 | Not all views are updatable |
| Q9 | Materialized views require manual REFRESH |
| Q10 | = ANY is equivalent to IN |
| Q11 | > ALL means greater than maximum |
| Q12 | Derived tables must have an alias |
| Q13 | EXCEPT returns first-only rows |
| Q14 | WITH CHECK OPTION prevents disappearing rows |
| Q15 | Materialized views can have indexes |
| Q16 | Scalar subquery in WHERE for above-average |
| Q17 | Scalar subquery error when returning multiple rows |
| Q18 | NOT IN with NULL returns no rows |
| Q19 | NOT EXISTS handles NULLs correctly |
| Q20 | Correlated vs non-correlated difference |
| Q21 | INTERSECT returns rows in both sets |
| Q22 | UNION ALL is fastest (no duplicate check) |
| Q23 | CTE purpose: temporary named result set |
| Q24 | Multiple CTEs separated by commas |
| Q25 | Anchor member is the base case |
| Q26 | Recursive CTE generates sequence 1-5 |
| Q27 | Depth limit prevents infinite loops |
| Q28 | Views run query each time accessed |
| Q29 | Updatable view requirements |
| Q30 | Materialized view stores data; view doesn't |
| Q31 | Use materialized view for expensive queries |
| Q32 | REFRESH MATERIALIZED VIEW updates data |
| Q33 | CONCURRENTLY allows reads during refresh |
| Q34 | EXISTS with correlated condition |
| Q35 | > ALL means greater than all values (max) |
| Q36 | UNION combines and removes duplicates |
| Q37 | CTE with JOIN and filter |
| Q38 | INTERSECT for products in both months |
| Q39 | Recursive CTE for employee hierarchy |
| Q40 | CREATE VIEW for security/abstraction |
| Q41 | Correlated subquery finds max per category |
| Q42 | EXCEPT for customer-only emails |
| Q43 | Regular view returns fresh data each time |
| Q44 | CONCURRENTLY requires unique index |
| Q45 | Correlated subquery for per-customer average |

---

## Learning Objectives Alignment

After completing this week's readings and quiz, students should be able to:

1. **Write scalar subqueries** and understand their constraints
2. **Use IN, EXISTS, ANY, ALL** appropriately for filtering
3. **Distinguish correlated from non-correlated** subqueries
4. **Handle NULL safely** in NOT IN vs NOT EXISTS scenarios
5. **Apply set operations** (UNION, INTERSECT, EXCEPT) correctly
6. **Write CTEs** for improved query readability
7. **Implement recursive CTEs** for hierarchical data
8. **Create and use views** for abstraction and security
9. **Understand updatable views** and WITH CHECK OPTION
10. **Use materialized views** for performance optimization
11. **Refresh materialized views** with appropriate strategies
