# Quiz 8: Document Databases - MongoDB Fundamentals

## Instructions

This quiz tests your understanding of MongoDB document databases, including schema design, CRUD operations, and aggregation pipelines. Some questions ask you to translate requirements into MongoDB code, while others ask you to explain what MongoDB operations do.

Time estimate: 30-45 minutes

---

!!! quiz
{
"title": "Document Model vs Relational",
"question": "You're building a blog platform. Each blog post can have 0-100 comments. Which schema design approach is BEST for MongoDB?",
"options": ["Store posts in one collection and comments in another collection (reference comments by post_id)", "Embed all comments inside each post document", "Store everything in a single denormalized collection with duplicated user data", "Use three collections (users, posts, comments) with foreign key references like a relational database"],
"answers": ["Store posts in one collection and comments in another collection (reference comments by post_id)"]
}
!!!

---

!!! quiz
{
"title": "JSON vs BSON",
"question": "Which statement about BSON is TRUE?",
"options": ["BSON is human-readable text format identical to JSON", "BSON supports additional data types like Date, Binary, and ObjectId that JSON cannot represent", "BSON is always larger in size than equivalent JSON due to metadata overhead", "MongoDB stores documents as JSON internally and converts to BSON only during network transmission"],
"answers": ["BSON supports additional data types like Date, Binary, and ObjectId that JSON cannot represent"]
}
!!!

---

!!! quiz
{
"title": "ObjectId Structure",
"question": "Given this ObjectId: 507f1f77bcf86cd799439011 — What information does it encode?",
"options": ["Only a random unique identifier (like UUID)", "Timestamp (4 bytes) + random value (5 bytes) + counter (3 bytes)", "User ID (4 bytes) + collection name (4 bytes) + sequence number (4 bytes)", "Creation date, server hostname, and process ID"],
"answers": ["Timestamp (4 bytes) + random value (5 bytes) + counter (3 bytes)"]
}
!!!

---

**Requirement:** A user registration system needs to create a new user account with:

- Username: "alice"
- Email: "alice@example.com"
- Age: 28
- Created timestamp (current time)
- Empty array of favorite posts

**Which MongoDB command correctly implements this?**

Option A:

```javascript
db.users.insertOne({
  username: 'alice',
  email: 'alice@example.com',
  age: 28,
  created_at: new Date(),
  favorite_posts: [],
});
```

Option B:

```javascript
db.users.insert({
  username: 'alice',
  email: 'alice@example.com',
  age: '28',
  created_at: NOW(),
  favorite_posts: null,
});
```

Option C:

```javascript
INSERT INTO users (username, email, age, created_at, favorite_posts)
VALUES ('alice', 'alice@example.com', 28, CURRENT_TIMESTAMP, []);
```

Option D:

```javascript
db.users.save({
  _id: ObjectId(),
  username: 'alice',
  email: 'alice@example.com',
  age: 28,
});
```

