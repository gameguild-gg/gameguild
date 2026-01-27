import { Product, ProductProgram, Program, ProgramContent, ProgramContentType } from '@/lib/api/generated';

import networkingWeek01Lecture from './01-intro/00-lecture.md';
import networkingWeek01ReadingsMd from './01-intro/01-readings.md';
import networkingWeek01QuizMd from './01-intro/02-quiz.md';
import networkingWeek01SetupMd from './01-intro/02-setup.md';
import networkingWeek01AssignmentMd from './01-intro/03-assignment.md';
import networkingWeek02Lecture from './02-addressing/01-lecture.md';
import networkingWeek02LectureReveal from './02-addressing/02-lecture-reveal.md';
import networkingWeek02ReadingsMd from './02-addressing/03-readings.md';
import networkingWeek02QuizMd from './02-addressing/04-quiz.md';
import networkingWeek02AssignmentMd from './02-addressing/05-assignment.md';
import networkingWeek03Lecture from './03-udp/00-lecture.md';
import networkingWeek03LectureReveal from './03-udp/01-reveal.md';
import networkingWeek03ReadingsMd from './03-udp/02-readings.md';
import networkingWeek03QuizMd from './03-udp/03-quiz.md';
import networkingWeek03AssignmentMd from './03-udp/04-assignment.md';
import networkingSyllabus from './syllabus.md';

export const networkingProgram: Program = {
    id: 'networking-program-1',
    title: 'Network Programming',
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

export const networkingWeek01Quiz: ProgramContent & { slug: string } = {
    id: 'quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-01',
    title: 'Quiz 01',
    description: 'Test your understanding of network fundamentals, OSI model, and protocols.',
    type: 0, // Page
    body: networkingWeek01QuizMd,
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

export const networkingWeek01Setup: ProgramContent & { slug: string } = {
    id: 'setup',
    slug: 'setup',
    programId: 'networking-program-1',
    parentId: 'week-01',
    title: 'Setup',
    description: 'GIT, repository, IDE setup, and testing assignments.',
    type: 0, // Page
    body: networkingWeek01SetupMd,
    sortOrder: 3,
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
    sortOrder: 4,
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

export const networkingWeek02Intro: ProgramContent & { slug: string } = {
    id: 'week-02',
    slug: 'week-02',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 02 — Network Addressing',
    description: 'IP addressing (IPv4/IPv6), subnetting, CIDR notation, DNS, routing basics, and Wireshark introduction.',
    type: 0, // Page
    body: networkingWeek02Lecture,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek02LectureSlides: ProgramContent & { slug: string } = {
    id: 'week-02-slides',
    slug: 'slides',
    programId: 'networking-program-1',
    parentId: 'week-02',
    title: 'Lecture Slides',
    description: 'Presentation slides for Week 02 lecture on IP addressing, subnetting, DNS, and routing.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek02LectureReveal,
    sortOrder: 0,
    isRequired: false,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek02Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek02Readings: ProgramContent & { slug: string } = {
    id: 'week-02-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-02',
    title: 'Readings',
    description: 'Required readings on IP addressing, subnetting, DNS, and routing.',
    type: 0, // Page
    body: networkingWeek02ReadingsMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 155,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek02Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek02Quiz: ProgramContent & { slug: string } = {
    id: 'week-02-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-02',
    title: 'Quiz 02',
    description: 'Test your understanding of IP addressing, subnetting, and DNS.',
    type: 0, // Page
    body: networkingWeek02QuizMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek02Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek02Assignment: ProgramContent & { slug: string } = {
    id: 'week-02-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-02',
    title: 'Assignment 02',
    description: 'Build an IP subnet calculator with network analysis capabilities.',
    type: 0, // Page
    body: networkingWeek02AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek02Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek03Intro: ProgramContent & { slug: string } = {
    id: 'week-03',
    slug: 'week-03',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 03 — UDP and Datagram Sockets',
    description: 'UDP protocol, Berkeley sockets API, datagram communication, broadcast discovery, and Boost.Asio.',
    type: 0, // Page
    body: networkingWeek03Lecture,
    sortOrder: 4,
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

export const networkingWeek03LectureSlides: ProgramContent & { slug: string } = {
    id: 'week-03-slides',
    slug: 'slides',
    programId: 'networking-program-1',
    parentId: 'week-03',
    title: 'Lecture Slides',
    description: 'Presentation slides for Week 03 lecture on UDP and datagram sockets.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek03LectureReveal,
    sortOrder: 0,
    isRequired: false,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek03Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek03Readings: ProgramContent & { slug: string } = {
    id: 'week-03-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-03',
    title: 'Readings',
    description: 'Required readings on UDP protocol, sockets API, and broadcast networking.',
    type: 0, // Page
    body: networkingWeek03ReadingsMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 110,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek03Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek03Quiz: ProgramContent & { slug: string } = {
    id: 'week-03-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-03',
    title: 'Quiz 03',
    description: 'Test your understanding of UDP protocol, datagram sockets, and broadcast.',
    type: 0, // Page
    body: networkingWeek03QuizMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek03Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek03Assignment: ProgramContent & { slug: string } = {
    id: 'week-03-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-03',
    title: 'Assignment 03',
    description: 'Build a UDP echo client/server with broadcast-based server discovery.',
    type: 0, // Page
    body: networkingWeek03AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek03Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek01Intro.children = [networkingWeek01Readings, networkingWeek01Quiz, networkingWeek01Setup, networkingWeek01Assignment];
networkingWeek02Intro.children = [networkingWeek02LectureSlides, networkingWeek02Readings, networkingWeek02Quiz, networkingWeek02Assignment];
networkingWeek03Intro.children = [networkingWeek03LectureSlides, networkingWeek03Readings, networkingWeek03Quiz, networkingWeek03Assignment];
networkingProgram.programContents = [networkingSyllabusContent, networkingWeek01Intro, networkingWeek02Intro, networkingWeek03Intro];
networkingProduct.productPrograms = [networkingProductProgram];

export default networkingProgram;
