# Week 12 — Graph Databases: Neo4j

**Dates:** March 30 – April 3, 2026  
**Topics:** Graph Databases, Neo4j, Cypher Query Language  
**Assessment:** Quiz 10 — Graph Databases & Neo4j

---

## Overview

This week introduces **graph databases**, a paradigm designed for highly interconnected data. You'll learn how **Neo4j** models data as nodes and relationships, master the **Cypher** query language, and build applications that leverage graph traversals for recommendations, fraud detection, and social networks.

### Learning Objectives

By the end of this week, you will be able to:

1. **Explain** the graph database model (nodes, relationships, properties, labels)
2. **Compare** graph databases to relational databases and justify when to use each
3. **Write** Cypher queries (CREATE, MATCH, MERGE, DELETE, patterns)
4. **Traverse** variable-length paths and find shortest paths
5. **Build** recommendation engines using collaborative filtering
6. **Integrate** Neo4j with TypeScript using neo4j-driver
7. **Design** graph schemas for real-world use cases

---

## Weekly Schedule

### Monday, March 30 — Graph Database Fundamentals

**Topics:**
- What is a graph database?
  - Nodes (entities)
  - Relationships (connections)
  - Properties (key-value pairs)
  - Labels (node categories)
- **Graph vs Relational:**
  - When graphs beat relational (deep joins, recommendations)
  - When relational is better (transactions, aggregations)
  - Index-free adjacency (O(1) traversal)
- **Use Cases:**
  - Social networks (friends-of-friends)
  - Recommendation engines (collaborative filtering)
  - Fraud detection (shared identifiers)
  - Knowledge graphs (Wikipedia, research)
  - Routing/navigation (shortest path)

**Readings:**
- [Neo4j Fundamentals](./neo4j-fundamentals.md) (Sections 1-4)

