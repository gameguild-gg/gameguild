# Neo4j Fundamentals — Graph Databases

## Introduction

**Graph databases** store data as **nodes** (entities) and **relationships** (connections), making them ideal for modeling interconnected data like social networks, recommendation engines, fraud detection systems, and knowledge graphs.

**Neo4j** is the most popular graph database, using the **Cypher** query language to traverse and query graph structures efficiently.

---

## What is a Graph Database?

### Graph Structure

A graph consists of:

1. **Nodes** (vertices): Entities like users, products, locations
2. **Relationships** (edges): Connections between nodes
3. **Properties**: Key-value pairs on nodes and relationships
4. **Labels**: Categorize nodes (e.g., `:Person`, `:Product`)

**Example Graph:**

```
(Alice:Person {name: "Alice", age: 30})
  -[:FRIENDS_WITH {since: 2020}]->
(Bob:Person {name: "Bob", age: 28})
  -[:LIKES]->
(Neo4j:Product {name: "Neo4j", category: "Database"})
```

### Graph vs Relational

| Aspect | Relational (SQL) | Graph (Neo4j) |
|--------|------------------|---------------|
| **Data Model** | Tables with rows | Nodes with relationships |
| **Relationships** | Foreign keys + JOINs | First-class citizens |
| **Query Performance** | Slow for deep joins (6+ levels) | Fast traversal (any depth) |
| **Schema** | Fixed schema required | Flexible schema (labeled property graph) |
| **Use Cases** | Transactions, structured data | Connected data, recommendations, fraud detection |

**When Graphs Beat Relational:**

- **Social networks**: "Find friends-of-friends-of-friends"
- **Recommendations**: "Users who liked X also liked Y"
- **Fraud detection**: Identify suspicious patterns across multiple hops
- **Knowledge graphs**: Complex interconnections (Wikipedia, research papers)
- **Routing/Navigation**: Shortest path algorithms

**When Relational is Better:**

- Transactional systems (banking, e-commerce orders)
- Aggregations over large datasets
- Simple one-to-many relationships
- Reporting and analytics (OLAP)

---

## Neo4j Architecture

### Components

1. **Graph Store**: Optimized for traversing relationships (index-free adjacency)
2. **Cypher Query Engine**: Declarative query language
3. **ACID Transactions**: Full transactional support
4. **Indexes**: Speed up node lookups by property
5. **Constraints**: Enforce uniqueness and existence

### Storage Model

Neo4j uses **index-free adjacency**:

- Each node stores direct pointers to its relationships
- No index lookups needed for traversals
- Constant-time relationship navigation (O(1))

**Comparison:**

```sql
-- Relational: JOIN requires index lookup (log N)
SELECT * FROM users u
JOIN friendships f ON u.id = f.user_id
WHERE f.friend_id = 123;
```

```cypher
// Neo4j: Direct pointer traversal (O(1))
MATCH (u:User)-[:FRIENDS_WITH]->(friend)
WHERE id(friend) = 123
RETURN u;
```

---

## Cypher Query Language

**Cypher** is a declarative, SQL-inspired language for graphs. It uses **ASCII art** to represent patterns.

### Patterns

```cypher
// Node pattern
(n)           // Any node
(p:Person)    // Node with label Person
(p:Person {name: "Alice"})  // Node with label and property

// Relationship pattern
-[:FRIENDS_WITH]->   // Directed relationship
-[:FRIENDS_WITH]-    // Undirected relationship
-[r:FRIENDS_WITH {since: 2020}]->  // Relationship with properties

// Path pattern
(a)-[:KNOWS]->(b)-[:KNOWS]->(c)  // Chain of relationships
```

---

## CRUD Operations

### CREATE — Insert Data

**Create Nodes:**

```cypher
// Create single node
CREATE (alice:Person {name: "Alice", age: 30, email: "alice@example.com"})
RETURN alice;

// Create multiple nodes
CREATE
  (bob:Person {name: "Bob", age: 28}),
  (charlie:Person {name: "Charlie", age: 35});
```

**Create Relationships:**

```cypher
// Create relationship between existing nodes
MATCH (a:Person {name: "Alice"}), (b:Person {name: "Bob"})
CREATE (a)-[r:FRIENDS_WITH {since: 2020}]->(b)
RETURN r;

// Create nodes and relationships in one statement
CREATE (alice:Person {name: "Alice"})-[:LIKES]->(neo4j:Product {name: "Neo4j"})
RETURN alice, neo4j;
```

