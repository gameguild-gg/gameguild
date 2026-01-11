# Week 12 Readings & Resources - Graph Databases & Neo4j

Curated collection of documentation, tutorials, tools, videos, and books for learning Neo4j and graph databases.

---

## Official Documentation

### Neo4j Documentation

**🔗 [Neo4j Documentation](https://neo4j.com/docs/)**  
Comprehensive official documentation covering all aspects of Neo4j.

**🔗 [Cypher Manual](https://neo4j.com/docs/cypher-manual/current/)**  
Complete reference for Cypher query language syntax, patterns, and functions.

**🔗 [Neo4j Operations Manual](https://neo4j.com/docs/operations-manual/current/)**  
Deployment, configuration, backup, monitoring, and scaling.

**🔗 [Neo4j Driver Manual](https://neo4j.com/docs/driver-manual/current/)**  
Official drivers for JavaScript/TypeScript, Python, Java, .NET, Go.

---

## Interactive Learning

### Neo4j GraphAcademy

**🔗 [GraphAcademy](https://graphacademy.neo4j.com/)**  
Free interactive courses with hands-on exercises and certifications.

**Recommended Courses:**

1. **[Neo4j Fundamentals](https://graphacademy.neo4j.com/courses/neo4j-fundamentals/)**  
   Introduction to graph databases, Cypher basics, and Neo4j Browser.

2. **[Cypher Fundamentals](https://graphacademy.neo4j.com/courses/cypher-fundamentals/)**  
   Deep dive into Cypher: MATCH, CREATE, MERGE, aggregations, path queries.

3. **[Graph Data Modeling Fundamentals](https://graphacademy.neo4j.com/courses/modeling-fundamentals/)**  
   Best practices for designing graph schemas, denormalization, performance optimization.

4. **[Building Neo4j Applications with Node.js](https://graphacademy.neo4j.com/courses/app-nodejs/)**  
   TypeScript/JavaScript integration with neo4j-driver, transactions, connection pooling.

5. **[Graph Data Science Fundamentals](https://graphacademy.neo4j.com/courses/gds-fundamentals/)**  
   PageRank, community detection, shortest path algorithms, link prediction.

---

## TypeScript/JavaScript Integration

### neo4j-driver

**🔗 [neo4j-driver NPM Package](https://www.npmjs.com/package/neo4j-driver)**  
Official JavaScript driver for Neo4j.

**🔗 [neo4j-driver API Documentation](https://neo4j.com/docs/api/javascript-driver/current/)**  
API reference with TypeScript type definitions.

**🔗 [Connection Pooling & Session Management](https://neo4j.com/docs/driver-manual/current/sessions-transactions/)**  
Best practices for managing connections and transactions.

**Installation:**

```bash
npm install neo4j-driver
npm install -D @types/node
```

**Example:**

```typescript
import neo4j from 'neo4j-driver';

const driver = neo4j.driver(
  'bolt://localhost:7687',
  neo4j.auth.basic('neo4j', 'password')
);

const session = driver.session();
const result = await session.run('MATCH (n) RETURN count(n) AS count');
console.log(result.records[0].get('count').toNumber());
await session.close();
await driver.close();
```

---

## Cypher Query Language

### Cypher References

**🔗 [Cypher Refcard](https://neo4j.com/docs/cypher-refcard/current/)**  
Quick reference cheat sheet for Cypher syntax (PDF available).

**🔗 [Cypher Style Guide](https://neo4j.com/developer/cypher-style-guide/)**  
Best practices for writing readable and maintainable Cypher queries.

**🔗 [Cypher Query Tuning](https://neo4j.com/developer/guide-performance-tuning/)**  
Performance optimization, query profiling with `EXPLAIN` and `PROFILE`.

**Example: Query Profiling**

```cypher
// Analyze query plan
EXPLAIN
MATCH (p:Person)-[:FRIENDS_WITH]->(friend)
WHERE p.name = "Alice"
RETURN friend.name;

// Analyze query execution
PROFILE
MATCH (p:Person)-[:FRIENDS_WITH]->(friend)
WHERE p.name = "Alice"
RETURN friend.name;
```

---

## Graph Algorithms

### Neo4j Graph Data Science Library

**🔗 [Graph Data Science Documentation](https://neo4j.com/docs/graph-data-science/current/)**  
Comprehensive guide to GDS library for graph algorithms.

**Algorithms Covered:**

- **Pathfinding:** Shortest Path, All Shortest Paths, A*, Dijkstra, Yen's K-Shortest Paths
- **Centrality:** PageRank, Betweenness, Closeness, Degree, Eigenvector
- **Community Detection:** Louvain, Label Propagation, Weakly Connected Components
- **Similarity:** Jaccard, Cosine, Overlap, Pearson
- **Link Prediction:** Adamic Adar, Common Neighbors, Preferential Attachment
- **Node Embeddings:** Node2Vec, GraphSAGE

**Installation (Neo4j Plugin):**

```yaml
# docker-compose.yml
environment:
  - NEO4J_PLUGINS=["graph-data-science"]
```

**Example: PageRank**

```cypher
// Create projection
CALL gds.graph.project(
  'myGraph',
  'Person',
  'FRIENDS_WITH'
);

// Run PageRank
CALL gds.pageRank.stream('myGraph')
YIELD nodeId, score
RETURN gds.util.asNode(nodeId).name AS person, score
ORDER BY score DESC;
```

---

## Tools & Visualizations

### Neo4j Browser

**🔗 [Neo4j Browser Guide](https://neo4j.com/developer/neo4j-browser/)**  
Built-in web interface for querying and visualizing graphs.

**Access:** http://localhost:7474 (when running Neo4j locally)

**Features:**
- Interactive Cypher query editor
- Graph visualizations with customizable styling
- Query history and favorites
- Data export (CSV, JSON)

---

### Neo4j Desktop

**🔗 [Neo4j Desktop Download](https://neo4j.com/download/)**  
Desktop application for managing local Neo4j instances.

**Features:**
- Create and manage multiple databases
- Install plugins (APOC, GDS)
- Graph app marketplace
- Database backups and imports

---

### Neo4j Bloom

**🔗 [Neo4j Bloom](https://neo4j.com/product/bloom/)**  
Visual graph exploration tool for non-technical users.

**Use Cases:**
- Fraud investigation
- Knowledge graph exploration
- Pattern discovery

---

### Cypher Shell

**🔗 [Cypher Shell](https://neo4j.com/docs/operations-manual/current/tools/cypher-shell/)**  
Command-line tool for executing Cypher queries.

```bash
# Connect to Neo4j
docker exec -it neo4j cypher-shell -u neo4j -p password

# Execute Cypher file
cypher-shell -u neo4j -p password < script.cypher
```

---

## Data Modeling

### Graph Modeling Resources

**🔗 [Graph Data Modeling Guidelines](https://neo4j.com/developer/guide-data-modeling/)**  
Best practices for translating domain models to graph schemas.

**🔗 [Graph Modeling Workshop](https://neo4j.com/graphacademy/training-modeling-40/)**  
Free hands-on workshop for graph modeling techniques.

**Key Concepts:**
- **Nodes:** Entities (nouns)
- **Relationships:** Connections (verbs)
- **Properties:** Attributes (adjectives)
- **Labels:** Categories/types

**Anti-Patterns to Avoid:**

1. ❌ **Dense nodes:** Nodes with 100,000+ relationships (causes performance issues)
2. ❌ **Overly generic relationships:** Use specific relationship types (`:KNOWS` instead of `:RELATED_TO`)
3. ❌ **Storing arrays instead of relationships:** Model connections explicitly
4. ❌ **Using properties for relationships:** If data connects two entities, use a relationship

---

## Use Cases & Case Studies

### Neo4j Use Case Library

**🔗 [Neo4j Use Cases](https://neo4j.com/use-cases/)**  
Real-world applications of graph databases.

**Categories:**

1. **Fraud Detection & Risk Management**
   - Network analysis for insurance fraud
   - Anti-money laundering (AML)
   - Credit card fraud detection

2. **Real-Time Recommendations**
   - E-commerce product recommendations
   - Content recommendations (Netflix, Airbnb)
   - Friend suggestions (LinkedIn, Facebook)

3. **Knowledge Graphs**
   - Wikipedia knowledge graph
   - NASA's lessons learned database
   - Medical research (drug interactions)

4. **Network & IT Operations**
   - Dependency mapping
   - Impact analysis
   - Root cause analysis

5. **Identity & Access Management**
   - Role-based access control (RBAC)
   - Permission inheritance
   - Organizational hierarchies

---

### Case Studies

**🔗 [Walmart: Real-Time Recommendations](https://neo4j.com/case-studies/walmart/)**  
How Walmart uses Neo4j for product recommendations.

**🔗 [eBay: Fraud Detection](https://neo4j.com/blog/how-ebay-uses-graph-databases-fraud-detection/)**  
Using graph algorithms to identify fraudulent seller networks.

**🔗 [NASA: Lessons Learned Database](https://neo4j.com/blog/nasa-lesson-learned-database-using-neo4j-linkurious/)**  
Knowledge graph for aerospace engineering lessons.

---

## Video Tutorials

### YouTube Channels

**🔗 [Neo4j YouTube Channel](https://www.youtube.com/neo4j)**  
Official channel with tutorials, webinars, and conference talks.

**Recommended Videos:**

1. **[Neo4j in 100 Seconds](https://www.youtube.com/watch?v=T6L9EoBy8Zk)** (Fireship)  
   Quick introduction to graph databases and Neo4j.

2. **[Graph Database Crash Course](https://www.youtube.com/watch?v=8jNPelugC2s)** (freeCodeCamp)  
   Comprehensive tutorial (1 hour) covering Cypher, modeling, and use cases.

3. **[Neo4j Tutorial for Beginners](https://www.youtube.com/watch?v=urO5FyP9PoI)** (Traversy Media)  
   Hands-on tutorial building a movie recommendation system.

4. **[Graph Databases Will Change Your Freakin' Life](https://www.youtube.com/watch?v=GekQqFZm7mA)** (Ed Finkler)  
   Entertaining talk on why graphs matter (30 min).

5. **[Cypher Query Language Tutorial](https://www.youtube.com/watch?v=l76udM3wB4U)** (Academind)  
   Deep dive into Cypher syntax and patterns (45 min).

---

### Conference Talks

**🔗 [NODES (Neo4j Online Developer Expo & Summit)](https://neo4j.com/nodes-2023/)**  
Annual conference with talks on graph databases, algorithms, and real-world use cases.

**Recommended Talks:**

- **"Graph Algorithms for Data Science"** - Practical applications of PageRank, community detection
- **"Building Recommendation Engines with Neo4j"** - Collaborative filtering techniques
- **"Fraud Detection with Graph Analytics"** - Pattern detection and anomaly analysis

---

## Books

### Recommended Reading

**📚 [Graph Databases, 2nd Edition](https://neo4j.com/graph-databases-book/)** (Free)  
By Ian Robinson, Jim Webber, Emil Eifrem (Neo4j founders)

- Introduction to graph theory
- Graph vs relational databases
- Data modeling best practices
- Cypher query patterns
- Real-world case studies

**📚 [Learning Neo4j](https://www.oreilly.com/library/view/learning-neo4j/9781783287758/)**  
By Rik Van Bruggen

- Beginner-friendly introduction
- Step-by-step tutorials
- Building applications with Neo4j
- Performance tuning

**📚 [Fullstack GraphQL Applications](https://www.manning.com/books/fullstack-graphql-applications)**  
By William Lyon

- Combining GraphQL with Neo4j
- Building modern APIs
- React + Apollo + Neo4j stack

---

## Practice Datasets

### Neo4j Sandbox

**🔗 [Neo4j Sandbox](https://neo4j.com/sandbox/)**  
Free cloud instances with pre-loaded datasets.

**Datasets Available:**

1. **Movies** - Actors, directors, movies, ratings
2. **Recommendations** - E-commerce product recommendations
3. **Twitter Network** - Social network analysis
4. **Fraud Detection** - Transaction patterns
5. **Network Management** - IT infrastructure dependencies

---

### Public Datasets

**🔗 [Neo4j Dataset Gallery](https://neo4j.com/developer/example-data/)**  
Curated list of public datasets in Neo4j format.

**Examples:**

- **Northwind Database** (classic SQL → graph conversion)
- **Game of Thrones** (character relationships)
- **Marvel Universe** (superhero connections)
- **Stack Overflow** (Q&A network)
- **Football Transfer Market** (player transfers)

**Load Dataset Example:**

```cypher
// Load Northwind database
:play northwind-graph

// Follow interactive guide to load data
```

---

## Community & Forums

### Neo4j Community

**🔗 [Neo4j Community Forum](https://community.neo4j.com/)**  
Ask questions, share projects, and get help from Neo4j experts.

**🔗 [Neo4j Discord](https://discord.gg/neo4j)**  
Real-time chat with the Neo4j community.

**🔗 [Stack Overflow: Neo4j Tag](https://stackoverflow.com/questions/tagged/neo4j)**  
Search existing questions or post new ones.

**🔗 [Neo4j Reddit](https://www.reddit.com/r/Neo4j/)**  
Community discussions and news.

---

## Graph Theory Foundations

### Academic Resources

**🔗 [Graph Theory on Wikipedia](https://en.wikipedia.org/wiki/Graph_theory)**  
Introduction to mathematical foundations of graphs.

**🔗 [Introduction to Graph Theory](https://www.youtube.com/playlist?list=PLDcUM9US4XdMROZ57-OIRtIK0aOynbgZN)** (YouTube Playlist)  
MIT OpenCourseWare lectures on graph theory.

**Key Concepts:**

- **Nodes (Vertices)** and **Edges (Relationships)**
- **Directed vs Undirected Graphs**
- **Weighted Graphs** (edges with properties)
- **Paths, Cycles, Trees**
- **Connectivity** (strongly connected, weakly connected)
- **Degree Distribution** (in-degree, out-degree)

---

## Comparison Articles

### Neo4j vs Other Databases

**🔗 [Graph Databases vs Relational Databases](https://neo4j.com/blog/rdbms-vs-graph-database/)**  
When to use each paradigm.

**🔗 [Neo4j vs MongoDB](https://neo4j.com/blog/neo4j-vs-mongodb/)**  
Graph databases vs document databases for connected data.

**🔗 [Performance Comparison: Neo4j vs MySQL](https://neo4j.com/news/how-much-faster-is-a-graph-database-really/)**  
Benchmark results for relationship-heavy queries.

**Summary:**

| Query Type | Relational (MySQL) | Graph (Neo4j) |
|------------|-------------------|---------------|
| 1-hop (direct friends) | Fast (indexed FK) | Fast (O(1)) |
| 2-hops (friends-of-friends) | Slow (2 joins) | Fast (O(1)) |
| 3-hops | Very slow (3 joins) | Fast (O(1)) |
| 4+ hops | Prohibitively slow | Fast (O(1)) |

Neo4j's **index-free adjacency** ensures constant-time traversal regardless of depth.

---

## Advanced Topics

### APOC (Awesome Procedures on Cypher)

**🔗 [APOC Documentation](https://neo4j.com/labs/apoc/)**  
Library of 450+ procedures and functions extending Cypher.

**Common Use Cases:**

- **Data import/export:** Load JSON, CSV, XML
- **Graph algorithms:** BFS, DFS, path expansion
- **Utilities:** UUID generation, date formatting, string manipulation
- **Triggers:** Event-driven actions

**Installation:**

```yaml
# docker-compose.yml
environment:
  - NEO4J_PLUGINS=["apoc"]
```

**Example: Load JSON Data**

```cypher
CALL apoc.load.json("https://api.example.com/users") YIELD value
CREATE (u:User {name: value.name, email: value.email});
```

---

### Neo4j Spatial

**🔗 [Neo4j Spatial](https://neo4j.com/docs/labs/neo4j-spatial/current/)**  
Plugin for geospatial data (points, polygons, distance calculations).

**Use Cases:**
- Location-based recommendations
- Route optimization
- Geofencing

---

### Neo4j Streams

**🔗 [Neo4j Streams](https://neo4j.com/labs/kafka/)**  
Integration with Apache Kafka for real-time data streaming.

**Use Cases:**
- Event sourcing
- Change data capture (CDC)
- Real-time analytics

---

## Cheat Sheets

### Cypher Cheat Sheet

**🔗 [Neo4j Cypher Refcard](https://neo4j.com/docs/cypher-refcard/current/)** (PDF)

**Quick Reference:**

```cypher
// CREATE
CREATE (n:Label {prop: "value"})
CREATE (a)-[:REL_TYPE {prop: "value"}]->(b)

// MATCH
MATCH (n:Label)
MATCH (a)-[:REL_TYPE]->(b)
MATCH (a)-[:REL_TYPE*1..3]->(b)  // Variable-length

// MERGE (upsert)
MERGE (n:Label {prop: "value"})
ON CREATE SET n.created = timestamp()
ON MATCH SET n.updated = timestamp()

// UPDATE
SET n.prop = "new value"
REMOVE n.prop

// DELETE
DELETE n              // Fails if node has relationships
DETACH DELETE n       // Deletes node and relationships

// AGGREGATION
count(), sum(), avg(), min(), max(), collect()

// OPTIONAL MATCH (LEFT JOIN)
OPTIONAL MATCH (n)-[:REL]->(m)

// ORDER, LIMIT, SKIP
ORDER BY n.prop DESC
LIMIT 10
SKIP 20

// INDEXES
CREATE INDEX FOR (n:Label) ON (n.prop)
CREATE CONSTRAINT FOR (n:Label) REQUIRE n.prop IS UNIQUE
```

---

## Additional Resources

**🔗 [Awesome Neo4j](https://github.com/neueda/awesome-neo4j)**  
Curated list of Neo4j resources, libraries, and tools (GitHub).

**🔗 [Neo4j Blog](https://neo4j.com/blog/)**  
Latest news, tutorials, and case studies.

**🔗 [Neo4j Developer Guides](https://neo4j.com/developer/get-started/)**  
Comprehensive guides for various programming languages and frameworks.

---

**Happy learning! 🚀**
