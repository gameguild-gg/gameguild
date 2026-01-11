# Week 14 - Vector Databases & Event Streaming

**Dates:** April 13-17, 2026

---

## Overview

This week explores two cutting-edge database technologies for AI and real-time applications:

1. **pgvector** - PostgreSQL extension for vector similarity search (semantic search, RAG, recommendations)
2. **Apache Kafka** - Distributed event streaming platform (real-time pipelines, microservices, analytics)

Both technologies enable modern application architectures: AI-powered search and event-driven systems.

---

## Learning Objectives

By the end of this week, you will:

- ✅ Understand vector embeddings and their applications in AI/ML
- ✅ Store and query vector embeddings with pgvector
- ✅ Perform similarity search using cosine, euclidean, and inner product metrics
- ✅ Create vector indices (IVFFlat, HNSW) for performance
- ✅ Build RAG (Retrieval-Augmented Generation) applications
- ✅ Understand event streaming architecture and use cases
- ✅ Create Kafka topics, producers, and consumers
- ✅ Use consumer groups for parallel processing
- ✅ Implement real-time data pipelines with Kafka
- ✅ Integrate pgvector and Kafka with TypeScript

---

## Weekly Schedule

### Monday, April 13 - Vector Databases (pgvector)

**Topics:**

- What are vector embeddings?
- pgvector PostgreSQL extension
- Similarity metrics: cosine (`<=>`), L2 (`<->`), inner product (`<#>`)
- Vector indexing: IVFFlat (< 1M vectors), HNSW (1M-10M vectors)
- RAG architecture: Retrieve → Augment → Generate
- Use cases: semantic search, recommendations, image similarity
- Drizzle ORM integration

**Reading:**

- [pgvector-fundamentals.md](./pgvector-fundamentals.md)
- [pgvector GitHub](https://github.com/pgvector/pgvector)

**Practice:**

```bash
# Start PostgreSQL with pgvector
docker-compose up -d

# Connect
psql -h localhost -U postgres -d vectordb

# Enable extension
CREATE EXTENSION vector;
```

---

### Thursday, April 16 - Event Streaming (Kafka)

**Topics:**

- What is event streaming?
- Kafka architecture: topics, partitions, brokers
- Producers: publishing events
- Consumers: subscribing to topics
- Consumer groups: parallel processing and load balancing
- Offsets and commit strategies
- Use cases: real-time analytics, microservices, CDC, log aggregation
- kafkajs client

**Reading:**

- [kafka-fundamentals.md](./kafka-fundamentals.md)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)

**Practice:**

```bash
# Start Kafka cluster
docker-compose up -d

# Wait 30 seconds

# Verify Kafka is running
docker-compose logs kafka | grep "started"
```

---

## Assessments

### Quiz 12 - Vector Databases & Event Streaming

**Due:** Thursday, April 16, 2026 by 11:59 PM

**Topics:**

- pgvector: embeddings, similarity metrics, indexing, RAG
- Kafka: topics, partitions, producers, consumers, consumer groups, offsets

**Format:**

- 12 multiple-choice questions
- Detailed explanations for each answer

**Access Quiz:**

- [quiz/vector-streaming-quiz.md](./quiz/vector-streaming-quiz.md)

---

### Final Project - Feature Freeze

**Due:** Saturday, April 19, 2026 by 11:59 PM

**Requirements:**

1. **Feature Complete** (50%)
   - All planned features implemented
   - Database operations working (CRUD, queries, transactions)
   - API endpoints functional
   - Error handling implemented

2. **Code Quality** (20%)
   - TypeScript types defined
   - Functions/modules well-organized
   - No critical bugs or errors
   - Code follows best practices

3. **Testing** (20%)
   - Unit tests for core functionality
   - Integration tests for database operations
   - Test coverage > 60%
   - All tests passing

4. **Documentation** (10%)
   - Updated README with complete setup instructions
   - API documentation (endpoints, request/response examples)
   - Database schema documented
   - Deployment guide

**Submission:**

- Submit GitHub repository link
- All tests must pass in GitHub Actions
- No new features after feature freeze (bug fixes only)

---

## Practical Exercises

### Exercise 1: Semantic Search with pgvector

**Scenario:** Build a document search engine that finds relevant articles by meaning, not keywords.

**Tasks:**

1. Set up PostgreSQL with pgvector extension
2. Create table with vector column:
   ```sql
   CREATE TABLE articles (
     id SERIAL PRIMARY KEY,
     title TEXT NOT NULL,
     content TEXT NOT NULL,
     embedding vector(1536),
     created_at TIMESTAMPTZ DEFAULT NOW()
   );
   ```

