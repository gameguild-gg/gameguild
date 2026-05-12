# Drizzle ORM with MongoDB

## Overview

**Drizzle ORM** is a lightweight TypeScript ORM that supports both SQL databases (PostgreSQL, MySQL, SQLite) and **MongoDB**. It provides type-safe queries and schema definitions while maintaining a minimal footprint.

For MongoDB, Drizzle provides a simplified interface over the native MongoDB driver while preserving type safety.

## Installation

```bash
npm install drizzle-orm mongodb
npm install -D drizzle-kit
```

## Project Setup

```
project/
├── src/
│   ├── db/
│   │   ├── schema.ts       # Schema definitions
│   │   ├── client.ts       # Database connection
│   │   └── queries.ts      # Reusable queries
│   └── index.ts
├── drizzle.config.ts       # Drizzle Kit config
└── package.json
```

## Database Connection

### src/db/client.ts

```typescript
import { drizzle } from 'drizzle-orm/mongodb';
import { MongoClient } from 'mongodb';

// MongoDB connection string
const MONGO_URI = process.env.MONGO_URI || 'mongodb://localhost:27017/mydb';

// Create MongoDB client
const client = new MongoClient(MONGO_URI);

// Connect to MongoDB
await client.connect();

// Get database
const mongoDb = client.db('mydb');

// Create Drizzle instance
export const db = drizzle(mongoDb);
```

**With connection pooling:**

```typescript
import { drizzle } from 'drizzle-orm/mongodb';
import { MongoClient } from 'mongodb';

const client = new MongoClient(process.env.MONGO_URI!, {
  maxPoolSize: 10,
  minPoolSize: 2,
  maxIdleTimeMS: 30000
});

await client.connect();
export const db = drizzle(client.db('mydb'));
```

## Schema Definition

### src/db/schema.ts

```typescript
import { ObjectId } from 'mongodb';
import { mongoCollection } from 'drizzle-orm/mongodb';

// Users collection
export const users = mongoCollection('users', {
  _id: { type: 'ObjectId', default: () => new ObjectId() },
  username: { type: 'string' },
  email: { type: 'string' },
  age: { type: 'number' },
  created_at: { type: 'date', default: () => new Date() }
});

// Posts collection
export const posts = mongoCollection('posts', {
  _id: { type: 'ObjectId', default: () => new ObjectId() },
  title: { type: 'string' },
  content: { type: 'string' },
  author_id: { type: 'ObjectId' },
  tags: { type: 'array', items: { type: 'string' } },
  likes: { type: 'number', default: 0 },
  created_at: { type: 'date', default: () => new Date() }
});

// Comments collection (with embedded author info)
export const comments = mongoCollection('comments', {
  _id: { type: 'ObjectId', default: () => new ObjectId() },
  post_id: { type: 'ObjectId' },
  user_id: { type: 'ObjectId' },
  text: { type: 'string' },
  author: {
    type: 'object',
    properties: {
      username: { type: 'string' },
      email: { type: 'string' }
    }
  },
  created_at: { type: 'date', default: () => new Date() }
});

// Infer TypeScript types
export type User = typeof users.$inferSelect;
export type NewUser = typeof users.$inferInsert;

export type Post = typeof posts.$inferSelect;
export type NewPost = typeof posts.$inferInsert;

export type Comment = typeof comments.$inferSelect;
export type NewComment = typeof comments.$inferInsert;
```

**Type inference example:**

```typescript
// NewUser type (for inserts)
{
  _id?: ObjectId;          // Optional (auto-generated)
  username: string;
  email: string;
  age: number;
  created_at?: Date;       // Optional (has default)
}

// User type (for selects)
{
  _id: ObjectId;
  username: string;
  email: string;
  age: number;
  created_at: Date;
}
```

## CRUD Operations

### Create (Insert)

```typescript
import { db } from './db/client';
import { users, posts } from './db/schema';

// Insert single user
const newUser = await db.insert(users).values({
  username: 'alice',
  email: 'alice@example.com',
  age: 28
});

console.log(newUser.insertedId);  // ObjectId

// Insert multiple users
const newUsers = await db.insert(users).values([
  { username: 'bob', email: 'bob@example.com', age: 32 },
  { username: 'charlie', email: 'charlie@example.com', age: 25 }
]);

console.log(newUsers.insertedIds);  // { '0': ObjectId, '1': ObjectId }

// Insert with custom _id
await db.insert(posts).values({
  _id: new ObjectId('507f1f77bcf86cd799439011'),
  title: 'My First Post',
  content: 'Hello MongoDB!',
  author_id: new ObjectId('...'),
  tags: ['mongodb', 'tutorial']
});
```

