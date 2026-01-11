# Quiz 8: Document Databases - MongoDB Fundamentals

## Instructions

This quiz tests your understanding of MongoDB document databases, including schema design, CRUD operations, and aggregation pipelines. Some questions ask you to translate requirements into MongoDB code, while others ask you to explain what MongoDB operations do.

---

## Question 1 - Document Model vs Relational

**Scenario:** You're building a blog platform. Each blog post can have 0-100 comments. Users can write many posts and many comments.

**Which schema design approach is BEST for MongoDB?**

- [ ] A. Store posts in one collection and comments in another collection (reference comments by post_id)

- [ ] B. Embed all comments inside each post document

- [ ] C. Store everything in a single denormalized collection with duplicated user data

- [ ] D. Use three collections (users, posts, comments) with foreign key references like a relational database

**Explanation:**

- **A is CORRECT** ✅
  - **Reason:** Comments can grow to hundreds per post (risk of hitting 16MB limit with embedding). Referencing allows independent queries like "all comments by user X" and avoids document size issues.
  
- **B is wrong** ❌
  - **Reason:** Embedding 100+ comments could make documents large and slow. Also makes it hard to query "all comments by a user" across posts.
  
- **C is wrong** ❌
  - **Reason:** Massive data duplication (user info repeated in every post/comment). Updates to user data require updating thousands of documents.
  
- **D is wrong** ❌
  - **Reason:** Over-normalization defeats MongoDB's purpose. If you need relational patterns everywhere, use a relational database.

**Key takeaway:** Use **referencing** when data grows unbounded or needs independent queries.

---

## Question 2 - JSON vs BSON

**Which statement about BSON is TRUE?**

- [ ] A. BSON is human-readable text format identical to JSON

- [ ] B. BSON supports additional data types like Date, Binary, and ObjectId that JSON cannot represent

- [ ] C. BSON is always larger in size than equivalent JSON due to metadata overhead

- [ ] D. MongoDB stores documents as JSON internally and converts to BSON only during network transmission

**Explanation:**

- **B is CORRECT** ✅
  - **Reason:** BSON extends JSON with types like `Date`, `ObjectId`, `Binary`, `Decimal128`, `Int32`, `Int64`, etc. JSON only has string, number, boolean, null, array, and object.
  
- **A is wrong** ❌
  - **Reason:** BSON is **binary** format (Binary JSON), not human-readable text.
  
- **C is wrong** ❌
  - **Reason:** BSON is *sometimes* larger due to metadata, but not *always*. For large strings/arrays, BSON can be smaller due to efficient encoding.
  
- **D is wrong** ❌
  - **Reason:** MongoDB stores documents as **BSON internally**. Drivers convert JSON ↔ BSON during read/write.

**Key takeaway:** BSON = Binary JSON with **extended data types** and **efficient traversal**.

---

## Question 3 - ObjectId Structure

**Given this ObjectId: `507f1f77bcf86cd799439011`**

**What information does it encode?**

- [ ] A. Only a random unique identifier (like UUID)

- [ ] B. Timestamp (4 bytes) + random value (5 bytes) + counter (3 bytes)

- [ ] C. User ID (4 bytes) + collection name (4 bytes) + sequence number (4 bytes)

- [ ] D. Creation date, server hostname, and process ID

**Explanation:**

- **B is CORRECT** ✅
  - **Structure:**
    - **Bytes 0-3:** Unix timestamp (seconds since epoch)
    - **Bytes 4-8:** Random value (machine + process identifier in older versions)
    - **Bytes 9-11:** Incrementing counter
  - **Total:** 12 bytes = 24 hex characters
  
- **A is wrong** ❌
  - **Reason:** ObjectId encodes **timestamp** + random + counter, not just random.
  
- **C is wrong** ❌
  - **Reason:** Doesn't encode user ID or collection name.
  
- **D is wrong** ❌
  - **Reason:** Modern ObjectIds use **random value**, not hostname/process ID (changed in MongoDB 3.4+).

