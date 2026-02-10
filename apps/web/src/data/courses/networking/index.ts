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
import networkingWeek04LectureReveal from './04-tcp/00-reveal.md';
import networkingWeek04LectureMd from './04-tcp/01-lecture.md';
import networkingWeek04IntroductionMd from './04-tcp/01a-introduction.md';
import networkingWeek04ConnectionEstablishmentMd from './04-tcp/01b-connection-establishment.md';
import networkingWeek04ReliabilityMd from './04-tcp/01c-reliability.md';
import networkingWeek04FlowControlMd from './04-tcp/01d-flow-control.md';
import networkingWeek04CongestionControlMd from './04-tcp/01e-congestion-control.md';
import networkingWeek04TerminationComparisonMd from './04-tcp/01f-termination-comparison.md';
import networkingWeek04BoostAsioMd from './04-tcp/01g-boost-asio.md';
import networkingWeek04MultiClientMd from './04-tcp/01h-multi-client.md';
import networkingWeek04ConcurrencyModelsMd from './04-tcp/01i-concurrency-models.md';
import networkingWeek04DebuggingMd from './04-tcp/01j-debugging.md';
import networkingWeek04ReadingsMd from './04-tcp/02-readings.md';
import networkingWeek04QuizMd from './04-tcp/03-quiz.md';
import networkingWeek04AssignmentMd from './04-tcp/04-assignment.md';
import networkingWeek05LectureReveal from './05-framing/01-reveal.md';
import networkingWeek05LectureMd from './05-framing/02-lecture.md';
import networkingWeek05FramingProblemMd from './05-framing/02a-framing-problem.md';
import networkingWeek05FramingStrategiesMd from './05-framing/02b-framing-strategies.md';
import networkingWeek05BufferManagementMd from './05-framing/02c-buffer-management.md';
import networkingWeek05PartialIOMd from './05-framing/02d-partial-io.md';
import networkingWeek05DeadlockPreventionMd from './05-framing/02e-deadlock-prevention.md';
import networkingWeek05ConcurrencyModelsMd from './05-framing/02f-concurrency-models.md';
import networkingWeek05CppConcurrencyMd from './05-framing/02g-cpp-concurrency.md';
import networkingWeek05EdgeCasesMd from './05-framing/02h-edge-cases.md';
import networkingWeek05ReadingsMd from './05-framing/03-readings.md';
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

