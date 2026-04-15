# Quiz 11 - Time Series & Search Engines

**Due:** Thursday, April 9, 2026
**Topics:** TimescaleDB, Elasticsearch, Inverted Indices, Aggregations

---

## Instructions

Answer all 12 questions. Each question has **one correct answer** unless otherwise specified. Explanations are provided for each option.

---

## Questions

### 1. What is the primary advantage of TimescaleDB over plain PostgreSQL for time-series data?

**A)** Full ACID compliance  
**B)** Automatic time-based partitioning (hypertables)  
**C)** Support for JSON columns  
**D)** Built-in authentication

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) Automatic time-based partitioning (hypertables)** - TimescaleDB automatically partitions data into **chunks** based on time intervals (e.g., 7-day chunks). This dramatically improves query performance for time-range queries and enables efficient data retention policies.

- ❌ **A) Full ACID compliance** - PostgreSQL already has full ACID compliance. TimescaleDB inherits this but doesn't add anything new in this area.

- ❌ **C) Support for JSON columns** - PostgreSQL supports JSONB natively. TimescaleDB doesn't add new JSON features.

- ❌ **D) Built-in authentication** - TimescaleDB uses PostgreSQL's authentication system. It doesn't have its own authentication mechanism.

**Key Concept:** Hypertables are TimescaleDB's core feature. They look like regular tables but are automatically partitioned into time-based chunks for optimal performance.

</details>

---

### 2. Which TimescaleDB function is used to downsample time-series data into fixed intervals?

**A)** `date_trunc()`  
**B)** `time_bucket()`  
**C)** `group_by_time()`  
**D)** `aggregate_time()`

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) time_bucket()** - This TimescaleDB function groups timestamps into fixed intervals (e.g., 1 hour, 1 day). Example: `SELECT time_bucket('1 hour', time) AS hour, AVG(temperature) FROM sensor_data GROUP BY hour`.

- ❌ **A) date_trunc()** - This is a PostgreSQL function that truncates timestamps to a precision (e.g., hour, day). While it can be used for grouping, `time_bucket()` is more flexible (supports arbitrary intervals like '5 minutes').

- ❌ **C) group_by_time()** - This function doesn't exist in TimescaleDB or PostgreSQL.

- ❌ **D) aggregate_time()** - This function doesn't exist.

**Key Concept:** `time_bucket()` is essential for downsampling. It allows queries like "average temperature per 15 minutes" with `time_bucket('15 minutes', time)`.

</details>

---

### 3. What happens when you compress a hypertable in TimescaleDB?

**A)** Data is deleted permanently  
**B)** Data becomes read-only and is compressed using columnar storage  
**C)** Data is moved to a separate archive database  
**D)** Queries become slower but storage is reduced

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) Data becomes read-only and is compressed using columnar storage** - Compression converts chunks to **columnar format** (similar to Parquet), achieving 10x-20x compression. Compressed chunks cannot be updated or deleted (read-only), but queries remain fast.

- ❌ **A) Data is deleted permanently** - Compression doesn't delete data; it just changes the storage format.

- ❌ **C) Data is moved to a separate archive database** - Data stays in the same hypertable, just stored differently.

- ❌ **D) Queries become slower but storage is reduced** - Queries are often **faster** on compressed data due to reduced I/O and columnar format optimizations.

**Key Concept:** Compression is configured with `ALTER TABLE ... SET (timescaledb.compress)` and automated with `add_compression_policy()`. Example: compress data older than 7 days.

</details>

---

### 4. What is a continuous aggregate in TimescaleDB?

**A)** A real-time aggregation query  
**B)** A materialized view that automatically updates with new data  
**C)** A temporary table for aggregations  
**D)** A function that calculates rolling averages

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) A materialized view that automatically updates with new data** - Continuous aggregates are **materialized views** that incrementally update as new data arrives. They precompute aggregations (e.g., hourly averages) for fast queries.

- ❌ **A) A real-time aggregation query** - Continuous aggregates are **precomputed**, not real-time. They're refreshed periodically (e.g., every hour).

- ❌ **C) A temporary table for aggregations** - They're materialized views, not temporary tables.

- ❌ **D) A function that calculates rolling averages** - They're views, not functions.

**Key Concept:** Example: `CREATE MATERIALIZED VIEW hourly_avg WITH (timescaledb.continuous) AS SELECT time_bucket('1 hour', time), AVG(temp) FROM sensor_data GROUP BY 1`.

</details>

---

### 5. Which retention policy setting will automatically delete data older than 90 days?

