# Quiz 8: Document Databases - MongoDB Fundamentals

---

## Document Model vs Relational

You're building a blog platform. Each blog post can have 0-100 comments. Users can write many posts and many comments. Which schema design approach is BEST for MongoDB?

- [x] Store posts in one collection and comments in another collection (reference comments by post_id)
- [ ] Embed all comments inside each post document
- [ ] Store everything in a single denormalized collection with duplicated user data
- [ ] Use three collections (users, posts, comments) with foreign key references like a relational database

Comments can grow to hundreds per post, risking the 16MB document size limit with embedding. Referencing allows independent queries like "all comments by user X" and avoids document size issues.

---

## JSON vs BSON

Which statement about BSON is TRUE?

- [ ] BSON is human-readable text format identical to JSON
- [x] BSON supports additional data types like Date, Binary, and ObjectId that JSON cannot represent
- [ ] BSON is always larger in size than equivalent JSON due to metadata overhead
- [ ] MongoDB stores documents as JSON internally and converts to BSON only during network transmission

BSON (Binary JSON) extends JSON with types like `Date`, `ObjectId`, `Binary`, `Decimal128`, `Int32`, `Int64`, etc. MongoDB stores documents as BSON internally, and drivers convert between JSON and BSON during read/write operations.

---

## ObjectId Structure

Given this ObjectId: `507f1f77bcf86cd799439011`, what information does it encode?

- [ ] Only a random unique identifier (like UUID)
- [x] Timestamp (4 bytes) + random value (5 bytes) + counter (3 bytes)
- [ ] User ID (4 bytes) + collection name (4 bytes) + sequence number (4 bytes)
- [ ] Creation date, server hostname, and process ID

An ObjectId is 12 bytes (24 hex characters): a Unix timestamp (4 bytes), a random value (5 bytes), and an incrementing counter (3 bytes). This makes it time-sortable and globally unique without coordination.

---

## Inserting a Document

A user registration system needs to create a new user with username "alice", email "alice@example.com", age 28, a created timestamp, and an empty favorites array. Which command is correct?

- [x] `db.users.insertOne({ username: "alice", email: "alice@example.com", age: 28, created_at: new Date(), favorite_posts: [] })`
- [ ] `db.users.insert({ username: "alice", email: "alice@example.com", age: "28", created_at: NOW(), favorite_posts: null })`
- [ ] `INSERT INTO users (username, email, age, created_at, favorite_posts) VALUES ('alice', 'alice@example.com', 28, CURRENT_TIMESTAMP, [])`
- [ ] `db.users.save({ _id: ObjectId(), username: "alice", email: "alice@example.com", age: 28 })`

Use `insertOne()` (the modern method) with proper JavaScript types: numbers for numeric fields (not strings), `new Date()` for timestamps (not `NOW()`), and `[]` for empty arrays (not `null`). The `save()` method is deprecated.

---

## Reading a Query

What does this query do? `db.orders.find({ status: "completed", amount: { $gte: 100, $lte: 500 } }).sort({ created_at: -1 }).limit(10)`

- [x] Find the 10 most recent completed orders with amounts between $100-$500
- [ ] Find all completed orders, sort them by amount descending, and return the top 10
- [ ] Update 10 completed orders to set their amount between 100 and 500
- [ ] Delete completed orders with amounts over $100 but less than $500

`.find()` is a read operation (not update or delete). The filter matches `status: "completed"` AND `amount` between 100 and 500. The sort is on `created_at` (not amount), and `-1` means descending (most recent first). `.limit(10)` restricts to 10 results.

---

## Attribute Pattern

An e-commerce app has products with varying attributes (electronics have brand/warranty, clothing has size/color, books have author/ISBN). Which schema pattern is BEST?

- [ ] Create separate collections (electronics, clothing, books) with different schemas
- [x] Use a single `products` collection with an `attributes` array of key-value pairs
- [ ] Create a relational-style schema with products and attributes tables with foreign keys
- [ ] Embed all possible attributes in every product document (nulls for unused fields)

The Attribute Pattern stores varying fields as `[{ "k": "brand", "v": "Dell" }, ...]` in a single collection. This keeps cross-category queries simple, allows flexible indexing on `attributes.k` and `attributes.v`, and avoids wasting space with null fields.

---

## $addToSet vs $push

Increment the `likes` count by 1 and add "trending" to the `tags` array without duplicates for a specific post. Which command is correct?

- [ ] `db.posts.updateOne({ _id: ObjectId("507f1f77bcf86cd799439011") }, { $inc: { likes: 1 }, $push: { tags: "trending" } })`
- [x] `db.posts.updateOne({ _id: ObjectId("507f1f77bcf86cd799439011") }, { $inc: { likes: 1 }, $addToSet: { tags: "trending" } })`
- [ ] `db.posts.findOneAndUpdate({ _id: ObjectId("507f1f77bcf86cd799439011") }, { likes: likes + 1, tags: tags.push("trending") })`
- [ ] `db.posts.update({ _id: ObjectId("507f1f77bcf86cd799439011") }, { $set: { likes: likes + 1, tags: ["trending"] } })`