**Activities:**
- Set up Neo4j Docker container
- Explore Neo4j Browser UI (http://localhost:7474)
- Create sample social network graph
- Practice basic Cypher queries

---

### Thursday, April 2 — Cypher Query Language

**Topics:**
- **CRUD Operations:**
  - CREATE (nodes, relationships)
  - MATCH (query patterns)
  - MERGE (upsert)
  - SET, REMOVE (update)
  - DELETE, DETACH DELETE (remove)
- **Advanced Patterns:**
  - Variable-length paths (`*1..3`)
  - Shortest path algorithms
  - OPTIONAL MATCH (LEFT JOIN)
  - Aggregations (count, collect, avg)
  - WITH clause (chaining queries)
- **Indexes & Constraints:**
  - CREATE INDEX (speed up lookups)
  - Unique constraints
  - Node key constraints
- **TypeScript Integration:**
  - neo4j-driver setup
  - Sessions and transactions
  - Handling Neo4j Integers
  - Connection pooling

**Readings:**
- [Neo4j Fundamentals](./neo4j-fundamentals.md) (Sections 5-10)
- [Readings & Resources](./readings-12.md)

**Activities:**
- Build friend recommendation system
- Implement product recommendation engine (collaborative filtering)
- Practice Cypher performance tuning (EXPLAIN, PROFILE)
- Integrate Neo4j with TypeScript

---

## Assessment

### Quiz 10 — Graph Databases & Neo4j (Due: Thursday, April 2)

**Topics Covered:**
- Graph model fundamentals (nodes, relationships, properties, labels)
- When to use graph databases vs relational databases
- Cypher pattern matching (directed, undirected, variable-length)
- CREATE, MATCH, MERGE, DELETE operations
- Shortest path queries
- OPTIONAL MATCH (LEFT JOIN equivalent)
- Relationship properties
- Indexes and constraints
- Use case analysis (recommendations, fraud detection)

**Format:**
- 12 multiple-choice questions
- Requirement → Cypher code
- Code → Description
- Use case selection

**Preparation:**
- Complete all readings
- Practice Cypher in Neo4j Browser
- Build recommendation engine
- Review quiz materials

[Take Quiz 10](./quiz/graph-neo4j-quiz.md)

---

## Final Project Milestone

**Checkpoint #1 (Due: Sunday, April 5)**

Submit your project's **first checkpoint** including:

1. **Database Implementation Progress:**
   - PostgreSQL schema created and populated
   - MongoDB collections designed
   - Redis/Neo4j (if applicable) initial setup

2. **Code Progress:**
   - Repository structure established
   - Database connection modules implemented
   - Basic CRUD operations functional
   - Tests passing for core functionality

3. **Documentation:**
   - Updated architecture diagram
   - API endpoint definitions
   - Data flow diagrams
   - Setup instructions (README.md)

4. **Checkpoint Demo:**
   - 5-minute video demonstrating working features
   - Show database queries in action
   - Explain design decisions

**Example Deliverables:**

**Social Media Platform:**
- ✅ PostgreSQL: Users table, authentication working
- ✅ MongoDB: Posts collection, CRUD operations
- ✅ Redis: Session storage implemented
- ✅ Neo4j: Friend relationships, basic recommendations
- 🎥 Demo: User signup → create post → friend recommendations

**E-commerce Site:**
- ✅ PostgreSQL: Products, orders, inventory tables
- ✅ MongoDB: Product reviews schema
- ✅ Redis: Shopping cart cache
- ✅ Tests: 80% coverage on core endpoints
- 🎥 Demo: Add to cart → checkout → order confirmation

---

## Weekly Content

### Required Readings

1. **[Neo4j Fundamentals](./neo4j-fundamentals.md)** (120 min)
   - Graph model, Cypher CRUD, patterns, neo4j-driver, use cases

### Supplemental Resources

2. **[Readings & Resources](./readings-12.md)** (30 min)
   - Official documentation, tutorials, videos, books, cheat sheets

---

## Key Concepts

### Graph Model

| Component | Description | Example |
|-----------|-------------|---------|
| **Node** | Entity (vertex) | `(alice:Person {name: "Alice", age: 30})` |
| **Relationship** | Connection (edge) | `-[:FRIENDS_WITH {since: 2020}]->` |
| **Property** | Key-value pair | `{name: "Alice", age: 30}` |
| **Label** | Node category | `:Person`, `:Product`, `:City` |
| **Direction** | Arrow indicates flow | `->` (outgoing), `<-` (incoming), `-` (any) |

---

### Cypher Basics

#### CREATE (Insert)

```cypher
// Create node
CREATE (alice:Person {name: "Alice", age: 30})
RETURN alice;

// Create relationship
MATCH (a:Person {name: "Alice"}), (b:Person {name: "Bob"})
CREATE (a)-[r:FRIENDS_WITH {since: 2020}]->(b)
RETURN r;
```

#### MATCH (Query)

```cypher
// Find all persons
MATCH (p:Person)
RETURN p;

// Find Alice's friends
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN friend.name;

// Friends-of-friends
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*2]->(fof)
RETURN DISTINCT fof.name;
```

#### MERGE (Upsert)

```cypher
// Create if not exists
MERGE (p:Person {email: "alice@example.com"})
ON CREATE SET p.name = "Alice", p.created = timestamp()
ON MATCH SET p.lastSeen = timestamp()
RETURN p;
```

#### DELETE (Remove)

```cypher
// Delete node and all its relationships
MATCH (p:Person {name: "Alice"})
DETACH DELETE p;
```

---

### Variable-Length Paths

```cypher
// Friends-of-friends (exactly 2 hops)
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*2]->(fof)
RETURN DISTINCT fof.name;

// Up to 3 hops
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*1..3]->(connected)
RETURN DISTINCT connected.name;

// Shortest path
MATCH path = shortestPath(
  (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]-(david:Person {name: "David"})
)
RETURN path, length(path);
```

---

### Indexes and Constraints

```cypher
// Create index (speed up lookups)
CREATE INDEX person_name_index FOR (p:Person) ON (p.name);

// Unique constraint (prevents duplicates + creates index)
CREATE CONSTRAINT person_email_unique FOR (p:Person) REQUIRE p.email IS UNIQUE;

// List indexes
SHOW INDEXES;
```

---

## Practical Exercises

### Exercise 1: Social Network Model

Create a social network with users and friendships:

```cypher
// Create users
CREATE
  (alice:Person {name: "Alice", age: 30}),
  (bob:Person {name: "Bob", age: 28}),
  (charlie:Person {name: "Charlie", age: 35}),
  (david:Person {name: "David", age: 25}),
  
  (alice)-[:FRIENDS_WITH {since: 2020}]->(bob),
  (bob)-[:FRIENDS_WITH {since: 2021}]->(charlie),
  (charlie)-[:FRIENDS_WITH {since: 2019}]->(david),
  (alice)-[:FRIENDS_WITH {since: 2022}]->(charlie);
```

**Tasks:**
1. Find all of Alice's friends
2. Find friends-of-friends for Alice (2 hops)
3. Find the shortest path between Alice and David
4. Recommend new friends (friends-of-friends not already friends)

---

### Exercise 2: Product Recommendations

Build a collaborative filtering recommendation engine:

```cypher
// Create users and products
CREATE
  (alice:User {name: "Alice"}),
  (bob:User {name: "Bob"}),
  (charlie:User {name: "Charlie"}),
  
  (laptop:Product {name: "Laptop", price: 1200}),
  (mouse:Product {name: "Mouse", price: 25}),
  (keyboard:Product {name: "Keyboard", price: 80}),
  (monitor:Product {name: "Monitor", price: 300}),
  
  (alice)-[:PURCHASED]->(laptop),
  (alice)-[:PURCHASED]->(mouse),
  (bob)-[:PURCHASED]->(laptop),
  (bob)-[:PURCHASED]->(keyboard),
  (charlie)-[:PURCHASED]->(mouse),
  (charlie)-[:PURCHASED]->(monitor);
```

**Query: "Users who bought X also bought Y"**

```cypher
// Recommend products for Alice
MATCH (alice:User {name: "Alice"})-[:PURCHASED]->(product:Product)
MATCH (otherUser:User)-[:PURCHASED]->(product)
MATCH (otherUser)-[:PURCHASED]->(recommendation:Product)
WHERE NOT (alice)-[:PURCHASED]->(recommendation)
  AND alice <> otherUser
RETURN recommendation.name AS product, count(*) AS score
ORDER BY score DESC;
```

---

### Exercise 3: TypeScript Integration

```typescript
import neo4j from 'neo4j-driver';

const driver = neo4j.driver(
  'bolt://localhost:7687',
  neo4j.auth.basic('neo4j', 'password123')
);

async function getFriendRecommendations(userName: string) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      MATCH (user:Person {name: $userName})-[:FRIENDS_WITH]->(friend)-[:FRIENDS_WITH]->(fof)
      WHERE NOT (user)-[:FRIENDS_WITH]->(fof)
        AND user <> fof
      RETURN fof.name AS recommendation, count(*) AS mutualFriends
      ORDER BY mutualFriends DESC
      LIMIT 5
      `,
      { userName }
    );
    
    return result.records.map(record => ({
      name: record.get('recommendation'),
      mutualFriends: record.get('mutualFriends').toNumber(),
    }));
  } finally {
    await session.close();
  }
}

