---
renderer: reveal
---

# The Database Zoo

Welcome to the database zoo!

Just like a real zoo houses different animals adapted to different environments, the database ecosystem contains many specialized systems.

---

<div style="display: flex; justify-content: center; align-items: center; height: 100%;">
<img src="https://i.programmerhumor.io/2022/10/programmerhumor-io-databases-memes-backend-memes-4713ea9c8ef8767.jpg" alt="Database Zoo meme" style="max-height: 500px; width: auto; object-fit: contain;" />
</div>

---

## Why So Many Database Types?

In the early days: **relational databases ruled supreme**

But as data grew in **volume**, **velocity**, and **variety**...

Specialized databases emerged for specific use cases.

---

## Polyglot Persistence

Modern applications often use **multiple database types** together

> No single database is perfect for everything. Each type makes trade-offs between consistency, availability, performance, and flexibility.

---

# 1. Relational Databases (RDBMS)

**The Classic Workhorse** 🐴

---

## What is RDBMS?

Data organized into **tables** (relations) with:

- **Rows** (records)
- **Columns** (attributes)

Uses **SQL** for querying

Supports **ACID transactions** for data integrity

---

## Key Characteristics

- **Structured schema**: Data must conform to a predefined schema
- **ACID compliance**: Strong consistency guarantees
- **Relationships**: Foreign keys link tables together
- **SQL**: Powerful, declarative query language

---

## How Data Looks

```
┌─────────────────────────────────────────────────────┐
│                     users                           │
├────────┬───────────────┬─────────────┬──────────────┤
│   id   │     name      │    email    │  created_at  │
├────────┼───────────────┼─────────────┼──────────────┤
│   1    │  Alice Smith  │ alice@...   │  2024-01-15  │
│   2    │  Bob Jones    │ bob@...     │  2024-01-16  │
│   3    │  Carol White  │ carol@...   │  2024-01-17  │
└────────┴───────────────┴─────────────┴──────────────┘
```

---

## Entity Relationship Diagram

```
┌──────────────────────┐         ┌──────────────────────┐
│        USERS         │         │        ORDERS        │
├──────────────────────┤         ├──────────────────────┤
│    id (PK)           │         │    id (PK)           │
│    name              │         │    user_id (FK)      │
│    email             │         │    amount            │
│    created_at        │         │    order_date        │
└──────────────────────┘         └──────────────────────┘
           │                              │
           │          places              │
           └──────────── 1:N ─────────────┘
```

---

## Popular Examples

| Database       | Known For                               |
| -------------- | --------------------------------------- |
| **PostgreSQL** | Advanced features, extensibility, JSONB |
| **MySQL**      | Web applications, ease of use           |
| **SQLite**     | Embedded, serverless, file-based        |
| **SQL Server** | Enterprise, Windows integration         |
| **Oracle**     | Enterprise, high availability           |

---

## Best Use Cases

- 💳 Financial transactions and banking systems
- 🛒 E-commerce platforms with complex orders
- 📊 Business applications with reporting needs
- 🏢 Any system requiring strong data integrity

---

## Example Query

```sql
-- Find all orders with customer names
SELECT u.name, o.amount, o.order_date
FROM users u
JOIN orders o ON u.id = o.user_id
WHERE o.amount > 100
ORDER BY o.order_date DESC;
```

---

# 2. Document Databases

**The Flexible Shape-Shifter** 🦎

---

## What are Document DBs?

Store data as **documents** (usually JSON or BSON)

**Flexible, nested structures** without predefined schema

Each document can have a **different structure**

---

## Key Characteristics

- **Schema-less**: Documents can have different fields
- **Nested data**: Natural representation of hierarchical data
- **Document-oriented queries**: Query by any field
- **Horizontal scaling**: Built for distributed systems

---

## How Data Looks

```json
{
  "_id": "user_12345",
  "name": "Alice Smith",
  "email": "alice@example.com",
  "profile": {
    "bio": "Software developer",
    "social": { "twitter": "@alice" }
  },
  "orders": [{ "id": "ord_1", "amount": 99.99 }],
  "tags": ["premium", "early-adopter"]
}
```