**Key takeaway:** ObjectId is **time-sortable** and **globally unique** without coordination.

---

## Question 4 - Requirement → MongoDB Insert

**Requirement:** A user registration system needs to create a new user account with:
- Username: "alice"
- Email: "alice@example.com"
- Age: 28
- Created timestamp (current time)
- Empty array of favorite posts

**Which MongoDB command correctly implements this?**

- [ ] A.
```javascript
db.users.insertOne({
  username: "alice",
  email: "alice@example.com",
  age: 28,
  created_at: new Date(),
  favorite_posts: []
});
```

- [ ] B.
```javascript
db.users.insert({
  username: "alice",
  email: "alice@example.com",
  age: "28",
  created_at: NOW(),
  favorite_posts: null
});
```

- [ ] C.
```javascript
INSERT INTO users (username, email, age, created_at, favorite_posts)
VALUES ('alice', 'alice@example.com', 28, CURRENT_TIMESTAMP, []);
```

- [ ] D.
```javascript
db.users.save({
  _id: ObjectId(),
  username: "alice",
  email: "alice@example.com",
  age: 28
});
```

**Explanation:**

- **A is CORRECT** ✅
  - Uses `insertOne()` (preferred modern method)
  - Age is **number** (28, not "28")
  - `new Date()` for current timestamp
  - Empty array `[]` for favorite_posts
  
- **B is wrong** ❌
  - Age is **string** "28" instead of number
  - `NOW()` doesn't exist in MongoDB (use `new Date()`)
  - `favorite_posts: null` is not an empty array
  
- **C is wrong** ❌
  - This is **SQL syntax**, not MongoDB
  
- **D is wrong** ❌
  - `save()` is deprecated (use `insertOne` or `updateOne`)
  - Missing required fields (created_at, favorite_posts)

**Key takeaway:** Use `insertOne()` with proper JavaScript types (`new Date()`, numbers, arrays).

---

## Question 5 - MongoDB Query → Description

**Given this MongoDB query:**

```javascript
db.orders.find({
  status: "completed",
  amount: { $gte: 100, $lte: 500 }
})
.sort({ created_at: -1 })
.limit(10);
```

**What does this query do?**

- [ ] A. Find the 10 most recent completed orders with amounts between $100-$500

- [ ] B. Find all completed orders, sort them by amount descending, and return the top 10

- [ ] C. Update 10 completed orders to set their amount between 100 and 500

- [ ] D. Delete completed orders with amounts over $100 but less than $500

**Explanation:**

- **A is CORRECT** ✅
  - **Filter:** `status: "completed"` + `amount >= 100 AND amount <= 500`
  - **Sort:** `created_at: -1` (descending = most recent first)
  - **Limit:** Top 10 results
  
- **B is wrong** ❌
  - Sorts by **created_at** (not amount)
  
- **C is wrong** ❌
  - This is a **read query** (`find`), not update (`updateOne/Many`)
  
- **D is wrong** ❌
  - This is a **read query**, not delete (`deleteOne/Many`)

**Key takeaway:** `.find()` filters → `.sort()` orders → `.limit()` restricts count.

---

## Question 6 - Schema Design Decision

**Scenario:** An e-commerce app has products with **varying attributes**:
- Electronics: brand, warranty, voltage
- Clothing: size, color, material
- Books: author, ISBN, publisher

**Which MongoDB schema pattern is BEST?**

- [ ] A. Create separate collections (electronics, clothing, books) with different schemas

- [ ] B. Use a single `products` collection with an `attributes` array of key-value pairs

- [ ] C. Create a relational-style schema with products and attributes tables with foreign keys

- [ ] D. Embed all possible attributes in every product document (nulls for unused fields)

**Explanation:**

