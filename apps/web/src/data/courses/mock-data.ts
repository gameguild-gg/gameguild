import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Intro to AI courses
import ai4gamesSyllabus from './ai4games/syllabus.md';
import ai4gamesSetup from './ai4games/week01/setup.md';
import ai4gamesSubmissions from './ai4games/week01/submissions.md';
import ai4gamesFlocking from './ai4games/week02/flocking.md';
import ai4gamesLife from './ai4games/week03/life.md';
import ai4gamesRng from './ai4games/week04/rng.md';
import ai4gamesMaze from './ai4games/week05/maze.md';
import ai4gamesPathfinding from './ai4games/week06/pathfinding.md';
import ai4gamesCatchTheCat from './ai4games/week07/catchthecat.md';
import ai4gamesSpatialQuantization from './ai4games/week08/spatial-quantization.md';
import ai4gamesPathfindingContinuous from './ai4games/week09/pathfinding-continuous.md';
import ai4gamesNoise from './ai4games/week10/noise.md';
import ai4gamesFinalProject from './ai4games/week11/final-project.md';

// Advanced AI Course Imports
import ai4games2ExtraLecture from './ai4games2/chapters/extras/llms.md';
import ai4games2Week01Lecture from './ai4games2/chapters/week01/lecture.md';
import ai4games2Week01Readings from './ai4games2/chapters/week01/readings.md';
import ai4games2Week02Lecture from './ai4games2/chapters/week02/lecture.md';
import ai4games2Week02Pcg from './ai4games2/chapters/week02/pcg.md';
import ai4games2Week03Astar from './ai4games2/chapters/week03/a-star.md';
import ai4games2Week03Lecture from './ai4games2/chapters/week03/lecture.md';
import ai4games2Week04Assignment from './ai4games2/chapters/week04/assignment.md';
import ai4games2Week04Lecture from './ai4games2/chapters/week04/lecture.md';
import ai4games2Week05Lecture from './ai4games2/chapters/week05/lecture.md';
import ai4games2Week05Lecture2 from './ai4games2/chapters/week05/lecture2.md';
import ai4games2Week06Lecture from './ai4games2/chapters/week06/lecture.md';
import ai4games2Week07Lecture from './ai4games2/chapters/week07/lecture.md';
import ai4games2Week08Lecture from './ai4games2/chapters/week08/lecture.md';
import ai4games2Week09Lecture from './ai4games2/chapters/week09/lecture.md';
import ai4games2Week10Lecture from './ai4games2/chapters/week10/lecture.md';
import ai4games2Week11Assignment from './ai4games2/chapters/week11/assignment.md';
import ai4games2Week11Board from './ai4games2/chapters/week11/board.md';
import ai4games2Week12Lecture from './ai4games2/chapters/week12/lecture.md';
import ai4games2Week13Lecture from './ai4games2/chapters/week13/lecture.md';
import ai4games2Syllabus from './ai4games2/syllabus.md';

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

// DSA Course Imports
import dsaIntroduction from './dsa/01-introduction/introduction.md';
import dsaAnalysis from './dsa/02-analysis/README.md';
import dsaDynamicData from './dsa/03-dynamic-data/README.md';
import dsaSorting from './dsa/04-sorting/README.md';
import dsaDivideAndConquer from './dsa/05-divide-and-conquer/README.md';
import dsaHashtables from './dsa/06-hashtables/README.md';
import dsaMidterm from './dsa/07-midterm/README.md';
import dsaStackAndQueue from './dsa/08-stack-and-queue/README.md';
import dsaBreak from './dsa/09-break/README.md';
import dsaGraphs from './dsa/10-graphs/README.md';
import dsaDijkstra from './dsa/11-dijkstra/README.md';
import dsaMst from './dsa/12-mst/README.md';
import dsaBst from './dsa/13-bst/README.md';
import dsaHeap from './dsa/14-heap/README.md';
import dsaProject from './dsa/15-project/README.md';
import dsaFinals from './dsa/16-finals/README.md';
import dsaSyllabus from './dsa/syllabus.md';

