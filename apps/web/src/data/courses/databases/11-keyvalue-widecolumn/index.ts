import type { ProgramContent } from '@/lib/api/generated';

// Week 11 - Key-Value & Wide-Column Stores: Redis and Cassandra

export const week11KeyValueContent: ProgramContent = {
  id: 'databases-week11',
  programId: 'databases-program-1',
  slug: 'week11-keyvalue-widecolumn',
  parentId: undefined,
  title: 'Week 11 - Key-Value & Wide-Column Stores',
  description:
    'Explore Redis (in-memory key-value store) and Cassandra (distributed wide-column store). Learn data structures, caching patterns, CAP theorem, CQL, and when to use each database.',
  type: 0, // Page
  body: '# Week 11 - Key-Value & Wide-Column Stores\n\nRedis and Cassandra fundamentals.',
  sortOrder: 11,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 300,
  visibility: 1, // Published
  program: undefined as any,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-23T00:00:00Z',
  updatedAt: '2026-03-23T00:00:00Z',
};

export const week11OverviewContent: ProgramContent = {
  id: 'databases-week11-overview',
  programId: 'databases-program-1',
  slug: 'overview',
  parentId: 'databases-week11',
  title: 'Week 11 Overview',
  description:
    'Introduction to key-value stores (Redis) and wide-column stores (Cassandra). Weekly schedule, learning objectives, and project milestones.',
  type: 0, // Page
  body: '',
  sortOrder: 1,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 15,
  visibility: 1, // Published
  program: undefined as any,
  parent: week11KeyValueContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-23T00:00:00Z',
  updatedAt: '2026-03-23T00:00:00Z',
};

export const week11RedisContent: ProgramContent = {
  id: 'databases-week11-redis',
  programId: 'databases-program-1',
  slug: 'redis-fundamentals',
  parentId: 'databases-week11',
  title: 'Redis Fundamentals',
  description:
    'In-memory key-value store with advanced data structures. Learn strings, lists, sets, hashes, sorted sets, TTL, Pub/Sub, and practical use cases like caching, rate limiting, and leaderboards.',
  type: 0, // Page
  body: '',
  sortOrder: 2,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 90,
  visibility: 1, // Published
  program: undefined as any,
  parent: week11KeyValueContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-23T00:00:00Z',
  updatedAt: '2026-03-23T00:00:00Z',
};

export const week11CassandraContent: ProgramContent = {
  id: 'databases-week11-cassandra',
  programId: 'databases-program-1',
  slug: 'cassandra-fundamentals',
  parentId: 'databases-week11',
  title: 'Cassandra Fundamentals',
  description:
    'Distributed wide-column NoSQL database for massive scale. Learn CAP theorem, CQL, partition keys, clustering keys, consistency levels, and denormalization patterns for time-series data.',
  type: 0, // Page
  body: '',
  sortOrder: 3,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 120,
  visibility: 1, // Published
  program: undefined as any,
  parent: week11KeyValueContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-23T00:00:00Z',
  updatedAt: '2026-03-23T00:00:00Z',
};

export const week11ReadingsContent: ProgramContent = {
  id: 'databases-week11-readings',
  programId: 'databases-program-1',
  slug: 'readings',
  parentId: 'databases-week11',
  title: 'Readings & Resources',
  description:
    'Curated collection of Redis and Cassandra documentation, tutorials, tools, videos, books, and community resources for deeper learning.',
  type: 0, // Page
  body: '',
  sortOrder: 4,
  isRequired: false,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 30,
  visibility: 1, // Published
  program: undefined as any,
  parent: week11KeyValueContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-23T00:00:00Z',
  updatedAt: '2026-03-23T00:00:00Z',
};

export const week11QuizContent: ProgramContent = {
  id: 'databases-week11-quiz',
  programId: 'databases-program-1',
  slug: 'quiz9',
  parentId: 'databases-week11',
  title: 'Quiz 9 - Key-Value & Wide-Column Stores',
  description:
    'Assessment covering Redis data structures, TTL, Pub/Sub, Cassandra CAP theorem, partition keys, CQL, denormalization, and use case analysis. 12 questions with detailed explanations.',
  type: 2, // Quiz
  body: '',
  sortOrder: 5,
  isRequired: true,
  gradingMethod: 1, // Points
  maxPoints: 100,
  estimatedMinutes: 45,
  visibility: 1, // Published
  program: undefined as any,
  parent: week11KeyValueContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-23T00:00:00Z',
  updatedAt: '2026-03-23T00:00:00Z',
};

// Set up parent-child relationships
week11KeyValueContent.children = [
  week11OverviewContent,
  week11RedisContent,
  week11CassandraContent,
  week11ReadingsContent,
  week11QuizContent,
];

export default week11KeyValueContent;
