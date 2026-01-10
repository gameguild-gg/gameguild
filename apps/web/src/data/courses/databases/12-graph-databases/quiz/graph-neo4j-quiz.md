# Quiz 10 — Graph Databases & Neo4j

**Total Points:** 100  
**Passing Score:** 70%  
**Time Limit:** 45 minutes

---

## Instructions

- Read each question carefully
- Select the **best** answer for each multiple-choice question
- Some questions require analyzing Cypher queries or graph patterns
- Review your answers before submitting

---

## Questions

### Question 1: Graph Model Fundamentals (8 points)

Which of the following best describes the **labeled property graph model** used by Neo4j?

**A)** Nodes can have labels, properties, and relationships; relationships can also have properties  
**B)** Nodes contain only labels and relationships contain only properties  
**C)** Nodes and relationships share the same property namespace  
**D)** Relationships cannot have properties, only directionality

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: A**

**Explanation:**

✅ **A is correct** — Neo4j uses the labeled property graph model where:
- Nodes can have one or more **labels** (e.g., `:Person`, `:Admin`)
- Nodes can have **properties** (key-value pairs like `{name: "Alice", age: 30}`)
- Relationships connect nodes and can have **types** (e.g., `:FRIENDS_WITH`)
- Relationships can also have **properties** (e.g., `{since: 2020, strength: 0.8}`)

❌ **B is incorrect** — Both nodes and relationships can have properties.

❌ **C is incorrect** — Nodes and relationships have separate property namespaces.

❌ **D is incorrect** — Relationships CAN have properties in addition to directionality.

**Key Concept:** The labeled property graph model allows flexible schema design with rich metadata on both nodes and relationships.

</details>

---

### Question 2: When to Use Graph Databases (8 points)

For which of the following use cases would Neo4j be the **most appropriate** choice compared to a relational database?

**A)** Storing transactional e-commerce order data with strong ACID guarantees  
**B)** Finding friends-of-friends-of-friends in a social network  
**C)** Aggregating sales data across millions of rows for monthly reports  
**D)** Managing user authentication and password hashing

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — Graph databases excel at **deep relationship traversals**:
- Finding friends-of-friends (2 hops) is fast with Neo4j's index-free adjacency
- SQL would require multiple self-joins (`users JOIN friendships f1 JOIN friendships f2 JOIN friendships f3`)
- Performance degrades exponentially in SQL for deep joins (6+ levels)
- Neo4j maintains constant-time traversal complexity (O(1) per hop)

❌ **A is incorrect** — E-commerce transactions are better suited for relational databases (PostgreSQL) due to:
- ACID guarantees at scale
- Complex aggregations and reporting
- Simple one-to-many relationships (order → items)

❌ **C is incorrect** — Aggregations over millions of rows are better handled by columnar databases or SQL databases optimized for analytics (PostgreSQL, ClickHouse).

❌ **D is incorrect** — Authentication is a simple CRUD operation better suited for relational databases.

**Decision Framework:**
- **Use Graph DB:** Relationship-heavy queries, recommendations, fraud detection, pattern matching
- **Use Relational DB:** Transactions, aggregations, simple relationships, reporting

</details>

---

### Question 3: Cypher Pattern Matching (8 points)

What does the following Cypher query return?

```cypher
MATCH (a:Person)-[:FRIENDS_WITH]-(b:Person)
WHERE a.name = "Alice"
RETURN b.name;
```

**A)** All people who Alice sent friend requests to (directed outgoing)  
**B)** All people who sent friend requests to Alice (directed incoming)  
**C)** All people who are friends with Alice (bidirectional or either direction)  
**D)** An error because the relationship direction is not specified

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: C**

**Explanation:**

✅ **C is correct** — The pattern `-[:FRIENDS_WITH]-` (without arrows) matches relationships in **any direction**:

