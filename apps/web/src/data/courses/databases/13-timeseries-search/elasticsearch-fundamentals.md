# Elasticsearch Fundamentals — Search Engines

## Introduction

**Elasticsearch** is a distributed search and analytics engine built on Apache Lucene. It excels at **full-text search**, real-time analytics, and handling massive amounts of unstructured data like logs, documents, and product catalogs.

---

## What is Elasticsearch?

Elasticsearch is optimized for **search**, not transactions. It uses **inverted indices** to enable fast full-text search across billions of documents.

### Elasticsearch vs Relational Databases

| Aspect | Relational DB (PostgreSQL) | Elasticsearch |
|--------|---------------------------|---------------|
| **Primary Use** | Transactions, ACID guarantees | Full-text search, analytics |
| **Query Type** | Exact matches (`WHERE name = 'Alice'`) | Fuzzy search, relevance scoring |
| **Performance** | Fast for structured queries | Fast for text search, aggregations |
| **Schema** | Strict schema required | Dynamic schema (schema-less) |
| **Joins** | Supported (INNER, LEFT, etc.) | Not supported (denormalization required) |
| **Indexing** | B-tree indices | Inverted indices |
| **ACID** | Full ACID support | Eventually consistent |

**When to Use Elasticsearch:**

- ✅ Full-text search (search bars, autocomplete)
- ✅ Log analysis (application logs, system logs)
- ✅ Analytics dashboards (Kibana)
- ✅ Product catalogs with faceted search
- ✅ Real-time monitoring (ELK stack)

**When to Avoid Elasticsearch:**

- ❌ Transactional systems (banking, e-commerce orders)
- ❌ Strong consistency requirements
- ❌ Complex joins and relationships

---

## Core Concepts

### 1. Documents

A **document** is a JSON object stored in Elasticsearch. It's equivalent to a row in a relational database.

**Example Document:**

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

---

### 2. Indices

An **index** is a collection of documents. It's equivalent to a table in a relational database.

**Example:** `products` index contains product documents.

**Naming Convention:**

- Lowercase only
- No special characters except `-`, `_`, `.`
- Examples: `products`, `users-2026`, `logs-app-prod`

---

### 3. Inverted Index

Elasticsearch uses **inverted indices** for fast full-text search.

**How It Works:**

**Original Documents:**

```
Doc 1: "Quick brown fox"
Doc 2: "Brown cat"
Doc 3: "Fox jumps over"
```

**Inverted Index:**

| Term | Documents |
|------|-----------|
| quick | Doc 1 |
| brown | Doc 1, Doc 2 |
| fox | Doc 1, Doc 3 |
| cat | Doc 2 |
| jumps | Doc 3 |
| over | Doc 3 |

**Query:** "brown fox"

**Result:** Doc 1 (contains both terms), Doc 2 (contains "brown"), Doc 3 (contains "fox")

**Relevance Scoring:** Doc 1 scores highest because it contains both terms.

---

### 4. Mappings

**Mappings** define the schema for documents: field types, analyzers, and index settings.

**Example Mapping:**

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

**Field Types:**

| Type | Description | Example |
|------|-------------|---------|
| **text** | Full-text searchable (analyzed) | Product descriptions |
| **keyword** | Exact matches, not analyzed | Tags, categories, IDs |
| **integer** | Whole numbers | Quantity, age |
| **float** / **double** | Decimals | Price, ratings |
| **boolean** | true/false | in_stock, published |
| **date** | ISO 8601 dates | "2026-04-01T10:00:00Z" |
| **object** | Nested JSON | { "user": { "name": "Alice" } } |
| **geo_point** | Latitude/longitude | { "lat": 40.7, "lon": -74.0 } |

---

### 5. Analyzers

**Analyzers** process text fields during indexing and searching. They:

1. **Tokenize**: Split text into terms ("Quick brown fox" → ["quick", "brown", "fox"])
2. **Lowercase**: Convert to lowercase ("Quick" → "quick")
3. **Remove stopwords**: Remove common words ("the", "a", "is")
4. **Stem**: Reduce words to root form ("running" → "run")

**Example:**

**Input:** "The Quick BROWN Fox is Running"

**After Standard Analyzer:**

```
["quick", "brown", "fox", "run"]
```

**Built-in Analyzers:**

- **standard**: Tokenize, lowercase, remove stopwords
- **simple**: Tokenize by non-letter characters, lowercase
- **whitespace**: Tokenize by whitespace only
- **keyword**: No analysis (exact match)
- **english**: Standard + English stemming ("running" → "run")

