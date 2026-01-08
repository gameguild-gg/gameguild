# Cassandra Fundamentals — Wide-Column Store

## What is Cassandra?

**Apache Cassandra** is a distributed wide-column NoSQL database designed for massive scalability, high availability, and fault tolerance. Originally developed at Facebook, it powers systems handling petabytes of data across thousands of servers.

### Key Characteristics

- **Distributed & Decentralized**: No single point of failure (masterless architecture)
- **Linearly Scalable**: Add nodes to increase throughput
- **High Availability**: Replicates data across multiple nodes/datacenters
- **Tunable Consistency**: Choose between consistency and availability (CAP theorem)
- **Wide-Column Storage**: Flexible schema with column families
- **Write-Optimized**: Extremely fast writes (append-only log structure)

### Architecture Highlights

```
┌─────────────────────────────────────────┐
│          Cassandra Cluster              │
│                                         │
│   Node 1    Node 2    Node 3    Node 4 │
│   ┌───┐    ┌───┐    ┌───┐    ┌───┐   │
│   │   │◄──►│   │◄──►│   │◄──►│   │   │
│   └───┘    └───┘    └───┘    └───┘   │
│                                         │
│  - No master/slave                      │
│  - Peer-to-peer gossip protocol         │
│  - Consistent hashing (token ring)      │
│  - Replication factor (RF=3)            │
└─────────────────────────────────────────┘
```

**Key Concepts:**

- **Node**: Single Cassandra instance (server)
- **Cluster**: Collection of nodes
- **Ring**: Nodes arranged in a logical ring (consistent hashing)
- **Token**: Each node owns a range of data based on partition key hash
- **Replication Factor (RF)**: Number of copies of data (e.g., RF=3 means 3 copies)
- **Consistency Level**: How many replicas must respond for read/write

---

## Use Cases

| Use Case | Why Cassandra? |
|----------|---------------|
| **Time-Series Data** | IoT sensors, logs, metrics (efficient writes) |
| **Messaging** | WhatsApp, Discord (horizontal scaling) |
| **Product Catalogs** | E-commerce (fast reads, flexible schema) |
| **Fraud Detection** | Real-time transaction analysis |
| **Recommendation Engines** | Netflix, Spotify (distributed processing) |
| **Event Tracking** | User activity, clickstreams |

**When NOT to use Cassandra:**

- ❌ ACID transactions across rows
- ❌ Complex joins (Cassandra has NO joins)
- ❌ Ad-hoc queries (must query by partition key)
- ❌ Small datasets (< 100GB) — overhead not worth it

---

## CAP Theorem

The **CAP theorem** states that a distributed database can only guarantee **2 out of 3**:

- **C**onsistency: All nodes see the same data at the same time
- **A**vailability: Every request receives a response (success or failure)
- **P**artition Tolerance: System continues despite network failures

### Cassandra's Approach: AP (Availability + Partition Tolerance)

Cassandra prioritizes **availability** and **partition tolerance** over strong consistency. However, it offers **tunable consistency** to balance between C and A.

```
Consistency Level (CL)     | Consistency | Availability
---------------------------|-------------|-------------
ONE                        | Low         | High
QUORUM (RF/2 + 1)          | Medium      | Medium
ALL                        | High        | Low
```

**Example:**

- **RF = 3** (data replicated on 3 nodes)
- **Write CL = QUORUM** (2/3 nodes must acknowledge)
- **Read CL = QUORUM** (2/3 nodes must respond)
- **Result**: Strong consistency (if write QUORUM + read QUORUM ≥ RF + 1)

**Eventual Consistency:**

If you use `CL = ONE` for writes, replicas may be temporarily out of sync. Cassandra uses **anti-entropy repair** and **read repair** to eventually sync replicas.

---

## Data Model

### Column Families (Tables)

Cassandra organizes data into **column families** (similar to tables in SQL, but more flexible).

**Wide-Column Model:**