```cypher
// Matches both:
(alice)-[:FRIENDS_WITH]->(bob)   // Outgoing
(charlie)-[:FRIENDS_WITH]->(alice)  // Incoming
```

This returns all friends regardless of who initiated the friendship.

❌ **A is incorrect** — That would require `-[:FRIENDS_WITH]->` (outgoing arrow).

❌ **B is incorrect** — That would require `<-[:FRIENDS_WITH]-` (incoming arrow).

❌ **D is incorrect** — Undirected patterns are valid in Cypher. Direction is optional.

**Comparison:**

```cypher
// Outgoing only (Alice → friend)
MATCH (a:Person)-[:FRIENDS_WITH]->(b:Person)
WHERE a.name = "Alice"
RETURN b.name;

// Incoming only (friend → Alice)
MATCH (a:Person)<-[:FRIENDS_WITH]-(b:Person)
WHERE a.name = "Alice"
RETURN b.name;

// Any direction (Alice ↔ friend)
MATCH (a:Person)-[:FRIENDS_WITH]-(b:Person)
WHERE a.name = "Alice"
RETURN b.name;
```

</details>

---

### Question 4: Variable-Length Paths (8 points)

Which Cypher query finds all people connected to Alice within **exactly 3 hops**?

**A)** `MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*3]->(p) RETURN DISTINCT p;`  
**B)** `MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*1..3]->(p) RETURN DISTINCT p;`  
**C)** `MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH]->(p) RETURN DISTINCT p LIMIT 3;`  
**D)** `MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]->(p) WHERE length(path) = 3 RETURN DISTINCT p;`

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: A**

**Explanation:**

✅ **A is correct** — `*3` means **exactly 3 hops**:

```cypher
(alice)-[:FRIENDS_WITH]->(f1)-[:FRIENDS_WITH]->(f2)-[:FRIENDS_WITH]->(p)
// Exactly 3 relationships traversed
```

❌ **B is incorrect** — `*1..3` means **1 to 3 hops** (inclusive):
- Returns friends (1 hop)
- Returns friends-of-friends (2 hops)
- Returns friends-of-friends-of-friends (3 hops)

This matches MORE people than just 3-hop connections.

❌ **C is incorrect** — This finds **direct friends** (1 hop) and limits results to 3 people. Not the same as "3 hops away."

❌ **D is incorrect** — Syntax error. `length()` requires a path variable:

```cypher
// Correct syntax for D
MATCH path = (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]-(p)
WHERE length(path) = 3
RETURN DISTINCT p;
```

But this is unnecessarily complex compared to A.

**Variable-Length Syntax:**

```cypher
*      // Any number of hops (dangerous!)
*1..3  // 1 to 3 hops (inclusive)
*2..   // 2 or more hops
*..4   // Up to 4 hops
*5     // Exactly 5 hops
```

</details>

---

### Question 5: MERGE vs CREATE (8 points)

What is the difference between `CREATE` and `MERGE` in Neo4j?

**A)** `CREATE` always creates a new node; `MERGE` creates only if the pattern doesn't exist (upsert)  
**B)** `MERGE` is faster because it uses indexes; `CREATE` does not  
**C)** `CREATE` requires unique constraints; `MERGE` does not  
**D)** They are functionally identical

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: A**

**Explanation:**

✅ **A is correct** — Key differences:

**CREATE:**
- Always creates a new node/relationship
- Can create duplicates
- Faster (no existence check)

```cypher
// Run twice → creates 2 Alice nodes
CREATE (p:Person {name: "Alice"});
CREATE (p:Person {name: "Alice"});
```

**MERGE:**
- **Upsert behavior**: Creates only if pattern doesn't exist
- Prevents duplicates
- Slower (checks existence first)

```cypher
// Run twice → creates only 1 Alice node
MERGE (p:Person {name: "Alice"});
MERGE (p:Person {name: "Alice"});
```

**MERGE with ON CREATE / ON MATCH:**

