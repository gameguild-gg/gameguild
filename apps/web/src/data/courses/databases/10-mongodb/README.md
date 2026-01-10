# Week 10 - Document Databases: MongoDB

**Dates:** March 16-20, 2026  
**Topic:** MongoDB Fundamentals & CRUD Operations  
**Assessment:** Quiz 8 - Document DB Concepts

---

## Overview

This week introduces **MongoDB**, a popular document-oriented NoSQL database. You'll learn when to choose document databases over relational databases, how to design schemas using embedding vs referencing patterns, and how to perform CRUD operations and aggregations.

### Learning Objectives

By the end of this week, you will be able to:

1. **Explain** the document model vs relational model
2. **Differentiate** between JSON and BSON data formats
3. **Design** MongoDB schemas using embedding and referencing patterns
4. **Implement** CRUD operations (insertOne, find, updateOne, deleteOne)
5. **Write** aggregation pipelines using $match, $group, $project, $lookup
6. **Integrate** MongoDB with Drizzle ORM for type-safe queries
7. **Choose** the appropriate database (document vs relational) for different use cases

---

## Weekly Schedule

### Monday, March 16 - MongoDB Fundamentals

**Topics:**
- Document model vs relational model
- JSON vs BSON (Binary JSON)
- ObjectId structure and generation
- When to choose document databases
- Collections and documents
- Docker setup for MongoDB

**Readings:**
- [MongoDB Fundamentals](./mongodb-fundamentals.md)
- [Schema Design Patterns](./schema-design-patterns.md)

**Activities:**
- Set up MongoDB Docker container
- Connect to MongoDB using MongoDB Compass
- Explore sample databases (restaurants, movies)

---

### Thursday, March 19 - CRUD & Aggregation

**Topics:**
- **CRUD Operations:**
  - insertOne/insertMany
  - find with query operators ($gte, $in, $regex)
  - Projections and sorting
  - Update operators ($set, $inc, $push, $pull)
  - deleteOne/deleteMany

- **Aggregation Pipeline:**
  - Pipeline stages: $match, $group, $project, $sort, $limit
  - $lookup for joining collections
  - $unwind for array deconstruction
  - Aggregation expressions (arithmetic, string, date)

- **Drizzle + MongoDB:**
  - Type-safe schema definitions
  - CRUD operations with Drizzle
  - Query operators (eq, gte, and, or)
  - Limitations and when to use native driver

**Readings:**
- [MongoDB CRUD Operations](./mongodb-crud.md)
- [Aggregation Pipeline](./aggregation-pipeline.md)
- [Drizzle + MongoDB](./drizzle-mongodb.md)
- [Recommended Resources](./readings-10.md)

**Activities:**
- Practice CRUD operations on sample data
- Write aggregation queries for analytics
- Build a simple Express + MongoDB API with Drizzle

---

## Assessment

### Quiz 8 - Document Databases (Due: Thursday, March 19)

**Topics Covered:**
- Document model vs relational model
- JSON vs BSON data types
- ObjectId structure
- Schema design patterns (embedding vs referencing)
- When to choose MongoDB
- CRUD operations (insertOne, find, updateOne)
- Update operators ($set, $inc, $push, $addToSet)
- Aggregation pipeline ($match, $group, $lookup)

**Format:**
- 12 multiple-choice questions
- Requirement → MongoDB code
- MongoDB code → Description
- Schema design scenarios

**Preparation:**
- Complete all readings
- Practice CRUD operations
- Write aggregation queries
- Review quiz materials

[Take Quiz 8](./quiz/mongodb-quiz.md)

---

## Final Project Milestone

**Team Formation + Proposal (Due: Sunday, March 22)**

This week marks the start of your **final project**. You should:

1. **Form a team** (3-4 students)
2. **Brainstorm project ideas** that incorporate both SQL and NoSQL databases
3. **Write a project proposal** (1-2 pages) including:
   - Project description and goals
   - Database architecture (PostgreSQL + MongoDB?)
   - Schema design (both relational and document)
   - Key features and user stories
   - Timeline and milestones

**Example Project Ideas:**
- Social media platform (users/posts in PostgreSQL, comments/likes in MongoDB)
- E-commerce site (products/orders in SQL, product reviews/ratings in MongoDB)
- Learning management system (courses/users in SQL, course content/quizzes in MongoDB)
- Real-time analytics dashboard (time-series data in MongoDB, aggregated reports in SQL)

---

## Weekly Content

### Required Readings

1. **[MongoDB Fundamentals](./mongodb-fundamentals.md)** (60 min)
   - Document model, JSON/BSON, ObjectId, use cases

2. **[Schema Design Patterns](./schema-design-patterns.md)** (75 min)
   - Embedding vs referencing, one-to-many, attribute/bucket/subset patterns

3. **[MongoDB CRUD Operations](./mongodb-crud.md)** (90 min)
   - insertOne/Many, find with operators, update operators, delete operations

4. **[Aggregation Pipeline](./aggregation-pipeline.md)** (90 min)
   - Pipeline stages, $match, $group, $lookup, practical examples

