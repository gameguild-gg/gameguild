# DBML Introduction

**Database Markup Language (DBML)** is a simple, readable DSL (Domain-Specific Language) designed specifically for defining and documenting database schemas. It provides a clean syntax that's easier to read and write than raw DDL, while being tool-agnostic and convertible to multiple database systems.

## Why DBML?

| Challenge with Raw DDL | DBML Solution |
|------------------------|---------------|
| Verbose syntax | Concise, readable format |
| No visual representation | Integrates with diagram tools |
| Database-specific | Generates SQL for multiple databases |
| Hard to collaborate on | Human-readable, git-friendly |
| Documentation separate | Inline notes and comments |

## DBML vs SQL DDL Comparison

**SQL DDL:**
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    content TEXT,
    author_id INTEGER NOT NULL REFERENCES users(id),
    published_at TIMESTAMP
);
```

**DBML Equivalent:**
```dbml
Table users {
  id serial [pk]
  username varchar(50) [not null, unique]
  email varchar(255) [not null]
  created_at timestamp [default: `now()`]
}

Table posts {
  id serial [pk]
  title varchar(200) [not null]
  content text
  author_id integer [not null, ref: > users.id]
  published_at timestamp
}
```

---

## DBML Syntax

### Tables

```dbml
Table table_name {
  column_name data_type [constraints]
}
```

**Example:**
```dbml
Table products {
  id serial [pk]
  name varchar(100) [not null]
  price decimal(10,2) [not null]
  description text
  created_at timestamp [default: `now()`]
}
```

### Data Types

DBML supports common SQL data types:

| Category | Types |
|----------|-------|
| **Numeric** | `integer`, `serial`, `bigint`, `decimal(p,s)`, `float`, `double` |
| **Text** | `varchar(n)`, `char(n)`, `text` |
| **Date/Time** | `date`, `time`, `timestamp`, `timestamptz` |
| **Boolean** | `boolean`, `bool` |
| **Binary** | `blob`, `bytea` |
| **Special** | `uuid`, `json`, `jsonb` |

### Column Constraints

Constraints are specified in square brackets `[]`:

```dbml
Table users {
  id serial [pk]                           // Primary key
  email varchar(255) [unique, not null]    // Multiple constraints
  role varchar(20) [default: 'user']       // Default value
  age integer [note: 'Must be 18+']        // Documentation
  status varchar(20) [null]                // Explicitly nullable
}
```

| Constraint | Syntax | Description |
|------------|--------|-------------|
| Primary Key | `[pk]` or `[primary key]` | Unique identifier |
| Not Null | `[not null]` | Required field |
| Unique | `[unique]` | No duplicates |
| Default | `[default: value]` | Default value |
| Note | `[note: 'text']` | Documentation |
| Increment | `[increment]` | Auto-increment |

### Composite Primary Keys

```dbml
Table order_items {
  order_id integer [not null]
  product_id integer [not null]
  quantity integer [not null, default: 1]
  
  indexes {
    (order_id, product_id) [pk]  // Composite primary key
  }
}
```

---

## Relationships

DBML uses `ref:` to define foreign key relationships. The symbols indicate cardinality:

| Symbol | Meaning | Relationship |
|--------|---------|--------------|
| `>` | Many-to-One | Many of this → One of that |
| `<` | One-to-Many | One of this → Many of that |
| `-` | One-to-One | One of this → One of that |
| `<>` | Many-to-Many | Many of this ↔ Many of that |

### Inline References

```dbml
Table posts {
  id serial [pk]
  author_id integer [ref: > users.id]      // posts.author_id → users.id
  category_id integer [ref: > categories.id]
}
```

### Standalone References

```dbml
Table users {
  id serial [pk]
  username varchar(50)
}

Table posts {
  id serial [pk]
  author_id integer
}

// Define relationships separately
Ref: posts.author_id > users.id
```

### Relationship Examples

**One-to-Many (Most Common):**
```dbml
// One user has many posts
Table users {
  id serial [pk]
}

Table posts {
  id serial [pk]
  author_id integer [ref: > users.id]  // Many posts → One user
}
```

**One-to-One:**
```dbml
// One user has one profile
Table users {
  id serial [pk]
}

