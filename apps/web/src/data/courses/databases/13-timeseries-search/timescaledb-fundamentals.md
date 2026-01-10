# TimescaleDB Fundamentals - Time Series Databases

## Introduction

**TimescaleDB** is a PostgreSQL extension optimized for time-series data. It combines the reliability and ecosystem of PostgreSQL with specialized features for handling massive streams of time-stamped data like IoT sensors, financial ticks, application metrics, and monitoring logs.

---

## What is Time-Series Data?

**Time-series data** consists of measurements or events tracked over time, where each record has:

1. **Timestamp**: When the event occurred
2. **Measurements**: Numeric values (temperature, price, CPU usage)
3. **Metadata**: Contextual information (device_id, location, user_id)

**Examples:**

- **IoT Sensors**: Temperature readings every 10 seconds
- **Stock Market**: Price ticks every millisecond
- **Application Monitoring**: CPU/memory metrics every minute
- **Website Analytics**: Page views per hour
- **Financial Transactions**: Trades timestamped to the microsecond

---

## Why TimescaleDB?

### Traditional Databases vs Time-Series Databases

| Aspect | PostgreSQL (Plain) | TimescaleDB |
|--------|-------------------|-------------|
| **Time-Range Queries** | Slow for billions of rows | Fast (partitioned by time) |
| **Data Retention** | Manual DELETE statements | Automatic retention policies |
| **Aggregations** | Expensive recalculation | Continuous aggregates (materialized) |
| **Compression** | Basic TOAST compression | Columnar compression (10x-20x) |
| **Downsampling** | Manual queries | Built-in downsampling |
| **Inserts** | Good | Optimized for high-throughput inserts |

**TimescaleDB solves:**

- ✅ **Scale**: Handle billions of time-series records
- ✅ **Performance**: Fast time-range queries with automatic partitioning
- ✅ **Storage**: Compress old data automatically (10x-20x compression)
- ✅ **Retention**: Auto-delete old data based on age
- ✅ **Analytics**: Continuous aggregates for real-time dashboards

---

## Core Concepts

### 1. Hypertables

A **hypertable** is TimescaleDB's abstraction over partitioned tables. It looks like a single table but is automatically partitioned by time (and optionally space).

**How it works:**

```
┌─── Hypertable (sensor_data) ───┐
│                                 │
├─ Chunk 1: Jan 1-7              │
├─ Chunk 2: Jan 8-14             │
├─ Chunk 3: Jan 15-21            │
├─ Chunk 4: Jan 22-31            │
└─────────────────────────────────┘
```

- Each **chunk** stores a time interval (default: 7 days)
- Queries automatically target relevant chunks only
- Old chunks can be compressed or dropped

**Benefits:**

- Query only relevant time ranges (fast)
- Compress old chunks (save storage)
- Drop old chunks (automatic retention)

---

### 2. Chunks

**Chunks** are the internal partitions of a hypertable. TimescaleDB automatically:

- Creates new chunks as data arrives
- Routes INSERT queries to the correct chunk
- Prunes chunks during SELECT queries (only read relevant chunks)

**Chunk Management:**

```sql
-- View chunks
SELECT * FROM timescaledb_information.chunks
WHERE hypertable_name = 'sensor_data';

-- Manually drop old chunks
SELECT drop_chunks('sensor_data', INTERVAL '90 days');
```

---

### 3. Compression

TimescaleDB uses **columnar compression** to reduce storage by 10x-20x.

**How it works:**

1. Identify old chunks (e.g., data older than 7 days)
2. Compress them with columnar storage
3. Queries still work transparently (decompression is automatic)

**Benefits:**

- Store 10-20x more data in the same disk space
- Queries on compressed data are still fast
- Automatic background compression

---

### 4. Retention Policies

Automatically **delete old data** after a specified duration.

**Example:** Keep only 90 days of sensor data

```sql
SELECT add_retention_policy('sensor_data', INTERVAL '90 days');
```

TimescaleDB automatically drops chunks older than 90 days.

