# pgvector Fundamentals — Vector Databases

## Introduction

**pgvector** is a PostgreSQL extension that adds support for **vector similarity search**. It enables storing and querying high-dimensional vectors (embeddings) for AI/ML applications like semantic search, recommendation systems, and Retrieval-Augmented Generation (RAG).

---

## What are Vector Databases?

Vector databases store **embeddings**: numerical representations of data (text, images, audio) as arrays of floats.

**Example Embedding:**

```
"Hello world" → [0.023, -0.891, 0.456, ..., 0.234]  // 1536 dimensions
```

**Use Cases:**

- ✅ Semantic search (find similar documents by meaning, not keywords)
- ✅ Recommendation engines (find similar products/users)
- ✅ RAG (Retrieval-Augmented Generation) for AI chatbots
- ✅ Image similarity search
- ✅ Anomaly detection

---

## pgvector vs Traditional Databases

| Aspect | Traditional DB (PostgreSQL) | pgvector |
|--------|----------------------------|----------|
| **Data Type** | Numbers, strings, dates | Vectors (arrays of floats) |
| **Search Method** | Exact match, pattern matching | Similarity search (cosine, euclidean) |
| **Query Type** | `WHERE name = 'Alice'` | `ORDER BY embedding <=> query_vector` |
| **Indexing** | B-tree, GIN | IVFFlat, HNSW |
| **Use Cases** | Transactions, structured data | AI/ML, semantic search, RAG |

**When to Use pgvector:**

- ✅ Semantic search ("find documents similar in meaning")
- ✅ RAG applications (chatbots with context retrieval)
- ✅ Recommendation systems
- ✅ Image/audio similarity search
- ✅ Already using PostgreSQL (add pgvector extension)

**When to Avoid pgvector:**

- ❌ Exact keyword search (use Elasticsearch instead)
- ❌ Need specialized vector DB features (use Pinecone, Weaviate, Qdrant)
- ❌ Billion-scale vectors (pgvector works best < 10M vectors)

---

## Core Concepts

### 1. Embeddings

**Embeddings** convert unstructured data (text, images) into numerical vectors.

**Example: Text Embeddings**

```
"cat" → [0.2, 0.8, -0.3, 0.5]
"kitten" → [0.3, 0.7, -0.2, 0.6]  // Similar to "cat"
"car" → [-0.8, 0.1, 0.9, -0.4]    // Different from "cat"
```

**Popular Embedding Models:**

- **OpenAI**: `text-embedding-3-small` (1536 dimensions), `text-embedding-3-large` (3072 dimensions)
- **Google**: `textembedding-gecko` (768 dimensions)
- **Open Source**: Sentence Transformers (384-768 dimensions)

---

### 2. Similarity Metrics

**How to measure "closeness" between vectors?**

#### Cosine Similarity (Most Common)

Measures the **angle** between vectors. Range: -1 to 1 (1 = identical, 0 = orthogonal, -1 = opposite).

**pgvector operator:** `<=>` (cosine distance, where 0 = identical)

```sql
SELECT * FROM documents
ORDER BY embedding <=> '[0.1, 0.2, 0.3]'
LIMIT 10;
```

**When to use:** Text embeddings, normalized vectors.

---

#### Euclidean Distance (L2)

Measures **straight-line distance** between vectors.

**pgvector operator:** `<->` (L2 distance)

```sql
SELECT * FROM images
ORDER BY embedding <-> '[0.5, 0.5, 0.5]'
LIMIT 10;
```

**When to use:** Image embeddings, non-normalized vectors.

---

#### Inner Product

Measures **dot product** (similarity for unnormalized vectors).

**pgvector operator:** `<#>` (negative inner product)

```sql
SELECT * FROM products
ORDER BY embedding <#> '[0.3, 0.4, 0.5]'
LIMIT 10;
```

**When to use:** Rare; mostly for specific ML models.

---

### 3. Vector Indexing

