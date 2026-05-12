# Week 13 - Time Series & Search Engines

**Dates:** April 6-10, 2026

---

## Overview

This week explores two specialized database technologies designed for specific use cases:

1. **TimescaleDB** - Time-series database (PostgreSQL extension) optimized for IoT, monitoring, and analytics workloads
2. **Elasticsearch** - Search engine optimized for full-text search, log analysis, and real-time analytics

Both technologies excel in their domains but require understanding their unique data models and query patterns.

---

## Learning Objectives

By the end of this week, you will:

- ✅ Understand when to use time-series databases vs relational databases
- ✅ Create and query TimescaleDB hypertables
- ✅ Use `time_bucket()` for downsampling time-series data
- ✅ Configure compression and retention policies
- ✅ Build continuous aggregates for precomputed analytics
- ✅ Understand inverted indices and how they enable fast full-text search
- ✅ Define Elasticsearch mappings with text and keyword fields
- ✅ Write queries using Elasticsearch Query DSL (match, term, bool)
- ✅ Create aggregations for analytics (terms, stats, histogram)
- ✅ Integrate TimescaleDB and Elasticsearch with TypeScript

---

## Weekly Schedule

### Monday, April 6 - TimescaleDB

**Topics:**

- What are time-series databases?
- TimescaleDB architecture: hypertables and chunks
- Creating hypertables
- Querying with `time_bucket()` for downsampling
- Compression policies (10x-20x storage reduction)
- Retention policies (automatic data deletion)
- Continuous aggregates (materialized views)
- Use cases: IoT sensors, APM, financial ticks
- Drizzle ORM integration

**Reading:**

