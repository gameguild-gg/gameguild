# Quiz: Indexing Fundamentals (Week 04)

## Instructions

This quiz tests your understanding of **indexes** and their creation in SQL.

---

## Moved from Week 02 DDL/DML/DQL Quiz

### Question 1 - Creating Indexes

**Requirement:** The database administrator needs to:
1. Add a unique constraint on the `email` column in the `users` table
2. Create an index on the `last_login` column for faster queries
3. Rename the column `user_name` to `username`

**Which SQL statements correctly implement ALL requirements?**

- [ ] A.
```sql
ALTER TABLE users ADD CONSTRAINT unique_email UNIQUE (email);
CREATE INDEX idx_users_last_login ON users (last_login);
ALTER TABLE users RENAME COLUMN user_name TO username;
```

- [ ] B.
```sql
UPDATE users SET email = UNIQUE(email);
INSERT INDEX idx_users_last_login ON users (last_login);
UPDATE users SET user_name = 'username';
```

- [ ] C.
```sql
CREATE UNIQUE email ON users;
CREATE INDEX last_login ON users;
RENAME user_name TO username IN users;
```

- [ ] D.
```sql
ALTER TABLE users ADD UNIQUE (email);
ALTER TABLE users ADD INDEX (last_login);
ALTER TABLE users CHANGE user_name username;
```

---

## Answer Key (Instructor Only)

| Question | Answer | Explanation |
|:--------:|:------:|-------------|
| 1 | **A** | Uses correct PostgreSQL syntax: `ADD CONSTRAINT UNIQUE`, `CREATE INDEX`, and `RENAME COLUMN`. Option D mixes MySQL syntax |