### Read (Select)

```typescript
import { eq, gte, and } from 'drizzle-orm';

// Find all users
const allUsers = await db.select().from(users);

// Find user by _id
const user = await db
  .select()
  .from(users)
  .where(eq(users._id, new ObjectId('...')))
  .limit(1);

console.log(user[0]);  // First (and only) result

// Find users with conditions
const adults = await db
  .select()
  .from(users)
  .where(gte(users.age, 18));

// Multiple conditions
const result = await db
  .select()
  .from(users)
  .where(
    and(
      gte(users.age, 25),
      eq(users.username, 'alice')
    )
  );

// Select specific fields (projection)
const usernames = await db
  .select({
    username: users.username,
    email: users.email
  })
  .from(users);

// Result: [{ username: 'alice', email: 'alice@...' }, ...]

// Sort and limit
const topUsers = await db
  .select()
  .from(users)
  .orderBy(users.created_at, 'desc')
  .limit(10);

// Pagination
const page2 = await db
  .select()
  .from(users)
  .offset(10)
  .limit(10);
```

### Update

```typescript
import { eq, gte } from 'drizzle-orm';

// Update single user
await db
  .update(users)
  .set({ age: 29 })
  .where(eq(users.username, 'alice'));

// Update multiple users
await db
  .update(users)
  .set({ age: users.age + 1 })  // Increment age
  .where(gte(users.age, 30));

// Update with $push (add to array)
await db
  .update(posts)
  .set({
    tags: { $push: 'tutorial' }  // MongoDB operator
  })
  .where(eq(posts._id, new ObjectId('...')));

// Update with $inc (increment)
await db
  .update(posts)
  .set({
    likes: { $inc: 1 }  // Increment likes by 1
  })
  .where(eq(posts._id, new ObjectId('...')));
```

### Delete

```typescript
import { eq, lt } from 'drizzle-orm';

// Delete single user
await db
  .delete(users)
  .where(eq(users.username, 'alice'));

// Delete multiple users
await db
  .delete(users)
  .where(lt(users.age, 18));

// Delete all (be careful!)
await db.delete(users);  // No where clause
```

## Query Operators

Drizzle provides type-safe equivalents to MongoDB query operators:

```typescript
import { 
  eq, ne, gt, gte, lt, lte, 
  and, or, not, 
  inArray, notInArray,
  isNull, isNotNull
} from 'drizzle-orm';

// Comparison
eq(users.age, 28)           // age === 28
ne(users.age, 28)           // age !== 28
gt(users.age, 25)           // age > 25
gte(users.age, 25)          // age >= 25
lt(users.age, 30)           // age < 30
lte(users.age, 30)          // age <= 30

// Logical
and(
  gte(users.age, 25),
  lte(users.age, 30)
)

or(
  eq(users.username, 'alice'),
  eq(users.username, 'bob')
)

not(eq(users.age, 28))

// Array membership
inArray(users.age, [25, 28, 32])
notInArray(users.age, [25, 28])

// Null checks
isNull(users.email)
isNotNull(users.email)
```

**Example queries:**

```typescript
// Find users aged 25-30
const users25to30 = await db
  .select()
  .from(users)
  .where(
    and(
      gte(users.age, 25),
      lte(users.age, 30)
    )
  );

// Find alice or bob
const aliceOrBob = await db
  .select()
  .from(users)
  .where(
    or(
      eq(users.username, 'alice'),
      eq(users.username, 'bob')
    )
  );

// Find users in specific age groups
const targetAges = await db
  .select()
  .from(users)
  .where(inArray(users.age, [25, 30, 35, 40]));
```

## Aggregation (Limited Support)

Drizzle's MongoDB support has **limited aggregation** capabilities. For complex aggregations, use the native MongoDB driver:

```typescript
import { db } from './db/client';

// Get the underlying MongoDB collection
const usersCollection = db.getCollection('users');

// Use native MongoDB aggregation
const result = await usersCollection.aggregate([
  { $match: { age: { $gte: 25 } } },
  { $group: {
      _id: "$age",
      count: { $sum: 1 }
    }
  },
  { $sort: { count: -1 } }
]).toArray();

console.log(result);
```

**Hybrid approach:**

```typescript
// Use Drizzle for simple queries
const users = await db.select().from(users).where(gte(users.age, 25));

// Use native driver for aggregation
const stats = await db.getCollection('users').aggregate([
  { $group: {
      _id: null,
      avg_age: { $avg: "$age" },
      total: { $sum: 1 }
    }
  }
]).toArray();
```

## Transactions (Not Supported in MongoDB Drizzle)

