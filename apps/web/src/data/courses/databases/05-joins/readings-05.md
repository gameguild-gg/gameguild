# Week 05 Readings & Quiz Coverage

## Required Readings

1. [Join Fundamentals](join-fundamentals.md)
2. [Outer Joins & Advanced Patterns](outer-joins-and-advanced-patterns.md)

---

## Quiz Coverage Analysis

### From [join-fundamentals.md](join-fundamentals.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| Why Joins? (normalized data) | Why Joins? | Implicit in all join questions |
| INNER JOIN Syntax | Basic Syntax | Q1, Q2, Q16 |
| JOIN = INNER JOIN | Basic Syntax note | Q1 |
| Table Aliases | Table Aliases | Q17 |
| Alias Best Practices | Alias Best Practices | Q17 |
| Join Conditions (ON clause) | Join Conditions | Q17, Q21, Q29 |
| Equality Condition | Equality Condition | Q17 |
| Multiple Conditions in ON | Multiple Conditions | Q21 |
| Non-Equality Conditions (BETWEEN, >) | Non-Equality Conditions | Q41, Q42, Q43, Q46 |
| Multi-Table Joins | Multi-Table Joins | Q34, Q37, Q39 |
| Join Order | Join Order | Q9 |
| Joining on Multiple Columns | Joining on Multiple Columns | Implicit |
| USING Clause | USING Clause | Q11, Q29 |
| NATURAL JOIN (and why to avoid) | NATURAL JOIN | Q7, Q23 |
| Filtering Joined Data | Filtering Joined Data | Q21, Q38 |
| WHERE vs ON for INNER JOIN | WHERE vs ON for Filtering | Q21 |
| Joins with Aggregations | Joins with Aggregations | Q25, Q34, Q39 |
| Common Mistakes: Cartesian Product | Missing Join Condition | Q12, Q30 |
| Common Mistakes: Ambiguous Columns | Ambiguous Column Names | Q17 |
| Common Mistakes: Wrong Columns | Joining Wrong Columns | Implicit |
| NULL Foreign Keys | Forgetting About NULL Foreign Keys | Q2, Q18 |
| Query Execution Order | Query Execution Order with Joins | Q26 |

### From [outer-joins-and-advanced-patterns.md](outer-joins-and-advanced-patterns.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| Outer Joins Overview | Outer Joins Overview | Q28 |
| LEFT JOIN / LEFT OUTER JOIN | LEFT JOIN | Q3, Q16, Q18, Q25 |
| LEFT JOIN Syntax | Syntax | Q3 |
| LEFT JOIN Visual Example | Visual Example | Q28 |
| Finding Non-Matching Rows | Finding Non-Matching Rows | Q18, Q32 |
| RIGHT JOIN | RIGHT JOIN | Q8 |
| RIGHT JOIN = swapped LEFT JOIN | RIGHT JOIN vs LEFT JOIN | Q8 |
| FULL OUTER JOIN | FULL OUTER JOIN | Q10, Q22, Q28 |
| FULL OUTER JOIN Visual | Visual Example | Q28 |
| MySQL FULL OUTER workaround | Simulating FULL OUTER JOIN | Q15 |
| Self-Joins | Self-Joins | Q6, Q20, Q24, Q33 |
| Self-Join Syntax | Syntax | Q20 |
| Employee-Manager Hierarchy | Employee-Manager Hierarchy | Q20 |
| Comparing Rows (same value) | Comparing Rows Within Same Table | Q24, Q33 |
| `id < id` to prevent duplicates | Note on preventing duplicates | Q24, Q33 |
| Multi-Level Hierarchy | Multi-Level Hierarchy | Implicit in Q20 |
| CROSS JOIN | CROSS JOIN | Q4, Q19, Q35 |
| CROSS JOIN Syntax | Syntax | Q35 |
| CROSS JOIN Use Cases | Use Cases | Q35 |
| CROSS JOIN Warning (large tables) | CROSS JOIN Warning | Q4 |
| Cartesian Product Calculation | Visual Example | Q4, Q19 |
| Venn Diagram Mental Model | Join Visualization Mental Models | Q28 |
| Row Matching Model | Row Matching Model | Q28 |
| WHERE vs ON for Outer Joins | Filtering with Outer Joins | Q5, Q21, Q38 |
| WHERE on LEFT JOINed table | Critical Difference | Q38 |
| Combining Multiple Join Types | Combining Multiple Join Types | Q31 |
| Anti-Joins (LEFT JOIN + NULL) | Anti-Joins | Q18, Q32 |
| Anti-Joins (NOT EXISTS) | NOT EXISTS pattern | Q36 |
| Semi-Joins (EXISTS) | Semi-Joins | Q27, Q40 |
| Performance: Index Join Columns | Performance Considerations | Q44, Q45 |
| Performance: Choose Right Join | Choose the Right Join Type | Implicit |