Table profiles {
  id serial [pk]
  user_id integer [unique, ref: - users.id]  // One profile - One user
}
```

**Many-to-Many (Junction Table):**
```dbml
Table students {
  id serial [pk]
  name varchar(100)
}

Table courses {
  id serial [pk]
  title varchar(200)
}

// Junction table for M:N relationship
Table enrollments {
  id serial [pk]
  student_id integer [ref: > students.id]
  course_id integer [ref: > courses.id]
  enrolled_at timestamp [default: `now()`]
  
  indexes {
    (student_id, course_id) [unique]  // Prevent duplicate enrollments
  }
}
```

### Referential Actions (ON DELETE / ON UPDATE)

```dbml
Ref: posts.author_id > users.id [delete: cascade, update: cascade]
Ref: comments.post_id > posts.id [delete: set null]
Ref: order_items.order_id > orders.id [delete: restrict]
```

| Action | Behavior |
|--------|----------|
| `cascade` | Delete/update child rows |
| `set null` | Set FK to NULL |
| `set default` | Set FK to default value |
| `restrict` | Prevent delete/update |
| `no action` | Similar to restrict |

---

## Indexes

```dbml
Table users {
  id serial [pk]
  email varchar(255) [unique]
  first_name varchar(50)
  last_name varchar(50)
  created_at timestamp
  
  indexes {
    email                              // Single column index
    (first_name, last_name)            // Composite index
    created_at [name: 'idx_created']   // Named index
    email [unique]                     // Unique index
    (last_name, first_name) [type: btree]  // B-tree index
  }
}
```

---

## Enums

```dbml
Enum order_status {
  pending
  processing
  shipped
  delivered
  cancelled
}

Table orders {
  id serial [pk]
  status order_status [default: 'pending']
}
```

---

## Table Groups

Organize related tables visually:

```dbml
TableGroup ecommerce {
  users
  orders
  order_items
  products
}

TableGroup content {
  posts
  comments
  categories
}
```

---

## Notes and Documentation

### Table Notes

```dbml
Table users [note: 'Stores all registered users'] {
  id serial [pk]
  email varchar(255) [note: 'Must be unique and valid email format']
}
```

### Multi-line Notes

```dbml
Table transactions {
  id serial [pk]
  amount decimal(10,2)
  
  Note: '''
    Financial transactions table.
    - All amounts stored in cents
    - Supports refunds via negative amounts
    - Immutable: never update, only insert
  '''
}
```

---

## Complete Schema Example: E-Commerce

```dbml
// Enums
Enum order_status {
  pending
  paid
  shipped
  delivered
  cancelled
}

// Users & Authentication
Table users {
  id serial [pk]
  email varchar(255) [unique, not null]
  password_hash varchar(255) [not null]
  first_name varchar(50)
  last_name varchar(50)
  created_at timestamp [default: `now()`]
  updated_at timestamp
  
  Note: 'Customer accounts'
}

Table addresses {
  id serial [pk]
  user_id integer [not null, ref: > users.id]
  street varchar(255) [not null]
  city varchar(100) [not null]
  state varchar(50)
  postal_code varchar(20) [not null]
  country varchar(50) [not null, default: 'USA']
  is_default boolean [default: false]
}

// Products
Table categories {
  id serial [pk]
  name varchar(100) [not null]
  parent_id integer [ref: > categories.id]  // Self-referencing for hierarchy
}

Table products {
  id serial [pk]
  sku varchar(50) [unique, not null]
  name varchar(200) [not null]
  description text
  price decimal(10,2) [not null]
  stock_quantity integer [default: 0]
  category_id integer [ref: > categories.id]
  is_active boolean [default: true]
  created_at timestamp [default: `now()`]
  
  indexes {
    sku
    category_id
    is_active
  }
}

// Orders
Table orders {
  id serial [pk]
  user_id integer [not null, ref: > users.id]
  shipping_address_id integer [ref: > addresses.id]
  status order_status [default: 'pending']
  total_amount decimal(10,2) [not null]
  created_at timestamp [default: `now()`]
  updated_at timestamp
  
  indexes {
    user_id
    status
    created_at
  }
}