```
Row Key: user123
┌──────────────┬────────┬────────┬────────┬────────┬────────┐
│ Column Name  │ name   │ email  │ age    │ city   │ ...    │
├──────────────┼────────┼────────┼────────┼────────┼────────┤
│ Value        │ Alice  │ a@...  │ 28     │ Boston │        │
└──────────────┴────────┴────────┴────────┴────────┴────────┘

Row Key: user456
┌──────────────┬────────┬────────┬────────┬────────┐
│ Column Name  │ name   │ email  │ country│        │
├──────────────┼────────┼────────┼────────┼────────┤
│ Value        │ Bob    │ b@...  │ USA    │        │
└──────────────┴────────┴────────┴────────┴────────┘
```

**Key difference from SQL:** Rows can have **different columns** (schema-less per row).

---

## CQL (Cassandra Query Language)

CQL looks similar to SQL, but with important differences.

### Create Keyspace (Database)

```sql
-- Keyspace = database
CREATE KEYSPACE my_app
WITH replication = {
  'class': 'SimpleStrategy',  -- Use SimpleStrategy for single DC
  'replication_factor': 3     -- 3 copies of data
};

-- For multi-datacenter:
CREATE KEYSPACE my_app
WITH replication = {
  'class': 'NetworkTopologyStrategy',
  'datacenter1': 3,
  'datacenter2': 2
};

-- Use keyspace
USE my_app;
```

### Create Table

```sql
CREATE TABLE users (
  user_id UUID PRIMARY KEY,
  username TEXT,
  email TEXT,
  created_at TIMESTAMP
);

-- Composite primary key (partition key + clustering key)
CREATE TABLE posts (
  user_id UUID,              -- Partition key (determines which node)
  post_id TIMEUUID,          -- Clustering key (sorts within partition)
  title TEXT,
  content TEXT,
  created_at TIMESTAMP,
  PRIMARY KEY (user_id, post_id)  -- (partition, clustering)
)
WITH CLUSTERING ORDER BY (post_id DESC);  -- Newest first
```

**Key Concepts:**

- **Partition Key**: Determines which node stores the data (via hash)
- **Clustering Key**: Sorts data **within a partition**
- **Composite Key**: `PRIMARY KEY ((part1, part2), cluster1, cluster2)`

### Insert Data

```sql
-- Insert user
INSERT INTO users (user_id, username, email, created_at)
VALUES (uuid(), 'alice', 'alice@example.com', toTimestamp(now()));

-- Insert with TTL (auto-delete after 3600 seconds)
INSERT INTO users (user_id, username, email)
VALUES (uuid(), 'temp_user', 'temp@example.com')
USING TTL 3600;

-- Upsert (INSERT overwrites existing data)
INSERT INTO users (user_id, username) VALUES (..., 'alice_updated');
```

### Query Data

```sql
-- ✅ Query by partition key (fast)
SELECT * FROM posts WHERE user_id = 123e4567-e89b-12d3-a456-426614174000;

-- ✅ Query with clustering key
SELECT * FROM posts
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000
  AND post_id > minTimeuuid('2026-01-01 00:00:00');

-- ❌ Query without partition key (SLOW - full table scan)
SELECT * FROM posts WHERE title = 'Hello';
-- Error: "Cannot execute this query as it might involve data filtering"

-- ✅ Allow filtering (not recommended in production)
SELECT * FROM posts WHERE title = 'Hello' ALLOW FILTERING;

-- Limit results
SELECT * FROM posts LIMIT 10;

-- Order by clustering key (automatic within partition)
SELECT * FROM posts
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000
ORDER BY post_id DESC;
```

**Important:**

- **Always query by partition key** to avoid full table scans
- Cassandra **does NOT support JOINs** — denormalize your data
- `ORDER BY` only works on clustering keys (data already sorted)

### Update Data

```sql
-- Update specific fields
UPDATE users
SET email = 'newemail@example.com'
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000;

-- Increment counter
CREATE TABLE post_stats (
  post_id UUID PRIMARY KEY,
  views COUNTER
);

UPDATE post_stats SET views = views + 1
WHERE post_id = 123e4567-e89b-12d3-a456-426614174000;

-- Add to collection (set)
ALTER TABLE users ADD tags SET<TEXT>;

UPDATE users SET tags = tags + {'developer', 'blogger'}
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000;
```