---

## Popular Examples

| Database              | Known For                                     |
| --------------------- | --------------------------------------------- |
| **MongoDB**           | Most popular document DB, rich query language |
| **CouchDB**           | Multi-master replication, offline-first       |
| **Amazon DocumentDB** | MongoDB-compatible, AWS managed               |
| **Firestore**         | Real-time sync, mobile/web apps               |

---

## Best Use Cases

- 📱 Mobile app backends with varying data structures
- 📝 Content management systems (CMS)
- 🛍️ Product catalogs with different attributes
- 👤 User profiles with customizable fields
- 🚀 Rapid prototyping and evolving schemas

---

## Example Query (MongoDB)

```javascript
// Find premium users with orders over $100
db.users.find(
  {
    tags: 'premium',
    'orders.amount': { $gt: 100 },
  },
  {
    name: 1,
    email: 1,
    'orders.$': 1,
  },
);
```

---

# 3. Key-Value Stores

**The Speed Demon** ⚡

---

## What are Key-Value Stores?

The **simplest and fastest** database type

Data stored as pairs of **keys** and **values**

Like a giant **hash map** or dictionary

---

## Key Characteristics

- **Simple model**: Just keys and values
- **Blazing fast**: O(1) lookups
- **In-memory options**: Sub-millisecond latency
- **TTL support**: Automatic expiration of data

---

## How Data Looks

```
┌─────────────────────────┬───────────────────────────┐
│          Key            │          Value            │
├─────────────────────────┼───────────────────────────┤
│  session:abc123         │  {"user_id": 42, ...}     │
│  user:42:name           │  "Alice Smith"            │
│  cache:product:99       │  {product data...}        │
│  rate_limit:ip:1.2.3.4  │  "47"                     │
│  leaderboard:game1      │  [sorted set of scores]   │
└─────────────────────────┴───────────────────────────┘
```

---

## Popular Examples

| Database            | Known For                                |
| ------------------- | ---------------------------------------- |
| **Redis**           | In-memory, rich data structures, Pub/Sub |
| **Memcached**       | Simple caching, multi-threaded           |
| **Amazon DynamoDB** | Serverless, auto-scaling, AWS native     |
| **etcd**            | Distributed configuration, Kubernetes    |

---

## Best Use Cases

- ⚡ Caching frequently accessed data
- 🔐 Session management
- 🚦 Rate limiting
- 🏆 Real-time leaderboards
- 📊 Counters and metrics
- 💬 Pub/Sub messaging

---

## Examples (Redis)

```bash
# Set a session with 30-minute TTL
SET session:abc123 '{"user_id": 42}' EX 1800

# Atomic increment of a counter
INCR page_views:home

# Add to a sorted set (leaderboard)
ZADD leaderboard:game1 1500 "player_42"

# Get top 10 players
ZREVRANGE leaderboard:game1 0 9 WITHSCORES
```

---

# 4. Graph Databases

**The Relationship Expert** 🕸️

---

## What are Graph DBs?

**Relationships as first-class citizens**

Data stored as **nodes** (entities) connected by **edges** (relationships)

Easy to traverse complex connections

---

## Key Characteristics

- **Nodes and edges**: Natural representation of connections
- **Relationship traversal**: Efficient path queries
- **Pattern matching**: Find complex relationship patterns
- **No JOINs needed**: Relationships are pre-computed

---

## How Data Looks

```
    | NODES (Entities) |      | EDGES (Relationships)   |
    │     Alice        │      │ Alice ─FOLLOWS─> Bob    │
    │     Bob          │      │ Alice ─FOLLOWS─> Carol  │
    │     Carol        │      │ Bob ───WORKS_AT─> TechCo│
    │     TechCo       │      │ Carol ─WORKS_AT─> TechCo│
                      Alice
              FOLLOWS      FOLLOWS
          Bob                   Carol
            WORKS_AT      WORKS_AT
                    TechCo
```

---

## Popular Examples

