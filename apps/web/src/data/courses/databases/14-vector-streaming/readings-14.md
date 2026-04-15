# Week 14 Readings - Vector Databases & Event Streaming

**Topics:** pgvector, Embeddings, Similarity Search, RAG, Kafka, Topics, Partitions, Producers, Consumers

---

## pgvector Resources

### Official Documentation

1. **pgvector GitHub Repository**  
   https://github.com/pgvector/pgvector  
   Official pgvector documentation: installation, data types, operators, indexing (IVFFlat, HNSW).

2. **pgvector Installation Guide**  
   https://github.com/pgvector/pgvector#installation  
   Install pgvector from source, package managers, or use Docker image (pgvector/pgvector).

3. **Similarity Search Guide**  
   https://github.com/pgvector/pgvector#similarity-search  
   Examples of cosine, L2, and inner product similarity queries.

4. **Indexing Documentation**  
   https://github.com/pgvector/pgvector#indexing  
   IVFFlat and HNSW index creation, tuning parameters (`lists`, `m`, `ef_construction`).

---

### Embeddings and AI

5. **OpenAI Embeddings API**  
   https://platform.openai.com/docs/guides/embeddings  
   Generate embeddings with `text-embedding-3-small` (1536 dimensions) or `text-embedding-3-large` (3072 dimensions).

6. **Sentence Transformers**  
   https://www.sbert.net/  
   Open-source library for generating text embeddings (384-768 dimensions). Free alternative to OpenAI.

7. **Hugging Face Embeddings**  
   https://huggingface.co/models?pipeline_tag=sentence-similarity  
   Pre-trained embedding models for semantic search.

8. **What are Embeddings?**  
   https://vickiboykis.com/what_are_embeddings/  
   Comprehensive guide to understanding embeddings, their history, and applications.

---

### RAG (Retrieval-Augmented Generation)

9. **Building RAG Applications**  
   https://www.pinecone.io/learn/retrieval-augmented-generation/  
   Deep dive into RAG architecture: retrieval, augmentation, generation.

10. **RAG with pgvector Tutorial**  
    https://neon.tech/blog/rag-with-postgres  
    Build a RAG chatbot using PostgreSQL, pgvector, and OpenAI.

11. **LangChain RAG Guide**  
    https://python.langchain.com/docs/use_cases/question_answering/  
    Framework for building RAG applications with vector stores.

12. **OpenAI RAG Best Practices**  
    https://platform.openai.com/docs/guides/production-best-practices  
    Chunking strategies, embedding optimization, and prompt engineering for RAG.

---

### Tutorials and Guides

13. **pgvector Tutorial: Semantic Search**  
    https://supabase.com/blog/openai-embeddings-postgres-vector  
    Step-by-step tutorial: generate embeddings, store in pgvector, build semantic search.

14. **Vector Database Comparison**  
    https://superlinked.com/vector-db-comparison/  
    Compare pgvector, Pinecone, Weaviate, Qdrant, Milvus, Chroma.

15. **Optimizing pgvector Performance**  
    https://www.timescale.com/blog/how-we-made-postgresql-as-fast-as-pinecone-for-vector-data/  
    Performance tuning: indexing strategies, query optimization, hardware considerations.

16. **IVFFlat vs HNSW Indexing**  
    https://www.pinecone.io/learn/series/faiss/hnsw/  
    Understanding approximate nearest neighbor (ANN) algorithms.

---

### Use Cases

17. **Semantic Search with pgvector**  
    https://github.com/pgvector/pgvector-python/blob/master/examples/openai_embeddings.py  
    Code example: search documents by meaning, not keywords.

18. **Recommendation Systems**  
    https://www.youtube.com/watch?v=QdDoFfkVkcw  
    Build recommendation engine with vector similarity.

19. **Image Similarity Search**  
    https://github.com/pgvector/pgvector/blob/master/README.md#image-similarity  
    Store and query image embeddings (CLIP, ResNet).

---

## Kafka Resources

### Official Documentation

20. **Apache Kafka Documentation**  
    https://kafka.apache.org/documentation/  
    Comprehensive Kafka reference: architecture, producers, consumers, configuration.

21. **Kafka Quickstart**  
    https://kafka.apache.org/quickstart  
    Step-by-step guide: start Kafka, create topics, produce/consume events.

22. **Kafka Concepts**  
    https://kafka.apache.org/documentation/#intro_concepts_and_arch  
    Core concepts: topics, partitions, producers, consumers, brokers, replication.

23. **kafkajs Documentation**  
    https://kafka.js.org/  
    Official Node.js/TypeScript client: installation, configuration, examples.

