# Quiz 13: Time Series & Search Engines

## Instructions

This quiz tests your understanding of TimescaleDB (time-series database) and Elasticsearch (search engine), including hypertables, time_bucket(), compression, retention policies, continuous aggregates, inverted indices, mappings, analyzers, Query DSL, and aggregations.

---

!!! quiz
{
"title": "Hypertables",
"question": "What is a hypertable in TimescaleDB?",
"options": ["An abstraction over a partitioned table that automatically splits data into time-based chunks", "A table that stores data in memory for fast access", "A temporary table used for aggregation queries", "A special index type for timestamp columns"],
"answers": ["An abstraction over a partitioned table that automatically splits data into time-based chunks"]
}
!!!

---

```sql
SELECT time_bucket('15 minutes', time) AS bucket,
       AVG(temperature)
FROM sensor_data
GROUP BY bucket;
```

!!! quiz
{
"title": "Downsampling with time_bucket()",
"question": "What does the query above produce?",
"options": ["The average temperature for the entire table", "One row per 15-minute interval with the average temperature in that window", "An error because time_bucket() only accepts hour or day intervals", "The raw temperature readings rounded to the nearest 15 minutes"],
"answers": ["One row per 15-minute interval with the average temperature in that window"]
}
!!!

---

```sql
SELECT * FROM sensor_data
WHERE time > NOW() - INTERVAL '24 hours';
```

!!! quiz
{
"title": "Chunk Pruning",
"question": "When you run the query above, how does TimescaleDB optimize it?",
"options": ["It scans every chunk in the hypertable and filters afterward", "It creates a temporary index on the time column before scanning", "It automatically skips chunks outside the 24-hour window and only reads relevant ones", "It caches the entire hypertable in memory for faster access"],
"answers": ["It automatically skips chunks outside the 24-hour window and only reads relevant ones"]
}
!!!

---

**You want to reduce storage for a sensor_data hypertable that grows by 10 GB per week. Old data is rarely updated.**

!!! quiz
{
"title": "Compression Configuration",
"question": "Which pair of commands enables automatic compression for data older than 7 days?",
"options": ["SELECT compress_hypertable('sensor_data', '7 days');", "CREATE POLICY compress ON sensor_data FOR SELECT USING (time < NOW() - INTERVAL '7 days');", "SELECT add_retention_policy('sensor_data', INTERVAL '7 days');", "ALTER TABLE sensor_data SET (timescaledb.compress); then SELECT add_compression_policy('sensor_data', INTERVAL '7 days');"],
"answers": ["ALTER TABLE sensor_data SET (timescaledb.compress); then SELECT add_compression_policy('sensor_data', INTERVAL '7 days');"]
}
!!!

---

!!! quiz
{
"title": "Compressed Data Limitation",
"question": "What limitation applies to compressed chunks in TimescaleDB?",
"options": ["Compressed chunks are read-only — you must decompress before UPDATE or DELETE", "Compressed chunks cannot be queried with SELECT statements", "Compressed chunks lose their indexes permanently", "Compressed chunks can only store integer columns"],
"answers": ["Compressed chunks are read-only — you must decompress before UPDATE or DELETE"]
}
!!!

---

**A dashboard shows the average temperature per hour for the past 30 days. The query takes 8 seconds on each page load.**

!!! quiz
{
"title": "Continuous Aggregates",
"question": "Which TimescaleDB feature best addresses this performance problem?",
"options": ["Add a B-tree index on (time, sensor_id)", "Create a continuous aggregate that precomputes hourly averages", "Increase the chunk interval from 7 days to 30 days", "Switch the column type from TIMESTAMPTZ to TIMESTAMP"],
"answers": ["Create a continuous aggregate that precomputes hourly averages"]
}
!!!

---

