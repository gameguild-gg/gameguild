# Redis Fundamentals — Key-Value Store

## What is Redis?

**Redis** (Remote Dictionary Server) is an in-memory key-value data store known for its blazing speed and rich data structures. Unlike traditional databases that store data on disk, Redis keeps data in **RAM**, making it 10-100x faster for read/write operations.

### Key Characteristics

- **In-Memory Storage**: Data lives in RAM (with optional disk persistence)
- **Single-Threaded**: Uses event loop for concurrency (no locks needed)
- **Atomic Operations**: All commands are atomic (thread-safe)
- **Rich Data Types**: Strings, lists, sets, hashes, sorted sets, bitmaps, HyperLogLogs, streams
- **Built-in Features**: TTL (expiration), pub/sub, transactions, Lua scripting

### Common Use Cases

| Use Case | Why Redis? |
|----------|-----------|
| **Caching** | Fast lookups, automatic expiration (TTL) |
| **Session Storage** | In-memory speed, key expiration |
| **Rate Limiting** | Atomic counters with TTL |
| **Leaderboards** | Sorted sets for rankings |
| **Real-Time Analytics** | Fast increments, aggregations |
| **Message Queues** | Lists with LPUSH/RPOP |
| **Pub/Sub** | Real-time messaging |

---

## Redis Data Structures

### 1. Strings

The simplest data type: a key mapped to a value (binary-safe, up to 512MB).

```bash
# Set a key-value pair
SET user:1000:name "Alice"

# Get value
GET user:1000:name
# Returns: "Alice"

# Set with expiration (10 seconds)
SETEX session:abc123 10 "user_data"

# Set only if key doesn't exist
SETNX lock:resource 1
# Returns: 1 (success) or 0 (key exists)

# Increment counter
SET views:post:42 100
INCR views:post:42
# Returns: 101

# Increment by N
INCRBY views:post:42 5
# Returns: 106

# Decrement
DECR views:post:42
# Returns: 105

# Set multiple keys at once
MSET user:1:name "Alice" user:1:email "alice@example.com"

# Get multiple keys
MGET user:1:name user:1:email
# Returns: ["Alice", "alice@example.com"]
```

**Use Cases:**
- Caching HTML pages or API responses
- Counters (page views, likes, downloads)
- Session tokens
- Feature flags

---

### 2. Lists

Ordered collections of strings (implemented as linked lists).

```bash
# Add to front (left)
LPUSH tasks "Write code"
LPUSH tasks "Review PR"
# tasks = ["Review PR", "Write code"]

# Add to end (right)
RPUSH tasks "Deploy"
# tasks = ["Review PR", "Write code", "Deploy"]

# Get range (0-based index)
LRANGE tasks 0 -1
# Returns: ["Review PR", "Write code", "Deploy"]

# Get first N items
LRANGE tasks 0 2
# Returns: ["Review PR", "Write code", "Deploy"]

# Remove from front
LPOP tasks
# Returns: "Review PR"
# tasks = ["Write code", "Deploy"]

# Remove from end
RPOP tasks
# Returns: "Deploy"
# tasks = ["Write code"]

# Get length
LLEN tasks
# Returns: 1

# Blocking pop (wait for item)
BLPOP tasks 5
# Waits up to 5 seconds for an item
```

**Use Cases:**
- **Message queues**: Producer uses LPUSH, consumer uses RPOP
- **Activity feeds**: Recent posts (LPUSH + LTRIM)
- **Job queues**: Background tasks
- **Undo/redo stacks**

---

### 3. Sets

Unordered collections of **unique** strings.

```bash
# Add members
SADD users:online "alice" "bob" "charlie"
# Returns: 3 (number added)

# Add duplicate (ignored)
SADD users:online "alice"
# Returns: 0

# Get all members
SMEMBERS users:online
# Returns: ["alice", "bob", "charlie"] (unordered)

# Check membership
SISMEMBER users:online "alice"
# Returns: 1 (true)

SISMEMBER users:online "david"
# Returns: 0 (false)

# Remove member
SREM users:online "bob"

# Get count
SCARD users:online
# Returns: 2

# Set operations
SADD group:admins "alice" "bob"
SADD group:moderators "bob" "charlie"

# Intersection (common members)
SINTER group:admins group:moderators
# Returns: ["bob"]

# Union (all members)
SUNION group:admins group:moderators
# Returns: ["alice", "bob", "charlie"]

# Difference (in first, not in second)
SDIFF group:admins group:moderators
# Returns: ["alice"]

# Random member
SRANDMEMBER users:online
# Returns: "alice" or "charlie" (random)

# Pop random member
SPOP users:online
# Returns: "charlie" (removed)
```