```cypher
MERGE (p:Person {email: "alice@example.com"})
ON CREATE SET p.name = "Alice", p.created = timestamp()
ON MATCH SET p.lastSeen = timestamp()
RETURN p;
```

❌ **B is incorrect** — Both can use indexes. Speed difference is due to existence checks, not indexing.

❌ **C is incorrect** — Neither requires unique constraints, but MERGE benefits from them for performance.

❌ **D is incorrect** — They have different semantics (create vs upsert).

**Best Practice:** Use `MERGE` when loading data to avoid duplicates. Use `CREATE` for bulk inserts when you know data is unique.

</details>

---

### Question 6: Shortest Path Query (8 points)

Given the following Cypher query:

```cypher
MATCH path = shortestPath(
  (start:Person {name: "Alice"})-[:FRIENDS_WITH*]-(end:Person {name: "David"})
)
RETURN length(path);
```

If Alice and David are connected through this path:

```
(Alice)-[:FRIENDS_WITH]->(Bob)-[:FRIENDS_WITH]->(Charlie)-[:FRIENDS_WITH]->(David)
```

What does `length(path)` return?

**A)** 4 (number of nodes)  
**B)** 3 (number of relationships)  
**C)** 2 (number of intermediate nodes)  
**D)** 1 (it's a single path)

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — `length(path)` returns the **number of relationships** (edges) in the path:

```
(Alice)-[:FRIENDS_WITH]->(Bob)-[:FRIENDS_WITH]->(Charlie)-[:FRIENDS_WITH]->(David)
        └─────── 1 ───────┘    └─────── 2 ───────┘    └─────── 3 ───────┘
```

- 3 relationships
- `length(path) = 3`

❌ **A is incorrect** — Number of **nodes** would be `length(nodes(path))` = 4 (Alice, Bob, Charlie, David).

❌ **C is incorrect** — Number of intermediate nodes (excluding start and end) = 2 (Bob, Charlie).

❌ **D is incorrect** — `1` would mean only one relationship (direct connection).

**Path Functions:**

```cypher
MATCH path = (a)-[:FRIENDS_WITH*]-(b)
RETURN
  length(path),           // Number of relationships
  nodes(path),            // Array of nodes
  relationships(path),    // Array of relationships
  length(nodes(path)) - 1;  // Same as length(path)
```

**Use Case:** Calculating "degrees of separation" in social networks (6 degrees of Kevin Bacon).

</details>

---

### Question 7: Aggregation with COLLECT (8 points)

What does the following query return?

```cypher
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN p.name, collect(friend.name) AS friends;
```

**A)** One row per friend with Alice's name repeated  
**B)** One row with Alice's name and an array of all friend names  
**C)** The total count of Alice's friends  
**D)** An error because COLLECT requires GROUP BY

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — `collect()` aggregates values into an **array**:

**Result:**

| p.name | friends |
|--------|---------|
| "Alice" | ["Bob", "Charlie", "David"] |

One row, with all friend names in an array.

❌ **A is incorrect** — That would happen without aggregation:

```cypher
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN p.name, friend.name;  // Multiple rows
```

| p.name | friend.name |
|--------|-------------|
| "Alice" | "Bob" |
| "Alice" | "Charlie" |
| "Alice" | "David" |

❌ **C is incorrect** — To get the count, use `count()`:

```cypher
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN p.name, count(friend) AS friendCount;
```

❌ **D is incorrect** — Cypher uses **implicit grouping**. No explicit `GROUP BY` needed. Non-aggregated columns (like `p.name`) become grouping keys automatically.

**Other Aggregation Functions:**

```cypher
count(friend)           // Count
collect(friend.name)    // Array
avg(friend.age)         // Average
sum(friend.score)       // Sum
min(friend.age)         // Minimum
max(friend.age)         // Maximum
```

</details>

---

### Question 8: Index Performance (8 points)

You have a query that's running slowly:

```cypher
MATCH (p:Person)
WHERE p.email = "alice@example.com"
RETURN p;
```