| Database           | Known For                           |
| ------------------ | ----------------------------------- |
| **Neo4j**          | Most popular, Cypher query language |
| **Amazon Neptune** | Managed, supports Gremlin & SPARQL  |
| **ArangoDB**       | Multi-model (graph + document)      |
| **TigerGraph**     | Massively parallel, analytics       |

---

## Best Use Cases

- 👥 Social networks (friends, followers, connections)
- 🎯 Recommendation engines
- 🕵️ Fraud detection
- 🧠 Knowledge graphs
- 🗺️ Network and infrastructure mapping

---

## Example Query (Cypher)

```cypher
// Find friends of friends who work at same company
MATCH (me:Person {name: 'Alice'})
      -[:FOLLOWS]->(friend)
      -[:FOLLOWS]->(fof)
WHERE (fof)-[:WORKS_AT]->(:Company)<-[:WORKS_AT]-(me)
RETURN fof.name, count(*) as mutual_friends
ORDER BY mutual_friends DESC
LIMIT 10;
```

---

# 5. Time Series Databases

**The Historian** 📈

---

## What are Time Series DBs?

Optimized for **time-stamped data**

Measurements that **change over time**

Excel at storing, compressing, and querying **temporal data**

---

## Key Characteristics

- **Time-indexed**: Data organized by timestamp
- **High write throughput**: Handle millions of data points
- **Automatic downsampling**: Aggregate old data to save space
- **Retention policies**: Auto-delete old data
- **Time-based queries**: Efficient range queries

---

## How Data Looks

```
┌─────────────────────────┬──────────┬───────────┬───────────┐
│        timestamp        │ sensor_id│   temp    │  humidity │
├─────────────────────────┼──────────┼───────────┼───────────┤
│ 2024-01-15 10:00:00.000 │  sens_01 │   22.5    │    45     │
│ 2024-01-15 10:00:01.000 │  sens_01 │   22.6    │    45     │
│ 2024-01-15 10:00:02.000 │  sens_01 │   22.4    │    46     │
│          ...            │   ...    │   ...     │   ...     │
└─────────────────────────┴──────────┴───────────┴───────────┘
```

---

## Popular Examples

| Database        | Known For                            |
| --------------- | ------------------------------------ |
| **TimescaleDB** | PostgreSQL extension, SQL compatible |
| **InfluxDB**    | Purpose-built, InfluxQL/Flux         |
| **Prometheus**  | Monitoring, pull-based metrics       |
| **QuestDB**     | High performance, SQL                |

---

## Best Use Cases

- 📈 IoT sensor data
- 🖥️ Application metrics and monitoring
- 💹 Financial market data (tick data)
- 📊 Analytics and dashboards
- 🏭 Industrial equipment monitoring

---

## Example Query (TimescaleDB)

```sql
-- Average temperature per hour for last 24 hours
SELECT
    time_bucket('1 hour', timestamp) AS hour,
    sensor_id,
    AVG(temp) as avg_temp
FROM sensor_readings
WHERE timestamp > NOW() - INTERVAL '24 hours'
GROUP BY hour, sensor_id
ORDER BY hour DESC;
```

---

# 6. Search Engines

**The Librarian** 🔍

---

## What are Search Engines?

Databases optimized for **full-text search**

Use **inverted indices** to find documents by content

Almost instant search across **billions of documents**

---

## Key Characteristics

- **Inverted index**: Maps words to documents
- **Full-text search**: Find documents by content
- **Relevance scoring**: Rank results by relevance
- **Analyzers**: Tokenization, stemming, synonyms
- **Fuzzy matching**: Handle typos and variations

---

## Inverted Index Example

```
Document Storage:
doc_1: "The quick brown fox jumps over the lazy dog"
doc_2: "Quick brown foxes are quick"

Inverted Index:
┌──────────┬─────────────────┐
│   Term   │   Documents     │
├──────────┼─────────────────┤
│  quick   │  doc_1, doc_2   │
│  brown   │  doc_1, doc_2   │
│  lazy    │  doc_1          │
│  dog     │  doc_1          │
└──────────┴─────────────────┘
```

---

## Popular Examples