!!! quiz
{
"title": "Retention vs Compression",
"question": "What is the difference between a retention policy and a compression policy in TimescaleDB?",
"options": ["Retention compresses old data; compression deletes old data", "They are the same — both remove old data", "Retention deletes old chunks after a given age; compression converts old chunks to columnar format to save space", "Retention converts chunks to read-only; compression moves chunks to a separate archive"],
"answers": ["Retention deletes old chunks after a given age; compression converts old chunks to columnar format to save space"]
}
!!!

---

```sql
SELECT time_bucket_gapfill('1 hour', time) AS hour,
       sensor_id,
       AVG(temperature) AS avg_temp
FROM sensor_data
WHERE time > NOW() - INTERVAL '24 hours'
  AND sensor_id = 1
GROUP BY hour, sensor_id
ORDER BY hour;
```

!!! quiz
{
"title": "Gap Filling",
"question": "What does time_bucket_gapfill() do differently from time_bucket()?",
"options": ["It removes duplicate timestamps within the same bucket", "It rounds timestamps to the nearest bucket boundary instead of truncating", "It automatically interpolates missing values using linear regression", "It fills in rows for time intervals that have no data, returning NULL for missing measurements"],
"answers": ["It fills in rows for time intervals that have no data, returning NULL for missing measurements"]
}
!!!

---

!!! quiz
{
"title": "TimescaleDB and SQL",
"question": "Which statement about TimescaleDB is TRUE?",
"options": ["TimescaleDB is a PostgreSQL extension — all standard SQL queries and tools work unchanged", "TimescaleDB requires learning a new query language separate from SQL", "TimescaleDB is a standalone database that replaces PostgreSQL", "TimescaleDB only supports INSERT and SELECT operations, not UPDATE or DELETE"],
"answers": ["TimescaleDB is a PostgreSQL extension — all standard SQL queries and tools work unchanged"]
}
!!!

---

!!! quiz
{
"title": "Time-Series Use Cases",
"question": "Which workload is the BEST fit for TimescaleDB?",
"options": ["A social network storing friend relationships and traversals", "An IoT platform ingesting 100,000 sensor readings per second with time-range dashboards", "An e-commerce product catalog with full-text search", "A content management system storing blog posts and user comments"],
"answers": ["An IoT platform ingesting 100,000 sensor readings per second with time-range dashboards"]
}
!!!

---

**Three documents are indexed in Elasticsearch:**

```
Doc 1: "Quick brown fox"
Doc 2: "Brown cat"
Doc 3: "Fox jumps quickly"
```

!!! quiz
{
"title": "Inverted Index Lookup",
"question": "Using the inverted index built from the documents above, which documents match the search query: brown fox?",
"options": ["Doc 1 and Doc 2 — they share the term brown", "No documents — the exact phrase brown fox only appears in Doc 1", "Doc 1, Doc 2, and Doc 3 — each contains at least one of the terms", "Doc 1 only — it contains both words in sequence"],
"answers": ["Doc 1, Doc 2, and Doc 3 — each contains at least one of the terms"]
}
!!!

---

!!! quiz
{
"title": "Elasticsearch vs Relational",
"question": "Which is a key difference between Elasticsearch and a relational database like PostgreSQL?",
"options": ["Elasticsearch supports JOINs across tables; PostgreSQL does not", "Elasticsearch requires a fixed schema; PostgreSQL supports dynamic schemas", "Elasticsearch supports full ACID transactions; PostgreSQL does not", "Elasticsearch uses inverted indices for fast full-text search; PostgreSQL uses B-tree indices optimized for exact lookups"],
"answers": ["Elasticsearch uses inverted indices for fast full-text search; PostgreSQL uses B-tree indices optimized for exact lookups"]
}
!!!

---

!!! quiz
{
"title": "Field Type Selection",
"question": "A products index has a category field used for filtering (e.g., show only Electronics). Which field type should you use?",
"options": ["keyword — for exact-match filtering and aggregations without analysis", "text — so the category can be searched with partial matches", "object — to allow nested category hierarchies", "integer — categories should be stored as numeric codes"],
"answers": ["keyword — for exact-match filtering and aggregations without analysis"]
}
!!!

