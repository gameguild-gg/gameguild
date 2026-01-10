# Apache Kafka Fundamentals - Event Streaming

## Introduction

**Apache Kafka** is a distributed event streaming platform used for building real-time data pipelines and streaming applications. It acts as a high-throughput, fault-tolerant message broker that can handle millions of events per second.

---

## What is Event Streaming?

**Event streaming** is the practice of capturing, storing, and processing streams of events in real-time.

**Example Events:**

- User clicked a button
- Sensor reported temperature: 25°C
- Order placed: $99.99
- Stock price updated: $150.25

**Traditional Request/Response vs Event Streaming:**

| Approach | Traditional (REST API) | Event Streaming (Kafka) |
|----------|----------------------|------------------------|
| **Communication** | Synchronous (request → response) | Asynchronous (publish → subscribe) |
| **Coupling** | Tight (client must know server) | Loose (producers/consumers independent) |
| **Data Flow** | Pull (client requests data) | Push (events published to topics) |
| **Scalability** | Limited by server capacity | Horizontally scalable |
| **Durability** | No persistence (unless saved) | Events persisted to disk |
| **Use Cases** | CRUD operations | Real-time analytics, event-driven architectures |

---

## Kafka vs Traditional Databases

| Aspect | Relational DB (PostgreSQL) | Kafka |
|--------|---------------------------|-------|
| **Purpose** | Store current state | Stream events over time |
| **Query** | Random access (SELECT) | Sequential read (consume from offset) |
| **Updates** | UPDATE, DELETE | Append-only (immutable events) |
| **Retention** | Indefinite | Configurable (7 days, 30 days, infinite) |
| **Throughput** | ~10K writes/sec | ~1M writes/sec per partition |
| **Latency** | ~10ms | ~2ms |

**When to Use Kafka:**

- ✅ Real-time data pipelines (logs, metrics, clickstreams)
- ✅ Event-driven microservices
- ✅ Stream processing (aggregations, filtering, enrichment)
- ✅ Change Data Capture (CDC) from databases
- ✅ High-throughput message broker

**When to Avoid Kafka:**

- ❌ Low-latency request/response (use REST/gRPC)
- ❌ Simple task queues (use Redis, RabbitMQ)
- ❌ Small-scale applications (Kafka adds operational complexity)

---

## Core Concepts

### 1. Topics

A **topic** is a category or feed name to which events are published.

**Example Topics:**

- `user-signups`
- `order-events`
- `sensor-readings`
- `payment-transactions`

**Characteristics:**

- Topics are **append-only logs**
- Events are **immutable** (cannot be updated or deleted)
- Topics can be **multi-subscriber** (many consumers)

**Example:**

```
Topic: order-events

Partition 0: [Event1] [Event2] [Event3] [Event4] ...
Partition 1: [Event5] [Event6] [Event7] [Event8] ...
Partition 2: [Event9] [Event10] [Event11] ...
```

---

### 2. Partitions

Topics are divided into **partitions** for parallelism and scalability.

**Key Properties:**

- Each partition is an **ordered, immutable sequence** of events
- Events within a partition have **monotonically increasing offsets**
- Partitions enable **parallel processing** (multiple consumers)
- Each partition is **replicated** across brokers for fault tolerance

**Example:**

```
Topic: user-clicks (3 partitions)

Partition 0: [click1] [click4] [click7] ...
Partition 1: [click2] [click5] [click8] ...
Partition 2: [click3] [click6] [click9] ...
```

**How events are assigned to partitions:**

1. **With key:** Hash(key) % num_partitions → ensures same key goes to same partition
2. **No key:** Round-robin across partitions

---

### 3. Producers

**Producers** publish events to topics.

**Responsibilities:**

- Serialize events (JSON, Avro, Protobuf)
- Choose partition (via key or round-robin)
- Send events to Kafka brokers
- Handle retries and acknowledgments

**Example:**

```typescript
await producer.send({
  topic: 'order-events',
  messages: [
    {
      key: 'user-123',           // Events with same key → same partition
      value: JSON.stringify({
        orderId: 'order-456',
        amount: 99.99,
        timestamp: new Date(),
      }),
    },
  ],
});
```

---

### 4. Consumers

**Consumers** subscribe to topics and process events.

**Responsibilities:**

- Fetch events from partitions
- Deserialize events
- Process events (e.g., save to database, send email)
- Track **offset** (position in partition)

**Example:**

```typescript
await consumer.subscribe({ topic: 'order-events' });

await consumer.run({
  eachMessage: async ({ topic, partition, message }) => {
    const order = JSON.parse(message.value.toString());
    console.log('Processing order:', order.orderId);
    // Save to database, send email, etc.
  },
});
```

---

### 5. Consumer Groups