// Usage
const recommendations = await getFriendRecommendations('Alice');
console.log(recommendations);
// [{ name: 'David', mutualFriends: 2 }, ...]

await driver.close();
```

---

## Common Pitfalls

### ❌ Forgetting DETACH DELETE

```cypher
// ❌ Error if node has relationships
MATCH (p:Person {name: "Alice"})
DELETE p;

// ✅ Deletes node AND relationships
MATCH (p:Person {name: "Alice"})
DETACH DELETE p;
```

### ❌ Creating Duplicates with CREATE

```cypher
// ❌ Creates duplicate nodes on each run
CREATE (p:Person {name: "Alice"});

// ✅ Use MERGE to prevent duplicates
MERGE (p:Person {name: "Alice"});
```

### ❌ Not Handling Neo4j Integers

```typescript
// ❌ Wrong: age is a Neo4j Integer object
const age = result.records[0].get('age');
console.log(age + 1);  // Incorrect

// ✅ Convert to JavaScript number
const age = result.records[0].get('age').toNumber();
console.log(age + 1);  // Correct
```

### ❌ Unbounded Variable-Length Paths

```cypher
// ❌ Could traverse entire graph (slow!)
MATCH (a:Person)-[:FRIENDS_WITH*]->(b)
RETURN b;

// ✅ Set maximum depth
MATCH (a:Person)-[:FRIENDS_WITH*1..5]->(b)
RETURN DISTINCT b
LIMIT 100;
```

### ❌ Not Using Labels in MATCH

```cypher
// ❌ Slow (scans all nodes)
MATCH (n {name: "Alice"})
RETURN n;

