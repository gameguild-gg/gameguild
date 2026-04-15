# Week 13 Readings - Time Series & Search Engines

**Topics:** TimescaleDB, Elasticsearch, Inverted Indices, Aggregations

---

## TimescaleDB Resources

### Official Documentation

1. **TimescaleDB Documentation**  
   https://docs.timescale.com/  
   Comprehensive documentation covering installation, hypertables, compression, continuous aggregates, and best practices.

2. **Getting Started with TimescaleDB**  
   https://docs.timescale.com/getting-started/latest/  
   Tutorial covering Docker setup, creating hypertables, basic queries, and time_bucket().

3. **Hypertables and Chunks**  
   https://docs.timescale.com/use-timescale/latest/hypertables/  
   Deep dive into how hypertables automatically partition data into chunks for optimal performance.

4. **Compression**  
   https://docs.timescale.com/use-timescale/latest/compression/  
   Guide to columnar compression: setup, compression policies, and achieving 10x-20x storage reduction.

5. **Continuous Aggregates**  
   https://docs.timescale.com/use-timescale/latest/continuous-aggregates/  
   How to precompute aggregations (hourly/daily averages) for fast queries on large datasets.

6. **Data Retention**  
   https://docs.timescale.com/use-timescale/latest/data-retention/  
   Automatically delete old data with retention policies.

---

### Tutorials and Guides

7. **Time-Series Data with PostgreSQL and TimescaleDB**  
   https://www.timescale.com/blog/how-to-work-with-timeseries-data-in-postgresql/  
   Practical guide comparing plain PostgreSQL vs TimescaleDB for time-series workloads.

8. **Optimizing TimescaleDB Performance**  
   https://www.timescale.com/blog/13-tips-to-improve-postgresql-insert-performance/  
   13 tips for high-performance inserts: batch writes, indexing strategies, chunk size tuning.

9. **IoT Data with TimescaleDB**  
   https://www.timescale.com/blog/how-to-build-an-iot-application-with-timescaledb/  
   Real-world example: storing and querying IoT sensor data at scale.

10. **TimescaleDB vs InfluxDB**  
    https://www.timescale.com/blog/timescaledb-vs-influxdb-for-time-series-data-timescale-influx-sql-nosql-36489299877/  
    Comparison of time-series databases: when to choose TimescaleDB (SQL, ACID) vs InfluxDB (NoSQL, eventual consistency).

---

### Tools

11. **Grafana for TimescaleDB**  
    https://grafana.com/docs/grafana/latest/datasources/postgres/  
    Visualize time-series data with Grafana dashboards connected to TimescaleDB.

12. **TimescaleDB Toolkit**  
    https://github.com/timescale/timescaledb-toolkit  
    PostgreSQL extension with advanced analytics functions: percentile_agg, counter_agg, ASAP smoothing.

---

## Elasticsearch Resources

### Official Documentation

13. **Elasticsearch Reference**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html  
    Complete reference for Elasticsearch: installation, mappings, queries, aggregations, performance tuning.

14. **Getting Started with Elasticsearch**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/getting-started.html  
    Tutorial covering Docker setup, indexing documents, basic queries (match, term, bool).

15. **Mapping Types**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/mapping-types.html  
    Field types: text, keyword, numeric, date, geo_point, object. When to use each type.

16. **Query DSL**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl.html  
    Full reference for query types: match, term, bool, range, fuzzy, prefix, wildcard.

17. **Aggregations**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/search-aggregations.html  
    Analytics: terms, stats, histogram, date_histogram, nested aggregations.

18. **Analyzers and Tokenizers**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/analysis.html  
    How analyzers process text: standard, english, custom. Building custom analyzers.

---

### Tutorials and Guides

19. **Elasticsearch: The Definitive Guide (Book)**  
    https://www.elastic.co/guide/en/elasticsearch/guide/current/index.html  
    Free online book covering search fundamentals, relevance scoring, distributed architecture.

20. **Full-Text Search with Elasticsearch**  
    https://www.elastic.co/blog/found-elasticsearch-from-the-bottom-up  
    Deep dive into inverted indices, term frequency, relevance scoring (TF-IDF, BM25).

21. **Designing the Perfect Elasticsearch Index**  
    https://www.elastic.co/blog/how-many-shards-should-i-have-in-my-elasticsearch-cluster  
    Best practices for index design: sharding, replication, mapping optimization.

22. **Elasticsearch Performance Tuning**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/tune-for-search-speed.html  
    Optimize query performance: filter context, keyword fields, bulk indexing.

23. **Building an Autocomplete with Elasticsearch**  
    https://www.elastic.co/guide/en/elasticsearch/reference/current/search-suggesters.html  
    Implementing autocomplete with completion suggester and edge n-grams.

---

### JavaScript/TypeScript Client

24. **@elastic/elasticsearch Documentation**  
    https://www.elastic.co/guide/en/elasticsearch/client/javascript-api/current/index.html  
    Official Node.js client: installation, connection, CRUD operations, queries, aggregations.

25. **TypeScript Examples**  
    https://github.com/elastic/elasticsearch-js/tree/main/docs/examples  
    Code examples: indexing, searching, bulk operations, aggregations, typed responses.

---

### Tools

26. **Kibana**  
    https://www.elastic.co/kibana  
    Visualization and management UI for Elasticsearch. Explore data, build dashboards, manage indices.

