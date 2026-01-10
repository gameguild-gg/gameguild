import type { ProgramContent } from '../../../types/content';

// Week 11 — Key-Value & Wide-Column Stores: Redis and Cassandra

export const week11KeyValueContent: ProgramContent = {
  id: 'databases-week11',
  slug: 'week11-keyvalue-widecolumn',
  title: 'Week 11 — Key-Value & Wide-Column Stores',
  description:
    'Explore Redis (in-memory key-value store) and Cassandra (distributed wide-column store). Learn data structures, caching patterns, CAP theorem, CQL, and when to use each database.',
  sortOrder: 110,
  estimatedMinutes: 300,
  visibility: 'published' as const,
  metadata: {
    week: 11,
    dates: '2026/03/23 – 2026/03/27',
    topics: ['Redis', 'Cassandra', 'Key-Value Stores', 'Wide-Column Stores', 'CAP Theorem'],
    assessments: ['Quiz 9: Key-Value & Wide-Column'],
    objectives: [
      'Implement Redis data structures (strings, lists, sets, hashes, sorted sets)',
      'Design caching, rate limiting, and leaderboard systems with Redis',
      'Understand CAP theorem and Cassandra\'s AP model',
      'Model data for Cassandra using partition keys and clustering keys',
      'Write CQL queries for time-series and wide-column data',
      'Choose appropriate databases for different use cases',
    ],
  },
  children: [
    'databases-week11-overview',
    'databases-week11-redis',
    'databases-week11-cassandra',
    'databases-week11-readings',
    'databases-week11-quiz',
  ],
};

export const week11OverviewContent: ProgramContent = {
  id: 'databases-week11-overview',
  slug: 'overview',
  title: 'Week 11 Overview',
  description:
    'Introduction to key-value stores (Redis) and wide-column stores (Cassandra). Weekly schedule, learning objectives, and project milestones.',
  sortOrder: 1,
  estimatedMinutes: 15,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/11-keyvalue-widecolumn/README.md',
    topics: ['Redis', 'Cassandra', 'NoSQL', 'Week Overview'],
  },
};

export const week11RedisContent: ProgramContent = {
  id: 'databases-week11-redis',
  slug: 'redis-fundamentals',
  title: 'Redis Fundamentals',
  description:
    'In-memory key-value store with advanced data structures. Learn strings, lists, sets, hashes, sorted sets, TTL, Pub/Sub, and practical use cases like caching, rate limiting, and leaderboards.',
  sortOrder: 2,
  estimatedMinutes: 90,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/11-keyvalue-widecolumn/redis-fundamentals.md',
    topics: ['Redis', 'Key-Value Store', 'Data Structures', 'TTL', 'Pub/Sub', 'ioredis', 'Caching'],
    objectives: [
      'Understand Redis data structures (strings, lists, sets, hashes, sorted sets)',
      'Implement caching with TTL expiration',
      'Build rate limiters with Redis',
      'Create leaderboards with sorted sets',
      'Use Pub/Sub for real-time messaging',
      'Integrate Redis with TypeScript using ioredis',
    ],
  },
};

export const week11CassandraContent: ProgramContent = {
  id: 'databases-week11-cassandra',
  slug: 'cassandra-fundamentals',
  title: 'Cassandra Fundamentals',
  description:
    'Distributed wide-column NoSQL database for massive scale. Learn CAP theorem, CQL, partition keys, clustering keys, consistency levels, and denormalization patterns for time-series data.',
  sortOrder: 3,
  estimatedMinutes: 120,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/11-keyvalue-widecolumn/cassandra-fundamentals.md',
    topics: [
      'Cassandra',
      'Wide-Column Store',
      'CAP Theorem',
      'CQL',
      'Partition Keys',
      'Clustering Keys',
      'Denormalization',
      'Time-Series Data',
    ],
    objectives: [
      'Understand CAP theorem and Cassandra\'s AP model',
      'Design data models with partition keys and clustering keys',
      'Write CQL queries (CREATE KEYSPACE, CREATE TABLE, INSERT, SELECT)',
      'Apply denormalization patterns (no JOINs)',
      'Configure consistency levels (ONE, QUORUM, ALL)',
      'Integrate Cassandra with TypeScript using cassandra-driver',
    ],
  },
};

export const week11ReadingsContent: ProgramContent = {
  id: 'databases-week11-readings',
  slug: 'readings',
  title: 'Readings & Resources',
  description:
    'Curated collection of Redis and Cassandra documentation, tutorials, tools, videos, books, and community resources for deeper learning.',
  sortOrder: 4,
  estimatedMinutes: 30,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/11-keyvalue-widecolumn/readings-11.md',
    topics: ['Resources', 'Documentation', 'Tutorials', 'Tools'],
  },
};

export const week11QuizContent: ProgramContent = {
  id: 'databases-week11-quiz',
  slug: 'quiz9',
  title: 'Quiz 9 — Key-Value & Wide-Column Stores',
  description:
    'Assessment covering Redis data structures, TTL, Pub/Sub, Cassandra CAP theorem, partition keys, CQL, denormalization, and use case analysis. 12 questions with detailed explanations.',
  sortOrder: 5,
  estimatedMinutes: 45,
  visibility: 'published' as const,
  contentType: 'assessment' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/11-keyvalue-widecolumn/quiz/redis-cassandra-quiz.md',
    topics: [
      'Redis',
      'Cassandra',
      'Data Structures',
      'CAP Theorem',
      'CQL',
      'Consistency Levels',
      'Denormalization',
    ],
    assessmentType: 'quiz',
    totalPoints: 100,
    passingScore: 70,
    gradingMethod: 'automatic',
    objectives: [
      'Select appropriate Redis data structures for requirements',
      'Implement TTL and expiration patterns',
      'Compare Pub/Sub vs message queue patterns',
      'Explain CAP theorem and Cassandra\'s AP model',
      'Design Cassandra schemas with partition/clustering keys',
      'Write CQL queries for time-series data',
      'Apply denormalization patterns',
      'Choose consistency levels for different use cases',
    ],
  },
};

export const allWeek11Content = [
  week11KeyValueContent,
  week11OverviewContent,
  week11RedisContent,
  week11CassandraContent,
  week11ReadingsContent,
  week11QuizContent,
];