---

## Docker Setup

### Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.12.0
    ports:
      - "9200:9200"  # HTTP API
      - "9300:9300"  # Transport (cluster communication)
    environment:
      - discovery.type=single-node
      - xpack.security.enabled=false  # Disable security for dev
      - "ES_JAVA_OPTS=-Xms512m -Xmx512m"  # Limit memory
    volumes:
      - es_data:/usr/share/elasticsearch/data

  kibana:
    image: docker.elastic.co/kibana/kibana:8.12.0
    ports:
      - "5601:5601"  # Kibana UI
    environment:
      - ELASTICSEARCH_HOSTS=http://elasticsearch:9200
    depends_on:
      - elasticsearch

volumes:
  es_data:
```

```bash
# Start Elasticsearch
docker-compose up -d

# Wait 30-60 seconds for startup

# Verify Elasticsearch
curl http://localhost:9200
# Should return cluster info JSON

# Access Kibana
open http://localhost:5601
```

---

## CRUD Operations

### Create Index

```bash
# Create index with mappings
curl -X PUT "localhost:9200/products" -H 'Content-Type: application/json' -d'
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
'
```

---

### Index Documents (INSERT)

**Single Document:**

```bash
curl -X POST "localhost:9200/products/_doc/1" -H 'Content-Type: application/json' -d'
{
  "name": "Wireless Mouse",
  "description": "Ergonomic wireless mouse with USB receiver",
  "price": 29.99,
  "category": "Electronics",
  "tags": ["wireless", "mouse", "ergonomic"],
  "in_stock": true,
  "created_at": "2026-04-01T10:00:00Z"
}
'
```

**Auto-Generated ID:**

```bash
curl -X POST "localhost:9200/products/_doc" -H 'Content-Type: application/json' -d'
{
  "name": "USB Keyboard",
  "price": 49.99,
  "category": "Electronics"
}
'
```

**Bulk Insert:**

```bash
curl -X POST "localhost:9200/products/_bulk" -H 'Content-Type: application/json' -d'
{"index": {"_id": "2"}}
{"name": "USB Keyboard", "price": 49.99, "category": "Electronics", "in_stock": true}
{"index": {"_id": "3"}}
{"name": "HDMI Cable", "price": 12.99, "category": "Electronics", "in_stock": true}
{"index": {"_id": "4"}}
{"name": "Office Chair", "price": 199.99, "category": "Furniture", "in_stock": false}
'
```

---

### Retrieve Documents (READ)

**Get by ID:**

```bash
curl -X GET "localhost:9200/products/_doc/1"
```

**Search All:**

```bash
curl -X GET "localhost:9200/products/_search?pretty"
```

---

### Update Documents

**Partial Update:**

```bash
curl -X POST "localhost:9200/products/_update/1" -H 'Content-Type: application/json' -d'
{
  "doc": {
    "price": 24.99,
    "in_stock": false
  }
}
'
```

**Full Replace:**

```bash
curl -X PUT "localhost:9200/products/_doc/1" -H 'Content-Type: application/json' -d'
{
  "name": "Wireless Mouse Pro",
  "price": 34.99,
  "category": "Electronics",
  "in_stock": true
}
'
```

---

### Delete Documents

**Delete by ID:**

```bash
curl -X DELETE "localhost:9200/products/_doc/1"
```

**Delete by Query:**

```bash
curl -X POST "localhost:9200/products/_delete_by_query" -H 'Content-Type: application/json' -d'
{
  "query": {
    "term": {
      "category": "Furniture"
    }
  }
}
'
```

---

### Delete Index

```bash
curl -X DELETE "localhost:9200/products"
```

---

## Query DSL (Domain Specific Language)

Elasticsearch uses a JSON-based query language.

### Match Query (Full-Text Search)

**Find products matching "wireless mouse":**

```json
{
  "query": {
    "match": {
      "description": "wireless mouse"
    }
  }
}
```

```bash
curl -X GET "localhost:9200/products/_search" -H 'Content-Type: application/json' -d'
{
  "query": {
    "match": {
      "description": "wireless mouse"
    }
  }
}
'
```

**How it works:**

1. Analyzer tokenizes query: ["wireless", "mouse"]
2. Searches inverted index for documents containing either term
3. Scores by relevance (documents with both terms score higher)

---

### Term Query (Exact Match)

**Find products with exact category "Electronics":**

```json
{
  "query": {
    "term": {
      "category": "Electronics"
    }
  }
}
```

**Note:** `term` queries are case-sensitive and not analyzed. Use for **keyword** fields.

---

### Range Query

**Find products priced between $20 and $50:**

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

**Date Range:**

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

### Bool Query (Combine Multiple Conditions)

**Boolean Logic:**

- **must**: AND (must match, affects score)
- **filter**: AND (must match, doesn't affect score)
- **should**: OR (should match, affects score)
- **must_not**: NOT (must not match)

**Example: Find in-stock electronics priced under $100**

```json
{
  "query": {
    "bool": {
      "must": [
        { "term": { "category": "Electronics" } },
        { "term": { "in_stock": true } }
      ],
      "filter": [
        { "range": { "price": { "lt": 100 } } }
      ]
    }
  }
}
```

**Example: Search "wireless" OR "bluetooth" in description**

```json
{
  "query": {
    "bool": {
      "should": [
        { "match": { "description": "wireless" } },
        { "match": { "description": "bluetooth" } }
      ]
    }
  }
}
```

---

### Multi-Match Query (Search Across Fields)

**Search "mouse" in name OR description:**

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

**Boosting (Prioritize name over description):**

```json
{
  "query": {
    "multi_match": {
      "query": "mouse",
      "fields": ["name^2", "description"]
    }
  }
}
```

`name^2` means name has 2x weight in relevance scoring.

---

### Fuzzy Query (Typo Tolerance)

**Find "mose" (typo for "mouse"):**

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

**Fuzziness:**

- `AUTO`: 0-2 edits based on term length
- `0`: Exact match
- `1`: 1 character difference
- `2`: 2 character differences

---

### Prefix Query (Autocomplete)

**Find products starting with "wire":**

```json
{
  "query": {
    "prefix": {
      "name": "wire"
    }
  }
}
```

Matches: "Wireless Mouse", "Wired Keyboard"

---

### Wildcard Query

**Find products with "mou*" (mouse, mount, mountain):**

```json
{
  "query": {
    "wildcard": {
      "name": "*mou*"
    }
  }
}
```

**Note:** Wildcards are slow on large datasets. Use prefix queries when possible.

---

### Exists Query (Check for Field Presence)

**Find products with a "tags" field:**

```json
{
  "query": {
    "exists": {
      "field": "tags"
    }
  }
}
```

---

## Aggregations

**Aggregations** compute analytics (counts, averages, histograms) over search results.

### Terms Aggregation (Group By)

**Count products by category:**

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
        { "key": "Electronics", "doc_count": 3 },
        { "key": "Furniture", "doc_count": 1 }
      ]
    }
  }
}
```