**Use Cases:**
- **Unique visitors**: Track unique IP addresses
- **Tags**: Post tags, product categories
- **Permissions**: User roles/permissions
- **Social graphs**: Friends, followers
- **Real-time analytics**: Unique daily users

---

### 4. Hashes

Maps of field-value pairs (like objects/dictionaries).

```bash
# Set single field
HSET user:1000 name "Alice"

# Set multiple fields
HSET user:1000 email "alice@example.com" age "28"

# Get single field
HGET user:1000 name
# Returns: "Alice"

# Get all fields and values
HGETALL user:1000
# Returns: {name: "Alice", email: "alice@example.com", age: "28"}

# Get multiple fields
HMGET user:1000 name email
# Returns: ["Alice", "alice@example.com"]

# Check if field exists
HEXISTS user:1000 name
# Returns: 1 (true)

# Increment numeric field
HINCRBY user:1000 login_count 1
# Returns: 1

# Get all field names
HKEYS user:1000
# Returns: ["name", "email", "age", "login_count"]

# Get all values
HVALS user:1000
# Returns: ["Alice", "alice@example.com", "28", "1"]

# Delete field
HDEL user:1000 age
```

**Use Cases:**
- **User profiles**: Store user attributes
- **Product details**: SKU, price, stock
- **Configuration**: Application settings
- **Rate limiting**: Track request counts per user
- **Shopping carts**: item_id → quantity

---

### 5. Sorted Sets (ZSets)

Sets where each member has a **score** (used for sorting).

```bash
# Add members with scores
ZADD leaderboard 1000 "alice"
ZADD leaderboard 850 "bob" 920 "charlie"

# Get rank (0-based, lowest score first)
ZRANK leaderboard "bob"
# Returns: 0 (lowest score)

# Get reverse rank (highest first)
ZREVRANK leaderboard "alice"
# Returns: 0 (highest score)

# Get score
ZSCORE leaderboard "alice"
# Returns: "1000"

# Increment score
ZINCRBY leaderboard 50 "bob"
# bob's score: 850 → 900

# Get range by rank (ascending)
ZRANGE leaderboard 0 -1 WITHSCORES
# Returns: ["bob", "900", "charlie", "920", "alice", "1000"]

# Get range by rank (descending)
ZREVRANGE leaderboard 0 2 WITHSCORES
# Returns: ["alice", "1000", "charlie", "920", "bob", "900"]

# Get top 3 players
ZREVRANGE leaderboard 0 2
# Returns: ["alice", "charlie", "bob"]

# Get range by score
ZRANGEBYSCORE leaderboard 900 1000
# Returns: ["bob", "charlie", "alice"]

# Count members in score range
ZCOUNT leaderboard 900 1000
# Returns: 3

# Remove member
ZREM leaderboard "bob"

# Remove by rank
ZREMRANGEBYRANK leaderboard 0 0
# Remove lowest score

# Get cardinality
ZCARD leaderboard
# Returns: 2
```

**Use Cases:**
- **Leaderboards**: Game scores, user rankings
- **Priority queues**: Task priority (score = timestamp)
- **Time-series data**: Events sorted by timestamp
- **Auto-complete**: Prefix matching with scores
- **Rate limiting**: Sliding window with timestamps

---

## Key Expiration (TTL)

Redis supports automatic key deletion after a specified time.

```bash
# Set expiration (10 seconds)
SET cache:page:home "<html>...</html>"
EXPIRE cache:page:home 10

# Set with expiration in one command
SETEX cache:page:home 10 "<html>...</html>"

# Check TTL (seconds remaining)
TTL cache:page:home
# Returns: 8 (seconds left)

# -1 means no expiration
# -2 means key doesn't exist

# Remove expiration
PERSIST cache:page:home

# Set expiration in milliseconds
PEXPIRE cache:page:home 5000

# Set expiration at Unix timestamp
EXPIREAT cache:page:home 1710000000
```

**Use Cases:**
- **Session storage**: Auto-expire after 30 minutes of inactivity
- **Caching**: Expire cached data after 5 minutes
- **One-time tokens**: OTP codes that expire after 60 seconds
- **Rate limiting**: Reset counters every hour

---

## Pub/Sub (Publish/Subscribe)

Real-time messaging pattern where publishers send messages to channels and subscribers receive them.

### Basic Pub/Sub

```bash
# Subscriber (in terminal 1)
SUBSCRIBE news:tech
# Waiting for messages...

# Publisher (in terminal 2)
PUBLISH news:tech "New AI model released!"
# Returns: 1 (number of subscribers)

# Subscriber receives:
# 1) "message"
# 2) "news:tech"
# 3) "New AI model released!"

# Subscribe to multiple channels
SUBSCRIBE news:tech news:sports

# Unsubscribe
UNSUBSCRIBE news:tech

# Pattern subscription (wildcard)
PSUBSCRIBE news:*
# Receives messages from news:tech, news:sports, etc.
```

