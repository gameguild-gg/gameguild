import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import week01Intro from './01-introduction/01-intro.md';
import week01Readings from './01-introduction/02-readings.md';
import week01DbZoo from './01-introduction/03-db-zoo.md';
import week01DecisionMatrix from './01-introduction/04-decision-matrix.md';
import week01DataTypes from './01-introduction/05-data-types.md';
import week01Quiz from './01-introduction/06-quiz.md';
import week01Assignment from './01-introduction/07-assignment-01.md';
import databasesSyllabus from './syllabus.md';

// Week 02 imports
import week02Readings from './02-sql-fundamentals/00-readings-02.md';
import week02DDL from './02-sql-fundamentals/01-data-definition-language.md';
import week02DML from './02-sql-fundamentals/02-data-manipulation-language.md';
import week02DQL from './02-sql-fundamentals/03-data-query-language.md';
import week02Constraints from './02-sql-fundamentals/04-constraints.md';
import week02Idempotency from './02-sql-fundamentals/05-idempotency.md';
import week02DBML from './02-sql-fundamentals/06-dbml-introduction.md';
import week02QuizConstraints from './02-sql-fundamentals/quiz/constraints-datatypes-quiz.md';
import week02QuizDDLDMLDQL from './02-sql-fundamentals/quiz/ddl-dml-dql-quiz.md';
import week02QuizIdempotencyFix from './02-sql-fundamentals/quiz/idempotency-fix-quiz.md';
import week02QuizIdempotency from './02-sql-fundamentals/quiz/idempotency-quiz.md';

// Program definition
export const databasesProgram: Program = {
    id: 'databases-program-1',
    title: 'Databases',
    description:
        'This course introduces students to database design, SQL, normalization, and relational database theory. Traditional relational databases will be contrasted with NoSQL paradigms including document-oriented, key-value store, and graph databases. Students will gain hands-on experience writing database applications.',
    slug: 'databases',
    thumbnail: 'https://i.imgur.com/D2Sfd70.jpeg',
    videoShowcaseUrl: null,
    estimatedHours: 48,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 0, // Programming
    difficulty: 1, // Intermediate
    visibility: 0, // Public
    status: 1, // Published
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    programContents: [],
    programUsers: [],
    programRatings: [],
    programWishlists: [],
};

