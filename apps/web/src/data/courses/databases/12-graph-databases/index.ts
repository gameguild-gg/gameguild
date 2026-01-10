import type { ProgramContent } from '../../../types/content';

// Week 12 — Graph Databases: Neo4j

export const week12GraphContent: ProgramContent = {
  id: 'databases-week12',
  slug: 'week12-graph-databases',
  title: 'Week 12 — Graph Databases: Neo4j',
  description:
    'Master graph databases with Neo4j. Learn nodes, relationships, Cypher query language, path traversals, and build recommendation engines for social networks and e-commerce.',
  sortOrder: 120,
  estimatedMinutes: 300,
  visibility: 'published' as const,
  metadata: {
    week: 12,
    dates: '2026/03/30 – 2026/04/03',
    topics: ['Graph Databases', 'Neo4j', 'Cypher', 'Recommendations', 'Social Networks'],
    assessments: ['Quiz 10: Graph Databases', 'Final Project Checkpoint #1'],
    objectives: [
      'Explain the graph database model (nodes, relationships, properties)',
      'Compare graph databases to relational databases',
      'Write Cypher queries (CREATE, MATCH, MERGE, DELETE)',
      'Traverse variable-length paths and find shortest paths',
      'Build recommendation engines with collaborative filtering',
      'Integrate Neo4j with TypeScript using neo4j-driver',
    ],
  },
  children: [
    'databases-week12-overview',
    'databases-week12-neo4j',
    'databases-week12-readings',
    'databases-week12-quiz',
  ],
};

export const week12OverviewContent: ProgramContent = {
  id: 'databases-week12-overview',
  slug: 'overview',
  title: 'Week 12 Overview',
  description:
    'Introduction to graph databases and Neo4j. Weekly schedule, learning objectives, and Final Project Checkpoint #1 requirements.',
  sortOrder: 1,
  estimatedMinutes: 15,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/12-graph-databases/README.md',
    topics: ['Graph Databases', 'Neo4j', 'Week Overview'],
  },
};

export const week12Neo4jContent: ProgramContent = {
  id: 'databases-week12-neo4j',
  slug: 'neo4j-fundamentals',
  title: 'Neo4j Fundamentals',
  description:
    'Comprehensive guide to graph databases and Neo4j. Learn Cypher query language, graph traversals, indexes, constraints, and build real-world applications like recommendation engines and fraud detection systems.',
  sortOrder: 2,
  estimatedMinutes: 120,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/12-graph-databases/neo4j-fundamentals.md',
    topics: [
      'Neo4j',
      'Cypher',
      'Graph Model',
      'Nodes',
      'Relationships',
      'Path Queries',
      'Recommendations',
      'neo4j-driver',
    ],
    objectives: [
      'Understand graph database model (nodes, relationships, properties, labels)',
      'Write Cypher CRUD operations (CREATE, MATCH, MERGE, DELETE)',
      'Traverse variable-length paths and find shortest paths',
      'Implement indexes and constraints for performance',
      'Build recommendation engines with collaborative filtering',
      'Integrate Neo4j with TypeScript using neo4j-driver',
      'Design graph schemas for social networks and fraud detection',
    ],
  },
};

export const week12ReadingsContent: ProgramContent = {
  id: 'databases-week12-readings',
  slug: 'readings',
  title: 'Readings & Resources',
  description:
    'Curated collection of Neo4j documentation, tutorials, graph algorithms, tools, videos, books, and community resources for deeper learning.',
  sortOrder: 3,
  estimatedMinutes: 30,
  visibility: 'published' as const,
  contentType: 'reading' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/12-graph-databases/readings-12.md',
    topics: ['Resources', 'Documentation', 'Tutorials', 'Tools', 'Graph Algorithms'],
  },
};

export const week12QuizContent: ProgramContent = {
  id: 'databases-week12-quiz',
  slug: 'quiz10',
  title: 'Quiz 10 — Graph Databases & Neo4j',
  description:
    'Assessment covering graph model fundamentals, Cypher query language, variable-length paths, MERGE vs CREATE, indexes, relationship properties, and use case analysis. 12 questions with detailed explanations.',
  sortOrder: 4,
  estimatedMinutes: 45,
  visibility: 'published' as const,
  contentType: 'assessment' as const,
  metadata: {
    fileType: 'markdown',
    filePath: 'databases/12-graph-databases/quiz/graph-neo4j-quiz.md',
    topics: [
      'Graph Databases',
      'Neo4j',
      'Cypher',
      'Nodes',
      'Relationships',
      'Path Queries',
      'MERGE',
      'Indexes',
    ],
    assessmentType: 'quiz',
    totalPoints: 100,
    passingScore: 70,
    gradingMethod: 'automatic',
    objectives: [
      'Explain the labeled property graph model',
      'Compare graph vs relational databases for different use cases',
      'Interpret Cypher pattern matching (directed, undirected, variable-length)',
      'Use MERGE for upsert operations vs CREATE for inserts',
      'Calculate path lengths and find shortest paths',
      'Apply OPTIONAL MATCH for LEFT JOIN behavior',
      'Access relationship properties in queries',
      'Design indexes for performance optimization',
      'Select appropriate databases for recommendation engines',
    ],
  },
};

export const allWeek12Content = [
  week12GraphContent,
  week12OverviewContent,
  week12Neo4jContent,
  week12ReadingsContent,
  week12QuizContent,
];