- [timescaledb-fundamentals.md](./timescaledb-fundamentals.md)
- [TimescaleDB Documentation](https://docs.timescale.com/)

**Practice:**

```bash
# Start TimescaleDB
docker-compose up -d

# Run sample queries from fundamentals guide
psql -h localhost -U postgres -d timescale_db
```

---

### Thursday, April 9 - Elasticsearch

**Topics:**

- What are search engines?
- Inverted indices explained
- Documents, indices, and mappings
- Field types: text vs keyword
- Analyzers and tokenizers (standard, english, custom)
- Query DSL: match, term, bool, range
- Aggregations: terms, stats, histogram
- Use cases: e-commerce search, log analysis, autocomplete
- @elastic/elasticsearch client

**Reading:**

- [elasticsearch-fundamentals.md](./elasticsearch-fundamentals.md)
- [Elasticsearch Reference](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)

**Practice:**

```bash
# Start Elasticsearch
docker-compose up -d

# Access Kibana Dev Tools
open http://localhost:5601/app/dev_tools#/console

# Run sample queries from fundamentals guide
```

---

## Assessments

### Quiz 11 - Time Series & Search Engines

**Due:** Thursday, April 9, 2026 by 11:59 PM

**Topics:**

- TimescaleDB: hypertables, time_bucket(), compression, retention, continuous aggregates
- Elasticsearch: inverted indices, mappings, analyzers, query DSL, aggregations

**Format:**

- 12 multiple-choice questions
- Detailed explanations for each answer

**Access Quiz:**

- [quiz/timeseries-search-quiz.md](./quiz/timeseries-search-quiz.md)

---

### Final Project - Checkpoint #2

**Due:** Sunday, April 12, 2026 by 11:59 PM

**Requirements:**

1. **Database Implementation Progress** (40%)
   - Schema finalized with tables/collections created
   - Sample data inserted (at least 100+ records)
   - Database running in Docker (docker-compose.yml)

2. **Code Progress** (30%)
   - Repository with initial commit
   - API endpoints or core functionality started
   - TypeScript/JavaScript integration with chosen database

3. **Documentation** (20%)
   - Updated README with setup instructions
   - Database schema diagram or description
   - API documentation or usage examples

4. **Demo Video** (10%)
   - 3-5 minute screen recording
   - Show database setup and running queries
   - Demonstrate basic CRUD operations

**Submission:**

- Submit GitHub repository link
- Submit video link (YouTube, Loom, etc.)

---

## Practical Exercises

### Exercise 1: IoT Sensor Monitoring with TimescaleDB

**Scenario:** Build a temperature monitoring system for 100 sensors reporting every minute.

**Tasks:**

1. Create `sensor_data` hypertable:
   ```sql
   CREATE TABLE sensor_data (
     time TIMESTAMPTZ NOT NULL,
     sensor_id INT NOT NULL,
     temperature FLOAT NOT NULL,
     humidity FLOAT
   );
   
   SELECT create_hypertable('sensor_data', 'time');
   ```

2. Insert sample data (10,000 records)
3. Query average temperature per hour using `time_bucket()`
4. Set up compression policy for data older than 7 days
5. Create retention policy to delete data older than 90 days
6. Build continuous aggregate for daily averages

**Expected Output:**

- Query performance comparison: plain PostgreSQL vs TimescaleDB
- Storage size before and after compression
- Continuous aggregate refreshed automatically

---

### Exercise 2: E-commerce Product Search with Elasticsearch

**Scenario:** Build a product search engine with 1,000 products.

**Tasks:**

1. Create `products` index with mappings:
   ```json
   {
     "mappings": {
       "properties": {
         "name": { "type": "text" },
         "description": { "type": "text" },
         "category": { "type": "keyword" },
         "price": { "type": "float" },
         "tags": { "type": "keyword" },
         "in_stock": { "type": "boolean" }
       }
     }
   }
   ```

2. Bulk insert 1,000 products
3. Implement full-text search ("wireless mouse")
4. Filter by category and price range
5. Create aggregations:
   - Count products per category
   - Price statistics (min, max, avg)
   - Price histogram ($0-$25, $25-$50, $50-$100, $100+)
6. Build autocomplete with prefix query

**Expected Output:**

- Relevant search results with scores
- Faceted search filters (category, price range)
- Fast autocomplete suggestions

---

### Exercise 3: TypeScript Integration

**TimescaleDB with Drizzle:**

```typescript
import { pgTable, serial, timestamp, doublePrecision } from 'drizzle-orm/pg-core';
import { drizzle } from 'drizzle-orm/node-postgres';
import { sql } from 'drizzle-orm';

const sensorData = pgTable('sensor_data', {
  time: timestamp('time').notNull(),
  sensorId: serial('sensor_id').notNull(),
  temperature: doublePrecision('temperature').notNull(),
});

const db = drizzle(process.env.DATABASE_URL!);

// Query with time_bucket
const hourlyAvg = await db.execute(sql`
  SELECT 
    time_bucket('1 hour', time) AS hour,
    AVG(temperature) AS avg_temp
  FROM sensor_data
  WHERE time > NOW() - INTERVAL '1 day'
  GROUP BY hour
  ORDER BY hour DESC
`);
```

**Elasticsearch with @elastic/elasticsearch:**

```typescript
import { Client } from '@elastic/elasticsearch';

const client = new Client({ node: 'http://localhost:9200' });

// Search products
const results = await client.search({
  index: 'products',
  body: {
    query: {
      bool: {
        must: [{ match: { description: 'wireless' } }],
        filter: [
          { term: { category: 'Electronics' } },
          { range: { price: { lte: 100 } } },
        ],
      },
    },
    aggs: {
      categories: { terms: { field: 'category' } },
    },
  },
});
```

---

## Docker Setup

### docker-compose.yml

```yaml
version: '3.8'

services:
  # TimescaleDB
  timescaledb:
    image: timescale/timescaledb:latest-pg16
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: timescale_db
    volumes:
      - timescale_data:/var/lib/postgresql/data

  # Elasticsearch
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.12.0
    ports:
      - "9200:9200"
      - "9300:9300"
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"
    volumes:
      - es_data:/usr/share/elasticsearch/data

  # Kibana (Elasticsearch UI)
  kibana:
    image: docker.elastic.co/kibana/kibana:8.12.0
    ports:
      - "5601:5601"
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch

volumes:
  timescale_data:
  es_data:
```

```bash
# Start all services
docker-compose up -d

# Wait 30-60 seconds for startup

# Verify TimescaleDB
psql -h localhost -U postgres -d timescale_db -c "SELECT version();"

# Verify Elasticsearch
curl http://localhost:9200

# Access Kibana
open http://localhost:5601
```

---

## Common Pitfalls

### TimescaleDB

❌ **Forgetting to create hypertable**

```sql
-- Wrong: Just a regular table
CREATE TABLE sensor_data (time TIMESTAMPTZ, temp FLOAT);

-- Correct: Create hypertable
CREATE TABLE sensor_data (time TIMESTAMPTZ, temp FLOAT);
SELECT create_hypertable('sensor_data', 'time');
```

❌ **Not using time_bucket() for aggregations**

```sql
-- Wrong: date_trunc is less flexible
SELECT date_trunc('hour', time), AVG(temp) FROM sensor_data GROUP BY 1;

-- Correct: time_bucket supports arbitrary intervals
SELECT time_bucket('15 minutes', time), AVG(temp) FROM sensor_data GROUP BY 1;
```

❌ **Querying compressed data with UPDATE/DELETE**

```sql
-- Error: Cannot update compressed chunks
UPDATE sensor_data SET temp = 25 WHERE time < NOW() - INTERVAL '7 days';

-- Solution: Decompress first or prevent compression
SELECT decompress_chunk('_timescaledb_internal._hyper_1_1_chunk');
```

---

### Elasticsearch

❌ **Using text fields for exact matches**

```json
// Wrong: "Electronics" is analyzed to "electronics"
{ "term": { "category": "Electronics" } }

// Correct: Use keyword field
{ "term": { "category.keyword": "Electronics" } }
```

❌ **Using must when filter is sufficient**

```json
// Slow: Calculates relevance score for category
{
  "bool": {
    "must": [
      { "term": { "category": "Electronics" } }
    ]
  }
}

// Fast: Skips scoring
{
  "bool": {
    "filter": [
      { "term": { "category": "Electronics" } }
    ]
  }
}
```

❌ **Not refreshing after bulk insert**

```typescript
// Documents not immediately searchable
await client.bulk({ body });

// Force refresh
await client.bulk({ body, refresh: true });
```

---

## Decision Matrix

### When to Use TimescaleDB

✅ **Use TimescaleDB when:**

- Time-series data (IoT, metrics, logs, financial)
- Need SQL and ACID guarantees
- Complex joins required
- PostgreSQL ecosystem (extensions, tools)
- High cardinality data (millions of unique sensors)

❌ **Don't use TimescaleDB when:**

- Data is not time-series
- Need distributed writes across multiple data centers
- Prefer NoSQL data model

---

### When to Use Elasticsearch

✅ **Use Elasticsearch when:**

- Full-text search required
- Log analysis and monitoring
- Real-time analytics and aggregations
- Fuzzy search and typo tolerance
- Faceted search (filters, categories)

❌ **Don't use Elasticsearch when:**

- Need strong consistency (ACID)
- Transactional workloads
- Complex joins and relationships
- Primary data store (use as secondary search index)

---

## Additional Resources

- **Readings:** [readings-13.md](./readings-13.md)
- **TimescaleDB Docs:** https://docs.timescale.com/
- **Elasticsearch Docs:** https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html
- **Drizzle ORM:** https://orm.drizzle.team/
- **@elastic/elasticsearch:** https://www.elastic.co/guide/en/elasticsearch/client/javascript-api/current/index.html

---

## Summary

| Technology | Type | Use Cases | Query Language | ACID |
|-----------|------|-----------|----------------|------|
| **TimescaleDB** | Time-series DB | IoT, metrics, monitoring | SQL | Yes |
| **Elasticsearch** | Search engine | Full-text search, logs | Query DSL (JSON) | No |

**Key Takeaways:**

- TimescaleDB = PostgreSQL + automatic time-based partitioning + compression
- Elasticsearch = Inverted indices + full-text search + real-time aggregations
- Use the right tool for the job: SQL for transactions, TimescaleDB for time-series, Elasticsearch for search

---

**Good luck this week! 🚀**
