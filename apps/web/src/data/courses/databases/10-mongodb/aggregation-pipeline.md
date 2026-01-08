# MongoDB Aggregation Pipeline

## Overview

The **aggregation pipeline** is MongoDB's powerful framework for data analysis. It processes documents through a series of **stages**, where each stage transforms the data.

Think of it like a data processing assembly line:

```
Documents → Stage 1 → Stage 2 → Stage 3 → ... → Results
```

## Basic Syntax

```javascript
db.collection.aggregate([
  { stage1 },
  { stage2 },
  { stage3 }
])
```

Each stage performs an operation (filter, group, sort, etc.) and passes results to the next stage.

## Common Pipeline Stages

### $match

**Filter documents** (like `find()`):

```javascript
// Sample data
db.orders.insertMany([
  { customer: "Alice", amount: 100, status: "completed" },
  { customer: "Bob", amount: 200, status: "pending" },
  { customer: "Alice", amount: 150, status: "completed" },
  { customer: "Charlie", amount: 50, status: "cancelled" }
])

// Get only completed orders
db.orders.aggregate([
  { $match: { status: "completed" } }
])

// Result:
[
  { customer: "Alice", amount: 100, status: "completed" },
  { customer: "Alice", amount: 150, status: "completed" }
]
```

**Best practice:** Place `$match` **early** in the pipeline to reduce documents processed by later stages.

### $project

**Select and transform fields**:

```javascript
// Select specific fields
db.orders.aggregate([
  { $match: { status: "completed" } },
  { $project: { 
      customer: 1,
      amount: 1,
      _id: 0  // Exclude _id
    }
  }
])

// Result:
[
  { customer: "Alice", amount: 100 },
  { customer: "Alice", amount: 150 }
]
```

**Computed fields:**

```javascript
// Add tax calculation
db.orders.aggregate([
  { $project: { 
      customer: 1,
      amount: 1,
      tax: { $multiply: ["$amount", 0.1] },  // 10% tax
      total: { $multiply: ["$amount", 1.1] }
    }
  }
])

// Result:
[
  { customer: "Alice", amount: 100, tax: 10, total: 110 },
  { customer: "Bob", amount: 200, tax: 20, total: 220 },
  ...
]
```

### $group