---

**The input text is: \"The Quick BROWN Foxes are Running\"**

!!! quiz
{
"title": "Analyzer Processing",
"question": "After the standard English analyzer processes the text above, which tokens are produced?",
"options": ["the quick brown foxes are running", "quick, brown, fox, run", "The, Quick, BROWN, Foxes, are, Running", "Quick, BROWN, Foxes, Running"],
"answers": ["quick, brown, fox, run"]
}
!!!

---

```json
{
  "query": {
    "bool": {
      "must": [{ "match": { "description": "wireless" } }],
      "filter": [{ "term": { "category": "Electronics" } }, { "range": { "price": { "lte": 50 } } }],
      "must_not": [{ "term": { "in_stock": false } }]
    }
  }
}
```

!!! quiz
{
"title": "Bool Query Interpretation",
"question": "Which products does the query above match?",
"options": ["All products that are either wireless or in the Electronics category", "Electronics products regardless of stock status if the price is under $50", "Only products where the description is exactly the word wireless", "In-stock Electronics under $50 whose description contains wireless"],
"answers": ["In-stock Electronics under $50 whose description contains wireless"]
}
!!!

---

!!! quiz
{
"title": "filter vs must",
"question": "Why should you use filter instead of must for exact-match conditions like category or price range?",
"options": ["filter supports regex patterns while must does not", "filter allows multiple values per field while must allows only one", "filter skips relevance scoring and is cached, making it faster than must", "filter runs after must, so it reduces the result set more efficiently"],
"answers": ["filter skips relevance scoring and is cached, making it faster than must"]
}
!!!

---

```json
{ "term": { "description": "Wireless Mouse" } }
```

!!! quiz
{
"title": "term vs match",
"question": "A developer runs the term query above on a text field. What happens?",
"options": ["It returns no results because text fields are analyzed but term queries are not — the stored tokens are lowercase", "It finds all products whose description contains wireless or mouse", "It performs a fuzzy search allowing typos in Wireless Mouse", "It returns an error because term queries cannot be used on text fields"],
"answers": ["It returns no results because text fields are analyzed but term queries are not — the stored tokens are lowercase"]
}
!!!

---

```json
{
  "size": 0,
  "aggs": {
    "price_ranges": {
      "histogram": {
        "field": "price",
        "interval": 50
      }
    }
  }
}
```

!!! quiz
{
"title": "Histogram Aggregation",
"question": "What does the aggregation above produce?",
"options": ["A sorted list of all unique prices in the index", "Buckets of products grouped into $50 price ranges (0-50, 50-100, 100-150, etc.) with document counts", "A single average price across all products", "A time-series chart of price changes over the past 50 days"],
"answers": ["Buckets of products grouped into $50 price ranges (0-50, 50-100, 100-150, etc.) with document counts"]
}
!!!

---

```json
{ "multi_match": { "query": "keyboard", "fields": ["name^3", "description"] } }
```

!!! quiz
{
"title": "Multi-Match and Boosting",
"question": "What does the multi_match query above do?",
"options": ["Returns exactly 3 results from the name field", "Requires the word keyboard to appear at least 3 times in the name field", "Searches both name and description, but matches in name are weighted 3× higher in the relevance score", "Searches only the name field and ignores description"],
"answers": ["Searches both name and description, but matches in name are weighted 3× higher in the relevance score"]
}
!!!

---

!!! quiz
{
"title": "Elasticsearch Consistency Model",
"question": "Which statement about Elasticsearch's consistency model is correct?",
"options": ["Elasticsearch guarantees strict serializability across all nodes", "Elasticsearch provides full ACID transactions like PostgreSQL", "Elasticsearch uses two-phase commit for every write operation", "Elasticsearch is eventually consistent — newly indexed documents may not be immediately searchable"],
"answers": ["Elasticsearch is eventually consistent — newly indexed documents may not be immediately searchable"]
}
!!!