### Delete Data

```sql
-- Delete row
DELETE FROM users WHERE user_id = 123e4567-e89b-12d3-a456-426614174000;

-- Delete specific column
DELETE email FROM users WHERE user_id = ...;

-- Tombstone: Cassandra marks data as deleted (doesn't delete immediately)
-- Actual deletion happens during compaction
```

---

## Data Types

### Primitive Types

```sql
TEXT, VARCHAR          -- Strings
INT, BIGINT, SMALLINT  -- Integers
FLOAT, DOUBLE          -- Floating point
DECIMAL                -- Exact decimal
BOOLEAN                -- true/false
UUID, TIMEUUID         -- Unique identifiers
TIMESTAMP              -- Date/time
BLOB                   -- Binary data
```

### Collections

```sql
-- List (ordered, allows duplicates)
CREATE TABLE users (
  user_id UUID PRIMARY KEY,
  phone_numbers LIST<TEXT>
);

INSERT INTO users (user_id, phone_numbers)
VALUES (uuid(), ['555-1234', '555-5678']);

-- Set (unordered, unique)
CREATE TABLE users (
  user_id UUID PRIMARY KEY,
  tags SET<TEXT>
);

-- Map (key-value pairs)
CREATE TABLE users (
  user_id UUID PRIMARY KEY,
  attributes MAP<TEXT, TEXT>
);

INSERT INTO users (user_id, attributes)
VALUES (uuid(), {'country': 'USA', 'city': 'Boston'});
```

**Warning:** Collections are stored **inside the row**. Don't store thousands of items (causes bloat).

---

## Partition Keys & Clustering Keys

### Partition Key

Determines **which node** stores the data (via consistent hashing).

```sql
CREATE TABLE users (
  user_id UUID PRIMARY KEY,  -- Partition key
  username TEXT
);

-- Data for user_id=X goes to Node A
-- Data for user_id=Y goes to Node B
```

### Clustering Key

Sorts data **within a partition**.

```sql
CREATE TABLE sensor_data (
  sensor_id TEXT,        -- Partition key
  timestamp TIMESTAMP,   -- Clustering key
  temperature FLOAT,
  PRIMARY KEY (sensor_id, timestamp)
)
WITH CLUSTERING ORDER BY (timestamp DESC);  -- Newest first

-- All data for sensor_id="sensor1" is stored together, sorted by timestamp
-- Query: SELECT * FROM sensor_data WHERE sensor_id = 'sensor1' LIMIT 10;
-- Returns: Last 10 readings (newest first)
```

### Composite Partition Key

Distribute data across **multiple keys**.

```sql
CREATE TABLE events (
  user_id UUID,
  event_date DATE,
  event_id TIMEUUID,
  event_type TEXT,
  PRIMARY KEY ((user_id, event_date), event_id)
);

-- Partition key: (user_id, event_date) — hash of both
-- Clustering key: event_id
-- Better distribution: user events split by date
```

---

## Indexes

### Secondary Index

Query by **non-partition-key columns** (limited use).

```sql
CREATE INDEX ON users (email);

-- Now you can query:
SELECT * FROM users WHERE email = 'alice@example.com';
```

**Limitations:**

- ❌ Secondary indexes are **slow** (query all nodes)
- ❌ Not recommended for high-cardinality columns (e.g., UUIDs)
- ✅ Use only for **low-cardinality** columns (e.g., status, category)

**Better approach:** Denormalize data into a separate table.

```sql
-- Instead of secondary index on email:
CREATE TABLE users_by_email (
  email TEXT PRIMARY KEY,
  user_id UUID
);

-- Query:
SELECT user_id FROM users_by_email WHERE email = 'alice@example.com';
-- Then fetch full user data from users table
```

---

## Denormalization

Cassandra **does NOT support JOINs**. You must **denormalize** data.

### Example: Blog Posts with Authors

**SQL approach (normalized):**

```sql
-- users table
user_id | username | email

-- posts table
post_id | user_id | title | content

-- Query with JOIN:
SELECT posts.*, users.username
FROM posts
JOIN users ON posts.user_id = users.user_id
WHERE posts.post_id = 123;
```

