import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import ai4gamesSyllabus from './syllabus.md';
import ai4gamesExpectations from './week01/expectations.md';
import ai4gamesSetup from './week01/setup.md';
import ai4gamesFlocking from './week02/flocking.md';
import ai4gamesLife from './week03/life.md';
import ai4gamesStateMachines from './week03/state-machines.md';
import ai4gamesMazeDatastructure from './week04/maze-datatructure.md';
import ai4gamesMaze from './week04/maze.md';
import ai4gamesPathfinding from './week05/pathfinding.md';
import ai4gamesCatchTheCat from './week06/catchthecat.md';
import ai4gamesPathfindingAssignment from './week08/assignment.md';
import ai4gamesSpatialQuantization from './week08/spatial-quantization.md';
import ai4gamesPathSmoothing from './week09/path-smoothing.md';
import ai4gamesNoise from './week10/noise.md';
import ai4gamesRng from './week10/rng.md';
import ai4gamesFinalProject from './week11/final-project.md';

// Program definition
export const ai4gamesProgram: Program = {
    id: 'ai4games-program-1',
    title: 'AI for Games',
    description:
        'Learn artificial intelligence techniques for game development, including behavioral agents, pathfinding algorithms, procedural content generation, and noise functions.',
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
    programRatings: [],
    programWishlists: [],
};

// Product definition
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
    // creator is optional; omit to avoid cross-module deps
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

// Product-Program relation
export const ai4gamesProductProgram: ProductProgram = {
    id: 'ai4games-product-program-1',
    productId: 'ai4games-product-1',
    product: ai4gamesProduct,
    programId: 'ai4games-program-1',
    program: ai4gamesProgram,
    sortOrder: 1,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Program Contents
export const ai4gamesSyllabusContent: ProgramContent = {
    id: 'ai4games-syllabus',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'AI for Games course overview and objectives',
    type: 0, // Page
    body: ai4gamesSyllabus,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesSetupContent: ProgramContent = {
    id: 'ai4games-setup',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 01: Development Environment Setup',
    description: 'Setting up the development environment for AI game programming',
    type: 0, // Page
    body: ai4gamesSetup,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesExpectationsContent: ProgramContent = {
    id: 'ai4games-expectations',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 01: Course Expectations Report',
    description: 'Student expectations and feedback analysis for the AI for Games course',
    type: 0, // Page
    body: ai4gamesExpectations,
    sortOrder: 5,
    isRequired: false,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesFlockingContent: ProgramContent = {
    id: 'ai4games-flocking',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 02: Flocking Behavior',
    description: 'Implementing flocking algorithms and behavioral agents',
    type: 0, // Page
    body: ai4gamesFlocking,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesStateMachinesContent: ProgramContent = {
    id: 'ai4games-state-machines',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 03: State Machines',
    description: 'Understanding and implementing state machines for game AI',
    type: 0, // Page
    body: ai4gamesStateMachines,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesLifeContent: ProgramContent = {
    id: 'ai4games-life',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 03: Game of Life',
    description: "Implementing Conway's Game of Life and cellular automata",
    type: 0, // Page
    body: ai4gamesLife,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 75,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesRngContent: ProgramContent = {
    id: 'ai4games-rng',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 10: Random Number Generation',
    description: 'Understanding and implementing random number generators',
    type: 0, // Page
    body: ai4gamesRng,
    sortOrder: 13,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};
export const ai4gamesMazeContent: ProgramContent = {
    id: 'ai4games-maze',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 04: Maze Generation',
    description: 'Algorithms for procedural maze generation',
    type: 0, // Page
    body: ai4gamesMaze,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesMazeDatastructureContent: ProgramContent = {
    id: 'ai4games-maze-datastructure',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 04: Maze Data Structures',
    description: 'Understanding data structures for maze representation',
    type: 0, // Page
    body: ai4gamesMazeDatastructure,
    sortOrder: 9,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesPathfindingContent: ProgramContent = {
    id: 'ai4games-pathfinding',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 05: Pathfinding Algorithms',
    description: 'Implementing A* and other pathfinding algorithms',
    type: 0, // Page
    body: ai4gamesPathfinding,
    sortOrder: 10,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesCatchTheCatContent: ProgramContent = {
    id: 'ai4games-catchthecat',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 07: Catch the Cat Game',
    description: 'Implementing AI for the Catch the Cat puzzle game',
    type: 0, // Page
    body: ai4gamesCatchTheCat,
    sortOrder: 11,
    isRequired: true,
    estimatedMinutes: 105,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesWeek08AssignmentContent: ProgramContent = {
    id: 'ai4games-week08-assignment',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 08: Enhanced Pathfinding Assignment',
    description: 'Hands-on assignment for enhanched pathfinding',
    type: 2, // Assignment
    body: ai4gamesPathfindingAssignment,
    sortOrder: 12,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesSpatialQuantizationContent: ProgramContent = {
    id: 'ai4games-spatial-quantization',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 08: Spatial Quantization',
    description: 'Spatial data structures and quantization techniques',
    type: 0, // Page
    body: ai4gamesSpatialQuantization,
    sortOrder: 12,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesPathSmoothingContent: ProgramContent = {
    id: 'ai4games-path-smoothing',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 09: Path Smoothing',
    description: 'Path smoothing techniques for cleaner navigation',
    type: 0, // Page
    body: ai4gamesPathSmoothing,
    sortOrder: 13,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesNoiseContent: ProgramContent = {
    id: 'ai4games-noise',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 10: Noise Functions',
    description: 'Perlin noise and procedural content generation',
    type: 0, // Page
    body: ai4gamesNoise,
    sortOrder: 14,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const ai4gamesFinalProjectContent: ProgramContent = {
    id: 'ai4games-final-project',
    programId: 'ai4games-program-1',
    parentId: undefined,
    title: 'Week 11: Final Project',
    description: 'Capstone project integrating AI techniques learned throughout the course',
    type: 2, // Assignment
    body: ai4gamesFinalProject,
    sortOrder: 15,
    isRequired: true,
    estimatedMinutes: 300,
    visibility: 1, // Published
    program: ai4gamesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Wire program contents and product-program relations
ai4gamesProgram.programContents = [
    ai4gamesSyllabusContent,
    ai4gamesSetupContent,
    ai4gamesExpectationsContent,
    ai4gamesFlockingContent,
    ai4gamesStateMachinesContent,
    ai4gamesLifeContent,
    ai4gamesMazeContent,
    ai4gamesMazeDatastructureContent,
    ai4gamesPathfindingContent,
    ai4gamesCatchTheCatContent,
    ai4gamesWeek08AssignmentContent,
    ai4gamesSpatialQuantizationContent,
    ai4gamesPathSmoothingContent,
    ai4gamesRngContent,
    ai4gamesNoiseContent,
    ai4gamesFinalProjectContent,
];