**Without indexing:** pgvector scans **all vectors** (slow for large datasets).

**With indexing:** Uses approximate nearest neighbor (ANN) algorithms for fast search.

#### IVFFlat (Inverted File with Flat Compression)

- **How it works:** Clusters vectors into "cells", searches only relevant cells.
- **Speed:** Fast (10-100x faster than exact search).
- **Accuracy:** Good (95-99% recall).
- **Best for:** < 1M vectors.

**Example:**

```sql
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);
```

**lists parameter:** Number of clusters. Rule of thumb: `sqrt(rows)` for < 1M rows.

---

#### HNSW (Hierarchical Navigable Small World)

- **How it works:** Builds a multi-layer graph for efficient search.
- **Speed:** Very fast (100-1000x faster than exact search).
- **Accuracy:** Excellent (99%+ recall).
- **Best for:** 1M-10M vectors, when query speed is critical.

**Example:**

```sql
CREATE INDEX ON documents USING hnsw (embedding vector_cosine_ops);
```

**Comparison:**

| Index Type | Speed | Accuracy | Build Time | Memory | Best For |
|-----------|-------|----------|------------|--------|----------|
| **None** (exact) | Slow | 100% | N/A | Low | < 10K vectors |
| **IVFFlat** | Fast | 95-99% | Fast | Medium | 10K-1M vectors |
| **HNSW** | Very fast | 99%+ | Slower | High | 1M-10M vectors |

---

## Docker Setup

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
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

volumes:
  pgvector_data:
```

```bash
# Start PostgreSQL with pgvector
docker-compose up -d

# Wait 10 seconds for startup

# Connect
psql -h localhost -U postgres -d vectordb
```

---

### Enable pgvector Extension

```sql
CREATE EXTENSION vector;

-- Verify
SELECT * FROM pg_extension WHERE extname = 'vector';
```

---

## Schema Design

### Create Table with Vector Column

```sql
CREATE TABLE documents (
  id SERIAL PRIMARY KEY,
  content TEXT NOT NULL,
  embedding vector(1536),  -- 1536 dimensions (OpenAI embeddings)
  created_at TIMESTAMPTZ DEFAULT NOW()
);
```

**vector(N):** N = number of dimensions (must match your embedding model).

**Common dimensions:**

- OpenAI `text-embedding-3-small`: 1536
- OpenAI `text-embedding-3-large`: 3072
- Sentence Transformers: 384 or 768

---

## CRUD Operations

### Insert Vectors

```sql
INSERT INTO documents (content, embedding) VALUES
  ('PostgreSQL is a relational database', '[0.1, 0.2, 0.3, ..., 0.5]'),
  ('MongoDB is a document database', '[0.2, 0.3, 0.1, ..., 0.4]'),
  ('Redis is a key-value store', '[0.3, 0.1, 0.2, ..., 0.6]');
```

**Note:** In practice, embeddings are generated by an API (OpenAI, Google) or ML model.

---

### Query Similar Vectors

**Find 5 most similar documents:**

```sql
SELECT id, content, embedding <=> '[0.15, 0.25, 0.2, ..., 0.45]' AS distance
FROM documents
ORDER BY distance
LIMIT 5;
```

**How it works:**

1. Calculate distance between query vector and each document vector
2. Sort by distance (ascending = most similar first)
3. Return top 5

---

### Filter + Similarity Search

**Find similar documents created in the last week:**

```sql
SELECT id, content, embedding <=> '[0.1, 0.2, 0.3]' AS distance
FROM documents
WHERE created_at > NOW() - INTERVAL '7 days'
ORDER BY distance
LIMIT 10;
```

---

## Indexing for Performance

### Create IVFFlat Index

```sql
-- Create index (after inserting data)
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

