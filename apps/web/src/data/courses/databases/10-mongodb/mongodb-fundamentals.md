# MongoDB Fundamentals

## Introduction to Document Databases

MongoDB is a **document-oriented NoSQL database** that stores data in flexible, JSON-like documents called **BSON** (Binary JSON). Unlike relational databases that store data in rigid tables with fixed schemas, MongoDB allows you to store complex, hierarchical data structures in a single document.

### Why MongoDB?

MongoDB excels in scenarios where:

- **Schema flexibility** is needed (rapid prototyping, evolving requirements)
- **Hierarchical data** needs to be stored naturally (user profiles, product catalogs)
- **Horizontal scalability** is critical (distributed systems, cloud-native apps)
- **Developer productivity** matters (JSON-like syntax, minimal ORM overhead)

## Document Model vs Relational Model

### Relational Database Approach

In a relational database, you'd split related data across multiple tables:

```sql
-- Users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50),
    email VARCHAR(100)
);

-- Posts table
CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id),
    title VARCHAR(200),
    content TEXT,
    created_at TIMESTAMP
);

-- Comments table
CREATE TABLE comments (
    id SERIAL PRIMARY KEY,
    post_id INT REFERENCES posts(id),
    user_id INT REFERENCES users(id),
    text TEXT,
    created_at TIMESTAMP
);
```

To fetch a post with its comments, you need **JOIN operations**:

```sql
SELECT 
    p.title, 
    p.content,
    c.text as comment_text,
    u.username as commenter
FROM posts p
LEFT JOIN comments c ON p.id = c.post_id
LEFT JOIN users u ON c.user_id = u.id
WHERE p.id = 123;
```

### MongoDB Document Approach

In MongoDB, you can **embed related data** in a single document:

```json
{
  "_id": ObjectId("507f1f77bcf86cd799439011"),
  "title": "Introduction to MongoDB",
  "content": "MongoDB is a document database...",
  "author": {
    "username": "johndoe",
    "email": "john@example.com"
  },
  "comments": [
    {
      "user": "janedoe",
      "text": "Great article!",
      "created_at": ISODate("2026-03-15T10:30:00Z")
    },
    {
      "user": "bobsmith",
      "text": "Very helpful, thanks!",
      "created_at": ISODate("2026-03-15T11:45:00Z")
    }
  ],
  "tags": ["database", "nosql", "tutorial"],
  "created_at": ISODate("2026-03-14T09:00:00Z"),
  "updated_at": ISODate("2026-03-15T12:00:00Z")
}
```

**Advantages:**

- ✅ **Single query** to get all data (no JOINs needed)
- ✅ **Natural data structure** mirrors application objects
- ✅ **Atomic operations** on the entire document
- ✅ **Flexible schema** - add fields without ALTER TABLE

**Trade-offs:**

- ❌ **Data duplication** if referencing instead of embedding
- ❌ **Document size limits** (16MB per document in MongoDB)
- ❌ **Update anomalies** if embedded data needs to stay in sync

## JSON vs BSON

### JSON (JavaScript Object Notation)

JSON is a human-readable text format:

```json
{
  "name": "Alice",
  "age": 30,
  "active": true,
  "tags": ["developer", "mongodb"]
}
```

### BSON (Binary JSON)

BSON is MongoDB's binary-encoded serialization format:

- **More data types**: Date, Binary Data, ObjectId, Decimal128, Int32, Int64
- **Efficient storage**: Binary format is more compact than text
- **Faster traversal**: Length-prefixed fields enable quick scanning

**Example of BSON-specific types:**

```javascript
{
  _id: ObjectId("507f191e810c19729de860ea"),        // BSON ObjectId
  name: "Alice",                                     // String
  age: NumberInt(30),                                // 32-bit integer
  salary: NumberDecimal("75000.50"),                 // Decimal128
  created_at: ISODate("2026-03-15T10:30:00.000Z"),  // Date
  avatar: BinData(0, "iVBORw0KGgoAAAANS..."),       // Binary data
  active: true                                       // Boolean
}
```

## The `_id` Field

