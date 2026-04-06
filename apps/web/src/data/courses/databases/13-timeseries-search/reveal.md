# Week 13: Time Series & Search Engines

TimescaleDB · Elasticsearch · Hypertables · Inverted Indices · Query DSL

---

## Agenda

- Time-Series Data Paradigm
- TimescaleDB Architecture — Hypertables & Chunks
- Creating Hypertables & Inserting Data
- Querying — time_bucket() & Downsampling
- Gap Filling — time_bucket_gapfill()
- Compression — Columnar Storage
- Retention Policies — Automatic Cleanup
- Continuous Aggregates — Materialized Analytics
- Search Engine Paradigm
- Elasticsearch Architecture — Inverted Indices
- Documents, Indices & Mappings
- Analyzers & Tokenizers
- Query DSL — match, term, bool, range
- Advanced Queries — Fuzzy, Prefix, Multi-Match
- Aggregations — terms, stats, histogram
- Docker Setup — TimescaleDB & Elasticsearch
- TypeScript Integration
- Common Pitfalls & Best Practices
- Decision Framework & Key Takeaways

---

## Part 1: Time-Series Databases

A Different Kind of Workload

---

## What is Time-Series Data?

Every record has three components:

1. **Timestamp** — when the event occurred
2. **Measurements** — numeric values (temperature, price, CPU %)
3. **Metadata** — contextual info (device_id, location, user_id)

💡 If your data has a timestamp and you mostly query by time ranges — you have time-series data.

---

## Examples of Time-Series Data

| Domain                     | Data Type                 | Volume                    |
| -------------------------- | ------------------------- | ------------------------- |
| **IoT Sensors**            | Temperature every 10s     | Millions/day              |
| **Stock Market**           | Price ticks per ms        | Billions/day              |
| **Application Monitoring** | CPU / memory every minute | Hundreds of thousands/day |
| **Website Analytics**      | Page views per hour       | Thousands/day             |
| **Financial Transactions** | Trades per microsecond    | Billions/day              |

---

## The Challenge: Scale

Imagine 10,000 sensors reporting every 10 seconds:

```
10,000 sensors × 6 readings/min × 60 min × 24 hr = 864,000,000 rows/day
```

After one year: **~315 billion rows**.

Plain PostgreSQL grinds to a halt. You need specialized tooling.

---

## What is TimescaleDB?

- A **PostgreSQL extension** — not a separate database
- All standard SQL works unchanged (JOINs, CTEs, window functions)
- Adds specialized features for time-series workloads
- Full **ACID** guarantees (it is PostgreSQL under the hood)

```mermaid
flowchart LR
    subgraph PG["PostgreSQL"]
        TS["TimescaleDB Extension"]
        SQL["Standard SQL Engine"]
        TS --- SQL
    end
    APP["Your Application"] -->|"SQL"| PG
```

💡 You don't learn a new query language — you learn new functions and features.

---

## TimescaleDB vs Plain PostgreSQL

| Aspect                 | PostgreSQL (Plain)        | TimescaleDB                   |
| ---------------------- | ------------------------- | ----------------------------- |
| **Time-Range Queries** | Slow for billions of rows | Fast (partitioned by time)    |
| **Data Retention**     | Manual DELETE statements  | Automatic retention policies  |
| **Aggregations**       | Expensive recalculation   | Continuous aggregates         |
| **Compression**        | Basic TOAST compression   | Columnar compression (10–20×) |
| **Downsampling**       | Manual queries            | Built-in time_bucket()        |
| **Inserts**            | Good                      | Optimized for high throughput |

---

## Part 2: TimescaleDB Architecture

Hypertables, Chunks & Automatic Partitioning

---

## Hypertables

A **hypertable** is TimescaleDB's core abstraction:

- Looks and behaves like a **single table**
- Internally split into **time-based chunks** (partitions)
- Chunks are created automatically as data arrives

```mermaid
flowchart TD
    HT["Hypertable: sensor_data<br/>(single logical table)"]
    HT --> C1["Chunk 1<br/>Jan 1–7"]
    HT --> C2["Chunk 2<br/>Jan 8–14"]
    HT --> C3["Chunk 3<br/>Jan 15–21"]
    HT --> C4["Chunk 4<br/>Jan 22–31"]
    HT --> C5["Chunk 5<br/>Feb 1–7"]
    style HT fill:#3498db,color:#fff
```

💡 Each chunk stores a 7-day interval by default. You query the hypertable — TimescaleDB routes to the right chunks.

---

## Chunks

**Chunks** are the internal partitions of a hypertable. TimescaleDB automatically:

- **Creates** new chunks as data arrives
- **Routes** INSERTs to the correct chunk
- **Prunes** chunks during SELECT (only reads relevant time ranges)

```mermaid
flowchart LR
    Q["SELECT * FROM sensor_data<br/>WHERE time > NOW() - '24h'"]
    Q --> C4["Chunk 4<br/>Jan 22–31<br/>✅ Read"]
    Q --> C5["Chunk 5<br/>Feb 1–7<br/>✅ Read"]
    Q -.->|"Skipped"| C1["Chunk 1<br/>Jan 1–7<br/>❌"]
    Q -.->|"Skipped"| C2["Chunk 2<br/>Jan 8–14<br/>❌"]
    Q -.->|"Skipped"| C3["Chunk 3<br/>Jan 15–21<br/>❌"]
    style C4 fill:#2ecc71,color:#fff
    style C5 fill:#2ecc71,color:#fff
    style C1 fill:#95a5a6,color:#fff
    style C2 fill:#95a5a6,color:#fff
    style C3 fill:#95a5a6,color:#fff
```

---

## Chunk Pruning

When you query a time range, TimescaleDB checks each chunk's boundaries against your WHERE clause:

| Chunk   | Time Range | Matches `time > NOW() - '24h'`? |
| ------- | ---------- | ------------------------------- |
| Chunk 1 | Jan 1–7    | ❌ Skip                         |
| Chunk 2 | Jan 8–14   | ❌ Skip                         |
| Chunk 3 | Jan 15–21  | ❌ Skip                         |
| Chunk 4 | Jan 22–31  | ✅ Read                         |
| Chunk 5 | Feb 1–7    | ✅ Read                         |

Even with billions of rows, the query only touches the chunks that contain relevant data.

---

## View Your Chunks

```sql
-- List all chunks for a hypertable
SELECT chunk_name, range_start, range_end
FROM timescaledb_information.chunks
WHERE hypertable_name = 'sensor_data'
ORDER BY range_start DESC;
```

| chunk_name        | range_start | range_end  |
| ----------------- | ----------- | ---------- |
| \_hyper_1_5_chunk | 2026-02-01  | 2026-02-08 |
| \_hyper_1_4_chunk | 2026-01-22  | 2026-01-29 |
| \_hyper_1_3_chunk | 2026-01-15  | 2026-01-22 |

---

## Part 3: Creating Hypertables

Setting Up Your First Time-Series Table

---

## Step 1: Enable the Extension

```sql
-- Enable TimescaleDB (once per database)
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Verify installation
SELECT extversion FROM pg_extension
WHERE extname = 'timescaledb';
```

---

## Step 2: Create a Regular Table

```sql
CREATE TABLE sensor_data (
  time        TIMESTAMPTZ NOT NULL,
  sensor_id   INT         NOT NULL,
  temperature FLOAT,
  humidity    FLOAT,
  location    TEXT
);
```

