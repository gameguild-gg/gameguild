import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';
import week01Lecture from './python/chapters/week01/lecture.md';
import syllabusBody from './python/syllabus.md';

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

// Week 1 Content (Syllabus)
export const week1Syllabus: ProgramContent = {
    id: 'python-week1-syllabus',
    programId: pythonProgram.id!,
    parentId: null,
    title: 'Course Syllabus and Introduction',
    description: 'Course overview, objectives, and weekly schedule',
    type: 0, // Page
    body: syllabusBody,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: pythonProgram,
    parent: null,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 1 Lecture Content
export const week1Lecture: ProgramContent = {
    id: 'python-week1-lecture',
    programId: pythonProgram.id!,
    parentId: week1Syllabus.id,
    title: 'Week 1: Introduction to Python',
    description: 'Introduction to algorithms, problem-solving, and Python basics',
    type: 0, // Page
    body: week01Lecture,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: pythonProgram,
    parent: week1Syllabus,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Update the relationships
week1Syllabus.children = [week1Lecture];
pythonProgram.programContents = [week1Syllabus, week1Lecture];
pythonProduct.productPrograms = [pythonProductProgram];

// Export all mock data
export const mockPrograms: Program[] = [pythonProgram];
export const mockProducts: Product[] = [pythonProduct];
export const mockProductPrograms: ProductProgram[] = [pythonProductProgram];
export const mockProgramContents: ProgramContent[] = [week1Syllabus, week1Lecture];

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