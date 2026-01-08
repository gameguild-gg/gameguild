# Quiz 9: Key-Value & Wide-Column Stores

## Instructions

This quiz tests your understanding of Redis (key-value store) and Cassandra (wide-column store), including data structures, use cases, and query patterns.

---

## Question 1 — Redis Data Structure Selection

**Scenario:** You're building a real-time leaderboard for a gaming app where players' scores change frequently. You need to:
- Update scores quickly
- Get top 10 players instantly
- Find a player's rank efficiently

**Which Redis data structure is BEST?**

- [ ] A. String (store JSON of all players)
- [ ] B. List (sorted by score)
- [ ] C. Set (unique player IDs)
- [ ] D. Sorted Set (Z

SET)

**Explanation:**

- **D is CORRECT** ✅
  - **Sorted Set** stores members with scores
  - `ZADD leaderboard 1000 "alice"` — O(log N) update
  - `ZREVRANGE leaderboard 0 9` — Get top 10
  - `ZREVRANK leaderboard "alice"` — Get rank
  - Automatically sorted by score
  
- **A is wrong** ❌
  - Storing JSON requires parsing entire structure
  - No efficient ranking or sorting
  
- **B is wrong** ❌
  - Lists don't maintain sort order automatically
  - Finding position is O(N)
  
- **C is wrong** ❌
  - Sets have no concept of scores or ordering

**Key takeaway:** Use **Sorted Sets** for rankings, leaderboards, and time-series with scores.

---

## Question 2 — Requirement → Redis Commands

**Requirement:** Implement a rate limiter that allows **100 requests per minute** per user. After 1 minute, the counter should reset automatically.

**Which Redis commands correctly implement this?**

- [ ] A.
```bash
INCR rate:user:1000
EXPIRE rate:user:1000 60
# Check if > 100
```

- [ ] B.
```bash
SET rate:user:1000 1 EX 60
INCR rate:user:1000
# Check if > 100
```

- [ ] C.
```bash
key = "rate:user:1000:" + current_minute
INCR key
if first_time:
  EXPIRE key 60
# Check if > 100
```

- [ ] D.
```bash
ZADD rate:user:1000 timestamp request_id
ZREMRANGEBYSCORE rate:user:1000 0 (now - 60)
ZCARD rate:user:1000
# Check if > 100
```

**Explanation:**

- **C is CORRECT** ✅
  - **Fixed window** approach with minute-based key
  - Example: `rate:user:1000:2026-03-23-10-45` (minute granularity)
  - `INCR` increments counter
  - `EXPIRE` on first request sets auto-deletion after 60s
  - Simple and efficient
  
- **A is wrong** ❌
  - Race condition: `EXPIRE` might not run if `INCR` fails
  - If `EXPIRE` is called after each `INCR`, TTL resets every request
  
- **B is wrong** ❌
  - `SET` overwrites value (loses count)
  
- **D is technically correct** ✅ but more complex
  - **Sliding window** approach (more accurate)
  - Stores each request with timestamp in sorted set
  - Removes old requests, counts remaining
  - More expensive (stores every request)

**Key takeaway:** Use **fixed window** (key per minute + INCR + EXPIRE) for simple rate limiting.

---

## Question 3 — Redis Pub/Sub vs Lists

**Scenario:** You need a message queue where:
- Producers send tasks
- Multiple workers process tasks
- Each task should be processed **exactly once**

**Which Redis approach is BEST?**

- [ ] A. Pub/Sub (PUBLISH/SUBSCRIBE)
- [ ] B. List (LPUSH/RPOP)
- [ ] C. List with blocking pop (LPUSH/BRPOP)
- [ ] D. Sorted Set (ZADD/ZPOPMIN)

**Explanation:**

- **C is CORRECT** ✅
  - **Producer**: `LPUSH tasks "task1"`
  - **Worker**: `BRPOP tasks 5` (blocks up to 5 seconds waiting for task)
  - Guarantees **exactly-once** delivery (task removed from list when popped)
  - Multiple workers can consume from same list
  