**Aggregate data** (like SQL's `GROUP BY`):

```javascript
// Total amount per customer
db.orders.aggregate([
  { $match: { status: "completed" } },
  { $group: {
      _id: "$customer",           // Group by customer
      total_spent: { $sum: "$amount" },
      order_count: { $sum: 1 }
    }
  }
])

// Result:
[
  { _id: "Alice", total_spent: 250, order_count: 2 }
]
```

**Group accumulator operators:**

```javascript
db.orders.aggregate([
  { $group: {
      _id: "$customer",
      total: { $sum: "$amount" },        // Sum amounts
      avg: { $avg: "$amount" },          // Average amount
      min: { $min: "$amount" },          // Minimum amount
      max: { $max: "$amount" },          // Maximum amount
      count: { $sum: 1 },                // Count documents
      first_order: { $first: "$amount" }, // First value
      last_order: { $last: "$amount" },   // Last value
      all_amounts: { $push: "$amount" }   // Collect into array
    }
  }
])
```

### $sort

**Sort documents**:

```javascript
// Sort by total spent (descending)
db.orders.aggregate([
  { $group: {
      _id: "$customer",
      total: { $sum: "$amount" }
    }
  },
  { $sort: { total: -1 } }  // 1 = ascending, -1 = descending
])

// Result:
[
  { _id: "Bob", total: 200 },
  { _id: "Alice", total: 250 },
  { _id: "Charlie", total: 50 }
]
```

### $limit and $skip

**Pagination**:

```javascript
// Get top 5 customers
db.orders.aggregate([
  { $group: { _id: "$customer", total: { $sum: "$amount" } } },
  { $sort: { total: -1 } },
  { $limit: 5 }
])

// Get next 5 (page 2)
db.orders.aggregate([
  { $group: { _id: "$customer", total: { $sum: "$amount" } } },
  { $sort: { total: -1 } },
  { $skip: 5 },
  { $limit: 5 }
])
```

### $unwind

**Deconstruct array field** into separate documents:

```javascript
// Sample data
db.posts.insertOne({
  title: "MongoDB Tutorial",
  tags: ["database", "nosql", "mongodb"]
})

db.posts.aggregate([
  { $unwind: "$tags" }
])

// Result (3 documents):
[
  { title: "MongoDB Tutorial", tags: "database" },
  { title: "MongoDB Tutorial", tags: "nosql" },
  { title: "MongoDB Tutorial", tags: "mongodb" }
]
```

**Use case: Count tag usage**

```javascript
db.posts.aggregate([
  { $unwind: "$tags" },
  { $group: { 
      _id: "$tags",
      count: { $sum: 1 }
    }
  },
  { $sort: { count: -1 } }
])

// Result:
[
  { _id: "mongodb", count: 5 },
  { _id: "database", count: 3 },
  { _id: "nosql", count: 2 }
]
```

### $lookup

**Join collections** (like SQL `JOIN`):

```javascript
// Sample data
db.customers.insertMany([
  { _id: 1, name: "Alice", email: "alice@example.com" },
  { _id: 2, name: "Bob", email: "bob@example.com" }
])

db.orders.insertMany([
  { customer_id: 1, amount: 100 },
  { customer_id: 1, amount: 150 },
  { customer_id: 2, amount: 200 }
])

// Join orders with customer info
db.orders.aggregate([
  { $lookup: {
      from: "customers",          // Collection to join
      localField: "customer_id",  // Field in orders
      foreignField: "_id",        // Field in customers
      as: "customer_info"         // Output array field
    }
  }
])

// Result:
[
  {
    customer_id: 1,
    amount: 100,
    customer_info: [
      { _id: 1, name: "Alice", email: "alice@example.com" }
    ]
  },
  ...
]
```

**Flatten joined data:**

```javascript
db.orders.aggregate([
  { $lookup: {
      from: "customers",
      localField: "customer_id",
      foreignField: "_id",
      as: "customer_info"
    }
  },
  { $unwind: "$customer_info" },  // Convert array to object
  { $project: {
      amount: 1,
      customer_name: "$customer_info.name",
      customer_email: "$customer_info.email"
    }
  }
])

// Result:
[
  { amount: 100, customer_name: "Alice", customer_email: "alice@example.com" },
  { amount: 150, customer_name: "Alice", customer_email: "alice@example.com" },
  { amount: 200, customer_name: "Bob", customer_email: "bob@example.com" }
]
```

### $addFields

**Add new computed fields** (preserves existing fields):

```javascript
db.orders.aggregate([
  { $addFields: {
      tax: { $multiply: ["$amount", 0.1] },
      total: { $multiply: ["$amount", 1.1] }
    }
  }
])

// Original fields + tax and total
```

### $count

**Count documents** in pipeline:

```javascript
db.orders.aggregate([
  { $match: { status: "completed" } },
  { $count: "completed_orders" }
])

// Result:
[
  { completed_orders: 42 }
]
```

### $bucket

**Group documents into buckets** (ranges):

```javascript
// Group orders by amount range
db.orders.aggregate([
  { $bucket: {
      groupBy: "$amount",
      boundaries: [0, 50, 100, 200, 500],
      default: "Other",
      output: {
        count: { $sum: 1 },
        orders: { $push: "$amount" }
      }
    }
  }
])

// Result:
[
  { _id: 0, count: 5, orders: [25, 30, 40] },     // 0-50
  { _id: 50, count: 10, orders: [55, 60, ...] },  // 50-100
  { _id: 100, count: 8, orders: [120, 150, ...] }, // 100-200
  ...
]
```

## Practical Examples

### Example 1: Sales Report

Calculate total sales per product category:

```javascript
// Sample data
db.sales.insertMany([
  { product: "Laptop", category: "Electronics", price: 1000, quantity: 2 },
  { product: "Mouse", category: "Electronics", price: 25, quantity: 10 },
  { product: "Desk", category: "Furniture", price: 300, quantity: 1 },
  { product: "Chair", category: "Furniture", price: 150, quantity: 4 }
])

// Pipeline
db.sales.aggregate([
  // Calculate revenue per item
  { $addFields: {
      revenue: { $multiply: ["$price", "$quantity"] }
    }
  },
  
  // Group by category
  { $group: {
      _id: "$category",
      total_revenue: { $sum: "$revenue" },
      items_sold: { $sum: "$quantity" },
      avg_price: { $avg: "$price" }
    }
  },
  
  // Sort by revenue
  { $sort: { total_revenue: -1 } },
  
  // Rename _id to category
  { $project: {
      _id: 0,
      category: "$_id",
      total_revenue: 1,
      items_sold: 1,
      avg_price: { $round: ["$avg_price", 2] }
    }
  }
])

// Result:
[
  { category: "Electronics", total_revenue: 2250, items_sold: 12, avg_price: 512.50 },
  { category: "Furniture", total_revenue: 900, items_sold: 5, avg_price: 225.00 }
]
```

### Example 2: User Activity Summary

Analyze user engagement:

```javascript
// Sample data
db.activities.insertMany([
  { user_id: 1, action: "login", timestamp: ISODate("2026-03-15T09:00:00Z") },
  { user_id: 1, action: "view_post", timestamp: ISODate("2026-03-15T09:05:00Z") },
  { user_id: 1, action: "like", timestamp: ISODate("2026-03-15T09:10:00Z") },
  { user_id: 2, action: "login", timestamp: ISODate("2026-03-15T10:00:00Z") },
  { user_id: 2, action: "comment", timestamp: ISODate("2026-03-15T10:15:00Z") }
])

// Pipeline
db.activities.aggregate([
  // Match last 7 days
  { $match: {
      timestamp: { $gte: ISODate("2026-03-08T00:00:00Z") }
    }
  },
  
  // Group by user
  { $group: {
      _id: "$user_id",
      total_actions: { $sum: 1 },
      actions: { $push: "$action" },
      first_seen: { $min: "$timestamp" },
      last_seen: { $max: "$timestamp" }
    }
  },
  
  // Count unique action types
  { $addFields: {
      unique_actions: { $size: { $setUnion: ["$actions", []] } }
    }
  },
  
  // Sort by activity
  { $sort: { total_actions: -1 } }
])

// Result:
[
  {
    _id: 1,
    total_actions: 3,
    actions: ["login", "view_post", "like"],
    unique_actions: 3,
    first_seen: ISODate("2026-03-15T09:00:00Z"),
    last_seen: ISODate("2026-03-15T09:10:00Z")
  },
  ...
]
```

### Example 3: Nested Lookup

Find users with their posts and comments:

```javascript
db.users.aggregate([
  // Match specific user
  { $match: { username: "alice" } },
  
  // Get user's posts
  { $lookup: {
      from: "posts",
      localField: "_id",
      foreignField: "author_id",
      as: "posts"
    }
  },
  
  // Get user's comments
  { $lookup: {
      from: "comments",
      localField: "_id",
      foreignField: "user_id",
      as: "comments"
    }
  },
  
  // Add counts
  { $addFields: {
      post_count: { $size: "$posts" },
      comment_count: { $size: "$comments" }
    }
  },
  
  // Select fields
  { $project: {
      username: 1,
      email: 1,
      post_count: 1,
      comment_count: 1,
      recent_posts: { $slice: ["$posts.title", 5] }  // Last 5 post titles
    }
  }
])
```

## Pipeline Expressions

### Arithmetic

```javascript
{ $add: ["$price", "$tax"] }
{ $subtract: ["$total", "$discount"] }
{ $multiply: ["$price", "$quantity"] }
{ $divide: ["$total", "$count"] }
{ $mod: ["$value", 10] }
```

### String Operations

```javascript
{ $concat: ["$firstName", " ", "$lastName"] }
{ $toUpper: "$username" }
{ $toLower: "$email" }
{ $substr: ["$text", 0, 50] }  // First 50 chars
{ $split: ["$tags", ","] }     // Split string to array
```

### Date Operations

```javascript
{ $year: "$created_at" }
{ $month: "$created_at" }
{ $dayOfMonth: "$created_at" }
{ $dayOfWeek: "$created_at" }
{ $hour: "$timestamp" }
{ $dateToString: { 
    format: "%Y-%m-%d",
    date: "$created_at"
  }
}
```

### Conditional

```javascript
// if-then-else
{ $cond: {
    if: { $gte: ["$age", 18] },
    then: "adult",
    else: "minor"
  }
}

// Switch case
{ $switch: {
    branches: [
      { case: { $lt: ["$score", 60] }, then: "F" },
      { case: { $lt: ["$score", 70] }, then: "D" },
      { case: { $lt: ["$score", 80] }, then: "C" },
      { case: { $lt: ["$score", 90] }, then: "B" }
    ],
    default: "A"
  }
}
```

## Performance Tips

1. **Use $match early** to reduce documents
2. **Create indexes** on fields used in `$match` and `$sort`
3. **Limit stages** (each stage has overhead)
4. **Use $project** to reduce document size early
5. **Avoid $lookup** when possible (denormalize if read-heavy)
6. **Use explain()** to analyze performance:

```javascript
db.orders.aggregate([...]).explain("executionStats")
```

## Key Takeaways

- **Aggregation pipeline** = sequence of transformation stages
- **$match**: Filter documents (use early!)
- **$group**: Aggregate data with `$sum`, `$avg`, `$min`, `$max`
- **$project**: Select and compute fields
- **$lookup**: Join collections (like SQL JOIN)
- **$unwind**: Deconstruct arrays
- **$sort**, **$limit**, **$skip**: Sort and paginate results
- **Expressions**: Arithmetic, string, date, conditional operations

---

**Next:** [Drizzle + MongoDB Integration](./drizzle-mongodb.md)