**Consumer groups** enable **parallel consumption** and **load balancing**.

**How it works:**

- Each consumer in a group reads from **different partitions**
- Kafka ensures each partition is consumed by **only one consumer per group**
- If a consumer fails, Kafka **rebalances** partitions to other consumers

**Example:**

```
Topic: order-events (3 partitions)
Consumer Group: order-processors (3 consumers)

Consumer 1 → Partition 0
Consumer 2 → Partition 1
Consumer 3 → Partition 2
```

**Multiple Consumer Groups:**

```
Topic: user-clicks

Group A (analytics):
  Consumer A1 → Partition 0
  Consumer A2 → Partition 1, 2

Group B (fraud-detection):
  Consumer B1 → Partition 0, 1, 2
```

**Key Rule:** Each partition is assigned to **one consumer per group**, but **multiple groups** can consume the same topic independently.

---

### 6. Offsets

An **offset** is a unique identifier for each event within a partition.

**Example:**

```
Partition 0: [Event0] [Event1] [Event2] [Event3] [Event4]
Offsets:        0        1        2        3        4
```

**Consumer Offset Tracking:**

- Consumers track their **current offset** for each partition
- Offsets are stored in Kafka (`__consumer_offsets` topic)
- On restart, consumer resumes from **last committed offset**

**Commit Strategies:**

1. **Auto-commit:** Kafka commits offsets automatically (default every 5 seconds)
2. **Manual commit:** Consumer commits after processing each message
3. **Batch commit:** Consumer commits after processing batch

---

### 7. Brokers

A **broker** is a Kafka server that stores and serves events.

**Kafka Cluster:**

```
Kafka Cluster (3 brokers)

Broker 1: Partition 0 (leader), Partition 2 (replica)
Broker 2: Partition 1 (leader), Partition 0 (replica)
Broker 3: Partition 2 (leader), Partition 1 (replica)
```

**Replication:**

- Each partition has **one leader** and **N-1 replicas**
- Producers/consumers interact with **leader**
- If leader fails, Kafka elects a new leader from replicas

---

## Docker Setup

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"

  kafka:
    image: confluentinc/cp-kafka:7.5.0
    depends_on:
      - zookeeper
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_TRANSACTION_STATE_LOG_MIN_ISR: 1
      KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR: 1
```

```bash
# Start Kafka
docker-compose up -d

# Wait 30 seconds for startup

# Verify Kafka is running
docker-compose logs kafka | grep "started (kafka.server.KafkaServer)"
```

---

## TypeScript Integration with kafkajs

### Installation

```bash
npm install kafkajs
npm install -D @types/node
```

---

### Create Kafka Client

```typescript
import { Kafka } from 'kafkajs';

const kafka = new Kafka({
  clientId: 'my-app',
  brokers: ['localhost:9092'],
});
```

---

### Create Topic

```typescript
const admin = kafka.admin();

async function createTopic(topicName: string, partitions: number = 3) {
  await admin.connect();
  
  await admin.createTopics({
    topics: [
      {
        topic: topicName,
        numPartitions: partitions,
        replicationFactor: 1,
      },
    ],
  });
  
  console.log(`Topic created: ${topicName}`);
  await admin.disconnect();
}

// Usage
await createTopic('order-events', 3);
```

---

### Producer

```typescript
const producer = kafka.producer();

async function publishEvent(topic: string, key: string, value: any) {
  await producer.connect();
  
  await producer.send({
    topic,
    messages: [
      {
        key,
        value: JSON.stringify(value),
        timestamp: Date.now().toString(),
      },
    ],
  });
  
  console.log('Event published:', { topic, key, value });
}

// Usage
await publishEvent('order-events', 'user-123', {
  orderId: 'order-456',
  amount: 99.99,
  items: ['laptop', 'mouse'],
});

await producer.disconnect();
```

---

### Batch Producer

```typescript
async function publishBatch(topic: string, events: { key: string; value: any }[]) {
  await producer.connect();
  
  await producer.send({
    topic,
    messages: events.map(event => ({
      key: event.key,
      value: JSON.stringify(event.value),
    })),
  });
  
  console.log(`Published ${events.length} events to ${topic}`);
  await producer.disconnect();
}

// Usage
await publishBatch('sensor-readings', [
  { key: 'sensor-1', value: { temperature: 25, humidity: 60 } },
  { key: 'sensor-2', value: { temperature: 22, humidity: 55 } },
  { key: 'sensor-3', value: { temperature: 28, humidity: 65 } },
]);
```

---

### Consumer

```typescript
const consumer = kafka.consumer({ groupId: 'order-processors' });

