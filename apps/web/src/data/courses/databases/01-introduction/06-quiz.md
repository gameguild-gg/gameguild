# Quiz 01 — Introduction to Databases

## Multiple Choice Questions (choices randomized per item)

1. Which database type is best suited for strong ACID guarantees and complex joins?
   - A) Wide-column store
   - B) Relational database
   - C) Key-value store
   - D) Document database

2. What is a key strength of document databases compared to relational databases?
   - A) Strict table schemas only
   - B) Schema-less, nested documents
   - C) Guaranteed sub-millisecond latency
   - D) Primary/foreign key enforcement

3. When is a key-value store like Redis the right choice?
   - A) Deep relationship traversal
   - B) Ultra-fast lookups, caching, sessions
   - C) Full-text relevance ranking
   - D) Complex multi-table joins

4. Which database type treats relationships as first-class citizens for traversal-heavy queries?
   - A) Document database
   - B) Time series database
   - C) Graph database
   - D) Key-value store

5. For time-ordered metrics and IoT sensor data at moderate scale, which database type fits best?
   - A) Time series database
   - B) Wide-column store
   - C) Vector database
   - D) Search engine

6. Which engine is specialized for full-text search and relevance ranking?
   - A) Graph DB
   - B) Search engine (Elasticsearch/OpenSearch/Solr)
   - C) Vector DB
   - D) Key-value cache

7. What is a primary use of vector databases introduced in Week 1?
   - A) Traditional OLTP with ACID
   - B) Log shipping and replication
   - C) Semantic/similarity search over embeddings
   - D) TTL-based caching

8. In Docker Compose, which command from Week 1 starts PostgreSQL and Adminer services in the background?
   - A) `docker-compose run postgres adminer`
   - B) `docker-compose start all`
   - C) `docker-compose up -d postgres adminer`
   - D) `docker up postgres adminer -d`

9. Why do modern systems often combine multiple database types (polyglot persistence)?
   - A) To avoid backups
   - B) No single DB excels at all access patterns; different workloads need different trade-offs
   - C) Licensing requires multiple vendors
   - D) To reduce operational complexity

10. When choosing a database, what is the first question in the decision flow (per Week 1’s decision framework)?

- A) "Is it globally distributed already?"
- B) "Do you need ACID transactions?"
- C) "Do you require full-text search?"
- D) "Is the dataset under 10 GB?"
