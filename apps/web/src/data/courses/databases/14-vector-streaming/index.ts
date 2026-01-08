import { ProgramContent } from '../types';

// Week 14 — Vector Databases & Event Streaming
export const week14VectorStreamingContent: ProgramContent = {
  id: 'databases-week14',
  title: 'Week 14 — Vector Databases & Event Streaming',
  description: 'pgvector (vector similarity search, RAG), Apache Kafka (event streaming, topics, partitions, producers, consumers)',
  type: 'week',
  topics: [
    'pgvector',
    'Vector databases',
    'Embeddings',
    'Similarity search',
    'Cosine similarity',
    'Euclidean distance',
    'IVFFlat indexing',
    'HNSW indexing',
    'RAG (Retrieval-Augmented Generation)',
    'Semantic search',
    'Apache Kafka',
    'Event streaming',
    'Topics and partitions',
    'Producers and consumers',
    'Consumer groups',
    'Offsets',
    'kafkajs client',
    'Real-time analytics',
    'Event-driven microservices'
  ],
  objectives: [
    'Understand vector embeddings and their applications in AI/ML',
    'Store and query vector embeddings with pgvector',
    'Perform similarity search using cosine, euclidean, and inner product metrics',
    'Create vector indices (IVFFlat, HNSW) for performance optimization',
    'Build RAG (Retrieval-Augmented Generation) applications',
    'Understand event streaming architecture and use cases',
    'Create Kafka topics, producers, and consumers',
    'Use consumer groups for parallel processing and load balancing',
    'Implement real-time data pipelines with Kafka',
    'Integrate pgvector and Kafka with TypeScript'
  ],
  estimatedMinutes: 360,
  metadata: {
    week: 14,
    date: '2026-04-13',
    assessments: ['Quiz 12', 'Final Project Feature Freeze'],
    technologies: ['pgvector', 'PostgreSQL', 'Apache Kafka', 'kafkajs', 'OpenAI API', 'Drizzle ORM']
  }
};

export const week14PgvectorContent: ProgramContent = {
  id: 'databases-week14-pgvector',
  title: 'pgvector Fundamentals',
  description: 'Vector similarity search with PostgreSQL: embeddings, similarity metrics, indexing, RAG architecture',
  type: 'lesson',
  topics: [
    'What are vector embeddings?',
    'pgvector PostgreSQL extension',
    'vector data type',
    'Similarity metrics: cosine (<=>), L2 (<->), inner product (<#>)',
    'OpenAI embeddings API',
    'Sentence Transformers',
    'IVFFlat index (< 1M vectors)',
    'HNSW index (1M-10M vectors)',
    'Index tuning (lists, m, ef_construction)',
    'RAG architecture (Retrieve, Augment, Generate)',
    'Semantic search implementation',
    'Recommendation systems',
    'Image similarity search',
    'Drizzle ORM integration',
    'Performance best practices',
    'Common pitfalls (dimension mismatch, indexing order)'
  ],
  objectives: [
    'Understand how embeddings represent unstructured data as vectors',
    'Store embeddings in PostgreSQL using pgvector extension',
    'Query vectors using cosine, L2, and inner product similarity',
    'Create IVFFlat indices for approximate nearest neighbor search',
    'Create HNSW indices for high-accuracy vector search',
    'Build RAG applications that retrieve context for LLMs',
    'Integrate pgvector with TypeScript and Drizzle ORM',
    'Optimize vector search performance with proper indexing'
  ],
  estimatedMinutes: 120,
  parent: 'databases-week14',
  metadata: {
    contentType: 'fundamentals',
    file: 'pgvector-fundamentals.md'
  }
};