-- For euclidean distance
CREATE INDEX ON documents USING ivfflat (embedding vector_l2_ops)
WITH (lists = 100);
```

**When to create index:** After inserting at least 10,000 rows.

**lists parameter:**

- For 10K-100K rows: `lists = 100`
- For 100K-1M rows: `lists = 1000`
- Rule of thumb: `sqrt(rows)`

---

### Create HNSW Index

```sql
-- HNSW index (better accuracy, slower build)
CREATE INDEX ON documents USING hnsw (embedding vector_cosine_ops);

-- Tune parameters (optional)
CREATE INDEX ON documents USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);
```

**Parameters:**

- **m**: Number of connections per layer (default 16, higher = better accuracy but slower)
- **ef_construction**: Search depth during index build (default 64)

---

## TypeScript Integration with Drizzle ORM

### Installation

```bash
npm install drizzle-orm postgres
npm install -D drizzle-kit
npm install openai  # For generating embeddings
```

---

### Drizzle Schema

```typescript
import { pgTable, serial, text, vector, timestamp } from 'drizzle-orm/pg-core';

export const documents = pgTable('documents', {
  id: serial('id').primaryKey(),
  content: text('content').notNull(),
  embedding: vector('embedding', { dimensions: 1536 }),
  createdAt: timestamp('created_at').defaultNow(),
});

export type Document = typeof documents.$inferSelect;
export type NewDocument = typeof documents.$inferInsert;
```

---

### Database Connection

```typescript
import { drizzle } from 'drizzle-orm/postgres-js';
import postgres from 'postgres';

const client = postgres(process.env.DATABASE_URL!);
export const db = drizzle(client);
```

---

### Generate Embeddings with OpenAI

```typescript
import OpenAI from 'openai';

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
});

async function generateEmbedding(text: string): Promise<number[]> {
  const response = await openai.embeddings.create({
    model: 'text-embedding-3-small',
    input: text,
  });
  
  return response.data[0].embedding;
}
```

---

### Insert Documents with Embeddings

```typescript
import { db } from './db';
import { documents } from './schema';
import { generateEmbedding } from './embeddings';

async function addDocument(content: string) {
  const embedding = await generateEmbedding(content);
  
  await db.insert(documents).values({
    content,
    embedding: JSON.stringify(embedding), // Store as JSON string
  });
  
  console.log('Document added:', content);
}

// Usage
await addDocument('PostgreSQL is a powerful relational database');
await addDocument('MongoDB is a flexible document database');
await addDocument('Redis is a fast in-memory key-value store');
```

---

### Semantic Search

```typescript
import { sql } from 'drizzle-orm';

async function semanticSearch(query: string, limit: number = 5) {
  const queryEmbedding = await generateEmbedding(query);
  const queryVector = JSON.stringify(queryEmbedding);
  
  const results = await db.execute(sql`
    SELECT 
      id,
      content,
      embedding <=> ${queryVector}::vector AS distance
    FROM documents
    ORDER BY distance
    LIMIT ${limit}
  `);
  
  return results.rows;
}

// Usage
const results = await semanticSearch('What is a relational database?');
console.log(results);
// [
//   { id: 1, content: 'PostgreSQL is a powerful relational database', distance: 0.12 },
//   { id: 2, content: 'MongoDB is a flexible document database', distance: 0.45 },
//   ...
// ]
```

---

### Filtered Semantic Search

```typescript
async function searchRecentDocuments(query: string, days: number = 7) {
  const queryEmbedding = await generateEmbedding(query);
  const queryVector = JSON.stringify(queryEmbedding);
  
  const results = await db.execute(sql`
    SELECT 
      id,
      content,
      created_at,
      embedding <=> ${queryVector}::vector AS distance
    FROM documents
    WHERE created_at > NOW() - INTERVAL '${days} days'
    ORDER BY distance
    LIMIT 10
  `);
  
  return results.rows;
}
```

---

## RAG (Retrieval-Augmented Generation)

**RAG** combines vector search with LLMs to answer questions using your own data.

**Architecture:**

1. **Store:** Embed documents and store in pgvector
2. **Retrieve:** Find relevant documents via similarity search
3. **Augment:** Add retrieved documents to LLM prompt
4. **Generate:** LLM generates answer using context

---

### RAG Implementation

```typescript
import OpenAI from 'openai';

