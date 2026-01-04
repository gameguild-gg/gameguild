# Scalability Basics

As applications grow, databases must scale to handle increased load. This lesson introduces fundamental concepts for scaling PostgreSQL.

---

## Why Scale?

Databases face pressure from multiple directions:

| Challenge | Symptoms |
|-----------|----------|
| Read-heavy load | Slow queries, high CPU |
| Write-heavy load | Lock contention, slow inserts |
| Large data volume | Slow queries, disk full |
| High availability | Downtime not acceptable |
| Geographic distribution | High latency for remote users |

---

## Scaling Strategies

### Vertical Scaling (Scale Up)

Add more resources to a single server:
- More CPU cores
- More RAM
- Faster SSDs
- Better network

**Pros:**
- Simple to implement
- No application changes needed
- No distributed complexity

**Cons:**
- Hardware limits
- Single point of failure
- Expensive at high end

### Horizontal Scaling (Scale Out)

Add more servers:
- Multiple database instances
- Distribute data and load

**Pros:**
- Nearly unlimited scaling
- Better fault tolerance
- Can use commodity hardware

**Cons:**
- Application complexity
- Distributed system challenges
- Consistency trade-offs

---

## Replication

**Replication** copies data from one database server to another.

### Primary-Replica (Master-Slave)

```
    ┌─────────────┐
    │   Primary   │ ◄── Writes
    │  (Master)   │
    └──────┬──────┘
           │
     WAL Streaming
           │
    ┌──────┴──────┐
    ▼             ▼
┌─────────┐  ┌─────────┐
│ Replica │  │ Replica │ ◄── Reads
│ (Slave) │  │ (Slave) │
└─────────┘  └─────────┘
```

- **Primary** handles all writes
- **Replicas** receive copies of changes
- **Replicas** can handle read queries

### Setting Up Streaming Replication

**On Primary:**
```sql
-- postgresql.conf
wal_level = replica
max_wal_senders = 3

-- pg_hba.conf
host replication replicator replica_ip/32 md5

-- Create replication user
CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD 'rep_pass';
```

**On Replica:**
```bash
# Clone primary
pg_basebackup -h primary_host -D /var/lib/postgresql/data -U replicator -P

# standby.signal file indicates this is a replica
touch /var/lib/postgresql/data/standby.signal
```

### Synchronous vs Asynchronous

**Asynchronous (Default):**
- Primary doesn't wait for replicas
- Better performance
- Possible data loss on primary failure

**Synchronous:**
- Primary waits for replica confirmation
- Guaranteed data on replica
- Higher latency

```sql
-- On primary, for synchronous:
synchronous_standby_names = 'replica1'
```

### Read Replicas in Applications

```javascript
// Application code distributes reads
const primaryPool = new Pool({ host: 'primary.db.com' });
const replicaPool = new Pool({ host: 'replica.db.com' });

// Writes go to primary
await primaryPool.query('INSERT INTO orders ...');

// Reads can go to replica
const products = await replicaPool.query('SELECT * FROM products');
```

---

## Partitioning

**Partitioning** splits a large table into smaller physical pieces while maintaining a single logical table.

### Why Partition?

- Faster queries (scan only relevant partitions)
- Easier maintenance (vacuum, backup by partition)
- Faster bulk deletes (drop partition vs DELETE)

### Range Partitioning

Partition by value ranges:

```sql
-- Create partitioned table
CREATE TABLE orders (
    id BIGSERIAL,
    customer_id INT,
    order_date DATE,
    total DECIMAL(10, 2)
) PARTITION BY RANGE (order_date);

-- Create partitions for each month
CREATE TABLE orders_2024_01 PARTITION OF orders
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

CREATE TABLE orders_2024_02 PARTITION OF orders
    FOR VALUES FROM ('2024-02-01') TO ('2024-03-01');

CREATE TABLE orders_2024_03 PARTITION OF orders
    FOR VALUES FROM ('2024-03-01') TO ('2024-04-01');
```

### List Partitioning

Partition by explicit values:

```sql
CREATE TABLE customers (
    id SERIAL,
    name TEXT,
    country TEXT
) PARTITION BY LIST (country);

CREATE TABLE customers_usa PARTITION OF customers
    FOR VALUES IN ('USA', 'US');

CREATE TABLE customers_europe PARTITION OF customers
    FOR VALUES IN ('UK', 'DE', 'FR', 'IT', 'ES');

CREATE TABLE customers_asia PARTITION OF customers
    FOR VALUES IN ('JP', 'CN', 'KR', 'IN');
```

### Hash Partitioning

Distribute evenly across partitions:

