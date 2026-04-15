# Quiz 10: Graph Databases & Neo4j

## Instructions

This quiz tests your understanding of Neo4j (graph database), including the labeled property graph model, Cypher query language, traversals, and use cases.

---

!!! quiz
{
"title": "Graph Model Fundamentals",
"question": "Which of the following best describes the labeled property graph model used by Neo4j?",
"options": ["Nodes can have labels, properties, and relationships; relationships can also have properties", "Nodes contain only labels and relationships contain only properties", "Nodes and relationships share the same property namespace", "Relationships cannot have properties, only directionality"],
"answers": ["Nodes can have labels, properties, and relationships; relationships can also have properties"]
}
!!!

---

!!! quiz
{
"title": "When to Use Graph Databases",
"question": "For which of the following use cases would Neo4j be the MOST appropriate choice compared to a relational database?",
"options": ["Storing transactional e-commerce order data with strong ACID guarantees", "Finding friends-of-friends-of-friends in a social network", "Aggregating sales data across millions of rows for monthly reports", "Managing user authentication and password hashing"],
"answers": ["Finding friends-of-friends-of-friends in a social network"]
}
!!!

---

**What does the following Cypher query return?**

```cypher
MATCH (a:Person)-[:FRIENDS_WITH]-(b:Person)
WHERE a.name = "Alice"
RETURN b.name;
```

!!! quiz
{
"title": "Cypher Pattern Matching",
"question": "Given the Cypher query above, what does it return?",
"options": ["All people who Alice sent friend requests to (directed outgoing)", "All people who sent friend requests to Alice (directed incoming)", "All people who are friends with Alice (bidirectional or either direction)", "An error because the relationship direction is not specified"],
"answers": ["All people who are friends with Alice (bidirectional or either direction)"]
}
!!!

---

**Which Cypher query finds all people connected to Alice within exactly 3 hops?**

Option A:

```cypher
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*3]->(p) RETURN DISTINCT p;
```

Option B:

```cypher
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*1..3]->(p) RETURN DISTINCT p;
```

Option C:

```cypher
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH]->(p) RETURN DISTINCT p LIMIT 3;
```

Option D:

```cypher
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]->(p) WHERE length(path) = 3 RETURN DISTINCT p;
```

!!! quiz
{
"title": "Variable-Length Paths",
"question": "Which Cypher query finds all people connected to Alice within exactly 3 hops?",
"options": ["A", "B", "C", "D"],
"answers": ["A"]
}
!!!

---

!!! quiz
{
"title": "MERGE vs CREATE",
"question": "What is the difference between CREATE and MERGE in Neo4j?",
"options": ["CREATE always creates a new node; MERGE creates only if the pattern doesn't exist (upsert)", "MERGE is faster because it uses indexes; CREATE does not", "CREATE requires unique constraints; MERGE does not", "They are functionally identical"],
"answers": ["CREATE always creates a new node; MERGE creates only if the pattern doesn't exist (upsert)"]
}
!!!

---

**Given the following Cypher query and graph:**

```cypher
MATCH path = shortestPath(
  (start:Person {name: "Alice"})-[:FRIENDS_WITH*]-(end:Person {name: "David"})
)
RETURN length(path);
```

Alice and David are connected through this path:

```
(Alice)-[:FRIENDS_WITH]->(Bob)-[:FRIENDS_WITH]->(Charlie)-[:FRIENDS_WITH]->(David)
```

!!! quiz
{
"title": "Shortest Path Query",
"question": "What does length(path) return for the path above?",
"options": ["4 (number of nodes)", "3 (number of relationships)", "2 (number of intermediate nodes)", "1 (it's a single path)"],
"answers": ["3 (number of relationships)"]
}
!!!

---

**What does the following query return?**

```cypher
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN p.name, collect(friend.name) AS friends;
```

