import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import dsaExpectations from './01-introduction/expectations.md';
import dsaIntroduction from './01-introduction/introduction.md';
import dsaAnalysis from './02-analysis/README.md';
import dsaDynamicData from './03-dynamic-data/README.md';
import dsaSorting from './04-sorting/README.md';
import dsaDivideAndConquer from './05-divide-and-conquer/README.md';
import dsaHashtables from './06-hashtables/README.md';
import dsaMidterm from './07-midterm/README.md';
import dsaStackAndQueue from './08-stack-and-queue/README.md';
import dsaBreak from './09-break/README.md';
import dsaGraphs from './10-graphs/README.md';
import dsaDijkstra from './11-dijkstra/README.md';
import dsaMst from './12-mst/README.md';
import dsaBst from './13-bst/README.md';
import dsaHeap from './14-heap/README.md';
import dsaProject from './15-project/README.md';
import dsaFinals from './16-finals/README.md';
import dsaSyllabus from './syllabus.md';

// Program definition
export const dsaProgram: Program = {
    id: 'dsa-program-1',
    title: 'Data Structures and Algorithms',
    description:
        'Students compare and contrast a variety of data structures. Students compare algorithms for tasks such as searching and sorting, while articulating efficiency in terms of time complexity.',
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
    programRatings: [],
    programWishlists: [],
};

// Product definition
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

// Product-Program relation
export const dsaProductProgram: ProductProgram = {
    id: 'dsa-product-program-1',
    productId: 'dsa-product-1',
    product: dsaProduct,
    programId: 'dsa-program-1',
    program: dsaProgram,
    sortOrder: 4,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Program Contents
export const dsaSyllabusContent: ProgramContent = {
    id: 'dsa-syllabus',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Data Structures and Algorithms Syllabus',
    description: 'Course syllabus and overview',
    type: 0, // Page
    body: dsaSyllabus,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaIntroductionContent: ProgramContent = {
    id: 'dsa-introduction',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Introduction to Data Structures and Algorithms',
    description: 'Course introduction and overview',
    type: 0, // Page
    body: dsaIntroduction,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaExpectationsContent: ProgramContent = {
    id: 'dsa-expectations',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Week 01: Course Expectations Report',
    description: 'Student expectations and feedback analysis for the Data Structures and Algorithms course',
    type: 0, // Page
    body: dsaExpectations,
    sortOrder: 3,
    isRequired: false,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaAnalysisContent: ProgramContent = {
    id: 'dsa-analysis',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Algorithm Analysis',
    description: 'Big O notation and algorithm complexity analysis',
    type: 0, // Page
    body: dsaAnalysis,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaDynamicDataContent: ProgramContent = {
    id: 'dsa-dynamic-data',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Dynamic Data Structures',
    description: 'Arrays, linked lists, and dynamic memory allocation',
    type: 0, // Page
    body: dsaDynamicData,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaSortingContent: ProgramContent = {
    id: 'dsa-sorting',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Sorting Algorithms',
    description: 'Bubble sort, selection sort, insertion sort, merge sort, quick sort',
    type: 0, // Page
    body: dsaSorting,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaDivideAndConquerContent: ProgramContent = {
    id: 'dsa-divide-and-conquer',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Divide and Conquer',
    description: 'Divide and conquer algorithms and techniques',
    type: 0, // Page
    body: dsaDivideAndConquer,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaHashtablesContent: ProgramContent = {
    id: 'dsa-hashtables',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Hash Tables',
    description: 'Hash functions, collision resolution, and hash table implementation',
    type: 0, // Page
    body: dsaHashtables,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaMidtermContent: ProgramContent = {
    id: 'dsa-midterm',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Midterm Exam',
    description: 'Midterm examination covering first half of course',
    type: 2, // Assignment
    body: dsaMidterm,
    sortOrder: 9,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaStackAndQueueContent: ProgramContent = {
    id: 'dsa-stack-and-queue',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Stacks and Queues',
    description: 'Stack and queue data structures and their applications',
    type: 0, // Page
    body: dsaStackAndQueue,
    sortOrder: 10,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaBreakContent: ProgramContent = {
    id: 'dsa-break',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Spring Break',
    description: 'Spring break - no classes',
    type: 0, // Page
    body: dsaBreak,
    sortOrder: 11,
    isRequired: false,
    estimatedMinutes: 0,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaGraphsContent: ProgramContent = {
    id: 'dsa-graphs',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Graph Data Structures',
    description: 'Graph representation, traversal algorithms (BFS, DFS)',
    type: 0, // Page
    body: dsaGraphs,
    sortOrder: 12,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaDijkstraContent: ProgramContent = {
    id: 'dsa-dijkstra',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: "Dijkstra's Algorithm",
    description: "Shortest path algorithms and Dijkstra's algorithm",
    type: 0, // Page
    body: dsaDijkstra,
    sortOrder: 13,
    isRequired: true,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaMstContent: ProgramContent = {
    id: 'dsa-mst',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Minimum Spanning Trees',
    description: "Kruskal's and Prim's algorithms for minimum spanning trees",
    type: 0, // Page
    body: dsaMst,
    sortOrder: 14,
    isRequired: true,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaBstContent: ProgramContent = {
    id: 'dsa-bst',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Binary Search Trees',
    description: 'Binary search tree implementation and operations',
    type: 0, // Page
    body: dsaBst,
    sortOrder: 15,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaHeapContent: ProgramContent = {
    id: 'dsa-heap',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Heaps and Priority Queues',
    description: 'Heap data structure and priority queue implementation',
    type: 0, // Page
    body: dsaHeap,
    sortOrder: 16,
    isRequired: true,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaProjectContent: ProgramContent = {
    id: 'dsa-project',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Final Project',
    description: 'Comprehensive final project applying course concepts',
    type: 2, // Assignment
    body: dsaProject,
    sortOrder: 17,
    isRequired: true,
    estimatedMinutes: 480,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const dsaFinalsContent: ProgramContent = {
    id: 'dsa-finals',
    programId: 'dsa-program-1',
    parentId: undefined,
    title: 'Final Exam',
    description: 'Comprehensive final examination',
    type: 2, // Assignment
    body: dsaFinals,
    sortOrder: 18,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: dsaProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Wire program contents
dsaProgram.programContents = [
    dsaSyllabusContent,
    dsaIntroductionContent,
    dsaExpectationsContent,
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