- **B is CORRECT** ✅ - **Attribute Pattern**
  ```javascript
  {
    "_id": ObjectId("..."),
    "name": "Laptop",
    "category": "electronics",
    "attributes": [
      { "k": "brand", "v": "Dell" },
      { "k": "warranty", "v": "2 years" },
      { "k": "ram", "v": "16GB" }
    ]
  }
  ```
  - **Advantages:** Flexible schema, easy to add new attributes, efficient indexing on `attributes.k` and `attributes.v`
  
- **A is wrong** ❌
  - Separating collections makes cross-category queries difficult ("search all products")
  
- **C is wrong** ❌
  - Over-normalization defeats MongoDB's flexibility
  
- **D is wrong** ❌
  - Wastes space with many null fields
  - Schema becomes huge and unmanageable

**Key takeaway:** Use **Attribute Pattern** for polymorphic data with varying fields.

---

## Question 7 - Requirement → MongoDB Update

**Requirement:** Increment the `likes` count by 1 and add "trending" to the `tags` array for post with `_id: ObjectId("507f1f77bcf86cd799439011")`. Don't add "trending" if it already exists in tags.

**Which MongoDB command correctly implements this?**

- [ ] A.
```javascript
db.posts.updateOne(
  { _id: ObjectId("507f1f77bcf86cd799439011") },
  { 
    $inc: { likes: 1 },
    $push: { tags: "trending" }
  }
);
```

- [ ] B.
```javascript
db.posts.updateOne(
  { _id: ObjectId("507f1f77bcf86cd799439011") },
  { 
    $inc: { likes: 1 },
    $addToSet: { tags: "trending" }
  }
);
```

- [ ] C.
```javascript
db.posts.findOneAndUpdate(
  { _id: ObjectId("507f1f77bcf86cd799439011") },
  { 
    likes: likes + 1,
    tags: tags.push("trending")
  }
);
```

- [ ] D.
```javascript
db.posts.update(
  { _id: ObjectId("507f1f77bcf86cd799439011") },
  { 
    $set: { likes: likes + 1, tags: ["trending"] }
  }
);
```

**Explanation:**

- **B is CORRECT** ✅
  - `$inc: { likes: 1 }` increments likes counter
  - `$addToSet: { tags: "trending" }` adds only if not present (no duplicates)
  
- **A is wrong** ❌
  - `$push` always adds to array (**creates duplicates** if "trending" already exists)
  
- **C is wrong** ❌
  - Invalid syntax: `likes + 1` and `tags.push()` aren't MongoDB operators
  
- **D is wrong** ❌
  - `$set: { tags: ["trending"] }` **replaces entire array** (loses existing tags)
  - `likes + 1` is invalid syntax

**Key takeaway:** Use `$inc` for numbers, `$addToSet` for unique array values.

---

## Question 8 - Aggregation Pipeline → Description

**Given this aggregation pipeline:**

```javascript
db.orders.aggregate([
  { $match: { status: "completed" } },
  { $group: {
      _id: "$customer_id",
      total_spent: { $sum: "$amount" },
      order_count: { $sum: 1 }
    }
  },
  { $sort: { total_spent: -1 } },
  { $limit: 10 }
]);
```

**What does this pipeline produce?**

- [ ] A. The 10 most expensive individual completed orders

- [ ] B. The top 10 customers by total spending on completed orders, with order counts

- [ ] C. A list of all customers with their average order amount

- [ ] D. The 10 most recent completed orders grouped by customer

**Explanation:**

- **B is CORRECT** ✅
  - **Stage 1:** Filter completed orders
  - **Stage 2:** Group by customer, sum amounts, count orders
  - **Stage 3:** Sort by total_spent descending
  - **Stage 4:** Take top 10
  - **Result:** `[{ _id: customer_id, total_spent: 1500, order_count: 8 }, ...]`
  
- **A is wrong** ❌
  - Groups by customer (not individual orders)
  
- **C is wrong** ❌
  - Calculates `$sum` (total), not `$avg` (average)
  - Limits to 10 (not all customers)
  
