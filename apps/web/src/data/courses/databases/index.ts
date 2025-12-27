import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import databasesSyllabus from './syllabus.md';

// Program definition
export const databasesProgram: Program = {
    id: 'databases-program-1',
    title: 'Databases',
    description:
        'This course introduces students to database design, SQL, normalization, and relational database theory. Traditional relational databases will be contrasted with NoSQL paradigms including document-oriented, key-value store, and graph databases. Students will gain hands-on experience writing database applications.',
    slug: 'databases',
    thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Databases',
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
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Databases',
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
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'Databases course overview, learning outcomes, and schedule',
    type: 0, // Page
    body: databasesSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: databasesProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Wire program contents and product-program relations
databasesProgram.programContents = [databasesSyllabusContent];

databasesProduct.productPrograms = [databasesProductProgram];

export default databasesProgram;
