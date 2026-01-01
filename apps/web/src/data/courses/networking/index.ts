import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

import networkingWeek01Lecture from './01-intro/00-lecture.md';
import networkingWeek01ReadingsMd from './01-intro/01-readings.md';
import networkingWeek01SetupMd from './01-intro/02-setup.md';
import networkingWeek01AssignmentMd from './01-intro/03-assignment.md';
import networkingSyllabus from './syllabus.md';

export const networkingProgram: Program = {
    id: 'networking-program-1',
    title: 'Game Network Programming with C++',
    description:
        'Learn to design, implement, and optimize real-time networked applications and games using sockets, serialization, synchronization, and performance tuning techniques.',
    slug: 'networking',
    thumbnail: 'https://i.imgur.com/Do3392o.jpeg',
    videoShowcaseUrl: null,
    estimatedHours: 60,
    enrollmentStatus: 0, // Open
    maxEnrollments: null,
    enrollmentDeadline: null,
    category: 1, // Game Development / Networking
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

export const networkingProduct: Product = {
    id: 'networking-product-1',
    title: 'Game Network Programming with C++ Course',
    name: 'Game Network Programming with C++',
    description:
        'Build multiplayer-ready applications with sockets, serialization, prediction/interpolation, reliability, and security fundamentals.',
    shortDescription: 'Hands-on game networking: sockets, sync, reliability, security.',
    imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Networking',
    type: 0, // Course
    isBundle: false,
    creatorId: '1',
    bundleItems: null,
    referralCommissionPercentage: 0,
    maxAffiliateDiscount: 0,
    affiliateCommissionPercentage: 0,
    visibility: 0, // Public
    status: 1, // Published
    slug: 'networking',
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
    productPrograms: [],
    productPricings: [],
    subscriptionPlans: [],
    userProducts: [],
    promoCodes: [],
};

export const networkingProductProgram: ProductProgram = {
    id: 'networking-product-program-1',
    productId: 'networking-product-1',
    product: networkingProduct,
    programId: 'networking-program-1',
    program: networkingProgram,
    sortOrder: 1,
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingSyllabusContent: ProgramContent & { slug: string } = {
    id: 'syllabus',
    slug: 'syllabus',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Course Syllabus',
    description: 'Networking course overview, objectives, schedule, and policies.',
    type: 0, // Page
    body: networkingSyllabus,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek01Intro: ProgramContent & { slug: string } = {
    id: 'week-01',
    slug: 'week-01',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 01 — Network Fundamentals',
    description: 'Introduction to OSI model, TCP/IP stack, network devices, and basic addressing.',
    type: 0, // Page
    body: networkingWeek01Lecture,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek01Readings: ProgramContent & { slug: string } = {
    id: 'readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-01',
    title: 'Readings',
    description: 'Required readings on OSI model, TCP/IP stack, and network concepts.',
    type: 0, // Page
    body: networkingWeek01ReadingsMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 70,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek01Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek01Setup: ProgramContent & { slug: string } = {
    id: 'setup',
    slug: 'setup',
    programId: 'networking-program-1',
    parentId: 'week-01',
    title: 'Setup',
    description: 'GIT, repository, IDE setup, and testing assignments.',
    type: 0, // Page
    body: networkingWeek01SetupMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek01Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek01Assignment: ProgramContent & { slug: string } = {
    id: 'assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-01',
    title: 'Assignment',
    description: 'Apply networking fundamentals with a hands-on exercise.',
    type: 0, // Page
    body: networkingWeek01AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek01Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek01Intro.children = [networkingWeek01Readings, networkingWeek01Setup, networkingWeek01Assignment];
networkingProgram.programContents = [networkingSyllabusContent, networkingWeek01Intro];
networkingProduct.productPrograms = [networkingProductProgram];

export default networkingProgram;
