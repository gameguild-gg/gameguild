import { ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import mongodbFundamentals from './mongodb-fundamentals.md';
import schemaDesignPatterns from './schema-design-patterns.md';
import mongodbCrud from './mongodb-crud.md';
import aggregationPipeline from './aggregation-pipeline.md';
import drizzleMongodb from './drizzle-mongodb.md';
import readings10 from './readings-10.md';
import mongodbQuiz from './quiz/mongodb-quiz.md';

// Week 10: Document Databases - MongoDB (Parent content)
export const week10MongoDBContent: ProgramContent = {
    id: 'databases-week-10-mongodb',
    programId: 'databases-program-1',
    slug: 'document-databases-mongodb',
    parentId: undefined,
    title: 'Week 10 — Document Databases: MongoDB',
    description: 'Learn MongoDB fundamentals, schema design, CRUD operations, and aggregation pipelines',
    type: 0, // Page
    body: mongodbFundamentals,
    sortOrder: 11, // After Week 09
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: undefined as any,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-16T00:00:00Z',
    updatedAt: '2026-03-16T00:00:00Z',
};

// Week 10 child contents

export const week10FundamentalsContent: ProgramContent = {
    id: 'databases-week-10-fundamentals',
    programId: 'databases-program-1',
    slug: 'mongodb-fundamentals',
    parentId: 'databases-week-10-mongodb',
    title: 'MongoDB Fundamentals',
    description: 'Introduction to document databases, JSON/BSON, ObjectId, and when to use MongoDB',
    type: 0, // Page
    body: mongodbFundamentals,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-16T00:00:00Z',
    updatedAt: '2026-03-16T00:00:00Z',
};

export const week10SchemaDesignContent: ProgramContent = {
    id: 'databases-week-10-schema-design',
    programId: 'databases-program-1',
    slug: 'schema-design-patterns',
    parentId: 'databases-week-10-mongodb',
    title: 'MongoDB Schema Design Patterns',
    description: 'Embedding vs referencing, one-to-many relationships, attribute pattern, and bucket pattern',
    type: 0, // Page
    body: schemaDesignPatterns,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-16T00:00:00Z',
    updatedAt: '2026-03-16T00:00:00Z',
};

export const week10CrudContent: ProgramContent = {
    id: 'databases-week-10-crud',
    programId: 'databases-program-1',
    slug: 'mongodb-crud-operations',
    parentId: 'databases-week-10-mongodb',
    title: 'MongoDB CRUD Operations',
    description: 'insertOne/Many, find with query operators, update operators ($set, $push, $pull), and delete operations',
    type: 0, // Page
    body: mongodbCrud,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-19T00:00:00Z',
    updatedAt: '2026-03-19T00:00:00Z',
};

export const week10AggregationContent: ProgramContent = {
    id: 'databases-week-10-aggregation',
    programId: 'databases-program-1',
    slug: 'aggregation-pipeline',
    parentId: 'databases-week-10-mongodb',
    title: 'MongoDB Aggregation Pipeline',
    description: 'Master $match, $group, $project, $lookup, $unwind, and complex aggregation queries',
    type: 0, // Page
    body: aggregationPipeline,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-19T00:00:00Z',
    updatedAt: '2026-03-19T00:00:00Z',
};

export const week10DrizzleContent: ProgramContent = {
    id: 'databases-week-10-drizzle',
    programId: 'databases-program-1',
    slug: 'drizzle-mongodb',
    parentId: 'databases-week-10-mongodb',
    title: 'Drizzle ORM with MongoDB',
    description: 'Type-safe MongoDB queries using Drizzle ORM, schema definitions, and CRUD operations',
    type: 0, // Page
    body: drizzleMongodb,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-19T00:00:00Z',
    updatedAt: '2026-03-19T00:00:00Z',
};

export const week10ReadingsContent: ProgramContent = {
    id: 'databases-week-10-readings',
    programId: 'databases-program-1',
    slug: 'readings-10',
    parentId: 'databases-week-10-mongodb',
    title: 'MongoDB Readings & Resources',
    description: 'Curated resources, documentation, tutorials, and articles on MongoDB',
    type: 0, // Page
    body: readings10,
    sortOrder: 6,
    isRequired: false,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-16T00:00:00Z',
    updatedAt: '2026-03-16T00:00:00Z',
};

export const week10QuizContent: ProgramContent = {
    id: 'databases-week-10-quiz',
    programId: 'databases-program-1',
    slug: 'quiz-08-mongodb',
    parentId: 'databases-week-10-mongodb',
    title: 'Quiz 8: Document Databases',
    description: 'Test your knowledge of MongoDB fundamentals, schema design, CRUD operations, and aggregation',
    type: 2, // Quiz
    body: mongodbQuiz,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 1, // Points
    maxPoints: 100,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: undefined as any,
    parent: week10MongoDBContent,
    children: [],
    contentInteractions: [],
    createdAt: '2026-03-19T00:00:00Z',
    updatedAt: '2026-03-19T00:00:00Z',
};

// Set up parent-child relationships
week10MongoDBContent.children = [
    week10FundamentalsContent,
    week10SchemaDesignContent,
    week10CrudContent,
    week10AggregationContent,
    week10DrizzleContent,
    week10ReadingsContent,
    week10QuizContent,
];

export default week10MongoDBContent;