This is just a standard PostgreSQL table — nothing special yet.

---

## Step 3: Convert to Hypertable

```sql
-- Single command converts it to a hypertable
SELECT create_hypertable('sensor_data', 'time');
```

That's it! TimescaleDB now manages partitioning automatically.

**Optional: Set a custom chunk interval**

```sql
SELECT create_hypertable(
  'sensor_data', 'time',
  chunk_time_interval => INTERVAL '1 day'
);
```

💡 Use smaller chunks for high-volume data (1 day), larger chunks for low-volume (7 days default).

---

## Multi-Dimensional Partitioning

Partition by **time AND space** for even better performance:

```sql
SELECT create_hypertable(
  'sensor_data', 'time',
  partitioning_column => 'sensor_id',
  number_partitions => 4
);
```

This creates chunks for each combination of time range and sensor_id hash — useful when you have millions of unique sensors.

---

## Inserting Data

Inserts work exactly like standard PostgreSQL:

```sql
-- Single insert
INSERT INTO sensor_data (time, sensor_id, temperature, humidity, location)
VALUES (NOW(), 1, 22.5, 65.0, 'Office A');

-- Bulk insert (recommended for high throughput)
INSERT INTO sensor_data (time, sensor_id, temperature, humidity, location)
VALUES
  ('2026-04-01 10:00:00', 1, 22.5, 65.0, 'Office A'),
  ('2026-04-01 10:00:00', 2, 23.1, 60.0, 'Office B'),
  ('2026-04-01 10:05:00', 1, 22.7, 64.5, 'Office A'),
  ('2026-04-01 10:05:00', 2, 23.3, 59.8, 'Office B');
```

💡 **Best practice**: batch thousands of rows per INSERT for maximum throughput.

---

## Part 4: Querying Time-Series Data

time_bucket(), Downsampling & Aggregations

---

## Time-Range Queries

```sql
-- Last 24 hours
SELECT * FROM sensor_data
WHERE time > NOW() - INTERVAL '24 hours'
ORDER BY time DESC;

-- Specific date range
SELECT * FROM sensor_data
WHERE time BETWEEN '2026-04-01' AND '2026-04-07'
  AND sensor_id = 1;
```

TimescaleDB **automatically prunes chunks** outside the time range — even with billions of rows, only relevant chunks are scanned.

---

## time_bucket() — The Key Function

`time_bucket()` groups timestamps into fixed-width intervals:

```sql
SELECT time_bucket('1 hour', time) AS hour,
       sensor_id,
       AVG(temperature) AS avg_temp,
       MAX(temperature) AS max_temp,
       MIN(temperature) AS min_temp
FROM sensor_data
WHERE time > NOW() - INTERVAL '7 days'
GROUP BY hour, sensor_id
ORDER BY hour DESC;
```

**Result:**

| hour             | sensor_id | avg_temp | max_temp | min_temp |
| ---------------- | --------- | -------- | -------- | -------- |
| 2026-04-01 10:00 | 1         | 22.6     | 23.0     | 22.2     |
| 2026-04-01 10:00 | 2         | 23.2     | 23.5     | 22.9     |
| 2026-04-01 11:00 | 1         | 22.8     | 23.2     | 22.4     |

---

## Common Bucket Intervals

```sql
time_bucket('1 second', time)   -- Real-time monitoring
time_bucket('1 minute', time)   -- High-frequency dashboards
time_bucket('5 minutes', time)  -- Typical IoT dashboards
time_bucket('1 hour', time)     -- Hourly reports
time_bucket('1 day', time)      -- Daily summaries
time_bucket('1 week', time)     -- Weekly trends
```

💡 `time_bucket()` is more flexible than `date_trunc()` — it supports arbitrary intervals like 5 minutes or 15 minutes.

---

## Downsampling: Raw → Summary

```mermaid
flowchart LR
    RAW["Raw Data<br/>1 reading / 10 sec<br/>8,640 rows/day/sensor"]
    MIN["5-Minute Buckets<br/>288 rows/day/sensor"]
    HOUR["1-Hour Buckets<br/>24 rows/day/sensor"]
    DAY["1-Day Buckets<br/>1 row/day/sensor"]
    RAW -->|"time_bucket('5 min')"| MIN
    MIN -->|"time_bucket('1 hour')"| HOUR
    HOUR -->|"time_bucket('1 day')"| DAY
```

Downsampling reduces data volume while preserving trends — essential for dashboards that show weeks or months of data.

---

## Part 5: Gap Filling

Handling Missing Data

---

## The Problem

Sensor 1 went offline for 2 hours. Your dashboard shows a gap:

| hour             | avg_temp |
| ---------------- | -------- |
| 2026-04-01 08:00 | 22.5     |
| 2026-04-01 09:00 | 22.8     |
| _(missing)_      | _(gap)_  |
| _(missing)_      | _(gap)_  |
| 2026-04-01 12:00 | 23.1     |

---

## time_bucket_gapfill() to the Rescue

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

**Result with gaps filled:**

| hour             | avg_temp |
| ---------------- | -------- |
| 2026-04-01 08:00 | 22.5     |
| 2026-04-01 09:00 | 22.8     |
| 2026-04-01 10:00 | NULL     |
| 2026-04-01 11:00 | NULL     |
| 2026-04-01 12:00 | 23.1     |

Missing intervals now appear with NULL values — no more gaps in charts.

---

## Last Observation Carried Forward (LOCF)

Fill gaps with the **previous known value**:

```sql
SELECT time_bucket_gapfill('1 hour', time) AS hour,
       sensor_id,
       COALESCE(
         AVG(temperature),
         locf(AVG(temperature))
       ) AS avg_temp
FROM sensor_data
WHERE time > NOW() - INTERVAL '24 hours'
  AND sensor_id = 1
GROUP BY hour, sensor_id
ORDER BY hour;
```

| hour             | avg_temp |
| ---------------- | -------- |
| 2026-04-01 08:00 | 22.5     |
| 2026-04-01 09:00 | 22.8     |
| 2026-04-01 10:00 | **22.8** |
| 2026-04-01 11:00 | **22.8** |
| 2026-04-01 12:00 | 23.1     |

💡 `locf()` stands for "Last Observation Carried Forward" — it repeats the last known value.

---

## Part 6: Compression

10–20× Storage Reduction

---

## Why Compression Matters

| Metric           | Without Compression | With Compression |
| ---------------- | ------------------- | ---------------- |
| 1 month of data  | 50 GB               | ~3 GB            |
| 6 months of data | 300 GB              | ~18 GB           |
| 1 year of data   | 600 GB              | ~35 GB           |

TimescaleDB uses **columnar compression** — groups similar values together and compresses them efficiently.

---

## How Columnar Compression Works

```mermaid
flowchart TD
    subgraph ROW["Row Storage (Uncompressed)"]
        R1["time | sensor_id | temp | humidity"]
        R2["time | sensor_id | temp | humidity"]
        R3["time | sensor_id | temp | humidity"]
        R4["time | sensor_id | temp | humidity"]
    end
    subgraph COL["Columnar Storage (Compressed)"]
        CT["time column<br/>(sorted, delta-encoded)"]
        CS["sensor_id column<br/>(run-length encoded)"]
        CTemp["temp column<br/>(gorilla compressed)"]
        CH["humidity column<br/>(gorilla compressed)"]
    end
    ROW -->|"Compress"| COL
```