- **D is wrong** ❌
  - Doesn't use `$sort: { created_at: -1 }` for recency

**Key takeaway:** `$match` → `$group` → `$sort` → `$limit` is a common analytics pattern.

---

## Question 9 - When to Choose MongoDB

**Which scenario is MongoDB BEST suited for?**

- [ ] A. Banking system requiring ACID transactions across multiple tables with complex joins

- [ ] B. Content management system with flexible schema, embedded comments, and rapid prototyping

- [ ] C. Inventory system with strict referential integrity and normalized inventory counts

- [ ] D. Financial ledger requiring strong consistency and multi-statement transactions

**Explanation:**

- **B is CORRECT** ✅
  - **Flexible schema:** Easily add fields (e.g., `video_url`, `gallery[]`) without migrations
  - **Embedded data:** Comments/tags embedded in posts (fewer queries)
  - **Rapid prototyping:** No need to design schema upfront
  
- **A is wrong** ❌
  - Banking needs **ACID transactions** across tables (use PostgreSQL)
  
- **C is wrong** ❌
  - Inventory requires **strict consistency** and normalization (use SQL)
  
- **D is wrong** ❌
  - Financial ledgers need **multi-statement transactions** and audit trails (use SQL)

**Key takeaway:** MongoDB shines with **flexible schemas**, **embedded data**, and **document-centric queries**.

---

## Question 10 - Embedding vs Referencing

**Scenario:** A movie database has:
- Movies (title, year, runtime)
- Actors (name, birthdate)
- Each movie has 5-50 actors
- Each actor appears in 10-200 movies

**Which schema design is BEST?**

- [ ] A. Embed actors array inside each movie document

- [ ] B. Embed movies array inside each actor document

- [ ] C. Use two collections (movies and actors) with actor_ids array in movies

- [ ] D. Use three collections (movies, actors, movie_actors join table)

**Explanation:**

- **C is CORRECT** ✅
  ```javascript
  // Movies collection
  {
    "_id": ObjectId("..."),
    "title": "Inception",
    "year": 2010,
    "actor_ids": [ObjectId("actor1"), ObjectId("actor2"), ...]
  }
  
  // Actors collection
  {
    "_id": ObjectId("actor1"),
    "name": "Leonardo DiCaprio",
    "birthdate": ISODate("1974-11-11")
  }
  ```
  - Query movies with `$lookup` to get actor details
  - Actor data **not duplicated** (update once)
  - Movies array in movies doc is **bounded** (5-50 actors)
  
- **A is wrong** ❌
  - Massive duplication: Actor "Tom Hanks" embedded in 100+ movies
  - Updating actor info requires updating 100+ documents
  
- **B is wrong** ❌
  - Popular actors have 200+ movies → array too large
  - Hard to query "all movies from 2010"
  
- **D is wrong** ❌
  - Over-normalization (join table) defeats MongoDB's purpose
  - Use this pattern only if you need SQL-style queries

**Key takeaway:** **Many-to-many** relationships → reference with array of IDs on the "primary" side.

---

## Question 11 - MongoDB $lookup (JOIN)

**Given these collections:**

```javascript
// users collection
{ _id: 1, name: "Alice" }
{ _id: 2, name: "Bob" }

// orders collection
{ _id: 101, user_id: 1, amount: 100 }
{ _id: 102, user_id: 1, amount: 150 }
{ _id: 103, user_id: 2, amount: 200 }
```

**Which aggregation fetches users with their orders?**

- [ ] A.
```javascript
db.users.aggregate([
  { $lookup: {
      from: "orders",
      localField: "_id",
      foreignField: "user_id",
      as: "orders"
    }
  }
]);
```

- [ ] B.
```javascript
db.orders.aggregate([
  { $lookup: {
      from: "users",
      localField: "user_id",
      foreignField: "_id",
      as: "user"
    }
  }
]);
```