Which solution will **most improve** performance?

**A)** Add `LIMIT 1` to the query  
**B)** Create an index: `CREATE INDEX FOR (p:Person) ON (p.email);`  
**C)** Use `MERGE` instead of `MATCH`  
**D)** Remove the `WHERE` clause and filter in application code

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — Creating an **index on `email`** allows Neo4j to:
- Skip scanning all `:Person` nodes
- Directly lookup nodes by email (O(log N) instead of O(N))
- Dramatically improve query performance

```cypher
CREATE INDEX person_email_index FOR (p:Person) ON (p.email);
```

After creating the index:

```cypher
MATCH (p:Person)
WHERE p.email = "alice@example.com"  // Uses index
RETURN p;
```

❌ **A is incorrect** — `LIMIT 1` only reduces the number of **returned results**, not the number of nodes scanned. Neo4j still scans all `:Person` nodes to find matches.

❌ **C is incorrect** — `MERGE` is for **creating** nodes if they don't exist (upsert), not for querying. It would be slower.

❌ **D is incorrect** — Filtering in application code is **much slower** because:
- All nodes must be transferred over the network
- Filtering happens in application memory instead of database
- Wastes bandwidth and processing time

**Best Practice:**
- Create indexes on properties used in `WHERE` clauses
- Use unique constraints for properties that should be unique (also creates index)

```cypher
// Unique constraint (creates index + enforces uniqueness)
CREATE CONSTRAINT person_email_unique FOR (p:Person) REQUIRE p.email IS UNIQUE;
```

</details>

---

### Question 9: OPTIONAL MATCH (8 points)

What does `OPTIONAL MATCH` do in Neo4j?

**A)** Makes the entire query optional (may return no results)  
**B)** Acts like a LEFT JOIN in SQL, returning `null` for missing patterns  
**C)** Improves performance by skipping some nodes  
**D)** Requires the pattern to exist or the query fails

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — `OPTIONAL MATCH` is equivalent to **SQL's LEFT JOIN**:

**Example:**

```cypher
MATCH (p:Person)
OPTIONAL MATCH (p)-[:FRIENDS_WITH]->(friend)
RETURN p.name, friend.name;
```

**Result:**

| p.name | friend.name |
|--------|-------------|
| "Alice" | "Bob" |
| "Alice" | "Charlie" |
| "David" | null |  ← David has no friends, but still appears

Without `OPTIONAL MATCH`:

```cypher
MATCH (p:Person)
MATCH (p)-[:FRIENDS_WITH]->(friend)  // Regular MATCH
RETURN p.name, friend.name;
```

David would **not appear** at all (only people with friends are returned).

❌ **A is incorrect** — The query always runs. Only the optional pattern can be null.

❌ **C is incorrect** — `OPTIONAL MATCH` doesn't improve performance; it changes semantics (include nulls).

❌ **D is incorrect** — The query doesn't fail if the optional pattern is missing; it returns `null`.

**Use Cases:**

```cypher
// Find all users and their optional profile pictures
MATCH (u:User)
OPTIONAL MATCH (u)-[:HAS_PROFILE_PIC]->(pic:Image)
RETURN u.name, pic.url;

// Find all products and their optional reviews
MATCH (p:Product)
OPTIONAL MATCH (p)<-[:REVIEWED]-(review:Review)
RETURN p.name, collect(review.rating) AS ratings;
```

</details>

---

### Question 10: Relationship Properties (8 points)

Given this data model:

```cypher
CREATE
  (alice:Person {name: "Alice"}),
  (bob:Person {name: "Bob"}),
  (alice)-[:FRIENDS_WITH {since: 2020, strength: 0.8}]->(bob);
```

How do you query the `strength` property of the relationship?