Columnar storage groups values by column, allowing specialized compression algorithms per data type.

---

## Enable Compression

**Step 1**: Enable compression on the hypertable:

```sql
ALTER TABLE sensor_data SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'sensor_id',
  timescaledb.compress_orderby = 'time DESC'
);
```

**Parameters:**

- `compress_segmentby` — group rows by this column (e.g., sensor_id)
- `compress_orderby` — sort order within groups (usually `time DESC`)

---

## Automatic Compression Policy

**Step 2**: Compress chunks older than 7 days automatically:

```sql
SELECT add_compression_policy('sensor_data', INTERVAL '7 days');
```

TimescaleDB runs a background job to compress qualifying chunks.

---

## Manual Compression

```sql
-- Compress a specific chunk
SELECT compress_chunk('_timescaledb_internal._hyper_1_2_chunk');

-- Compress all chunks older than 7 days
SELECT compress_chunk(c.chunk_schema || '.' || c.chunk_name)
FROM timescaledb_information.chunks c
WHERE c.hypertable_name = 'sensor_data'
  AND c.range_end < NOW() - INTERVAL '7 days';
```

---

## Check Compression Stats

```sql
SELECT chunk_name,
       before_compression_total_bytes,
       after_compression_total_bytes,
       ROUND(before_compression_total_bytes::numeric /
             after_compression_total_bytes, 1) AS ratio
FROM timescaledb_information.compressed_chunk_stats
WHERE hypertable_name = 'sensor_data';
```

| chunk_name        | before_bytes | after_bytes | ratio |
| ----------------- | ------------ | ----------- | ----- |
| \_hyper_1_2_chunk | 1,048 MB     | 52 MB       | 20.0× |
| \_hyper_1_3_chunk | 1,048 MB     | 58 MB       | 18.1× |

20× compression! 🎉

---

## ⚠️ Compressed Chunks Are Read-Only

```sql
-- ❌ Error: cannot UPDATE compressed chunks
UPDATE sensor_data SET temperature = 25
WHERE time < NOW() - INTERVAL '7 days';

-- ✅ Decompress first, then modify
SELECT decompress_chunk('_timescaledb_internal._hyper_1_2_chunk');
UPDATE sensor_data SET temperature = 25
WHERE time BETWEEN '2026-01-01' AND '2026-01-07';
```

💡 SELECT queries work transparently on compressed data — decompression is automatic for reads.

---

## Part 7: Retention Policies

Automatic Data Lifecycle Management

---

## The Problem

Without retention, your database grows forever:

```
Month 1:   50 GB
Month 6:  300 GB
Month 12: 600 GB  ← disk full 💥
```

---

## Add a Retention Policy

Automatically drop chunks older than 90 days:

```sql
SELECT add_retention_policy('sensor_data', INTERVAL '90 days');
```

TimescaleDB runs a background job to drop old chunks — no manual DELETE needed.

---

## Retention + Compression = Best of Both Worlds

```mermaid
flowchart LR
    subgraph HOT["Hot Data (0–7 days)"]
        H["Uncompressed<br/>Fast reads & writes"]
    end
    subgraph WARM["Warm Data (7–90 days)"]
        W["Compressed<br/>10–20× smaller<br/>Read-only"]
    end
    subgraph COLD["Dropped (90+ days)"]
        D["Deleted<br/>Freed disk space"]
    end
    HOT -->|"Compress after 7 days"| WARM
    WARM -->|"Drop after 90 days"| COLD
```

```sql
-- Compress after 7 days
SELECT add_compression_policy('sensor_data', INTERVAL '7 days');

-- Delete after 90 days
SELECT add_retention_policy('sensor_data', INTERVAL '90 days');
```

---

## Manage Retention Policies

```sql
-- View active policies
SELECT * FROM timescaledb_information.jobs
WHERE proc_name = 'policy_retention';

-- Remove a retention policy
SELECT remove_retention_policy('sensor_data');

-- Manual deletion (drop chunks older than 90 days)
SELECT drop_chunks('sensor_data', INTERVAL '90 days');
```

---

## Part 8: Continuous Aggregates

Pre-Computed Analytics

---

## The Problem

A dashboard shows hourly averages for the past 30 days. Every page load recalculates from raw data:

```sql
-- Slow: scans millions of raw rows every time
SELECT time_bucket('1 hour', time) AS hour,
       sensor_id,
       AVG(temperature) AS avg_temp
FROM sensor_data
WHERE time > NOW() - INTERVAL '30 days'
GROUP BY hour, sensor_id;
-- ⏱️ 8 seconds per query
```

With 100 concurrent users: **database meltdown**.

---

## The Solution: Continuous Aggregates

Pre-compute and store the results as a materialized view:

```sql
-- Create continuous aggregate (runs once)
CREATE MATERIALIZED VIEW sensor_data_hourly
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 hour', time) AS hour,
       sensor_id,
       AVG(temperature) AS avg_temp,
       MAX(temperature) AS max_temp,
       MIN(temperature) AS min_temp,
       COUNT(*) AS sample_count
FROM sensor_data
GROUP BY hour, sensor_id;
```

---

## Query the Aggregate

```sql
-- Fast: reads pre-calculated results
SELECT * FROM sensor_data_hourly
WHERE hour > NOW() - INTERVAL '30 days'
  AND sensor_id = 1
ORDER BY hour DESC;
-- ⏱️ 50 ms (160× faster!)
```

---

## Automatic Refresh Policy

Keep the aggregate up-to-date automatically:

```sql
SELECT add_continuous_aggregate_policy(
  'sensor_data_hourly',
  start_offset  => INTERVAL '3 hours',
  end_offset    => INTERVAL '1 hour',
  schedule_interval => INTERVAL '1 hour'
);
```

**Parameters:**

- `start_offset` — how far back to re-aggregate (handles late-arriving data)
- `end_offset` — don't aggregate the most recent data (still arriving)
- `schedule_interval` — how often the refresh job runs

---

## Multi-Level Aggregation

```mermaid
flowchart TD
    RAW["Raw sensor_data<br/>1 reading / 10 sec"]
    HOURLY["sensor_data_hourly<br/>1 row / hour / sensor"]
    DAILY["sensor_data_daily<br/>1 row / day / sensor"]
    RAW -->|"Continuous Aggregate"| HOURLY
    HOURLY -->|"Continuous Aggregate"| DAILY
    style RAW fill:#e74c3c,color:#fff
    style HOURLY fill:#f39c12,color:#fff
    style DAILY fill:#2ecc71,color:#fff
```

```sql
-- Daily aggregate built on top of hourly aggregate
CREATE MATERIALIZED VIEW sensor_data_daily
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 day', hour) AS day,
       sensor_id,
       AVG(avg_temp) AS avg_temp,
       MAX(max_temp) AS max_temp,
       MIN(min_temp) AS min_temp,
       SUM(sample_count) AS total_samples
FROM sensor_data_hourly
GROUP BY day, sensor_id;
```

💡 Stack aggregates to serve dashboards at different zoom levels — seconds, minutes, hours, days.

---

## Part 9: Search Engines

A Different Kind of Database

---

## What is Elasticsearch?

- A **distributed search and analytics engine** built on Apache Lucene
- Optimized for **full-text search**, not transactions
- Uses **inverted indices** instead of B-tree indices
- **Eventually consistent** (not ACID)

💡 If your users need a search bar — you probably need Elasticsearch.