const openai = new OpenAI({ apiKey: process.env.OPENAI_API_KEY });

async function ragQuery(userQuestion: string): Promise<string> {
  // 1. Find relevant documents
  const relevantDocs = await semanticSearch(userQuestion, 3);
  
  // 2. Build context from top results
  const context = relevantDocs
    .map((doc: any) => doc.content)
    .join('\n\n');
  
  // 3. Create augmented prompt
  const prompt = `Answer the question based on the following context:

Context:
${context}

Question: ${userQuestion}

Answer:`;
  
  // 4. Generate answer with LLM
  const response = await openai.chat.completions.create({
    model: 'gpt-4',
    messages: [{ role: 'user', content: prompt }],
  });
  
  return response.choices[0].message.content || 'No answer generated';
}

// Usage
const answer = await ragQuery('What databases support document storage?');
console.log(answer);
// "Based on the context, MongoDB is a flexible document database that supports document storage."
```

---

## Use Cases

### 1. Semantic Search Engine

**Scenario:** Search documentation by meaning, not keywords.

**Example:**

```typescript
async function searchDocs(query: string) {
  const results = await semanticSearch(query, 10);
  
  return results.map((doc: any) => ({
    title: doc.content.substring(0, 50) + '...',
    content: doc.content,
    relevance: (1 - doc.distance).toFixed(2), // Convert distance to similarity %
  }));
}

// Query: "How to store images?"
// Returns docs about: BLOB storage, file uploads, S3, even if they don't contain "images"
```

---

### 2. Recommendation System

**Scenario:** Recommend similar products based on description embeddings.

```typescript
const products = pgTable('products', {
  id: serial('id').primaryKey(),
  name: text('name').notNull(),
  description: text('description').notNull(),
  embedding: vector('embedding', { dimensions: 1536 }),
});

async function recommendSimilarProducts(productId: number, limit: number = 5) {
  // Get product embedding
  const product = await db.execute(sql`
    SELECT embedding FROM products WHERE id = ${productId}
  `);
  
  if (product.rows.length === 0) return [];
  
  const productEmbedding = product.rows[0].embedding;
  
  // Find similar products (exclude self)
  const results = await db.execute(sql`
    SELECT 
      id,
      name,
      description,
      embedding <=> ${productEmbedding}::vector AS distance
    FROM products
    WHERE id != ${productId}
    ORDER BY distance
    LIMIT ${limit}
  `);
  
  return results.rows;
}
```

---

### 3. AI Chatbot with Context (RAG)

**Scenario:** Build a chatbot that answers questions about your company's knowledge base.

**Steps:**

1. Chunk and embed all documents (FAQs, docs, emails)
2. Store in pgvector
3. On user query: retrieve relevant chunks, send to LLM with context

**Example:**

```typescript
async function chatbot(userMessage: string): Promise<string> {
  // Find relevant knowledge base articles
  const relevantDocs = await semanticSearch(userMessage, 5);
  
  // Build context
  const context = relevantDocs
    .map((doc: any, i: number) => `[${i + 1}] ${doc.content}`)
    .join('\n\n');
  
  // Query LLM with context
  const response = await openai.chat.completions.create({
    model: 'gpt-4',
    messages: [
      { role: 'system', content: 'You are a helpful assistant. Answer questions using the provided context.' },
      { role: 'user', content: `Context:\n${context}\n\nQuestion: ${userMessage}` },
    ],
  });
  
  return response.choices[0].message.content!;
}
```

---

## Performance Best Practices

### 1. Choose the Right Index

```sql
-- < 1M vectors: IVFFlat
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 100);

