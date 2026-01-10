# Week 04 Readings & Quiz Coverage

## Required Readings

Complete the following readings before attempting the Week 04 quiz:

1. [normalization-theory.md](normalization-theory.md) - 1NF, 2NF, 3NF, BCNF, functional dependencies
2. [entity-relationships.md](entity-relationships.md) - ER modeling, cardinality, junction tables, UML notation
3. [indexing-fundamentals.md](indexing-fundamentals.md) - Index types, creation, and performance
4. [practical-normalization.md](practical-normalization.md) - Denormalization trade-offs, real-world schemas, materialized views

---

## Week 04 Quiz Overview

| Quiz | Questions | Format | Topics Covered |
|------|-----------|--------|----------------|
| [normalization-quiz.md](quiz/normalization-quiz.md) | 48 | T/F + MC | Normal forms, ER design, indexing |
| [indexing-quiz.md](quiz/indexing-quiz.md) | 1 | MC | Index creation syntax (legacy) |

**Total: 49 questions**

---

## Quiz Coverage Analysis

### Part A: Normal Forms (Questions 1-15) - From [normalization-theory.md](normalization-theory.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| 1NF definition | First Normal Form (1NF) | Q1, Q2, Q9 |
| Atomic values | First Normal Form (1NF) | Q9 |
| 2NF and partial dependencies | Second Normal Form (2NF) | Q1, Q3, Q10 |
| Composite primary keys | Second Normal Form (2NF) | Q3, Q10 |
| 3NF and transitive dependencies | Third Normal Form (3NF) | Q4, Q6, Q11, Q12, Q13 |
| BCNF | Boyce-Codd Normal Form (BCNF) | Q5, Q15 |
| Functional dependencies | Functional Dependencies | Q7, Q11 |
| Normalization trade-offs | Why Normalize? / Practice | Q8, Q14 |
| Denormalization reasons | From practical-normalization.md | Q14 |

---

### Part B: Entity-Relationships (Questions 16-30) - From [entity-relationships.md](entity-relationships.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| 1:1 relationship | One-to-One (1:1) | Q19, Q29 |
| 1:N relationship | One-to-Many (1:N) | Q16, Q24, Q27 |
| M:N relationship | Many-to-Many (M:N) | Q17, Q23, Q25 |
| Junction tables | Junction Tables (Bridge Tables) | Q17, Q22, Q25 |
| Junction table attributes | Junction Table with Extra Attributes | Q22, Q25 |
| Self-referencing relationships | Self-Referencing Relationships | Q18, Q26, Q30 |
| Hierarchical data | Hierarchical Self-Reference: Categories | Q30 |
| Weak entities | Weak Entities | Q20, Q28 |
| Cardinality notation | ER Diagram Notations | Q21 |
| Crow's Foot notation | Crow's Foot Notation | Q21 |
| Participation constraints | Participation Constraints | Q27 |
| Foreign key placement | One-to-Many (1:N) | Q16, Q24 |
| UNIQUE constraint for 1:1 | One-to-One (1:1) | Q29 |

---

### Part C: Indexing (Questions 31-45) - From [indexing-fundamentals.md](indexing-fundamentals.md)

| Topic | Content Section | Quiz Questions |
|-------|-----------------|----------------|
| Automatic index creation | Automatic Indexes | Q31, Q32 |
| PRIMARY KEY auto-index | Automatic Indexes | Q31 |
| FOREIGN KEY (no auto-index) | Automatic Indexes | Q32 |
| Composite index column order | Multi-Column (Composite) Index | Q33, Q40 |
| Index trade-offs | Performance Impact | Q34 |
| Partial indexes | Partial Index | Q35 |
| Hash indexes | Hash | Q36 |
| Index-only scans | Index-Only Scans (Covering Indexes) | Q37 |
| B-Tree indexes | B-Tree Indexes (Default) | Q38 |
| Index candidates | When to Create Indexes | Q39 |
| EXPLAIN vs EXPLAIN ANALYZE | Analyzing Query Performance | Q41 |
| GIN indexes | GIN (Generalized Inverted Index) | Q42 |
| Index maintenance | Index Maintenance | Q43 |
| Expression indexes | Expression Index | Q44 |
| CREATE INDEX CONCURRENTLY | Concurrent Index Creation | Q45 |

---

### Bonus Section (Questions 46-48) - Integrated Scenarios

| Topic | Sources | Quiz Questions |
|-------|---------|----------------|
| Normalization process | normalization-theory.md | Q46 |
| Index recommendations | indexing-fundamentals.md | Q47 |
| Performance troubleshooting | indexing-fundamentals.md + practical-normalization.md | Q48 |

---

## Topic-to-Question Quick Reference

### From [normalization-theory.md](normalization-theory.md)

| Topic | Questions |
|-------|-----------|
| Why Normalize? (anomalies) | Q8 |
| Functional Dependencies | Q7, Q11 |
| First Normal Form (1NF) | Q1, Q2, Q9 |
| Second Normal Form (2NF) | Q1, Q3, Q10 |
| Third Normal Form (3NF) | Q4, Q6, Q11, Q12, Q13 |
| Boyce-Codd Normal Form (BCNF) | Q5, Q15 |
| Normalization Process | Q46 |

### From [entity-relationships.md](entity-relationships.md)