!!! quiz
{
"title": "Requirement to MongoDB Insert",
"question": "Which MongoDB command correctly creates a new user with username 'alice', email, age 28, current timestamp, and empty favorite_posts array?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**What does this MongoDB query do?**

```javascript
db.orders
  .find({
    status: 'completed',
    amount: { $gte: 100, $lte: 500 },
  })
  .sort({ created_at: -1 })
  .limit(10);
```

!!! quiz
{
"title": "MongoDB Query Description",
"question": "What does the query above do?",
"options": ["Find the 10 most recent completed orders with amounts between $100-$500", "Find all completed orders, sort them by amount descending, and return the top 10", "Update 10 completed orders to set their amount between 100 and 500", "Delete completed orders with amounts over $100 but less than $500"],
"answers": ["Find the 10 most recent completed orders with amounts between $100-$500"]
}
!!!

---

!!! quiz
{
"title": "Schema Design Decision",
"question": "An e-commerce app has products with varying attributes (electronics: brand/warranty, clothing: size/color, books: author/ISBN). Which MongoDB schema pattern is BEST?",
"options": ["Create separate collections (electronics, clothing, books) with different schemas", "Use a single products collection with an attributes array of key-value pairs", "Create a relational-style schema with products and attributes tables with foreign keys", "Embed all possible attributes in every product document (nulls for unused fields)"],
"answers": ["Use a single products collection with an attributes array of key-value pairs"]
}
!!!

---

**Requirement:** Increment the `likes` count by 1 and add "trending" to the `tags` array for a post. Don't add "trending" if it already exists in tags. Which MongoDB command correctly implements this?

Option A:

```javascript
db.posts.updateOne(
  { _id: ObjectId('507f1f77bcf86cd799439011') },
  {
    $inc: { likes: 1 },
    $push: { tags: 'trending' },
  },
);
```

Option B:

```javascript
db.posts.updateOne(
  { _id: ObjectId('507f1f77bcf86cd799439011') },
  {
    $inc: { likes: 1 },
    $addToSet: { tags: 'trending' },
  },
);
```

Option C:

```javascript
db.posts.findOneAndUpdate(
  { _id: ObjectId('507f1f77bcf86cd799439011') },
  {
    likes: likes + 1,
    tags: tags.push('trending'),
  },
);
```

Option D:

```javascript
db.posts.update(
  { _id: ObjectId('507f1f77bcf86cd799439011') },
  {
    $set: { likes: likes + 1, tags: ['trending'] },
  },
);
```

!!! quiz
{
"title": "Requirement to MongoDB Update",
"question": "Which MongoDB command correctly increments likes by 1 and adds 'trending' to tags without duplicates?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

**What does this aggregation pipeline produce?**

```javascript
db.orders.aggregate([
  { $match: { status: 'completed' } },
  {
    $group: {
      _id: '$customer_id',
      total_spent: { $sum: '$amount' },
      order_count: { $sum: 1 },
    },
  },
  { $sort: { total_spent: -1 } },
  { $limit: 10 },
]);
```

!!! quiz
{
"title": "Aggregation Pipeline Description",
"question": "What does the aggregation pipeline above produce?",
"options": ["The 10 most expensive individual completed orders", "The top 10 customers by total spending on completed orders, with order counts", "A list of all customers with their average order amount", "The 10 most recent completed orders grouped by customer"],
"answers": ["The top 10 customers by total spending on completed orders, with order counts"]
}
!!!

---

!!! quiz
{
"title": "When to Choose MongoDB",
"question": "Which scenario is MongoDB BEST suited for?",
"options": ["Banking system requiring ACID transactions across multiple tables with complex joins", "Content management system with flexible schema, embedded comments, and rapid prototyping", "Inventory system with strict referential integrity and normalized inventory counts", "Financial ledger requiring strong consistency and multi-statement transactions"],
"answers": ["Content management system with flexible schema, embedded comments, and rapid prototyping"]
}
!!!

---

!!! quiz
{
"title": "Embedding vs Referencing",
"question": "A movie database has movies (5-50 actors each) and actors (10-200 movies each). Which schema design is BEST?",
"options": ["Embed actors array inside each movie document", "Embed movies array inside each actor document", "Use two collections (movies and actors) with actor_ids array in movies", "Use three collections (movies, actors, movie_actors join table)"],
"answers": ["Use two collections (movies and actors) with actor_ids array in movies"]
}
!!!

---

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

Option A (starts from users):

```javascript
db.users.aggregate([
  {
    $lookup: {
      from: 'orders',
      localField: '_id',
      foreignField: 'user_id',
      as: 'orders',
    },
  },
]);
```

Option B:

```javascript
db.orders.aggregate([
  {
    $lookup: {
      from: 'users',
      localField: 'user_id',
      foreignField: '_id',
      as: 'user',
    },
  },
]);
```

Option C:

```javascript
db.users.join(db.orders, {
  left: '_id',
  right: 'user_id',
});
```

Option D:

```javascript
db.users.aggregate([{ $match: { _id: { $in: db.orders.distinct('user_id') } } }]);
```

!!! quiz
{
"title": "MongoDB $lookup (JOIN)",
"question": "Which aggregation correctly fetches users with their orders?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

**Which MongoDB field definition uses the BEST BSON data types?**

Option A (strings for everything):

```javascript
{
  created_at: "2026-03-15T10:00:00Z",  // ISO string
  price: "99.99",                       // String
  tags: "mongodb,database,nosql"        // Comma-separated string
}
```

Option B:

```javascript
{
  created_at: ISODate("2026-03-15T10:00:00Z"),  // BSON Date
  price: NumberDecimal("99.99"),                 // BSON Decimal128
  tags: ["mongodb", "database", "nosql"]         // Array
}
```

Option C:

```javascript
{
  created_at: TIMESTAMP(),
  price: DECIMAL(10, 2),
  tags: ARRAY<STRING>
}
```

Option D:

```javascript
{
  created_at: new Date("2026-03-15"),
  price: 99.99,  // JavaScript number (64-bit float)
  tags: ["mongodb", "database", "nosql"]
}
```

!!! quiz
{
"title": "BSON Data Types",
"question": "Which MongoDB field definition uses the BEST BSON data types?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

**Related Content:**

- [MongoDB Fundamentals](../mongodb-fundamentals.md)
- [Schema Design Patterns](../schema-design-patterns.md)
- [CRUD Operations](../mongodb-crud.md)
- [Aggregation Pipeline](../aggregation-pipeline.md)
