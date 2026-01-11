import type { ProgramContent } from '@/lib/api/generated';

// Week 12 - Graph Databases: Neo4j

export const week12GraphContent: ProgramContent = {
  id: 'databases-week12',
  programId: 'databases-program-1',
  slug: 'week12-graph-databases',
  parentId: undefined,
  title: 'Week 12 - Graph Databases: Neo4j',
  description:
    'Master graph databases with Neo4j. Learn nodes, relationships, Cypher query language, path traversals, and build recommendation engines for social networks and e-commerce.',
  type: 0, // Page
  body: '# Week 12 - Graph Databases: Neo4j\n\nNeo4j fundamentals and graph database concepts.',
  sortOrder: 12,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 300,
  visibility: 1, // Published
  program: undefined as any,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-30T00:00:00Z',
  updatedAt: '2026-03-30T00:00:00Z',
};

export const week12OverviewContent: ProgramContent = {
  id: 'databases-week12-overview',
  programId: 'databases-program-1',
  slug: 'overview',
  parentId: 'databases-week12',
  title: 'Week 12 Overview',
  description:
    'Introduction to graph databases and Neo4j. Weekly schedule, learning objectives, and Final Project Checkpoint #1 requirements.',
  type: 0, // Page
  body: '',
  sortOrder: 1,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 15,
  visibility: 1, // Published
  program: undefined as any,
  parent: week12GraphContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-30T00:00:00Z',
  updatedAt: '2026-03-30T00:00:00Z',
};

export const week12Neo4jContent: ProgramContent = {
  id: 'databases-week12-neo4j',
  programId: 'databases-program-1',
  slug: 'neo4j-fundamentals',
  parentId: 'databases-week12',
  title: 'Neo4j Fundamentals',
  description:
    'Comprehensive guide to graph databases and Neo4j. Learn Cypher query language, graph traversals, indexes, constraints, and build real-world applications like recommendation engines and fraud detection systems.',
  type: 0, // Page
  body: '',
  sortOrder: 2,
  isRequired: true,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 120,
  visibility: 1, // Published
  program: undefined as any,
  parent: week12GraphContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-30T00:00:00Z',
  updatedAt: '2026-03-30T00:00:00Z',
};

export const week12ReadingsContent: ProgramContent = {
  id: 'databases-week12-readings',
  programId: 'databases-program-1',
  slug: 'readings',
  parentId: 'databases-week12',
  title: 'Readings & Resources',
  description:
    'Curated collection of Neo4j documentation, tutorials, graph algorithms, tools, videos, books, and community resources for deeper learning.',
  type: 0, // Page
  body: '',
  sortOrder: 3,
  isRequired: false,
  gradingMethod: 0, // None
  maxPoints: null,
  estimatedMinutes: 30,
  visibility: 1, // Published
  program: undefined as any,
  parent: week12GraphContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-30T00:00:00Z',
  updatedAt: '2026-03-30T00:00:00Z',
};

export const week12QuizContent: ProgramContent = {
  id: 'databases-week12-quiz',
  programId: 'databases-program-1',
  slug: 'quiz10',
  parentId: 'databases-week12',
  title: 'Quiz 10 - Graph Databases & Neo4j',
  description:
    'Assessment covering graph model fundamentals, Cypher query language, variable-length paths, MERGE vs CREATE, indexes, relationship properties, and use case analysis. 12 questions with detailed explanations.',
  type: 2, // Quiz
  body: '',
  sortOrder: 4,
  isRequired: true,
  gradingMethod: 1, // Points
  maxPoints: 100,
  estimatedMinutes: 45,
  visibility: 1, // Published
  program: undefined as any,
  parent: week12GraphContent,
  children: [],
  contentInteractions: [],
  createdAt: '2026-03-30T00:00:00Z',
  updatedAt: '2026-03-30T00:00:00Z',
};

// Set up parent-child relationships
week12GraphContent.children = [
  week12OverviewContent,
  week12Neo4jContent,
  week12ReadingsContent,
  week12QuizContent,
];

export default week12GraphContent;