**Create with Variable-Length Paths:**

```cypher
// Social network initialization
CREATE
  (alice:Person {name: "Alice"}),
  (bob:Person {name: "Bob"}),
  (charlie:Person {name: "Charlie"}),
  (david:Person {name: "David"}),
  
  (alice)-[:FRIENDS_WITH]->(bob),
  (bob)-[:FRIENDS_WITH]->(charlie),
  (charlie)-[:FRIENDS_WITH]->(david),
  (alice)-[:FRIENDS_WITH]->(charlie);
```

---

### MATCH — Query Data

**Basic MATCH:**

```cypher
// Find all Person nodes
MATCH (p:Person)
RETURN p;

// Find person by name
MATCH (p:Person {name: "Alice"})
RETURN p;

// Find with WHERE clause
MATCH (p:Person)
WHERE p.age > 25
RETURN p.name, p.age;
```

**MATCH with Relationships:**

```cypher
// Find Alice's friends
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN friend.name;

// Find bidirectional friendships
MATCH (a:Person)-[:FRIENDS_WITH]-(b:Person)
WHERE a.name = "Alice"
RETURN b.name;

// Find who likes Neo4j
MATCH (p:Person)-[:LIKES]->(product:Product {name: "Neo4j"})
RETURN p.name;
```

**Variable-Length Paths:**

```cypher
// Friends-of-friends (2 hops)
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*2]->(fof)
RETURN DISTINCT fof.name;

// Up to 3 hops
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*1..3]->(connected)
RETURN DISTINCT connected.name;

// Any number of hops (use with caution!)
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]->(connected)
RETURN DISTINCT connected.name;
```

**Shortest Path:**

```cypher
// Find shortest path between Alice and David
MATCH path = shortestPath(
  (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]-(david:Person {name: "David"})
)
RETURN path, length(path);

// All shortest paths
MATCH path = allShortestPaths(
  (alice:Person {name: "Alice"})-[:FRIENDS_WITH*]-(david:Person {name: "David"})
)
RETURN path;
```

---

### MERGE — Upsert (Create or Match)

**MERGE** ensures a pattern exists: creates it if missing, matches if exists.

```cypher
// Create person if not exists
MERGE (p:Person {name: "Alice"})
RETURN p;

// MERGE with ON CREATE / ON MATCH
MERGE (p:Person {email: "alice@example.com"})
ON CREATE SET p.name = "Alice", p.created = timestamp()
ON MATCH SET p.lastSeen = timestamp()
RETURN p;

// MERGE relationships (avoid duplicates)
MATCH (a:Person {name: "Alice"}), (b:Person {name: "Bob"})
MERGE (a)-[r:FRIENDS_WITH]->(b)
ON CREATE SET r.since = 2020
RETURN r;
```

**MERGE vs CREATE:**

```cypher
// ❌ CREATE always creates (duplicates possible)
CREATE (p:Person {name: "Alice"})

// ✅ MERGE creates only if not exists
MERGE (p:Person {name: "Alice"})
```

---

### UPDATE — Modify Data

**SET:**

```cypher
// Update single property
MATCH (p:Person {name: "Alice"})
SET p.age = 31
RETURN p;

// Update multiple properties
MATCH (p:Person {name: "Alice"})
SET p.age = 31, p.city = "New York"
RETURN p;

// Add label
MATCH (p:Person {name: "Alice"})
SET p:Admin
RETURN p;

// Replace all properties (use with caution!)
MATCH (p:Person {name: "Alice"})
SET p = {name: "Alice", age: 31, email: "newemail@example.com"}
RETURN p;
```

**REMOVE:**

```cypher
// Remove property
MATCH (p:Person {name: "Alice"})
REMOVE p.email
RETURN p;

// Remove label
MATCH (p:Person {name: "Alice"})
REMOVE p:Admin
RETURN p;
```

---

### DELETE — Remove Data

```cypher
// Delete node (must have no relationships)
MATCH (p:Person {name: "Alice"})
DELETE p;

// Delete node and all its relationships
MATCH (p:Person {name: "Alice"})
DETACH DELETE p;

// Delete relationship only
MATCH (a:Person {name: "Alice"})-[r:FRIENDS_WITH]->(b:Person {name: "Bob"})
DELETE r;

// Delete all nodes and relationships (DANGEROUS!)
MATCH (n)
DETACH DELETE n;
```

---

## Advanced Cypher Patterns

### Aggregations