`$inc` atomically increments a numeric field. `$addToSet` adds a value to an array only if it doesn't already exist, preventing duplicates. `$push` always appends (allowing duplicates), and `$set` on the array would replace the entire array.

---

## Aggregation Pipeline

What does this pipeline produce? `db.orders.aggregate([ { $match: { status: "completed" } }, { $group: { _id: "$customer_id", total_spent: { $sum: "$amount" }, order_count: { $sum: 1 } } }, { $sort: { total_spent: -1 } }, { $limit: 10 } ])`

- [ ] The 10 most expensive individual completed orders
- [x] The top 10 customers by total spending on completed orders, with order counts
- [ ] A list of all customers with their average order amount
- [ ] The 10 most recent completed orders grouped by customer

`$match` filters completed orders, `$group` groups by `customer_id` and sums amounts/counts orders, `$sort` orders by `total_spent` descending, and `$limit` takes the top 10. The `$sum` operator calculates totals (not averages), and the result is grouped per customer (not per order).

---

## When to Choose MongoDB

Which scenario is MongoDB BEST suited for?

- [ ] Banking system requiring ACID transactions across multiple tables with complex joins
- [x] Content management system with flexible schema, embedded comments, and rapid prototyping
- [ ] Inventory system with strict referential integrity and normalized inventory counts
- [ ] Financial ledger requiring strong consistency and multi-statement transactions

MongoDB excels with flexible schemas (add fields without migrations), embedded data (comments/tags inside posts for fewer queries), and rapid prototyping (no upfront schema design). Banking, inventory, and financial ledgers require strict ACID transactions, referential integrity, and consistency better served by relational databases.

---

## Many-to-Many Relationships

A movie database has movies (5-50 actors each) and actors (10-200 movies each). Which schema design is BEST?

- [ ] Embed actors array inside each movie document
- [ ] Embed movies array inside each actor document
- [x] Use two collections (movies and actors) with an `actor_ids` array in movies
- [ ] Use three collections (movies, actors, movie_actors join table)

Referencing with an array of IDs avoids duplicating actor data across 100+ movies. The array in movies is bounded (5-50 actors), and you can use `$lookup` to fetch actor details. Embedding would cause massive duplication, and a join table over-normalizes for MongoDB.

---

## $lookup (JOIN)

Given a `users` collection `{ _id: 1, name: "Alice" }` and an `orders` collection `{ _id: 101, user_id: 1, amount: 100 }`, which aggregation fetches users with their orders?

- [x] `db.users.aggregate([ { $lookup: { from: "orders", localField: "_id", foreignField: "user_id", as: "orders" } } ])`
- [ ] `db.orders.aggregate([ { $lookup: { from: "users", localField: "user_id", foreignField: "_id", as: "user" } } ])`
- [ ] `db.users.join(db.orders, { left: "_id", right: "user_id" })`
- [ ] `db.users.aggregate([ { $match: { _id: { $in: db.orders.distinct("user_id") } } } ])`

`$lookup` starts from the "left" collection (users), matches `localField` against `foreignField` in the "from" collection, and outputs matches as an array. Starting from orders would give orders-with-user rather than users-with-orders. MongoDB has no `.join()` method.

---

## BSON Data Types

Which MongoDB field definition uses the most precise BSON types?

- [ ] `{ created_at: "2026-03-15T10:00:00Z", price: "99.99", tags: "mongodb,database,nosql" }`
- [x] `{ created_at: ISODate("2026-03-15T10:00:00Z"), price: NumberDecimal("99.99"), tags: ["mongodb", "database", "nosql"] }`
- [ ] `{ created_at: TIMESTAMP(), price: DECIMAL(10, 2), tags: ARRAY<STRING> }`
- [ ] `{ created_at: new Date("2026-03-15"), price: 99.99, tags: ["mongodb", "database", "nosql"] }`

`ISODate()` creates a BSON Date, and `NumberDecimal()` creates a BSON Decimal128 with exact precision (no floating-point rounding). Storing dates and numbers as strings prevents proper sorting and comparison. `TIMESTAMP()` and `DECIMAL(10,2)` are SQL syntax, not MongoDB.

---

## Maximum Document Size

What is the maximum size of a single document in MongoDB?

- [ ] 4 MB
- [x] 16 MB
- [ ] 64 MB
- [ ] No limit

MongoDB enforces a 16 MB maximum document size. This limit prevents excessively large documents that would consume too much RAM and bandwidth. For data exceeding this limit, use GridFS or reference data across multiple documents.

---

## Delete Operations

