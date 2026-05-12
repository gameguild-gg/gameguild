# Week 11 - Key-Value & Wide-Column Stores

**Dates:** March 23-27, 2026  
**Topics:** Redis (Key-Value) & Cassandra (Wide-Column)  
**Assessment:** Quiz 9 - Key-Value & Wide-Column Stores

---

## Overview

This week explores two NoSQL database paradigms: **Redis** (in-memory key-value store) and **Cassandra** (distributed wide-column store). You'll learn when to use each, how they handle data differently from relational databases, and how to implement common patterns like caching, leaderboards, and time-series storage.

### Learning Objectives

By the end of this week, you will be able to:

1. **Explain** key-value and wide-column data models
2. **Implement** Redis data structures (strings, lists, sets, hashes, sorted sets)
3. **Design** caching strategies, rate limiters, and leaderboards with Redis
4. **Understand** the CAP theorem and Cassandra's AP model
5. **Model** data for Cassandra using partition keys and clustering keys
6. **Write** CQL queries for time-series and wide-column data
7. **Choose** appropriate databases for different use cases

---

## Weekly Schedule

### Monday, March 23 - Redis (Key-Value Store)

**Topics:**
- What is Redis? (in-memory, single-threaded, atomic operations)
- **Data Structures:**
  - Strings (SET, GET, INCR, SETEX)
  - Lists (LPUSH, RPOP, LRANGE - message queues)
  - Sets (SADD, SMEMBERS, SINTER - unique collections)
  - Hashes (HSET, HGETALL - objects)
  - Sorted Sets (ZADD, ZREVRANGE - leaderboards)
- **Features:**
  - TTL (expiration) for caching
  - Pub/Sub for real-time messaging
  - Transactions (MULTI/EXEC)
  - Lua scripting
- **Use Cases:**
  - Caching (API responses, pages)
  - Session storage
  - Rate limiting
  - Leaderboards
  - Real-time analytics

**Readings:**
- [Redis Fundamentals](./redis-fundamentals.md)

**Activities:**
- Set up Redis Docker container
- Practice Redis commands in redis-cli
- Build a caching layer with ioredis
- Implement rate limiter with Redis

---

### Thursday, March 26 - Cassandra (Wide-Column Store)

**Topics:**
- What is Cassandra? (distributed, masterless, linearly scalable)
- **Architecture:**
  - Ring topology (consistent hashing)
  - Replication (RF=3)
  - Tunable consistency (ONE, QUORUM, ALL)
- **CAP Theorem:**
  - AP (Availability + Partition Tolerance)
  - Eventual consistency
- **Data Model:**
  - Column families (tables)
  - Partition keys (determines node)
  - Clustering keys (sorts within partition)
  - Denormalization (no JOINs)
- **CQL (Cassandra Query Language):**
  - CREATE KEYSPACE, CREATE TABLE
  - INSERT, SELECT, UPDATE, DELETE
  - PRIMARY KEY (partition, clustering)
  - Query by partition key requirement
- **Use Cases:**
  - Time-series data (IoT, logs, metrics)
  - Messaging (WhatsApp-scale)
  - Product catalogs
  - Event tracking

**Readings:**
- [Cassandra Fundamentals](./cassandra-fundamentals.md)
- [Readings & Resources](./readings-11.md)

**Activities:**
- Set up Cassandra Docker container
- Practice CQL in cqlsh
- Model time-series data with partition/clustering keys
- Query with cassandra-driver in TypeScript

---

## Assessment

### Quiz 9 - Key-Value & Wide-Column Stores (Due: Thursday, March 26)

**Topics Covered:**
- Redis data structures (strings, lists, sets, hashes, sorted sets)
- Redis TTL and expiration
- Pub/Sub vs message queues
- CAP theorem (AP vs CP)
- Cassandra partition keys vs clustering keys
- Cassandra consistency levels
- Denormalization strategies
- Redis vs Cassandra use case selection

**Format:**
- 12 multiple-choice questions
- Requirement → Redis/CQL code
- Code → Description
- Use case analysis

**Preparation:**
- Complete all readings
- Practice Redis commands
- Write CQL queries
- Review quiz materials

[Take Quiz 9](./quiz/redis-cassandra-quiz.md)

---

## Final Project Milestone

**Architecture Design (Due: Sunday, March 29)**

Submit your project's **database architecture design** including:

1. **Database Selection:**
   - Which databases will you use? (PostgreSQL, MongoDB, Redis, etc.)
   - Why each database? (use case justification)

2. **Schema Designs:**
   - PostgreSQL: ER diagram + normalized schema
   - MongoDB: Document schema with embedding/referencing decisions
   - Redis: Key naming conventions + data structures

3. **Data Flow Diagrams:**
   - How data moves between databases
   - Caching strategies
   - Replication/sync patterns