// Intro to Game Programming Course Imports
import intro2gproSyllabus from './intro2gpro/syllabus.md';

// Introduction to Game Programming syllabus content
export const intro2gproSyllabusContent: ProgramContent = {
    id: 'intro2gpro-syllabus',
    programId: 'intro2gpro-program-1',
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'Introduction to Game Programming course syllabus',
    type: 0, // Page
    body: intro2gproSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: {} as Program, // Will be set after program creation
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'syllabus',
};

// Mock user for the creator
const mockUser = {
    id: '1',
    name: 'Alexandre Tolstenko',
    username: 'tolstenko',
    email: 'atolstenko@champlain.edu',
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
    description: 'Learn artificial intelligence techniques for game development, including behavioral agents, pathfinding algorithms, procedural content generation, and noise functions.',
    slug: 'ai4games',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=AI+for+Games',
    videoShowcaseUrl: null,
    estimatedHours: 48,
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

// AI4Games 2 Program
export const ai4games2Program: Program = {
    id: 'ai4games-program-2',
    title: 'Advanced Game AI',
    description: 'Learn advanced artificial intelligence techniques specifically designed for game development, including pathfinding, decision-making, and procedural content generation.',
    slug: 'ai4games2',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Advanced+Game+AI',
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
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Portfolio+Development',
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

// DSA Program
export const dsaProgram: Program = {
    id: 'dsa-program-1',
    title: 'Data Structures and Algorithms',
    description: 'Students compare and contrast a variety of data structures. Students compare algorithms for tasks such as searching and sorting, while articulating efficiency in terms of time complexity.',
    slug: 'dsa',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Data+Structures+%26+Algorithms',
    videoShowcaseUrl: null,
    estimatedHours: 60,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 0, // Programming
    difficulty: 2, // Advanced
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

// Intro to Game Programming Program
export const intro2gproProgram: Program = {
    id: 'intro2gpro-program-1',
    title: 'Introduction to Game Programming',
    description: 'Students will be introduced to and familiarized with their roles as Game Programmers. The course explores the various disciplines and vocations within game programming, provides an overview of the skills that make a game programmer successful, and presents both industry and academic contexts for their duties.',
    slug: 'intro2gpro',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Intro+to+Game+Programming',
    videoShowcaseUrl: null,
    estimatedHours: 45,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 1, // Game Development
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
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=AI+for+Games',
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

// AI4Games2 Product
export const ai4games2Product: Product = {
    id: 'ai4games2-product-1',
    title: 'Advanced Game AI Course',
    name: 'Advanced Game AI',
    description: 'Master advanced AI techniques for game development',
    shortDescription: 'Learn advanced pathfinding, decision-making, and procedural content generation',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Advanced+Game+AI',
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
    slug: 'ai4games2',
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
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Portfolio+Development',
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

// DSA Product
export const dsaProduct: Product = {
    id: 'dsa-product-1',
    title: 'Data Structures and Algorithms Course',
    name: 'Data Structures and Algorithms',
    description: 'Master data structures and algorithms with comprehensive analysis of time complexity',
    shortDescription: 'Learn essential data structures and algorithms for efficient programming',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Data+Structures+%26+Algorithms',
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
    slug: 'dsa',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

// Intro to Game Programming Product
export const intro2gproProduct: Product = {
    id: 'intro2gpro-product-1',
    title: 'Introduction to Game Programming Course',
    name: 'Introduction to Game Programming',
    description: 'Learn the fundamentals of game programming and explore various disciplines within game development',
    shortDescription: 'Get introduced to game programming roles, tools, and industry contexts',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Intro+to+Game+Programming',
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
    slug: 'intro2gpro',
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

export const ai4games2ProductProgram: ProductProgram = {
    id: 'ai4games-product-program-2',
    productId: ai4games2Product.id!,
    product: ai4games2Product,
    programId: ai4games2Program.id!,
    program: ai4games2Program,
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

export const dsaProductProgram: ProductProgram = {
    id: 'dsa-product-program-1',
    productId: dsaProduct.id!,
    product: dsaProduct,
    programId: dsaProgram.id!,
    program: dsaProgram,
    sortOrder: 4,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const intro2gproProductProgram: ProductProgram = {
    id: 'intro2gpro-product-program-1',
    productId: intro2gproProduct.id!,
    product: intro2gproProduct,
    programId: intro2gproProgram.id!,
    program: intro2gproProgram,
    sortOrder: 5,
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
    slug: 'python-syllabus',
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
    slug: 'introduction-to-python',
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
    slug: 'python-basics',
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
    slug: 'functions-and-math',
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
    slug: 'conditionals-and-loops',
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
    slug: 'lists-and-data-structures',
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
    slug: 'exercise-two-sum',
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
    slug: 'exercise-search-insert-position',
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
    slug: 'advanced-loops',
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
    slug: 'nested-loops',
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
    slug: 'dictionaries-and-sets',
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
    slug: 'sets-and-operations',
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
    slug: 'files-and-exceptions',
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
    slug: 'apis-and-web-services',
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
    slug: 'local-llms-ollama',
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
    slug: 'boolean-operations',
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
    slug: 'switch-statements',
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
    slug: 'sets-data-structure',
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
    slug: 'tuples-immutability',
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
    slug: 'syllabus',
};

export const ai4gamesSubmissionsContent: ProgramContent = {
    id: 'ai4games-submissions',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 01: Assignment Submissions',
    description: 'Guidelines for assignment submissions',
    type: 0, // Page
    body: ai4gamesSubmissions,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'submissions',
};

export const ai4gamesSetupContent: ProgramContent = {
    id: 'ai4games-setup',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 01: Development Environment Setup',
    description: 'Setting up the development environment for AI game programming',
    type: 0, // Page
    body: ai4gamesSetup,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'setup',
};

export const ai4gamesFlockingContent: ProgramContent = {
    id: 'ai4games-flocking',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 02: Flocking Behavior',
    description: 'Implementing flocking algorithms and behavioral agents',
    type: 0, // Page
    body: ai4gamesFlocking,
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
    slug: 'flocking',
};

export const ai4gamesLifeContent: ProgramContent = {
    id: 'ai4games-life',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 03: Game of Life',
    description: 'Implementing Conway\'s Game of Life and cellular automata',
    type: 0, // Page
    body: ai4gamesLife,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'life',
};

export const ai4gamesRngContent: ProgramContent = {
    id: 'ai4games-rng',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 04: Random Number Generation',
    description: 'Understanding and implementing random number generators',
    type: 0, // Page
    body: ai4gamesRng,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'rng',
};

export const ai4gamesMazeContent: ProgramContent = {
    id: 'ai4games-maze',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 05: Maze Generation',
    description: 'Algorithms for procedural maze generation',
    type: 0, // Page
    body: ai4gamesMaze,
    sortOrder: 9,
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
    slug: 'maze',
};

export const ai4gamesPathfindingContent: ProgramContent = {
    id: 'ai4games-pathfinding',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 06: Pathfinding Algorithms',
    description: 'Implementing A* and other pathfinding algorithms',
    type: 0, // Page
    body: ai4gamesPathfinding,
    sortOrder: 10,
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
    slug: 'pathfinding',
};

export const ai4gamesCatchTheCatContent: ProgramContent = {
    id: 'ai4games-catchthecat',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 07: Catch the Cat Game',
    description: 'Implementing AI for the Catch the Cat puzzle game',
    type: 0, // Page
    body: ai4gamesCatchTheCat,
    sortOrder: 11,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 105,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'catchthecat',
};

export const ai4gamesSpatialQuantizationContent: ProgramContent = {
    id: 'ai4games-spatial-quantization',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 08: Spatial Quantization',
    description: 'Spatial data structures and quantization techniques',
    type: 0, // Page
    body: ai4gamesSpatialQuantization,
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
    slug: 'spatial-quantization',
};

export const ai4gamesPathfindingContinuousContent: ProgramContent = {
    id: 'ai4games-pathfinding-continuous',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 09: Continuous Pathfinding',
    description: 'Pathfinding in continuous space environments',
    type: 0, // Page
    body: ai4gamesPathfindingContinuous,
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
    slug: 'pathfinding-continuous',
};

export const ai4gamesNoiseContent: ProgramContent = {
    id: 'ai4games-noise',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 10: Noise Functions',
    description: 'Perlin noise and procedural content generation',
    type: 0, // Page
    body: ai4gamesNoise,
    sortOrder: 14,
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
    slug: 'noise',
};

export const ai4gamesFinalProjectContent: ProgramContent = {
    id: 'ai4games-final-project',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 11: Final Project',
    description: 'Capstone project integrating AI techniques learned throughout the course',
    type: 2, // Assignment
    body: ai4gamesFinalProject,
    sortOrder: 15,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 300,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'final-project',
};

export const ai4games2SyllabusContent: ProgramContent = {
    id: 'ai4games-syllabus',
    programId: ai4games2Program.id!,
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'AI for Games course overview and objectives',
    type: 0, // Page
    body: ai4games2Syllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: ai4games2Program,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'ai4games-syllabus',
};

export const ai4games2Week01Content: ProgramContent = {
    id: 'ai4games-week01',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 01: Random and Noise',
    description: 'Randomness in games and noise functions',
    type: 0, // Page
    body: ai4games2Week01Lecture,
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
    slug: 'random-and-noise',
};

export const ai4games2Week01ReadingsContent: ProgramContent = {
    id: 'ai4games-week01-readings',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 01: Readings',
    description: 'Additional readings and resources',
    type: 0, // Page
    body: ai4games2Week01Readings,
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
    slug: 'random-noise-readings',
};

export const ai4games2Week02Content: ProgramContent = {
    id: 'ai4games-week02',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 02: Wave Function Collapse',
    description: 'Wave Function Collapse algorithm for procedural generation',
    type: 0, // Page
    body: ai4games2Week02Lecture,
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
    slug: 'wave-function-collapse',
};

export const ai4games2Week02PcgContent: ProgramContent = {
    id: 'ai4games-week02-pcg',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 02: Procedural Content Generation',
    description: 'Procedural Content Generation techniques',
    type: 0, // Page
    body: ai4games2Week02Pcg,
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
    slug: 'procedural-content-generation',
};

export const ai4games2Week03Content: ProgramContent = {
    id: 'ai4games-week03',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 03: AI Engines',
    description: 'AI engines and pathfinding algorithms',
    type: 0, // Page
    body: ai4games2Week03Lecture,
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
    slug: 'ai-engines-pathfinding',
};

export const ai4games2Week03AstarContent: ProgramContent = {
    id: 'ai4games-week03-astar',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 03: A* Pathfinding',
    description: 'A* pathfinding algorithm implementation',
    type: 0, // Page
    body: ai4games2Week03Astar,
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
    slug: 'astar-pathfinding',
};

export const ai4games2Week04Content: ProgramContent = {
    id: 'ai4games-week04',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 04: GOAP',
    description: 'Goal-Oriented Action Planning',
    type: 0, // Page
    body: ai4games2Week04Lecture,
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
    slug: 'goap-planning',
};

export const ai4games2Week04AssignmentContent: ProgramContent = {
    id: 'ai4games-week04-assignment',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 04: GOAP Assignment',
    description: 'GOAP implementation assignment',
    type: 2, // Assignment
    body: ai4games2Week04Assignment,
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
    slug: 'goap-assignment',
};

export const ai4games2Week05Content: ProgramContent = {
    id: 'ai4games-week05',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 05: Dynamic GOAP',
    description: 'Dynamic GOAP implementation in C++',
    type: 0, // Page
    body: ai4games2Week05Lecture,
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
    slug: 'dynamic-goap-cpp',
};

export const ai4games2Week05CsharpContent: ProgramContent = {
    id: 'ai4games-week05-csharp',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 05: Dynamic GOAP in C#',
    description: 'Dynamic GOAP implementation in C#',
    type: 0, // Page
    body: ai4games2Week05Lecture2,
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
    slug: 'dynamic-goap-csharp',
};

// Additional AI4Games Content
export const ai4games2Week06Content: ProgramContent = {
    id: 'ai4games-week06',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 06: Advanced AI Techniques',
    description: 'Advanced AI techniques for games',
    type: 0, // Page
    body: ai4games2Week06Lecture,
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
    slug: 'advanced-ai-techniques',
};

export const ai4games2Week07Content: ProgramContent = {
    id: 'ai4games-week07',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 07: AI Behavior Trees',
    description: 'Behavior trees and decision making',
    type: 0, // Page
    body: ai4games2Week07Lecture,
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
    slug: 'behavior-trees',
};

export const ai4games2Week08Content: ProgramContent = {
    id: 'ai4games-week08',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 08: Machine Learning in Games',
    description: 'Machine learning applications in game development',
    type: 0, // Page
    body: ai4games2Week08Lecture,
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
    slug: 'machine-learning-games',
};

export const ai4games2Week09Content: ProgramContent = {
    id: 'ai4games-week09',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 09: Neural Networks for Games',
    description: 'Neural networks and deep learning in games',
    type: 0, // Page
    body: ai4games2Week09Lecture,
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
    slug: 'neural-networks-games',
};

export const ai4games2Week10Content: ProgramContent = {
    id: 'ai4games-week10',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 10: Reinforcement Learning',
    description: 'Reinforcement learning in game AI',
    type: 0, // Page
    body: ai4games2Week10Lecture,
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
    slug: 'reinforcement-learning',
};

export const ai4games2Week11AssignmentContent: ProgramContent = {
    id: 'ai4games-week11-assignment',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 11: AI Assignment',
    description: 'Advanced AI assignment',
    type: 2, // Assignment
    body: ai4games2Week11Assignment,
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
    slug: 'advanced-ai-assignment',
};

export const ai4games2Week11BoardContent: ProgramContent = {
    id: 'ai4games-week11-board',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 11: Game Board AI',
    description: 'AI for board games',
    type: 0, // Page
    body: ai4games2Week11Board,
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
    slug: 'board-game-ai',
};

export const ai4games2Week12Content: ProgramContent = {
    id: 'ai4games-week12',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 12: AI Optimization',
    description: 'Optimizing AI performance in games',
    type: 0, // Page
    body: ai4games2Week12Lecture,
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
    slug: 'ai-optimization',
};

export const ai4games2Week13Content: ProgramContent = {
    id: 'ai4games-week13',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Week 13: Final AI Project',
    description: 'Final AI project and review',
    type: 0, // Page
    body: ai4games2Week13Lecture,
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
    slug: 'final-ai-project',
};

export const ai4games2ExtraContent: ProgramContent = {
    id: 'ai4games-extra',
    programId: ai4gamesProgram.id!,
    parentId: undefined,
    title: 'Extra Content LLMs History',
    description: 'History of LLMs',
    type: 0, // Page
    body: ai4games2ExtraLecture,
    sortOrder: 21,
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
    slug: 'llms-history',
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
    slug: 'portfolio-syllabus',
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
    slug: 'introduction-portfolio-development',
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
    slug: 'first-portfolio-assignment',
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
    slug: 'second-portfolio-assignment',
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
    slug: 'portfolio-planning',
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
    slug: 'portfolio-planning-assignment',
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
    slug: 'advanced-portfolio-assignment',
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
    slug: 'content-creation',
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
    slug: 'content-creation-assignment',
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
    slug: 'design-principles',
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
    slug: 'design-principles-assignment',
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
    slug: 'technical-implementation',
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
    slug: 'technical-implementation-assignment',
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
    slug: 'hands-on-activity',
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
    slug: 'deployment-hosting',
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
    slug: 'deployment-assignment',
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
    slug: 'seo-analytics',
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
    slug: 'seo-assignment',
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
    slug: 'performance-optimization',
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
    slug: 'performance-assignment',
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
    slug: 'performance-activity',
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
    slug: 'advanced-features',
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
    slug: 'advanced-features-assignment',
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
    slug: 'final-assignment',
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
    slug: 'portfolio-review',
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
    slug: 'final-activity',
};

// DSA Program Content
export const dsaSyllabusContent: ProgramContent = {
    id: 'dsa-syllabus',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Data Structures and Algorithms Syllabus',
    description: 'Course syllabus and overview',
    type: 0, // Page
    body: dsaSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'syllabus',
};

export const dsaIntroductionContent: ProgramContent = {
    id: 'dsa-introduction',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Introduction to Data Structures and Algorithms',
    description: 'Course introduction and overview',
    type: 0, // Page
    body: dsaIntroduction,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'introduction',
};

export const dsaAnalysisContent: ProgramContent = {
    id: 'dsa-analysis',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Algorithm Analysis',
    description: 'Big O notation and algorithm complexity analysis',
    type: 0, // Page
    body: dsaAnalysis,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'analysis',
};

export const dsaDynamicDataContent: ProgramContent = {
    id: 'dsa-dynamic-data',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Dynamic Data Structures',
    description: 'Arrays, linked lists, and dynamic memory allocation',
    type: 0, // Page
    body: dsaDynamicData,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'dynamic-data',
};

export const dsaSortingContent: ProgramContent = {
    id: 'dsa-sorting',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Sorting Algorithms',
    description: 'Bubble sort, selection sort, insertion sort, merge sort, quick sort',
    type: 0, // Page
    body: dsaSorting,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'sorting',
};

export const dsaDivideAndConquerContent: ProgramContent = {
    id: 'dsa-divide-and-conquer',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Divide and Conquer',
    description: 'Divide and conquer algorithms and techniques',
    type: 0, // Page
    body: dsaDivideAndConquer,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'divide-and-conquer',
};

export const dsaHashtablesContent: ProgramContent = {
    id: 'dsa-hashtables',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Hash Tables',
    description: 'Hash functions, collision resolution, and hash table implementation',
    type: 0, // Page
    body: dsaHashtables,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'hashtables',
};

export const dsaMidtermContent: ProgramContent = {
    id: 'dsa-midterm',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Midterm Exam',
    description: 'Midterm examination covering first half of course',
    type: 2, // Assignment
    body: dsaMidterm,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 1, // Points
    maxPoints: 100,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'midterm',
};

export const dsaStackAndQueueContent: ProgramContent = {
    id: 'dsa-stack-and-queue',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Stacks and Queues',
    description: 'Stack and queue data structures and their applications',
    type: 0, // Page
    body: dsaStackAndQueue,
    sortOrder: 9,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'stack-and-queue',
};

export const dsaBreakContent: ProgramContent = {
    id: 'dsa-break',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Spring Break',
    description: 'Spring break - no classes',
    type: 0, // Page
    body: dsaBreak,
    sortOrder: 10,
    isRequired: false,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 0,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'break',
};

export const dsaGraphsContent: ProgramContent = {
    id: 'dsa-graphs',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Graph Data Structures',
    description: 'Graph representation, traversal algorithms (BFS, DFS)',
    type: 0, // Page
    body: dsaGraphs,
    sortOrder: 11,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'graphs',
};

export const dsaDijkstraContent: ProgramContent = {
    id: 'dsa-dijkstra',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Dijkstra\'s Algorithm',
    description: 'Shortest path algorithms and Dijkstra\'s algorithm',
    type: 0, // Page
    body: dsaDijkstra,
    sortOrder: 12,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'dijkstra',
};

export const dsaMstContent: ProgramContent = {
    id: 'dsa-mst',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Minimum Spanning Trees',
    description: 'Kruskal\'s and Prim\'s algorithms for minimum spanning trees',
    type: 0, // Page
    body: dsaMst,
    sortOrder: 13,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'mst',
};

export const dsaBstContent: ProgramContent = {
    id: 'dsa-bst',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Binary Search Trees',
    description: 'Binary search tree implementation and operations',
    type: 0, // Page
    body: dsaBst,
    sortOrder: 14,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'bst',
};

export const dsaHeapContent: ProgramContent = {
    id: 'dsa-heap',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Heaps and Priority Queues',
    description: 'Heap data structure and priority queue implementation',
    type: 0, // Page
    body: dsaHeap,
    sortOrder: 15,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'heap',
};

export const dsaProjectContent: ProgramContent = {
    id: 'dsa-project',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Final Project',
    description: 'Comprehensive final project applying course concepts',
    type: 2, // Assignment
    body: dsaProject,
    sortOrder: 16,
    isRequired: true,
    gradingMethod: 1, // Points
    maxPoints: 200,
    estimatedMinutes: 480,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'project',
};

export const dsaFinalsContent: ProgramContent = {
    id: 'dsa-finals',
    programId: dsaProgram.id!,
    parentId: undefined,
    title: 'Final Exam',
    description: 'Comprehensive final examination',
    type: 2, // Assignment
    body: dsaFinals,
    sortOrder: 17,
    isRequired: true,
    gradingMethod: 1, // Points
    maxPoints: 150,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    slug: 'finals',
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
    ai4gamesSubmissionsContent,
    ai4gamesSetupContent,
    ai4gamesFlockingContent,
    ai4gamesLifeContent,
    ai4gamesRngContent,
    ai4gamesMazeContent,
    ai4gamesPathfindingContent,
    ai4gamesCatchTheCatContent,
    ai4gamesSpatialQuantizationContent,
    ai4gamesPathfindingContinuousContent,
    ai4gamesNoiseContent,
    ai4gamesFinalProjectContent
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

    // DSA content
    dsaSyllabusContent,
    dsaIntroductionContent,
    dsaAnalysisContent,
    dsaDynamicDataContent,
    dsaSortingContent,
    dsaDivideAndConquerContent,
    dsaHashtablesContent,
    dsaMidtermContent,
    dsaStackAndQueueContent,
    dsaBreakContent,
    dsaGraphsContent,
    dsaDijkstraContent,
    dsaMstContent,
    dsaBstContent,
    dsaHeapContent,
    dsaProjectContent,
    dsaFinalsContent,
];

ai4games2Program.programContents = [
    ai4games2SyllabusContent,
    ai4games2Week01Content,
    ai4games2Week01ReadingsContent,
    ai4games2Week02Content,
    ai4games2Week02PcgContent,
    ai4games2Week03Content,
    ai4games2Week03AstarContent,
    ai4games2Week04Content,
    ai4games2Week04AssignmentContent,
    ai4games2Week05Content,
    ai4games2Week05CsharpContent,
    ai4games2Week06Content,
    ai4games2Week07Content,
    ai4games2Week08Content,
    ai4games2Week09Content,
    ai4games2Week10Content,
    ai4games2Week11AssignmentContent,
    ai4games2Week11BoardContent,
    ai4games2Week12Content,
    ai4games2Week13Content,
    ai4games2ExtraContent,
];

dsaProgram.programContents = [
    dsaSyllabusContent,
    dsaIntroductionContent,
    dsaAnalysisContent,
    dsaDynamicDataContent,
    dsaSortingContent,
    dsaDivideAndConquerContent,
    dsaHashtablesContent,
    dsaMidtermContent,
    dsaStackAndQueueContent,
    dsaBreakContent,
    dsaGraphsContent,
    dsaDijkstraContent,
    dsaMstContent,
    dsaBstContent,
    dsaHeapContent,
    dsaProjectContent,
    dsaFinalsContent,
];

intro2gproProgram.programContents = [
    intro2gproSyllabusContent,
];

// Set program references
intro2gproSyllabusContent.program = intro2gproProgram;

pythonProduct.productPrograms = [pythonProductProgram];
ai4gamesProduct.productPrograms = [ai4gamesProductProgram];
ai4games2Product.productPrograms = [ai4games2ProductProgram];
portfolioProduct.productPrograms = [portfolioProductProgram];
dsaProduct.productPrograms = [dsaProductProgram];
intro2gproProduct.productPrograms = [intro2gproProductProgram];

// Export all mock data
export const mockPrograms: Program[] = [pythonProgram, ai4gamesProgram, ai4games2Program, portfolioProgram, dsaProgram, intro2gproProgram];
export const mockProducts: Product[] = [pythonProduct, ai4gamesProduct, ai4games2Product, portfolioProduct, dsaProduct, intro2gproProduct];
export const mockProductPrograms: ProductProgram[] = [pythonProductProgram, ai4gamesProductProgram, ai4games2ProductProgram, portfolioProductProgram, dsaProductProgram, intro2gproProductProgram];
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
    ai4gamesFerpaContent,
    ai4gamesSubmissionsContent,
    ai4gamesSetupContent,
    ai4gamesToolingContent,
    ai4gamesFlockingContent,
    ai4gamesLifeContent,
    ai4gamesRngContent,
    ai4gamesMazeContent,
    ai4gamesPathfindingContent,
    ai4gamesCatchTheCatContent,
    ai4gamesSpatialQuantizationContent,
    ai4gamesPathfindingContinuousContent,
    ai4gamesNoiseContent,
    ai4gamesFinalProjectContent,
    // AI4Games 2 content
    ai4games2SyllabusContent,
    ai4games2Week01Content,
    ai4games2Week01ReadingsContent,
    ai4games2Week02Content,
    ai4games2Week02PcgContent,
    ai4games2Week03Content,
    ai4games2Week03AstarContent,
    ai4games2Week04Content,
    ai4games2Week04AssignmentContent,
    ai4games2Week05Content,
    ai4games2Week05CsharpContent,
    ai4games2Week06Content,
    ai4games2Week07Content,
    ai4games2Week08Content,
    ai4games2Week09Content,
    ai4games2Week10Content,
    ai4games2Week11AssignmentContent,
    ai4games2Week11BoardContent,
    ai4games2Week12Content,
    ai4games2Week13Content,
    ai4games2ExtraContent,
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
    // Intro to Game Programming content
    intro2gproSyllabusContent,
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

export function getProgramContentBySlug(programSlug: string, contentPath: string[]): ProgramContent | null {
    const program = getProgramBySlug(programSlug);
    if (!program) {
        console.log('🔍 Program not found:', programSlug);
        return null;
    }

    console.log('🔍 Searching for content:', { programSlug, contentPath, programContentsCount: program.programContents?.length });
    console.log('🔍 Available slugs:', program.programContents?.map(item => ({ slug: item.slug, parent: item.parent, parentId: item.parentId })));

    // first level search on all program contents where the parent is null or undefined
    let content: ProgramContent | null = program.programContents?.find(item => (item.parent === null || item.parent === undefined) && item.slug === contentPath[0]) || null;

    console.log('🔍 First level search result:', content ? { slug: content.slug, title: content.title } : 'Not found');

    // if nested, search in children iteracivelly up to the last item in the path
    if (contentPath.length > 1) {
        for (const slug of contentPath.slice(1)) {
            content = content?.children?.find(item => item.slug === slug) || null;
            if (!content) {
                return null;
            }
        }
    }
    return content;
}