```cypher
// Count friends
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN count(friend) AS friendCount;

// Average age of friends
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN avg(friend.age) AS avgAge;

// Group by and count
MATCH (p:Person)-[:LIKES]->(product:Product)
RETURN product.name, count(p) AS likeCount
ORDER BY likeCount DESC;
```

### WITH Clause (Chaining Queries)

```cypher
// Find people with > 5 friends
MATCH (p:Person)-[:FRIENDS_WITH]->(friend)
WITH p, count(friend) AS friendCount
WHERE friendCount > 5
RETURN p.name, friendCount;

// Paginate results
MATCH (p:Person)
WITH p
ORDER BY p.name
SKIP 10
LIMIT 10
RETURN p;
```

### OPTIONAL MATCH (LEFT JOIN equivalent)

```cypher
// Find all people and their friends (include people with no friends)
MATCH (p:Person)
OPTIONAL MATCH (p)-[:FRIENDS_WITH]->(friend)
RETURN p.name, collect(friend.name) AS friends;
```

### COLLECT and UNWIND

```cypher
// Collect friends into array
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)
RETURN p.name, collect(friend.name) AS friends;

// Unwind array into rows
UNWIND ["Alice", "Bob", "Charlie"] AS name
CREATE (p:Person {name: name});
```

### CASE Expressions

```cypher
// Categorize users by age
MATCH (p:Person)
RETURN p.name,
  CASE
    WHEN p.age < 18 THEN "Minor"
    WHEN p.age < 65 THEN "Adult"
    ELSE "Senior"
  END AS category;
```

---

## Indexes and Constraints

### Indexes

```cypher
// Create index on Person.name
CREATE INDEX person_name_index FOR (p:Person) ON (p.name);

// Composite index
CREATE INDEX person_name_age_index FOR (p:Person) ON (p.name, p.age);

// Full-text index (for text search)
CREATE FULLTEXT INDEX person_fulltext FOR (p:Person) ON EACH [p.name, p.bio];

// List indexes
SHOW INDEXES;

// Drop index
DROP INDEX person_name_index;
```

### Constraints

```cypher
// Unique constraint (also creates index)
CREATE CONSTRAINT person_email_unique FOR (p:Person) REQUIRE p.email IS UNIQUE;

// Existence constraint (property must exist)
CREATE CONSTRAINT person_name_exists FOR (p:Person) REQUIRE p.name IS NOT NULL;

// Node key (multiple properties must be unique together)
CREATE CONSTRAINT person_key FOR (p:Person) REQUIRE (p.name, p.email) IS NODE KEY;

// List constraints
SHOW CONSTRAINTS;

// Drop constraint
DROP CONSTRAINT person_email_unique;
```

---

## Docker Setup

### Docker Compose

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
      - NEO4J_AUTH=neo4j/password123  # Default credentials
      - NEO4J_PLUGINS=["apoc"]  # APOC procedures
    volumes:
      - neo4j_data:/data
      - neo4j_logs:/logs

volumes:
  neo4j_data:
  neo4j_logs:
```

### Start Neo4j

```bash
# Start container
docker-compose up -d

# Wait for startup (10-15 seconds)
docker logs neo4j -f

# Access browser UI
open http://localhost:7474

# Login with neo4j / password123
```

### Cypher Shell

```bash
# Run Cypher commands directly
docker exec -it neo4j cypher-shell -u neo4j -p password123

# Execute Cypher file
docker exec -i neo4j cypher-shell -u neo4j -p password123 < seed.cypher
```

---

## TypeScript Integration with neo4j-driver

### Installation

```bash
npm install neo4j-driver
npm install -D @types/node
```

### Basic Connection

```typescript
import neo4j, { Driver, Session } from 'neo4j-driver';

// Create driver (singleton)
const driver: Driver = neo4j.driver(
  'bolt://localhost:7687',
  neo4j.auth.basic('neo4j', 'password123')
);

// Verify connectivity
async function verifyConnection() {
  const session = driver.session();
  try {
    const result = await session.run('RETURN 1 AS num');
    console.log('Connection successful:', result.records[0].get('num'));
  } finally {
    await session.close();
  }
}

// Close driver on shutdown
async function closeDriver() {
  await driver.close();
}
```

---

### Create Nodes

```typescript
async function createPerson(name: string, age: number, email: string) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      CREATE (p:Person {name: $name, age: $age, email: $email})
      RETURN p
      `,
      { name, age, email }
    );
    
    const person = result.records[0].get('p');
    console.log('Created person:', person.properties);
    
    return person;
  } finally {
    await session.close();
  }
}

// Usage
await createPerson('Alice', 30, 'alice@example.com');
```

