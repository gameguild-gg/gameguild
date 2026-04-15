# MongoDB CRUD Operations

## Overview

CRUD operations are the foundation of working with MongoDB:

- **C**reate: `insertOne()`, `insertMany()`
- **R**ead: `find()`, `findOne()`, `aggregate()`
- **U**pdate: `updateOne()`, `updateMany()`, `replaceOne()`
- **D**elete: `deleteOne()`, `deleteMany()`

## Create Operations

### insertOne()

Insert a **single document** into a collection:

```javascript
db.users.insertOne({
  username: "alice",
  email: "alice@example.com",
  age: 28,
  created_at: new Date()
})

// Result:
{
  acknowledged: true,
  insertedId: ObjectId("507f1f77bcf86cd799439011")
}
```

MongoDB **automatically generates** `_id` if not provided.

### insertMany()

Insert **multiple documents** at once:

```javascript
db.users.insertMany([
  {
    username: "bob",
    email: "bob@example.com",
    age: 32
  },
  {
    username: "charlie",
    email: "charlie@example.com",
    age: 25
  },
  {
    username: "diana",
    email: "diana@example.com",
    age: 30
  }
])

// Result:
{
  acknowledged: true,
  insertedIds: {
    '0': ObjectId("..."),
    '1': ObjectId("..."),
    '2': ObjectId("...")
  }
}
```

**Options:**

```javascript
db.users.insertMany(docs, {
  ordered: false  // Continue inserting even if one fails
})
```

## Read Operations

### findOne()

Fetch the **first matching document**:

```javascript
// Find by _id
db.users.findOne({ _id: ObjectId("507f1f77bcf86cd799439011") })

// Find by field
db.users.findOne({ username: "alice" })

// Returns:
{
  _id: ObjectId("507f1f77bcf86cd799439011"),
  username: "alice",
  email: "alice@example.com",
  age: 28,
  created_at: ISODate("2026-03-15T10:00:00Z")
}
```

Returns `null` if no match found.

### find()

Fetch **all matching documents** (returns cursor):

```javascript
// Find all users
db.users.find()

// Find with filter
db.users.find({ age: { $gte: 30 } })

// Convert cursor to array
db.users.find({ age: { $gte: 30 } }).toArray()
```

### Query Operators

#### Comparison Operators

```javascript
// $eq: Equal to
db.users.find({ age: { $eq: 28 } })
db.users.find({ age: 28 })  // Same as above

// $ne: Not equal to
db.users.find({ age: { $ne: 28 } })

// $gt, $gte: Greater than (or equal)
db.users.find({ age: { $gt: 25 } })
db.users.find({ age: { $gte: 30 } })

// $lt, $lte: Less than (or equal)
db.users.find({ age: { $lt: 30 } })
db.users.find({ age: { $lte: 25 } })

// $in: Match any value in array
db.users.find({ age: { $in: [25, 28, 32] } })

// $nin: Not in array
db.users.find({ age: { $nin: [25, 28] } })
```

#### Logical Operators

```javascript
// $and: Match all conditions
db.users.find({
  $and: [
    { age: { $gte: 25 } },
    { age: { $lte: 30 } }
  ]
})

// Implicit $and (same as above)
db.users.find({ age: { $gte: 25, $lte: 30 } })

// $or: Match any condition
db.users.find({
  $or: [
    { username: "alice" },
    { username: "bob" }
  ]
})

// $nor: Match neither condition
db.users.find({
  $nor: [
    { age: { $lt: 20 } },
    { age: { $gt: 40 } }
  ]
})

// $not: Negate condition
db.users.find({ age: { $not: { $gte: 30 } } })
```

#### Element Operators

```javascript
// $exists: Field exists
db.users.find({ email: { $exists: true } })
db.users.find({ phone: { $exists: false } })

// $type: Field has specific BSON type
db.users.find({ age: { $type: "int" } })
db.users.find({ username: { $type: "string" } })
```

#### Array Operators