**Use Cases:**
- **Real-time notifications**: Chat apps, live updates
- **Event broadcasting**: System events to multiple services
- **Cache invalidation**: Notify cache servers to refresh
- **Live dashboards**: Push metrics to browsers

---

## Transactions

Group multiple commands into an atomic operation.

```bash
# Start transaction
MULTI

# Queue commands (not executed yet)
SET user:1000:balance 100
DECRBY user:1000:balance 20
INCRBY user:2000:balance 20

# Execute all commands atomically
EXEC
# Returns: [OK, 80, 20]

# Discard transaction
MULTI
SET key value
DISCARD
# Nothing executed
```

### Optimistic Locking with WATCH

```bash
# Watch key for changes
WATCH user:1000:balance

# Get current balance
GET user:1000:balance
# Returns: "100"

# Start transaction
MULTI
DECRBY user:1000:balance 20

# If another client modifies user:1000:balance before EXEC,
# the transaction will fail
EXEC
# Returns: nil (if key was modified) or [80] (success)
```

**Use Cases:**
- **Money transfers**: Deduct from one account, add to another
- **Inventory management**: Check stock, decrement if available
- **Atomic counters**: Multiple increments/decrements together

---

## Lua Scripting

Execute custom logic atomically on the Redis server.

```bash
# Simple script (increment if value < 100)
EVAL "if redis.call('GET', KEYS[1]) < '100' then return redis.call('INCR', KEYS[1]) else return 0 end" 1 counter
# KEYS[1] = 'counter'

# Script with arguments
EVAL "redis.call('SET', KEYS[1], ARGV[1]); return redis.call('GET', KEYS[1])" 1 mykey myvalue
# KEYS[1] = 'mykey', ARGV[1] = 'myvalue'
```

**Use Cases:**
- **Complex atomic operations**: Multi-step logic
- **Rate limiting algorithms**: Token bucket, sliding window
- **Distributed locks**: Check-and-set patterns

---

## Docker Setup

### docker-compose.yml

```yaml
version: '3.8'

services:
  redis:
    image: redis:7-alpine
    container_name: redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    # --appendonly yes enables persistence

volumes:
  redis-data:
```

```bash
# Start Redis
docker-compose up -d

# Connect to Redis CLI
docker exec -it redis redis-cli

# Test connection
redis-cli ping
# Returns: PONG

# Stop Redis
docker-compose down
```

### Persistence Options

**1. RDB (Snapshot)**

Periodic snapshots of the dataset to disk.

```bash
# In redis.conf or command
redis-server --save 60 1000
# Save every 60 seconds if at least 1000 keys changed
```

**2. AOF (Append-Only File)**

Logs every write operation (more durable).

```bash
redis-server --appendonly yes --appendfsync everysec
# everysec: Fsync every second (good balance)
# always: Fsync on every write (safest, slower)
# no: Let OS decide (fastest, risky)
```

---

## TypeScript Integration (ioredis)

### Installation

```bash
npm install ioredis
npm install -D @types/node
```

### Basic Usage

```typescript
import Redis from 'ioredis';

// Connect to Redis
const redis = new Redis({
  host: 'localhost',
  port: 6379,
  // password: 'your-password',
  // db: 0, // Database index (0-15)
});

// Test connection
redis.ping().then(result => {
  console.log(result); // "PONG"
});

// String operations
await redis.set('user:1000:name', 'Alice');
const name = await redis.get('user:1000:name');
console.log(name); // "Alice"

// With expiration
await redis.setex('session:abc', 3600, 'session_data');

// Increment
await redis.set('views:post:42', '100');
const views = await redis.incr('views:post:42');
console.log(views); // 101

// List operations
await redis.lpush('tasks', 'Task 1', 'Task 2');
const tasks = await redis.lrange('tasks', 0, -1);
console.log(tasks); // ['Task 2', 'Task 1']

// Hash operations
await redis.hset('user:1000', {
  name: 'Alice',
  email: 'alice@example.com',
  age: '28'
});

const user = await redis.hgetall('user:1000');
console.log(user);
// { name: 'Alice', email: 'alice@example.com', age: '28' }

// Set operations
await redis.sadd('users:online', 'alice', 'bob', 'charlie');
const online = await redis.smembers('users:online');
console.log(online); // ['alice', 'bob', 'charlie']

// Sorted set operations
await redis.zadd('leaderboard', 1000, 'alice', 850, 'bob');
const top3 = await redis.zrevrange('leaderboard', 0, 2, 'WITHSCORES');
console.log(top3); // ['alice', '1000', 'bob', '850']

// Pub/Sub
const subscriber = new Redis();
subscriber.subscribe('news:tech');

subscriber.on('message', (channel, message) => {
  console.log(`${channel}: ${message}`);
});

const publisher = new Redis();
await publisher.publish('news:tech', 'Breaking news!');

// Pipeline (batch commands)
const pipeline = redis.pipeline();
pipeline.set('key1', 'value1');
pipeline.set('key2', 'value2');
pipeline.get('key1');
const results = await pipeline.exec();
console.log(results);
// [[null, 'OK'], [null, 'OK'], [null, 'value1']]

// Transactions
const multi = redis.multi();
multi.set('balance:1', '100');
multi.decrby('balance:1', '20');
multi.incrby('balance:2', '20');
const txResults = await multi.exec();
console.log(txResults);

// Graceful shutdown
await redis.quit();
```