27. **Elastic Stack (ELK)**  
    https://www.elastic.co/elastic-stack  
    Elasticsearch + Logstash + Kibana: complete log analysis and monitoring solution.

28. **Dev Tools Console**  
    http://localhost:5601/app/dev_tools#/console  
    Interactive console in Kibana for running Elasticsearch queries.

---

## Comparison Articles

29. **Time-Series Database Comparison**  
    https://www.timescale.com/blog/time-series-database-2024-comparison/  
    Compare TimescaleDB, InfluxDB, Prometheus, QuestDB, ClickHouse.

30. **Search Engine Comparison**  
    https://www.elastic.co/elasticsearch-vs-solr  
    Elasticsearch vs Apache Solr: features, performance, use cases.

31. **When to Use Elasticsearch vs PostgreSQL**  
    https://www.elastic.co/blog/found-uses-of-elasticsearch  
    Decision matrix: full-text search vs structured queries, denormalization vs normalization.

---

## Videos

32. **TimescaleDB Crash Course**  
    https://www.youtube.com/watch?v=XJg85V4K7Zc  
    1-hour video covering hypertables, compression, continuous aggregates, retention policies.

33. **Elasticsearch Tutorial for Beginners**  
    https://www.youtube.com/watch?v=C3tlMqaNSaI  
    Comprehensive tutorial: setup, indexing, queries, aggregations, Kibana.

34. **Inverted Indices Explained**  
    https://www.youtube.com/watch?v=BNQSdUIEXN0  
    Visual explanation of how inverted indices enable fast full-text search.

35. **ELK Stack Tutorial**  
    https://www.youtube.com/watch?v=gS_nHTWZEJ8  
    Build a log analysis pipeline with Elasticsearch, Logstash, and Kibana.

---

## Books

36. **Elasticsearch in Action**  
    By Radu Gheorghe, Matthew Lee Hinman, Roy Russo  
    Comprehensive guide to Elasticsearch: indexing, searching, aggregations, distributed architecture.

37. **Relevant Search**  
    By Doug Turnbull, John Berryman  
    Advanced relevance tuning: scoring, boosting, query optimization.

38. **Time Series Databases: New Ways to Store and Access Data**  
    By Ted Dunning, Ellen Friedman  
    Overview of time-series database concepts and use cases.

---

## Practice Datasets

39. **NYC Taxi Dataset**  
    https://www1.nyc.gov/site/tlc/about/tlc-trip-record-data.page  
    Real-world time-series data: 1.3 billion taxi trips. Perfect for TimescaleDB practice.

40. **Elasticsearch Sample Data**  
    https://www.elastic.co/guide/en/kibana/current/get-started.html#gs-get-data-into-kibana  
    Kibana includes sample datasets: e-commerce orders, flight data, web logs.

41. **IoT Sensor Data Generator**  
    https://github.com/timescale/timescaledb-docker-ha/tree/master/examples  
    Generate realistic IoT sensor data for testing TimescaleDB features.

---

## Community and Forums

42. **TimescaleDB Slack**  
    https://timescaledb.slack.com/  
    Official Slack community for TimescaleDB users and developers.

43. **Elastic Community Forums**  
    https://discuss.elastic.co/  
    Active forums for Elasticsearch questions, troubleshooting, and best practices.

44. **Stack Overflow**  
    https://stackoverflow.com/questions/tagged/timescaledb  
    https://stackoverflow.com/questions/tagged/elasticsearch  
    Thousands of answered questions for both technologies.

---

## Hands-On Labs

45. **TimescaleDB Interactive Tutorial**  
    https://www.timescale.com/tutorials  
    Step-by-step labs: create hypertables, run time_bucket queries, set up compression.

46. **Elasticsearch Workshop**  
    https://www.elastic.co/training/free  
    Free online workshops: search fundamentals, aggregations, production deployment.

---

## Final Project Resources

47. **TimescaleDB + Drizzle Example**  
    https://github.com/drizzle-team/drizzle-orm/tree/main/examples/timescale  
    Sample project using Drizzle ORM with TimescaleDB hypertables.

48. **Elasticsearch E-commerce Search**  
    https://github.com/elastic/app-search-reference-ui-react  
    Reference implementation: product search with facets, filters, autocomplete.

49. **Monitoring Dashboard with Grafana + TimescaleDB**  
    https://github.com/timescale/examples  
    Build real-time monitoring dashboards for IoT/metrics data.

50. **Log Analysis with ELK Stack**  
    https://github.com/deviantony/docker-elk  
    Docker Compose setup for complete ELK stack (Elasticsearch, Logstash, Kibana).

---

## Key Concepts Checklist

**TimescaleDB:**

- [ ] Create hypertables
- [ ] Use time_bucket() for downsampling
- [ ] Configure compression policies
- [ ] Set up continuous aggregates
- [ ] Implement retention policies
- [ ] Integrate with Drizzle ORM

**Elasticsearch:**

- [ ] Understand inverted indices
- [ ] Define mappings (text vs keyword)
- [ ] Use analyzers (standard, english, custom)
- [ ] Write match and term queries
- [ ] Combine conditions with bool queries
- [ ] Create aggregations (terms, stats, histogram)
- [ ] Integrate with @elastic/elasticsearch client

---

**Happy learning! 📚**