---

### Query Nodes

```typescript
async function findPersonByName(name: string) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      MATCH (p:Person {name: $name})
      RETURN p
      `,
      { name }
    );
    
    if (result.records.length === 0) {
      return null;
    }
    
    const person = result.records[0].get('p');
    return person.properties;
  } finally {
    await session.close();
  }
}

// Usage
const alice = await findPersonByName('Alice');
console.log(alice);  // { name: 'Alice', age: 30, email: 'alice@example.com' }
```

---

### Create Relationships

```typescript
async function createFriendship(name1: string, name2: string, since: number) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      MATCH (a:Person {name: $name1}), (b:Person {name: $name2})
      CREATE (a)-[r:FRIENDS_WITH {since: $since}]->(b)
      RETURN r
      `,
      { name1, name2, since }
    );
    
    const relationship = result.records[0].get('r');
    console.log('Created friendship:', relationship.properties);
    
    return relationship;
  } finally {
    await session.close();
  }
}

// Usage
await createFriendship('Alice', 'Bob', 2020);
```

---

### Query Relationships

```typescript
async function findFriends(name: string) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      MATCH (p:Person {name: $name})-[:FRIENDS_WITH]->(friend)
      RETURN friend.name AS name, friend.age AS age
      `,
      { name }
    );
    
    const friends = result.records.map(record => ({
      name: record.get('name'),
      age: record.get('age').toNumber(),  // Convert Neo4j Integer to JS number
    }));
    
    return friends;
  } finally {
    await session.close();
  }
}

// Usage
const aliceFriends = await findFriends('Alice');
console.log(aliceFriends);  // [{ name: 'Bob', age: 28 }, ...]
```

---

### Transactions

```typescript
async function transferFriendship(from: string, to: string, friend: string) {
  const session = driver.session();
  
  const tx = session.beginTransaction();
  
  try {
    // Delete old relationship
    await tx.run(
      `
      MATCH (a:Person {name: $from})-[r:FRIENDS_WITH]->(f:Person {name: $friend})
      DELETE r
      `,
      { from, friend }
    );
    
    // Create new relationship
    await tx.run(
      `
      MATCH (b:Person {name: $to}), (f:Person {name: $friend})
      CREATE (b)-[:FRIENDS_WITH {since: timestamp()}]->(f)
      `,
      { to, friend }
    );
    
    await tx.commit();
    console.log('Friendship transferred successfully');
  } catch (error) {
    await tx.rollback();
    console.error('Transaction failed:', error);
    throw error;
  } finally {
    await session.close();
  }
}
```

---

### Working with Neo4j Integers

Neo4j uses **64-bit integers**, which JavaScript doesn't natively support. The driver returns `neo4j.Integer` objects.

```typescript
import neo4j from 'neo4j-driver';

// Convert to JavaScript number
const age = neo4j.int(30);
const jsNumber = age.toNumber();  // 30

// Create from JavaScript number
const count = neo4j.int(1000);

// In query results
const result = await session.run('MATCH (p:Person) RETURN count(p) AS total');
const total = result.records[0].get('total').toNumber();
```

---

## Use Case Examples

### Example 1: Social Network Recommendations

**Problem**: Recommend friends-of-friends who are not already friends.

```cypher
// Find friend recommendations for Alice
MATCH (alice:Person {name: "Alice"})-[:FRIENDS_WITH]->(friend)-[:FRIENDS_WITH]->(fof)
WHERE NOT (alice)-[:FRIENDS_WITH]->(fof)
  AND alice <> fof
RETURN fof.name AS recommendation, count(*) AS mutualFriends
ORDER BY mutualFriends DESC
LIMIT 10;
```

**TypeScript Implementation:**

```typescript
async function getFriendRecommendations(name: string, limit: number = 10) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      MATCH (user:Person {name: $name})-[:FRIENDS_WITH]->(friend)-[:FRIENDS_WITH]->(fof)
      WHERE NOT (user)-[:FRIENDS_WITH]->(fof)
        AND user <> fof
      RETURN fof.name AS recommendation, count(*) AS mutualFriends
      ORDER BY mutualFriends DESC
      LIMIT $limit
      `,
      { name, limit: neo4j.int(limit) }
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
const recommendations = await getFriendRecommendations('Alice', 5);
console.log(recommendations);
// [
//   { name: 'Charlie', mutualFriends: 3 },
//   { name: 'David', mutualFriends: 2 },
//   ...
// ]
```

