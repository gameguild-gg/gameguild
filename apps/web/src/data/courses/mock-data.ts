import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Python Course Imports
import week01Lecture from './python/chapters/week01/lecture.md';
import week02Lecture from './python/chapters/week02/lecture.md';
import week03Lecture from './python/chapters/week03/lecture.md';
import week04BooleanOperations from './python/chapters/week04/boolean_operations.md';
import week04Lecture from './python/chapters/week04/lecture.md';
import week04Switch from './python/chapters/week04/switch.md';
import week05Exercise01 from './python/chapters/week05/exercise-lists-01.md';
import week05Exercise02 from './python/chapters/week05/exercise-lists-02.md';
import week05Lists from './python/chapters/week05/lists.md';
import week05Sets from './python/chapters/week05/sets.md';
import week05Tuples from './python/chapters/week05/tuples.md';
import week06Lecture from './python/chapters/week06/lecture.md';
import week07Lecture from './python/chapters/week07/lecture.md';
import week10Dictionaries from './python/chapters/week10/dictionaries.md';
import week10Sets from './python/chapters/week10/sets.md';
import week11Lecture from './python/chapters/week11/lecture.md';
import week12Lecture from './python/chapters/week12/lecture.md';
import week12LocalLlm from './python/chapters/week12/local-llm.md';
import pythonSyllabus from './python/syllabus.md';

// AI4Games Course Imports
import ai4gamesWeek01Lecture from './ai4games/chapters/week01/lecture.md';
import ai4gamesWeek01Readings from './ai4games/chapters/week01/readings.md';
import ai4gamesWeek02Lecture from './ai4games/chapters/week02/lecture.md';
import ai4gamesWeek02Pcg from './ai4games/chapters/week02/pcg.md';
import ai4gamesWeek03Astar from './ai4games/chapters/week03/a-star.md';
import ai4gamesWeek03Lecture from './ai4games/chapters/week03/lecture.md';
import ai4gamesWeek04Assignment from './ai4games/chapters/week04/assignment.md';
import ai4gamesWeek04Lecture from './ai4games/chapters/week04/lecture.md';
import ai4gamesWeek05Lecture from './ai4games/chapters/week05/lecture.md';
import ai4gamesWeek05Lecture2 from './ai4games/chapters/week05/lecture2.md';
import ai4gamesWeek06Lecture from './ai4games/chapters/week06/lecture.md';
import ai4gamesWeek07Lecture from './ai4games/chapters/week07/lecture.md';
import ai4gamesWeek08Lecture from './ai4games/chapters/week08/lecture.md';
import ai4gamesWeek09Lecture from './ai4games/chapters/week09/lecture.md';
import ai4gamesWeek10Lecture from './ai4games/chapters/week10/lecture.md';
import ai4gamesWeek11Assignment from './ai4games/chapters/week11/assignment.md';
import ai4gamesWeek11Board from './ai4games/chapters/week11/board.md';
import ai4gamesWeek12Lecture from './ai4games/chapters/week12/lecture.md';
import ai4gamesWeek13Lecture from './ai4games/chapters/week13/lecture.md';
import ai4gamesSyllabus from './ai4games/syllabus.md';