4. **Scalability Plan:**
   - Read/write patterns
   - Indexing strategy
   - Partitioning/sharding approach

**Example Architectures:**

**Social Media Platform:**
- **PostgreSQL**: Users, friendships (relational)
- **MongoDB**: Posts, comments (document-oriented)
- **Redis**: Sessions, online users, news feed cache

**E-commerce Site:**
- **PostgreSQL**: Products, orders, inventory
- **MongoDB**: Product reviews, ratings
- **Redis**: Shopping cart, product view cache, flash sale counters

**IoT Dashboard:**
- **PostgreSQL**: Device metadata, user accounts
- **Cassandra**: Time-series sensor data
- **Redis**: Real-time metrics, leaderboards

---

## Weekly Content

### Required Readings

1. **[Redis Fundamentals](./redis-fundamentals.md)** (90 min)
   - Data structures, TTL, Pub/Sub, transactions, ioredis

2. **[Cassandra Fundamentals](./cassandra-fundamentals.md)** (120 min)
   - Architecture, CAP theorem, CQL, partition/clustering keys, cassandra-driver

### Supplemental Resources

3. **[Readings & Resources](./readings-11.md)** (30 min)
   - Official documentation, tutorials, tools, cheat sheets

---

## Key Concepts

### Redis (Key-Value)

#### Data Structures

| Structure | Use Case | Key Commands |
|-----------|----------|--------------|
| **String** | Caching, counters | SET, GET, INCR, SETEX |
| **List** | Message queues, feeds | LPUSH, RPOP, LRANGE, BRPOP |
| **Set** | Unique visitors, tags | SADD, SMEMBERS, SINTER |
| **Hash** | User profiles, objects | HSET, HGETALL, HINCRBY |
| **Sorted Set** | Leaderboards, rankings | ZADD, ZREVRANGE, ZRANK |

#### TTL (Time-To-Live)

```bash
# Set expiration (10 seconds)
SETEX cache:page:home 10 "<html>...</html>"

# Check TTL
TTL cache:page:home  # Returns: 8 (seconds remaining)

# Remove expiration
PERSIST cache:page:home
```

#### Common Patterns

**Caching:**
```typescript
const cached = await redis.get(`cache:user:${id}`);
if (cached) return JSON.parse(cached);

const user = await db.users.findOne({ id });
await redis.setex(`cache:user:${id}`, 300, JSON.stringify(user));
return user;
```

**Rate Limiting:**
```typescript
const key = `rate:${userId}:${Math.floor(Date.now() / 60000)}`;
const count = await redis.incr(key);
if (count === 1) await redis.expire(key, 60);
return count > 100; // Max 100/minute
```

**Leaderboard:**
```typescript
await redis.zadd('leaderboard', score, userId);
const top10 = await redis.zrevrange('leaderboard', 0, 9, 'WITHSCORES');
```

---

### Cassandra (Wide-Column)

#### CAP Theorem

Cassandra is **AP** (Availability + Partition Tolerance):

- **Eventual consistency** by default
- **Tunable consistency** to act like CP when needed
- System stays available during network partitions

**Consistency Levels:**

```sql
-- Strong consistency
SELECT * FROM users WHERE user_id = ?
USING CONSISTENCY QUORUM;  -- RF/2 + 1

-- Weak consistency (faster)
SELECT * FROM users WHERE user_id = ?
USING CONSISTENCY ONE;
```

#### Primary Keys

```sql
-- Simple primary key (partition key only)
CREATE TABLE users (
  user_id UUID PRIMARY KEY,  -- Partition key
  username TEXT
);

-- Composite primary key (partition + clustering)
CREATE TABLE posts (
  user_id UUID,        -- Partition key (which node)
  post_id TIMEUUID,    -- Clustering key (sort order)
  title TEXT,
  PRIMARY KEY (user_id, post_id)
)
WITH CLUSTERING ORDER BY (post_id DESC);  -- Newest first
```

#### Query Rules

```sql
-- ✅ Always query by partition key
SELECT * FROM posts WHERE user_id = ?;

-- ❌ Cannot query without partition key (full table scan)
SELECT * FROM posts WHERE title = 'Hello';
-- Error: "Cannot execute this query as it might involve data filtering"

-- ✅ Add clustering key conditions
SELECT * FROM posts
WHERE user_id = ? AND post_id > ?;
```

#### Denormalization

Cassandra has **NO JOINs**. Create one table per query pattern:

```sql
-- Query 1: Get posts by user
CREATE TABLE posts_by_user (
  user_id UUID,
  post_id UUID,
  username TEXT,  -- Denormalized!
  title TEXT,
  PRIMARY KEY (user_id, post_id)
);

-- Query 2: Get post by ID
CREATE TABLE posts_by_id (
  post_id UUID PRIMARY KEY,
  user_id UUID,
  username TEXT,  -- Denormalized!
  title TEXT
);
```