3. Generate embeddings with OpenAI API:
   ```typescript
   const embedding = await openai.embeddings.create({
     model: 'text-embedding-3-small',
     input: article.content,
   });
   ```

4. Insert articles with embeddings
5. Query similar articles:
   ```sql
   SELECT title, content, embedding <=> $1 AS distance
   FROM articles
   ORDER BY distance
   LIMIT 5;
   ```

6. Create IVFFlat index:
   ```sql
   CREATE INDEX ON articles USING ivfflat (embedding vector_cosine_ops)
   WITH (lists = 100);
   ```

7. Compare query performance before and after indexing

**Expected Output:**

- Semantic search finds relevant articles even without keyword matches
- Index reduces query time by 10-100x
- Example: Search "machine learning" finds articles about "neural networks", "AI", "deep learning"

---

### Exercise 2: RAG Chatbot with pgvector

**Scenario:** Build a Q&A chatbot that answers questions about your company's documentation.

**Tasks:**

1. Chunk documentation into paragraphs (~500 words each)
2. Generate embeddings for each chunk
3. Store in pgvector:
   ```typescript
   await db.insert(chunks).values({
     content: chunk,
     embedding: JSON.stringify(embedding),
   });
   ```

4. On user query:
   - Generate query embedding
   - Find top 3 relevant chunks (similarity search)
   - Build prompt with context
   - Query GPT-4 with augmented prompt

5. Implement conversation history (last 5 messages)

**Expected Output:**

- Chatbot answers questions using documentation context
- Responses cite relevant sections
- Handles follow-up questions

---

### Exercise 3: Real-Time Analytics with Kafka

**Scenario:** Track user events (page views, clicks, purchases) in real-time.

**Tasks:**

1. Create Kafka topic:
   ```typescript
   await admin.createTopics({
     topics: [{ topic: 'user-events', numPartitions: 3 }],
   });
   ```

2. Producer: Track user events
   ```typescript
   await producer.send({
     topic: 'user-events',
     messages: [
       {
         key: userId,
         value: JSON.stringify({
           userId,
           eventType: 'page_view',
           page: '/products',
           timestamp: new Date(),
         }),
       },
     ],
   });
   ```

3. Consumer: Aggregate events in real-time
   ```typescript
   const eventCounts = new Map<string, number>();
   
   await consumer.run({
     eachMessage: async ({ message }) => {
       const event = JSON.parse(message.value.toString());
       const count = eventCounts.get(event.eventType) || 0;
       eventCounts.set(event.eventType, count + 1);
       
       console.log('Event counts:', Object.fromEntries(eventCounts));
     },
   });
   ```

4. Add second consumer group for fraud detection
5. Implement manual offset commits

**Expected Output:**

- Real-time event counters
- Multiple consumer groups processing same events
- Fault tolerance (consumer restart resumes from last offset)

---

### Exercise 4: Event-Driven Microservices

**Scenario:** Build order processing system with event-driven architecture.

**Services:**

1. **Order Service** (Producer)
   - Creates orders
   - Publishes `order-created` event

2. **Inventory Service** (Consumer)
   - Subscribes to `order-created`
   - Reserves inventory
   - Publishes `inventory-reserved` event

3. **Payment Service** (Consumer)
   - Subscribes to `inventory-reserved`
   - Processes payment
   - Publishes `payment-completed` event

4. **Notification Service** (Consumer)
   - Subscribes to `payment-completed`
   - Sends confirmation email

**Tasks:**

- Implement all 4 services
- Use message keys for ordering (same order ID → same partition)
- Handle failures (dead-letter queue)
- Add monitoring (track event processing times)

**Expected Output:**

- Loosely coupled services communicating via events
- Scalable (add more consumers to each service)
- Fault-tolerant (services can restart independently)

---

## Docker Setup

### docker-compose.yml

```yaml
version: '3.8'

services:
  # PostgreSQL with pgvector
  postgres:
    image: pgvector/pgvector:pg16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: vectordb
    volumes:
      - pgvector_data:/var/lib/postgresql/data

  # Zookeeper (required for Kafka)
  zookeeper:
    image: confluentinc/cp-zookeeper:7.5.0
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"

  # Kafka
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

volumes:
  pgvector_data:
```

```bash
# Start all services
docker-compose up -d

# Wait 30 seconds for Kafka startup

# Verify PostgreSQL
psql -h localhost -U postgres -d vectordb -c "CREATE EXTENSION vector;"

# Verify Kafka
docker-compose logs kafka | grep "started (kafka.server.KafkaServer)"
```