---

### 5. Continuous Aggregates

**Materialized views** that automatically update as new data arrives.

**Use Case:** Dashboard showing average temperature per hour

**Without Continuous Aggregates:**

```sql
-- Slow: recalculates every query
SELECT time_bucket('1 hour', time) AS hour,
       sensor_id,
       AVG(temperature) AS avg_temp
FROM sensor_data
WHERE time > NOW() - INTERVAL '30 days'
GROUP BY hour, sensor_id;
```

**With Continuous Aggregates:**

```sql
-- Create continuous aggregate (runs once)
CREATE MATERIALIZED VIEW sensor_data_hourly
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 hour', time) AS hour,
       sensor_id,
       AVG(temperature) AS avg_temp
FROM sensor_data
GROUP BY hour, sensor_id;

-- Fast: pre-calculated
SELECT * FROM sensor_data_hourly
WHERE hour > NOW() - INTERVAL '30 days';
```

TimescaleDB automatically updates the aggregate as new data arrives.

---

## Setting Up TimescaleDB

### Docker Setup

```yaml
# docker-compose.yml
version: '3.8'

services:
  timescaledb:
    image: timescale/timescaledb:latest-pg16
    ports:
      - "5432:5432"
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
docker-compose up -d

# Wait 5-10 seconds for startup

# Connect with psql
docker exec -it timescaledb psql -U postgres -d timeseries

# Verify TimescaleDB extension
timeseries=# \dx
```

You should see `timescaledb` in the extensions list.

---

### Enable TimescaleDB Extension

```sql
-- Enable TimescaleDB
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Verify version
SELECT extversion FROM pg_extension WHERE extname = 'timescaledb';
```

---

## Creating Hypertables

### Step 1: Create Regular Table

```sql
CREATE TABLE sensor_data (
  time        TIMESTAMPTZ NOT NULL,
  sensor_id   INT NOT NULL,
  temperature FLOAT,
  humidity    FLOAT,
  location    TEXT
);
```

### Step 2: Convert to Hypertable

```sql
-- Convert to hypertable (partitioned by time)
SELECT create_hypertable('sensor_data', 'time');
```

**Optional: Partition by Space (Multi-Dimensional)**

```sql
-- Partition by time AND sensor_id
SELECT create_hypertable(
  'sensor_data',
  'time',
  partitioning_column => 'sensor_id',
  number_partitions => 4
);
```

This creates chunks for each combination of time range and sensor_id hash.

---

## Inserting Data

### Single Insert

```sql
INSERT INTO sensor_data (time, sensor_id, temperature, humidity, location)
VALUES (NOW(), 1, 22.5, 65.0, 'Office A');
```

### Bulk Insert

```sql
INSERT INTO sensor_data (time, sensor_id, temperature, humidity, location)
VALUES
  ('2026-04-01 10:00:00', 1, 22.5, 65.0, 'Office A'),
  ('2026-04-01 10:00:00', 2, 23.1, 60.0, 'Office B'),
  ('2026-04-01 10:05:00', 1, 22.7, 64.5, 'Office A'),
  ('2026-04-01 10:05:00', 2, 23.3, 59.8, 'Office B');
```

**Best Practice:** Use batch inserts for high-throughput scenarios (insert 1000s of rows per transaction).

---

## Querying Time-Series Data

### Time-Range Queries

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

TimescaleDB **automatically prunes chunks** outside the time range, making these queries fast even with billions of rows.

---

### Time Bucketing (Downsampling)

**`time_bucket()`** groups timestamps into fixed intervals.

**Example: Average temperature per hour**

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

| hour | sensor_id | avg_temp | max_temp | min_temp |
|------|-----------|----------|----------|----------|
| 2026-04-01 10:00 | 1 | 22.6 | 23.0 | 22.2 |
| 2026-04-01 10:00 | 2 | 23.2 | 23.5 | 22.9 |
| 2026-04-01 11:00 | 1 | 22.8 | 23.2 | 22.4 |