export const networkingWeek04Intro: ProgramContent & { slug: string } = {
    id: 'week-04',
    slug: 'week-04',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 04 — TCP and Stream Sockets',
    description: 'TCP protocol, connection establishment, reliability mechanisms, flow/congestion control, and Boost.Asio TCP programming.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek04LectureReveal,
    sortOrder: 5,
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

export const networkingWeek04LectureContent: ProgramContent & { slug: string } = {
    id: 'week-04-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-04',
    title: 'Lecture Notes',
    description: 'Detailed lecture notes on TCP protocol, connection management, and multi-client server implementation.',
    type: 0, // Page
    body: networkingWeek04LectureMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04Readings: ProgramContent & { slug: string } = {
    id: 'week-04-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-04',
    title: 'Readings',
    description: 'Required readings on TCP protocol, stream sockets, and connection management.',
    type: 0, // Page
    body: networkingWeek04ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 150,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04Quiz: ProgramContent & { slug: string } = {
    id: 'week-04-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-04',
    title: 'Quiz 04',
    description: 'Test your understanding of TCP protocol, connection states, and stream sockets.',
    type: 0, // Page
    body: networkingWeek04QuizMd,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04Assignment: ProgramContent & { slug: string } = {
    id: 'week-04-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-04',
    title: 'Assignment 04',
    description: 'Build a multi-client TCP chatroom with Boost.Asio.',
    type: 0, // Page
    body: networkingWeek04AssignmentMd,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 04 Lecture Sections (split from main lecture)

export const networkingWeek04Introduction: ProgramContent & { slug: string } = {
    id: 'week-04-introduction',
    slug: 'introduction',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '1. Introduction to TCP',
    description: 'TCP as a connection-oriented, reliable, byte-stream protocol and TCP header format.',
    type: 0, // Page
    body: networkingWeek04IntroductionMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 10,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04ConnectionEstablishment: ProgramContent & { slug: string } = {
    id: 'week-04-connection-establishment',
    slug: 'connection-establishment',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '2. Connection Establishment',
    description: 'TCP three-way handshake and connection state machine.',
    type: 0, // Page
    body: networkingWeek04ConnectionEstablishmentMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04Reliability: ProgramContent & { slug: string } = {
    id: 'week-04-reliability',
    slug: 'reliability',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '3. Reliability Mechanisms',
    description: 'Sequence numbers, acknowledgments, and retransmission.',
    type: 0, // Page
    body: networkingWeek04ReliabilityMd,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04FlowControl: ProgramContent & { slug: string } = {
    id: 'week-04-flow-control',
    slug: 'flow-control',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '4. Flow Control',
    description: 'Sliding window protocol and preventing receiver buffer overflow.',
    type: 0, // Page
    body: networkingWeek04FlowControlMd,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 10,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04CongestionControl: ProgramContent & { slug: string } = {
    id: 'week-04-congestion-control',
    slug: 'congestion-control',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '5. Congestion Control',
    description: 'Slow start, AIMD, and network congestion prevention.',
    type: 0, // Page
    body: networkingWeek04CongestionControlMd,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04TerminationComparison: ProgramContent & { slug: string } = {
    id: 'week-04-termination-comparison',
    slug: 'termination-comparison',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '6. Termination and Protocol Comparison',
    description: 'TCP connection termination and TCP vs UDP comparison.',
    type: 0, // Page
    body: networkingWeek04TerminationComparisonMd,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04BoostAsio: ProgramContent & { slug: string } = {
    id: 'week-04-boost-asio',
    slug: 'boost-asio',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '7. TCP Programming with Boost.Asio',
    description: 'Client/server setup, socket options, and graceful shutdown.',
    type: 0, // Page
    body: networkingWeek04BoostAsioMd,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04MultiClient: ProgramContent & { slug: string } = {
    id: 'week-04-multi-client',
    slug: 'multi-client',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '8. Multi-Client Connection Management',
    description: 'Server architecture, io_context.run(), user registry, and chat commands.',
    type: 0, // Page
    body: networkingWeek04MultiClientMd,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04ConcurrencyModels: ProgramContent & { slug: string } = {
    id: 'week-04-concurrency-models',
    slug: 'concurrency-models',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '9. Alternative Concurrency Models',
    description: 'Async I/O with Boost.Asio and choosing the right model.',
    type: 0, // Page
    body: networkingWeek04ConcurrencyModelsMd,
    sortOrder: 9,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek04Debugging: ProgramContent & { slug: string } = {
    id: 'week-04-debugging',
    slug: 'debugging',
    programId: 'networking-program-1',
    parentId: 'week-04-lecture',
    title: '10. Common TCP Issues and Debugging',
    description: 'Troubleshooting common issues and final summary.',
    type: 0, // Page
    body: networkingWeek04DebuggingMd,
    sortOrder: 10,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek04LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 05: Message Framing, Buffering, and Concurrency

export const networkingWeek05Intro: ProgramContent & { slug: string } = {
    id: 'week-05',
    slug: 'week-05',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 05 — Message Framing and Concurrency',
    description: 'Message framing strategies, buffer management, partial I/O handling, deadlock prevention, and concurrency models.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek05LectureReveal,
    sortOrder: 6,
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

export const networkingWeek05LectureContent: ProgramContent & { slug: string } = {
    id: 'week-05-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-05',
    title: 'Lecture Notes',
    description: 'Detailed lecture notes on framing strategies, buffer management, and concurrency models.',
    type: 0, // Page
    body: networkingWeek05LectureMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05Readings: ProgramContent & { slug: string } = {
    id: 'week-05-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-05',
    title: 'Readings',
    description: 'Required readings on message framing, buffering, and concurrency patterns.',
    type: 0, // Page
    body: networkingWeek05ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 05 Lecture Sections (split from main lecture)

export const networkingWeek05FramingProblem: ProgramContent & { slug: string } = {
    id: 'week-05-framing-problem',
    slug: 'framing-problem',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '1. The TCP Framing Problem',
    description: 'Understanding why TCP requires message framing and how byte stream semantics work.',
    type: 0, // Page
    body: networkingWeek05FramingProblemMd,
    sortOrder: 1,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 10,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05FramingStrategies: ProgramContent & { slug: string } = {
    id: 'week-05-framing-strategies',
    slug: 'framing-strategies',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '2. Framing Strategies',
    description: 'Length-prefix, delimiter-based, TLV, and fixed-length framing approaches.',
    type: 0, // Page
    body: networkingWeek05FramingStrategiesMd,
    sortOrder: 2,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05BufferManagement: ProgramContent & { slug: string } = {
    id: 'week-05-buffer-management',
    slug: 'buffer-management',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '3. Buffer Management',
    description: 'Buffer types, receive patterns, and lifetime management in Boost.Asio.',
    type: 0, // Page
    body: networkingWeek05BufferManagementMd,
    sortOrder: 3,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 10,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05PartialIO: ProgramContent & { slug: string } = {
    id: 'week-05-partial-io',
    slug: 'partial-io',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '4. Handling Partial I/O',
    description: 'Dealing with partial reads/writes and composed operations.',
    type: 0, // Page
    body: networkingWeek05PartialIOMd,
    sortOrder: 4,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05DeadlockPrevention: ProgramContent & { slug: string } = {
    id: 'week-05-deadlock-prevention',
    slug: 'deadlock-prevention',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '5. Deadlock Prevention',
    description: 'Avoiding TCP deadlock scenarios and implementing write queues.',
    type: 0, // Page
    body: networkingWeek05DeadlockPreventionMd,
    sortOrder: 5,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05ConcurrencyModels: ProgramContent & { slug: string } = {
    id: 'week-05-concurrency-models',
    slug: 'concurrency-models',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '6. Concurrency Models',
    description: 'OS threads, coroutines, fibers, and work stealing concepts.',
    type: 0, // Page
    body: networkingWeek05ConcurrencyModelsMd,
    sortOrder: 6,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05CppConcurrency: ProgramContent & { slug: string } = {
    id: 'week-05-cpp-concurrency',
    slug: 'cpp-concurrency',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '7. C++ Concurrency Implementation',
    description: 'std::jthread, Boost.Asio callbacks, C++20 coroutines, and Boost.Fiber.',
    type: 0, // Page
    body: networkingWeek05CppConcurrencyMd,
    sortOrder: 7,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek05EdgeCases: ProgramContent & { slug: string } = {
    id: 'week-05-edge-cases',
    slug: 'edge-cases',
    programId: 'networking-program-1',
    parentId: 'week-05-lecture',
    title: '8. Edge Cases and Summary',
    description: 'Connection termination, byte order, and assignment preparation.',
    type: 0, // Page
    body: networkingWeek05EdgeCasesMd,
    sortOrder: 8,
    isRequired: true,
    gradingMethod: 0, // None
    maxPoints: null,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek01Intro.children = [networkingWeek01Readings, networkingWeek01Quiz, networkingWeek01Setup, networkingWeek01Assignment];
networkingWeek02Intro.children = [networkingWeek02LectureSlides, networkingWeek02Readings, networkingWeek02Quiz, networkingWeek02Assignment];
networkingWeek03Intro.children = [networkingWeek03LectureSlides, networkingWeek03Readings, networkingWeek03Quiz, networkingWeek03Assignment];
networkingWeek04Intro.children = [networkingWeek04LectureContent, networkingWeek04Readings, networkingWeek04Quiz, networkingWeek04Assignment];
networkingWeek04LectureContent.children = [
    networkingWeek04Introduction,
    networkingWeek04ConnectionEstablishment,
    networkingWeek04Reliability,
    networkingWeek04FlowControl,
    networkingWeek04CongestionControl,
    networkingWeek04TerminationComparison,
    networkingWeek04BoostAsio,
    networkingWeek04MultiClient,
    networkingWeek04ConcurrencyModels,
    networkingWeek04Debugging,
];
networkingWeek05LectureContent.children = [
    networkingWeek05FramingProblem,
    networkingWeek05FramingStrategies,
    networkingWeek05BufferManagement,
    networkingWeek05PartialIO,
    networkingWeek05DeadlockPrevention,
    networkingWeek05ConcurrencyModels,
    networkingWeek05CppConcurrency,
    networkingWeek05EdgeCases,
];
networkingWeek05Intro.children = [networkingWeek05LectureContent, networkingWeek05Readings];
networkingProgram.programContents = [networkingSyllabusContent, networkingWeek01Intro, networkingWeek02Intro, networkingWeek03Intro, networkingWeek04Intro, networkingWeek05Intro];
networkingProduct.productPrograms = [networkingProductProgram];

export default networkingProgram;
