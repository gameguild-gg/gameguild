import { Product, ProductProgram, Program, ProgramContent, ProgramContentType } from '@/lib/api/generated';

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
import week02Lecture from './02-sql-fundamentals/00-lecture.md';
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

// Week 03 imports
import week03AggregationGrouping from './03-filtering-aggregation/aggregation-and-grouping.md';
import week03FilteringPatternMatching from './03-filtering-aggregation/filtering-and-pattern-matching.md';
import week03Quiz from './03-filtering-aggregation/quiz/filtering-aggregation-quiz.md';
import week03Reveal from './03-filtering-aggregation/reveal.md';

// Week 04 imports
import week04EntityRelationships from './04-normalization/entity-relationships.md';
import week04IndexingFundamentals from './04-normalization/indexing-fundamentals.md';
import week04NormalizationTheory from './04-normalization/normalization-theory.md';
import week04PracticalNormalization from './04-normalization/practical-normalization.md';
import week04QuizNormalization from './04-normalization/quiz/normalization-quiz.md';
import week04Readings from './04-normalization/readings-04.md';
import week04Reveal from './04-normalization/reveal.md';

// Week 05 imports
import week05JoinFundamentals from './05-joins/join-fundamentals.md';
import week05OuterJoins from './05-joins/outer-joins-and-advanced-patterns.md';
import week05JoinsQuiz from './05-joins/quiz/joins-quiz.md';
import week05Readings from './05-joins/readings-05.md';
import week05Reveal from './05-joins/reveal.md';

// Week 06 imports
import week06CTEsAndViews from './06-subqueries-ctes-views/ctes-and-views.md';
import week06SubqueriesCTEsViewsQuiz from './06-subqueries-ctes-views/quiz/subqueries-ctes-views-quiz.md';
import week06Readings from './06-subqueries-ctes-views/readings-06.md';
import week06Reveal from './06-subqueries-ctes-views/reveal.md';
import week06SubqueriesAndSetOperations from './06-subqueries-ctes-views/subqueries-and-set-operations.md';

// Week 07 imports
import week07AccessControl from './07-schema-patterns/access-control.md';
import week07FunctionsProceduresTriggers from './07-schema-patterns/functions-procedures-triggers.md';
import week07ORMQueryBuilders from './07-schema-patterns/orm-query-builders.md';
import week07Quiz from './07-schema-patterns/quiz/week07-quiz.md';
import week07Readings from './07-schema-patterns/readings-07.md';
import week07ReferentialActions from './07-schema-patterns/referential-actions.md';
import week07Reveal from './07-schema-patterns/reveal.md';
import week07ScalabilityBasics from './07-schema-patterns/scalability-basics.md';
import week07SchemaPatterns from './07-schema-patterns/schema-patterns.md';
import week07Transactions from './07-schema-patterns/transactions.md';

// Week 10 imports
import week10AggregationPipeline from './10-mongodb/aggregation-pipeline.md';
import week10Assignment from './10-mongodb/assignment.md';
import week10DrizzleMongoDB from './10-mongodb/drizzle-mongodb.md';
import week10MongoDBCRUD from './10-mongodb/mongodb-crud.md';
import week10MongoDBFundamentals from './10-mongodb/mongodb-fundamentals.md';
import week10Quiz from './10-mongodb/quiz/mongodb-quiz.md';
import week10Readings from './10-mongodb/readings-10.md';
import week10Reveal from './10-mongodb/reveal.md';
import week10SchemaDesignPatterns from './10-mongodb/schema-design-patterns.md';

// Week 11 imports
import week11Assignment from './11-keyvalue-widecolumn/assignment.md';
import week11CassandraFundamentals from './11-keyvalue-widecolumn/cassandra-fundamentals.md';
import week11Quiz from './11-keyvalue-widecolumn/quiz/redis-cassandra-quiz.md';
import week11Readings from './11-keyvalue-widecolumn/readings-11.md';
import week11RedisFundamentals from './11-keyvalue-widecolumn/redis-fundamentals.md';
import week11Reveal from './11-keyvalue-widecolumn/reveal.md';

// Week 12 imports
import week12Assignment from './12-graph-databases/assignment.md';
import week12Neo4jFundamentals from './12-graph-databases/neo4j-fundamentals.md';
import week12Quiz from './12-graph-databases/quiz/graph-neo4j-quiz.md';
import week12Readings from './12-graph-databases/readings-12.md';
import week12Reveal from './12-graph-databases/reveal.md';

// Week 13 imports
import week13Assignment from './13-timeseries-search/assignment.md';
import week13ElasticsearchFundamentals from './13-timeseries-search/elasticsearch-fundamentals.md';
import week13Quiz from './13-timeseries-search/quiz/quiz.md';
import week13Readings from './13-timeseries-search/readings-13.md';
import week13Reveal from './13-timeseries-search/reveal.md';
import week13TimescaledbFundamentals from './13-timeseries-search/timescaledb-fundamentals.md';

