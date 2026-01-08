# Week 11 — Redis & Cassandra Readings & Resources

## Redis (Key-Value Store)

### Official Documentation

- [Redis Documentation](https://redis.io/docs/)
  - Complete Redis command reference and guides

- [Redis Commands](https://redis.io/commands/)
  - Interactive command reference with examples

- [Redis Data Types](https://redis.io/docs/manual/data-types/)
  - In-depth guide to strings, lists, sets, hashes, sorted sets

### Tutorials & Guides

- [Redis University](https://university.redis.com/)
  - Free online courses on Redis fundamentals and advanced topics

- [Try Redis (Interactive Tutorial)](https://try.redis.io/)
  - Learn Redis commands in your browser

- [Redis Best Practices](https://redis.io/docs/manual/patterns/)
  - Design patterns and optimization strategies

### TypeScript/Node.js Integration

- [ioredis Documentation](https://github.com/redis/ioredis)
  - Official Node.js Redis client with TypeScript support

- [ioredis API Reference](https://redis.github.io/ioredis/)
  - Complete API documentation

### Use Case Articles

- [Redis as a Cache](https://redis.io/docs/manual/client-side-caching/)
  - Caching strategies and invalidation patterns

- [Redis for Session Storage](https://redis.io/docs/manual/keyspace/)
  - Session management with TTL

- [Rate Limiting with Redis](https://redis.io/glossary/rate-limiting/)
  - Fixed window, sliding window, token bucket algorithms

- [Leaderboards with Sorted Sets](https://redis.io/docs/data-types/sorted-sets/)
  - Gaming leaderboards and ranking systems

### Advanced Topics

- [Redis Pub/Sub](https://redis.io/docs/manual/pubsub/)
  - Real-time messaging patterns

- [Redis Transactions](https://redis.io/docs/manual/transactions/)
  - MULTI/EXEC and optimistic locking with WATCH

- [Redis Lua Scripting](https://redis.io/docs/manual/programmability/eval-intro/)
  - Server-side scripting for atomic operations

- [Redis Persistence](https://redis.io/docs/manual/persistence/)
  - RDB snapshots vs AOF (append-only file)

---

## Cassandra (Wide-Column Store)

### Official Documentation

- [Apache Cassandra Documentation](https://cassandra.apache.org/doc/latest/)
  - Complete Cassandra reference

- [CQL (Cassandra Query Language)](https://cassandra.apache.org/doc/latest/cassandra/cql/)
  - CQL syntax and commands

- [Data Modeling in Cassandra](https://cassandra.apache.org/doc/latest/cassandra/data_modeling/)
  - Schema design principles

### Tutorials & Guides

- [DataStax Academy](https://academy.datastax.com/)
  - Free courses on Cassandra fundamentals (DS101, DS201, DS220)

- [Cassandra Tutorial](https://www.tutorialspoint.com/cassandra/index.htm)
  - Step-by-step beginner guide

- [Cassandra Data Modeling Best Practices](https://www.datastax.com/blog/basic-rules-cassandra-data-modeling)
  - Design patterns and anti-patterns

### Architecture & Theory

- [CAP Theorem Explained](https://www.ibm.com/topics/cap-theorem)
  - Understanding consistency, availability, partition tolerance

- [Cassandra Architecture](https://cassandra.apache.org/doc/latest/cassandra/architecture/)
  - Rings, tokens, replication, consistency levels

- [Consistency Levels in Cassandra](https://docs.datastax.com/en/cassandra-oss/3.x/cassandra/dml/dmlConfigConsistency.html)
  - ONE, QUORUM, ALL trade-offs

- [How Cassandra Writes Work](https://www.datastax.com/blog/how-cassandra-writes-data)
  - Commit log, memtable, SSTables, compaction

### TypeScript/Node.js Integration

- [cassandra-driver Documentation](https://docs.datastax.com/en/developer/nodejs-driver/latest/)
  - Official Node.js driver

- [cassandra-driver API Reference](https://docs.datastax.com/en/developer/nodejs-driver/latest/api/)
  - Complete API documentation

### Data Modeling

- [Cassandra Data Modeling Workshop](https://www.youtube.com/watch?v=UP74jC1pzkw)
  - Video: Query-first design approach

- [Denormalization in Cassandra](https://www.datastax.com/blog/we-shall-have-order)
  - Why and how to denormalize data

- [Time-Series Data Modeling](https://www.datastax.com/blog/introduction-time-series-data-modeling-apache-cassandra)
  - IoT, logs, metrics patterns

- [Bucketing Patterns](https://www.datastax.com/blog/cassandra-data-modeling-time-series-data)
  - Avoiding large partitions

### Advanced Topics

- [Secondary Indexes](https://cassandra.apache.org/doc/latest/cassandra/cql/indexes.html)
  - When to use (and when NOT to use)

- [Materialized Views](https://cassandra.apache.org/doc/latest/cassandra/cql/mvs.html)
  - Automated denormalization

- [Lightweight Transactions (LWT)](https://www.datastax.com/blog/lightweight-transactions-cassandra)
  - Compare-and-set with IF conditions

- [Cassandra Performance Tuning](https://www.datastax.com/blog/guide-cassandra-performance-tuning)
  - Compaction, tombstones, monitoring

---

## Redis vs Cassandra Comparison

### Decision Guides

- [Redis vs Cassandra: When to Use Which](https://www.integrate.io/blog/redis-vs-cassandra/)
  - Use case comparison

- [Key-Value vs Wide-Column Stores](https://www.mongodb.com/databases/types/key-value-database)
  - Data model differences

### Performance Benchmarks

- [Redis Benchmark Tool](https://redis.io/docs/management/optimization/benchmarks/)
  - Testing Redis performance

- [Cassandra Performance Benchmark](https://www.datastax.com/blog/benchmarking-cassandra-scalability-aws-over-1-million-writes-second)
  - Scalability tests

---

## CAP Theorem & Distributed Systems

### Theory

- [CAP Theorem Illustrated](https://mwhittaker.github.io/blog/an_illustrated_proof_of_the_cap_theorem/)
  - Visual proof and explanation

- [Eventual Consistency Explained](https://www.allthingsdistributed.com/2008/12/eventually_consistent.html)
  - Werner Vogels (Amazon CTO) on consistency models

- [Designing Data-Intensive Applications](https://dataintensive.net/)
  - Book: Chapter on Replication and Consistency

### Distributed Databases

- [Dynamo: Amazon's Highly Available Key-Value Store](https://www.allthingsdistributed.com/files/amazon-dynamo-sosp2007.pdf)
  - Research paper (Cassandra is inspired by Dynamo)

- [Bigtable: A Distributed Storage System](https://research.google/pubs/pub27898/)
  - Google's paper on wide-column storage

---

## Practical Tools

### Redis Tools

- [RedisInsight](https://redis.com/redis-enterprise/redis-insight/)
  - Official GUI for Redis (visualize data, monitor performance)

- [redis-cli](https://redis.io/docs/manual/cli/)
  - Command-line interface

- [Redis Desktop Manager](https://github.com/uglide/RedisDesktopManager)
  - Cross-platform GUI (open-source)

### Cassandra Tools

- [cqlsh](https://cassandra.apache.org/doc/latest/cassandra/tools/cqlsh.html)
  - CQL shell (command-line)

- [DataStax DevCenter](https://www.datastax.com/products/datastax-devcenter)
  - IDE for CQL development

- [nodetool](https://cassandra.apache.org/doc/latest/cassandra/tools/nodetool/nodetool.html)
  - Cluster management CLI

### Docker

- [Redis Docker Hub](https://hub.docker.com/_/redis)
  - Official Redis images

- [Cassandra Docker Hub](https://hub.docker.com/_/cassandra)
  - Official Cassandra images

- [Docker Compose Examples](https://github.com/bitnami/containers/tree/main/bitnami/cassandra)
  - Multi-node Cassandra clusters

---

## Video Tutorials

### Redis

- [Redis Crash Course](https://www.youtube.com/watch?v=jgpVdJB2sKQ) (Web Dev Simplified)
  - 30-minute introduction

- [Redis Full Course](https://www.youtube.com/watch?v=XCsS_NVAa1g) (Academind)
  - In-depth Redis tutorial

### Cassandra

- [Cassandra Tutorial for Beginners](https://www.youtube.com/watch?v=s1xc1HVsRk0) (edureka!)
  - 2-hour comprehensive course

- [Apache Cassandra Crash Course](https://www.youtube.com/watch?v=J-cSy5MeMOA) (Hussein Nasser)
  - Architecture and CQL basics

### Distributed Systems

- [Distributed Systems in One Lesson](https://www.youtube.com/watch?v=Y6Ev8GIlbxc) (Tim Berglund)
  - CAP theorem, consistency models

---

## Books

### Redis

- **Redis Essentials** by Maxwell Dayvson Da Silva, Hugo Lopes Tavares
  - Practical guide to Redis data structures and patterns

- **Redis in Action** by Josiah L. Carlson
  - Real-world use cases and architectures

### Cassandra

- **Cassandra: The Definitive Guide** by Jeff Carpenter, Eben Hewitt
  - Comprehensive reference (3rd edition covers Cassandra 4.x)

- **Learning Apache Cassandra** by Mat Brown
  - Beginner-friendly introduction

### Distributed Systems

- **Designing Data-Intensive Applications** by Martin Kleppmann
  - Essential reading on database internals and distributed systems

---

## Practice Datasets

### Redis

- [Redis Sample Datasets](https://github.com/redis-developer/redis-datasets)
  - Movies, users, shopping carts

- [Redis Labs Demo](https://github.com/redis-developer/redis-microservices-demo)
  - Microservices with Redis

### Cassandra

- [Cassandra Sample Data (killrvideo)](https://github.com/KillrVideo/killrvideo-data)
  - Video streaming app dataset

- [Time-Series Sample Data](https://github.com/datastax/python-driver/tree/master/examples)
  - IoT sensor data examples

---

## Community & Forums

### Redis

- [Redis Discord](https://discord.gg/redis)
  - Official community chat

- [Stack Overflow - Redis](https://stackoverflow.com/questions/tagged/redis)
  - Q&A forum

- [Reddit - r/redis](https://www.reddit.com/r/redis/)
  - Community discussions

### Cassandra

- [Apache Cassandra Slack](https://cassandra.apache.org/community/)
  - Official Slack workspace

- [Stack Overflow - Cassandra](https://stackoverflow.com/questions/tagged/cassandra)
  - Q&A forum

- [Reddit - r/cassandra](https://www.reddit.com/r/cassandra/)
  - Community discussions

---

## Cheat Sheets

- [Redis Command Cheat Sheet](https://redis.io/docs/manual/patterns/twitter-clone/)
  - Quick reference

- [Redis Data Types Cheat Sheet](https://cheatography.com/tasjaevan/cheat-sheets/redis/)
  - Printable reference

- [Cassandra CQL Cheat Sheet](https://www.datastax.com/blog/cassandra-query-language-cql-tutorial)
  - Common commands

- [Cassandra Data Modeling Cheat Sheet](https://www.datastax.com/blog/basic-rules-cassandra-data-modeling)
  - Design patterns

---

**Related Content:**
- [Redis Fundamentals](./redis-fundamentals.md)
- [Cassandra Fundamentals](./cassandra-fundamentals.md)
- [Quiz 9: Redis & Cassandra](./quiz/redis-cassandra-quiz.md)