### Practical Examples

#### 1. Caching API Responses

```typescript
async function getUser(userId: string) {
  const cacheKey = `cache:user:${userId}`;
  
  // Try cache first
  const cached = await redis.get(cacheKey);
  if (cached) {
    return JSON.parse(cached);
  }
  
  // Fetch from database
  const user = await db.users.findOne({ id: userId });
  
  // Cache for 5 minutes
  await redis.setex(cacheKey, 300, JSON.stringify(user));
  
  return user;
}
```

#### 2. Rate Limiting (Fixed Window)

```typescript
async function isRateLimited(userId: string): Promise<boolean> {
  const key = `rate:${userId}:${Math.floor(Date.now() / 60000)}`; // Per minute
  const current = await redis.incr(key);
  
  if (current === 1) {
    await redis.expire(key, 60); // Expire after 1 minute
  }
  
  return current > 100; // Max 100 requests per minute
}
```

#### 3. Session Storage

```typescript
async function createSession(userId: string): Promise<string> {
  const sessionId = crypto.randomUUID();
  const sessionKey = `session:${sessionId}`;
  
  await redis.setex(
    sessionKey,
    3600, // 1 hour
    JSON.stringify({ userId, createdAt: Date.now() })
  );
  
  return sessionId;
}

async function getSession(sessionId: string) {
  const data = await redis.get(`session:${sessionId}`);
  return data ? JSON.parse(data) : null;
}
```

#### 4. Leaderboard

```typescript
async function updateScore(userId: string, score: number) {
  await redis.zadd('leaderboard', score, userId);
}

async function getTopPlayers(count: number) {
  const players = await redis.zrevrange('leaderboard', 0, count - 1, 'WITHSCORES');
  
  const results = [];
  for (let i = 0; i < players.length; i += 2) {
    results.push({
      userId: players[i],
      score: parseInt(players[i + 1])
    });
  }
  return results;
}

async function getUserRank(userId: string): Promise<number | null> {
  const rank = await redis.zrevrank('leaderboard', userId);
  return rank !== null ? rank + 1 : null; // 1-based rank
}
```

---

## Best Practices

### 1. Use Appropriate Data Structures

- ❌ Don't use strings for everything
- ✅ Use hashes for objects, sets for uniqueness, sorted sets for rankings

### 2. Set Expiration on Cache Keys

```typescript
// ❌ Cache without TTL (grows forever)
await redis.set('cache:page:home', html);

// ✅ Cache with TTL
await redis.setex('cache:page:home', 300, html);
```

### 3. Use Pipelines for Batch Operations

```typescript
// ❌ Multiple round trips
for (const user of users) {
  await redis.set(`user:${user.id}`, user.name);
}

// ✅ Single round trip
const pipeline = redis.pipeline();
for (const user of users) {
  pipeline.set(`user:${user.id}`, user.name);
}
await pipeline.exec();
```

### 4. Monitor Memory Usage

```bash
# Check memory usage
INFO memory

# Get database size
DBSIZE

# Find large keys
redis-cli --bigkeys
```

### 5. Use Key Namespacing

```typescript
// ✅ Good: Organized with prefixes
cache:user:1000
session:abc123
rate:user:1000:2026-03-23
```

---

## Key Takeaways

- **Redis** is an in-memory key-value store optimized for speed
- **5 core data structures**: Strings, Lists, Sets, Hashes, Sorted Sets
- **TTL (expiration)**: Automatic key deletion for caching and sessions
- **Pub/Sub**: Real-time messaging between clients
- **Transactions**: Atomic operations with MULTI/EXEC
- **Use cases**: Caching, sessions, rate limiting, leaderboards, queues
- **ioredis**: Type-safe Redis client for Node.js/TypeScript

---

**Next:** [Cassandra Wide-Column Store](./cassandra-fundamentals.md)