```javascript
// Sample data
db.posts.insertOne({
  title: "My Post",
  tags: ["mongodb", "database", "nosql"],
  likes: 100
})

// $all: Array contains all values
db.posts.find({ tags: { $all: ["mongodb", "database"] } })

// $elemMatch: Array element matches all conditions
db.users.insertOne({
  username: "alice",
  scores: [
    { subject: "math", score: 85 },
    { subject: "english", score: 92 }
  ]
})

db.users.find({
  scores: {
    $elemMatch: { subject: "math", score: { $gte: 80 } }
  }
})

// $size: Array has specific length
db.posts.find({ tags: { $size: 3 } })
```

#### String Operators

```javascript
// $regex: Pattern matching
db.users.find({ username: { $regex: /^a/i } })  // Starts with 'a' (case-insensitive)
db.users.find({ email: { $regex: /@example\.com$/ } })  // Ends with @example.com

// Using string pattern
db.users.find({ username: { $regex: "alice", $options: "i" } })
```

### Projections

Select **specific fields** to return:

```javascript
// Include only username and email (exclude _id explicitly)
db.users.find(
  { age: { $gte: 30 } },
  { username: 1, email: 1, _id: 0 }
)

// Returns:
[
  { username: "bob", email: "bob@example.com" },
  { username: "diana", email: "diana@example.com" }
]

// Exclude specific fields
db.users.find(
  {},
  { password: 0, ssn: 0 }  // Exclude password and ssn
)
```

**Rules:**

- Cannot mix inclusion and exclusion (except for `_id`)
- `_id` is **included by default** (must explicitly exclude with `_id: 0`)

### Sorting and Limiting

```javascript
// Sort by age (ascending)
db.users.find().sort({ age: 1 })

// Sort by age (descending)
db.users.find().sort({ age: -1 })

// Sort by multiple fields
db.users.find().sort({ age: -1, username: 1 })

// Limit results
db.users.find().limit(5)

// Skip first N results (pagination)
db.users.find().skip(10).limit(5)

// Combine: Get top 3 oldest users
db.users.find().sort({ age: -1 }).limit(3)

// Pagination (page 2, 10 per page)
db.users.find()
  .sort({ created_at: -1 })
  .skip(10)
  .limit(10)
```

### Counting Documents

```javascript
// Count all documents
db.users.countDocuments()

// Count matching documents
db.users.countDocuments({ age: { $gte: 30 } })

// Deprecated (but faster for large collections with no filter)
db.users.estimatedDocumentCount()
```

## Update Operations

### updateOne()

Update the **first matching document**:

```javascript
// Update alice's age
db.users.updateOne(
  { username: "alice" },         // Filter
  { $set: { age: 29 } }          // Update
)

// Result:
{
  acknowledged: true,
  matchedCount: 1,
  modifiedCount: 1
}
```

### updateMany()

Update **all matching documents**:

```javascript
// Increment age for all users >= 30
db.users.updateMany(
  { age: { $gte: 30 } },
  { $inc: { age: 1 } }
)

// Result:
{
  acknowledged: true,
  matchedCount: 2,
  modifiedCount: 2
}
```

### Update Operators

#### $set