---

## Elasticsearch vs Relational Databases

| Aspect             | Relational DB (PostgreSQL)         | Elasticsearch                   |
| ------------------ | ---------------------------------- | ------------------------------- |
| **Primary Use**    | Transactions, ACID guarantees      | Full-text search, analytics     |
| **Query Type**     | Exact matches (`WHERE name = 'X'`) | Fuzzy search, relevance scoring |
| **Schema**         | Strict, defined upfront            | Dynamic, schema-less            |
| **Joins**          | Supported (INNER, LEFT, etc.)      | Not supported (denormalize)     |
| **Indexing**       | B-tree indices                     | Inverted indices                |
| **ACID**           | Full ACID support                  | Eventually consistent           |
| **Query Language** | SQL                                | Query DSL (JSON)                |

---

## When to Use Elasticsearch

- ✅ **Full-text search** — search bars, autocomplete
- ✅ **Log analysis** — application logs, system logs (ELK stack)
- ✅ **Analytics dashboards** — Kibana visualizations
- ✅ **Product catalogs** — faceted search with filters
- ✅ **Real-time monitoring** — metrics, alerts

---

## When NOT to Use Elasticsearch

- ❌ **Transactional systems** — banking, orders
- ❌ **Strong consistency** — ACID required
- ❌ **Complex joins** — normalized relational data
- ❌ **Primary data store** — use as a secondary search index

💡 Elasticsearch complements your primary database — it doesn't replace it.

---

## Part 10: Inverted Indices

The Secret Behind Fast Search

---

## B-Tree vs Inverted Index

```mermaid
flowchart TD
    subgraph BTREE["B-Tree Index (PostgreSQL)"]
        BT["Sorted keys → row pointers<br/>Fast for: exact match, range"]
    end
    subgraph INV["Inverted Index (Elasticsearch)"]
        IV["Each term → list of documents<br/>Fast for: full-text search"]
    end
```

---

## How an Inverted Index Works

**Three documents:**

```
Doc 1: "Quick brown fox"
Doc 2: "Brown cat"
Doc 3: "Fox jumps over"
```

**Step 1: Tokenize and lowercase**

```
Doc 1: ["quick", "brown", "fox"]
Doc 2: ["brown", "cat"]
Doc 3: ["fox", "jumps", "over"]
```

**Step 2: Build the inverted index**

| Term  | Document IDs |
| ----- | ------------ |
| quick | Doc 1        |
| brown | Doc 1, Doc 2 |
| fox   | Doc 1, Doc 3 |
| cat   | Doc 2        |
| jumps | Doc 3        |
| over  | Doc 3        |

---

## Search: "brown fox"

1. Tokenize the query: `["brown", "fox"]`
2. Look up each term in the inverted index:
   - `brown` → Doc 1, Doc 2
   - `fox` → Doc 1, Doc 3
3. Combine results: **Doc 1, Doc 2, Doc 3**

**Relevance scoring**: Doc 1 scores highest because it contains **both** terms.

```mermaid
flowchart LR
    Q["Query: brown fox"]
    Q --> T1["brown<br/>→ Doc 1, Doc 2"]
    Q --> T2["fox<br/>→ Doc 1, Doc 3"]
    T1 --> R["Results:<br/>Doc 1 ⭐ (2 matches)<br/>Doc 2 (1 match)<br/>Doc 3 (1 match)"]
    T2 --> R
```

---

## Why Inverted Indices Are Fast

| Operation               | B-Tree (PostgreSQL)                   | Inverted Index (Elasticsearch)   |
| ----------------------- | ------------------------------------- | -------------------------------- |
| Find row by exact value | O(log N) — fast ✅                    | O(1) term lookup — fast ✅       |
| Search text "brown fox" | Full table scan — slow ❌             | Two term lookups — fast ✅       |
| Fuzzy search "brwn"     | Not supported ❌                      | Edit-distance matching — fast ✅ |
| Autocomplete "bro..."   | LIKE 'bro%' — slow on large tables ❌ | Prefix trie — fast ✅            |

💡 Inverted indices trade write speed for **extremely fast reads on text data**.

---

## Part 11: Documents, Indices & Mappings

Elasticsearch's Data Model

---

## Core Concepts

| Elasticsearch | Relational DB | Description                    |
| ------------- | ------------- | ------------------------------ |
| **Document**  | Row           | A JSON object                  |
| **Index**     | Table         | A collection of documents      |
| **Mapping**   | Schema        | Field types and analysis rules |
| **Field**     | Column        | A key in the JSON document     |

---

## Documents

A document is a JSON object stored in Elasticsearch:

```json
{
  "_id": "1",
  "_index": "products",
  "_source": {
    "name": "Wireless Mouse",
    "description": "Ergonomic wireless mouse with USB receiver",
    "price": 29.99,
    "category": "Electronics",
    "tags": ["wireless", "mouse", "ergonomic"],
    "in_stock": true,
    "created_at": "2026-04-01T10:00:00Z"
  }
}
```

- `_id` — unique document identifier
- `_index` — which index this document belongs to
- `_source` — the actual data

---

## Indices

An **index** is a collection of documents (like a table):

```bash
# Index naming convention
products          # ✅ lowercase
users-2026        # ✅ with year suffix
logs-app-prod     # ✅ descriptive
Products          # ❌ no uppercase
my index          # ❌ no spaces
```

---

## Mappings

**Mappings** define the schema — which fields exist and their types:

```json
{
  "mappings": {
    "properties": {
      "name": { "type": "text" },
      "description": { "type": "text" },
      "price": { "type": "float" },
      "category": { "type": "keyword" },
      "tags": { "type": "keyword" },
      "in_stock": { "type": "boolean" },
      "created_at": { "type": "date" }
    }
  }
}
```

---

## Field Types

| Type               | Description                     | Use For                    |
| ------------------ | ------------------------------- | -------------------------- |
| **text**           | Full-text searchable (analyzed) | Product descriptions, body |
| **keyword**        | Exact match, not analyzed       | Tags, categories, IDs      |
| **integer**        | Whole numbers                   | Quantity, age              |
| **float / double** | Decimal numbers                 | Price, ratings             |
| **boolean**        | true / false                    | in_stock, published        |
| **date**           | ISO 8601 dates                  | Timestamps                 |
| **object**         | Nested JSON                     | Structured sub-documents   |
| **geo_point**      | Latitude / longitude            | Location-based search      |

---

## ⚠️ text vs keyword — The Critical Distinction

| Feature          | text                           | keyword               |
| ---------------- | ------------------------------ | --------------------- |
| **Analysis**     | Tokenized, lowercased, stemmed | Stored as-is          |
| **Search type**  | Full-text (match)              | Exact match (term)    |
| **Good for**     | Natural language text          | IDs, tags, categories |
| **Aggregations** | ❌ Not efficient               | ✅ Fast               |
| **Sorting**      | ❌ Unreliable                  | ✅ Alphabetical       |

```json
// text: "Wireless Mouse" → stored as ["wireless", "mouse"]
// keyword: "Wireless Mouse" → stored as "Wireless Mouse" (exact)
```

💡 Use **text** for things people search. Use **keyword** for things people filter and sort by.

---

## Part 12: Analyzers & Tokenizers

How Elasticsearch Processes Text

---

## The Analysis Pipeline

When you index a `text` field, Elasticsearch runs it through an **analyzer**:

