import type { ProgramContent } from '@/lib/api/generated';

// Week 14 - Vector Databases & Event Streaming

export const week14VectorStreamingContent: ProgramContent = {
  id: 'databases-week14',
  programId: 'databases-program-1',
  slug: 'week14-vector-streaming',
  parentId: undefined,
  title: 'Week 14 - Vector Databases & Event Streaming',
  description: 'pgvector (vector similarity search, RAG), Apache Kafka (event streaming, topics, partitions, producers, consumers)',
  type: 0, // Page
  body: '# Week 14 - Vector Databases & Event Streaming\n\npgvector and Apache Kafka fundamentals.',
  sortOrder: 14,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 360,
  visibility: 1, // Published
  program: undefined as any,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2026-04-13T00:00:00Z',
  updatedAt: '2026-04-13T00:00:00Z',
};

export const week14PgvectorContent: ProgramContent = {
  id: 'databases-week14-pgvector',
  programId: 'databases-program-1',
  slug: 'pgvector-fundamentals',
  parentId: 'databases-week14',
  title: 'pgvector Fundamentals',
  description: 'Vector similarity search with PostgreSQL: embeddings, similarity metrics, indexing, RAG architecture',
  type: 0, // Page
  body: '',
  sortOrder: 1,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 120,
  visibility: 1, // Published
  program: undefined as any,
  parent: week14VectorStreamingContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-04-13T00:00:00Z',
  updatedAt: '2026-04-13T00:00:00Z',
};

export const week14KafkaContent: ProgramContent = {
  id: 'databases-week14-kafka',
  programId: 'databases-program-1',
  slug: 'kafka-fundamentals',
  parentId: 'databases-week14',
  title: 'Apache Kafka Fundamentals',
  description: 'Event streaming platform: topics, partitions, producers, consumers, consumer groups, real-time pipelines',
  type: 0, // Page
  body: '',
  sortOrder: 2,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 120,
  visibility: 1, // Published
  program: undefined as any,
  parent: week14VectorStreamingContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-04-13T00:00:00Z',
  updatedAt: '2026-04-13T00:00:00Z',
};

export const week14QuizContent: ProgramContent = {
  id: 'databases-week14-quiz',
  programId: 'databases-program-1',
  slug: 'quiz12',
  parentId: 'databases-week14',
  title: 'Quiz 12 - Vector Databases & Event Streaming',
  description: '12 questions covering pgvector (embeddings, similarity search, indexing, RAG) and Kafka (topics, partitions, producers, consumers, consumer groups)',
  type: 2, // Quiz
  body: '',
  sortOrder: 3,
  isRequired: true,
  gradingMethod: 1, // Points
  maxPoints: 100,
  estimatedMinutes: 30,
  visibility: 1, // Published
  program: undefined as any,
  parent: week14VectorStreamingContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-04-13T00:00:00Z',
  updatedAt: '2026-04-13T00:00:00Z',
};

export const week14ReadingsContent: ProgramContent = {
  id: 'databases-week14-readings',
  programId: 'databases-program-1',
  slug: 'readings-14',
  parentId: 'databases-week14',
  title: 'Week 14 Readings',
  description: 'Curated resources for pgvector and Kafka: official docs, tutorials, RAG guides, event streaming patterns, tools, videos, books',
  type: 0, // Page
  body: '',
  sortOrder: 4,
  isRequired: false,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 180,
  visibility: 1, // Published
  program: undefined as any,
  parent: week14VectorStreamingContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-04-13T00:00:00Z',
  updatedAt: '2026-04-13T00:00:00Z',
};

// Set up parent-child relationships
week14VectorStreamingContent.children = [
  week14PgvectorContent,
  week14KafkaContent,
  week14QuizContent,
  week14ReadingsContent,
];

export default week14VectorStreamingContent;