**A)** `MATCH (a:Person)-[:FRIENDS_WITH]->(b) RETURN FRIENDS_WITH.strength;`  
**B)** `MATCH (a:Person)-[r:FRIENDS_WITH]->(b) RETURN r.strength;`  
**C)** `MATCH (a:Person)-[:FRIENDS_WITH {strength}]->(b) RETURN strength;`  
**D)** `MATCH (a:Person)-[:FRIENDS_WITH]->(b) RETURN b.strength;`

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — To access relationship properties:

1. **Bind the relationship to a variable**: `[r:FRIENDS_WITH]`
2. **Access properties with dot notation**: `r.strength`

```cypher
MATCH (a:Person {name: "Alice"})-[r:FRIENDS_WITH]->(b:Person)
RETURN a.name, b.name, r.since, r.strength;
```

**Result:**

| a.name | b.name | r.since | r.strength |
|--------|--------|---------|------------|
| "Alice" | "Bob" | 2020 | 0.8 |

❌ **A is incorrect** — Syntax error. `FRIENDS_WITH` is not a variable; it's a relationship type. Must bind to variable: `[r:FRIENDS_WITH]`.

❌ **C is incorrect** — `{strength}` in pattern matches relationships **with** a `strength` property but doesn't return the value. Incorrect syntax for returning the property.

❌ **D is incorrect** — `b.strength` accesses the **node's** property, not the relationship's property.

**Comparison:**

```cypher
// Node property
MATCH (p:Person {name: "Alice"})
RETURN p.age;  // Property of the node

// Relationship property
MATCH (a)-[r:FRIENDS_WITH]->(b)
RETURN r.since;  // Property of the relationship
```

**Use Case:** Weighted graphs (e.g., social network strength, road distances, trust scores).

</details>

---

### Question 11: Detaching DELETE (8 points)

What happens when you run the following query on a node with relationships?

```cypher
MATCH (p:Person {name: "Alice"})
DELETE p;
```

**A)** Alice and all her relationships are deleted  
**B)** An error occurs because Alice has relationships  
**C)** Only outgoing relationships are deleted  
**D)** Alice's properties are cleared, but the node remains

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: B**

**Explanation:**

✅ **B is correct** — Neo4j **prevents** deleting nodes with relationships to maintain graph integrity:

**Error Message:**

```
Cannot delete node<42>, because it still has relationships.
To delete this node, you must first delete its relationships.
```

**Solution:** Use `DETACH DELETE` to delete the node **and** all its relationships:

```cypher
MATCH (p:Person {name: "Alice"})
DETACH DELETE p;  // Deletes Alice AND all her relationships
```

❌ **A is incorrect** — That's what `DETACH DELETE` does, not `DELETE`.

❌ **C is incorrect** — No partial deletion occurs. The query fails entirely.

❌ **D is incorrect** — The node is not modified; the query fails.

**Comparison:**

```cypher
// ❌ Fails if node has relationships
DELETE p;

// ✅ Deletes node and all its relationships
DETACH DELETE p;

// ✅ Delete relationships first, then node
MATCH (p:Person {name: "Alice"})-[r]-()
DELETE r;
MATCH (p:Person {name: "Alice"})
DELETE p;
```

**Safety Consideration:** `DETACH DELETE` is powerful. Always double-check your `MATCH` clause to avoid deleting unintended nodes.

</details>

---

### Question 12: Use Case Analysis (12 points)

A company wants to build a **recommendation engine** for an e-commerce platform. Users can:
- Purchase products
- Rate products (1-5 stars)
- Follow other users

The recommendation algorithm should find products that:
1. Similar users have purchased
2. Have high ratings
3. The current user hasn't purchased yet

Which database paradigm is **most appropriate**?

**A)** Relational database (PostgreSQL) with complex joins  
**B)** Document database (MongoDB) with embedded recommendations  
**C)** Graph database (Neo4j) with collaborative filtering queries  
**D)** Key-value store (Redis) with cached product lists

<details>
<summary>Answer & Explanation</summary>

**Correct Answer: C**