-- 1M-10M vectors: HNSW
CREATE INDEX ON documents USING hnsw (embedding vector_cosine_ops);
```

---

### 2. Tune IVFFlat Lists

```sql
-- Too few lists (10): Fast queries but poor accuracy
-- Too many lists (10000): High accuracy but slow queries
-- Sweet spot: sqrt(rows)

-- For 100K rows
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops)
WITH (lists = 316);  -- sqrt(100000) ≈ 316
```

---

### 3. Use Approximate Search for Large Datasets

```sql
-- Exact search (slow for > 10K rows)
SELECT * FROM documents
ORDER BY embedding <=> '[0.1, 0.2]'
LIMIT 10;

-- Approximate search with IVFFlat (fast)
SET ivfflat.probes = 10;  -- Search 10 clusters (higher = more accurate but slower)
SELECT * FROM documents
ORDER BY embedding <=> '[0.1, 0.2]'
LIMIT 10;
```

---

### 4. Batch Insert Embeddings

```typescript
// ❌ Slow: Insert one at a time
for (const doc of documents) {
  const embedding = await generateEmbedding(doc.content);
  await db.insert(documents).values({ content: doc.content, embedding });
}

// ✅ Fast: Batch generate + batch insert
const embeddings = await Promise.all(
  documents.map(doc => generateEmbedding(doc.content))
);

await db.insert(documents).values(
  documents.map((doc, i) => ({
    content: doc.content,
    embedding: JSON.stringify(embeddings[i]),
  }))
);
```

---

## Common Pitfalls

### ❌ Dimension Mismatch

```sql
-- Error: Embedding has wrong dimensions
CREATE TABLE docs (embedding vector(1536));

INSERT INTO docs VALUES ('[0.1, 0.2, 0.3]');  -- Error: 3 dimensions != 1536
```

**Solution:** Ensure all embeddings have same dimensions as schema.

---

### ❌ Creating Index Before Inserting Data

```sql
-- Wrong order: Index first
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops);
INSERT INTO documents VALUES (...);  -- Slow

-- Correct order: Insert first, then index
INSERT INTO documents VALUES (...);
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops);
```

---

### ❌ Not Using Indexes

```sql
-- Without index: Scans all 1M rows (slow)
SELECT * FROM documents
ORDER BY embedding <=> '[0.1, 0.2]'
LIMIT 10;

-- With index: Searches approximate neighbors (fast)
CREATE INDEX ON documents USING ivfflat (embedding vector_cosine_ops);
```

---

### ❌ Mixing Distance Operators

```sql
-- Index created with cosine
CREATE INDEX ON docs USING ivfflat (embedding vector_cosine_ops);

-- Query uses L2 distance (won't use index!)
SELECT * FROM docs ORDER BY embedding <-> '[0.1, 0.2]';  -- Slow

-- Fix: Use same operator as index
SELECT * FROM docs ORDER BY embedding <=> '[0.1, 0.2]';  -- Fast
```

---

## Summary

| Feature | Description |
|---------|-------------|
| **pgvector** | PostgreSQL extension for vector similarity search |
| **Embeddings** | Numerical representations of data (text, images) |
| **Similarity Metrics** | Cosine (`<=>`), Euclidean (`<->`), Inner Product (`<#>`) |
| **Indexing** | IVFFlat (< 1M vectors), HNSW (1M-10M vectors) |
| **Use Cases** | Semantic search, RAG, recommendations, image similarity |
| **Drizzle Integration** | `vector('embedding', { dimensions: 1536 })` |

---

## Next Steps

1. ✅ Set up PostgreSQL with pgvector extension
2. ✅ Generate embeddings with OpenAI API
3. ✅ Store embeddings in PostgreSQL
4. ✅ Create similarity search queries
5. ✅ Add IVFFlat or HNSW index
6. ✅ Build RAG application with TypeScript
7. 📚 Read [pgvector Documentation](https://github.com/pgvector/pgvector)

---

**Happy vector searching! 🚀**