---

### Tutorials and Guides

24. **Kafka in 100 Seconds**  
    https://www.youtube.com/watch?v=uvb00oaa3k8  
    Quick visual introduction to Kafka concepts.

25. **Building Event-Driven Microservices**  
    https://www.confluent.io/blog/event-driven-microservices-with-kafka/  
    Design patterns for event-driven architectures.

26. **Kafka Partitioning Explained**  
    https://www.conduktor.io/kafka/kafka-topics-partitions-and-offsets/  
    How partitions enable parallelism and ordering guarantees.

27. **Consumer Groups Deep Dive**  
    https://www.confluent.io/blog/tutorial-getting-started-with-the-new-apache-kafka-0-9-consumer-client/  
    Understanding consumer group rebalancing, offsets, and fault tolerance.

28. **Kafka Streams Tutorial**  
    https://kafka.apache.org/documentation/streams/  
    Stream processing: filtering, mapping, aggregating, joining.

---

### kafkajs Examples

29. **kafkajs Getting Started**  
    https://kafka.js.org/docs/getting-started  
    Installation, connection, producing/consuming messages.

30. **kafkajs Producer Examples**  
    https://kafka.js.org/docs/producing  
    Send messages, batching, compression, error handling.

31. **kafkajs Consumer Examples**  
    https://kafka.js.org/docs/consuming  
    Subscribe to topics, process messages, commit offsets, error handling.

32. **kafkajs Consumer Groups**  
    https://kafka.js.org/docs/consumer-groups  
    Configure consumer groups, rebalancing, offset management.

---

### Use Cases

33. **Real-Time Analytics with Kafka**  
    https://www.confluent.io/blog/stream-processing-vs-batch-processing/  
    Build real-time dashboards and metrics pipelines.

34. **Change Data Capture (CDC)**  
    https://debezium.io/documentation/reference/tutorial.html  
    Stream database changes to Kafka with Debezium.

35. **Log Aggregation with Kafka**  
    https://www.elastic.co/blog/kafka-elasticsearch-log-aggregation  
    Centralize application logs with Kafka + Elasticsearch.

36. **Event Sourcing with Kafka**  
    https://www.confluent.io/blog/event-sourcing-cqrs-stream-processing-apache-kafka-whats-connection/  
    Design event-sourced systems with Kafka as event store.

---

### Tools and Ecosystem

37. **Confluent Cloud**  
    https://www.confluent.io/confluent-cloud/  
    Managed Kafka service (free tier available).

38. **Kafka UI (Kafdrop)**  
    https://github.com/obsidiandynamics/kafdrop  
    Web UI for monitoring Kafka topics, consumers, brokers.

39. **Schema Registry**  
    https://docs.confluent.io/platform/current/schema-registry/index.html  
    Manage Avro/JSON/Protobuf schemas for Kafka events.

40. **Kafka Connect**  
    https://kafka.apache.org/documentation/#connect  
    Integrate Kafka with databases, storage systems, and cloud services.

---

## Comparison Articles

41. **pgvector vs Pinecone vs Weaviate**  
    https://www.timescale.com/blog/pgvector-vs-pinecone/  
    When to use PostgreSQL + pgvector vs dedicated vector databases.

42. **Kafka vs RabbitMQ vs Redis Pub/Sub**  
    https://www.confluent.io/blog/kafka-vs-rabbitmq/  
    Compare event streaming platforms and message brokers.

43. **RAG vs Fine-Tuning**  
    https://www.pinecone.io/learn/retrieval-augmented-generation-vs-fine-tuning/  
    When to use RAG (context retrieval) vs fine-tuning LLMs.

44. **Stream Processing: Kafka vs Flink vs Spark**  
    https://www.confluent.io/blog/kafka-streams-vs-flink-vs-spark/  
    Compare stream processing frameworks.

---

## Videos

45. **pgvector Tutorial for Beginners**  
    https://www.youtube.com/watch?v=h0suAcP3yiM  
    Install pgvector, store embeddings, build semantic search.

46. **Building RAG Applications**  
    https://www.youtube.com/watch?v=sVcwVQRHIc8  
    End-to-end RAG tutorial with OpenAI and pgvector.

47. **Kafka Architecture Explained**  
    https://www.youtube.com/watch?v=06iRM1Ghr1k  
    Visual explanation of topics, partitions, brokers, replication.

48. **Kafka Streams in Action**  
    https://www.youtube.com/watch?v=Z3JKCLG3VP4  
    Real-time stream processing with Kafka Streams.