**Cassandra approach (denormalized):**

```sql
-- posts table (embed author info)
CREATE TABLE posts (
  post_id UUID PRIMARY KEY,
  user_id UUID,
  username TEXT,        -- Denormalized!
  email TEXT,           -- Denormalized!
  title TEXT,
  content TEXT
);

-- No JOIN needed:
SELECT * FROM posts WHERE post_id = 123;
-- Returns post + author info in one query
```

**Trade-off:**

- ✅ **Faster reads** (no JOINs)
- ❌ **Data duplication** (username stored in every post)
- ❌ **Update complexity** (if username changes, update all posts)

**Solution:** Accept eventual consistency or use application-level updates.

---

## Docker Setup

### docker-compose.yml

```yaml
version: '3.8'

services:
  cassandra:
    image: cassandra:5
    container_name: cassandra
    ports:
      - "9042:9042"  # CQL port
    environment:
      - CASSANDRA_CLUSTER_NAME=MyCluster
      - CASSANDRA_DC=datacenter1
      - CASSANDRA_RACK=rack1
    volumes:
      - cassandra-data:/var/lib/cassandra
    healthcheck:
      test: ["CMD-SHELL", "cqlsh -e 'describe cluster'"]
      interval: 30s
      timeout: 10s
      retries: 5

volumes:
  cassandra-data:
```

```bash
# Start Cassandra
docker-compose up -d

# Wait for startup (~30 seconds)
docker logs -f cassandra

# Connect to CQL shell
docker exec -it cassandra cqlsh

# Test
cqlsh> DESCRIBE KEYSPACES;
```

### Multi-Node Cluster (3 nodes)

```yaml
version: '3.8'

services:
  cassandra-1:
    image: cassandra:5
    environment:
      - CASSANDRA_SEEDS=cassandra-1
      - CASSANDRA_CLUSTER_NAME=MyCluster
    ports:
      - "9042:9042"

  cassandra-2:
    image: cassandra:5
    environment:
      - CASSANDRA_SEEDS=cassandra-1
      - CASSANDRA_CLUSTER_NAME=MyCluster
    depends_on:
      - cassandra-1

  cassandra-3:
    image: cassandra:5
    environment:
      - CASSANDRA_SEEDS=cassandra-1
      - CASSANDRA_CLUSTER_NAME=MyCluster
    depends_on:
      - cassandra-1
```

```bash
# Check cluster status
docker exec -it cassandra-1 nodetool status

# Output:
# UN  172.18.0.2  cassandra-1
# UN  172.18.0.3  cassandra-2
# UN  172.18.0.4  cassandra-3
# UN = Up Normal
```

---

## TypeScript Integration (cassandra-driver)

### Installation

```bash
npm install cassandra-driver
npm install -D @types/cassandra-driver
```

### Basic Usage

```typescript
import { Client } from 'cassandra-driver';

// Connect to Cassandra
const client = new Client({
  contactPoints: ['localhost'],
  localDataCenter: 'datacenter1',
  keyspace: 'my_app'
});

await client.connect();
console.log('Connected to Cassandra');

// Execute query
const result = await client.execute(
  'SELECT * FROM users WHERE user_id = ?',
  [userId],
  { prepare: true }  // Use prepared statements (faster, safer)
);

console.log(result.rows);

// Insert data
await client.execute(
  'INSERT INTO users (user_id, username, email) VALUES (?, ?, ?)',
  [uuid(), 'alice', 'alice@example.com'],
  { prepare: true }
);

// Batch insert
const queries = users.map(user => ({
  query: 'INSERT INTO users (user_id, username, email) VALUES (?, ?, ?)',
  params: [uuid(), user.username, user.email]
}));

await client.batch(queries, { prepare: true });

// Shutdown
await client.shutdown();
```

### Practical Example: Time-Series Data

```typescript
import { Client, types } from 'cassandra-driver';

const client = new Client({
  contactPoints: ['localhost'],
  localDataCenter: 'datacenter1'
});

await client.connect();

// Create keyspace and table
await client.execute(`
  CREATE KEYSPACE IF NOT EXISTS iot
  WITH replication = {
    'class': 'SimpleStrategy',
    'replication_factor': 1
  }