!!! quiz
{
"title": "Aggregation with COLLECT",
"question": "Given the Cypher query above, what does it return?",
"options": ["One row per friend with Alice's name repeated", "One row with Alice's name and an array of all friend names", "The total count of Alice's friends", "An error because COLLECT requires GROUP BY"],
"answers": ["One row with Alice's name and an array of all friend names"]
}
!!!

---

**You have a query that's running slowly:**

```cypher
MATCH (p:Person)
WHERE p.email = "alice@example.com"
RETURN p;
```

!!! quiz
{
"title": "Index Performance",
"question": "Which solution will MOST improve performance of the slow query above?",
"options": ["Add LIMIT 1 to the query", "Create an index: CREATE INDEX FOR (p:Person) ON (p.email);", "Use MERGE instead of MATCH", "Remove the WHERE clause and filter in application code"],
"answers": ["Create an index: CREATE INDEX FOR (p:Person) ON (p.email);"]
}
!!!

---

!!! quiz
{
"title": "OPTIONAL MATCH",
"question": "What does OPTIONAL MATCH do in Neo4j?",
"options": ["Makes the entire query optional (may return no results)", "Acts like a LEFT JOIN in SQL, returning null for missing patterns", "Improves performance by skipping some nodes", "Requires the pattern to exist or the query fails"],
"answers": ["Acts like a LEFT JOIN in SQL, returning null for missing patterns"]
}
!!!

---

**Given this data model:**

```cypher
CREATE
  (alice:Person {name: "Alice"}),
  (bob:Person {name: "Bob"}),
  (alice)-[:FRIENDS_WITH {since: 2020, strength: 0.8}]->(bob);
```

**How do you query the `strength` property of the relationship?**

Option A:

```cypher
MATCH (a:Person)-[:FRIENDS_WITH]->(b) RETURN FRIENDS_WITH.strength;
```

Option B:

```cypher
MATCH (a:Person)-[r:FRIENDS_WITH]->(b) RETURN r.strength;
```

Option C:

```cypher
MATCH (a:Person)-[:FRIENDS_WITH {strength}]->(b) RETURN strength;
```

Option D:

```cypher
MATCH (a:Person)-[:FRIENDS_WITH]->(b) RETURN b.strength;
```

!!! quiz
{
"title": "Relationship Properties",
"question": "Which query correctly returns the strength property of the FRIENDS_WITH relationship?",
"options": ["A", "B", "C", "D"],
"answers": ["B"]
}
!!!

---

**What happens when you run the following query on a node with relationships?**

```cypher
MATCH (p:Person {name: "Alice"})
DELETE p;
```

!!! quiz
{
"title": "Detaching DELETE",
"question": "What happens when you run DELETE on a node that has relationships?",
"options": ["Alice and all her relationships are deleted", "An error occurs because Alice has relationships", "Only outgoing relationships are deleted", "Alice's properties are cleared, but the node remains"],
"answers": ["An error occurs because Alice has relationships"]
}
!!!

---

**Scenario:** A company wants to build a **recommendation engine** for an e-commerce platform. Users can purchase products, rate products (1-5 stars), and follow other users. The recommendation algorithm should find products that similar users have purchased, have high ratings, and the current user hasn't purchased yet.

!!! quiz
{
"title": "Use Case Analysis",
"question": "Which database paradigm is MOST appropriate for the recommendation engine described above?",
"options": ["Relational database (PostgreSQL) with complex joins", "Document database (MongoDB) with embedded recommendations", "Graph database (Neo4j) with collaborative filtering queries", "Key-value store (Redis) with cached product lists"],
"answers": ["Graph database (Neo4j) with collaborative filtering queries"]
}
!!!

**Hybrid Approach:**

- **Neo4j:** Compute recommendations (graph traversals)
- **PostgreSQL:** Store transactional data (orders, payments)
- **Redis:** Cache top-N recommendations per user

**Real-World Example:** eBay uses Neo4j for recommendations alongside MySQL for transactions.