Which command deletes all orders with status "cancelled" that are older than 2025?

- [ ] `db.orders.remove({ status: "cancelled", created_at: { $lt: ISODate("2025-01-01") } })`
- [x] `db.orders.deleteMany({ status: "cancelled", created_at: { $lt: ISODate("2025-01-01") } })`
- [ ] `DELETE FROM orders WHERE status = 'cancelled' AND created_at < '2025-01-01'`
- [ ] `db.orders.drop({ status: "cancelled", created_at: { $lt: ISODate("2025-01-01") } })`

`deleteMany()` is the modern method to remove multiple matching documents. `remove()` is deprecated. SQL `DELETE FROM` syntax doesn't work in MongoDB. `drop()` deletes an entire collection (not filtered documents).

---

## Indexing Basics

Which statement about MongoDB indexes is TRUE?

- [ ] MongoDB automatically creates indexes on all fields when a document is inserted
- [ ] Indexes slow down read queries but speed up write operations
- [x] A compound index `{ last_name: 1, first_name: 1 }` can efficiently support queries filtering on `last_name` alone
- [ ] You should create an index on every field to maximize query performance

Compound indexes follow a left-to-right prefix rule: an index on `{ last_name: 1, first_name: 1 }` supports queries on `last_name` alone or on both fields, but not on `first_name` alone. Indexes speed up reads but slow down writes (each insert/update must also update the index). Only `_id` gets an automatic index.

---

## $unwind Operator

What does the `$unwind` stage do in an aggregation pipeline?

- [ ] Combines multiple documents into a single document
- [ ] Removes duplicate values from an array field
- [x] Deconstructs an array field, outputting one document per array element
- [ ] Sorts array elements within each document

`$unwind` takes a document with an array field and produces one output document per array element. For example, a document with `tags: ["a", "b", "c"]` becomes three documents, each with a single tag value. This is useful before grouping or counting array elements.

---

## Upsert Operation

What does the `upsert: true` option do in an update operation?

- [ ] Updates all matching documents instead of just the first one
- [ ] Prevents the update if the document already exists
- [x] Inserts a new document if no document matches the filter; otherwise updates the matching document
- [ ] Replaces the entire document instead of modifying specific fields

Upsert combines "update" and "insert": if the filter matches a document, it updates it; if no match is found, it creates a new document using the filter criteria and update values. This is useful for "create or update" patterns without needing two separate operations.

---

## $project Stage

What does the `$project` stage do in an aggregation pipeline?

- [x] Selects which fields to include or exclude in the output, and can compute new fields
- [ ] Filters documents based on a condition
- [ ] Groups documents by a specified field
- [ ] Joins data from another collection

`$project` reshapes documents by including/excluding fields (`field: 1` or `field: 0`) and creating computed fields (e.g., `{ fullName: { $concat: ["$first", " ", "$last"] } }`). Filtering is done by `$match`, grouping by `$group`, and joining by `$lookup`.

---

## Write Concern

What does `writeConcern: { w: "majority" }` mean in MongoDB?

- [ ] The write is acknowledged after being written to the primary node only
- [x] The write is acknowledged after being replicated to a majority of replica set members
- [ ] The write is cached in memory and flushed to disk later
- [ ] The write is sent to all nodes simultaneously without waiting for acknowledgment

With `w: "majority"`, MongoDB waits until the write has been replicated to more than half the replica set members before confirming success. This provides stronger durability guarantees than `w: 1` (primary only) at the cost of higher latency. `w: 0` means no acknowledgment at all.

---

## Schema Validation

How can you enforce that a `price` field must be a positive number in a MongoDB collection?

- [ ] MongoDB cannot enforce field-level validations; this must be done in application code
- [ ] `db.products.createConstraint({ price: { type: "number", min: 0 } })`
- [x] `db.createCollection("products", { validator: { $jsonSchema: { properties: { price: { bsonType: "double", minimum: 0 } } } } })`
- [ ] `ALTER COLLECTION products ADD CHECK (price > 0)`

MongoDB supports JSON Schema validation via the `validator` option with `$jsonSchema`. You can enforce BSON types, required fields, min/max values, patterns, and more at the database level. `ALTER COLLECTION` and `createConstraint` are not MongoDB syntax.

---

## Transactions in MongoDB

Which statement about multi-document transactions in MongoDB is TRUE?

- [ ] MongoDB does not support multi-document transactions at all
- [ ] Transactions are only available on standalone MongoDB instances
- [x] Multi-document ACID transactions are supported on replica sets and sharded clusters starting from MongoDB 4.0/4.2
- [ ] Every MongoDB operation is automatically wrapped in a transaction

MongoDB added multi-document ACID transactions in version 4.0 (replica sets) and 4.2 (sharded clusters). However, transactions are not automatic — you must explicitly start a session and use `startTransaction()`. Single-document operations are always atomic without transactions.