---

### Stats Aggregation (Min, Max, Avg, Sum)

**Price statistics:**

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
      "count": 4,
      "min": 12.99,
      "max": 199.99,
      "avg": 73.24,
      "sum": 292.96
    }
  }
}
```

---

### Histogram Aggregation (Bucketing)

**Price distribution in $50 buckets:**

```json
{
  "size": 0,
  "aggs": {
    "price_histogram": {
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
    "price_histogram": {
      "buckets": [
        { "key": 0, "doc_count": 2 },    // $0-$50
        { "key": 50, "doc_count": 1 },   // $50-$100
        { "key": 150, "doc_count": 1 }   // $150-$200
      ]
    }
  }
}
```

---

### Date Histogram (Time-Based Bucketing)

**Products created per day:**

```json
{
  "size": 0,
  "aggs": {
    "products_per_day": {
      "date_histogram": {
        "field": "created_at",
        "calendar_interval": "day"
      }
    }
  }
}
```

---

## TypeScript Integration with @elastic/elasticsearch

### Installation

```bash
npm install @elastic/elasticsearch
npm install -D @types/node
```

---

### Basic Connection

```typescript
import { Client } from '@elastic/elasticsearch';

const client = new Client({
  node: 'http://localhost:9200',
});

// Verify connection
async function checkConnection() {
  const info = await client.info();
  console.log('Elasticsearch cluster:', info);
}

checkConnection();
```

---

### Create Index with Mappings

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
  
  console.log('Index created');
}
```

---

### Index Documents

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
  
  console.log('Indexed document:', response._id);
  return response;
}

// Usage
await indexProduct({
  name: 'Wireless Mouse',
  description: 'Ergonomic wireless mouse with USB receiver',
  price: 29.99,
  category: 'Electronics',
  tags: ['wireless', 'mouse', 'ergonomic'],
  in_stock: true,
  created_at: new Date(),
}, '1');
```

---

### Bulk Index

```typescript
async function bulkIndexProducts(products: Product[]) {
  const body = products.flatMap((product, index) => [
    { index: { _index: 'products', _id: String(index + 1) } },
    product,
  ]);
  
  const response = await client.bulk({ body, refresh: true });
  
  console.log('Bulk indexed:', response.items.length, 'documents');
  return response;
}