- [ ] C.
```javascript
db.users.join(db.orders, {
  left: "_id",
  right: "user_id"
});
```

- [ ] D.
```javascript
db.users.aggregate([
  { $match: { _id: { $in: db.orders.distinct("user_id") } } }
]);
```

**Explanation:**

- **A is CORRECT** ✅
  - Start with **users** collection
  - `$lookup` joins **orders** where `orders.user_id === users._id`
  - Result: `{ _id: 1, name: "Alice", orders: [{...}, {...}] }`
  
- **B is wrong** ❌
  - Fetches **orders with user info** (not users with orders)
  - Result structure: `{ _id: 101, user_id: 1, amount: 100, user: [{...}] }`
  
- **C is wrong** ❌
  - MongoDB doesn't have `.join()` method (use `$lookup`)
  
- **D is wrong** ❌
  - Only filters users (doesn't fetch orders)

**Key takeaway:** `$lookup` starts from "left" collection, joins "right" collection, outputs array.

---

## Question 12 - BSON Data Types

**Which MongoDB field definition is VALID?**

- [ ] A.
```javascript
{
  created_at: "2026-03-15T10:00:00Z",  // ISO string
  price: "99.99",                       // String
  tags: "mongodb,database,nosql"        // Comma-separated string
}
```

- [ ] B.
```javascript
{
  created_at: ISODate("2026-03-15T10:00:00Z"),  // BSON Date
  price: NumberDecimal("99.99"),                 // BSON Decimal128
  tags: ["mongodb", "database", "nosql"]         // Array
}
```

- [ ] C.
```javascript
{
  created_at: TIMESTAMP(),
  price: DECIMAL(10, 2),
  tags: ARRAY<STRING>
}
```

- [ ] D.
```javascript
{
  created_at: new Date("2026-03-15"),
  price: 99.99,  // JavaScript number (64-bit float)
  tags: ["mongodb", "database", "nosql"]
}
```

**Explanation:**

- **B is CORRECT** ✅ (Most precise)
  - `ISODate()` = BSON Date type
  - `NumberDecimal()` = BSON Decimal128 (exact decimal, no float rounding)
  - Array of strings
  
- **D is ALSO VALID** ✅ (Common in practice)
  - `new Date()` = JavaScript Date → BSON Date
  - `99.99` = JavaScript number → BSON Double (but may have float rounding)
  - Array of strings
  
- **A is technically valid but BAD PRACTICE** ❌
  - Stores dates/numbers as **strings** (can't sort/compare properly)
  - Tags as CSV string instead of array (can't query with `$in`)
  
- **C is wrong** ❌
  - SQL syntax, not MongoDB

**Key takeaway:** Use **BSON types** (`ISODate`, `NumberDecimal`) for proper type handling.

---

## Scoring

**Grade Scale:**
- 11-12 correct: **A** (Excellent)
- 9-10 correct: **B** (Good)
- 7-8 correct: **C** (Satisfactory)
- 5-6 correct: **D** (Needs improvement)
- 0-4 correct: **F** (Review material)

---

## Answer Key

1. **A** - Reference comments (unbounded growth)
2. **B** - BSON has extended types (Date, ObjectId, Binary)
3. **B** - ObjectId = timestamp + random + counter
4. **A** - `insertOne()` with correct types
5. **A** - Find 10 recent completed orders $100-$500
6. **B** - Attribute Pattern for varying fields
7. **B** - `$inc` and `$addToSet`
8. **B** - Top 10 customers by spending
9. **B** - CMS with flexible schema
10. **C** - Reference actors with array of IDs
11. **A** - `$lookup` from users to orders
12. **B** or **D** - BSON types (B is more precise)

---

**Related Content:**
- [MongoDB Fundamentals](../mongodb-fundamentals.md)
- [Schema Design Patterns](../schema-design-patterns.md)
- [CRUD Operations](../mongodb-crud.md)
- [Aggregation Pipeline](../aggregation-pipeline.md)