`);

await client.execute(`
  CREATE TABLE IF NOT EXISTS iot.sensor_data (
    sensor_id TEXT,
    timestamp TIMESTAMP,
    temperature FLOAT,
    humidity FLOAT,
    PRIMARY KEY (sensor_id, timestamp)
  )
  WITH CLUSTERING ORDER BY (timestamp DESC)
`);

// Insert sensor reading
async function recordReading(sensorId: string, temp: number, humidity: number) {
  await client.execute(
    'INSERT INTO iot.sensor_data (sensor_id, timestamp, temperature, humidity) VALUES (?, ?, ?, ?)',
    [sensorId, new Date(), temp, humidity],
    { prepare: true }
  );
}

// Get last 10 readings for sensor
async function getRecentReadings(sensorId: string) {
  const result = await client.execute(
    'SELECT * FROM iot.sensor_data WHERE sensor_id = ? LIMIT 10',
    [sensorId],
    { prepare: true }
  );
  
  return result.rows.map(row => ({
    timestamp: row.timestamp,
    temperature: row.temperature,
    humidity: row.humidity
  }));
}

// Usage
await recordReading('sensor-001', 22.5, 65.2);
await recordReading('sensor-001', 23.1, 64.8);

const readings = await getRecentReadings('sensor-001');
console.log(readings);
```

---

## Best Practices

### 1. Design for Your Queries

Cassandra requires **query-first** design.

```
❌ Don't: Design normalized schema, then write queries
✅ Do: List queries first, then design tables to support them
```

**Example:**

Queries needed:
1. Get user by ID
2. Get all posts by user
3. Get post by ID

**Tables:**

```sql
-- Query 1: Get user by ID
CREATE TABLE users (
  user_id UUID PRIMARY KEY,
  username TEXT,
  email TEXT
);

-- Query 2: Get all posts by user
CREATE TABLE posts_by_user (
  user_id UUID,
  post_id TIMEUUID,
  title TEXT,
  content TEXT,
  PRIMARY KEY (user_id, post_id)
);

-- Query 3: Get post by ID
CREATE TABLE posts_by_id (
  post_id UUID PRIMARY KEY,
  user_id UUID,
  title TEXT,
  content TEXT
);
```

### 2. Denormalize Aggressively

Don't fear data duplication. **Reads are more important than writes** in Cassandra.

### 3. Use Appropriate Consistency Levels

```typescript
// Strong consistency (slower)
await client.execute(query, params, {
  consistency: types.consistencies.quorum
});

// Weak consistency (faster)
await client.execute(query, params, {
  consistency: types.consistencies.one
});
```

### 4. Avoid Large Partitions

Keep partitions **under 100MB**. Use composite partition keys or bucketing.

```sql
-- ❌ Bad: All events for user in one partition (could be huge)
CREATE TABLE events (
  user_id UUID,
  event_id TIMEUUID,
  PRIMARY KEY (user_id, event_id)
);

-- ✅ Good: Partition by user + month
CREATE TABLE events (
  user_id UUID,
  month TEXT,  -- '2026-03'
  event_id TIMEUUID,
  PRIMARY KEY ((user_id, month), event_id)
);
```

### 5. Use TTL for Expiring Data

```sql
-- Auto-delete after 30 days
INSERT INTO sessions (session_id, data)
VALUES (uuid(), '...')
USING TTL 2592000;  -- 30 days in seconds
```

---

## Key Takeaways

- **Cassandra** is a distributed wide-column NoSQL database
- **CAP theorem**: Cassandra chooses **AP** (availability + partition tolerance)
- **Tunable consistency**: Balance between consistency and availability
- **No JOINs**: Must denormalize data
- **Query by partition key**: Full table scans are inefficient
- **Partition key** determines node, **clustering key** sorts within partition
- **Use cases**: Time-series, messaging, catalogs, event tracking
- **CQL**: SQL-like syntax with important differences

---

**Next:** [Week 11 Quiz](./quiz/redis-cassandra-quiz.md)