| Topic | Questions |
|-------|-----------|
| One-to-One (1:1) | Q19, Q29 |
| One-to-Many (1:N) | Q16, Q24, Q27 |
| Many-to-Many (M:N) | Q17, Q23, Q25 |
| Junction Tables | Q17, Q22, Q25 |
| Self-Referencing Relationships | Q18, Q26, Q30 |
| Weak Entities | Q20, Q28 |
| ER Diagram Notations | Q21 |
| Participation Constraints | Q27 |

### From [indexing-fundamentals.md](indexing-fundamentals.md)

| Topic | Questions |
|-------|-----------|
| Why Indexes Matter | Q34 |
| B-Tree Indexes | Q38 |
| Creating Indexes (single, composite) | Q33, Q40 |
| Unique Index | Q31 |
| Partial Index | Q35 |
| Expression Index | Q44 |
| Hash, GIN, BRIN types | Q36, Q42 |
| Automatic Indexes | Q31, Q32 |
| Index-Only Scans | Q37 |
| EXPLAIN ANALYZE | Q41 |
| Index Maintenance | Q43 |
| CREATE INDEX CONCURRENTLY | Q45 |
| When to Index | Q39, Q47 |

### From [practical-normalization.md](practical-normalization.md)

| Topic | Questions |
|-------|-----------|
| When to Denormalize | Q14, Q8 |
| Performance Troubleshooting | Q48 |
| Schema Analysis | Q46 |

---

## Learning Objectives Alignment

After completing the readings and quiz, students should be able to:

### Normalization Theory
- ✅ Define and identify 1NF, 2NF, 3NF, BCNF - **Q1-Q6, Q9-Q10, Q12-Q13, Q15**
- ✅ Identify functional dependencies - **Q7, Q11**
- ✅ Identify partial and transitive dependencies - **Q3, Q4, Q6, Q10, Q11, Q13**
- ✅ Normalize a denormalized table - **Q46**
- ✅ Explain trade-offs of normalization - **Q8, Q14**

### Entity-Relationship Modeling
- ✅ Identify relationship types (1:1, 1:N, M:N) - **Q16-Q17, Q19, Q23-Q24, Q29**
- ✅ Implement relationships with proper FK placement - **Q16, Q24, Q26, Q29**
- ✅ Design junction tables with additional attributes - **Q17, Q22, Q25**
- ✅ Model self-referencing relationships - **Q18, Q26, Q30**
- ✅ Identify weak entities - **Q20, Q28**
- ✅ Read Crow's Foot notation - **Q21**
- ✅ Distinguish mandatory vs optional participation - **Q21, Q27**

### Indexing Fundamentals
- ✅ Understand automatic vs manual index creation - **Q31, Q32**
- ✅ Create effective composite indexes - **Q33, Q40**
- ✅ Understand index trade-offs - **Q34**
- ✅ Use partial and expression indexes - **Q35, Q44**
- ✅ Choose appropriate index types - **Q36, Q38, Q42**
- ✅ Analyze query plans with EXPLAIN - **Q41**
- ✅ Maintain indexes properly - **Q43, Q45**
- ✅ Identify good index candidates - **Q39, Q47**

### Practical Application
- ✅ Make denormalization decisions - **Q14**
- ✅ Design normalized schemas - **Q46**
- ✅ Troubleshoot performance issues - **Q48**

---

## Study Tips

1. **Master the dependency types first** - Understanding partial and transitive dependencies is key to understanding 2NF and 3NF

2. **Draw ER diagrams** - Visualize relationships before writing SQL; use dbdiagram.io or similar tools

3. **Practice the normalization process** - Take a denormalized spreadsheet and normalize it step-by-step

4. **Remember FK placement rule** - In 1:N, the FK goes in the "many" side; in 1:1, use UNIQUE on the FK

5. **Index foreign keys manually** - PostgreSQL doesn't do this automatically (a common exam topic!)

6. **Use EXPLAIN ANALYZE** - Run it on real queries to see how indexes affect execution

7. **Column order matters for composite indexes** - `(A, B)` helps queries on A or (A, B), but not B alone

---

## Time Estimates

| Reading | Estimated Time |
|---------|----------------|
| normalization-theory.md | 25-30 min |
| entity-relationships.md | 25-30 min |
| indexing-fundamentals.md | 20-25 min |
| practical-normalization.md | 20-25 min |
| **Total Readings** | **~90-110 min** |

| Quiz | Estimated Time |
|------|----------------|
| normalization-quiz.md (48 questions) | 45-60 min |
| indexing-quiz.md (1 question) | 2 min |
| **Total Quizzes** | **~50-65 min** |

---

## Additional Resources

### Sample Databases for Practice
- **Sakila** (MySQL) - DVD rental, well-normalized
- **Northwind** (SQL Server) - Classic trading company
- **AdventureWorks** (SQL Server) - Comprehensive ERP schema
- **dvdrental** (PostgreSQL) - Port of Sakila

### ER Diagram Tools
- [dbdiagram.io](https://dbdiagram.io) - DBML-based, free
- [MySQL Workbench](https://www.mysql.com/products/workbench/) - Visual design
- [pgModeler](https://pgmodeler.io/) - PostgreSQL-specific

### PostgreSQL Index Documentation
- [PostgreSQL: Index Types](https://www.postgresql.org/docs/current/indexes-types.html)
- [PostgreSQL: EXPLAIN](https://www.postgresql.org/docs/current/sql-explain.html)