MongoDB supports **multi-document transactions**, but Drizzle's MongoDB adapter **does not** currently support them. Use the native driver:

```typescript
import { MongoClient } from 'mongodb';

const client = new MongoClient(process.env.MONGO_URI!);
await client.connect();

const session = client.startSession();

try {
  await session.withTransaction(async () => {
    const usersCollection = client.db('mydb').collection('users');
    const postsCollection = client.db('mydb').collection('posts');

    // Insert user
    const userResult = await usersCollection.insertOne(
      { username: 'alice', email: 'alice@example.com' },
      { session }
    );

    // Insert post by same user
    await postsCollection.insertOne(
      {
        title: 'First Post',
        author_id: userResult.insertedId
      },
      { session }
    );

    // Both succeed or both fail
  });

  console.log('Transaction committed');
} catch (error) {
  console.error('Transaction aborted:', error);
} finally {
  await session.endSession();
}
```

## Environment Configuration

### .env

```
MONGO_URI=mongodb://localhost:27017/mydb
```

### drizzle.config.ts

```typescript
import type { Config } from 'drizzle-kit';

export default {
  schema: './src/db/schema.ts',
  out: './drizzle',
  driver: 'mongodb',
  dbCredentials: {
    url: process.env.MONGO_URI!
  }
} satisfies Config;
```

## Best Practices

### 1. Connection Management

```typescript
// Singleton pattern for database connection
let db: ReturnType<typeof drizzle> | null = null;

export async function getDb() {
  if (!db) {
    const client = new MongoClient(process.env.MONGO_URI!);
    await client.connect();
    db = drizzle(client.db('mydb'));
  }
  return db;
}
```

### 2. Reusable Queries

**src/db/queries.ts:**

```typescript
import { db } from './client';
import { users, posts } from './schema';
import { eq } from 'drizzle-orm';

export async function getUserById(id: ObjectId) {
  const result = await db
    .select()
    .from(users)
    .where(eq(users._id, id))
    .limit(1);
    
  return result[0] ?? null;
}

export async function getUserPosts(userId: ObjectId) {
  return db
    .select()
    .from(posts)
    .where(eq(posts.author_id, userId))
    .orderBy(posts.created_at, 'desc');
}

export async function createPost(data: NewPost) {
  const result = await db.insert(posts).values(data);
  return result.insertedId;
}
```

### 3. Type Safety

```typescript
import type { NewUser, User } from './db/schema';

// Function signature with types
async function createUser(data: NewUser): Promise<ObjectId> {
  const result = await db.insert(users).values(data);
  return result.insertedId;
}

// TypeScript ensures correct fields
await createUser({
  username: 'alice',
  email: 'alice@example.com',
  age: 28
  // created_at is optional (has default)
});

// ❌ TypeScript error: missing required fields
await createUser({
  username: 'bob'
  // Missing email and age
});
```

### 4. Error Handling

```typescript
import { MongoError } from 'mongodb';

try {
  await db.insert(users).values({
    username: 'alice',
    email: 'alice@example.com',
    age: 28
  });
} catch (error) {
  if (error instanceof MongoError) {
    if (error.code === 11000) {
      console.error('Duplicate key error');
    }
  }
  throw error;
}
```

## Limitations

Drizzle's MongoDB support is **simpler** than its SQL support:

| Feature | SQL (PostgreSQL) | MongoDB |
|---------|------------------|---------|
| Schema migrations | ✅ Yes | ❌ No (schemaless) |
| Relations (joins) | ✅ Yes | ❌ Limited (manual $lookup) |
| Transactions | ✅ Yes | ❌ No (use native driver) |
| Aggregations | ✅ Yes | ❌ Limited (use native driver) |
| Type safety | ✅ Full | ✅ Full |
| CRUD operations | ✅ Yes | ✅ Yes |

**When to use Drizzle for MongoDB:**

- ✅ Simple CRUD operations
- ✅ Type-safe schema definitions
- ✅ Basic queries with filters

**When to use native driver:**

- ❌ Complex aggregation pipelines
- ❌ Transactions
- ❌ Advanced MongoDB features ($lookup, $graphLookup, etc.)

## Key Takeaways

- **Drizzle + MongoDB** provides type-safe CRUD operations
- **Schema definitions** generate TypeScript types automatically
- **Query operators** (`eq`, `gte`, `and`, etc.) offer type safety
- **Limitations**: No transactions, limited aggregations (use native driver)
- **Best for**: Simple applications with basic queries
- **Hybrid approach**: Use Drizzle for CRUD, native driver for advanced features

---

**Next:** [MongoDB Quiz](./quiz/mongodb-quiz.md)