```mermaid
flowchart LR
    INPUT["Input: The Quick BROWN Foxes are Running"]
    TOK["Tokenizer<br/>Split into words"]
    LOW["Lowercase Filter<br/>quick, brown, foxes, running"]
    STOP["Stop Word Filter<br/>Remove: the, are"]
    STEM["Stemmer<br/>foxes→fox, running→run"]
    OUTPUT["Tokens: quick, brown, fox, run"]
    INPUT --> TOK --> LOW --> STOP --> STEM --> OUTPUT
```

---

## Built-in Analyzers

| Analyzer       | Processing                               | Example Output                        |
| -------------- | ---------------------------------------- | ------------------------------------- |
| **standard**   | Tokenize + lowercase                     | ["the", "quick", "brown", "foxes"]    |
| **simple**     | Tokenize by non-letter + lowercase       | ["the", "quick", "brown", "foxes"]    |
| **whitespace** | Tokenize by whitespace only              | ["The", "Quick", "BROWN", "Foxes"]    |
| **keyword**    | No analysis (entire string as one token) | ["The Quick BROWN Foxes are Running"] |
| **english**    | Standard + stop words + stemming         | ["quick", "brown", "fox", "run"]      |

---

## Example: Standard vs English Analyzer

**Input**: "The Quick BROWN Foxes are Running"

**Standard analyzer**:

```
["the", "quick", "brown", "foxes", "are", "running"]
```

- Tokenizes and lowercases
- Keeps all words (including "the", "are")
- No stemming ("foxes" stays "foxes")

**English analyzer**:

```
["quick", "brown", "fox", "run"]
```

- Tokenizes and lowercases
- Removes stop words ("the", "are")
- Stems words ("foxes" → "fox", "running" → "run")

💡 The English analyzer finds more matches because "foxes" and "fox" become the same token.

---

## Why Analyzers Matter for Queries

When you search with a `match` query, the **same analyzer** runs on your query text:

```
Query: "Running foxes"
         ↓ English analyzer
Tokens: ["run", "fox"]
         ↓ Inverted index lookup
Result: Documents containing "run" OR "fox"
```

This is why searching "running" matches documents containing "run" — both are stemmed to the same token.

---

## Part 13: Query DSL

Elasticsearch's JSON-Based Query Language

---

## Query DSL Overview

Elasticsearch uses a **JSON-based** query language instead of SQL:

```json
{
  "query": {
    "<query_type>": {
      "<field>": "<value>"
    }
  }
}
```

---

## Match Query — Full-Text Search

Find products matching "wireless mouse":

```json
{
  "query": {
    "match": {
      "description": "wireless mouse"
    }
  }
}
```

**How it works:**

1. Analyzer tokenizes query: `["wireless", "mouse"]`
2. Searches inverted index for documents containing **either** term
3. Scores by relevance (documents with both terms score higher)

---

## Term Query — Exact Match

Find products in the "Electronics" category:

```json
{
  "query": {
    "term": {
      "category": "Electronics"
    }
  }
}
```

**Important:**

- `term` queries are **not analyzed** — the value is matched as-is
- Use on **keyword** fields only
- Case-sensitive!

---

## ⚠️ term on text fields — A Common Mistake

```json
// ❌ WRONG: searching text field with term query
{
  "query": {
    "term": {
      "description": "Wireless Mouse"
    }
  }
}
// Returns 0 results! The text field stores lowercase tokens
// ["wireless", "mouse"], but "Wireless Mouse" doesn't match either
```

```json
// ✅ CORRECT: use match for text fields
{
  "query": {
    "match": {
      "description": "Wireless Mouse"
    }
  }
}
// match analyzes the query → ["wireless", "mouse"] → finds matches
```

💡 **Rule**: `match` for text fields, `term` for keyword fields.

---

## Range Query

Find products priced between $20 and $50:

```json
{
  "query": {
    "range": {
      "price": {
        "gte": 20,
        "lte": 50
      }
    }
  }
}
```

**Date ranges:**

```json
{
  "query": {
    "range": {
      "created_at": {
        "gte": "2026-01-01",
        "lte": "2026-04-01"
      }
    }
  }
}
```

---

## Bool Query — Combine Conditions

The `bool` query combines multiple conditions with boolean logic:

| Clause       | Logic | Affects Score? | Description                      |
| ------------ | ----- | -------------- | -------------------------------- |
| **must**     | AND   | ✅ Yes         | Must match, contributes to score |
| **filter**   | AND   | ❌ No          | Must match, no scoring (cached!) |
| **should**   | OR    | ✅ Yes         | Should match, boosts score       |
| **must_not** | NOT   | ❌ No          | Must not match                   |

---

## Bool Query Example

Find in-stock Electronics under $50 with "wireless" in the description:

```json
{
  "query": {
    "bool": {
      "must": [{ "match": { "description": "wireless" } }],
      "filter": [{ "term": { "category": "Electronics" } }, { "range": { "price": { "lte": 50 } } }, { "term": { "in_stock": true } }],
      "must_not": [{ "term": { "discontinued": true } }]
    }
  }
}
```

- `must` → relevance scoring on "wireless"
- `filter` → exact-match conditions (cached, no scoring)
- `must_not` → exclude discontinued products

---

## filter vs must — Why It Matters

```json
// ❌ Slow: calculates relevance score for category
{
  "bool": {
    "must": [
      { "term": { "category": "Electronics" } }
    ]
  }
}

// ✅ Fast: skips scoring, results are cached
{
  "bool": {
    "filter": [
      { "term": { "category": "Electronics" } }
    ]
  }
}
```

💡 Use `filter` for exact-match conditions where relevance scoring doesn't matter. Elasticsearch **caches** filter results — repeated queries are nearly instant.

---

## Part 14: Advanced Queries

Fuzzy, Prefix, Multi-Match & Boosting

---

## Fuzzy Query — Typo Tolerance

Find "mose" (typo for "mouse"):

```json
{
  "query": {
    "fuzzy": {
      "name": {
        "value": "mose",
        "fuzziness": "AUTO"
      }
    }
  }
}
```

**Fuzziness values:**

| Setting | Meaning                         |
| ------- | ------------------------------- |
| `AUTO`  | 0–2 edits based on term length  |
| `0`     | Exact match only                |
| `1`     | 1 character difference allowed  |
| `2`     | 2 character differences allowed |

💡 `AUTO` is usually the best choice — short words require exact matches, longer words allow more edits.

---

## Prefix Query — Autocomplete

Find products starting with "wire":

```json
{
  "query": {
    "prefix": {
      "name": "wire"
    }
  }
}
```

Matches: **Wireless** Mouse, **Wired** Keyboard

---

## Wildcard Query

```json
{
  "query": {
    "wildcard": {
      "name": "*mou*"
    }
  }
}
```

Matches: Wireless **Mou**se, **Mou**ntain Bike

⚠️ Wildcards are **slow** on large datasets — prefer prefix queries when possible.

---

## Multi-Match — Search Across Fields

Search "mouse" in both name and description:

```json
{
  "query": {
    "multi_match": {
      "query": "mouse",
      "fields": ["name", "description"]
    }
  }
}
```

---

## Field Boosting

Prioritize matches in the name field:

```json
{
  "query": {
    "multi_match": {
      "query": "keyboard",
      "fields": ["name^3", "description"]
    }
  }
}
```

`name^3` means matches in the `name` field are weighted **3× higher** in the relevance score.