// Week 14 imports
import week14Assignment from './14-vector-streaming/assignment.md';
import week14KafkaFundamentals from './14-vector-streaming/kafka-fundamentals.md';
import week14PgvectorFundamentals from './14-vector-streaming/pgvector-fundamentals.md';
import week14Quiz from './14-vector-streaming/quiz/quiz.md';
import week14Readings from './14-vector-streaming/readings-14.md';
import week14Reveal from './14-vector-streaming/reveal.md';

// Week 15 checkpoint import
import week15Assignment from './15-peer/assignment.md';

// Week 16 checkpoint import
import week16Assignment from './16-presentations/assignment.md';

// Final project import
import finalProjectIndex from './09-break/final-project.md';

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
    type: ProgramContentType.REVEAL,
    body: week01DbZoo,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week01IntroContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z'
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
export const week02LectureContent: ProgramContent = {
    id: 'databases-week-02-lecture',
    programId: 'databases-program-1',
    slug: 'sql-fundamentals-lecture',
    parentId: undefined,
    title: 'SQL Fundamentals Lecture',
    description: 'Reveal.js lecture covering DDL, DML, DQL, constraints, idempotency, and DBML',
    type: ProgramContentType.REVEAL,
    body: week02Lecture,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 02: SQL Fundamentals - Readings
export const week02ReadingsContent: ProgramContent = {
    id: 'databases-week-02-readings',
    programId: 'databases-program-1',
    slug: 'sql-fundamentals',
    parentId: 'databases-week-02-lecture',
    title: 'SQL Fundamentals',
    description: 'Learn DDL, DML, DQL, constraints, idempotency, and DBML',
    type: 0, // Page
    body: week02Readings,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02LectureContent,
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
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week02QuizzesContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 03: Filtering, Aggregation & Grouping
export const week03RevealContent: ProgramContent = {
    id: 'databases-week-03-reveal',
    programId: 'databases-program-1',
    slug: 'filtering-aggregation-reveal',
    parentId: undefined,
    title: 'Filtering, Pattern Matching & Aggregation',
    description: 'Reveal.js presentation covering WHERE clauses, pattern matching, NULL handling, aggregate functions, GROUP BY, and HAVING',
    type: ProgramContentType.REVEAL,
    body: week03Reveal,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 50,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week03FilteringPatternMatchingContent: ProgramContent = {
    id: 'databases-week-03-filtering-pattern-matching',
    programId: 'databases-program-1',
    slug: 'filtering-pattern-matching',
    parentId: 'databases-week-03-reveal',
    title: 'Filtering & Pattern Matching',
    description: 'Learn boolean logic, IN/BETWEEN operators, LIKE/ILIKE, regex, NULL handling, CASE expressions, and date filters',
    type: 0, // Page
    body: week03FilteringPatternMatching,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week03RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week03AggregationGroupingContent: ProgramContent = {
    id: 'databases-week-03-aggregation-grouping',
    programId: 'databases-program-1',
    slug: 'aggregation-grouping',
    parentId: 'databases-week-03-reveal',
    title: 'Aggregation & Grouping',
    description: 'Master COUNT, SUM, AVG, MIN, MAX, GROUP BY, HAVING, DISTINCT aggregates, and conditional aggregation',
    type: 0, // Page
    body: week03AggregationGrouping,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week03RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week03QuizContent: ProgramContent = {
    id: 'databases-week-03-quiz',
    programId: 'databases-program-1',
    slug: 'filtering-aggregation-quiz',
    parentId: 'databases-week-03-reveal',
    title: 'Quiz: Filtering & Aggregation',
    description: 'Test your understanding of boolean logic, filtering operators, pattern matching, NULL handling, CASE expressions, and aggregate functions',
    type: 0, // Page
    body: week03Quiz,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week03RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 04: Normalization & Database Design - Parent content (Reveal)
export const week04RevealContent: ProgramContent = {
    id: 'databases-week-04-reveal',
    programId: 'databases-program-1',
    slug: 'normalization-design',
    parentId: undefined,
    title: 'Normalization & Database Design',
    description: 'Reveal.js presentation covering normal forms, functional dependencies, ER modeling, constraints, indexing, and denormalization',
    type: ProgramContentType.REVEAL,
    body: week04Reveal,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 04 child contents
export const week04NormalizationTheoryContent: ProgramContent = {
    id: 'databases-week-04-normalization-theory',
    programId: 'databases-program-1',
    slug: 'normalization-theory',
    parentId: 'databases-week-04-reveal',
    title: 'Normalization Theory',
    description: 'Learn 1NF, 2NF, 3NF, BCNF and functional dependencies',
    type: 0, // Page
    body: week04NormalizationTheory,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week04RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week04PracticalNormalizationContent: ProgramContent = {
    id: 'databases-week-04-practical-normalization',
    programId: 'databases-program-1',
    slug: 'practical-normalization',
    parentId: 'databases-week-04-reveal',
    title: 'Practical Normalization & Denormalization',
    description: 'When to normalize, when to denormalize, and real-world schema analysis',
    type: 0, // Page
    body: week04PracticalNormalization,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week04RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week04EntityRelationshipsContent: ProgramContent = {
    id: 'databases-week-04-entity-relationships',
    programId: 'databases-program-1',
    slug: 'entity-relationships',
    parentId: 'databases-week-04-reveal',
    title: 'Entity-Relationship Modeling',
    description: 'ER diagrams, cardinality (1:1, 1:N, M:N), junction tables, and notation styles',
    type: 0, // Page
    body: week04EntityRelationships,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week04RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week04IndexingFundamentalsContent: ProgramContent = {
    id: 'databases-week-04-indexing-fundamentals',
    programId: 'databases-program-1',
    slug: 'indexing-fundamentals',
    parentId: 'databases-week-04-reveal',
    title: 'Indexing Fundamentals',
    description: 'B-Tree indexes, index types, when to index, and EXPLAIN ANALYZE',
    type: 0, // Page
    body: week04IndexingFundamentals,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week04RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week04ReadingsContent: ProgramContent = {
    id: 'databases-week-04-readings',
    programId: 'databases-program-1',
    slug: 'readings-04',
    parentId: 'databases-week-04-reveal',
    title: 'Week 04 Readings & Resources',
    description: 'Sample databases, ER diagram tools, and PostgreSQL documentation',
    type: 0, // Page
    body: week04Readings,
    sortOrder: 5,
    isRequired: false,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week04RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 04 Quiz content (direct child of reveal)
export const week04QuizNormalizationContent: ProgramContent = {
    id: 'databases-week-04-quiz-normalization',
    programId: 'databases-program-1',
    slug: 'normalization-quiz',
    parentId: 'databases-week-04-reveal',
    title: 'Quiz: Normalization, Entity-Relationships & Indexing',
    description: 'Comprehensive quiz covering normal forms, ER modeling, and indexing fundamentals',
    type: 0, // Page
    body: week04QuizNormalization,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week04RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 05: Joins - Parent content (Reveal)
export const week05RevealContent: ProgramContent = {
    id: 'databases-week-05-reveal',
    programId: 'databases-program-1',
    slug: 'joins',
    parentId: undefined,
    title: 'SQL Joins: Fundamentals & Outer Joins',
    description: 'Reveal.js presentation covering join fundamentals and outer joins',
    type: ProgramContentType.REVEAL,
    body: week05Reveal,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week05ReadingsContent: ProgramContent = {
    id: 'databases-week-05-readings',
    programId: 'databases-program-1',
    slug: 'readings-05',
    parentId: 'databases-week-05-reveal',
    title: 'Week 05 Readings & Resources',
    description: 'Readings for joins, outer joins, and join patterns',
    type: 0, // Page
    body: week05Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week05RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week05JoinFundamentalsContent: ProgramContent = {
    id: 'databases-week-05-join-fundamentals',
    programId: 'databases-program-1',
    slug: 'join-fundamentals',
    parentId: 'databases-week-05-readings',
    title: 'Join Fundamentals',
    description: 'INNER JOIN mechanics, join conditions, aliases, and multi-table joins',
    type: 0, // Page
    body: week05JoinFundamentals,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 35,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week05ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week05OuterJoinsContent: ProgramContent = {
    id: 'databases-week-05-outer-joins',
    programId: 'databases-program-1',
    slug: 'outer-joins-and-advanced-patterns',
    parentId: 'databases-week-05-readings',
    title: 'Outer Joins & Advanced Patterns',
    description: 'LEFT/RIGHT/FULL OUTER joins, self-joins, and CROSS JOIN',
    type: 0, // Page
    body: week05OuterJoins,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week05ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week05JoinsQuizContent: ProgramContent = {
    id: 'databases-week-05-joins-quiz',
    programId: 'databases-program-1',
    slug: 'joins-quiz',
    parentId: 'databases-week-05-reveal',
    title: 'Quiz: SQL Joins',
    description: 'Practice INNER/OUTER JOINs, self-joins, and CROSS JOIN patterns',
    type: 0, // Page
    body: week05JoinsQuiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 35,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week05RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 06: Subqueries, CTEs & Views - Parent content (Reveal)
export const week06RevealContent: ProgramContent = {
    id: 'databases-week-06-reveal',
    programId: 'databases-program-1',
    slug: 'subqueries-ctes-views',
    parentId: undefined,
    title: 'Subqueries, CTEs & Views',
    description: 'Reveal.js presentation covering subqueries, set operations, CTEs, recursive CTEs, views, and materialized views',
    type: ProgramContentType.REVEAL,
    body: week06Reveal,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 50,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week06ReadingsContent: ProgramContent = {
    id: 'databases-week-06-readings',
    programId: 'databases-program-1',
    slug: 'readings-06',
    parentId: 'databases-week-06-reveal',
    title: 'Week 06 Readings & Resources',
    description: 'Readings for subqueries, set operations, CTEs, views, and materialized views',
    type: 0, // Page
    body: week06Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week06RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week06SubqueriesAndSetOperationsContent: ProgramContent = {
    id: 'databases-week-06-subqueries-set-operations',
    programId: 'databases-program-1',
    slug: 'subqueries-and-set-operations',
    parentId: 'databases-week-06-readings',
    title: 'Subqueries & Set Operations',
    description: 'Scalar subqueries, IN, EXISTS, ANY/ALL, correlated subqueries, UNION, INTERSECT, EXCEPT',
    type: 0, // Page
    body: week06SubqueriesAndSetOperations,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week06ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week06CTEsAndViewsContent: ProgramContent = {
    id: 'databases-week-06-ctes-views',
    programId: 'databases-program-1',
    slug: 'ctes-and-views',
    parentId: 'databases-week-06-readings',
    title: 'CTEs & Views',
    description: 'Common Table Expressions, recursive CTEs, views, updatable views, and materialized views',
    type: 0, // Page
    body: week06CTEsAndViews,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week06ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week06SubqueriesCTEsViewsQuizContent: ProgramContent = {
    id: 'databases-week-06-subqueries-ctes-views-quiz',
    programId: 'databases-program-1',
    slug: 'subqueries-ctes-views-quiz',
    parentId: 'databases-week-06-reveal',
    title: 'Quiz: Subqueries, CTEs & Views',
    description: 'Practice subqueries, set operations, CTEs, views, and materialized views',
    type: 0, // Page
    body: week06SubqueriesCTEsViewsQuiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 40,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week06RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 07 Content
export const week07RevealContent: ProgramContent = {
    id: 'databases-week-07-reveal',
    programId: 'databases-program-1',
    slug: 'schema-patterns-tcl-dcl-orm',
    parentId: undefined,
    title: 'Schema Patterns, TCL, DCL & ORM',
    description: 'Reveal.js presentation covering schema patterns, data integrity, referential actions, functions, procedures, triggers, transactions, access control, scalability, and ORMs',
    type: ProgramContentType.REVEAL,
    body: week07Reveal,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07ReadingsContent: ProgramContent = {
    id: 'databases-week-07-readings',
    programId: 'databases-program-1',
    slug: 'readings-07',
    parentId: 'databases-week-07-reveal',
    title: 'Week 07 Readings & Resources',
    description: 'Readings for schema patterns, referential actions, functions, procedures, triggers, transactions, access control, scalability, and ORMs',
    type: 0, // Page
    body: week07Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07SchemaPatternsContent: ProgramContent = {
    id: 'databases-week-07-schema-patterns',
    programId: 'databases-program-1',
    slug: 'schema-patterns',
    parentId: 'databases-week-07-readings',
    title: 'Schema Patterns & Data Integrity',
    description: 'Soft delete, optimistic locking, history tables, checksums, and audit trails',
    type: 0, // Page
    body: week07SchemaPatterns,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07ReferentialActionsContent: ProgramContent = {
    id: 'databases-week-07-referential-actions',
    programId: 'databases-program-1',
    slug: 'referential-actions',
    parentId: 'databases-week-07-readings',
    title: 'Referential Actions',
    description: 'CASCADE, RESTRICT, SET NULL, SET DEFAULT, and managing foreign key relationships',
    type: 0, // Page
    body: week07ReferentialActions,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07FunctionsProceduresTriggersContent: ProgramContent = {
    id: 'databases-week-07-functions-procedures-triggers',
    programId: 'databases-program-1',
    slug: 'functions-procedures-triggers',
    parentId: 'databases-week-07-readings',
    title: 'Functions, Procedures & Triggers',
    description: 'SQL functions, PL/pgSQL functions, stored procedures, and database triggers',
    type: 0, // Page
    body: week07FunctionsProceduresTriggers,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07TransactionsContent: ProgramContent = {
    id: 'databases-week-07-transactions',
    programId: 'databases-program-1',
    slug: 'transactions',
    parentId: 'databases-week-07-readings',
    title: 'Transactions & ACID Properties',
    description: 'TCL commands, ACID properties, isolation levels, and concurrency control',
    type: 0, // Page
    body: week07Transactions,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07AccessControlContent: ProgramContent = {
    id: 'databases-week-07-access-control',
    programId: 'databases-program-1',
    slug: 'access-control',
    parentId: 'databases-week-07-readings',
    title: 'Access Control & DCL',
    description: 'GRANT, REVOKE, roles, privileges, and Row-Level Security',
    type: 0, // Page
    body: week07AccessControl,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07ScalabilityBasicsContent: ProgramContent = {
    id: 'databases-week-07-scalability-basics',
    programId: 'databases-program-1',
    slug: 'scalability-basics',
    parentId: 'databases-week-07-readings',
    title: 'Scalability Basics',
    description: 'Replication, partitioning, sharding, connection pooling, and scaling strategies',
    type: 0, // Page
    body: week07ScalabilityBasics,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07ORMQueryBuildersContent: ProgramContent = {
    id: 'databases-week-07-orm-query-builders',
    programId: 'databases-program-1',
    slug: 'orm-query-builders',
    parentId: 'databases-week-07-readings',
    title: 'ORMs & Query Builders',
    description: 'Drizzle ORM, query builders, and SQL injection prevention',
    type: 0, // Page
    body: week07ORMQueryBuilders,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week07QuizContent: ProgramContent = {
    id: 'databases-week-07-quiz',
    programId: 'databases-program-1',
    slug: 'schema-patterns-tcl-dcl-orm-quiz',
    parentId: 'databases-week-07-reveal',
    title: 'Quiz: Schema Patterns, TCL, DCL & ORM',
    description: 'Practice schema patterns, referential actions, functions, procedures, triggers, transactions, access control, scalability, and ORMs',
    type: 0, // Page
    body: week07Quiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week07RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 10: Document Databases - MongoDB
export const week10RevealContent: ProgramContent = {
    id: 'databases-week-10-reveal',
    programId: 'databases-program-1',
    slug: 'document-databases-mongodb',
    parentId: undefined,
    title: 'Document Databases: MongoDB',
    description: 'Reveal.js presentation covering MongoDB fundamentals, schema design, CRUD operations, aggregation pipelines, and Drizzle ORM integration',
    type: ProgramContentType.REVEAL,
    body: week10Reveal,
    sortOrder: 10,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10ReadingsContent: ProgramContent = {
    id: 'databases-week-10-readings',
    programId: 'databases-program-1',
    slug: 'readings-10',
    parentId: 'databases-week-10-reveal',
    title: 'Week 10 Readings & Resources',
    description: 'Curated references for MongoDB documentation, schema design, CRUD, aggregation, and tooling',
    type: 0, // Page
    body: week10Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10MongoDBFundamentalsContent: ProgramContent = {
    id: 'databases-week-10-mongodb-fundamentals',
    programId: 'databases-program-1',
    slug: 'mongodb-fundamentals',
    parentId: 'databases-week-10-readings',
    title: 'MongoDB Fundamentals',
    description: 'Introduction to the document model, BSON types, ObjectId structure, and MongoDB use cases',
    type: 0, // Page
    body: week10MongoDBFundamentals,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10SchemaDesignPatternsContent: ProgramContent = {
    id: 'databases-week-10-schema-design-patterns',
    programId: 'databases-program-1',
    slug: 'schema-design-patterns',
    parentId: 'databases-week-10-readings',
    title: 'Schema Design Patterns',
    description: 'Learn embedding vs referencing and practical MongoDB schema patterns for scalable systems',
    type: 0, // Page
    body: week10SchemaDesignPatterns,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10MongoDBCRUDContent: ProgramContent = {
    id: 'databases-week-10-mongodb-crud',
    programId: 'databases-program-1',
    slug: 'mongodb-crud',
    parentId: 'databases-week-10-readings',
    title: 'MongoDB CRUD Operations',
    description: 'Master Create, Read, Update, and Delete operations using MongoDB query and update operators',
    type: 0, // Page
    body: week10MongoDBCRUD,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10AggregationPipelineContent: ProgramContent = {
    id: 'databases-week-10-aggregation-pipeline',
    programId: 'databases-program-1',
    slug: 'aggregation-pipeline',
    parentId: 'databases-week-10-readings',
    title: 'MongoDB Aggregation Pipeline',
    description: 'Use pipeline stages like $match, $group, $project, and $lookup to transform and analyze data',
    type: 0, // Page
    body: week10AggregationPipeline,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10DrizzleMongoDBContent: ProgramContent = {
    id: 'databases-week-10-drizzle-mongodb',
    programId: 'databases-program-1',
    slug: 'drizzle-mongodb',
    parentId: 'databases-week-10-readings',
    title: 'Drizzle ORM with MongoDB',
    description: 'Integrate MongoDB with Drizzle ORM for type-safe schema definitions and CRUD workflows',
    type: 0, // Page
    body: week10DrizzleMongoDB,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week10QuizContent: ProgramContent = {
    id: 'databases-week-10-quiz',
    programId: 'databases-program-1',
    slug: 'mongodb-quiz',
    parentId: 'databases-week-10-reveal',
    title: 'Quiz 08: Document Databases - MongoDB Fundamentals',
    description: 'Assess your understanding of MongoDB fundamentals, schema design, CRUD operations, and aggregation pipelines',
    type: 0, // Page
    body: week10Quiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week10RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 11: Key-Value & Wide-Column Stores
export const week11RevealContent: ProgramContent = {
    id: 'databases-week-11-reveal',
    programId: 'databases-program-1',
    slug: 'keyvalue-widecolumn',
    parentId: undefined,
    title: 'Key-Value & Wide-Column Stores',
    description: 'Reveal.js presentation covering Redis, Cassandra, CAP theorem, CQL, and data modeling',
    type: ProgramContentType.REVEAL,
    body: week11Reveal,
    sortOrder: 11,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week11ReadingsContent: ProgramContent = {
    id: 'databases-week-11-readings',
    programId: 'databases-program-1',
    slug: 'readings-11',
    parentId: 'databases-week-11-reveal',
    title: 'Week 11 Readings & Resources',
    description: 'Curated references for Redis, Cassandra, CAP theorem, and distributed systems',
    type: 0, // Page
    body: week11Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week11RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week11RedisFundamentalsContent: ProgramContent = {
    id: 'databases-week-11-redis-fundamentals',
    programId: 'databases-program-1',
    slug: 'redis-fundamentals',
    parentId: 'databases-week-11-readings',
    title: 'Redis Fundamentals',
    description: 'In-memory key-value store: data structures, TTL, Pub/Sub, transactions, and ioredis',
    type: 0, // Page
    body: week11RedisFundamentals,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week11ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week11CassandraFundamentalsContent: ProgramContent = {
    id: 'databases-week-11-cassandra-fundamentals',
    programId: 'databases-program-1',
    slug: 'cassandra-fundamentals',
    parentId: 'databases-week-11-readings',
    title: 'Cassandra Fundamentals',
    description: 'Distributed wide-column store: architecture, CAP theorem, CQL, partition/clustering keys',
    type: 0, // Page
    body: week11CassandraFundamentals,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week11ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week11QuizContent: ProgramContent = {
    id: 'databases-week-11-quiz',
    programId: 'databases-program-1',
    slug: 'redis-cassandra-quiz',
    parentId: 'databases-week-11-reveal',
    title: 'Quiz 09: Key-Value & Wide-Column Stores',
    description: 'Assess your understanding of Redis, Cassandra, CAP theorem, and data modeling',
    type: 0, // Page
    body: week11Quiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week11RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Final Project - Parent content
export const finalProjectContent: ProgramContent = {
    id: 'databases-final-project',
    programId: 'databases-program-1',
    slug: 'final-project',
    parentId: undefined,
    title: 'Final Project',
    description: 'Build a multi-database application using 3+ database types orchestrated via Docker Compose',
    type: 0, // Page
    body: finalProjectIndex,
    sortOrder: 10,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 1: Proposal (Week 10)
export const week10AssignmentContent: ProgramContent = {
    id: 'databases-week-10-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-1-proposal',
    parentId: 'databases-final-project',
    title: 'Checkpoint 1: Proposal',
    description: 'Form your team, select a topic, and submit a project proposal slideshow',
    type: 1, // Assignment
    body: week10Assignment,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 2: Architecture Design (Week 11)
export const week11AssignmentContent: ProgramContent = {
    id: 'databases-week-11-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-2-architecture',
    parentId: 'databases-final-project',
    title: 'Checkpoint 2: Architecture Design',
    description: 'Design and document your multi-database system architecture with diagrams and data flow',
    type: 1, // Assignment
    body: week11Assignment,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week12RevealContent: ProgramContent = {
    id: 'databases-week-12-reveal',
    programId: 'databases-program-1',
    slug: 'graph-databases',
    parentId: undefined,
    title: 'Graph Databases: Neo4j',
    description: 'Reveal.js presentation covering Neo4j, Cypher query language, traversals, and use cases',
    type: ProgramContentType.REVEAL,
    body: week12Reveal,
    sortOrder: 12,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week12ReadingsContent: ProgramContent = {
    id: 'databases-week-12-readings',
    programId: 'databases-program-1',
    slug: 'readings-12',
    parentId: 'databases-week-12-reveal',
    title: 'Week 12 Readings & Resources',
    description: 'Curated references for Neo4j, Cypher, graph algorithms, and graph data modeling',
    type: 0, // Page
    body: week12Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week12RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week12Neo4jFundamentalsContent: ProgramContent = {
    id: 'databases-week-12-neo4j-fundamentals',
    programId: 'databases-program-1',
    slug: 'neo4j-fundamentals',
    parentId: 'databases-week-12-readings',
    title: 'Neo4j Fundamentals',
    description: 'Graph model, Cypher CRUD, variable-length paths, indexes, neo4j-driver, and use cases',
    type: 0, // Page
    body: week12Neo4jFundamentals,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week12ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week12QuizContent: ProgramContent = {
    id: 'databases-week-12-quiz',
    programId: 'databases-program-1',
    slug: 'graph-neo4j-quiz',
    parentId: 'databases-week-12-reveal',
    title: 'Quiz 10: Graph Databases & Neo4j',
    description: 'Assess your understanding of Neo4j, Cypher, traversals, and graph use cases',
    type: 0, // Page
    body: week12Quiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week12RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 3: Proof of Concept (Week 12)
export const week12AssignmentContent: ProgramContent = {
    id: 'databases-week-12-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-3-poc',
    parentId: 'databases-final-project',
    title: 'Checkpoint 3: Proof of Concept',
    description: 'Docker Compose running with at least one database operational and seed data loaded',
    type: 1, // Assignment
    body: week12Assignment,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 240,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week13RevealContent: ProgramContent = {
    id: 'databases-week-13-reveal',
    programId: 'databases-program-1',
    slug: 'timeseries-search',
    parentId: undefined,
    title: 'Time Series & Search Engines: TimescaleDB & Elasticsearch',
    description: 'Reveal.js presentation covering TimescaleDB hypertables, compression, continuous aggregates, Elasticsearch inverted indices, Query DSL, and aggregations',
    type: ProgramContentType.REVEAL,
    body: week13Reveal,
    sortOrder: 13,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week13ReadingsContent: ProgramContent = {
    id: 'databases-week-13-readings',
    programId: 'databases-program-1',
    slug: 'readings-13',
    parentId: 'databases-week-13-reveal',
    title: 'Week 13 Readings & Resources',
    description: 'Curated references for TimescaleDB, Elasticsearch, inverted indices, and aggregations',
    type: 0, // Page
    body: week13Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week13RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week13TimescaledbFundamentalsContent: ProgramContent = {
    id: 'databases-week-13-timescaledb-fundamentals',
    programId: 'databases-program-1',
    slug: 'timescaledb-fundamentals',
    parentId: 'databases-week-13-readings',
    title: 'TimescaleDB Fundamentals',
    description: 'Hypertables, chunks, time_bucket(), compression, retention policies, continuous aggregates, and Drizzle ORM integration',
    type: 0, // Page
    body: week13TimescaledbFundamentals,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week13ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week13ElasticsearchFundamentalsContent: ProgramContent = {
    id: 'databases-week-13-elasticsearch-fundamentals',
    programId: 'databases-program-1',
    slug: 'elasticsearch-fundamentals',
    parentId: 'databases-week-13-readings',
    title: 'Elasticsearch Fundamentals',
    description: 'Inverted indices, documents, indices, mappings, analyzers, Query DSL, aggregations, and @elastic/elasticsearch client',
    type: 0, // Page
    body: week13ElasticsearchFundamentals,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week13ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week13QuizContent: ProgramContent = {
    id: 'databases-week-13-quiz',
    programId: 'databases-program-1',
    slug: 'timeseries-search-quiz',
    parentId: 'databases-week-13-reveal',
    title: 'Quiz 11: Time Series & Search Engines',
    description: 'Assess your understanding of TimescaleDB, Elasticsearch, inverted indices, and Query DSL',
    type: 0, // Page
    body: week13Quiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week13RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 4: Testing Session 1 (Week 13)
export const week13AssignmentContent: ProgramContent = {
    id: 'databases-week-13-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-4-testing',
    parentId: 'databases-final-project',
    title: 'Checkpoint 4: Testing Session 1',
    description: 'In-class peer testing session — demonstrate your system and collect feedback',
    type: 1, // Assignment
    body: week13Assignment,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 240,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 14: Vector Databases & Event Streaming
export const week14RevealContent: ProgramContent = {
    id: 'databases-week-14-reveal',
    programId: 'databases-program-1',
    slug: 'vector-streaming-lecture',
    parentId: 'databases-program-1',
    title: 'Week 14: Vector Databases & Event Streaming',
    description: 'pgvector, embeddings, similarity search, RAG, Apache Kafka, topics, partitions, producers, consumers, consumer groups',
    type: ProgramContentType.REVEAL,
    body: week14Reveal,
    sortOrder: 14,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week14ReadingsContent: ProgramContent = {
    id: 'databases-week-14-readings',
    programId: 'databases-program-1',
    slug: 'vector-streaming-readings',
    parentId: 'databases-week-14-reveal',
    title: 'Week 14 Readings',
    description: 'Curated readings on pgvector, embeddings, RAG, Kafka architecture, kafkajs, and event-driven systems',
    type: 0, // Page
    body: week14Readings,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week14RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week14PgvectorFundamentalsContent: ProgramContent = {
    id: 'databases-week-14-pgvector-fundamentals',
    programId: 'databases-program-1',
    slug: 'pgvector-fundamentals',
    parentId: 'databases-week-14-readings',
    title: 'pgvector Fundamentals',
    description: 'Vector similarity search, embeddings, cosine/euclidean/inner product metrics, IVFFlat, HNSW indexing, RAG, and Drizzle ORM integration',
    type: 0, // Page
    body: week14PgvectorFundamentals,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week14ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week14KafkaFundamentalsContent: ProgramContent = {
    id: 'databases-week-14-kafka-fundamentals',
    programId: 'databases-program-1',
    slug: 'kafka-fundamentals',
    parentId: 'databases-week-14-readings',
    title: 'Kafka Fundamentals',
    description: 'Event streaming, topics, partitions, producers, consumers, consumer groups, offsets, and kafkajs client',
    type: 0, // Page
    body: week14KafkaFundamentals,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week14ReadingsContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const week14QuizContent: ProgramContent = {
    id: 'databases-week-14-quiz',
    programId: 'databases-program-1',
    slug: 'vector-streaming-quiz',
    parentId: 'databases-week-14-reveal',
    title: 'Quiz 12: Vector Databases & Event Streaming',
    description: 'Assess your understanding of pgvector, embeddings, similarity search, RAG, Kafka, and event streaming',
    type: 0, // Page
    body: week14Quiz,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: week14RevealContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 5: Feature Freeze (Week 14)
export const week14AssignmentContent: ProgramContent = {
    id: 'databases-week-14-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-5-feature-freeze',
    parentId: 'databases-final-project',
    title: 'Checkpoint 5: Testing Session 2 & Feature Freeze',
    description: 'Second testing session and feature freeze — bug fixes and documentation only after this week',
    type: 1, // Assignment
    body: week14Assignment,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 240,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 6: Peer Evaluation & Code Freeze (Week 15)
export const week15AssignmentContent: ProgramContent = {
    id: 'databases-week-15-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-6-peer-eval',
    parentId: 'databases-final-project',
    title: 'Checkpoint 6: Peer Evaluation & Code Freeze',
    description: 'Exchange repos for code review, submit writeup draft, and freeze code by Wednesday',
    type: 1, // Assignment
    body: week15Assignment,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 240,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Checkpoint 7: Final Presentations (Week 16)
export const week16AssignmentContent: ProgramContent = {
    id: 'databases-week-16-assignment',
    programId: 'databases-program-1',
    slug: 'checkpoint-7-presentations',
    parentId: 'databases-final-project',
    title: 'Checkpoint 7: Final Presentations',
    description: '10-minute presentation with live demo, Q&A, and all final deliverables due',
    type: 1, // Assignment
    body: week16Assignment,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 240,
    visibility: 1, // Published
    program: databasesProgram,
    parent: finalProjectContent,
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
    week02LectureContent,
    week03RevealContent,
    week04RevealContent,
    week05RevealContent,
    week06RevealContent,
    week07RevealContent,
    week10RevealContent,
    week11RevealContent,
    week12RevealContent,
    week13RevealContent,
    week14RevealContent,
    finalProjectContent,
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


week02LectureContent.children = [
    week02DDLContent,
    week02DMLContent,
    week02DQLContent,
    week02ConstraintsContent,
    week02IdempotencyContent,
    week02DBMLContent,
    week02QuizzesContent,];

week02QuizzesContent.children = [
    week02QuizIdempotencyContent,
    week02QuizIdempotencyFixContent,
    week02QuizDDLDMLDQLContent,
    week02QuizConstraintsContent,
];

week03RevealContent.children = [
    week03FilteringPatternMatchingContent,
    week03AggregationGroupingContent,
    week03QuizContent,
];

week04RevealContent.children = [
    week04NormalizationTheoryContent,
    week04PracticalNormalizationContent,
    week04EntityRelationshipsContent,
    week04IndexingFundamentalsContent,
    week04ReadingsContent,
    week04QuizNormalizationContent,
];

week05RevealContent.children = [
    week05ReadingsContent,
    week05JoinsQuizContent,
];

week05ReadingsContent.children = [
    week05JoinFundamentalsContent,
    week05OuterJoinsContent,
];

week06RevealContent.children = [
    week06ReadingsContent,
    week06SubqueriesCTEsViewsQuizContent,
];

week06ReadingsContent.children = [
    week06SubqueriesAndSetOperationsContent,
    week06CTEsAndViewsContent,
];

week07RevealContent.children = [
    week07ReadingsContent,
    week07QuizContent,
];

week07ReadingsContent.children = [
    week07SchemaPatternsContent,
    week07ReferentialActionsContent,
    week07FunctionsProceduresTriggersContent,
    week07TransactionsContent,
    week07AccessControlContent,
    week07ScalabilityBasicsContent,
    week07ORMQueryBuildersContent,
];

week10RevealContent.children = [
    week10ReadingsContent,
    week10QuizContent,
];

week10ReadingsContent.children = [
    week10MongoDBFundamentalsContent,
    week10SchemaDesignPatternsContent,
    week10MongoDBCRUDContent,
    week10AggregationPipelineContent,
    week10DrizzleMongoDBContent,
];

week11RevealContent.children = [
    week11ReadingsContent,
    week11QuizContent,
];

week11ReadingsContent.children = [
    week11RedisFundamentalsContent,
    week11CassandraFundamentalsContent,
];

week12RevealContent.children = [
    week12ReadingsContent,
    week12QuizContent,
];

week12ReadingsContent.children = [
    week12Neo4jFundamentalsContent,
];

week13RevealContent.children = [
    week13ReadingsContent,
    week13QuizContent,
];

week13ReadingsContent.children = [
    week13TimescaledbFundamentalsContent,
    week13ElasticsearchFundamentalsContent,
];

week14RevealContent.children = [
    week14ReadingsContent,
    week14QuizContent,
];

week14ReadingsContent.children = [
    week14PgvectorFundamentalsContent,
    week14KafkaFundamentalsContent,
];

finalProjectContent.children = [
    week10AssignmentContent,
    week11AssignmentContent,
    week12AssignmentContent,
    week13AssignmentContent,
    week14AssignmentContent,
    week15AssignmentContent,
    week16AssignmentContent,
];

databasesProduct.productPrograms = [databasesProductProgram];

export default databasesProgram;