**A)** `add_compression_policy('sensor_data', INTERVAL '90 days')`  
**B)** `add_retention_policy('sensor_data', INTERVAL '90 days')`  
**C)** `set_retention('sensor_data', 90)`  
**D)** `delete_old_data('sensor_data', '90 days')`

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) add_retention_policy('sensor_data', INTERVAL '90 days')** - This TimescaleDB function automatically drops chunks older than 90 days. Full syntax: `SELECT add_retention_policy('sensor_data', INTERVAL '90 days')`.

- ❌ **A) add_compression_policy(...)** - This compresses data, it doesn't delete it.

- ❌ **C) set_retention(...)** - This function doesn't exist.

- ❌ **D) delete_old_data(...)** - This function doesn't exist.

**Key Concept:** Retention policies are crucial for managing storage in time-series databases. They automatically delete old data without manual intervention.

</details>

---

### 6. What is an inverted index in Elasticsearch?

**A)** An index sorted in reverse chronological order  
**B)** A mapping from terms to the documents containing those terms  
**C)** An index that stores documents in reverse order  
**D)** A backup index used for failover

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) A mapping from terms to the documents containing those terms** - An inverted index maps each unique word (term) to the list of documents containing it. Example: "fox" → [Doc1, Doc3]. This enables fast full-text search.

- ❌ **A) An index sorted in reverse chronological order** - "Inverted" refers to the term-to-document mapping, not sorting order.

- ❌ **C) An index that stores documents in reverse order** - Document order is irrelevant; the index maps terms.

- ❌ **D) A backup index used for failover** - This is not related to inverted indices.

**Key Concept:** Inverted indices are the foundation of search engines. They allow queries like "find all documents containing 'wireless mouse'" to execute in milliseconds.

</details>

---

### 7. What is the difference between `text` and `keyword` field types in Elasticsearch?

**A)** `text` is searchable, `keyword` is not  
**B)** `text` is analyzed (tokenized), `keyword` is not (exact match)  
**C)** `text` is faster, `keyword` is slower  
**D)** They are identical

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) text is analyzed (tokenized), keyword is not (exact match)** - `text` fields are processed by an analyzer (tokenization, lowercasing, stemming). `keyword` fields store the exact value for exact-match queries and aggregations.

- ❌ **A) text is searchable, keyword is not** - Both are searchable, but in different ways.

- ❌ **C) text is faster, keyword is slower** - `keyword` queries are often faster for exact matches.

- ❌ **D) They are identical** - They have fundamentally different purposes.

**Key Concept:**
- Use `text` for full-text search (e.g., product descriptions).
- Use `keyword` for exact matches and aggregations (e.g., category filters, tags).

Example:
```json
{
  "name": { "type": "text" },       // Full-text search
  "category": { "type": "keyword" }  // Exact match
}
```

</details>

---

### 8. Which Elasticsearch query type should you use for full-text search with relevance scoring?

**A)** `term`  
**B)** `match`  
**C)** `filter`  
**D)** `exists`

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) match** - The `match` query is used for full-text search. It analyzes the query string and scores documents by relevance. Example: `{ "match": { "description": "wireless mouse" } }`.

- ❌ **A) term** - The `term` query is for **exact matches** on `keyword` fields. It doesn't analyze the query or score by relevance.

- ❌ **C) filter** - `filter` is a clause in `bool` queries, not a query type itself.

- ❌ **D) exists** - The `exists` query checks if a field is present in a document.

**Key Concept:** Use `match` for search bars and fuzzy searches. Use `term` for exact filters like "category = Electronics".

</details>

---

### 9. What does the `bool` query in Elasticsearch allow you to do?

**A)** Convert boolean values to strings  
**B)** Combine multiple query conditions using AND, OR, NOT logic  
**C)** Query boolean fields only  
**D)** Enable/disable search features

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) Combine multiple query conditions using AND, OR, NOT logic** - The `bool` query has four clauses: `must` (AND), `should` (OR), `must_not` (NOT), and `filter` (AND without scoring).

- ❌ **A) Convert boolean values to strings** - `bool` queries have nothing to do with data type conversion.

- ❌ **C) Query boolean fields only** - `bool` queries work with any field types.

- ❌ **D) Enable/disable search features** - This is not the purpose of `bool` queries.

**Key Concept:** Example combining conditions:
```json
{
  "bool": {
    "must": [
      { "match": { "description": "wireless" } }
    ],
    "filter": [
      { "term": { "category": "Electronics" } },
      { "range": { "price": { "lte": 100 } } }
    ],
    "must_not": [
      { "term": { "in_stock": false } }
    ]
  }
}
```

</details>

---

### 10. What is the purpose of analyzers in Elasticsearch?