export const week14KafkaContent: ProgramContent = {
  id: 'databases-week14-kafka',
  title: 'Apache Kafka Fundamentals',
  description: 'Event streaming platform: topics, partitions, producers, consumers, consumer groups, real-time pipelines',
  type: 'lesson',
  topics: [
    'What is event streaming?',
    'Kafka vs traditional databases',
    'Topics (append-only logs)',
    'Partitions (ordered sequences)',
    'Producers (publish events)',
    'Consumers (subscribe to topics)',
    'Consumer groups (parallel processing)',
    'Offsets (event position tracking)',
    'Brokers (Kafka servers)',
    'Replication (fault tolerance)',
    'Commit strategies (auto vs manual)',
    'kafkajs client for TypeScript',
    'Real-time analytics use case',
    'Event-driven microservices',
    'Change Data Capture (CDC)',
    'Log aggregation',
    'Performance best practices',
    'Common pitfalls (rebalancing, auto-commit)'
  ],
  objectives: [
    'Understand event streaming architecture and use cases',
    'Create Kafka topics with multiple partitions',
    'Publish events with producers (single and batch)',
    'Consume events with consumers and consumer groups',
    'Use message keys for partition assignment and ordering',
    'Implement manual offset commits for fault tolerance',
    'Build real-time analytics pipelines',
    'Design event-driven microservices with Kafka',
    'Integrate Kafka with TypeScript using kafkajs client'
  ],
  estimatedMinutes: 120,
  parent: 'databases-week14',
  metadata: {
    contentType: 'fundamentals',
    file: 'kafka-fundamentals.md'
  }
};

export const week14QuizContent: ProgramContent = {
  id: 'databases-week14-quiz',
  title: 'Quiz 12 — Vector Databases & Event Streaming',
  description: '12 questions covering pgvector (embeddings, similarity search, indexing, RAG) and Kafka (topics, partitions, producers, consumers, consumer groups)',
  type: 'assessment',
  topics: [
    'pgvector purpose and use cases',
    'Similarity metrics (cosine, euclidean, inner product)',
    'Vector indexing (IVFFlat, HNSW)',
    'RAG architecture',
    'IVFFlat lists parameter',
    'Kafka topics definition',
    'Partition purpose and benefits',
    'Message key partitioning strategy',
    'Consumer groups',
    'Offsets and resumable consumption',
    'Consumer rebalancing',
    'Manual offset commits'
  ],
  objectives: [
    'Demonstrate understanding of pgvector and vector similarity search',
    'Explain when to use different similarity metrics',
    'Understand vector indexing strategies',
    'Describe RAG architecture components',
    'Explain Kafka core concepts (topics, partitions, offsets)',
    'Understand consumer groups and fault tolerance',
    'Apply best practices for Kafka event processing'
  ],
  estimatedMinutes: 30,
  parent: 'databases-week14',
  metadata: {
    assessmentType: 'quiz',
    questionCount: 12,
    dueDate: '2026-04-16',
    file: 'quiz/vector-streaming-quiz.md'
  }
};

export const week14ReadingsContent: ProgramContent = {
  id: 'databases-week14-readings',
  title: 'Week 14 Readings',
  description: 'Curated resources for pgvector and Kafka: official docs, tutorials, RAG guides, event streaming patterns, tools, videos, books',
  type: 'reading',
  topics: [
    'pgvector official documentation',
    'OpenAI embeddings API',
    'Sentence Transformers',
    'RAG architecture guides',
    'pgvector performance optimization',
    'Apache Kafka official documentation',
    'kafkajs client guide',
    'Event-driven microservices patterns',
    'Kafka partitioning strategies',
    'Consumer group deep dives',
    'Stream processing tutorials',
    'Real-time analytics examples',
    'Vector database comparisons',
    'Kafka vs message broker comparisons',
    'Practice datasets and examples',
    'Video tutorials',
    'Books (Kafka: The Definitive Guide, Designing Data-Intensive Applications)'
  ],
  objectives: [
    'Access official documentation for pgvector and Kafka',
    'Learn RAG implementation patterns',
    'Explore event streaming use cases',
    'Compare vector databases and event platforms',
    'Practice with real-world examples and datasets'
  ],
  estimatedMinutes: 180,
  parent: 'databases-week14',
  metadata: {
    contentType: 'readings',
    resourceCount: 70,
    file: 'readings-14.md'
  }
};

// Export all content
export const week14Content = [
  week14VectorStreamingContent,
  week14PgvectorContent,
  week14KafkaContent,
  week14QuizContent,
  week14ReadingsContent
];
