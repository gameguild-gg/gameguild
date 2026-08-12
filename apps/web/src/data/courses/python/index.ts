import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import week01Lecture from './chapters/week01/lecture.md';
import week02Lecture from './chapters/week02/lecture.md';
import week03Lecture from './chapters/week03/lecture.md';
import week04BooleanOperations from './chapters/week04/boolean_operations.md';
import week04Lecture from './chapters/week04/lecture.md';
import week04Switch from './chapters/week04/switch.md';
import week05Exercise01 from './chapters/week05/exercise-lists-01.md';
import week05Exercise02 from './chapters/week05/exercise-lists-02.md';
import week05Lists from './chapters/week05/lists.md';
import week05Sets from './chapters/week05/sets.md';
import week05Tuples from './chapters/week05/tuples.md';
import week06Lecture from './chapters/week06/lecture.md';
import week07Lecture from './chapters/week07/lecture.md';
import week10Dictionaries from './chapters/week10/dictionaries.md';
import week10Sets from './chapters/week10/sets.md';
import week11Lecture from './chapters/week11/lecture.md';
import week12Lecture from './chapters/week12/lecture.md';
import week12LocalLlm from './chapters/week12/local-llm.md';
import pythonSyllabus from './syllabus.md';

// Program definition
export const pythonProgram: Program = {
    id: 'python-program-1',
    title: 'Python Programming',
    description:
        'Students will learn the history and basics of computing as well as the fundamentals of Python programming. General topics include: the history of computing, number systems, Boolean logic, algorithm design and implementation, and modern computer organization.',
    slug: 'python',
    thumbnail: 'https://www.python.org/static/community_logos/python-logo-generic.svg',
    videoShowcaseUrl: null,
    estimatedHours: 40,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 0, // Programming
    difficulty: 0, // Beginner
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
export const pythonProduct: Product = {
    id: 'python-product-1',
    title: 'Python Programming Course',
    name: 'Python Programming',
    description: 'Learn Python programming fundamentals with this comprehensive course',
    shortDescription: 'Master Python programming from basics to advanced concepts',
    imageUrl: 'https://www.python.org/static/community_logos/python-logo-generic.svg',
    type: 0, // Course
    isBundle: false,
    creatorId: '1',
    bundleItems: null,
    referralCommissionPercentage: 0,
    maxAffiliateDiscount: 0,
    affiliateCommissionPercentage: 0,
    visibility: 0, // Public
    status: 1, // Published
    slug: 'python',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

// Product-Program relation
export const pythonProductProgram: ProductProgram = {
    id: 'python-product-program-1',
    productId: 'python-product-1',
    product: pythonProduct,
    programId: 'python-program-1',
    program: pythonProgram,
    sortOrder: 1,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Program Contents
export const pythonSyllabusContent: ProgramContent = {
    id: 'python-syllabus',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Course Syllabus and Introduction',
    description: 'Course overview, objectives, and weekly schedule',
    type: 0, // Page
    body: pythonSyllabus,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek01Content: ProgramContent = {
    id: 'python-week01',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 01: Introduction to Python',
    description: 'Introduction to algorithms, problem-solving, and Python basics',
    type: 0, // Page
    body: week01Lecture,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek02Content: ProgramContent = {
    id: 'python-week02',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 02: Python Basics',
    description: 'Introduction to Python programming basics',
    type: 0, // Page
    body: week02Lecture,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek03Content: ProgramContent = {
    id: 'python-week03',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 03: Functions and Math',
    description: 'Functions and math in Python programming',
    type: 0, // Page
    body: week03Lecture,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek04Content: ProgramContent = {
    id: 'python-week04',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 04: Python Conditionals and Loops',
    description: 'Flow control in Python programming',
    type: 0, // Page
    body: week04Lecture,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 105,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek05ListsContent: ProgramContent = {
    id: 'python-week05-lists',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 05: Lists and Data Structures',
    description: 'Lists, tuples, and string manipulation',
    type: 0, // Page
    body: week05Lists,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek05Exercise01Content: ProgramContent = {
    id: 'python-week05-exercise01',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Exercise: Two Sum',
    description: 'Practice exercise for lists and algorithms',
    type: 2, // Assignment
    body: week05Exercise01,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek05Exercise02Content: ProgramContent = {
    id: 'python-week05-exercise02',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Exercise: Search Insert Position',
    description: 'Practice exercise for list operations',
    type: 2, // Assignment
    body: week05Exercise02,
    sortOrder: 15,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek06Content: ProgramContent = {
    id: 'python-week06',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 06: Advanced Loops',
    description: 'Advanced looping techniques and patterns',
    type: 0, // Page
    body: week06Lecture,
    sortOrder: 9,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek07Content: ProgramContent = {
    id: 'python-week07',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 07: Nested Loops',
    description: 'Nested loops and advanced loop control',
    type: 0, // Page
    body: week07Lecture,
    sortOrder: 10,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek10DictionariesContent: ProgramContent = {
    id: 'python-week10-dictionaries',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 10: Dictionaries and Sets',
    description: 'Advanced data structures in Python',
    type: 0, // Page
    body: week10Dictionaries,
    sortOrder: 11,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek10SetsContent: ProgramContent = {
    id: 'python-week10-sets',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 10: Sets and Set Operations',
    description: 'Set operations and advanced data structures',
    type: 0, // Page
    body: week10Sets,
    sortOrder: 12,
    isRequired: true,
    estimatedMinutes: 105,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek11Content: ProgramContent = {
    id: 'python-week11',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 11: Files and Exceptions',
    description: 'File handling and exception management',
    type: 0, // Page
    body: week11Lecture,
    sortOrder: 13,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek12Content: ProgramContent = {
    id: 'python-week12',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 12: APIs and Web Services',
    description: 'Working with APIs and web services',
    type: 0, // Page
    body: week12Lecture,
    sortOrder: 14,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek12LocalLlmContent: ProgramContent = {
    id: 'python-week12-local-llm',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 12: Local LLMs with Ollama',
    description: 'Working with local language models',
    type: 0, // Page
    body: week12LocalLlm,
    sortOrder: 15,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek04BooleanOperationsContent: ProgramContent = {
    id: 'python-week04-boolean-operations',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 04: Boolean Operations',
    description: 'Boolean logic and operations in Python',
    type: 0, // Page
    body: week04BooleanOperations,
    sortOrder: 16,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek04SwitchContent: ProgramContent = {
    id: 'python-week04-switch',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 04: Switch Statements',
    description: 'Switch statements and control flow',
    type: 0, // Page
    body: week04Switch,
    sortOrder: 17,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek05SetsContent: ProgramContent = {
    id: 'python-week05-sets',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 05: Sets',
    description: 'Set data structure and operations',
    type: 0, // Page
    body: week05Sets,
    sortOrder: 18,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const pythonWeek05TuplesContent: ProgramContent = {
    id: 'python-week05-tuples',
    programId: 'python-program-1',
    parentId: undefined,
    title: 'Week 05: Tuples',
    description: 'Tuple data structure and immutability',
    type: 0, // Page
    body: week05Tuples,
    sortOrder: 19,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

pythonProgram.programContents = [
    pythonSyllabusContent,
    pythonWeek01Content,
    pythonWeek02Content,
    pythonWeek03Content,
    pythonWeek04Content,
    pythonWeek04BooleanOperationsContent,
    pythonWeek04SwitchContent,
    pythonWeek05ListsContent,
    pythonWeek05SetsContent,
    pythonWeek05TuplesContent,
    pythonWeek05Exercise01Content,
    pythonWeek05Exercise02Content,
    pythonWeek06Content,
    pythonWeek07Content,
    pythonWeek10DictionariesContent,
    pythonWeek10SetsContent,
    pythonWeek11Content,
    pythonWeek12Content,
    pythonWeek12LocalLlmContent,
];