Set field value (creates field if doesn't exist):

```javascript
db.users.updateOne(
  { username: "alice" },
  { $set: { 
      email: "newemail@example.com",
      last_login: new Date()
    }
  }
)
```

#### $unset

Remove field from document:

```javascript
db.users.updateOne(
  { username: "alice" },
  { $unset: { temp_field: "" } }  // Value doesn't matter
)
```

#### $inc

Increment numeric field:

```javascript
// Increase likes by 1
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $inc: { likes: 1 } }
)

// Decrease by 5
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $inc: { likes: -5 } }
)
```

#### $mul

Multiply numeric field:

```javascript
// Double the price
db.products.updateOne(
  { _id: ObjectId("...") },
  { $mul: { price: 2 } }
)
```

#### $rename

Rename field:

```javascript
db.users.updateMany(
  {},
  { $rename: { "username": "user_name" } }
)
```

#### $min / $max

Update only if new value is smaller/larger:

```javascript
// Set age to 25 only if current age > 25
db.users.updateOne(
  { username: "alice" },
  { $min: { age: 25 } }
)

// Set high_score to 100 only if current high_score < 100
db.users.updateOne(
  { username: "alice" },
  { $max: { high_score: 100 } }
)
```

#### $currentDate

Set field to current date/timestamp:

```javascript
db.users.updateOne(
  { username: "alice" },
  { $currentDate: { 
      last_modified: true,           // Sets to Date
      last_access: { $type: "timestamp" }  // Sets to Timestamp
    }
  }
)
```

### Array Update Operators

#### $push

Add element to array:

```javascript
// Add single tag
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $push: { tags: "tutorial" } }
)

// Add multiple tags
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $push: { 
      tags: { 
        $each: ["beginner", "advanced"],
        $sort: 1  // Sort array after push
      }
    }
  }
)
```

#### $addToSet

Add element to array (only if not already present):

```javascript
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $addToSet: { tags: "mongodb" } }  // Won't add duplicates
)

// Add multiple unique values
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $addToSet: { 
      tags: { $each: ["nosql", "database"] }
    }
  }
)
```

#### $pop

Remove first or last element:

```javascript
// Remove last element
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $pop: { tags: 1 } }
)

// Remove first element
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $pop: { tags: -1 } }
)
```

#### $pull

Remove all matching elements:

```javascript
// Remove specific tag
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $pull: { tags: "outdated" } }
)

// Remove elements matching condition
db.users.updateOne(
  { username: "alice" },
  { $pull: { 
      scores: { score: { $lt: 70 } }  // Remove scores < 70
    }
  }
)
```

#### $pullAll

Remove multiple specific values:

```javascript
db.posts.updateOne(
  { _id: ObjectId("...") },
  { $pullAll: { tags: ["outdated", "deprecated"] } }
)
```

#### $ positional operator

Update **first matching** array element:

```javascript
// Increment score for math subject
db.users.updateOne(
  { username: "alice", "scores.subject": "math" },
  { $inc: { "scores.$.score": 5 } }
)
```

#### $[] all positional operator

Update **all** array elements:

```javascript
// Add 10 points to all scores
db.users.updateOne(
  { username: "alice" },
  { $inc: { "scores.$[].score": 10 } }
)
```

#### $[identifier] filtered positional

Update array elements matching condition:

```javascript
// Increase scores >= 90 by 5 points
db.users.updateOne(
  { username: "alice" },
  { $inc: { "scores.$[elem].score": 5 } },
  { arrayFilters: [{ "elem.score": { $gte: 90 } }] }
)
```

### replaceOne()

Replace **entire document** (except `_id`):

```javascript
db.users.replaceOne(
  { username: "alice" },
  {
    username: "alice",
    email: "alice@newdomain.com",
    age: 30,
    role: "admin"
    // Old fields (not listed here) are removed
  }
)
```

### Upsert

Insert if not found, update if exists:

```javascript
db.users.updateOne(
  { username: "eve" },
  { $set: { email: "eve@example.com", age: 27 } },
  { upsert: true }  // Insert if username "eve" doesn't exist
)
```

## Delete Operations

### deleteOne()

Delete **first matching document**:

```javascript
db.users.deleteOne({ username: "alice" })

// Result:
{
  acknowledged: true,
  deletedCount: 1
}
```

### deleteMany()

Delete **all matching documents**:

```javascript
// Delete all users under 18
db.users.deleteMany({ age: { $lt: 18 } })

// Delete ALL documents (be careful!)
db.users.deleteMany({})
```

### Soft Delete Pattern

Instead of deleting, mark as deleted:

```javascript
// Add deleted flag
db.users.updateOne(
  { username: "alice" },
  { 
    $set: { 
      deleted: true,
      deleted_at: new Date()
    }
  }
)

// Query non-deleted users
db.users.find({ deleted: { $ne: true } })

// Or use null for deleted field
db.users.find({ deleted: null })
```

## Key Takeaways

- **insertOne/Many**: Create documents
- **find/findOne**: Read with filters, projections, sorting
- **Query operators**: `$gte`, `$in`, `$regex`, `$and`, `$or`, `$exists`
- **updateOne/Many**: Modify documents with `$set`, `$inc`, `$push`, `$pull`
- **Array operators**: `$push`, `$addToSet`, `$pull`, positional `$`
- **deleteOne/Many**: Remove documents (consider soft delete pattern)
- **Upsert**: Insert if not exists, update if exists

---

**Next:** [MongoDB Aggregation Pipeline](./aggregation-pipeline.md)