**Common Intervals:**

```sql
time_bucket('1 second', time)
time_bucket('1 minute', time)
time_bucket('5 minutes', time)
time_bucket('1 hour', time)
time_bucket('1 day', time)
time_bucket('1 week', time)
```

---

### Gap Filling

**`time_bucket_gapfill()`** fills missing time intervals with NULL or interpolated values.

**Problem:** Sensor didn't report data for some hours.

**Solution:**

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

This ensures every hour appears in results, even if no data exists (returns NULL).

**Interpolation:**

```sql
SELECT time_bucket_gapfill('1 hour', time) AS hour,
       sensor_id,
       COALESCE(AVG(temperature), locf(AVG(temperature))) AS avg_temp
FROM sensor_data
WHERE time > NOW() - INTERVAL '24 hours'
  AND sensor_id = 1
GROUP BY hour, sensor_id
ORDER BY hour;
```

`locf()` (Last Observation Carried Forward) fills gaps with the previous value.

---

## Compression

### Enable Compression

```sql
-- Enable compression on hypertable
ALTER TABLE sensor_data SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'sensor_id',
  timescaledb.compress_orderby = 'time DESC'
);
```

**Parameters:**

- `compress_segmentby`: Group rows by this column (e.g., sensor_id, device_id)
- `compress_orderby`: Sort order within groups (usually time DESC)

---

### Compression Policy

Automatically compress chunks older than 7 days:

```sql
SELECT add_compression_policy('sensor_data', INTERVAL '7 days');
```

TimescaleDB runs a background job to compress chunks.

---

### Manual Compression

```sql
-- Compress specific chunk
SELECT compress_chunk('_timescaledb_internal._hyper_1_2_chunk');

-- Compress all chunks older than 7 days
SELECT compress_chunk(c.chunk_schema || '.' || c.chunk_name)
FROM timescaledb_information.chunks c
WHERE c.hypertable_name = 'sensor_data'
  AND c.range_end < NOW() - INTERVAL '7 days';
```

---

### Check Compression Stats

```sql
SELECT chunk_name,
       before_compression_total_bytes,
       after_compression_total_bytes,
       before_compression_total_bytes::float / after_compression_total_bytes AS compression_ratio
FROM timescaledb_information.compressed_chunk_stats
WHERE hypertable_name = 'sensor_data';
```

**Example Output:**

| chunk_name | before_bytes | after_bytes | compression_ratio |
|------------|--------------|-------------|-------------------|
| _hyper_1_2_chunk | 1048576000 | 52428800 | 20.0 |

20x compression! 🎉

---

## Retention Policies

### Add Retention Policy

Automatically drop data older than 90 days:

```sql
SELECT add_retention_policy('sensor_data', INTERVAL '90 days');
```

TimescaleDB runs a background job to drop old chunks.

---

### View Retention Policies

```sql
SELECT * FROM timescaledb_information.jobs
WHERE proc_name = 'policy_retention';
```

---

### Remove Retention Policy

```sql
SELECT remove_retention_policy('sensor_data');
```

---

### Manual Data Deletion

```sql
-- Drop chunks older than 90 days
SELECT drop_chunks('sensor_data', INTERVAL '90 days');

-- Drop specific time range
SELECT drop_chunks('sensor_data', older_than => '2026-01-01'::TIMESTAMPTZ);
```

---

## Continuous Aggregates

### Create Continuous Aggregate

**Example: Hourly averages**

```sql
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

### Refresh Policy

Automatically refresh the aggregate as new data arrives:

```sql
SELECT add_continuous_aggregate_policy(
  'sensor_data_hourly',
  start_offset => INTERVAL '3 hours',
  end_offset => INTERVAL '1 hour',
  schedule_interval => INTERVAL '1 hour'
);
```

**Parameters:**

- `start_offset`: Don't aggregate data newer than this (allows late-arriving data)
- `end_offset`: Aggregate up to this point
- `schedule_interval`: Run the refresh job every hour

---

### Query Continuous Aggregate

```sql
-- Fast: pre-calculated
SELECT * FROM sensor_data_hourly
WHERE hour > NOW() - INTERVAL '7 days'
  AND sensor_id = 1