```mermaid
flowchart LR
    Q["Query: keyboard"]
    Q --> N["name field<br/>score × 3"]
    Q --> D["description field<br/>score × 1"]
    N --> RANK["Final Ranking:<br/>1. Mechanical Keyboard (name match)<br/>2. Keyboard Stand (name match)<br/>3. ...includes keyboard adapter (desc)"]
    D --> RANK
```

---

## Part 15: Aggregations

Analytics Over Search Results

---

## What Are Aggregations?

Aggregations compute **analytics** over your search results — counts, averages, histograms, etc.

```json
{
  "size": 0,          // Don't return individual documents
  "aggs": {           // Just compute aggregations
    "my_agg": { ... }
  }
}
```

💡 `"size": 0` tells Elasticsearch to skip returning documents — only return the aggregation results.

---

## Terms Aggregation — Group By

Count products by category:

```json
{
  "size": 0,
  "aggs": {
    "categories": {
      "terms": {
        "field": "category"
      }
    }
  }
}
```

**Response:**

```json
{
  "aggregations": {
    "categories": {
      "buckets": [
        { "key": "Electronics", "doc_count": 15 },
        { "key": "Furniture", "doc_count": 8 },
        { "key": "Clothing", "doc_count": 5 }
      ]
    }
  }
}
```

---

## Stats Aggregation — Min, Max, Avg, Sum

```json
{
  "size": 0,
  "aggs": {
    "price_stats": {
      "stats": {
        "field": "price"
      }
    }
  }
}
```

**Response:**

```json
{
  "aggregations": {
    "price_stats": {
      "count": 28,
      "min": 12.99,
      "max": 199.99,
      "avg": 73.24,
      "sum": 2050.72
    }
  }
}
```

---

## Histogram Aggregation — Bucketing

Price distribution in $50 ranges:

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

**Response:**

```json
{
  "aggregations": {
    "price_ranges": {
      "buckets": [
        { "key": 0, "doc_count": 8 },
        { "key": 50, "doc_count": 12 },
        { "key": 100, "doc_count": 5 },
        { "key": 150, "doc_count": 3 }
      ]
    }
  }
}
```

---

## Date Histogram — Time-Based Buckets

Products created per month:

```json
{
  "size": 0,
  "aggs": {
    "products_per_month": {
      "date_histogram": {
        "field": "created_at",
        "calendar_interval": "month"
      }
    }
  }
}
```

💡 Date histograms are the Elasticsearch equivalent of TimescaleDB's `time_bucket()` — but for search data rather than time-series data.

---

## Combining Search + Aggregations

Search for "wireless" AND get price stats for matching products:

```json
{
  "query": {
    "match": { "description": "wireless" }
  },
  "aggs": {
    "avg_price": {
      "avg": { "field": "price" }
    },
    "by_category": {
      "terms": { "field": "category" }
    }
  }
}
```

This returns matching documents AND aggregations computed over the matching set.

---

## Part 16: Docker Setup

Running TimescaleDB & Elasticsearch Locally

---

## Docker Compose — TimescaleDB

```yaml
version: '3.8'

services:
  timescaledb:
    image: timescale/timescaledb:latest-pg16
    ports:
      - '5432:5432'
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=password
      - POSTGRES_DB=timeseries
    volumes:
      - timescale_data:/var/lib/postgresql/data

volumes:
  timescale_data:
```

```bash
# Start TimescaleDB
docker-compose up -d timescaledb

# Connect with psql
docker exec -it timescaledb psql -U postgres -d timeseries

# Verify extension
SELECT extversion FROM pg_extension
WHERE extname = 'timescaledb';
```

---

## Docker Compose — Elasticsearch

```yaml
version: '3.8'

services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.12.0
    ports:
      - '9200:9200'
      - '9300:9300'
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - 'ES_JAVA_OPTS=-Xms512m -Xmx512m'
    volumes:
      - es_data:/usr/share/elasticsearch/data

  kibana:
    image: docker.elastic.co/kibana/kibana:8.12.0
    ports:
      - '5601:5601'
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch

volumes:
  es_data:
```

---

## Starting & Verifying

```bash
# Start everything
docker-compose up -d

# Verify TimescaleDB (port 5432)
psql -h localhost -U postgres -d timeseries \
  -c "SELECT extversion FROM pg_extension WHERE extname = 'timescaledb';"

# Verify Elasticsearch (port 9200)
curl http://localhost:9200

# Open Kibana Dev Tools (port 5601)
open http://localhost:5601/app/dev_tools#/console
```

---

## Service Ports

| Service       | Port | Protocol | Purpose                      |
| ------------- | ---- | -------- | ---------------------------- |
| TimescaleDB   | 5432 | TCP      | PostgreSQL wire protocol     |
| Elasticsearch | 9200 | HTTP     | REST API (queries, indexing) |
| Elasticsearch | 9300 | TCP      | Cluster communication        |
| Kibana        | 5601 | HTTP     | Web UI for Elasticsearch     |

---

## Part 17: TypeScript Integration

Connecting from Your Application

---

## TimescaleDB with Drizzle ORM

```bash
npm install drizzle-orm pg
npm install -D drizzle-kit @types/pg
```

---

## Define the Schema

```typescript
import { pgTable, serial, timestamp, doublePrecision, integer, text } from 'drizzle-orm/pg-core';

const sensorData = pgTable('sensor_data', {
  time: timestamp('time', { withTimezone: true }).notNull(),
  sensorId: integer('sensor_id').notNull(),
  temperature: doublePrecision('temperature'),
  humidity: doublePrecision('humidity'),
  location: text('location'),
});
```

---

## Connect and Query

```typescript
import { drizzle } from 'drizzle-orm/node-postgres';
import { sql } from 'drizzle-orm';
import { Pool } from 'pg';

const pool = new Pool({
  connectionString: 'postgres://postgres:password@localhost:5432/timeseries',
});

const db = drizzle(pool);

// Insert data
await db.insert(sensorData).values({
  time: new Date(),
  sensorId: 1,
  temperature: 22.5,
  humidity: 65.0,
  location: 'Office A',
});
```

---

## time_bucket() in TypeScript

```typescript
// Hourly averages for the last 7 days
const hourlyAvg = await db.execute(sql`
  SELECT
    time_bucket('1 hour', time) AS hour,
    sensor_id,
    AVG(temperature) AS avg_temp,
    MAX(temperature) AS max_temp,
    MIN(temperature) AS min_temp
  FROM sensor_data
  WHERE time > NOW() - INTERVAL '7 days'
  GROUP BY hour, sensor_id
  ORDER BY hour DESC
`);

console.log(hourlyAvg.rows);
```

---

## Elasticsearch with @elastic/elasticsearch

```bash
npm install @elastic/elasticsearch
npm install -D @types/node
```

---

## Connect to Elasticsearch

```typescript
import { Client } from '@elastic/elasticsearch';

const client = new Client({
  node: 'http://localhost:9200',
});

// Verify connection
const info = await client.info();
console.log('Cluster:', info.name);
```

---

## Create Index with Mappings

```typescript
async function createProductsIndex() {
  await client.indices.create({
    index: 'products',
    body: {
      mappings: {
        properties: {
          name: { type: 'text' },
          description: { type: 'text' },
          price: { type: 'float' },
          category: { type: 'keyword' },
          tags: { type: 'keyword' },
          in_stock: { type: 'boolean' },
          created_at: { type: 'date' },
        },
      },
    },
  });
}
```