- **A is wrong** ❌
  - Pub/Sub is **fire-and-forget** (no persistence)
  - If subscriber is offline, message is lost
  - No acknowledgment or retry mechanism
  
- **B is almost correct** ⚠️
  - Works, but `RPOP` returns `nil` immediately if list is empty
  - Workers must poll continuously (wasteful)
  
- **D is wrong** ❌
  - Sorted sets are for priority queues, not simple FIFO
  - More complex than needed

**Key takeaway:** Use **Lists with BRPOP** for reliable task queues (blocks until task available).

---

## Question 4 — Redis TTL Behavior

**Given these commands:**

```bash
SET session:abc "user_data"
EXPIRE session:abc 60

# 30 seconds later...
GET session:abc
SET session:abc "updated_data"

# What is the TTL now?
TTL session:abc
```

**What does `TTL session:abc` return?**

- [ ] A. 30 (30 seconds remaining)
- [ ] B. 60 (TTL reset to 60)
- [ ] C. -1 (no expiration)
- [ ] D. -2 (key doesn't exist)

**Explanation:**

- **C is CORRECT** ✅
  - `SET` **removes the TTL** (resets to no expiration)
  - Original: `SET` + `EXPIRE` → 60s TTL
  - After 30s: `GET` (doesn't affect TTL)
  - After `SET`: TTL removed
  
- **A is wrong** ❌
  - `SET` doesn't preserve TTL
  
- **B is wrong** ❌
  - TTL doesn't reset to 60; it's removed entirely

**To preserve TTL:**

```bash
# Option 1: Use SETEX
SETEX session:abc 60 "updated_data"

# Option 2: Re-apply EXPIRE
SET session:abc "updated_data"
EXPIRE session:abc 60

# Option 3: Use GETEX (Redis 6.2+)
SET session:abc "updated_data" KEEPTTL
```

**Key takeaway:** `SET` **removes TTL**. Use `SETEX` or `KEEPTTL` to preserve expiration.

---

## Question 5 — Cassandra CAP Theorem

**Cassandra is classified as which CAP model?**

- [ ] A. CA (Consistency + Availability) — sacrifices partition tolerance
- [ ] B. CP (Consistency + Partition Tolerance) — sacrifices availability
- [ ] C. AP (Availability + Partition Tolerance) — sacrifices consistency
- [ ] D. CAP (all three guaranteed)

**Explanation:**

- **C is CORRECT** ✅
  - Cassandra is **AP** (prioritizes availability and partition tolerance)
  - **Eventually consistent** by default
  - System stays available even during network partitions
  - Uses **tunable consistency** to balance C and A
  
- **A is wrong** ❌
  - CA systems can't handle network partitions (e.g., traditional RDBMS in single datacenter)
  - Cassandra is distributed across datacenters
  
- **B is wrong** ❌
  - CP systems (like HBase, MongoDB with majority) sacrifice availability during partitions
  - Cassandra continues serving requests even if some nodes are down
  
- **D is wrong** ❌
  - **CAP theorem states you can only pick 2 out of 3**

**Tunable Consistency Example:**

```sql
-- Strong consistency (acts like CP)
SELECT * FROM users WHERE user_id = ?
USING CONSISTENCY QUORUM;

-- Weak consistency (pure AP)
SELECT * FROM users WHERE user_id = ?
USING CONSISTENCY ONE;
```

**Key takeaway:** Cassandra is **AP** but offers **tunable consistency** to act like CP when needed.

---

## Question 6 — Cassandra Primary Key

**Given this table:**

```sql
CREATE TABLE posts (
  user_id UUID,
  post_id TIMEUUID,
  title TEXT,
  content TEXT,
  PRIMARY KEY (user_id, post_id)
);
```

**Which statement is TRUE?**

- [ ] A. `user_id` is the clustering key, `post_id` is the partition key
- [ ] B. `user_id` is the partition key, `post_id` is the clustering key
- [ ] C. Both `user_id` and `post_id` are partition keys
- [ ] D. `(user_id, post_id)` is a composite partition key

**Explanation:**

- **B is CORRECT** ✅
  - **Partition Key**: `user_id` (determines which node stores data)
  - **Clustering Key**: `post_id` (sorts posts within user's partition)
  - All posts for `user_id=X` stored together on same node
  - Posts sorted by `post_id` (TIMEUUID = time-sortable)
  
- **A is wrong** ❌
  - Reversed roles
  
- **C is wrong** ❌
  - `post_id` is clustering, not partition
  
- **D is wrong** ❌
  - Composite partition key syntax: `PRIMARY KEY ((user_id, post_id))`
  - Note the **double parentheses**

**Composite Partition Key Example:**

```sql
CREATE TABLE events (
  user_id UUID,
  event_date DATE,
  event_id TIMEUUID,
  PRIMARY KEY ((user_id, event_date), event_id)
);
-- Partition key: (user_id, event_date)
-- Clustering key: event_id
```

**Key takeaway:** `PRIMARY KEY (partition, clustering)` — partition determines node, clustering sorts within partition.

---

## Question 7 — Requirement → CQL Query

**Requirement:** Get the **last 10 posts** for user with ID `123e4567-e89b-12d3-a456-426614174000`, sorted by most recent first.

**Assumptions:**
- Table from Question 6 exists
- `CLUSTERING ORDER BY (post_id DESC)` is set

**Which query is CORRECT?**

- [ ] A.
```sql
SELECT * FROM posts
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000
ORDER BY post_id DESC
LIMIT 10;
```

- [ ] B.
```sql
SELECT * FROM posts
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000
LIMIT 10;
```

- [ ] C.
```sql
SELECT * FROM posts
ORDER BY post_id DESC
LIMIT 10;
```

- [ ] D.
```sql
SELECT * FROM posts
WHERE post_id DESC
LIMIT 10;
```

**Explanation:**

- **B is CORRECT** ✅
  - Must query by **partition key** (`user_id`)
  - `LIMIT 10` gets first 10 results
  - Data already sorted by `post_id DESC` (clustering order)
  - No need for explicit `ORDER BY` (it's implicit)
  
- **A is technically correct** ✅ but redundant
  - Explicit `ORDER BY post_id DESC` is unnecessary
  - Data is already sorted by clustering key
  
- **C is wrong** ❌
  - No `WHERE user_id = ...` (full table scan)
  - Cassandra will reject: "Cannot execute this query as it might involve data filtering"
  
- **D is wrong** ❌
  - Invalid syntax: `WHERE post_id DESC` doesn't make sense

**Key takeaway:** Always query by **partition key**. Clustering order is automatic.

---

## Question 8 — Cassandra Denormalization

**Scenario:** You have users and posts. You need to support two queries:
1. Get all posts by a user
2. Get a specific post by post ID (with author info)

**Which schema design is BEST for Cassandra?**

- [ ] A. Single normalized table with secondary index on `user_id`

```sql
CREATE TABLE posts (
  post_id UUID PRIMARY KEY,
  user_id UUID,
  title TEXT
);
CREATE INDEX ON posts (user_id);
```

- [ ] B. Two denormalized tables (one per query)

```sql
-- Query 1: Get posts by user
CREATE TABLE posts_by_user (
  user_id UUID,
  post_id UUID,
  username TEXT,
  title TEXT,
  PRIMARY KEY (user_id, post_id)
);

-- Query 2: Get post by ID
CREATE TABLE posts_by_id (
  post_id UUID PRIMARY KEY,
  user_id UUID,
  username TEXT,
  title TEXT
);
```

- [ ] C. Single table with composite partition key

```sql
CREATE TABLE posts (
  user_id UUID,
  post_id UUID,
  title TEXT,
  PRIMARY KEY ((user_id, post_id))
);
```

- [ ] D. Use JOINs to fetch user data when querying posts

**Explanation:**

- **B is CORRECT** ✅
  - **Denormalize**: Create one table per query pattern
  - `posts_by_user`: Fast lookup of user's posts
  - `posts_by_id`: Fast lookup of individual post
  - Accept data duplication (username stored twice)
  - **This is the Cassandra way**
  
- **A is wrong** ❌
  - Secondary indexes are **slow** (query all nodes)
  - Not recommended for high-traffic queries
  
- **C is wrong** ❌
  - Composite partition key `((user_id, post_id))` doesn't help
  - Can't query by `post_id` alone (need both keys)
  
- **D is wrong** ❌
  - **Cassandra has NO JOINs**
  - Must denormalize

**Trade-off:**

- ✅ Faster reads (optimized per query)
- ❌ Data duplication (disk space cost)
- ❌ Update complexity (update both tables)

**Key takeaway:** Cassandra requires **one table per query pattern** (denormalization).

---

## Question 9 — Redis vs Cassandra Use Cases

**Which scenario is BETTER suited for Cassandra than Redis?**

- [ ] A. Caching API responses for 5 minutes
- [ ] B. Storing 100TB of time-series sensor data across 50 servers
- [ ] C. Real-time leaderboard with 10,000 players
- [ ] D. Session storage with 30-minute expiration

**Explanation:**

- **B is CORRECT** ✅
  - **Cassandra strengths:**
    - Petabyte-scale storage (disk-based)
    - Horizontal scaling (add nodes for capacity)
    - Time-series optimized (partition by time)
    - Fault tolerance (replication across datacenters)
  - **Redis weakness:** In-memory storage (expensive at 100TB scale)
  
- **A is wrong** ❌
  - Redis is perfect for caching (in-memory speed + TTL)
  
- **C is wrong** ❌
  - Redis sorted sets are ideal (in-memory, sub-millisecond updates)
  - Cassandra would work but slower
  
- **D is wrong** ❌
  - Redis excels at session storage (TTL, fast lookups)

**Decision Matrix:**

| Requirement | Redis | Cassandra |
|------------|-------|-----------|
| < 100GB data, fast reads | ✅ | ❌ |
| > 1TB data, distributed | ❌ | ✅ |
| TTL expiration | ✅ | ✅ (both support) |
| Sub-millisecond latency | ✅ | ❌ (ms latency) |
| ACID transactions | ❌ | ❌ (neither) |
| High write throughput | ✅ (if fits in RAM) | ✅ (disk-based) |

**Key takeaway:** Redis = **in-memory speed**, Cassandra = **massive scale + durability**.

---

## Question 10 — Cassandra Consistency Levels

**Given:**
- Replication Factor (RF) = 3 (data replicated on 3 nodes)
- Write CL = QUORUM (2/3 nodes must acknowledge)
- Read CL = QUORUM (2/3 nodes must respond)

**Which statement is TRUE?**

- [ ] A. Reads may return stale data (eventual consistency)
- [ ] B. Reads always return the latest data (strong consistency)
- [ ] C. Writes may fail even if 1 node is down
- [ ] D. System becomes unavailable if any node is down

**Explanation:**

- **B is CORRECT** ✅
  - **Strong consistency** formula: `Write CL + Read CL > RF`
  - Here: `QUORUM + QUORUM = 2 + 2 = 4 > 3 (RF)`
  - At least one node overlaps between write and read quorums
  - Guaranteed to read latest write
  
- **A is wrong** ❌
  - With `CL = ONE`, reads could return stale data
  - But `QUORUM + QUORUM` ensures strong consistency
  
- **C is wrong** ❌
  - Write needs 2/3 nodes
  - If 1 node is down, 2 are still available (write succeeds)
  
- **D is wrong** ❌
  - System tolerates 1 node failure (availability maintained)

**Consistency Level Trade-offs:**

| Write CL | Read CL | Consistency | Availability |
|----------|---------|-------------|--------------|
| ONE | ONE | Eventual | High |
| QUORUM | QUORUM | Strong | Medium |
| ALL | ALL | Strong | Low (fails if any node down) |

**Key takeaway:** `Write QUORUM + Read QUORUM > RF` = **strong consistency**.

---

## Question 11 — Redis Hash vs String

**Scenario:** Store user profile with 10 fields (name, email, age, city, etc.).

**Which Redis approach is BETTER?**

- [ ] A. Store as JSON string
```bash
SET user:1000 '{"name":"Alice","email":"alice@example.com",...}'
```

- [ ] B. Store as Hash
```bash
HSET user:1000 name "Alice" email "alice@example.com" ...
```

- [ ] C. Store each field as separate key
```bash
SET user:1000:name "Alice"
SET user:1000:email "alice@example.com"
...
```

- [ ] D. Store in a sorted set

**Explanation:**

- **B is CORRECT** ✅
  - **Hash** is designed for objects with fields
  - Update single field: `HSET user:1000 email "newemail@example.com"`
  - Get single field: `HGET user:1000 email`
  - Get all fields: `HGETALL user:1000`
  - Memory efficient (optimized encoding for small hashes)
  
- **A is acceptable** ⚠️ but less efficient
  - Must parse/stringify JSON on every update
  - Can't update single field (must replace entire JSON)
  - Good if you always read/write entire object
  
- **C is wrong** ❌
  - 10 keys instead of 1 (management overhead)
  - More memory (each key has overhead)
  - Can't atomically get all fields
  
- **D is wrong** ❌
  - Sorted sets are for scoring/ranking, not object storage

**Key takeaway:** Use **Hashes** for objects with multiple fields.

---

## Question 12 — Cassandra Write Path

**What happens when you write to Cassandra?**

```sql
INSERT INTO users (user_id, username) VALUES (..., 'alice');
```

**Which statement describes the write path?**

- [ ] A. Data written directly to SSTables on disk
- [ ] B. Data written to memory (memtable) and commit log, then flushed to disk
- [ ] C. Data sent to master node, which distributes to replicas
- [ ] D. Data written to WAL (write-ahead log), then to B-tree index

**Explanation:**

- **B is CORRECT** ✅
  - **Write path:**
    1. Write to **commit log** (append-only log for durability)
    2. Write to **memtable** (in-memory structure)
    3. Acknowledge to client (fast!)
    4. Later: Flush memtable to **SSTable** (sorted string table) on disk
    5. **Compaction**: Merge SSTables over time
  - **Why fast:** Writes are append-only (no seeks), memtable is in-memory
  
- **A is wrong** ❌
  - Writes go to memtable first, not directly to SSTables
  
- **C is wrong** ❌
  - Cassandra has **no master** (peer-to-peer)
  - Client can write to any node (coordinator)
  
- **D is wrong** ❌
  - Cassandra uses **commit log**, not WAL (but similar concept)
  - No B-tree (uses LSM tree structure)

**Write Path Diagram:**

```
Client → Coordinator Node
         ↓
    Commit Log (disk, append-only)
         ↓
    Memtable (memory)
         ↓
    (Periodically flush)
         ↓
    SSTable (disk, immutable)
         ↓
    (Compaction merges SSTables)
```

**Key takeaway:** Cassandra writes are **fast** (commit log + memtable, both optimized for writes).

---

## Scoring

**Grade Scale:**
- 11-12 correct: **A** (Excellent)
- 9-10 correct: **B** (Good)
- 7-8 correct: **C** (Satisfactory)
- 5-6 correct: **D** (Needs improvement)
- 0-4 correct: **F** (Review material)

---

## Answer Key

1. **D** — Sorted Set for leaderboard
2. **C** — Fixed window rate limiting (key per minute + INCR + EXPIRE)
3. **C** — Lists with BRPOP for task queue
4. **C** — SET removes TTL (-1 = no expiration)
5. **C** — Cassandra is AP (availability + partition tolerance)
6. **B** — user_id = partition key, post_id = clustering key
7. **B** — Query by partition key, LIMIT 10
8. **B** — Denormalize into two tables (one per query)
9. **B** — 100TB time-series data (Cassandra's strength)
10. **B** — QUORUM + QUORUM = strong consistency
11. **B** — Hash for multi-field objects
12. **B** — Write to commit log + memtable, flush to SSTable

---

**Related Content:**
- [Redis Fundamentals](../redis-fundamentals.md)
- [Cassandra Fundamentals](../cassandra-fundamentals.md)
- [Readings & Resources](../readings-11.md)
