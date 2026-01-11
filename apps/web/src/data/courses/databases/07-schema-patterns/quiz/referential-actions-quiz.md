# Quiz: Schema Patterns & Referential Actions (Week 07)

## Instructions

This quiz tests your understanding of **ON DELETE/UPDATE actions**, **CASCADE options**, and **advanced constraint patterns**.

---

## Moved from Week 02 Constraints Quiz

### Question 1 - ON DELETE SET NULL

**Consider the following table:**

```sql
CREATE TABLE products (
    id INT PRIMARY KEY,
    sku VARCHAR(50) UNIQUE,
    name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL CHECK (price > 0),
    category_id INT REFERENCES categories(id) ON DELETE SET NULL
);
```

**What happens when a category is deleted from the `categories` table?**

- [ ] A. All products in that category are also deleted
- [ ] B. The delete operation fails if any products reference that category
- [ ] C. Products in that category have their `category_id` set to `NULL`
- [ ] D. Products in that category have their `category_id` set to `0`

---

### Question 2 - ON DELETE CASCADE

**Which `ON DELETE` action should you use when deleting a parent record should also delete all related child records?**

- [ ] A. `ON DELETE RESTRICT`
- [ ] B. `ON DELETE SET NULL`
- [ ] C. `ON DELETE CASCADE`
- [ ] D. `ON DELETE NO ACTION`

---

## Answer Key (Instructor Only)

| Question | Answer | Explanation |
|:--------:|:------:|-------------|
| 1 | **C** | `ON DELETE SET NULL` sets the FK column to NULL when the referenced row is deleted. |
| 2 | **C** | `ON DELETE CASCADE` automatically deletes child rows when parent is deleted. |