// ✅ Fast (uses label index)
MATCH (p:Person {name: "Alice"})
RETURN p;
```

---

## Tools & Setup

### Neo4j Docker

```yaml
# docker-compose.yml
version: '3.8'

services:
  neo4j:
    image: neo4j:5-community
    ports:
      - "7474:7474"  # Browser UI
      - "7687:7687"  # Bolt protocol
    environment:
      - NEO4J_AUTH=neo4j/password123
      - NEO4J_PLUGINS=["apoc", "graph-data-science"]
    volumes:
      - neo4j_data:/data
      - neo4j_logs:/logs

volumes:
  neo4j_data:
  neo4j_logs:
```

```bash
# Start Neo4j
docker-compose up -d

# Wait 10-15 seconds for startup

# Access Browser UI
open http://localhost:7474

# Login: neo4j / password123
```

---

### Cypher Shell

```bash
# Connect to Cypher shell
docker exec -it neo4j cypher-shell -u neo4j -p password123

# Execute Cypher
neo4j@neo4j> MATCH (n) RETURN count(n);

# Exit
neo4j@neo4j> :exit
```

---

## Decision Matrix: Graph vs Relational

| Requirement | Graph (Neo4j) | Relational (PostgreSQL) |
|------------|---------------|-------------------------|
| **Relationship-Heavy Queries** | ✅ Excellent (O(1) traversal) | ❌ Slow (multiple joins) |
| **Deep Traversals (4+ levels)** | ✅ Fast | ❌ Prohibitively slow |
| **Recommendations** | ✅ Native graph algorithms | ❌ Complex SQL |
| **Pattern Matching** | ✅ Cypher excels | ❌ Requires procedural code |
| **Transactions (ACID)** | ✅ Supported | ✅ Excellent |
| **Aggregations (sum, count)** | ⚠️ Possible but slower | ✅ Optimized |
| **Schema Flexibility** | ✅ Dynamic | ⚠️ Requires migrations |
| **Write Performance (bulk)** | ⚠️ Slower | ✅ Fast |

---

## When to Use Neo4j

### ✅ Use Neo4j When:

- **Deep relationship queries**: Friends-of-friends-of-friends (3+ hops)
- **Recommendations**: Collaborative filtering, product suggestions
- **Fraud detection**: Pattern matching across networks
- **Social networks**: Connection traversal, influence analysis
- **Knowledge graphs**: Wikipedia, research databases
- **Routing**: Shortest path, navigation

### ❌ Avoid Neo4j When:

- **Simple CRUD**: Basic create/read/update/delete (use PostgreSQL)
- **Large aggregations**: Sum/average over millions of rows
- **Time-series data**: Metrics, logs (use TimescaleDB)
- **Unconnected data**: No relationships between entities
- **Document-oriented**: Flexible schemas without relationships (use MongoDB)

---

## Next Steps

After completing this week's content:

1. ✅ **Complete Quiz 10** on graph databases and Cypher
2. ✅ **Submit Final Project Checkpoint #1** (database implementation progress)
3. ✅ **Experiment** with Neo4j in your project (if applicable)
4. ✅ **Explore** graph algorithms (PageRank, community detection) with GDS library
5. 📚 **Preview Week 13** (Time Series & Search Engines)

---

**Questions or feedback?** Post in the course discussion forum or office hours.

**Happy graphing! 🚀**