```sql
CREATE TABLE events (
    id BIGSERIAL,
    user_id INT,
    event_type TEXT,
    created_at TIMESTAMP
) PARTITION BY HASH (user_id);

CREATE TABLE events_0 PARTITION OF events
    FOR VALUES WITH (MODULUS 4, REMAINDER 0);

CREATE TABLE events_1 PARTITION OF events
    FOR VALUES WITH (MODULUS 4, REMAINDER 1);

CREATE TABLE events_2 PARTITION OF events
    FOR VALUES WITH (MODULUS 4, REMAINDER 2);

CREATE TABLE events_3 PARTITION OF events
    FOR VALUES WITH (MODULUS 4, REMAINDER 3);
```

### Partition Maintenance

```sql
-- Drop old data by dropping partition
DROP TABLE orders_2023_01;

-- Detach partition (keeps data, removes from table)
ALTER TABLE orders DETACH PARTITION orders_2023_02;

-- Attach existing table as partition
ALTER TABLE orders ATTACH PARTITION orders_2024_04
    FOR VALUES FROM ('2024-04-01') TO ('2024-05-01');
```

### Automatic Partition Creation

PostgreSQL doesn't auto-create partitions. Options:
1. Create partitions in advance
2. Use pg_partman extension
3. Application creates as needed

---

## Sharding

**Sharding** distributes data across multiple independent database servers.

### Sharding Concepts

```
                    Application
                         │
              ┌──────────┼──────────┐
              ▼          ▼          ▼
          ┌───────┐  ┌───────┐  ┌───────┐
          │Shard 1│  │Shard 2│  │Shard 3│
          │A-H    │  │I-P    │  │Q-Z    │
          └───────┘  └───────┘  └───────┘
```

Each shard is an independent database containing a subset of data.

### Shard Key Selection

The **shard key** determines which shard holds each row.

**Good shard keys:**
- Even distribution
- Queries mostly hit single shard
- Rarely changes

**Common shard keys:**
- User ID (for user-centric apps)
- Tenant ID (for multi-tenant SaaS)
- Geographic region

**Poor shard keys:**
- Timestamps (all new data hits one shard)
- Incrementing IDs (uneven distribution)
- Low cardinality fields

### Application-Level Sharding

```javascript
function getShard(userId) {
    const shardCount = 4;
    const shardId = userId % shardCount;
    return shards[shardId];
}

// Query the right shard
const shard = getShard(userId);
const user = await shard.query('SELECT * FROM users WHERE id = $1', [userId]);
```

### Cross-Shard Queries

Sharding makes some operations complex:

```sql
-- This is HARD with sharding:
SELECT * FROM users ORDER BY created_at LIMIT 10;

-- Need to:
-- 1. Query each shard
-- 2. Merge results
-- 3. Sort globally
-- 4. Apply limit
```

### Sharding Solutions

| Solution | Description |
|----------|-------------|
| Citus | PostgreSQL extension for distributed tables |
| Vitess | MySQL/compatible sharding (YouTube scale) |
| CockroachDB | Distributed SQL, auto-sharding |
| Application | Custom sharding logic in code |

---

## Connection Pooling

Each database connection uses memory (~10MB). Connection pooling reuses connections.

### Without Pooling

```
App Instance 1 ──┐
App Instance 2 ──┼── 300 connections ──▶ Database (overloaded)
App Instance 3 ──┘
```

### With Pooling

```
App Instance 1 ──┐                      ┌── 30 connections
App Instance 2 ──┼──▶ Connection Pool ──┤                    ──▶ Database
App Instance 3 ──┘                      └── (shared)
```

### PgBouncer

Popular connection pooler for PostgreSQL:

```ini
# pgbouncer.ini
[databases]
myapp = host=localhost port=5432 dbname=myapp

[pgbouncer]
pool_mode = transaction
max_client_conn = 1000
default_pool_size = 20
```

**Pool Modes:**
- **Session**: Connection per client session
- **Transaction**: Connection per transaction (recommended)
- **Statement**: Connection per statement (limited features)

### Application-Level Pooling

Most database drivers include pooling:

```javascript
// Node.js with pg
const { Pool } = require('pg');
const pool = new Pool({
    max: 20,              // Max connections
    idleTimeoutMillis: 30000,
    connectionTimeoutMillis: 2000,
});
```

---

## Caching

Cache frequently accessed data to reduce database load.

### Query Result Caching

```javascript
// Check cache first
const cacheKey = `product:${productId}`;
let product = await redis.get(cacheKey);

if (!product) {
    // Cache miss - query database
    product = await db.query('SELECT * FROM products WHERE id = $1', [productId]);
    
    // Store in cache
    await redis.setex(cacheKey, 3600, JSON.stringify(product));
}

return product;
```