ORDER BY hour DESC;
```

**Benefits:**

- ✅ 10x-100x faster than querying raw data
- ✅ Automatic updates as new data arrives
- ✅ Can be compressed independently

---

### Refresh Manually

```sql
CALL refresh_continuous_aggregate('sensor_data_hourly', '2026-04-01', '2026-04-07');
```

---

## TypeScript Integration with Drizzle ORM

### Installation

```bash
npm install drizzle-orm pg
npm install -D @types/pg drizzle-kit
```

---

### Define Schema

```typescript
// schema.ts
import { pgTable, timestamp, integer, doublePrecision, text, index } from 'drizzle-orm/pg-core';

export const sensorData = pgTable('sensor_data', {
  time: timestamp('time').notNull(),
  sensorId: integer('sensor_id').notNull(),
  temperature: doublePrecision('temperature'),
  humidity: doublePrecision('humidity'),
  location: text('location'),
}, (table) => ({
  timeIdx: index('sensor_data_time_idx').on(table.time),
  sensorIdx: index('sensor_data_sensor_idx').on(table.sensorId),
}));
```

---

### Create Hypertable (Migration)

```typescript
// migrations/0001_create_hypertable.sql
-- Create table
CREATE TABLE sensor_data (
  time        TIMESTAMPTZ NOT NULL,
  sensor_id   INT NOT NULL,
  temperature FLOAT,
  humidity    FLOAT,
  location    TEXT
);

-- Convert to hypertable
SELECT create_hypertable('sensor_data', 'time');

-- Add compression
ALTER TABLE sensor_data SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'sensor_id'
);

-- Add compression policy
SELECT add_compression_policy('sensor_data', INTERVAL '7 days');

-- Add retention policy
SELECT add_retention_policy('sensor_data', INTERVAL '90 days');
```

Run migration:

```bash
drizzle-kit push:pg
```

---

### Insert Data

```typescript
import { db } from './db';
import { sensorData } from './schema';

// Single insert
await db.insert(sensorData).values({
  time: new Date(),
  sensorId: 1,
  temperature: 22.5,
  humidity: 65.0,
  location: 'Office A',
});

// Bulk insert
await db.insert(sensorData).values([
  { time: new Date(), sensorId: 1, temperature: 22.5, humidity: 65.0, location: 'Office A' },
  { time: new Date(), sensorId: 2, temperature: 23.1, humidity: 60.0, location: 'Office B' },
  { time: new Date(), sensorId: 3, temperature: 21.8, humidity: 68.0, location: 'Office C' },
]);
```

---

### Query with Time Ranges

```typescript
import { sql } from 'drizzle-orm';
import { gt } from 'drizzle-orm';

// Last 24 hours
const last24Hours = await db
  .select()
  .from(sensorData)
  .where(sql`${sensorData.time} > NOW() - INTERVAL '24 hours'`)
  .orderBy(sensorData.time);

// Specific sensor, last week
const oneDayAgo = new Date(Date.now() - 24 * 60 * 60 * 1000);

const sensorReadings = await db
  .select()
  .from(sensorData)
  .where(sql`${sensorData.time} > ${oneDayAgo} AND ${sensorData.sensorId} = 1`)
  .orderBy(sensorData.time);
