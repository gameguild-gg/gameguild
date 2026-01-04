# Week 03 Readings & Quiz Coverage

## Required Readings

1. [Filtering & Pattern Matching](filtering-and-pattern-matching.md)
2. [Aggregation & Grouping](aggregation-and-grouping.md)

---

## Quiz Coverage Analysis

### From [filtering-and-pattern-matching.md](filtering-and-pattern-matching.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| Boolean Logic (AND, OR, NOT) | Boolean Logic | Q6, Q26, Q31, Q51 |
| Truth Tables | AND/OR/NOT Truth Tables | Q31, Q51 |
| Operator Precedence | Operator Precedence | Q6 |
| IN Operator | The IN Operator | Q26, Q29 |
| NOT IN | NOT IN | Q10, Q29 |
| NOT IN with NULL pitfall | Warning box | Q10, Q29 |
| BETWEEN Operator | The BETWEEN Operator | Q5, Q26 |
| NOT BETWEEN | NOT BETWEEN | Q32 |
| LIKE Pattern Matching (%, _) | Pattern Matching with LIKE | Q11, Q19 |
| Case Sensitivity (LIKE vs ILIKE) | Case Sensitivity | Q4, Q25 |
| NOT LIKE | NOT LIKE | Q39, Q54 |
| Escaping Wildcards (ESCAPE) | Escaping Wildcards | Q41 |
| SIMILAR TO | SIMILAR TO and Regular Expressions | Q33 |
| POSIX Regex (~, ~*) | POSIX Regular Expressions | Q42, Q50 |
| IS NULL / IS NOT NULL | NULL Handling | Q1, Q13, Q26 |
| NULL in Comparisons | NULL in Comparisons | Q1 |
| COALESCE | COALESCE | Q9, Q18 |
| NULLIF | NULLIF | Q16 |
| IS DISTINCT FROM | NULL-safe Comparison | Q34, Q45 |
| CASE Expressions (Simple) | Simple CASE | Q22 |
| CASE Expressions (Searched) | Searched CASE | Q22, Q27 |
| CASE in ORDER BY | CASE in ORDER BY | Implicit |
| CASE in WHERE | CASE in WHERE | Implicit |
| Date/Time Functions | Date/Time Functions | Q23, Q28 |
| EXTRACT | Extracting Date Parts | Q28 |
| DATE_TRUNC | Date Truncation | Q37, Q46 |
| INTERVAL Arithmetic | Date Arithmetic | Q38, Q52 |
| AGE() Function | Age between dates | Q47 |
| TO_CHAR | Date Formatting | Q55 |

### From [aggregation-and-grouping.md](aggregation-and-grouping.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| COUNT(*) vs COUNT(column) | COUNT | Q3, Q12 |
| COUNT(DISTINCT) | COUNT(DISTINCT column) | Q23 |
| SUM | SUM | Q35, Q43 |
| SUM with NULL | Handling NULL in SUM | Q35 |
| AVG | AVG | Q7 |
| AVG vs SUM/COUNT | AVG vs SUM/COUNT | Q7 |
| AVG with NULL | Average with NULL Consideration | Q7 |
| MIN and MAX | MIN and MAX | Q36, Q44, Q53 |
| MIN/MAX on Strings | String MIN/MAX | Q36 |
| MIN/MAX on Dates | Date MIN/MAX | Q44 |
| GROUP BY | GROUP BY | Q14, Q21, Q24, Q27, Q28, Q30 |
| GROUP BY with Expressions | GROUP BY with Expressions | Q27 |
| Multiple Group Columns | Multiple Group Columns | Q28 |
| HAVING | HAVING | Q2, Q14, Q15, Q20, Q28, Q30 |
| WHERE vs HAVING | WHERE vs HAVING | Q8, Q14, Q15, Q20 |
| HAVING with Multiple Conditions | HAVING with Multiple Conditions | Q30 |
| Finding Duplicates | Common HAVING Patterns | Q21 |
| DISTINCT in Aggregates | DISTINCT in Aggregates | Q23 |
| Query Execution Order | Query Execution Order | Q8, Q20 |
| Conditional Aggregation (CASE) | Pivot-like Aggregation | Q40, Q48 |
| FILTER Clause (PostgreSQL) | Conditional Aggregation | Q49 |