---

## Practical Exercises

### Exercise 1: Redis Leaderboard

Implement a gaming leaderboard:

1. Update player scores: `ZADD leaderboard score player_id`
2. Get top 10 players: `ZREVRANGE leaderboard 0 9 WITHSCORES`
3. Get player rank: `ZREVRANK leaderboard player_id`
4. Increment score: `ZINCRBY leaderboard 50 player_id`

### Exercise 2: Redis Rate Limiter

Implement a 100 requests/minute rate limiter:

```typescript
async function isRateLimited(userId: string): Promise<boolean> {
  const key = `rate:${userId}:${Math.floor(Date.now() / 60000)}`;
  const count = await redis.incr(key);
  if (count === 1) {
    await redis.expire(key, 60);  // Auto-delete after 1 minute
  }
  return count > 100;
}
```

### Exercise 3: Cassandra Time-Series

Model IoT sensor data:

```sql
CREATE TABLE sensor_data (
  sensor_id TEXT,
  timestamp TIMESTAMP,
  temperature FLOAT,
  humidity FLOAT,
  PRIMARY KEY (sensor_id, timestamp)
)
WITH CLUSTERING ORDER BY (timestamp DESC);

-- Insert reading
INSERT INTO sensor_data (sensor_id, timestamp, temperature, humidity)
VALUES ('sensor-001', toTimestamp(now()), 22.5, 65.0);

-- Get last 10 readings
SELECT * FROM sensor_data
WHERE sensor_id = 'sensor-001'
LIMIT 10;
```

### Exercise 4: Cassandra Denormalization

Design schema for blog with two queries:
1. Get all posts by user
2. Get specific post by post_id

Create two tables (one per query).

---

## Common Pitfalls

### ❌ Redis: Forgetting TTL on Cache Keys

**Problem:** Cache grows forever without expiration

```bash
# ❌ Bad
SET cache:page:home "<html>..."

# ✅ Good
SETEX cache:page:home 300 "<html>..."
```

### ❌ Redis: Using SET After EXPIRE

**Problem:** `SET` removes TTL

```bash
SET session:abc "data"
EXPIRE session:abc 3600

# 1 hour later...
SET session:abc "updated_data"  # TTL removed!

# ✅ Solution
SETEX session:abc 3600 "updated_data"
```

### ❌ Cassandra: Querying Without Partition Key

**Problem:** Full table scan (extremely slow)

```sql
-- ❌ Bad
SELECT * FROM posts WHERE title = 'Hello';

-- ✅ Good
SELECT * FROM posts WHERE user_id = ? AND title = 'Hello';
```

### ❌ Cassandra: Large Partitions

**Problem:** Partition > 100MB causes hotspots

```sql
-- ❌ Bad: All user events in one partition
PRIMARY KEY (user_id, event_id)

-- ✅ Good: Partition by user + month
PRIMARY KEY ((user_id, month), event_id)
```

---

## Tools & Setup

### Redis Docker

```yaml
# docker-compose.yml
version: '3.8'
services:
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
```

```bash
# Start Redis
docker-compose up -d

# Connect to redis-cli
docker exec -it redis redis-cli

# Test
redis-cli ping  # Returns: PONG
```

### Cassandra Docker

```yaml
# docker-compose.yml
version: '3.8'
services:
  cassandra:
    image: cassandra:5
    ports:
      - "9042:9042"
    environment:
      - CASSANDRA_CLUSTER_NAME=MyCluster
```

```bash
# Start Cassandra
docker-compose up -d

# Wait 30 seconds for startup...

# Connect to cqlsh
docker exec -it cassandra cqlsh

# Test
cqlsh> DESCRIBE KEYSPACES;
```

---

## Decision Matrix: Redis vs Cassandra

| Requirement | Redis | Cassandra |
|------------|-------|-----------|
| **Data Size** | < 100GB (fits in RAM) | > 1TB (petabyte-scale) |
| **Latency** | Sub-millisecond | 1-10ms |
| **Durability** | Optional (AOF/RDB) | Always (disk-based) |
| **Scalability** | Vertical (more RAM) | Horizontal (add nodes) |
| **Use Cases** | Cache, sessions, leaderboards | Time-series, messaging, catalogs |
| **Consistency** | Strong (single-node) | Tunable (eventual → strong) |
| **Transactions** | MULTI/EXEC | Lightweight (IF conditions) |
| **Query Flexibility** | Key-based only | CQL with partition key |

---

## Next Steps

After completing this week's content:

1. ✅ **Complete Quiz 9** on Redis and Cassandra
2. ✅ **Submit architecture design** for final project
3. ✅ **Experiment** with Redis and Cassandra in your project
4. 📚 **Preview Week 12** (Graph Databases: Neo4j)

---

**Questions or feedback?** Post in the course discussion forum or office hours.

**Happy coding! 🚀**