---

## Topic-to-Question Quick Reference

| Question | Topic(s) Covered |
|:--------:|------------------|
| Q1 | JOIN = INNER JOIN |
| Q2 | INNER JOIN only returns matching rows |
| Q3 | LEFT JOIN = LEFT OUTER JOIN |
| Q4 | CROSS JOIN Cartesian product (100 × 50 = 5000) |
| Q5 | WHERE vs ON in outer joins (critical difference) |
| Q6 | Self-joins don't require self-referencing FK |
| Q7 | NATURAL JOIN matches all same-named columns |
| Q8 | RIGHT JOIN can be rewritten as LEFT JOIN |
| Q9 | INNER JOIN order doesn't affect results |
| Q10 | FULL OUTER JOIN returns all rows from both tables |
| Q11 | USING clause requires identical column names |
| Q12 | Missing join condition creates Cartesian product |
| Q13 | Non-aggregated SELECT columns must be in GROUP BY |
| Q14 | NOT EXISTS often faster than LEFT JOIN anti-pattern |
| Q15 | MySQL doesn't support FULL OUTER JOIN directly |
| Q16 | LEFT JOIN keeps all customers, even without orders |
| Q17 | Ambiguous column names require table qualifiers |
| Q18 | Anti-join pattern: LEFT JOIN + WHERE IS NULL |
| Q19 | CROSS JOIN: 3 × 4 = 12 rows |
| Q20 | Self-join for employee-manager hierarchy |
| Q21 | ON filter preserves left rows; WHERE removes them |
| Q22 | FULL OUTER JOIN keeps all rows with NULLs for non-matches |
| Q23 | NATURAL JOIN pitfall: matches all same-named columns |
| Q24 | Self-join with id < id prevents duplicate pairs |
| Q25 | LEFT JOIN + COALESCE for customers with no orders |
| Q26 | Query execution: FROM + JOINs before WHERE |
| Q27 | EXISTS semi-join returns rows with matches (no duplicates) |
| Q28 | Identify FULL OUTER JOIN from result set |
| Q29 | USING (column_name) correct syntax |
| Q30 | Comma join without condition = Cartesian product |
| Q31 | Mixed join types: INNER + LEFT in same query |
| Q32 | LEFT JOIN + IS NULL = anti-join for empty categories |
| Q33 | Self-join on hire_date with id < id for unique pairs |
| Q34 | Four-table join with aggregation |
| Q35 | CROSS JOIN for all color × size combinations |
| Q36 | NOT EXISTS for customers with no 2026 orders |
| Q37 | INNER JOIN chain ensures all relationships exist |
| Q38 | WHERE on LEFT JOINed table acts like INNER JOIN |
| Q39 | JOIN + GROUP BY + HAVING with COUNT(DISTINCT) |
| Q40 | EXISTS = semi-join pattern |
| Q41 | Join conditions can use non-equality comparisons |
| Q42 | BETWEEN in join condition for range-based join |
| Q43 | Comparison join (hire_date > hire_date) |
| Q44 | Indexes on FK columns improve join performance |
| Q45 | Index join columns AND filter columns |
| Q46 | Range-based join with >= and <= |

---

## Learning Objectives Alignment

After completing this week's readings and quiz, students should be able to:

1. **Understand INNER JOIN** mechanics and when rows are excluded
2. **Use table aliases** effectively for readability
3. **Write multi-table joins** connecting 3+ tables
4. **Choose appropriate join types** (INNER, LEFT, RIGHT, FULL OUTER)
5. **Implement self-joins** for hierarchies and row comparisons
6. **Use CROSS JOIN** appropriately for generating combinations
7. **Distinguish WHERE vs ON** in outer joins
8. **Apply anti-join and semi-join patterns** (NOT EXISTS, EXISTS)
9. **Avoid common mistakes** (Cartesian products, ambiguous columns)
10. **Index join columns** for performance optimization
