# ORMs, Query Builders & SQL Injection

Modern applications often use abstractions over raw SQL. Understanding these tools—and their security implications—is essential.

---

## The Abstraction Spectrum

```
Raw SQL ◄────────────────────────────────────────────► Full ORM

"SELECT * FROM users"    Query Builder    Active Record    Data Mapper
        ▲                    ▲                  ▲              ▲
    Most Control        Balanced          Convenience     Most Abstract
    Most Verbose      Type-safe SQL       Fast Dev        Entity Focus
```

---

## Raw SQL

Direct SQL strings sent to the database.

### Advantages

- Full control over queries
- No abstraction overhead
- Can use all database features

### Disadvantages

- Verbose for common operations
- Easy to introduce SQL injection
- No type checking

### Example with Node.js (pg)

```javascript
import { Pool } from 'pg';

const pool = new Pool();

// Simple query
const result = await pool.query('SELECT * FROM users WHERE id = $1', [userId]);
const user = result.rows[0];

// Insert
await pool.query(
    'INSERT INTO orders (customer_id, total) VALUES ($1, $2) RETURNING id',
    [customerId, total]
);
```

---

## Query Builders

Generate SQL programmatically with type safety.

### Advantages

- Type-safe query construction
- Composable queries
- Protection from SQL injection
- Cross-database compatibility

### Disadvantages

- Learning curve
- Some complex queries difficult to express
- Additional dependency

---

## Drizzle ORM

Drizzle is a TypeScript ORM with a SQL-like syntax that prioritizes type safety and developer experience.

### Installation

```bash
npm install drizzle-orm pg
npm install -D drizzle-kit @types/pg
```

### Schema Definition

```typescript
// src/db/schema.ts
import { pgTable, serial, varchar, integer, timestamp, decimal } from 'drizzle-orm/pg-core';

export const users = pgTable('users', {
    id: serial('id').primaryKey(),
    email: varchar('email', { length: 255 }).notNull().unique(),
    name: varchar('name', { length: 100 }),
    createdAt: timestamp('created_at').defaultNow(),
});

export const orders = pgTable('orders', {
    id: serial('id').primaryKey(),
    customerId: integer('customer_id').notNull().references(() => users.id),
    total: decimal('total', { precision: 10, scale: 2 }).notNull(),
    status: varchar('status', { length: 20 }).default('pending'),
    createdAt: timestamp('created_at').defaultNow(),
});

export const orderItems = pgTable('order_items', {
    id: serial('id').primaryKey(),
    orderId: integer('order_id').notNull().references(() => orders.id),
    productId: integer('product_id').notNull(),
    quantity: integer('quantity').notNull(),
    price: decimal('price', { precision: 10, scale: 2 }).notNull(),
});
```

### Database Connection

```typescript
// src/db/index.ts
import { drizzle } from 'drizzle-orm/node-postgres';
import { Pool } from 'pg';
import * as schema from './schema';

const pool = new Pool({
    connectionString: process.env.DATABASE_URL,
});

export const db = drizzle(pool, { schema });
```

### Basic Queries

#### SELECT

```typescript
import { db } from './db';
import { users, orders } from './db/schema';
import { eq, and, gt, like, desc, sql } from 'drizzle-orm';

// Select all
const allUsers = await db.select().from(users);

// Select specific columns
const userEmails = await db.select({
    id: users.id,
    email: users.email,
}).from(users);

// Where clause
const user = await db.select()
    .from(users)
    .where(eq(users.id, 1));

// Multiple conditions
const recentBigOrders = await db.select()
    .from(orders)
    .where(
        and(
            gt(orders.total, 100),
            gt(orders.createdAt, new Date('2024-01-01'))
        )
    );

// LIKE pattern
const gmailUsers = await db.select()
    .from(users)
    .where(like(users.email, '%@gmail.com'));

// Order and limit
const recentOrders = await db.select()
    .from(orders)
    .orderBy(desc(orders.createdAt))
    .limit(10);
```

#### INSERT

```typescript
// Single insert
const newUser = await db.insert(users)
    .values({
        email: 'new@example.com',
        name: 'New User',
    })
    .returning();

// Multiple insert
await db.insert(orderItems)
    .values([
        { orderId: 1, productId: 101, quantity: 2, price: '29.99' },
        { orderId: 1, productId: 102, quantity: 1, price: '49.99' },
    ]);
```

#### UPDATE

```typescript
// Update with condition
await db.update(orders)
    .set({ status: 'shipped' })
    .where(eq(orders.id, 1));

// Update with returning
const updated = await db.update(users)
    .set({ name: 'Updated Name' })
    .where(eq(users.id, 1))
    .returning();
```

#### DELETE

```typescript
// Delete with condition
await db.delete(orders)
    .where(eq(orders.status, 'cancelled'));
```

### Joins