### Cache Invalidation

```javascript
// When data changes, invalidate cache
await db.query('UPDATE products SET price = $1 WHERE id = $2', [newPrice, productId]);
await redis.del(`product:${productId}`);
```

### Caching Strategies

| Strategy | Description | Use Case |
|----------|-------------|----------|
| Cache-aside | App manages cache | General purpose |
| Read-through | Cache auto-loads on miss | Simpler code |
| Write-through | Write to cache and DB | Write-heavy |
| Write-behind | Write to cache, async to DB | High performance |

---

## Indexing for Scale

Proper indexing is often the biggest performance win.

### Identify Slow Queries

```sql
-- Enable query logging
log_min_duration_statement = 1000  -- Log queries > 1 second

-- Find slow queries (pg_stat_statements extension)
SELECT query, calls, mean_exec_time, total_exec_time
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 10;
```

### Use EXPLAIN ANALYZE

```sql
EXPLAIN ANALYZE SELECT * FROM orders WHERE customer_id = 12345;

-- Look for:
-- - Seq Scan (consider index)
-- - High "actual time"
-- - "rows removed by filter" (index not used)
```

### Covering Indexes

Include all needed columns in index:

```sql
-- Query needs id and name
SELECT id, name FROM users WHERE email = 'user@example.com';

-- Covering index avoids table lookup
CREATE INDEX idx_users_email_covering 
ON users (email) INCLUDE (id, name);
```

### Partial Indexes

Index only relevant rows:

```sql
-- Most orders are completed; we usually query active ones
CREATE INDEX idx_orders_active 
ON orders (customer_id, created_at) 
WHERE status != 'completed';
```

---

## High Availability

Ensure the database remains available during failures.

### Automatic Failover

```
     ┌─────────┐
     │ Primary │◄── Writes
     └────┬────┘
          │ Heartbeat
          ▼
     ┌─────────┐
     │ Replica │ ──▶ Becomes primary if heartbeat lost
     └─────────┘
```

### Tools for HA

| Tool | Description |
|------|-------------|
| Patroni | Template for PostgreSQL HA with etcd/ZooKeeper |
| repmgr | Replication manager with failover |
| pgpool-II | Connection pooling + failover |
| Cloud managed | AWS RDS, Azure, GCP auto-handle HA |

---

## When to Scale

### Signs You Need to Scale

| Symptom | Possible Solutions |
|---------|-------------------|
| Slow read queries | Read replicas, caching, indexing |
| Slow write queries | Vertical scaling, sharding |
| Running out of disk | Archiving, partitioning, sharding |
| Too many connections | Connection pooling |
| High availability required | Replication with failover |

### Start Simple

1. **Optimize queries first** — Indexing often solves problems
2. **Add caching** — Reduce database load
3. **Connection pooling** — Handle more connections
4. **Read replicas** — Distribute read load
5. **Partitioning** — Manage large tables
6. **Sharding** — Only when truly necessary (adds complexity)

---

## Scaling Decision Matrix

| Need | Solution | Complexity |
|------|----------|------------|
| More CPU/RAM | Vertical scaling | Low |
| More read throughput | Read replicas | Medium |
| More write throughput | Sharding | High |
| Handle large tables | Partitioning | Medium |
| More connections | Connection pooling | Low |
| Reduce latency | Caching | Medium |
| High availability | Replication + failover | Medium |
| Global distribution | Multi-region replicas | High |

---

## Practice

### Exercise 1: Read Replica Design

Design a read replica setup for an e-commerce site:
- Primary in US-East
- Replicas for EU and Asia users
- What queries go where?

### Exercise 2: Partitioning Strategy

Design a partitioning scheme for a logging table with:
- 10 million rows per day
- Queries mostly by time range
- Data kept for 90 days

### Exercise 3: Connection Pool Sizing

Calculate appropriate pool size for:
- 50 application instances
- Each handling 100 concurrent users
- Database can handle 200 connections

---

## Key Takeaways

1. **Start with vertical scaling** — simpler and often sufficient
2. **Read replicas** handle read-heavy workloads
3. **Partitioning** improves query performance on large tables
4. **Sharding** distributes data but adds complexity
5. **Connection pooling** is almost always beneficial
6. **Caching** dramatically reduces database load
7. **Proper indexing** is often the biggest win
8. **High availability** requires replication and failover
9. **Scale incrementally** — add complexity only when needed
10. **Managed databases** (RDS, Cloud SQL) simplify operations