async function consumeEvents(topic: string) {
  await consumer.connect();
  await consumer.subscribe({ topic, fromBeginning: true });
  
  await consumer.run({
    eachMessage: async ({ topic, partition, message }) => {
      const key = message.key?.toString();
      const value = JSON.parse(message.value?.toString() || '{}');
      
      console.log({
        topic,
        partition,
        offset: message.offset,
        key,
        value,
      });
      
      // Process event
      await processOrder(value);
    },
  });
}

async function processOrder(order: any) {
  console.log('Processing order:', order.orderId);
  // Save to database, send email, etc.
}

// Usage
await consumeEvents('order-events');
```

---

### Consumer Group Example

```typescript
// Consumer 1 (analytics)
const analyticsConsumer = kafka.consumer({ groupId: 'analytics-group' });

await analyticsConsumer.connect();
await analyticsConsumer.subscribe({ topic: 'user-clicks' });

await analyticsConsumer.run({
  eachMessage: async ({ message }) => {
    const click = JSON.parse(message.value?.toString() || '{}');
    console.log('[Analytics] Tracking click:', click);
    // Save to analytics DB
  },
});

// Consumer 2 (fraud detection) - DIFFERENT GROUP
const fraudConsumer = kafka.consumer({ groupId: 'fraud-detection-group' });

await fraudConsumer.connect();
await fraudConsumer.subscribe({ topic: 'user-clicks' });

await fraudConsumer.run({
  eachMessage: async ({ message }) => {
    const click = JSON.parse(message.value?.toString() || '{}');
    console.log('[Fraud] Analyzing click:', click);
    // Check for suspicious patterns
  },
});
```

**Both consumers process the same events independently!**

---

### Manual Offset Commit

```typescript
const consumer = kafka.consumer({
  groupId: 'order-processors',
  autoCommit: false, // Disable auto-commit
});

await consumer.subscribe({ topic: 'order-events' });

await consumer.run({
  eachMessage: async ({ topic, partition, message }) => {
    const order = JSON.parse(message.value?.toString() || '{}');
    
    // Process event
    await processOrder(order);
    
    // Manually commit offset after successful processing
    await consumer.commitOffsets([
      {
        topic,
        partition,
        offset: (parseInt(message.offset) + 1).toString(), // Next offset
      },
    ]);
    
    console.log('Committed offset:', message.offset);
  },
});
```

---

## Use Cases

### 1. Real-Time Analytics

**Scenario:** Track user behavior in real-time for dashboards.

```typescript
// Producer: Track user clicks
await producer.send({
  topic: 'user-clicks',
  messages: [
    {
      key: userId,
      value: JSON.stringify({
        userId,
        page: '/products',
        timestamp: new Date(),
      }),
    },
  ],
});

// Consumer: Aggregate clicks
const clickCounts = new Map<string, number>();

await consumer.run({
  eachMessage: async ({ message }) => {
    const click = JSON.parse(message.value?.toString() || '{}');
    const count = clickCounts.get(click.userId) || 0;
    clickCounts.set(click.userId, count + 1);
    
    console.log(`User ${click.userId} clicks: ${count + 1}`);
  },
});
```

---

### 2. Microservices Communication

**Scenario:** Order service publishes events, inventory service consumes.

```typescript
// Order Service (Producer)
async function createOrder(order: any) {
  // Save to database
  await db.insert(orders).values(order);
  
  // Publish event
  await producer.send({
    topic: 'order-created',
    messages: [
      {
        key: order.id,
        value: JSON.stringify(order),
      },
    ],
  });
}

// Inventory Service (Consumer)
const inventoryConsumer = kafka.consumer({ groupId: 'inventory-service' });

await inventoryConsumer.subscribe({ topic: 'order-created' });

await inventoryConsumer.run({
  eachMessage: async ({ message }) => {
    const order = JSON.parse(message.value?.toString() || '{}');
    
    // Reserve inventory
    for (const item of order.items) {
      await reserveInventory(item.productId, item.quantity);
    }
    
    console.log('Inventory reserved for order:', order.id);
  },
});
```

---

### 3. Change Data Capture (CDC)

**Scenario:** Stream database changes to other systems.

```typescript
// PostgreSQL trigger → Kafka producer
async function publishDatabaseChange(table: string, operation: string, data: any) {
  await producer.send({
    topic: 'database-changes',
    messages: [
      {
        key: `${table}-${data.id}`,
        value: JSON.stringify({
          table,
          operation, // INSERT, UPDATE, DELETE
          data,
          timestamp: new Date(),
        }),
      },
    ],
  });
}