**Explanation:**

✅ **C is correct** — Graph databases excel at **collaborative filtering** recommendations:

**Why Neo4j?**

1. **Natural data model:**

```cypher
(User)-[:PURCHASED]->(Product)
(User)-[:RATED {stars: 5}]->(Product)
(User)-[:FOLLOWS]->(User)
```

2. **Efficient traversal for "users like you":**

```cypher
// Find similar users based on shared purchases
MATCH (me:User {id: $userId})-[:PURCHASED]->(p:Product)
MATCH (similar:User)-[:PURCHASED]->(p)
WHERE me <> similar
WITH similar, count(p) AS commonProducts
ORDER BY commonProducts DESC
LIMIT 10
RETURN similar;
```

3. **Collaborative filtering query:**

```cypher
// Find products similar users purchased that I haven't
MATCH (me:User {id: $userId})-[:PURCHASED]->(myProduct:Product)
MATCH (similar:User)-[:PURCHASED]->(myProduct)
MATCH (similar)-[:PURCHASED]->(rec:Product)
MATCH (rec)<-[rating:RATED]-(similar)
WHERE NOT (me)-[:PURCHASED]->(rec)
RETURN rec.name, avg(rating.stars) AS avgRating, count(*) AS purchaseCount
ORDER BY avgRating DESC, purchaseCount DESC
LIMIT 20;
```

4. **Performance:** Traversing user→product→user→product is **fast** in Neo4j (index-free adjacency) but **slow** in SQL (multiple self-joins).

❌ **A is incorrect** — Relational databases **can** do this, but performance degrades with complex joins:

```sql
-- Slow for large datasets
SELECT p2.name
FROM purchases p1
JOIN purchases p2 ON p1.user_id = p2.user_id
LEFT JOIN purchases p3 ON p3.product_id = p2.product_id AND p3.user_id = ?
WHERE p1.user_id = ?
  AND p3.product_id IS NULL  -- Not purchased by current user
GROUP BY p2.product_id;
```

This requires 3+ joins and becomes prohibitively slow with millions of users/products.

❌ **B is incorrect** — MongoDB can store recommendations, but **computing** them requires application logic or aggregation pipelines that are less efficient than graph traversals.

❌ **D is incorrect** — Redis can **cache** pre-computed recommendations, but doesn't help with **generating** recommendations. You'd still need another database to compute them.

**Hybrid Approach:**

- **Neo4j:** Compute recommendations (graph traversals)
- **PostgreSQL:** Store transactional data (orders, payments)
- **Redis:** Cache top-N recommendations per user

**Real-World Example:** eBay uses Neo4j for recommendations alongside MySQL for transactions.

</details>

---

## Answer Key

| Question | Answer | Topic |
|----------|--------|-------|
| 1 | A | Labeled property graph model |
| 2 | B | Graph vs relational use cases |
| 3 | C | Cypher pattern matching (undirected) |
| 4 | A | Variable-length paths |
| 5 | A | MERGE vs CREATE |
| 6 | B | Shortest path & length() |
| 7 | B | COLLECT aggregation |
| 8 | B | Index performance |
| 9 | B | OPTIONAL MATCH (LEFT JOIN) |
| 10 | B | Relationship properties |
| 11 | B | DETACH DELETE |
| 12 | C | Recommendation engine use case |

---

## Grading Scale

| Score | Grade |
|-------|-------|
| 90-100 | A |
| 80-89 | B |
| 70-79 | C |
| 60-69 | D |
| 0-59 | F |

**Passing Score:** 70 points (C)

---

## Study Resources

- [Neo4j Fundamentals](../neo4j-fundamentals.md)
- [Neo4j Official Documentation](https://neo4j.com/docs/)
- [Cypher Query Language Reference](https://neo4j.com/docs/cypher-manual/current/)
- [Graph Database Use Cases](https://neo4j.com/use-cases/)

---

**Good luck! 🚀**