// Product definition
export const databasesProduct: Product = {
    id: 'databases-product-1',
    title: 'Databases Course',
    name: 'Databases',
    description:
        'Master database design, SQL, and modern database paradigms from relational to NoSQL systems',
    shortDescription: 'Learn SQL, normalization, and NoSQL databases with hands-on projects',
    imageUrl: 'https://i.imgur.com/D2Sfd70.jpeg',
    type: 0, // Course
    isBundle: false,
    creatorId: '1',
    bundleItems: null,
    referralCommissionPercentage: 0,
    maxAffiliateDiscount: 0,
    affiliateCommissionPercentage: 0,
    visibility: 0, // Public
    status: 1, // Published
    slug: 'databases',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

// Product-Program relation
export const databasesProductProgram: ProductProgram = {
    id: 'databases-product-program-1',
    productId: 'databases-product-1',
    product: databasesProduct,
    programId: 'databases-program-1',
    program: databasesProgram,
    sortOrder: 1,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Program Contents
export const databasesSyllabusContent: ProgramContent = {
    id: 'databases-syllabus',
    programId: 'databases-program-1',
    slug: 'syllabus',
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'Databases course overview, learning outcomes, and schedule',
    type: 0, // Page
    body: databasesSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 01: Introduction - Parent content
export const week01IntroContent: ProgramContent = {
    id: 'databases-week-01-intro',
    programId: 'databases-program-1',
    slug: 'introduction',
    parentId: undefined,
    title: 'Introduction to Databases',
    description: 'Learn the fundamentals of databases, DBMS, query languages, and transactions',
    type: 0, // Page
    body: week01Intro,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 01 child contents
export const week01ReadingsContent: ProgramContent = {
    id: 'databases-week-01-readings',
    programId: 'databases-program-1',
    slug: 'readings',
    parentId: 'databases-week-01-intro',
    title: 'Recommended Readings',
    description: 'Reference materials and further reading on database concepts',
    type: 0, // Page
    body: week01Readings,
    sortOrder: 1,
    isRequired: false,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 35,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week01DbZooContent: ProgramContent = {
    id: 'databases-week-01-db-zoo',
    programId: 'databases-program-1',
    slug: 'database-zoo',
    parentId: 'databases-week-01-intro',
    title: 'Database Zoo',
    description: 'Overview of different database types and paradigms',
    type: 0, // Page
    body: week01DbZoo,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week01DecisionMatrixContent: ProgramContent = {
    id: 'databases-week-01-decision-matrix',
    programId: 'databases-program-1',
    slug: 'decision-matrix',
    parentId: 'databases-week-01-intro',
    title: 'Database Decision Matrix',
    description: 'Framework for choosing the right database for your project',
    type: 0, // Page
    body: week01DecisionMatrix,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week01QuizContent: ProgramContent = {
    id: 'databases-week-01-quiz',
    programId: 'databases-program-1',
    slug: 'quiz-01',
    parentId: 'databases-week-01-intro',
    title: 'Quiz 01: Introduction to Databases',
    description: 'Multiple-choice quiz on the Week 1 database landscape and setup basics',
    type: 0, // Page
    body: week01Quiz,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week01DataTypesContent: ProgramContent = {
    id: 'databases-week-01-data-types',
    programId: 'databases-program-1',
    slug: 'data-types',
    parentId: 'databases-week-01-intro',
    title: 'Database Data Types',
    description: 'Comprehensive overview of data types used in databases',
    type: 0, // Page
    body: week01DataTypes,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week01AssignmentContent: ProgramContent = {
    id: 'databases-week-01-assignment',
    programId: 'databases-program-1',
    slug: 'assignment-01',
    parentId: 'databases-week-01-intro',
    title: 'Assignment 01: Docker & PostgreSQL Setup',
    description: 'Set up Docker, PostgreSQL, and Adminer for your first assignment',
    type: 1, // Assignment
    body: week01Assignment,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 02: SQL Fundamentals - Parent content
export const week02ReadingsContent: ProgramContent = {
    id: 'databases-week-02-readings',
    programId: 'databases-program-1',
    slug: 'sql-fundamentals',
    parentId: undefined,
    title: 'SQL Fundamentals',
    description: 'Learn DDL, DML, DQL, constraints, idempotency, and DBML',
    type: 0, // Page
    body: week02Readings,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 02 child contents
export const week02DDLContent: ProgramContent = {
    id: 'databases-week-02-ddl',
    programId: 'databases-program-1',
    slug: 'data-definition-language',
    parentId: 'databases-week-02-readings',
    title: 'Data Definition Language (DDL)',
    description: 'Learn CREATE, ALTER, DROP, and TRUNCATE statements',
    type: 0, // Page
    body: week02DDL,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02DMLContent: ProgramContent = {
    id: 'databases-week-02-dml',
    programId: 'databases-program-1',
    slug: 'data-manipulation-language',
    parentId: 'databases-week-02-readings',
    title: 'Data Manipulation Language (DML)',
    description: 'Learn INSERT, UPDATE, and DELETE statements',
    type: 0, // Page
    body: week02DML,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 35,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02DQLContent: ProgramContent = {
    id: 'databases-week-02-dql',
    programId: 'databases-program-1',
    slug: 'data-query-language',
    parentId: 'databases-week-02-readings',
    title: 'Data Query Language (DQL)',
    description: 'Master SELECT queries with filtering, sorting, and limiting',
    type: 0, // Page
    body: week02DQL,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 35,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02ConstraintsContent: ProgramContent = {
    id: 'databases-week-02-constraints',
    programId: 'databases-program-1',
    slug: 'constraints',
    parentId: 'databases-week-02-readings',
    title: 'SQL Constraints',
    description: 'Understanding PRIMARY KEY, FOREIGN KEY, UNIQUE, NOT NULL, CHECK, and DEFAULT',
    type: 0, // Page
    body: week02Constraints,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02IdempotencyContent: ProgramContent = {
    id: 'databases-week-02-idempotency',
    programId: 'databases-program-1',
    slug: 'idempotency',
    parentId: 'databases-week-02-readings',
    title: 'Idempotency in SQL',
    description: 'Learn to design idempotent database operations',
    type: 0, // Page
    body: week02Idempotency,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02DBMLContent: ProgramContent = {
    id: 'databases-week-02-dbml',
    programId: 'databases-program-1',
    slug: 'dbml-introduction',
    parentId: 'databases-week-02-readings',
    title: 'Database Markup Language (DBML)',
    description: 'Introduction to DBML for schema design and documentation',
    type: 0, // Page
    body: week02DBML,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 02 Quiz parent node
export const week02QuizzesContent: ProgramContent = {
    id: 'databases-week-02-quizzes',
    programId: 'databases-program-1',
    slug: 'quizzes',
    parentId: 'databases-week-02-readings',
    title: 'Week 02 Quizzes',
    description: 'Test your knowledge of SQL fundamentals',
    type: 0, // Page
    body: '# Week 02 Quizzes\n\nComplete the quizzes below to test your understanding of SQL fundamentals.',
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 02 Quiz contents (children of week02QuizzesContent)
export const week02QuizIdempotencyContent: ProgramContent = {
    id: 'databases-week-02-quiz-idempotency',
    programId: 'databases-program-1',
    slug: 'idempotency-quiz',
    parentId: 'databases-week-02-quizzes',
    title: 'Quiz: Idempotency in SQL Operations',
    description: 'Categorize SQL statements as idempotent or non-idempotent',
    type: 0, // Page
    body: week02QuizIdempotency,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02QuizzesContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02QuizIdempotencyFixContent: ProgramContent = {
    id: 'databases-week-02-quiz-idempotency-fix',
    programId: 'databases-program-1',
    slug: 'idempotency-fix-quiz',
    parentId: 'databases-week-02-quizzes',
    title: 'Quiz: Making SQL Operations Idempotent',
    description: 'Learn how to modify SQL statements to make them idempotent',
    type: 0, // Page
    body: week02QuizIdempotencyFix,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02QuizzesContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02QuizDDLDMLDQLContent: ProgramContent = {
    id: 'databases-week-02-quiz-ddl-dml-dql',
    programId: 'databases-program-1',
    slug: 'ddl-dml-dql-quiz',
    parentId: 'databases-week-02-quizzes',
    title: 'Quiz: DDL, DML, and DQL Translation',
    description: 'Translate between natural language requirements and SQL statements',
    type: 0, // Page
    body: week02QuizDDLDMLDQL,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02QuizzesContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week02QuizConstraintsContent: ProgramContent = {
    id: 'databases-week-02-quiz-constraints',
    programId: 'databases-program-1',
    slug: 'constraints-datatypes-quiz',
    parentId: 'databases-week-02-quizzes',
    title: 'Quiz: Constraints and Data Types',
    description: 'Test your understanding of SQL constraints and data types',
    type: 0, // Page
    body: week02QuizConstraints,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02QuizzesContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Wire program contents and product-program relations
// only the parent contents go directly under the program
databasesProgram.programContents = [
    databasesSyllabusContent,
    week01IntroContent,
    week02ReadingsContent,
];

// Set up parent-child relationships
week01IntroContent.children = [
    week01ReadingsContent,
    week01DbZooContent,
    week01DecisionMatrixContent,
    week01DataTypesContent,
    week01QuizContent,
    week01AssignmentContent,
];

week02ReadingsContent.children = [
    week02DDLContent,
    week02DMLContent,
    week02DQLContent,
    week02ConstraintsContent,
    week02IdempotencyContent,
    week02DBMLContent,
    week02QuizzesContent,
];

week02QuizzesContent.children = [
    week02QuizIdempotencyContent,
    week02QuizIdempotencyFixContent,
    week02QuizDDLDMLDQLContent,
    week02QuizConstraintsContent,
];

databasesProduct.productPrograms = [databasesProductProgram];

export default databasesProgram;