// Usage
await bulkIndexProducts([
  { name: 'USB Keyboard', price: 49.99, category: 'Electronics', in_stock: true, created_at: new Date() },
  { name: 'HDMI Cable', price: 12.99, category: 'Electronics', in_stock: true, created_at: new Date() },
  { name: 'Office Chair', price: 199.99, category: 'Furniture', in_stock: false, created_at: new Date() },
]);
```

---

### Search

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
  
  const hits = response.hits.hits.map(hit => ({
    id: hit._id,
    score: hit._score,
    ...hit._source as Product,
  }));
  
  return hits;
}

// Usage
const results = await searchProducts('wireless mouse');
console.log(results);
```

---

### Filter by Category

```typescript
async function getProductsByCategory(category: string) {
  const response = await client.search({
    index: 'products',
    body: {
      query: {
        term: {
          category,
        },
      },
    },
  });
  
  return response.hits.hits.map(hit => hit._source as Product);
}

// Usage
const electronics = await getProductsByCategory('Electronics');
```

---

### Complex Bool Query

```typescript
async function searchInStockElectronics(query: string, maxPrice: number) {
  const response = await client.search({
    index: 'products',
    body: {
      query: {
        bool: {
          must: [
            { match: { description: query } },
            { term: { category: 'Electronics' } },
            { term: { in_stock: true } },
          ],
          filter: [
            { range: { price: { lte: maxPrice } } },
          ],
        },
      },
      sort: [
        { price: 'asc' },
      ],
    },
  });
  
  return response.hits.hits.map(hit => hit._source as Product);
}

// Usage
const results = await searchInStockElectronics('wireless', 50);
```

---

### Aggregations

```typescript
async function getProductStatsByCategory() {
  const response = await client.search({
    index: 'products',
    body: {
      size: 0,
      aggs: {
        categories: {
          terms: {
            field: 'category',
          },
          aggs: {
            avg_price: {
              avg: {
                field: 'price',
              },
            },
            max_price: {
              max: {
                field: 'price',
              },
            },
          },
        },
      },
    },
  });
  
  return response.aggregations?.categories.buckets.map((bucket: any) => ({
    category: bucket.key,
    count: bucket.doc_count,
    avgPrice: bucket.avg_price.value,
    maxPrice: bucket.max_price.value,
  }));
}

// Usage
const stats = await getProductStatsByCategory();
console.log(stats);
// [
//   { category: 'Electronics', count: 3, avgPrice: 30.95, maxPrice: 49.99 },
//   { category: 'Furniture', count: 1, avgPrice: 199.99, maxPrice: 199.99 }
// ]
```

---

## Use Cases

### 1. E-commerce Product Search

**Features:**

- Full-text search across name and description
- Faceted search (filter by category, price range, brand)
- Autocomplete
- Fuzzy search (typo tolerance)

**Example:**

```typescript
async function productSearch(
  query: string,
  category?: string,
  minPrice?: number,
  maxPrice?: number
) {
  const must: any[] = [];
  const filter: any[] = [];
  
  if (query) {
    must.push({
      multi_match: {
        query,
        fields: ['name^3', 'description', 'tags^2'],
        fuzziness: 'AUTO',
      },
    });
  }
  
  if (category) {
    filter.push({ term: { category } });
  }
  
  if (minPrice || maxPrice) {
    filter.push({
      range: {
        price: {
          ...(minPrice && { gte: minPrice }),
          ...(maxPrice && { lte: maxPrice }),
        },
      },
    });
  }
  
  const response = await client.search({
    index: 'products',
    body: {
      query: {
        bool: {
          must,
          filter,
        },
      },
      aggs: {
        categories: {
          terms: { field: 'category' },
        },
        price_ranges: {
          range: {
            field: 'price',
            ranges: [
              { to: 25 },
              { from: 25, to: 50 },
              { from: 50, to: 100 },
              { from: 100 },
            ],
          },
        },
      },
    },
  });
  
  return {
    products: response.hits.hits.map(hit => hit._source),
    facets: response.aggregations,
  };
}
```

---

### 2. Log Analysis (ELK Stack)

**Scenario:** Store and search application logs.

**Mapping:**