// Portfolio Course Imports
import portfolioWeek01Assignment01 from './portfolio/chapters/week01/assignment01.md';
import portfolioWeek01Assignment02 from './portfolio/chapters/week01/assignment02.md';
import portfolioWeek01Lecture01 from './portfolio/chapters/week01/lecture01.md';
import portfolioWeek02Assignment03 from './portfolio/chapters/week02/assignment03.md';
import portfolioWeek02Assignment04 from './portfolio/chapters/week02/assignment04.md';
import portfolioWeek02Lecture02 from './portfolio/chapters/week02/lecture02.md';
import portfolioWeek03Assignment05 from './portfolio/chapters/week03/assignment05.md';
import portfolioWeek03Lecture from './portfolio/chapters/week03/lecture.md';
import portfolioWeek04Assignment06 from './portfolio/chapters/week04/assignment06.md';
import portfolioWeek04Lecture from './portfolio/chapters/week04/lecture.md';
import portfolioWeek05Activity from './portfolio/chapters/week05/activity.md';
import portfolioWeek05Assignment07 from './portfolio/chapters/week05/assignment07.md';
import portfolioWeek05Lecture from './portfolio/chapters/week05/lecture.md';
import portfolioWeek06Assignment from './portfolio/chapters/week06/assignment.md';
import portfolioWeek06Lecture from './portfolio/chapters/week06/lecture.md';
import portfolioWeek07Assignment from './portfolio/chapters/week07/assignment.md';
import portfolioWeek07Lecture from './portfolio/chapters/week07/lecture.md';
import portfolioWeek08Activity from './portfolio/chapters/week08/activity.md';
import portfolioWeek08Assignment from './portfolio/chapters/week08/assignment.md';
import portfolioWeek08Lecture from './portfolio/chapters/week08/lecture.md';
import portfolioWeek09Assignment from './portfolio/chapters/week09/assignment.md';
import portfolioWeek09Lecture from './portfolio/chapters/week09/lecture.md';
import portfolioWeek10Assignment from './portfolio/chapters/week10/assignment.md';
import portfolioWeek11Activity from './portfolio/chapters/week11/activity.md';
import portfolioWeek11Lecture from './portfolio/chapters/week11/lecture.md';
import portfolioSyllabus from './portfolio/syllabus.md';