---

## Index Documents

```typescript
interface Product {
  name: string;
  description?: string;
  price: number;
  category: string;
  tags?: string[];
  in_stock: boolean;
  created_at: Date;
}

async function indexProduct(product: Product, id?: string) {
  const response = await client.index({
    index: 'products',
    id,
    body: product,
  });
  return response._id;
}

await indexProduct(
  {
    name: 'Wireless Mouse',
    description: 'Ergonomic wireless mouse with USB receiver',
    price: 29.99,
    category: 'Electronics',
    tags: ['wireless', 'mouse', 'ergonomic'],
    in_stock: true,
    created_at: new Date(),
  },
  '1',
);
```

---

## Bulk Index

```typescript
async function bulkIndexProducts(products: Product[]) {
  const body = products.flatMap((product, i) => [{ index: { _index: 'products', _id: String(i + 1) } }, product]);

  const response = await client.bulk({ body, refresh: true });
  console.log(`Indexed ${response.items.length} documents`);
}
```

💡 Always use `refresh: true` in development so documents are immediately searchable. In production, rely on the default 1-second refresh interval.

---

## Search Products

```typescript
async function searchProducts(query: string) {
  const response = await client.search({
    index: 'products',
    body: {
      query: {
        multi_match: {
          query,
          fields: ['name^2', 'description'],
        },
      },
    },
  });

  return response.hits.hits.map((hit) => ({
    id: hit._id,
    score: hit._score,
    ...(hit._source as Product),
  }));
}

const results = await searchProducts('wireless mouse');
```

---

## Complex Bool Query in TypeScript

```typescript
async function searchFilteredProducts(query: string, category: string, maxPrice: number) {
  const response = await client.search({
    index: 'products',
    body: {
      query: {
        bool: {
          must: [{ match: { description: query } }],
          filter: [{ term: { category } }, { range: { price: { lte: maxPrice } } }, { term: { in_stock: true } }],
        },
      },
      sort: [{ price: 'asc' }],
    },
  });

  return response.hits.hits.map((hit) => hit._source as Product);
}

const cheap = await searchFilteredProducts('wireless', 'Electronics', 50);
```

---

## Aggregations in TypeScript

```typescript
async function getCategoryStats() {
  const response = await client.search({
    index: 'products',
    body: {
      size: 0,
      aggs: {
        by_category: {
          terms: { field: 'category' },
        },
        price_stats: {
          stats: { field: 'price' },
        },
      },
    },
  });

  const categories = response.aggregations?.by_category as any;
  const prices = response.aggregations?.price_stats as any;

  return {
    categories: categories.buckets,
    priceStats: prices,
  };
}
```

---

## Part 18: Common Pitfalls & Best Practices

Mistakes to Avoid

---

## TimescaleDB Pitfall 1: Forgetting to Create the Hypertable

```sql
-- ❌ Wrong: just a regular table — no automatic partitioning
CREATE TABLE sensor_data (time TIMESTAMPTZ, temp FLOAT);

-- ✅ Correct: convert to hypertable
CREATE TABLE sensor_data (time TIMESTAMPTZ NOT NULL, temp FLOAT);
SELECT create_hypertable('sensor_data', 'time');
```

---

## TimescaleDB Pitfall 2: Using date_trunc() Instead of time_bucket()

```sql
-- ❌ Less flexible: only supports standard intervals
SELECT date_trunc('hour', time), AVG(temp)
FROM sensor_data GROUP BY 1;

-- ✅ More flexible: supports arbitrary intervals
SELECT time_bucket('15 minutes', time), AVG(temp)
FROM sensor_data GROUP BY 1;
```

💡 `time_bucket()` supports any interval: 5 minutes, 15 minutes, 2 hours, 3 days...

---

## TimescaleDB Pitfall 3: Modifying Compressed Data

```sql
-- ❌ Error: cannot UPDATE compressed chunks
UPDATE sensor_data SET temp = 25
WHERE time < NOW() - INTERVAL '7 days';

-- ✅ Decompress first, then modify
SELECT decompress_chunk('_timescaledb_internal._hyper_1_1_chunk');
UPDATE sensor_data SET temp = 25
WHERE time BETWEEN '2026-01-01' AND '2026-01-07';
```

---

## Elasticsearch Pitfall 1: term on text Fields

```json
// ❌ Returns 0 results — text field stores lowercase tokens
{ "term": { "description": "Wireless Mouse" } }

// ✅ Use match for text fields
{ "match": { "description": "Wireless Mouse" } }

// ✅ Or use .keyword subfield for exact match
{ "term": { "description.keyword": "Wireless Mouse" } }
```

---

## Elasticsearch Pitfall 2: Using must When filter Suffices

```json
// ❌ Slow: calculates relevance score unnecessarily
{
  "bool": {
    "must": [
      { "term": { "category": "Electronics" } }
    ]
  }
}

// ✅ Fast: skips scoring, results are cached
{
  "bool": {
    "filter": [
      { "term": { "category": "Electronics" } }
    ]
  }
}
```

---

## Elasticsearch Pitfall 3: Not Refreshing After Bulk Insert

```typescript
// ❌ Documents not immediately searchable
await client.bulk({ body });

// ✅ Force refresh (development only)
await client.bulk({ body, refresh: true });

// ✅ Or wait for automatic refresh (production)
// Elasticsearch refreshes every 1 second by default
```

---

## Best Practice 1: Batch Inserts (TimescaleDB)

```sql
-- ❌ Slow: 1000 individual inserts
INSERT INTO sensor_data VALUES (NOW(), 1, 22.5, 65.0, 'A');
INSERT INTO sensor_data VALUES (NOW(), 2, 23.1, 60.0, 'B');
-- ... 998 more

-- ✅ Fast: single bulk insert
INSERT INTO sensor_data VALUES
  (NOW(), 1, 22.5, 65.0, 'A'),
  (NOW(), 2, 23.1, 60.0, 'B'),
  -- ... 998 more
  ;
```

---

## Best Practice 2: Use Indexes Wisely

```sql
-- TimescaleDB: index on frequently filtered metadata columns
CREATE INDEX idx_sensor_location
ON sensor_data (sensor_id, location);
```

```json
// Elasticsearch: use keyword fields for filtering
{
  "mappings": {
    "properties": {
      "category": { "type": "keyword" },
      "brand": { "type": "keyword" }
    }
  }
}
```

---

## Best Practice 3: Use Parameterized Queries

```typescript
// ❌ SQL injection risk!
const query = `SELECT * FROM sensor_data WHERE sensor_id = ${userInput}`;

// ✅ Safe: parameterized query
const result = await db.execute(sql`
  SELECT * FROM sensor_data
  WHERE sensor_id = ${sensorId}
`);
```

```typescript
// ❌ Elasticsearch query injection
const body = JSON.parse(userInput);
await client.search({ index: 'products', body });

// ✅ Safe: construct query programmatically
await client.search({
  index: 'products',
  body: {
    query: { match: { description: sanitizedInput } },
  },
});
```

---

## Best Practice 4: Monitor Compression Ratios

```sql
-- Check if compression is working effectively
SELECT hypertable_name,
       pg_size_pretty(before_compression_total_bytes) AS before,
       pg_size_pretty(after_compression_total_bytes) AS after,
       ROUND(before_compression_total_bytes::numeric /
             NULLIF(after_compression_total_bytes, 0), 1) AS ratio
FROM timescaledb_information.compressed_chunk_stats;
```