Every MongoDB document **must have an `_id` field** that serves as the **primary key**.

### Auto-generated ObjectId

If you don't provide an `_id`, MongoDB automatically generates an **ObjectId**:

```javascript
{
  "_id": ObjectId("507f1f77bcf86cd799439011")
}
```

**ObjectId Structure** (12 bytes):

```
4-byte timestamp | 5-byte random value | 3-byte incrementing counter
```

- **Globally unique** across distributed systems
- **Sortable by creation time** (first 4 bytes are Unix timestamp)
- **No coordination required** (unlike auto-increment integers)

### Custom `_id` Values

You can use custom IDs:

```javascript
// String ID
{ "_id": "user-12345", "name": "Alice" }

// Integer ID (for compatibility with relational DBs)
{ "_id": 42, "name": "Bob" }

// UUID
{ "_id": "550e8400-e29b-41d4-a716-446655440000", "name": "Charlie" }
```

⚠️ **Important:** MongoDB will reject inserts if `_id` already exists (unique constraint).

## When to Choose Document Databases

### ✅ **Good Use Cases**

1. **Content Management Systems**
   - Articles, comments, tags stored together
   - Flexible schema for different content types

2. **User Profiles & Social Networks**
   - User data with nested preferences, settings, activity logs
   - Rapidly evolving user attributes

3. **Product Catalogs**
   - Products with varying attributes (electronics vs clothing)
   - Categories, reviews, images embedded

4. **Real-time Analytics & Logging**
   - Event logs with arbitrary metadata
   - Time-series data with flexible schema

5. **Mobile/Gaming Applications**
   - Player profiles with dynamic stats, inventory, achievements
   - Offline-first sync requirements

### ❌ **Poor Use Cases**

1. **Complex Transactions Across Multiple Documents**
   - Banking systems with strict ACID requirements
   - Double-entry accounting

2. **Many-to-Many Relationships**
   - School enrollment system (students ↔ classes)
   - Better handled with relational JOINs

3. **Highly Normalized Data**
   - Reporting systems aggregating across many entities
   - Data warehouse / OLAP workloads

4. **Ad-hoc Queries Across Collections**
   - Complex analytical queries joining 5+ tables
   - SQL excels at this

## Collections and Documents

In MongoDB:

- **Database** contains **Collections**
- **Collection** contains **Documents**

Analogy to relational databases:

| Relational | MongoDB      |
| ---------- | ------------ |
| Database   | Database     |
| Table      | Collection   |
| Row        | Document     |
| Column     | Field        |
| Index      | Index        |
| JOIN       | $lookup      |
| Primary Key | `_id` field |

### Creating Collections

Collections are created **implicitly** when you insert a document:

```javascript
db.users.insertOne({ name: "Alice", email: "alice@example.com" })
// Collection "users" is automatically created
```

Or **explicitly** with options:

```javascript
db.createCollection("users", {
  validator: {
    $jsonSchema: {
      bsonType: "object",
      required: ["name", "email"],
      properties: {
        name: { bsonType: "string" },
        email: { bsonType: "string", pattern: "^.+@.+$" }
      }
    }
  },
  validationLevel: "strict"
})
```

## Docker Setup

Add MongoDB to your `docker-compose.yml`:

```yaml
services:
  mongodb:
    image: mongo:7
    container_name: mongodb
    ports:
      - "27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: admin
      MONGO_INITDB_ROOT_PASSWORD: password
    volumes:
      - mongodb_data:/data/db

volumes:
  mongodb_data:
```

Start the container:

```bash
docker-compose up -d mongodb
```

Connect using MongoDB Shell:

```bash
docker exec -it mongodb mongosh -u admin -p password
```

## Key Takeaways

1. **Documents** are flexible, schema-less JSON-like structures (stored as BSON)
2. **Embedding** related data reduces JOINs but may cause duplication
3. **ObjectId** provides distributed, time-sortable unique identifiers
4. Choose MongoDB when you need **flexibility, scalability, and developer speed**
5. Avoid MongoDB for **complex transactions or highly normalized relational data**

---

**Next:** [Schema Design Patterns](./schema-design-patterns.md)