---

### Example 2: Product Recommendations (Collaborative Filtering)

**Model:**

```
(User)-[:PURCHASED]->(Product)
```

**Query**: "Users who bought X also bought Y"

```cypher
// Find product recommendations based on purchase history
MATCH (alice:User {name: "Alice"})-[:PURCHASED]->(product:Product)
MATCH (otherUser:User)-[:PURCHASED]->(product)
MATCH (otherUser)-[:PURCHASED]->(recommendation:Product)
WHERE NOT (alice)-[:PURCHASED]->(recommendation)
  AND alice <> otherUser
RETURN recommendation.name AS product, count(*) AS score
ORDER BY score DESC
LIMIT 10;
```

**TypeScript:**

```typescript
async function getProductRecommendations(userName: string, limit: number = 10) {
  const session = driver.session();
  
  try {
    const result = await session.run(
      `
      MATCH (user:User {name: $userName})-[:PURCHASED]->(product:Product)
      MATCH (otherUser:User)-[:PURCHASED]->(product)
      MATCH (otherUser)-[:PURCHASED]->(rec:Product)
      WHERE NOT (user)-[:PURCHASED]->(rec)
        AND user <> otherUser
      RETURN rec.name AS product, rec.price AS price, count(*) AS score
      ORDER BY score DESC
      LIMIT $limit
      `,
      { userName, limit: neo4j.int(limit) }
    );
    
    return result.records.map(record => ({
      product: record.get('product'),
      price: record.get('price').toNumber(),
      score: record.get('score').toNumber(),
    }));
  } finally {
    await session.close();
  }
}
```

---

### Example 3: Fraud Detection (Shared Identifiers)

**Problem**: Detect fraudulent accounts by finding users who share phone numbers or email addresses.

```cypher
// Create model
CREATE
  (alice:User {id: 1, name: "Alice"}),
  (bob:User {id: 2, name: "Bob"}),
  (charlie:User {id: 3, name: "Charlie"}),
  
  (phone1:Phone {number: "555-1234"}),
  (phone2:Phone {number: "555-5678"}),
  (email1:Email {address: "shared@example.com"}),
  
  (alice)-[:HAS_PHONE]->(phone1),
  (bob)-[:HAS_PHONE]->(phone1),     // Shared phone!
  (bob)-[:HAS_EMAIL]->(email1),
  (charlie)-[:HAS_EMAIL]->(email1);  // Shared email!

// Find suspicious clusters
MATCH (u1:User)-[:HAS_PHONE|HAS_EMAIL]->(shared)<-[:HAS_PHONE|HAS_EMAIL]-(u2:User)
WHERE u1 <> u2
RETURN u1.name, u2.name, collect(DISTINCT shared) AS sharedIdentifiers;
```

**Result:**

```
u1.name   u2.name    sharedIdentifiers
Alice     Bob        [Phone {number: "555-1234"}]
Bob       Charlie    [Email {address: "shared@example.com"}]
```

---

### Example 4: Shortest Path (Routing)

**Problem**: Find shortest route between cities.

```cypher
// Create road network
CREATE
  (nyc:City {name: "New York"}),
  (boston:City {name: "Boston"}),
  (dc:City {name: "Washington DC"}),
  (atlanta:City {name: "Atlanta"}),
  
  (nyc)-[:ROAD {distance: 215}]->(boston),
  (nyc)-[:ROAD {distance: 225}]->(dc),
  (dc)-[:ROAD {distance: 640}]->(atlanta),
  (boston)-[:ROAD {distance: 850}]->(atlanta);

// Find shortest path from NYC to Atlanta
MATCH path = shortestPath(
  (start:City {name: "New York"})-[:ROAD*]-(end:City {name: "Atlanta"})
)
RETURN [node in nodes(path) | node.name] AS cities,
       reduce(dist = 0, rel in relationships(path) | dist + rel.distance) AS totalDistance;
```

**Result:**

```
cities                              totalDistance
["New York", "Washington DC", "Atlanta"]    865
```

---

## Performance Best Practices

### 1. Always Use Labels in MATCH

```cypher
-- ❌ Slow (scans all nodes)
MATCH (n {name: "Alice"})
RETURN n;

-- ✅ Fast (uses label index)
MATCH (p:Person {name: "Alice"})
RETURN p;
```

### 2. Create Indexes on Frequently Queried Properties

```cypher
CREATE INDEX person_name FOR (p:Person) ON (p.name);
CREATE INDEX product_id FOR (p:Product) ON (p.id);
```