| Database          | Known For                         |
| ----------------- | --------------------------------- |
| **Elasticsearch** | Most popular, part of ELK stack   |
| **OpenSearch**    | AWS fork of Elasticsearch         |
| **Meilisearch**   | Developer-friendly, typo-tolerant |
| **Typesense**     | Fast, typo-tolerant, easy to use  |

---

## Best Use Cases

- 🔍 Site search and product search
- 📝 Log analysis and monitoring
- 📚 Document search (articles, PDFs)
- 🛒 E-commerce with faceted navigation
- 💡 Autocomplete and suggestions

---

## Example Query (Elasticsearch)

```json
{
  "query": {
    "bool": {
      "must": {
        "multi_match": {
          "query": "wireless headphones",
          "fields": ["name^3", "description"],
          "fuzziness": "AUTO"
        }
      },
      "filter": [{ "range": { "price": { "lte": 200 } } }]
    }
  }
}
```

---

# 7. Vector Databases

**The AI Whisperer** 🤖

---

## What are Vector DBs?

Store and search **high-dimensional vectors** (embeddings)

Numerical representations from **ML models**

Enable **similarity search** for AI applications

---

## Key Characteristics

- **Embedding storage**: Store high-dimensional vectors
- **Similarity search**: Find nearest neighbors
- **Distance metrics**: Cosine, Euclidean, dot product
- **Approximate search**: Trade accuracy for speed (ANN)
- **Hybrid search**: Combine vector + keyword search

---

## How Similarity Search Works

```
Query: "How do I reset my password?"
       ↓ (convert to embedding)
Query Vector: [0.023, -0.031, 0.148, ...]
       ↓ (find nearest neighbors)
Results:
  doc_1 (0.92 similarity)
  doc_3 (0.87 similarity)
  doc_2 (0.54 similarity)
```

---

## Popular Examples

| Database     | Known For                          |
| ------------ | ---------------------------------- |
| **pgvector** | PostgreSQL extension, familiar SQL |
| **Pinecone** | Managed, serverless, easy to use   |
| **Weaviate** | Open source, multi-modal           |
| **Milvus**   | Open source, highly scalable       |
| **Qdrant**   | Rust-based, fast, filtering        |

---

## Best Use Cases

- 🤖 Retrieval-Augmented Generation (RAG)
- 🔍 Semantic search (meaning, not keywords)
- 🎯 Recommendation systems
- 🖼️ Image similarity search
- 🎵 Audio/music similarity
- ❓ Question answering systems

---

## Example (pgvector)

```sql
-- Create table with vector column
CREATE TABLE documents (
    id SERIAL PRIMARY KEY,
    content TEXT,
    embedding vector(1536)
);

-- Find similar documents
SELECT id, content,
       1 - (embedding <=> query_vec) as similarity
FROM documents
ORDER BY embedding <=> query_vec
LIMIT 5;
```

---

# 8. Wide-Column Stores

**The Distributed Giant** 🏔️

---

## What are Wide-Column Stores?

Data in **tables with rows and dynamic columns**

Each row can have **different columns**

Built for **massive scale** and **high availability**

---

## Key Characteristics

- **Column families**: Groups of related columns
- **Sparse columns**: Rows can have different columns
- **Partition keys**: Data distributed across nodes
- **Eventual consistency**: High availability, some lag
- **Linear scalability**: Add nodes to increase capacity

---

## How Data Looks

```
┌─────────────┬──────────────────────────────────────────┐
│  Row Key    │     Columns (can vary per row)           │
├─────────────┼──────────────────────────────────────────┤
│  user_123   │  login: {...}  │  purchase: {...}        │
│  user_456   │  login: {...}  │  comment: {...}         │
│  user_789   │  purchase: {...} │ purchase: {...}       │
└─────────────┴──────────────────────────────────────────┘
```

---

## Popular Examples

| Database             | Known For                                    |
| -------------------- | -------------------------------------------- |
| **Apache Cassandra** | Highly available, no single point of failure |
| **ScyllaDB**         | Cassandra-compatible, written in C++         |
| **HBase**            | Hadoop ecosystem, strong consistency         |
| **Google Bigtable**  | Managed, powers Google services              |

---

## Best Use Cases