5. **[Drizzle + MongoDB](./drizzle-mongodb.md)** (60 min)
   - Type-safe schemas, CRUD with Drizzle, query operators, limitations

### Supplemental Resources

6. **[MongoDB Readings & Resources](./readings-10.md)** (30 min)
   - Official documentation, tutorials, best practices, tools

---

## Key Concepts

### Document Model

- **Documents** are JSON-like objects stored in collections
- **Schema-less** (fields can vary between documents)
- **Embedded documents** for one-to-few relationships
- **References** (ObjectIds) for one-to-many and many-to-many

### BSON (Binary JSON)

- **Extended data types**: Date, ObjectId, Binary, Decimal128, etc.
- **Efficient storage and traversal**
- **16MB document size limit**

### Schema Design Principles

- **Model for your queries** (not just entity relationships)
- **Embed when data is accessed together**
- **Reference when data grows unbounded or changes frequently**
- **Denormalize for read performance** (duplicate data if reads >> writes)

### CRUD Best Practices

- Use `insertOne()` for single documents, `insertMany()` for bulk inserts
- Use query operators (`$gte`, `$in`, `$regex`) for complex filters
- Use projections to limit returned fields
- Use `$set`, `$inc`, `$push` for partial updates (don't replace entire document)
- Use `$addToSet` to prevent array duplicates

### Aggregation Pipeline

- **Pipeline stages** process documents sequentially
- **$match early** to reduce documents processed
- **$group** aggregates data with `$sum`, `$avg`, `$max`, `$min`
- **$lookup** joins collections (like SQL JOIN)
- **$unwind** deconstructs arrays into separate documents

---

## Practical Exercises

### Exercise 1: Schema Design

Design a MongoDB schema for a **blog platform**:
- Users (username, email, created_at)
- Posts (title, content, author, tags, likes, created_at)
- Comments (post_id, user_id, text, created_at)

**Decisions:**
- Should comments be **embedded** in posts or in a **separate collection**?
- Should user info be **duplicated** in comments or **referenced**?
- How would you handle posts with **1000+ comments**?

### Exercise 2: CRUD Operations

Implement the following operations:

1. Insert 5 users with different ages
2. Find all users aged 25-35
3. Increment all users' age by 1
4. Add "featured" tag to a specific post
5. Delete all users under 18

### Exercise 3: Aggregation

Write aggregation queries to:

1. Find the top 10 most-liked posts
2. Calculate average age of users by country
3. Count how many times each tag is used across all posts
4. Find users with the most posts (author leaderboard)
5. Join posts with user info to get author names

### Exercise 4: Drizzle Integration

Build a simple Express API with:
- `POST /users` - Create user
- `GET /users/:id` - Get user by ID
- `GET /posts?author_id=...` - Get posts by author
- `PUT /posts/:id/like` - Increment post likes
- `GET /posts/top` - Get top 10 posts by likes (use aggregation)

---

## Common Pitfalls

### ❌ Embedding Too Much Data

**Problem:** Embedding all comments in a post document
```javascript
{
  title: "Popular Post",
  comments: [ /* 10,000 comments */ ]  // Document too large!
}
```

**Solution:** Use referencing for unbounded data

### ❌ Over-Normalizing

**Problem:** Treating MongoDB like SQL with many references
```javascript
// ❌ Too many collections
users → posts → comments → likes → tags
```

**Solution:** Embed related data that's accessed together

### ❌ Ignoring Indexes

**Problem:** Slow queries on large collections
```javascript
db.posts.find({ author_id: ObjectId("...") })  // Slow without index
```

**Solution:** Create indexes on frequently queried fields
```javascript
db.posts.createIndex({ author_id: 1 })
```

### ❌ Using $lookup Excessively

**Problem:** Multiple $lookup stages (like SQL JOINs)
```javascript
db.posts.aggregate([
  { $lookup: { from: "users", ... } },
  { $lookup: { from: "comments", ... } },
  { $lookup: { from: "likes", ... } }
])
```

**Solution:** Denormalize by embedding or caching frequently accessed data

---

## Tools & Setup

### MongoDB Docker

```yaml
# docker-compose.yml
version: '3.8'
services:
  mongo:
    image: mongo:7
    container_name: mongodb
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: password
    volumes:
      - mongo-data:/data/db

volumes:
  mongo-data:
```

```bash
docker-compose up -d
```

### MongoDB Compass

Download and install: https://www.mongodb.com/products/compass

**Connection String:**
```
mongodb://admin:password@localhost:27017
```

### MongoDB Shell (mongosh)

```bash
# Connect to MongoDB
docker exec -it mongodb mongosh -u admin -p password

# Create database
use mydb

# Insert document
db.users.insertOne({ username: "alice", age: 28 })

# Find documents
db.users.find()
```

---

## Next Steps

After completing this week's content:

1. ✅ **Complete Quiz 8** on document databases
2. ✅ **Form a team** for the final project
3. ✅ **Write project proposal** using both SQL and MongoDB
4. 📚 **Preview Week 11** (Advanced MongoDB, Transactions, Performance)

---

**Questions or feedback?** Post in the course discussion forum or office hours.

**Happy coding! 🚀**