Table order_items {
  id serial [pk]
  order_id integer [not null, ref: > orders.id]
  product_id integer [not null, ref: > products.id]
  quantity integer [not null, default: 1]
  unit_price decimal(10,2) [not null]
  
  indexes {
    (order_id, product_id) [unique]
  }
}

// Table Groups
TableGroup customers {
  users
  addresses
}

TableGroup catalog {
  categories
  products
}

TableGroup sales {
  orders
  order_items
}
```

---

## DBML Tools

### dbdiagram.io

The primary tool for working with DBML is [dbdiagram.io](https://dbdiagram.io/):

- **Free online editor** with real-time diagram visualization
- **Export options:** PNG, PDF, SQL (PostgreSQL, MySQL, SQL Server, etc.)
- **Import from SQL:** Convert existing DDL to DBML
- **Collaboration:** Share diagrams with team members
- **Version control friendly:** DBML files are plain text

### DBML CLI

Install the command-line tool for local development:

```bash
npm install -g @dbml/cli
```

**Convert DBML to SQL:**
```bash
dbml2sql schema.dbml --postgres -o schema.sql
dbml2sql schema.dbml --mysql -o schema.sql
```

**Convert SQL to DBML:**
```bash
sql2dbml schema.sql --postgres -o schema.dbml
```

### VS Code Extension

Install the **DBML Language** extension for:
- Syntax highlighting
- Auto-completion
- Error checking
- Preview diagrams

---

## Workflow Integration

### 1. Design Phase
```
1. Write DBML schema
2. Visualize in dbdiagram.io
3. Review with team
4. Iterate on design
```

### 2. Implementation Phase
```
1. Export DBML to SQL
2. Apply migrations to database
3. Generate ORM models (optional)
4. Keep DBML as documentation
```

### 3. Documentation
```
1. Store .dbml files in version control
2. Generate diagrams for documentation
3. Update DBML when schema changes
4. Use as onboarding reference
```

---

## Practice Exercises

### Exercise 1: Blog Platform
Design a DBML schema for a blog platform with:
- Users (authors and readers)
- Posts with categories and tags (many-to-many)
- Comments with nested replies (self-referencing)
- User follows (users follow other users)

### Exercise 2: Library System
Create a DBML schema for a library with:
- Books with multiple authors (many-to-many)
- Members with borrowing history
- Reservations and due dates
- Fines for late returns

### Exercise 3: Convert Existing SQL
Take the following SQL and convert it to DBML:
```sql
CREATE TABLE departments (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    budget DECIMAL(15,2)
);

CREATE TABLE employees (
    id SERIAL PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    hire_date DATE NOT NULL,
    salary DECIMAL(10,2),
    department_id INTEGER REFERENCES departments(id),
    manager_id INTEGER REFERENCES employees(id)
);
```

### Exercise 4: Identify Improvements
Review this DBML and suggest improvements:
```dbml
Table users {
  id integer
  name varchar(255)
  email varchar(255)
}

Table orders {
  id integer
  user integer
  date varchar(50)
  total varchar(20)
}
```

<details>
<summary>Hint for Exercise 4</summary>

Consider:
- Missing primary keys
- Missing constraints (not null, unique)
- Wrong data types (date as varchar, total as varchar)
- Column naming (user vs user_id)
- Missing timestamps
- Missing foreign key definition
</details>

---

## Key Takeaways

1. **DBML is for design** — Use it to plan schemas before writing SQL
2. **Human-readable** — Easier to review and understand than DDL
3. **Tool integration** — Works with dbdiagram.io for visualization
4. **Database-agnostic** — Export to PostgreSQL, MySQL, SQL Server, etc.
5. **Documentation** — Self-documenting with notes and comments
6. **Version control** — Plain text files work great with git

---

## Additional Resources

- [DBML Official Documentation](https://dbml.dbdiagram.io/docs/)
- [dbdiagram.io](https://dbdiagram.io/)
- [DBML CLI on npm](https://www.npmjs.com/package/@dbml/cli)
- [VS Code DBML Extension](https://marketplace.visualstudio.com/items?itemName=matt-meyers.vscode-dbml)