// Consumer: Sync to search index (Elasticsearch)
await consumer.run({
  eachMessage: async ({ message }) => {
    const change = JSON.parse(message.value?.toString() || '{}');
    
    if (change.operation === 'INSERT' || change.operation === 'UPDATE') {
      await elasticsearchClient.index({
        index: change.table,
        id: change.data.id,
        body: change.data,
      });
    } else if (change.operation === 'DELETE') {
      await elasticsearchClient.delete({
        index: change.table,
        id: change.data.id,
      });
    }
  },
});
```

---

### 4. Log Aggregation

**Scenario:** Centralize application logs from multiple services.

```typescript
// Application Logger (Producer)
async function logEvent(level: string, message: string, metadata: any) {
  await producer.send({
    topic: 'application-logs',
    messages: [
      {
        key: metadata.service,
        value: JSON.stringify({
          level,
          message,
          service: metadata.service,
          timestamp: new Date(),
          ...metadata,
        }),
      },
    ],
  });
}

// Log Processor (Consumer)
await consumer.run({
  eachMessage: async ({ message }) => {
    const log = JSON.parse(message.value?.toString() || '{}');
    
    // Filter errors
    if (log.level === 'ERROR') {
      await alertTeam(log);
    }
    
    // Save to Elasticsearch
    await elasticsearchClient.index({
      index: 'logs',
      body: log,
    });
  },
});
```

---

## Performance Best Practices

### 1. Batch Producing

```typescript
// ❌ Slow: Send one event at a time
for (const event of events) {
  await producer.send({ topic: 'events', messages: [event] });
}

// ✅ Fast: Batch send
await producer.send({
  topic: 'events',
  messages: events,
});
```

---

### 2. Compression

```typescript
// Enable compression (gzip, snappy, lz4, zstd)
const producer = kafka.producer({
  compression: CompressionTypes.GZIP,
});
```

---

### 3. Partitioning Strategy

```typescript
// ❌ Bad: No key (random partitioning, breaks ordering)
await producer.send({
  topic: 'user-events',
  messages: [{ value: JSON.stringify(event) }],
});

// ✅ Good: Use key (same user → same partition → ordered)
await producer.send({
  topic: 'user-events',
  messages: [
    {
      key: event.userId,
      value: JSON.stringify(event),
    },
  ],
});
```

---

### 4. Consumer Parallelism

```typescript
// More partitions = more parallelism
await admin.createTopics({
  topics: [
    {
      topic: 'high-throughput-topic',
      numPartitions: 10, // 10 consumers can process in parallel
      replicationFactor: 1,
    },
  ],
});
```

---

## Common Pitfalls

### ❌ Not Handling Consumer Rebalances

```typescript
// Consumer may be paused during rebalance
consumer.on('consumer.rebalancing', () => {
  console.log('Rebalancing... pausing processing');
});

consumer.on('consumer.rebalanced', () => {
  console.log('Rebalanced, resuming processing');
});
```

---

### ❌ Processing Events Multiple Times

```typescript
// ❌ Auto-commit may commit before processing completes
const consumer = kafka.consumer({ groupId: 'my-group' }); // Auto-commit enabled

await consumer.run({
  eachMessage: async ({ message }) => {
    await processEvent(message); // If this fails, event already committed!
  },
});

// ✅ Manual commit after successful processing
const consumer = kafka.consumer({
  groupId: 'my-group',
  autoCommit: false,
});

await consumer.run({
  eachMessage: async ({ topic, partition, message }) => {
    await processEvent(message);
    
    // Commit only after success
    await consumer.commitOffsets([
      { topic, partition, offset: (parseInt(message.offset) + 1).toString() },
    ]);
  },
});
```

---

### ❌ Blocking Event Processing

```typescript
// ❌ Slow: Process events sequentially
await consumer.run({
  eachMessage: async ({ message }) => {
    await slowDatabaseSave(message); // Blocks next event
  },
});

// ✅ Fast: Process in parallel (be careful with ordering!)
await consumer.run({
  eachBatch: async ({ batch }) => {
    await Promise.all(
      batch.messages.map(message => processMessage(message))
    );
  },
});
```

---

## Summary

| Feature | Description |
|---------|-------------|
| **Topics** | Categories for events (e.g., `order-events`) |
| **Partitions** | Ordered, immutable event sequences for parallelism |
| **Producers** | Publish events to topics |
| **Consumers** | Subscribe to topics and process events |
| **Consumer Groups** | Load balancing and parallel consumption |
| **Offsets** | Unique position in partition |
| **Brokers** | Kafka servers that store and serve events |
| **kafkajs** | TypeScript/Node.js client for Kafka |

---

## Next Steps

1. ✅ Set up Kafka with Docker
2. ✅ Create topics with partitions
3. ✅ Publish events with producers
4. ✅ Consume events with consumer groups
5. ✅ Implement real-time analytics pipeline
6. ✅ Build event-driven microservices
7. 📚 Read [Kafka Documentation](https://kafka.apache.org/documentation/)

---

**Happy streaming! 🚀**
