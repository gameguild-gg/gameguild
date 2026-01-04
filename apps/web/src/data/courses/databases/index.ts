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

// Wire program contents and product-program relations
databasesProgram.programContents = [
    databasesSyllabusContent,
    week01IntroContent,
    week01ReadingsContent,
    week01DbZooContent,
    week01DecisionMatrixContent,
    week01DataTypesContent,
    week01QuizContent,
    week01AssignmentContent,
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

databasesProduct.productPrograms = [databasesProductProgram];

export default databasesProgram;