49. **Event-Driven Microservices**  
    https://www.youtube.com/watch?v=STKCRSUsyP0  
    Design patterns for event-driven architectures with Kafka.

---

## Books

50. **Designing Data-Intensive Applications**  
    By Martin Kleppmann  
    Chapter 11: Stream Processing. Deep dive into Kafka, event logs, and stream processing.

51. **Kafka: The Definitive Guide**  
    By Neha Narkhede, Gwen Shapira, Todd Palino  
    Comprehensive guide to Kafka: architecture, configuration, operations, use cases.

52. **Building Machine Learning Powered Applications**  
    By Emmanuel Ameisen  
    Chapters on embeddings, RAG, and production ML systems.

53. **Vector Search for Practitioners**  
    By James Briggs  
    Practical guide to vector databases, embeddings, and semantic search.

---

## Practice Datasets

54. **OpenAI Cookbook Examples**  
    https://github.com/openai/openai-cookbook/tree/main/examples  
    Code examples: embeddings, semantic search, Q&A systems.

55. **Kaggle Datasets for Embeddings**  
    https://www.kaggle.com/datasets  
    Search "embeddings" or "text similarity" for practice datasets.

56. **Confluent Kafka Examples**  
    https://github.com/confluentinc/examples  
    Real-world Kafka examples: microservices, CDC, stream processing.

57. **Awesome Kafka**  
    https://github.com/monksy/awesome-kafka  
    Curated list of Kafka resources, tools, tutorials.

---

## Community and Forums

58. **pgvector Discussions**  
    https://github.com/pgvector/pgvector/discussions  
    Ask questions, share use cases, get help with pgvector.

59. **Kafka Users Mailing List**  
    https://kafka.apache.org/contact  
    Official Kafka community mailing lists.

60. **Stack Overflow: pgvector**  
    https://stackoverflow.com/questions/tagged/pgvector  
    Q&A for pgvector issues and use cases.

61. **Stack Overflow: Kafka**  
    https://stackoverflow.com/questions/tagged/apache-kafka  
    Thousands of answered Kafka questions.

62. **Confluent Community Slack**  
    https://launchpass.com/confluentcommunity  
    Active Slack community for Kafka users.

---

## Hands-On Labs

63. **pgvector Workshop**  
    https://neon.tech/demos/semantic-search  
    Interactive demo: build semantic search with pgvector.

64. **Kafka Tutorials by Confluent**  
    https://kafka-tutorials.confluent.io/  
    Step-by-step tutorials: produce/consume, streams, connectors.

65. **LangChain RAG Tutorials**  
    https://python.langchain.com/docs/tutorials/  
    Build RAG applications with LangChain and vector stores.

---

## Final Project Resources

66. **pgvector + Drizzle Example**  
    https://github.com/drizzle-team/drizzle-orm/tree/main/examples/pgvector  
    Sample project using Drizzle ORM with pgvector.

67. **kafkajs Examples Repository**  
    https://github.com/tulios/kafkajs/tree/master/examples  
    Production-ready examples: producers, consumers, admin operations.

68. **RAG Application Template**  
    https://github.com/openai/openai-cookbook/blob/main/examples/Question_answering_using_embeddings.ipynb  
    Jupyter notebook: RAG with OpenAI embeddings.

69. **Event-Driven Microservices with Kafka**  
    https://github.com/eventuate-tram/eventuate-tram-core  
    Framework for building event-driven microservices.

70. **Real-Time Analytics Dashboard**  
    https://github.com/confluentinc/demo-scene  
    Demo applications: real-time analytics, CDC, stream processing.

---

## Key Concepts Checklist

**pgvector:**

- [ ] Install pgvector extension in PostgreSQL
- [ ] Generate embeddings with OpenAI API
- [ ] Store vectors in PostgreSQL (`vector(1536)`)
- [ ] Query similarity with `<=>` (cosine), `<->` (L2), `<#>` (inner product)
- [ ] Create IVFFlat index (< 1M vectors)
- [ ] Create HNSW index (1M-10M vectors)
- [ ] Build RAG application (retrieve + augment + generate)
- [ ] Integrate with Drizzle ORM

**Kafka:**

- [ ] Understand topics, partitions, offsets
- [ ] Create topics with kafkajs admin client
- [ ] Publish events with producers
- [ ] Consume events with consumer groups
- [ ] Handle consumer rebalancing
- [ ] Commit offsets (auto vs manual)
- [ ] Use message keys for partitioning
- [ ] Build real-time analytics pipeline

---

**Happy learning! 📚**