```typescript
// Inner join
const ordersWithCustomers = await db.select({
    orderId: orders.id,
    orderTotal: orders.total,
    customerEmail: users.email,
    customerName: users.name,
})
.from(orders)
.innerJoin(users, eq(orders.customerId, users.id));

// Left join
const usersWithOrders = await db.select()
    .from(users)
    .leftJoin(orders, eq(users.id, orders.customerId));

// Multiple joins
const orderDetails = await db.select()
    .from(orders)
    .innerJoin(users, eq(orders.customerId, users.id))
    .innerJoin(orderItems, eq(orders.id, orderItems.orderId));
```

### Aggregations

```typescript
import { count, sum, avg } from 'drizzle-orm';

// Count
const userCount = await db.select({
    count: count(),
}).from(users);

// Sum with grouping
const orderTotals = await db.select({
    customerId: orders.customerId,
    totalSpent: sum(orders.total),
})
.from(orders)
.groupBy(orders.customerId);

// Average
const avgOrderValue = await db.select({
    average: avg(orders.total),
}).from(orders);
```

### Transactions

```typescript
await db.transaction(async (tx) => {
    // All queries in this block run in a transaction
    const [order] = await tx.insert(orders)
        .values({ customerId: 1, total: '99.99' })
        .returning();
    
    await tx.insert(orderItems)
        .values({
            orderId: order.id,
            productId: 101,
            quantity: 1,
            price: '99.99',
        });
    
    // If any query fails, all are rolled back
});
```

### Raw SQL (Escape Hatch)

```typescript
import { sql } from 'drizzle-orm';

// Raw SQL with template literals (still parameterized!)
const result = await db.execute(
    sql`SELECT * FROM users WHERE email = ${email}`
);

// Using raw SQL in select
const usersWithOrderCount = await db.select({
    id: users.id,
    email: users.email,
    orderCount: sql<number>`(SELECT COUNT(*) FROM orders WHERE customer_id = ${users.id})`,
}).from(users);
```

### Migrations with Drizzle Kit

```bash
# Generate migration from schema changes
npx drizzle-kit generate

# Apply migrations
npx drizzle-kit migrate

# Push schema directly (development only)
npx drizzle-kit push
```

---

## Query Builders vs ORMs

| Feature | Query Builder (Drizzle) | Full ORM (Prisma) |
|---------|------------------------|-------------------|
| Learning curve | Lower | Higher |
| SQL knowledge needed | Yes | Less |
| Type safety | Excellent | Excellent |
| Performance control | High | Medium |
| Complex queries | Easier | Harder |
| Schema definition | Code-first | Schema file |
| Relationships | Manual joins | Automatic |

---

## SQL Injection

**SQL injection** occurs when user input is incorporated into SQL queries without proper sanitization.

### The Attack

```javascript
// VULNERABLE CODE - DO NOT USE
const email = req.body.email;  // User input: "'; DROP TABLE users; --"

// This query becomes:
// SELECT * FROM users WHERE email = ''; DROP TABLE users; --'
const result = await pool.query(
    `SELECT * FROM users WHERE email = '${email}'`
);
```

### Why It's Dangerous

- **Data theft**: `' OR '1'='1` returns all rows
- **Data modification**: `'; UPDATE users SET role='admin' WHERE email='attacker@...`
- **Data deletion**: `'; DROP TABLE users; --`
- **Server compromise**: In some databases, execute system commands

### Prevention: Parameterized Queries

**ALWAYS use parameterized queries (prepared statements):**

```javascript
// SAFE: Using parameterized queries
const result = await pool.query(
    'SELECT * FROM users WHERE email = $1',
    [email]  // Parameter passed separately
);
```

The database treats parameters as data, never as SQL code.

### Prevention with Query Builders

Query builders automatically parameterize:

```typescript
// Drizzle - automatically safe
const user = await db.select()
    .from(users)
    .where(eq(users.email, email));

// Even raw SQL with template literals is safe
const result = await db.execute(
    sql`SELECT * FROM users WHERE email = ${email}`
);
```

### Common Injection Points

| Location | Vulnerable | Safe |
|----------|-----------|------|
| WHERE clause | `WHERE id = ${id}` | `WHERE id = $1`, [id] |
| INSERT values | `VALUES ('${name}')` | `VALUES ($1)`, [name] |
| ORDER BY | `ORDER BY ${column}` | Whitelist validation |
| LIMIT | `LIMIT ${count}` | `LIMIT $1`, [parseInt(count)] |
| Table names | `SELECT * FROM ${table}` | Whitelist validation |

### Special Cases: Identifiers

Column and table names cannot be parameterized. Use whitelisting:

```javascript
// VULNERABLE
const sortColumn = req.query.sort;  // Could be: "id; DROP TABLE users"
const result = await pool.query(
    `SELECT * FROM users ORDER BY ${sortColumn}`
);

// SAFE: Whitelist allowed values
const ALLOWED_COLUMNS = ['id', 'name', 'email', 'created_at'];
const sortColumn = req.query.sort;

if (!ALLOWED_COLUMNS.includes(sortColumn)) {
    throw new Error('Invalid sort column');
}

const result = await pool.query(
    `SELECT * FROM users ORDER BY ${sortColumn}`
);
```

### Second-Order Injection

Data stored in database used in later queries:

```javascript
// User registers with username: admin'--
// Stored in database

// Later, query uses stored value unsafely
const user = await pool.query('SELECT * FROM users WHERE id = $1', [id]);
const username = user.rows[0].username;

// VULNERABLE: Using stored data in query
await pool.query(`SELECT * FROM logs WHERE username = '${username}'`);

// SAFE: Always parameterize
await pool.query('SELECT * FROM logs WHERE username = $1', [username]);
```

### Testing for SQL Injection

Common test payloads:
- `' OR '1'='1`
- `'; DROP TABLE users; --`
- `1; SELECT * FROM users`
- `1 UNION SELECT username, password FROM users`

Use automated scanners: SQLMap, Burp Suite

---

## ORM Security Best Practices

### 1. Always Use Query Builder Methods

```typescript
// BAD: String interpolation
const query = `SELECT * FROM users WHERE name = '${name}'`;

// GOOD: Query builder
const result = await db.select().from(users).where(eq(users.name, name));
```

### 2. Validate and Sanitize Input

```typescript
// Validate input types
const userId = parseInt(req.params.id, 10);
if (isNaN(userId)) {
    throw new Error('Invalid user ID');
}

// Sanitize strings
const sanitizedName = name.trim().substring(0, 100);
```

### 3. Use TypeScript

Type safety catches many errors at compile time:

```typescript
// TypeScript catches this error
const result = await db.select()
    .from(users)
    .where(eq(users.id, "not a number"));  // Error: string not assignable to number
```

### 4. Limit Query Results

```typescript
// Always limit results
const users = await db.select()
    .from(users)
    .limit(100);  // Prevent returning millions of rows
```

### 5. Use Read-Only Connections for Reports

```typescript
// Separate connections with different privileges
const writeDb = drizzle(writePool);  // Full access
const readDb = drizzle(readPool);    // SELECT only

// Reports use read-only connection
const report = await readDb.select()...
```

---

## Comparison: Raw SQL vs Query Builder vs ORM

### Simple Query

```typescript
// Raw SQL
const result = await pool.query(
    'SELECT * FROM users WHERE id = $1',
    [userId]
);
const user = result.rows[0];

// Drizzle Query Builder
const [user] = await db.select()
    .from(users)
    .where(eq(users.id, userId));

// Prisma ORM
const user = await prisma.user.findUnique({
    where: { id: userId }
});
```

### Complex Query with Joins

```typescript
// Raw SQL
const result = await pool.query(`
    SELECT o.id, o.total, u.email, u.name
    FROM orders o
    JOIN users u ON o.customer_id = u.id
    WHERE o.status = $1 AND o.total > $2
    ORDER BY o.created_at DESC
    LIMIT 10
`, ['pending', 100]);

// Drizzle
const result = await db.select({
    id: orders.id,
    total: orders.total,
    email: users.email,
    name: users.name,
})
.from(orders)
.innerJoin(users, eq(orders.customerId, users.id))
.where(
    and(
        eq(orders.status, 'pending'),
        gt(orders.total, 100)
    )
)
.orderBy(desc(orders.createdAt))
.limit(10);
```

### When to Use Each

| Use Case | Recommendation |
|----------|----------------|
| Simple CRUD | ORM or Query Builder |
| Complex queries | Query Builder or Raw SQL |
| Performance critical | Raw SQL with optimization |
| Rapid development | ORM |
| Team with SQL expertise | Query Builder |
| Legacy database | Query Builder or Raw SQL |

---

## Practice

### Exercise 1: Convert Raw SQL to Drizzle

Convert these raw SQL queries to Drizzle:

```sql
-- Query 1
SELECT email, name FROM users WHERE created_at > '2024-01-01';

-- Query 2
INSERT INTO orders (customer_id, total, status) 
VALUES (1, 99.99, 'pending') RETURNING *;

-- Query 3
UPDATE products SET price = price * 1.1 WHERE category = 'electronics';
```

### Exercise 2: Identify SQL Injection

Find and fix the SQL injection vulnerabilities:

```javascript
app.get('/users', async (req, res) => {
    const search = req.query.search;
    const sort = req.query.sort || 'id';
    const result = await pool.query(
        `SELECT * FROM users WHERE name LIKE '%${search}%' ORDER BY ${sort}`
    );
    res.json(result.rows);
});
```

### Exercise 3: Transaction with Drizzle

Write a Drizzle transaction that:
1. Creates a new order
2. Adds 3 order items
3. Updates the customer's `total_orders` count
4. Rolls back if any step fails

---

## Key Takeaways

1. **Query builders** provide type-safe SQL generation
2. **Drizzle ORM** offers a SQL-like syntax with excellent TypeScript support
3. **Always use parameterized queries** to prevent SQL injection
4. **Never concatenate user input** into SQL strings
5. **Column/table names cannot be parameterized** — use whitelists
6. **ORMs automatically parameterize** when using their query methods
7. **Raw SQL escape hatches** should still use parameters
8. **Test for SQL injection** in security audits
9. **Use read-only connections** for reporting queries
10. **Choose the right abstraction level** based on project needs