```

---

### Time Bucketing (Downsampling)

```typescript
// Hourly averages
const hourlyAvg = await db.execute(sql`
  SELECT time_bucket('1 hour', time) AS hour,
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

### Continuous Aggregate Query

```typescript
// Query pre-calculated hourly data
const hourlyData = await db.execute(sql`
  SELECT * FROM sensor_data_hourly
  WHERE hour > NOW() - INTERVAL '7 days'
    AND sensor_id = 1
  ORDER BY hour DESC
`);

console.log(hourlyData.rows);
```

---

## Use Cases

### 1. IoT Sensor Monitoring

**Scenario:** Track temperature/humidity from 10,000 sensors, each reporting every 10 seconds.

**Data Rate:** 10,000 sensors × 6 readings/minute = 60,000 inserts/minute

**Solution:**

```sql
CREATE TABLE iot_sensors (
  time        TIMESTAMPTZ NOT NULL,
  device_id   TEXT NOT NULL,
  temperature FLOAT,
  humidity    FLOAT,
  battery     FLOAT,
  location    POINT
);

SELECT create_hypertable('iot_sensors', 'time');

-- Add compression (keep last 7 days uncompressed)
ALTER TABLE iot_sensors SET (timescaledb.compress);
SELECT add_compression_policy('iot_sensors', INTERVAL '7 days');

-- Retention (keep 1 year)
SELECT add_retention_policy('iot_sensors', INTERVAL '1 year');

-- Continuous aggregate (hourly stats)
CREATE MATERIALIZED VIEW iot_sensors_hourly
WITH (timescaledb.continuous) AS
SELECT time_bucket('1 hour', time) AS hour,
       device_id,
       AVG(temperature) AS avg_temp,
       AVG(humidity) AS avg_humidity,
       MIN(battery) AS min_battery
FROM iot_sensors
GROUP BY hour, device_id;
```

**Query: Find devices with low battery**

```sql
SELECT DISTINCT device_id, min_battery
FROM iot_sensors_hourly
WHERE hour > NOW() - INTERVAL '24 hours'
  AND min_battery < 20
ORDER BY min_battery;
```

---

### 2. Application Performance Monitoring (APM)

**Scenario:** Track API response times, error rates, CPU/memory usage.

```sql
CREATE TABLE app_metrics (
  time          TIMESTAMPTZ NOT NULL,
  service_name  TEXT NOT NULL,
  endpoint      TEXT,
  response_time FLOAT,
  status_code   INT,
  cpu_percent   FLOAT,
  memory_mb     FLOAT
);

SELECT create_hypertable('app_metrics', 'time');

-- Downsampled view: 5-minute averages
CREATE MATERIALIZED VIEW app_metrics_5min
WITH (timescaledb.continuous) AS
SELECT time_bucket('5 minutes', time) AS bucket,
       service_name,
       endpoint,
       AVG(response_time) AS avg_response_time,
       percentile_cont(0.95) WITHIN GROUP (ORDER BY response_time) AS p95_response_time,
       COUNT(*) FILTER (WHERE status_code >= 500) AS error_count,
       AVG(cpu_percent) AS avg_cpu
FROM app_metrics
GROUP BY bucket, service_name, endpoint;
```

**Query: Find slow endpoints (P95 > 1 second)**

```sql
SELECT service_name, endpoint, p95_response_time
FROM app_metrics_5min
WHERE bucket > NOW() - INTERVAL '1 hour'
  AND p95_response_time > 1000
ORDER BY p95_response_time DESC;
```

---

### 3. Financial Data (Stock Ticks)

**Scenario:** Store stock price ticks with microsecond precision.

```sql
CREATE TABLE stock_ticks (
  time      TIMESTAMPTZ(6) NOT NULL,  -- Microsecond precision
  symbol    TEXT NOT NULL,
  price     NUMERIC(10, 2),
  volume    BIGINT,
  exchange  TEXT
);

SELECT create_hypertable('stock_ticks', 'time', chunk_time_interval => INTERVAL '1 day');

-- Compression by symbol
ALTER TABLE stock_ticks SET (
  timescaledb.compress,
  timescaledb.compress_segmentby = 'symbol'
);
SELECT add_compression_policy('stock_ticks', INTERVAL '7 days');
```

**Query: OHLC (Open, High, Low, Close) per minute**

```sql
SELECT time_bucket('1 minute', time) AS minute,
       symbol,
       FIRST(price, time) AS open,
       MAX(price) AS high,
       MIN(price) AS low,
       LAST(price, time) AS close,
       SUM(volume) AS total_volume
FROM stock_ticks
WHERE symbol = 'AAPL'
  AND time > NOW() - INTERVAL '1 day'
GROUP BY minute, symbol
ORDER BY minute;
```

---

## Performance Best Practices

### 1. Chunk Size Tuning

**Default chunk size:** 7 days

For high-frequency data, reduce chunk size:

```sql
SELECT set_chunk_time_interval('sensor_data', INTERVAL '1 day');
```

For low-frequency data, increase chunk size:

```sql
SELECT set_chunk_time_interval('sensor_data', INTERVAL '30 days');
```

**Rule of Thumb:** Each chunk should be 25% of available RAM.

---

### 2. Indexing

```sql
-- Index on time (automatic with hypertables)
-- Index on frequently filtered columns
CREATE INDEX ON sensor_data (sensor_id, time DESC);

-- Composite index
CREATE INDEX ON sensor_data (location, time DESC) WHERE temperature > 30;
```

---

### 3. Batch Inserts

```typescript
// ❌ Slow: 1000 individual inserts
for (const reading of readings) {
  await db.insert(sensorData).values(reading);
}

// ✅ Fast: Single batch insert
await db.insert(sensorData).values(readings);
```

---

### 4. Use Continuous Aggregates for Dashboards

**Don't:**

```sql
-- Expensive: recalculates every time
SELECT time_bucket('1 hour', time) AS hour, AVG(temperature)
FROM sensor_data
WHERE time > NOW() - INTERVAL '30 days'
GROUP BY hour;
```

**Do:**

```sql
-- Fast: pre-calculated
SELECT * FROM sensor_data_hourly
WHERE hour > NOW() - INTERVAL '30 days';
```

---

## Common Pitfalls

### ❌ Forgetting to Create Hypertable

```sql
-- ❌ Just a regular table (no TimescaleDB features)
CREATE TABLE sensor_data (...);

-- ✅ Convert to hypertable
SELECT create_hypertable('sensor_data', 'time');
```

---

### ❌ Not Using time_bucket for Aggregations

```sql
-- ❌ Slow: GROUP BY raw timestamps
SELECT time, AVG(temperature)
FROM sensor_data
GROUP BY time;

-- ✅ Fast: GROUP BY time buckets
SELECT time_bucket('1 hour', time) AS hour, AVG(temperature)
FROM sensor_data
GROUP BY hour;
```

---

### ❌ Querying Compressed Data with UPDATES

```sql
-- ❌ Error: Cannot UPDATE compressed chunks
UPDATE sensor_data SET temperature = 25 WHERE sensor_id = 1;

-- ✅ Decompress first (expensive)
SELECT decompress_chunk('_timescaledb_internal._hyper_1_2_chunk');
UPDATE sensor_data SET temperature = 25 WHERE sensor_id = 1;
```

**Best Practice:** TimescaleDB is optimized for **append-only** workloads (INSERT). Avoid UPDATEs on time-series data.

---

## Summary

| Feature | Description |
|---------|-------------|
| **Hypertables** | Automatic time-based partitioning |
| **Chunks** | Internal partitions (default: 7 days) |
| **Compression** | 10x-20x storage reduction with columnar compression |
| **Retention Policies** | Automatic deletion of old data |
| **Continuous Aggregates** | Materialized views that auto-update |
| **time_bucket()** | Downsampling into fixed intervals |
| **Gap Filling** | Fill missing time intervals with NULL or interpolation |
| **Drizzle Integration** | Full PostgreSQL compatibility |

---

## Next Steps

1. ✅ Set up TimescaleDB with Docker
2. ✅ Create hypertable for sensor data
3. ✅ Insert time-series data
4. ✅ Query with time_bucket() for downsampling
5. ✅ Add compression and retention policies
6. ✅ Create continuous aggregates for dashboards
7. 📚 Read [TimescaleDB Documentation](https://docs.timescale.com/)

---

**Happy time-series analysis! 🚀**