---

## Topic-to-Question Quick Reference

| Question | Topic(s) Covered |
|:--------:|------------------|
| Q1 | NULL = NULL returns NULL, not TRUE |
| Q2 | HAVING can be used without GROUP BY |
| Q3 | COUNT(*) vs COUNT(column) difference |
| Q4 | LIKE is case-sensitive in PostgreSQL |
| Q5 | BETWEEN is inclusive |
| Q6 | AND has higher precedence than OR |
| Q7 | AVG ignores NULL values |
| Q8 | Column aliases not available in WHERE |
| Q9 | COALESCE returns first non-NULL |
| Q10 | NOT IN with NULL returns no rows |
| Q11 | LIKE pattern matching (Pro%Plus) |
| Q12 | COUNT(*) vs COUNT(column) interpretation |
| Q13 | IS NULL correct syntax |
| Q14 | WHERE + GROUP BY + HAVING combination |
| Q15 | WHERE filters rows, HAVING filters groups |
| Q16 | NULLIF returns NULL when values equal |
| Q17 | Percentage calculation with COUNT |
| Q18 | COALESCE returns first non-NULL value |
| Q19 | Underscore (_) matches exactly one character |
| Q20 | HAVING can't use SELECT aliases (execution order) |
| Q21 | Finding duplicates with GROUP BY + HAVING |
| Q22 | CASE ELSE clause behavior |
| Q23 | COUNT(DISTINCT) for unique values |
| Q24 | Non-aggregated columns must be in GROUP BY |
| Q25 | ILIKE for case-insensitive matching |
| Q26 | BETWEEN + IN + IS NOT NULL combination |
| Q27 | CASE expression with GROUP BY |
| Q28 | EXTRACT + GROUP BY + HAVING for monthly data |
| Q29 | NOT IN subquery for finding exclusions |
| Q30 | JOIN + GROUP BY + HAVING + ORDER BY |
| Q31 | NOT (TRUE AND FALSE) = TRUE |
| Q32 | NOT BETWEEN equivalent expression |
| Q33 | SIMILAR TO pattern matching |
| Q34 | IS DISTINCT FROM treats NULLs as equal |
| Q35 | SUM returns NULL when all values are NULL |
| Q36 | MIN/MAX on strings (alphabetical) |
| Q37 | DATE_TRUNC truncates to period start |
| Q38 | INTERVAL arithmetic for relative dates |
| Q39 | NOT LIKE with NULL returns NULL |
| Q40 | Conditional COUNT with CASE |
| Q41 | Escaping wildcards with ESCAPE clause |
| Q42 | ~* is case-insensitive regex in PostgreSQL |
| Q43 | SUM ignores NULL values |
| Q44 | MIN/MAX for earliest/latest dates |
| Q45 | IS DISTINCT FROM behavior |
| Q46 | DATE_TRUNC('week', ...) for week start |
| Q47 | AGE() returns interval |
| Q48 | Conditional aggregation with CASE in COUNT |
| Q49 | FILTER clause for conditional aggregation |
| Q50 | Regex pattern matching with ~ |
| Q51 | NOT with NULL returns NULL |
| Q52 | INTERVAL for date filtering |
| Q53 | MAX - MIN for range calculation |
| Q54 | NOT LIKE with NULL returns NULL |
| Q55 | TO_CHAR date formatting |

---

## Learning Objectives Alignment

After completing this week's readings and quiz, students should be able to:

1. **Apply boolean logic** correctly with AND, OR, NOT operators
2. **Use pattern matching** with LIKE, ILIKE, and regex operators
3. **Handle NULL values** properly with IS NULL, COALESCE, NULLIF
4. **Write CASE expressions** for conditional logic in queries
5. **Use aggregate functions** (COUNT, SUM, AVG, MIN, MAX) correctly
6. **Understand GROUP BY and HAVING** for data summarization
7. **Distinguish WHERE from HAVING** based on query execution order
8. **Work with dates** using EXTRACT, DATE_TRUNC, INTERVAL, AGE