### 3. Use LIMIT to Prevent Runaway Queries

```cypher
-- ❌ Dangerous (could return millions of rows)
MATCH (p:Person)-[:FRIENDS_WITH*]->(connected)
RETURN connected;

-- ✅ Safe
MATCH (p:Person {name: "Alice"})-[:FRIENDS_WITH*1..3]->(connected)
RETURN DISTINCT connected
LIMIT 100;
```

### 4. Use Parameters (Avoid String Concatenation)

```typescript
// ❌ SQL injection risk
await session.run(`MATCH (p:Person {name: "${name}"}) RETURN p`);

// ✅ Safe with parameters
await session.run('MATCH (p:Person {name: $name}) RETURN p', { name });
```

### 5. Close Sessions and Drivers

```typescript
// Always close sessions
const session = driver.session();
try {
  // queries...
} finally {
  await session.close();
}

// Close driver on application shutdown
process.on('SIGINT', async () => {
  await driver.close();
  process.exit(0);
});
```

---

## Common Pitfalls

### ❌ Forgetting DETACH DELETE

```cypher
-- ❌ Error: Cannot delete node with relationships
MATCH (p:Person {name: "Alice"})
DELETE p;

-- ✅ Delete node and all relationships
MATCH (p:Person {name: "Alice"})
DETACH DELETE p;
```

### ❌ Creating Duplicate Relationships with CREATE

```cypher
-- ❌ Creates duplicate relationships on each run
CREATE (a:Person {name: "Alice"})-[:FRIENDS_WITH]->(b:Person {name: "Bob"});

-- ✅ Use MERGE to prevent duplicates
MERGE (a:Person {name: "Alice"})
MERGE (b:Person {name: "Bob"})
MERGE (a)-[:FRIENDS_WITH]->(b);
```

### ❌ Not Handling Neo4j Integers

```typescript
// ❌ Wrong: age is a Neo4j Integer object
const age = result.records[0].get('age');
console.log(age + 1);  // Incorrect result

// ✅ Convert to JavaScript number
const age = result.records[0].get('age').toNumber();
console.log(age + 1);  // Correct
```

### ❌ Unbounded Variable-Length Paths

```cypher
-- ❌ Could traverse entire graph (slow!)
MATCH (a:Person)-[:FRIENDS_WITH*]->(b)
RETURN b;

-- ✅ Set maximum depth
MATCH (a:Person)-[:FRIENDS_WITH*1..5]->(b)
RETURN DISTINCT b
LIMIT 100;
```

---

## When to Use Neo4j

### ✅ Use Neo4j When:

- **Relationship-heavy queries**: Friends-of-friends, recommendations, influence paths
- **Graph algorithms**: Shortest path, PageRank, community detection
- **Dynamic schemas**: Relationships evolve over time
- **Deep traversals**: Need to query 3+ levels of relationships
- **Pattern matching**: Detect fraud, anomalies, or complex patterns

### ❌ Avoid Neo4j When:

- **Simple CRUD**: Basic create/read/update/delete (use PostgreSQL)
- **Large aggregations**: Summing millions of rows (use PostgreSQL or columnar DBs)
- **Unconnected data**: No relationships between entities
- **ACID transactions at scale**: Banking systems (use PostgreSQL)
- **Time-series data**: Metrics, logs (use TimescaleDB)

---

## Summary

| Concept | Description |
|---------|-------------|
| **Graph Model** | Nodes, relationships, properties, labels |
| **Cypher** | Declarative query language with ASCII art patterns |
| **CREATE** | Insert nodes and relationships |
| **MATCH** | Query patterns in the graph |
| **MERGE** | Upsert (create if not exists, match otherwise) |
| **DELETE** | Remove nodes (use DETACH DELETE for nodes with relationships) |
| **Indexes** | Speed up node lookups by property |
| **Constraints** | Enforce uniqueness and existence |
| **neo4j-driver** | TypeScript client for Neo4j |
| **Use Cases** | Social networks, recommendations, fraud detection, routing |

---

## Next Steps

1. ✅ Set up Neo4j with Docker
2. ✅ Practice Cypher in Neo4j Browser (http://localhost:7474)
3. ✅ Implement friend recommendations
4. ✅ Build product recommendation engine
5. ✅ Explore APOC procedures for advanced algorithms
6. 📚 Read [Neo4j Graph Algorithms](https://neo4j.com/docs/graph-data-science/current/)

---

**Happy graphing! 🚀**
