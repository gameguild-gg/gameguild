# Quiz 9: Key-Value & Wide-Column Stores

## Instructions

This quiz tests your understanding of Redis (key-value store) and Cassandra (wide-column store), including data structures, use cases, and query patterns.

---

!!! quiz
{
"title": "Redis Data Structure Selection",
"question": "You're building a real-time leaderboard for a gaming app where players' scores change frequently. You need to update scores quickly, get top 10 players instantly, and find a player's rank efficiently. Which Redis data structure is BEST?",
"options": ["String (store JSON of all players)", "Sorted Set (ZSET)", "List (sorted by score)", "Set (unique player IDs)"],
"answers": ["Sorted Set (ZSET)"]
}
!!!

---

**Requirement:** Implement a rate limiter that allows **100 requests per minute** per user. After 1 minute, the counter should reset automatically.

**Which Redis commands correctly implement this?**

Option A:

```bash
INCR rate:user:1000
EXPIRE rate:user:1000 60
# Check if > 100
```

Option B:

```bash
SET rate:user:1000 1 EX 60
INCR rate:user:1000
# Check if > 100
```

Option C:

```bash
key = "rate:user:1000:" + current_minute
INCR key
if first_time:
  EXPIRE key 60
# Check if > 100
```

Option D:

```bash
ZADD rate:user:1000 timestamp request_id
ZREMRANGEBYSCORE rate:user:1000 0 (now - 60)
ZCARD rate:user:1000
# Check if > 100
```

!!! quiz
{
"title": "Requirement to Redis Rate Limiter",
"question": "Which Redis commands correctly implement a rate limiter allowing 100 requests per minute per user?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

!!! quiz
{
"title": "Redis Pub/Sub vs Lists",
"question": "You need a message queue where producers send tasks, multiple workers process tasks, and each task should be processed exactly once. Which Redis approach is BEST?",
"options": ["List with blocking pop (LPUSH/BRPOP)", "Pub/Sub (PUBLISH/SUBSCRIBE)", "List (LPUSH/RPOP)", "Sorted Set (ZADD/ZPOPMIN)"],
"answers": ["List with blocking pop (LPUSH/BRPOP)"]
}
!!!

---

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

!!! quiz
{
"title": "Redis TTL Behavior",
"question": "After running the commands above, what does TTL session:abc return?",
"options": ["30 (30 seconds remaining)", "60 (TTL reset to 60)", "-2 (key doesn't exist)", "-1 (no expiration)"],
"answers": ["-1 (no expiration)"]
}
!!!

---

!!! quiz
{
"title": "Cassandra CAP Theorem",
"question": "Cassandra is classified as which CAP model?",
"options": ["AP (Availability + Partition Tolerance) — sacrifices consistency", "CA (Consistency + Availability) — sacrifices partition tolerance", "CP (Consistency + Partition Tolerance) — sacrifices availability", "CAP (all three guaranteed)"],
"answers": ["AP (Availability + Partition Tolerance) — sacrifices consistency"]
}
!!!

---

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

!!! quiz
{
"title": "Cassandra Primary Key",
"question": "Given the posts table above, which statement is TRUE?",
"options": ["user_id is the clustering key, post_id is the partition key", "user_id is the partition key, post_id is the clustering key", "Both user_id and post_id are partition keys", "(user_id, post_id) is a composite partition key"],
"answers": ["user_id is the partition key, post_id is the clustering key"]
}
!!!

---

**Requirement:** Get the **last 10 posts** for user with ID `123e4567-e89b-12d3-a456-426614174000`, sorted by most recent first. The table from the previous question exists with `CLUSTERING ORDER BY (post_id DESC)`.

Option A:

```sql
SELECT * FROM posts
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000
ORDER BY post_id DESC
LIMIT 10;
```

Option B:

```sql
SELECT * FROM posts
WHERE post_id DESC
LIMIT 10;
```

Option C:

```sql
SELECT * FROM posts
ORDER BY post_id DESC
LIMIT 10;
```

Option D:

```sql
SELECT * FROM posts
WHERE user_id = 123e4567-e89b-12d3-a456-426614174000
LIMIT 10;
```

!!! quiz
{
"title": "Requirement to CQL Query",
"question": "Which CQL query correctly gets the last 10 posts for a user, sorted by most recent first?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

**Scenario:** You have users and posts. You need to support two queries: (1) Get all posts by a user, and (2) Get a specific post by post ID with author info. Which schema design is BEST for Cassandra?

Option A — Single normalized table with secondary index:

```sql
CREATE TABLE posts (
  post_id UUID PRIMARY KEY,
  user_id UUID,
  title TEXT
);
CREATE INDEX ON posts (user_id);
```

Option B — Use JOINs to fetch user data when querying posts.

Option C — Single table with composite partition key:

```sql
CREATE TABLE posts (
  user_id UUID,
  post_id UUID,
  title TEXT,
  PRIMARY KEY ((user_id, post_id))
);
```

Option D — Two denormalized tables (one per query):

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

!!! quiz
{
"title": "Cassandra Denormalization",
"question": "Which schema design is BEST for supporting both query patterns in Cassandra?",
"options": ["A", "B", "C", "D"],
"answers": ["D"]
}
!!!

---

!!! quiz
{
"title": "Redis vs Cassandra Use Cases",
"question": "Which scenario is BETTER suited for Cassandra than Redis?",
"options": ["Caching API responses for 5 minutes", "Storing 100TB of time-series sensor data across 50 servers", "Real-time leaderboard with 10,000 players", "Session storage with 30-minute expiration"],
"answers": ["Storing 100TB of time-series sensor data across 50 servers"]
}
!!!

---

!!! quiz
{
"title": "Cassandra Consistency Levels",
"question": "Given RF = 3, Write CL = QUORUM (2/3 nodes), Read CL = QUORUM (2/3 nodes) — which statement is TRUE?",
"options": ["Reads may return stale data (eventual consistency)", "Writes may fail even if 1 node is down", "Reads always return the latest data (strong consistency)", "System becomes unavailable if any node is down"],
"answers": ["Reads always return the latest data (strong consistency)"]
}
!!!

---

**Scenario:** Store user profile with 10 fields (name, email, age, city, etc.). Which Redis approach is BETTER?

Option A — Store as JSON string:

```bash
SET user:1000 '{"name":"Alice","email":"alice@example.com",...}'
```

Option B — Store each field as separate key:

```bash
SET user:1000:name "Alice"
SET user:1000:email "alice@example.com"
...
```

Option C — Store as Hash:

```bash
HSET user:1000 name "Alice" email "alice@example.com" ...
```

Option D — Store in a sorted set.

!!! quiz
{
"title": "Redis Hash vs String",
"question": "Which Redis approach is BEST for storing a user profile with 10 fields?",
"options": ["A", "B", "C", "D"],
"answers": ["C"]
}
!!!

---

**What happens when you write to Cassandra?**

```sql
INSERT INTO users (user_id, username) VALUES (..., 'alice');
```

!!! quiz
{
"title": "Cassandra Write Path",
"question": "Which statement describes Cassandra's write path?",
"options": ["Data written to memory (memtable) and commit log, then flushed to disk", "Data written directly to SSTables on disk", "Data sent to master node, which distributes to replicas", "Data written to WAL (write-ahead log), then to B-tree index"],
"answers": ["Data written to memory (memtable) and commit log, then flushed to disk"]
}
!!!