**A)** Analyze query performance  
**B)** Process text fields during indexing and searching (tokenization, lowercasing, stemming)  
**C)** Analyze cluster health  
**D)** Create analytical reports

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) Process text fields during indexing and searching** - Analyzers break text into tokens, convert to lowercase, remove stopwords, and stem words. Example: "Running Quickly" → ["run", "quick"].

- ❌ **A) Analyze query performance** - Performance analysis is done with profiling tools, not analyzers.

- ❌ **C) Analyze cluster health** - Cluster health is monitored separately.

- ❌ **D) Create analytical reports** - This is not the role of analyzers.

**Key Concept:** Common analyzers:
- **standard**: Tokenize, lowercase, remove stopwords
- **english**: Standard + English stemming ("running" → "run")
- **keyword**: No analysis (exact match)

</details>

---

### 11. Which aggregation would you use to count documents grouped by a field (e.g., count products per category)?

**A)** `stats`  
**B)** `terms`  
**C)** `histogram`  
**D)** `avg`

<details>
<summary>View Answer</summary>

**Correct Answer: B**

**Explanations:**

- ✅ **B) terms** - The `terms` aggregation groups documents by field values and counts each group. Example: `{ "aggs": { "categories": { "terms": { "field": "category" } } } }`.

- ❌ **A) stats** - The `stats` aggregation calculates min, max, avg, sum for numeric fields.

- ❌ **C) histogram** - The `histogram` aggregation buckets numeric values into intervals (e.g., price ranges).

- ❌ **D) avg** - The `avg` aggregation calculates averages, not counts per group.

**Key Concept:** `terms` aggregation is equivalent to SQL's `GROUP BY`. Result example:
```json
{
  "buckets": [
    { "key": "Electronics", "doc_count": 25 },
    { "key": "Furniture", "doc_count": 10 }
  ]
}
```

</details>

---

### 12. When should you use the `filter` context instead of `must` in a `bool` query?

**A)** When you need relevance scoring  
**B)** When you want to exclude documents  
**C)** When you don't need relevance scoring (exact matches, faster)  
**D)** When querying text fields

<details>
<summary>View Answer</summary>

**Correct Answer: C**

**Explanations:**

- ✅ **C) When you don't need relevance scoring (exact matches, faster)** - The `filter` clause skips scoring, making queries faster. Use it for exact matches (e.g., category, price range, boolean flags). Filtered queries are also cached for better performance.

- ❌ **A) When you need relevance scoring** - If you need scoring, use `must` or `should`.

- ❌ **B) When you want to exclude documents** - Use `must_not` for exclusions.

- ❌ **D) When querying text fields** - Both `must` and `filter` can query text fields, but `filter` skips scoring.

**Key Concept:** Performance optimization:
```json
// ❌ Slower: calculates score for category/price
{
  "bool": {
    "must": [
      { "term": { "category": "Electronics" } },
      { "range": { "price": { "lte": 100 } } }
    ]
  }
}

// ✅ Faster: skips scoring for filters
{
  "bool": {
    "filter": [
      { "term": { "category": "Electronics" } },
      { "range": { "price": { "lte": 100 } } }
    ]
  }
}
```

</details>

---

## Answer Key

| Question | Correct Answer |
|----------|----------------|
| 1 | B |
| 2 | B |
| 3 | B |
| 4 | B |
| 5 | B |
| 6 | B |
| 7 | B |
| 8 | B |
| 9 | B |
| 10 | B |
| 11 | B |
| 12 | C |

---

## Grading Scale

- **12 correct**: A+ (100%)
- **11 correct**: A (92%)
- **10 correct**: B+ (83%)
- **9 correct**: B (75%)
- **8 correct**: C+ (67%)
- **7 correct**: C (58%)
- **Below 7**: Needs review

---

## Key Takeaways

### TimescaleDB

1. **Hypertables** automatically partition time-series data into chunks
2. **time_bucket()** downsamples data into fixed intervals
3. **Compression** achieves 10x-20x storage reduction with columnar format
4. **Continuous aggregates** precompute aggregations for fast queries
5. **Retention policies** automatically delete old data

### Elasticsearch

6. **Inverted indices** map terms to documents for fast full-text search
7. **text** fields are analyzed (full-text), **keyword** fields are exact-match
8. **match** queries score by relevance, **term** queries are exact matches
9. **bool** queries combine conditions (must, should, must_not, filter)
10. **Analyzers** tokenize, lowercase, and stem text
11. **terms** aggregation groups documents (equivalent to SQL GROUP BY)
12. **filter** context skips scoring for better performance

---

**Good luck! 🚀**