// Mock user for the creator
const mockUser = {
    id: '1',
    name: 'Game Guild Instructor',
    email: 'instructor@gameguild.com',
    isActive: true,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Mock Python Program
export const pythonProgram: Program = {
    id: 'python-program-1',
    title: 'Python Programming',
    description: 'Students will learn the history and basics of computing as well as the fundamentals of Python programming. General topics include: the history of computing, number systems, Boolean logic, algorithm design and implementation, and modern computer organization.',
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
    productPrograms: [],
    certificates: [],
    feedbackSubmissions: [],
    programRatings: [],
    programWishlists: [],
};

// Mock Python Product (free course)
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
    creator: mockUser,
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

// Mock Product-Program relationship
export const pythonProductProgram: ProductProgram = {
    id: 'python-product-program-1',
    productId: pythonProduct.id!,
    product: pythonProduct,
    programId: pythonProgram.id!,
    program: pythonProgram,
    sortOrder: 1,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// AI4Games Program
export const ai4gamesProgram: Program = {
    id: 'ai4games-program-1',
    title: 'AI for Games',
    description: 'Learn artificial intelligence techniques specifically designed for game development, including pathfinding, decision-making, and procedural content generation.',
    slug: 'ai4games',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff?text=AI+for+Games',
    videoShowcaseUrl: null,
    estimatedHours: 60,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 1, // Game Development
    difficulty: 1, // Intermediate
    visibility: 0, // Public
    status: 1, // Published
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    programContents: [],
    programUsers: [],
    productPrograms: [],
    certificates: [],
    feedbackSubmissions: [],
    programRatings: [],
    programWishlists: [],
};

// Portfolio Program
export const portfolioProgram: Program = {
    id: 'portfolio-program-1',
    title: 'Portfolio Development',
    description: 'Build a professional portfolio to showcase your skills and projects to potential employers and clients.',
    slug: 'portfolio',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff?text=Portfolio+Development',
    videoShowcaseUrl: null,
    estimatedHours: 30,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 2, // Career Development
    difficulty: 0, // Beginner
    visibility: 0, // Public
    status: 1, // Published
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    programContents: [],
    programUsers: [],
    productPrograms: [],
    certificates: [],
    feedbackSubmissions: [],
    programRatings: [],
    programWishlists: [],
};

// AI4Games Product
export const ai4gamesProduct: Product = {
    id: 'ai4games-product-1',
    title: 'AI for Games Course',
    name: 'AI for Games',
    description: 'Master AI techniques for game development',
    shortDescription: 'Learn pathfinding, decision-making, and procedural content generation',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff?text=AI+for+Games',
    type: 0, // Course
    isBundle: false,
    creatorId: '1',
    creator: mockUser,
    bundleItems: null,
    referralCommissionPercentage: 0,
    maxAffiliateDiscount: 0,
    affiliateCommissionPercentage: 0,
    visibility: 0, // Public
    status: 1, // Published
    slug: 'ai4games',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

// Portfolio Product
export const portfolioProduct: Product = {
    id: 'portfolio-product-1',
    title: 'Portfolio Development Course',
    name: 'Portfolio Development',
    description: 'Build a professional portfolio',
    shortDescription: 'Showcase your skills and projects effectively',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff?text=Portfolio+Development',
    type: 0, // Course
    isBundle: false,
    creatorId: '1',
    creator: mockUser,
    bundleItems: null,
    referralCommissionPercentage: 0,
    maxAffiliateDiscount: 0,
    affiliateCommissionPercentage: 0,
    visibility: 0, // Public
    status: 1, // Published
    slug: 'portfolio',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

// Product-Program relationships
export const ai4gamesProductProgram: ProductProgram = {
    id: 'ai4games-product-program-1',
    productId: ai4gamesProduct.id!,
    product: ai4gamesProduct,
    programId: ai4gamesProgram.id!,
    program: ai4gamesProgram,
    sortOrder: 2,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioProductProgram: ProductProgram = {
    id: 'portfolio-product-program-1',
    productId: portfolioProduct.id!,
    product: portfolioProduct,
    programId: portfolioProgram.id!,
    program: portfolioProgram,
    sortOrder: 3,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Python Course Content
export const pythonSyllabusContent: ProgramContent = {
    id: 'python-syllabus',
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Course Syllabus and Introduction',
    description: 'Course overview, objectives, and weekly schedule',
    type: 0, // Page
    body: pythonSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 01: Introduction to Python',
    description: 'Introduction to algorithms, problem-solving, and Python basics',
    type: 0, // Page
    body: week01Lecture,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 02: Python Basics',
    description: 'Introduction to Python programming basics',
    type: 0, // Page
    body: week02Lecture,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 03: Functions and Math',
    description: 'Functions and math in Python programming',
    type: 0, // Page
    body: week03Lecture,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 04: Python Conditionals and Loops',
    description: 'Flow control in Python programming',
    type: 0, // Page
    body: week04Lecture,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 05: Lists and Data Structures',
    description: 'Lists, tuples, and string manipulation',
    type: 0, // Page
    body: week05Lists,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Exercise: Two Sum',
    description: 'Practice exercise for lists and algorithms',
    type: 2, // Assignment
    body: week05Exercise01,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Exercise: Search Insert Position',
    description: 'Practice exercise for list operations',
    type: 2, // Assignment
    body: week05Exercise02,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 06: Advanced Loops',
    description: 'Advanced looping techniques and patterns',
    type: 0, // Page
    body: week06Lecture,
    sortOrder: 9,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 07: Nested Loops',
    description: 'Nested loops and advanced loop control',
    type: 0, // Page
    body: week07Lecture,
    sortOrder: 10,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 10: Dictionaries and Sets',
    description: 'Advanced data structures in Python',
    type: 0, // Page
    body: week10Dictionaries,
    sortOrder: 11,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 10: Sets and Set Operations',
    description: 'Set operations and advanced data structures',
    type: 0, // Page
    body: week10Sets,
    sortOrder: 12,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 11: Files and Exceptions',
    description: 'File handling and exception management',
    type: 0, // Page
    body: week11Lecture,
    sortOrder: 13,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 12: APIs and Web Services',
    description: 'Working with APIs and web services',
    type: 0, // Page
    body: week12Lecture,
    sortOrder: 14,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 12: Local LLMs with Ollama',
    description: 'Working with local language models',
    type: 0, // Page
    body: week12LocalLlm,
    sortOrder: 15,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Additional Python Week 4 Content
export const pythonWeek04BooleanOperationsContent: ProgramContent = {
    id: 'python-week04-boolean-operations',
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 04: Boolean Operations',
    description: 'Boolean logic and operations in Python',
    type: 0, // Page
    body: week04BooleanOperations,
    sortOrder: 16,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 04: Switch Statements',
    description: 'Switch statements and control flow',
    type: 0, // Page
    body: week04Switch,
    sortOrder: 17,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Additional Python Week 5 Content
export const pythonWeek05SetsContent: ProgramContent = {
    id: 'python-week05-sets',
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 05: Sets',
    description: 'Set data structure and operations',
    type: 0, // Page
    body: week05Sets,
    sortOrder: 18,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
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
    programId: pythonProgram.id!,
    parentId: undefined,
    title: 'Week 05: Tuples',
    description: 'Tuple data structure and immutability',
    type: 0, // Page
    body: week05Tuples,
    sortOrder: 19,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: pythonProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// AI4Games Course Content
export const ai4gamesSyllabusContent: ProgramContent = {
    id: 'ai4games-syllabus',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'AI for Games course overview and objectives',
    type: 0, // Page
    body: ai4gamesSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek01Content: ProgramContent = {
    id: 'ai4games-week01',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 01: Random and Noise',
    description: 'Randomness in games and noise functions',
    type: 0, // Page
    body: ai4gamesWeek01Lecture,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek01ReadingsContent: ProgramContent = {
    id: 'ai4games-week01-readings',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 01: Readings',
    description: 'Additional readings and resources',
    type: 0, // Page
    body: ai4gamesWeek01Readings,
    sortOrder: 3,
    isRequired: false,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek02Content: ProgramContent = {
    id: 'ai4games-week02',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 02: Wave Function Collapse',
    description: 'Wave Function Collapse algorithm for procedural generation',
    type: 0, // Page
    body: ai4gamesWeek02Lecture,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek02PcgContent: ProgramContent = {
    id: 'ai4games-week02-pcg',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 02: Procedural Content Generation',
    description: 'Procedural Content Generation techniques',
    type: 0, // Page
    body: ai4gamesWeek02Pcg,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek03Content: ProgramContent = {
    id: 'ai4games-week03',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 03: AI Engines',
    description: 'AI engines and pathfinding algorithms',
    type: 0, // Page
    body: ai4gamesWeek03Lecture,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek03AstarContent: ProgramContent = {
    id: 'ai4games-week03-astar',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 03: A* Pathfinding',
    description: 'A* pathfinding algorithm implementation',
    type: 0, // Page
    body: ai4gamesWeek03Astar,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek04Content: ProgramContent = {
    id: 'ai4games-week04',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 04: GOAP',
    description: 'Goal-Oriented Action Planning',
    type: 0, // Page
    body: ai4gamesWeek04Lecture,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek04AssignmentContent: ProgramContent = {
    id: 'ai4games-week04-assignment',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 04: GOAP Assignment',
    description: 'GOAP implementation assignment',
    type: 2, // Assignment
    body: ai4gamesWeek04Assignment,
    sortOrder: 9,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek05Content: ProgramContent = {
    id: 'ai4games-week05',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 05: Dynamic GOAP',
    description: 'Dynamic GOAP implementation in C++',
    type: 0, // Page
    body: ai4gamesWeek05Lecture,
    sortOrder: 10,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek05CsharpContent: ProgramContent = {
    id: 'ai4games-week05-csharp',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 05: Dynamic GOAP in C#',
    description: 'Dynamic GOAP implementation in C#',
    type: 0, // Page
    body: ai4gamesWeek05Lecture2,
    sortOrder: 11,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Additional AI4Games Content
export const ai4gamesWeek06Content: ProgramContent = {
    id: 'ai4games-week06',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 06: Advanced AI Techniques',
    description: 'Advanced AI techniques for games',
    type: 0, // Page
    body: ai4gamesWeek06Lecture,
    sortOrder: 12,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek07Content: ProgramContent = {
    id: 'ai4games-week07',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 07: AI Behavior Trees',
    description: 'Behavior trees and decision making',
    type: 0, // Page
    body: ai4gamesWeek07Lecture,
    sortOrder: 13,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek08Content: ProgramContent = {
    id: 'ai4games-week08',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 08: Machine Learning in Games',
    description: 'Machine learning applications in game development',
    type: 0, // Page
    body: ai4gamesWeek08Lecture,
    sortOrder: 14,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek09Content: ProgramContent = {
    id: 'ai4games-week09',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 09: Neural Networks for Games',
    description: 'Neural networks and deep learning in games',
    type: 0, // Page
    body: ai4gamesWeek09Lecture,
    sortOrder: 15,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek10Content: ProgramContent = {
    id: 'ai4games-week10',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 10: Reinforcement Learning',
    description: 'Reinforcement learning in game AI',
    type: 0, // Page
    body: ai4gamesWeek10Lecture,
    sortOrder: 16,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek11AssignmentContent: ProgramContent = {
    id: 'ai4games-week11-assignment',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 11: AI Assignment',
    description: 'Advanced AI assignment',
    type: 2, // Assignment
    body: ai4gamesWeek11Assignment,
    sortOrder: 17,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek11BoardContent: ProgramContent = {
    id: 'ai4games-week11-board',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 11: Game Board AI',
    description: 'AI for board games',
    type: 0, // Page
    body: ai4gamesWeek11Board,
    sortOrder: 18,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek12Content: ProgramContent = {
    id: 'ai4games-week12',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 12: AI Optimization',
    description: 'Optimizing AI performance in games',
    type: 0, // Page
    body: ai4gamesWeek12Lecture,
    sortOrder: 19,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek13Content: ProgramContent = {
    id: 'ai4games-week13',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 13: Final AI Project',
    description: 'Final AI project and review',
    type: 0, // Page
    body: ai4gamesWeek13Lecture,
    sortOrder: 20,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Portfolio Course Content
export const portfolioSyllabusContent: ProgramContent = {
    id: 'portfolio-syllabus',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'Portfolio Development course overview',
    type: 0, // Page
    body: portfolioSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek01Lecture01Content: ProgramContent = {
    id: 'portfolio-week01-lecture01',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 01: Introduction to Portfolio Development',
    description: 'Introduction to portfolio development concepts',
    type: 0, // Page
    body: portfolioWeek01Lecture01,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek01Assignment01Content: ProgramContent = {
    id: 'portfolio-week01-assignment01',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 01: Assignment 01',
    description: 'First portfolio assignment',
    type: 2, // Assignment
    body: portfolioWeek01Assignment01,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek01Assignment02Content: ProgramContent = {
    id: 'portfolio-week01-assignment02',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 01: Assignment 02',
    description: 'Second portfolio assignment',
    type: 2, // Assignment
    body: portfolioWeek01Assignment02,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek02Lecture02Content: ProgramContent = {
    id: 'portfolio-week02-lecture02',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 02: Portfolio Planning',
    description: 'Planning your portfolio structure',
    type: 0, // Page
    body: portfolioWeek02Lecture02,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek02Assignment03Content: ProgramContent = {
    id: 'portfolio-week02-assignment03',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 02: Assignment 03',
    description: 'Portfolio planning assignment',
    type: 2, // Assignment
    body: portfolioWeek02Assignment03,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek02Assignment04Content: ProgramContent = {
    id: 'portfolio-week02-assignment04',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 02: Assignment 04',
    description: 'Advanced portfolio assignment',
    type: 2, // Assignment
    body: portfolioWeek02Assignment04,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek03LectureContent: ProgramContent = {
    id: 'portfolio-week03-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 03: Content Creation',
    description: 'Creating compelling portfolio content',
    type: 0, // Page
    body: portfolioWeek03Lecture,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek03Assignment05Content: ProgramContent = {
    id: 'portfolio-week03-assignment05',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 03: Assignment 05',
    description: 'Content creation assignment',
    type: 2, // Assignment
    body: portfolioWeek03Assignment05,
    sortOrder: 9,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek04LectureContent: ProgramContent = {
    id: 'portfolio-week04-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 04: Design Principles',
    description: 'Design principles for portfolios',
    type: 0, // Page
    body: portfolioWeek04Lecture,
    sortOrder: 10,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek04Assignment06Content: ProgramContent = {
    id: 'portfolio-week04-assignment06',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 04: Assignment 06',
    description: 'Design principles assignment',
    type: 2, // Assignment
    body: portfolioWeek04Assignment06,
    sortOrder: 11,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek05LectureContent: ProgramContent = {
    id: 'portfolio-week05-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 05: Technical Implementation',
    description: 'Technical implementation of portfolios',
    type: 0, // Page
    body: portfolioWeek05Lecture,
    sortOrder: 12,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek05Assignment07Content: ProgramContent = {
    id: 'portfolio-week05-assignment07',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 05: Assignment 07',
    description: 'Technical implementation assignment',
    type: 2, // Assignment
    body: portfolioWeek05Assignment07,
    sortOrder: 13,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 200,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek05ActivityContent: ProgramContent = {
    id: 'portfolio-week05-activity',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 05: Hands-on Activity',
    description: 'Hands-on portfolio building activity',
    type: 1, // Activity
    body: portfolioWeek05Activity,
    sortOrder: 14,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek06LectureContent: ProgramContent = {
    id: 'portfolio-week06-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 06: Deployment and Hosting',
    description: 'Deploying and hosting your portfolio',
    type: 0, // Page
    body: portfolioWeek06Lecture,
    sortOrder: 15,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek06AssignmentContent: ProgramContent = {
    id: 'portfolio-week06-assignment',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 06: Deployment Assignment',
    description: 'Deploy your portfolio assignment',
    type: 2, // Assignment
    body: portfolioWeek06Assignment,
    sortOrder: 16,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek07LectureContent: ProgramContent = {
    id: 'portfolio-week07-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 07: SEO and Analytics',
    description: 'SEO and analytics for portfolios',
    type: 0, // Page
    body: portfolioWeek07Lecture,
    sortOrder: 17,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek07AssignmentContent: ProgramContent = {
    id: 'portfolio-week07-assignment',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 07: SEO Assignment',
    description: 'SEO optimization assignment',
    type: 2, // Assignment
    body: portfolioWeek07Assignment,
    sortOrder: 18,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek08LectureContent: ProgramContent = {
    id: 'portfolio-week08-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 08: Performance Optimization',
    description: 'Performance optimization for portfolios',
    type: 0, // Page
    body: portfolioWeek08Lecture,
    sortOrder: 19,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek08AssignmentContent: ProgramContent = {
    id: 'portfolio-week08-assignment',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 08: Performance Assignment',
    description: 'Performance optimization assignment',
    type: 2, // Assignment
    body: portfolioWeek08Assignment,
    sortOrder: 20,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek08ActivityContent: ProgramContent = {
    id: 'portfolio-week08-activity',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 08: Performance Activity',
    description: 'Performance optimization activity',
    type: 1, // Activity
    body: portfolioWeek08Activity,
    sortOrder: 21,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek09LectureContent: ProgramContent = {
    id: 'portfolio-week09-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 09: Advanced Features',
    description: 'Advanced portfolio features',
    type: 0, // Page
    body: portfolioWeek09Lecture,
    sortOrder: 22,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek09AssignmentContent: ProgramContent = {
    id: 'portfolio-week09-assignment',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 09: Advanced Features Assignment',
    description: 'Advanced features assignment',
    type: 2, // Assignment
    body: portfolioWeek09Assignment,
    sortOrder: 23,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek10AssignmentContent: ProgramContent = {
    id: 'portfolio-week10-assignment',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 10: Final Assignment',
    description: 'Final portfolio assignment',
    type: 2, // Assignment
    body: portfolioWeek10Assignment,
    sortOrder: 24,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 240,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek11LectureContent: ProgramContent = {
    id: 'portfolio-week11-lecture',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 11: Portfolio Review',
    description: 'Portfolio review and feedback',
    type: 0, // Page
    body: portfolioWeek11Lecture,
    sortOrder: 25,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const portfolioWeek11ActivityContent: ProgramContent = {
    id: 'portfolio-week11-activity',
    programId: portfolioProgram.id!,
    parentId: undefined,
    title: 'Week 11: Final Activity',
    description: 'Final portfolio activity',
    type: 1, // Activity
    body: portfolioWeek11Activity,
    sortOrder: 26,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: portfolioProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Update relationships
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

ai4gamesProgram.programContents = [
    ai4gamesSyllabusContent,
    ai4gamesWeek01Content,
    ai4gamesWeek01ReadingsContent,
    ai4gamesWeek02Content,
    ai4gamesWeek02PcgContent,
    ai4gamesWeek03Content,
    ai4gamesWeek03AstarContent,
    ai4gamesWeek04Content,
    ai4gamesWeek04AssignmentContent,
    ai4gamesWeek05Content,
    ai4gamesWeek05CsharpContent,
    ai4gamesWeek06Content,
    ai4gamesWeek07Content,
    ai4gamesWeek08Content,
    ai4gamesWeek09Content,
    ai4gamesWeek10Content,
    ai4gamesWeek11AssignmentContent,
    ai4gamesWeek11BoardContent,
    ai4gamesWeek12Content,
    ai4gamesWeek13Content,
];

portfolioProgram.programContents = [
    portfolioSyllabusContent,
    portfolioWeek01Lecture01Content,
    portfolioWeek01Assignment01Content,
    portfolioWeek01Assignment02Content,
    portfolioWeek02Lecture02Content,
    portfolioWeek02Assignment03Content,
    portfolioWeek02Assignment04Content,
    portfolioWeek03LectureContent,
    portfolioWeek03Assignment05Content,
    portfolioWeek04LectureContent,
    portfolioWeek04Assignment06Content,
    portfolioWeek05LectureContent,
    portfolioWeek05Assignment07Content,
    portfolioWeek05ActivityContent,
    portfolioWeek06LectureContent,
    portfolioWeek06AssignmentContent,
    portfolioWeek07LectureContent,
    portfolioWeek07AssignmentContent,
    portfolioWeek08LectureContent,
    portfolioWeek08AssignmentContent,
    portfolioWeek08ActivityContent,
    portfolioWeek09LectureContent,
    portfolioWeek09AssignmentContent,
    portfolioWeek10AssignmentContent,
    portfolioWeek11LectureContent,
    portfolioWeek11ActivityContent,
];

pythonProduct.productPrograms = [pythonProductProgram];
ai4gamesProduct.productPrograms = [ai4gamesProductProgram];
portfolioProduct.productPrograms = [portfolioProductProgram];

// Export all mock data
export const mockPrograms: Program[] = [pythonProgram, ai4gamesProgram, portfolioProgram];
export const mockProducts: Product[] = [pythonProduct, ai4gamesProduct, portfolioProduct];
export const mockProductPrograms: ProductProgram[] = [pythonProductProgram, ai4gamesProductProgram, portfolioProductProgram];
export const mockProgramContents: ProgramContent[] = [
    // Python content
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
    // AI4Games content
    ai4gamesSyllabusContent,
    ai4gamesWeek01Content,
    ai4gamesWeek01ReadingsContent,
    ai4gamesWeek02Content,
    ai4gamesWeek02PcgContent,
    ai4gamesWeek03Content,
    ai4gamesWeek03AstarContent,
    ai4gamesWeek04Content,
    ai4gamesWeek04AssignmentContent,
    ai4gamesWeek05Content,
    ai4gamesWeek05CsharpContent,
    ai4gamesWeek06Content,
    ai4gamesWeek07Content,
    ai4gamesWeek08Content,
    ai4gamesWeek09Content,
    ai4gamesWeek10Content,
    ai4gamesWeek11AssignmentContent,
    ai4gamesWeek11BoardContent,
    ai4gamesWeek12Content,
    ai4gamesWeek13Content,
    // Portfolio content
    portfolioSyllabusContent,
    portfolioWeek01Lecture01Content,
    portfolioWeek01Assignment01Content,
    portfolioWeek01Assignment02Content,
    portfolioWeek02Lecture02Content,
    portfolioWeek02Assignment03Content,
    portfolioWeek02Assignment04Content,
    portfolioWeek03LectureContent,
    portfolioWeek03Assignment05Content,
    portfolioWeek04LectureContent,
    portfolioWeek04Assignment06Content,
    portfolioWeek05LectureContent,
    portfolioWeek05Assignment07Content,
    portfolioWeek05ActivityContent,
    portfolioWeek06LectureContent,
    portfolioWeek06AssignmentContent,
    portfolioWeek07LectureContent,
    portfolioWeek07AssignmentContent,
    portfolioWeek08LectureContent,
    portfolioWeek08AssignmentContent,
    portfolioWeek08ActivityContent,
    portfolioWeek09LectureContent,
    portfolioWeek09AssignmentContent,
    portfolioWeek10AssignmentContent,
    portfolioWeek11LectureContent,
    portfolioWeek11ActivityContent,
];

// Helper function to get program by slug
export function getProgramBySlug(slug: string): Program | null {
    return mockPrograms.find(program => program.slug === slug) || null;
}

// Helper function to get product by slug
export function getProductBySlug(slug: string): Product | null {
    return mockProducts.find(product => product.slug === slug) || null;
}

// Helper function to get all programs
export function getAllPrograms(): Program[] {
    return mockPrograms;
}

// Helper function to get all products
export function getAllProducts(): Product[] {
    return mockProducts;
} 