- 📝 Write-heavy workloads
- ⏰ Time-series data at massive scale
- 🌍 Global distribution with local latency
- 📊 Analytics and big data
- 💬 Messaging and chat history
- 🎮 Gaming leaderboards

---

## Example Query (CQL)

```sql
CREATE TABLE user_activities (
    user_id UUID,
    activity_time TIMESTAMP,
    activity_type TEXT,
    details MAP<TEXT, TEXT>,
    PRIMARY KEY ((user_id), activity_time)
) WITH CLUSTERING ORDER BY (activity_time DESC);

SELECT * FROM user_activities
WHERE user_id = 550e8400-e29b-41d4-a716-446655440000
LIMIT 100;
```

---

# 9. Event Streaming Platforms

**The Message Broker** 📡

---

## What is Event Streaming?

Designed for **real-time data pipelines**

Store and process **streams of events** (messages)

Multiple consumers can read **independently**

---

## Key Characteristics

- **Append-only log**: Events are immutable
- **Topics and partitions**: Organized message streams
- **Consumer groups**: Multiple readers, each gets subset
- **Replay capability**: Re-read historical events
- **High throughput**: Millions of events per second

---

## How Data Looks

```
Topic: order_events
┌─────────────────────────────────────────────────┐
│  Partition 0: [e0] [e3] [e6] [e9] ...           │
│  Partition 1: [e1] [e4] [e7] [e10] ...          │
│  Partition 2: [e2] [e5] [e8] [e11] ...          │
└─────────────────────────────────────────────────┘

Event: { "event_type": "order_placed",
         "order_id": "ord_456",
         "total": 99.99 }
```

---

## Popular Examples

| Platform           | Known For                            |
| ------------------ | ------------------------------------ |
| **Apache Kafka**   | Industry standard, highly scalable   |
| **Amazon Kinesis** | AWS managed, serverless option       |
| **Apache Pulsar**  | Multi-tenancy, geo-replication       |
| **Redpanda**       | Kafka-compatible, simpler operations |

---

## Best Use Cases

- 🔄 Real-time data pipelines
- 📡 Event sourcing architectures
- 🔗 Microservices communication
- 📊 Stream processing and analytics
- 📝 Log aggregation
- 🔄 Change data capture (CDC)

---

## Example (Kafka)

```typescript
// Producer
await producer.send({
  topic: 'order_events',
  messages: [
    {
      key: 'user_789',
      value: JSON.stringify({
        event_type: 'order_placed',
        total: 99.99,
      }),
    },
  ],
});

// Consumer
await consumer.run({
  eachMessage: async ({ message }) => {
    const event = JSON.parse(message.value.toString());
    console.log(`Processing: ${event.event_type}`);
  },
});
```

---

# Summary: The Zoo at a Glance

---

## Comparison Table

| Type            | Primary Use             | Scale Model | Consistency     |
| --------------- | ----------------------- | ----------- | --------------- |
| **Relational**  | Transactions, reporting | Vertical    | Strong (ACID)   |
| **Document**    | Flexible content        | Horizontal  | Tunable         |
| **Key-Value**   | Caching, sessions       | Horizontal  | Eventual/Strong |
| **Graph**       | Relationships           | Vertical    | Strong          |
| **Time Series** | Metrics, IoT            | Horizontal  | Strong          |

---

## Comparison Table (continued)

| Type                | Primary Use       | Scale Model | Consistency |
| ------------------- | ----------------- | ----------- | ----------- |
| **Search Engine**   | Full-text search  | Horizontal  | Eventual    |
| **Vector**          | AI/ML, similarity | Horizontal  | Eventual    |
| **Wide-Column**     | Big data, writes  | Horizontal  | Eventual    |
| **Event Streaming** | Real-time events  | Horizontal  | Eventual    |

---

# What's Next?

Now that you understand the different database types...

You're ready to learn **when to use which one**.

Next section: **Database Decision Matrix**

> Remember: Modern applications often use **multiple databases together** - PostgreSQL for core data, Redis for caching, Elasticsearch for search, Kafka for events. This is called **polyglot persistence**.