```typescript
await client.indices.create({
  index: 'logs-app-2026',
  body: {
    mappings: {
      properties: {
        timestamp: { type: 'date' },
        level: { type: 'keyword' },
        message: { type: 'text' },
        service: { type: 'keyword' },
        user_id: { type: 'keyword' },
        request_id: { type: 'keyword' },
        ip_address: { type: 'ip' },
        duration_ms: { type: 'integer' },
      },
    },
  },
});
```

**Query: Find errors in last hour:**

```typescript
const response = await client.search({
  index: 'logs-app-2026',
  body: {
    query: {
      bool: {
        must: [
          { term: { level: 'ERROR' } },
          { range: { timestamp: { gte: 'now-1h' } } },
        ],
      },
    },
    sort: [{ timestamp: 'desc' }],
  },
});
```

---

### 3. Autocomplete

**Mapping with completion suggester:**

```typescript
await client.indices.create({
  index: 'products',
  body: {
    mappings: {
      properties: {
        name: { type: 'text' },
        suggest: {
          type: 'completion',
        },
      },
    },
  },
});
```

**Index with suggestions:**

```typescript
await client.index({
  index: 'products',
  body: {
    name: 'Wireless Mouse',
    suggest: {
      input: ['wireless mouse', 'mouse', 'wireless'],
    },
  },
});
```

**Autocomplete query:**

```typescript
async function autocomplete(prefix: string) {
  const response = await client.search({
    index: 'products',
    body: {
      suggest: {
        product_suggest: {
          prefix,
          completion: {
            field: 'suggest',
          },
        },
      },
    },
  });
  
  return response.suggest?.product_suggest[0].options.map(option => option.text);
}

// Usage
await autocomplete('wire');  // Returns: ["wireless mouse", "wireless"]
```

---

## Performance Best Practices

### 1. Use Keyword for Exact Matches

```json
// ❌ Slow: text field with term query (analyzed)
{ "term": { "category.keyword": "Electronics" } }

// ✅ Fast: keyword field
{ "term": { "category": "Electronics" } }
```

---

### 2. Use Filter Context for Non-Scoring Queries

```json
// ❌ Slow: must (calculates relevance score)
{
  "bool": {
    "must": [
      { "term": { "in_stock": true } },
      { "range": { "price": { "lte": 100 } } }
    ]
  }
}

// ✅ Fast: filter (skips scoring)
{
  "bool": {
    "filter": [
      { "term": { "in_stock": true } },
      { "range": { "price": { "lte": 100 } } }
    ]
  }
}
```

---

### 3. Limit Result Size

```json
{
  "size": 10,  // Return only 10 results
  "from": 0    // Pagination offset
}
```

---

### 4. Use Bulk API for Indexing

```typescript
// ❌ Slow: 1000 individual index requests
for (const product of products) {
  await client.index({ index: 'products', body: product });
}

// ✅ Fast: Single bulk request
await client.bulk({
  body: products.flatMap(p => [{ index: { _index: 'products' } }, p]),
});
```

---

## Common Pitfalls

### ❌ Using Text Fields for Exact Matches

```json
// ❌ Won't work: "Electronics" is analyzed to "electronics"
{ "term": { "category": "Electronics" } }

// ✅ Use keyword field
{ "term": { "category.keyword": "Electronics" } }
```

---

### ❌ Not Refreshing After Bulk Insert

```typescript
// ❌ Documents not immediately searchable
await client.bulk({ body });

// ✅ Force refresh
await client.bulk({ body, refresh: true });
```

---

### ❌ Deep Pagination (from > 10,000)

```json
// ❌ Slow and memory-intensive
{ "from": 10000, "size": 10 }

// ✅ Use search_after or scroll API
```

---

## Summary

| Feature | Description |
|---------|-------------|
| **Inverted Index** | Fast full-text search across billions of documents |
| **Documents** | JSON objects (equivalent to rows) |
| **Indices** | Collections of documents (equivalent to tables) |
| **Mappings** | Schema definitions (field types, analyzers) |
| **Analyzers** | Process text (tokenize, lowercase, stem) |
| **Query DSL** | JSON-based query language (match, term, bool, range) |
| **Aggregations** | Analytics (counts, averages, histograms) |
| **@elastic/elasticsearch** | Official TypeScript client |

---

## Next Steps

1. ✅ Set up Elasticsearch with Docker
2. ✅ Create index with mappings
3. ✅ Index documents (bulk API)
4. ✅ Practice query DSL (match, term, bool)
5. ✅ Build product search with TypeScript
6. ✅ Implement autocomplete
7. 📚 Read [Elasticsearch Documentation](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)

---

**Happy searching! 🚀**