| hypertable  | before | after | ratio |
| ----------- | ------ | ----- | ----- |
| sensor_data | 1 GB   | 52 MB | 19.7× |

---

## Part 19: Decision Framework

When to Use What

---

## When to Use TimescaleDB ✅

| Use Case                 | Why TimescaleDB?                            |
| ------------------------ | ------------------------------------------- |
| IoT sensor data          | High-throughput inserts, time-range queries |
| Application monitoring   | Metrics, APM, dashboards                    |
| Financial data           | ACID guarantees, SQL compatibility          |
| Log storage (structured) | Retention policies, compression             |
| Real-time analytics      | Continuous aggregates, time_bucket()        |

---

## When NOT to Use TimescaleDB ❌

| Use Case               | Better Alternative |
| ---------------------- | ------------------ |
| Full-text search       | Elasticsearch      |
| Graph traversals       | Neo4j              |
| Simple key-value cache | Redis              |
| Document storage       | MongoDB            |
| Data not time-stamped  | Plain PostgreSQL   |

---

## When to Use Elasticsearch ✅

| Use Case                     | Why Elasticsearch?                  |
| ---------------------------- | ----------------------------------- |
| Full-text search             | Inverted indices, relevance scoring |
| Log analysis                 | ELK stack, Kibana dashboards        |
| Product catalogs             | Faceted search, autocomplete        |
| Real-time monitoring         | Fast aggregations, alerting         |
| Fuzzy / typo-tolerant search | Built-in fuzziness, stemming        |

---

## When NOT to Use Elasticsearch ❌

| Use Case                  | Better Alternative |
| ------------------------- | ------------------ |
| Transactional systems     | PostgreSQL         |
| Strong consistency (ACID) | PostgreSQL         |
| Primary data store        | Any relational DB  |
| Complex joins             | PostgreSQL         |
| Time-series analytics     | TimescaleDB        |

---

## The Polyglot Architecture

```mermaid
flowchart LR
    APP["Your Application"]
    APP -->|"Users, Orders"| PG["PostgreSQL"]
    APP -->|"Sessions, Cache"| RD["Redis"]
    APP -->|"Relationships"| N4["Neo4j"]
    APP -->|"Time-Series"| TS["TimescaleDB"]
    APP -->|"Search"| ES["Elasticsearch"]
```

| Layer            | Database          | Why                        |
| ---------------- | ----------------- | -------------------------- |
| Users & Orders   | PostgreSQL        | ACID, complex queries      |
| Sessions / Cache | Redis             | Sub-ms speed, TTL          |
| Social Graph     | Neo4j             | Relationship traversals    |
| IoT Metrics      | **TimescaleDB**   | Time-series optimized      |
| Product Search   | **Elasticsearch** | Relevance-ranked full-text |

💡 Your final project should combine **3+ databases** — each doing what it does best.

---

## Quick Reference: TimescaleDB Cheat Sheet

| Operation            | SQL                                                                  |
| -------------------- | -------------------------------------------------------------------- |
| Enable extension     | `CREATE EXTENSION IF NOT EXISTS timescaledb;`                        |
| Create hypertable    | `SELECT create_hypertable('table', 'time');`                         |
| Downsample           | `SELECT time_bucket('1 hour', time) ... GROUP BY 1;`                 |
| Gap fill             | `SELECT time_bucket_gapfill('1 hour', time) ...;`                    |
| Enable compression   | `ALTER TABLE t SET (timescaledb.compress);`                          |
| Compression policy   | `SELECT add_compression_policy('t', INTERVAL '7d');`                 |
| Retention policy     | `SELECT add_retention_policy('t', INTERVAL '90d');`                  |
| Continuous aggregate | `CREATE MATERIALIZED VIEW ... WITH (timescaledb.continuous) AS ...;` |
| View chunks          | `SELECT * FROM timescaledb_information.chunks;`                      |
| Drop old chunks      | `SELECT drop_chunks('t', INTERVAL '90 days');`                       |

---

## Quick Reference: Elasticsearch Cheat Sheet

| Operation             | Query DSL                                                        |
| --------------------- | ---------------------------------------------------------------- |
| Full-text search      | `{ "match": { "field": "query" } }`                              |
| Exact match           | `{ "term": { "field": "value" } }`                               |
| Range filter          | `{ "range": { "price": { "gte": 10, "lte": 50 } } }`             |
| Bool AND              | `{ "bool": { "must": [...] } }`                                  |
| Bool OR               | `{ "bool": { "should": [...] } }`                                |
| Bool NOT              | `{ "bool": { "must_not": [...] } }`                              |
| Filter (no score)     | `{ "bool": { "filter": [...] } }`                                |
| Multi-field search    | `{ "multi_match": { "query": "q", "fields": [...] } }`           |
| Fuzzy search          | `{ "fuzzy": { "name": { "value": "q", "fuzziness": "AUTO" } } }` |
| Prefix (autocomplete) | `{ "prefix": { "name": "wir" } }`                                |
| Count by group        | `{ "aggs": { "a": { "terms": { "field": "cat" } } } }`           |
| Stats                 | `{ "aggs": { "a": { "stats": { "field": "price" } } } }`         |

---

## Key Takeaways

1. **TimescaleDB** is a PostgreSQL extension — full SQL compatibility with time-series superpowers
2. **Hypertables** automatically partition data into time-based chunks for fast queries
3. **time_bucket()** is the key function for downsampling and aggregation
4. **Compression** reduces storage 10–20× with columnar encoding
5. **Retention policies** automatically delete old data; **continuous aggregates** precompute analytics
6. **Elasticsearch** uses **inverted indices** for fast full-text search — not B-trees
7. **text** fields are analyzed (tokenized, lowercased, stemmed); **keyword** fields are exact
8. Use **match** for text fields, **term** for keyword fields
9. Use **filter** instead of **must** for exact-match conditions — cached and faster
10. **Aggregations** compute analytics (counts, stats, histograms) over search results

---

## Databases You've Learned

| Week | Database              | Paradigm                  |
| ---- | --------------------- | ------------------------- |
| 1–6  | **PostgreSQL**        | Relational (SQL)          |
| 7–8  | **MongoDB**           | Document Store            |
| 9–10 | **Advanced SQL**      | Transactions, Performance |
| 11   | **Redis & Cassandra** | Key-Value & Wide-Column   |
| 12   | **Neo4j**             | Graph Database            |
| 13   | **TimescaleDB & ES**  | Time-Series & Search      |

---

## Final Project Reminder

Your project uses **3+ databases** together:

```mermaid
flowchart TD
    APP["Your Application"]
    APP --> PG["PostgreSQL<br/>Users, Orders, Auth"]
    APP --> TS["TimescaleDB<br/>Metrics, IoT Data"]
    APP --> ES["Elasticsearch<br/>Search, Logs"]
    APP --> RD["Redis<br/>Cache, Sessions"]
    APP --> N4["Neo4j<br/>Relationships, Recs"]
    style APP fill:#3498db,color:#fff
    style PG fill:#2ecc71,color:#fff
    style TS fill:#f39c12,color:#fff
    style ES fill:#9b59b6,color:#fff
    style RD fill:#e74c3c,color:#fff
    style N4 fill:#1abc9c,color:#fff
```