---

## Common Pitfalls

### pgvector

❌ **Dimension Mismatch**

```sql
-- Error: Embedding has 3 dimensions, table expects 1536
CREATE TABLE docs (embedding vector(1536));
INSERT INTO docs VALUES ('[0.1, 0.2, 0.3]');
```

**Solution:** Ensure all embeddings match schema dimensions.

---

❌ **Creating Index Before Inserting Data**

```sql
-- Wrong: Index first
CREATE INDEX ON docs USING ivfflat (embedding vector_cosine_ops);
INSERT INTO docs VALUES (...);

-- Correct: Insert first, then index
INSERT INTO docs VALUES (...);
CREATE INDEX ON docs USING ivfflat (embedding vector_cosine_ops);
```

---

❌ **Using Wrong Similarity Operator**

```sql
-- Index created with cosine
CREATE INDEX ON docs USING ivfflat (embedding vector_cosine_ops);

-- Query uses L2 (won't use index!)
SELECT * FROM docs ORDER BY embedding <-> '[0.1, 0.2]';

-- Fix: Use same operator as index
SELECT * FROM docs ORDER BY embedding <=> '[0.1, 0.2]';
```

---

### Kafka

❌ **Not Providing Message Keys**

```typescript
// ❌ No key: Events randomly distributed, breaks ordering
await producer.send({
  topic: 'user-events',
  messages: [{ value: JSON.stringify(event) }],
});

// ✅ With key: Same user → same partition → ordered
await producer.send({
  topic: 'user-events',
  messages: [
    { key: event.userId, value: JSON.stringify(event) },
  ],
});
```

---

❌ **Auto-Commit Before Processing**

```typescript
// ❌ Auto-commit may commit before processing completes
await consumer.run({
  eachMessage: async ({ message }) => {
    await processEvent(message); // If this fails, event lost!
  },
});

// ✅ Manual commit after successful processing
await consumer.run({
  eachMessage: async ({ topic, partition, message }) => {
    await processEvent(message);
    await consumer.commitOffsets([
      { topic, partition, offset: (parseInt(message.offset) + 1).toString() },
    ]);
  },
});
```

---

❌ **Not Handling Consumer Rebalances**

```typescript
// Consumer paused during rebalance
consumer.on('consumer.rebalancing', () => {
  console.log('Rebalancing...');
  // Save state, pause processing
});

consumer.on('consumer.rebalanced', () => {
  console.log('Rebalanced, resuming');
});
```

---

## Decision Matrix

### When to Use pgvector

✅ **Use pgvector when:**

- Semantic search (find similar documents by meaning)
- RAG applications (chatbots, Q&A systems)
- Recommendation systems
- Already using PostgreSQL (add pgvector extension)
- < 10M vectors

❌ **Don't use pgvector when:**

- Need exact keyword search (use Elasticsearch)
- > 10M vectors (use Pinecone, Weaviate, Qdrant)
- Real-time updates at massive scale

---

### When to Use Kafka

✅ **Use Kafka when:**

- Real-time data pipelines (logs, metrics, clickstreams)
- Event-driven microservices
- Stream processing (aggregations, filtering)
- Change Data Capture (CDC)
- High-throughput (> 100K events/sec)

❌ **Don't use Kafka when:**

- Request/response patterns (use REST/gRPC)
- Simple task queues (use Redis, RabbitMQ)
- Small-scale applications (Kafka adds complexity)
- Need strong ordering across all events (Kafka guarantees order per partition only)

---

## Additional Resources

- **Readings:** [readings-14.md](./readings-14.md)
- **pgvector GitHub:** https://github.com/pgvector/pgvector
- **Kafka Docs:** https://kafka.apache.org/documentation/
- **kafkajs:** https://kafka.js.org/
- **OpenAI Embeddings:** https://platform.openai.com/docs/guides/embeddings

---

## Summary

| Technology | Type | Use Cases | Query Language | Scaling |
|-----------|------|-----------|----------------|---------|
| **pgvector** | Vector DB (PostgreSQL extension) | Semantic search, RAG, recommendations | SQL + similarity operators | Vertical (single server) |
| **Kafka** | Event streaming platform | Real-time pipelines, microservices, analytics | Producer/Consumer API | Horizontal (distributed) |

**Key Takeaways:**

- **pgvector** = PostgreSQL + vector similarity search (cosine, L2, inner product)
- **Kafka** = Distributed event log for real-time data streams
- Use the right tool: pgvector for AI/ML search, Kafka for event-driven architectures

---

**Good luck this week! 🚀**
