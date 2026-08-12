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
import networkingWeek05QuizMd from './05-framing/04-quiz.md';
import networkingWeek06LectureReveal from './06-serialization/01-reveal.md';
import networkingWeek06LectureMd from './06-serialization/02-lecture.md';
import networkingWeek06WhySerializationMd from './06-serialization/02a-why-serialization.md';
import networkingWeek06EndiannessMd from './06-serialization/02b-endianness.md';
import networkingWeek06StructPackingMd from './06-serialization/02c-struct-packing.md';
import networkingWeek06TextFormatsMd from './06-serialization/02d-text-formats.md';
import networkingWeek06BinaryFormatsMd from './06-serialization/02e-binary-formats.md';
import networkingWeek06CustomBitpackingMd from './06-serialization/02f-custom-bitpacking.md';
import networkingWeek06CompressionMd from './06-serialization/02g-compression.md';
import networkingWeek06PerformanceComparisonMd from './06-serialization/02h-performance-comparison.md';
import networkingWeek06ReadingsMd from './06-serialization/03-readings.md';
import networkingWeek06AssignmentMd from './06-serialization/04-assignment.md';
import networkingWeek06QuizMd from './06-serialization/05-quiz.md';
import networkingWeek07LectureReveal from './07-distributed-state-sync/01-reveal.md';
import networkingWeek07LectureMd from './07-distributed-state-sync/02-lecture.md';
import networkingWeek07StateSyncModelsMd from './07-distributed-state-sync/02a-state-sync-models.md';
import networkingWeek07AuthoritativeServerMd from './07-distributed-state-sync/02b-authoritative-server.md';
import networkingWeek07ServerReconciliationMd from './07-distributed-state-sync/02c-server-reconciliation.md';
import networkingWeek07DeltaCompressionMd from './07-distributed-state-sync/02d-delta-compression.md';
import networkingWeek07ReadingsMd from './07-distributed-state-sync/03-readings.md';
import networkingWeek07AssignmentMd from './07-distributed-state-sync/04-assignment.md';
import networkingWeek07QuizMd from './07-distributed-state-sync/05-quiz.md';
import networkingWeek09FinalProject from './09-break/finalproject.md';
import networkingWeek10RevealMd from './10-http/01-reveal.md';
import networkingWeek10LectureMd from './10-http/02-lecture.md';
import networkingWeek10HttpFundamentalsMd from './10-http/02a-http-fundamentals.md';
import networkingWeek10HttpMessagesMd from './10-http/02b-http-messages.md';
import networkingWeek10MethodsStatusCodesMd from './10-http/02c-methods-and-status-codes.md';
import networkingWeek10UrlsHeadersMd from './10-http/02d-urls-and-headers.md';
import networkingWeek10RestConstraintsMd from './10-http/02e-rest-constraints.md';
import networkingWeek10HttpCachingMd from './10-http/02f-http-caching.md';
import networkingWeek10HttpEvolutionMd from './10-http/02g-http-evolution.md';
import networkingWeek10HttpCppBoostBeastMd from './10-http/02h-http-cpp-boost-beast.md';
import networkingWeek10ReadingsMd from './10-http/03-readings.md';
import networkingWeek10QuizMd from './10-http/04-quiz.md';
import networkingWeek10AssignmentMd from './10-http/assignment.md';
import networkingWeek11RevealMd from './11-nonblocking/01-reveal.md';
import networkingWeek11LectureMd from './11-nonblocking/02-lecture.md';
import networkingWeek11ParallelismVsConcurrencyMd from './11-nonblocking/02a-parallelism-vs-concurrency.md';
import networkingWeek11BlockingVsNonblockingMd from './11-nonblocking/02b-blocking-vs-nonblocking.md';
import networkingWeek11IOMultiplexingMd from './11-nonblocking/02c-io-multiplexing.md';
import networkingWeek11EventLoopReactorMd from './11-nonblocking/02d-event-loop-reactor.md';
import networkingWeek11WorkerThreadManagersMd from './11-nonblocking/02e-worker-thread-managers.md';
import networkingWeek11ThreadSafetyMd from './11-nonblocking/02f-thread-safety.md';
import networkingWeek11ModernCppConcurrencyMd from './11-nonblocking/02g-modern-cpp-concurrency.md';
import networkingWeek11CsiVsGprPatternsMd from './11-nonblocking/02h-csi-vs-gpr-patterns.md';
import networkingWeek11ReadingsMd from './11-nonblocking/03-readings.md';
import networkingWeek11AssignmentMd from './11-nonblocking/assignment.md';
import networkingWeek12RevealMd from './12-performance/01-reveal.md';
import networkingWeek12LectureMd from './12-performance/02-lecture.md';
import networkingWeek12MeasuringLatencyJitterLossMd from './12-performance/02a-measuring-latency-jitter-loss.md';
import networkingWeek12TickRateAndSimulationFrequencyMd from './12-performance/02b-tick-rate-and-simulation-frequency.md';
import networkingWeek12InterpolationAndJitterBuffersMd from './12-performance/02c-interpolation-and-jitter-buffers.md';
import networkingWeek12ReliableUdpSequenceAcksMd from './12-performance/02d-reliable-udp-sequence-acks.md';
import networkingWeek12RetransmissionAndLossDetectionMd from './12-performance/02e-retransmission-and-loss-detection.md';
import networkingWeek12CongestionPacingAndFairnessMd from './12-performance/02f-congestion-pacing-and-fairness.md';
import networkingWeek12PacketBudgetsPrioritizationDegradationMd from './12-performance/02g-packet-budgets-prioritization-degradation.md';
import networkingWeek12CsiVsGprPerformancePatternsMd from './12-performance/02h-csi-vs-gpr-performance-patterns.md';
import networkingWeek12ReadingsMd from './12-performance/03-readings.md';
import networkingWeek12AssignmentMd from './12-performance/assignment.md';
import networkingWeek12QuizMd from './12-performance/quiz.md';
import networkingWeek13AssignmentMd from './13-prediction/assignment.md';
import networkingWeek14RevealMd from './14-architecture/01-reveal.md';
import networkingWeek14LectureMd from './14-architecture/02-lecture.md';
import networkingWeek14AuthorityModelsMd from './14-architecture/02a-authority-models.md';
import networkingWeek14DedicatedVsListenServersMd from './14-architecture/02b-dedicated-vs-listen-servers.md';
import networkingWeek14RollbackNetworkingMd from './14-architecture/02c-rollback-networking.md';
import networkingWeek14SessionManagementMd from './14-architecture/02d-session-management.md';
import networkingWeek14MatchmakingMd from './14-architecture/02e-matchmaking.md';
import networkingWeek14ScalingGameServersMd from './14-architecture/02f-scaling-game-servers.md';
import networkingWeek14DistributedSystemsFoundationsMd from './14-architecture/02g-distributed-systems-foundations.md';
import networkingWeek14ArchitectureDecisionPatternsMd from './14-architecture/02h-architecture-decision-patterns.md';
import networkingWeek14ReadingsMd from './14-architecture/03-readings.md';
import networkingWeek14AssignmentMd from './14-architecture/assignment.md';
import networkingWeek15AssignmentMd from './15-security/assignment.md';
import networkingWeek16AssignmentMd from './16-presentations/assignment.md';
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
export const networkingWeek05Quiz: ProgramContent & { slug: string } = {
    id: 'week-05-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-05',
    title: 'Quiz',
    description: 'Quiz on message framing, buffering, and concurrency.',
    type: 0, // Page
    body: networkingWeek05QuizMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek05Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek05Intro.children = [networkingWeek05LectureContent, networkingWeek05Readings, networkingWeek05Quiz];

// Week 06: Serialization

export const networkingWeek06Intro: ProgramContent & { slug: string } = {
    id: 'week-06',
    slug: 'week-06',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 06 — Serialization',
    description: 'Serialization and deserialization: endianness, struct packing, JSON, Protocol Buffers, FlatBuffers, custom bitpacking, and compression.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek06LectureReveal,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06LectureContent: ProgramContent & { slug: string } = {
    id: 'week-06-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-06',
    title: 'Lecture Notes',
    description: 'Detailed lecture notes on serialization: endianness, struct packing, text/binary formats, bitpacking, and compression.',
    type: 0, // Page
    body: networkingWeek06LectureMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06Readings: ProgramContent & { slug: string } = {
    id: 'week-06-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-06',
    title: 'Readings',
    description: 'Required readings on serialization formats, endianness, and bitpacking.',
    type: 0, // Page
    body: networkingWeek06ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 100,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06Quiz: ProgramContent & { slug: string } = {
    id: 'week-06-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-06',
    title: 'Quiz 06',
    description: 'Test your understanding of serialization, endianness, struct packing, binary formats, and compression techniques.',
    type: 0, // Page
    body: networkingWeek06QuizMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 06 Lecture Sections (split from main lecture)

export const networkingWeek06WhySerialization: ProgramContent & { slug: string } = {
    id: 'week-06-why-serialization',
    slug: 'why-serialization',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '1. Why Serialization Matters',
    description: 'Why memcpy of structs fails: endianness, padding, and versioning problems.',
    type: 0, // Page
    body: networkingWeek06WhySerializationMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06Endianness: ProgramContent & { slug: string } = {
    id: 'week-06-endianness',
    slug: 'endianness',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '2. Endianness and Byte Order',
    description: 'Big-endian vs little-endian, network byte order, and Boost.Endian conversions.',
    type: 0, // Page
    body: networkingWeek06EndiannessMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06StructPacking: ProgramContent & { slug: string } = {
    id: 'week-06-struct-packing',
    slug: 'struct-packing',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '3. Struct Packing and Alignment',
    description: 'Compiler padding, alignment rules, and why sizeof varies across platforms.',
    type: 0, // Page
    body: networkingWeek06StructPackingMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06TextFormats: ProgramContent & { slug: string } = {
    id: 'week-06-text-formats',
    slug: 'text-formats',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '4. Text Formats: JSON and Beyond',
    description: 'JSON grammar, C++ JSON libraries, and comparison with CSV, XML, YAML, TOML.',
    type: 0, // Page
    body: networkingWeek06TextFormatsMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06BinaryFormats: ProgramContent & { slug: string } = {
    id: 'week-06-binary-formats',
    slug: 'binary-formats',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '5. Binary Serialization Formats',
    description: 'Protocol Buffers, FlatBuffers, MessagePack, CBOR: varints, TLV, and zero-copy.',
    type: 0, // Page
    body: networkingWeek06BinaryFormatsMd,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06CustomBitpacking: ProgramContent & { slug: string } = {
    id: 'week-06-custom-bitpacking',
    slug: 'custom-bitpacking',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '6. Custom Bitpacking',
    description: 'BitWriter/BitReader pattern, range-based serialization, compressed floats, and quaternion encoding.',
    type: 0, // Page
    body: networkingWeek06CustomBitpackingMd,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06Compression: ProgramContent & { slug: string } = {
    id: 'week-06-compression',
    slug: 'compression',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '7. Compression Techniques',
    description: 'Delta encoding, quantization, variable-length quantities, LZ4, and Zstandard.',
    type: 0, // Page
    body: networkingWeek06CompressionMd,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06PerformanceComparison: ProgramContent & { slug: string } = {
    id: 'week-06-performance-comparison',
    slug: 'performance-comparison',
    programId: 'networking-program-1',
    parentId: 'week-06-lecture',
    title: '8. Performance Comparison and Summary',
    description: 'Benchmarks: JSON vs Protobuf vs FlatBuffers vs custom bitpacking, and format selection guidelines.',
    type: 0, // Page
    body: networkingWeek06PerformanceComparisonMd,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 10,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek06Assignment: ProgramContent & { slug: string } = {
    id: 'week-06-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-06',
    title: 'Assignment 06',
    description: 'Build a serialization library with endianness conversion, varint/ZigZag encoding, and bitpacking streams.',
    type: 0, // Page
    body: networkingWeek06AssignmentMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek06Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek06Intro.children = [networkingWeek06LectureContent, networkingWeek06Readings, networkingWeek06Quiz, networkingWeek06Assignment];
networkingWeek06LectureContent.children = [
    networkingWeek06WhySerialization,
    networkingWeek06Endianness,
    networkingWeek06StructPacking,
    networkingWeek06TextFormats,
    networkingWeek06BinaryFormats,
    networkingWeek06CustomBitpacking,
    networkingWeek06Compression,
    networkingWeek06PerformanceComparison,
];

// Week 07: Distributed State and Synchronization

export const networkingWeek07Intro: ProgramContent & { slug: string } = {
    id: 'week-07',
    slug: 'week-07',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 07 — Distributed State and Synchronization',
    description: 'State sync models (client-server vs P2P), authoritative server, server reconciliation, delta compression.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek07LectureReveal,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07LectureContent: ProgramContent & { slug: string } = {
    id: 'week-07-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-07',
    title: 'Lecture Notes',
    description: 'State synchronization models, authoritative server, reconciliation, delta compression.',
    type: 0, // Page
    body: networkingWeek07LectureMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07StateSyncModels: ProgramContent & { slug: string } = {
    id: 'week-07-state-sync-models',
    slug: 'state-sync-models',
    programId: 'networking-program-1',
    parentId: 'week-07-lecture',
    title: '1. State Synchronization Models',
    description: 'Client-server vs P2P, state sync vs input sync, P2P lockstep and host authority.',
    type: 0, // Page
    body: networkingWeek07StateSyncModelsMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07AuthoritativeServer: ProgramContent & { slug: string } = {
    id: 'week-07-authoritative-server',
    slug: 'authoritative-server',
    programId: 'networking-program-1',
    parentId: 'week-07-lecture',
    title: '2. Authoritative Server and Never Trust the Client',
    description: 'Server as source of truth, zero-trust, host authority in P2P.',
    type: 0, // Page
    body: networkingWeek07AuthoritativeServerMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07ServerReconciliation: ProgramContent & { slug: string } = {
    id: 'week-07-server-reconciliation',
    slug: 'server-reconciliation',
    programId: 'networking-program-1',
    parentId: 'week-07-lecture',
    title: '3. Server Reconciliation',
    description: 'Client-side prediction, server reconciliation, P2P conflict resolution.',
    type: 0, // Page
    body: networkingWeek07ServerReconciliationMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 25,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07DeltaCompression: ProgramContent & { slug: string } = {
    id: 'week-07-delta-compression',
    slug: 'delta-compression',
    programId: 'networking-program-1',
    parentId: 'week-07-lecture',
    title: '4. Delta Compression',
    description: 'Send deltas instead of full state, selective updates, XOR trick.',
    type: 0, // Page
    body: networkingWeek07DeltaCompressionMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07Assignment: ProgramContent & { slug: string } = {
    id: 'week-07-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-07',
    title: 'Assignment 07',
    description: 'Build an authoritative game server and clients with client-side prediction, server reconciliation, and delta compression.',
    type: 0, // Page
    body: networkingWeek07AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07Readings: ProgramContent & { slug: string } = {
    id: 'week-07-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-07',
    title: 'Readings',
    description: 'CAP theorem, P2P vs client-server, Gambetta, Fiedler, delta compression, never trust the client.',
    type: 0, // Page
    body: networkingWeek07ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 105,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek07Quiz: ProgramContent & { slug: string } = {
    id: 'week-07-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-07',
    title: 'Quiz 07',
    description: 'Test your understanding of distributed state, CAP, P2P sync, authoritative server, reconciliation, and delta compression.',
    type: 0, // Page
    body: networkingWeek07QuizMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek07Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek07Intro.children = [networkingWeek07LectureContent, networkingWeek07Readings, networkingWeek07Assignment, networkingWeek07Quiz];
networkingWeek07LectureContent.children = [
    networkingWeek07StateSyncModels,
    networkingWeek07AuthoritativeServer,
    networkingWeek07ServerReconciliation,
    networkingWeek07DeltaCompression,
];

// Week 09 — Spring Break / Final Project Description
export const networkingWeek09FinalProjectContent: ProgramContent & { slug: string } = {
    id: 'week-09-final-project',
    slug: 'final-project',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Final Project',
    description: 'Final project overview: deliverables, topic suggestions, milestones, and grading.',
    type: 0, // Page
    body: networkingWeek09FinalProject,
    sortOrder: 9,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

// Week 10 — HTTP: The Application-Layer Protocol + Checkpoint 1: Proposal
export const networkingWeek10Intro: ProgramContent & { slug: string } = {
    id: 'week-10',
    slug: 'week-10',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 10 — HTTP: The Application-Layer Protocol',
    description: 'HTTP fundamentals, messages, methods, status codes, REST constraints, caching, HTTP evolution, and Boost.Beast.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek10RevealMd,
    sortOrder: 10,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10Assignment: ProgramContent & { slug: string } = {
    id: 'week-10-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-10',
    title: 'Checkpoint 1: Proposal',
    description: 'Team formation and project proposal submission.',
    type: 0, // Page
    body: networkingWeek10AssignmentMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10LectureContent: ProgramContent & { slug: string } = {
    id: 'week-10-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-10',
    title: 'Lecture Notes',
    description: 'HTTP fundamentals, messages, methods, status codes, URLs, headers, REST, caching, evolution, and Boost.Beast.',
    type: 0, // Page
    body: networkingWeek10LectureMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10HttpFundamentals: ProgramContent & { slug: string } = {
    id: 'week-10-http-fundamentals',
    slug: 'http-fundamentals',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '1. HTTP Fundamentals',
    description: 'HTTP in the network stack, request/response cycle, statelessness, framing connection to Week 5.',
    type: 0, // Page
    body: networkingWeek10HttpFundamentalsMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10HttpMessages: ProgramContent & { slug: string } = {
    id: 'week-10-http-messages',
    slug: 'http-messages',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '2. HTTP Messages: Requests and Responses',
    description: 'Message anatomy, request line, status line, headers, body, chunked encoding.',
    type: 0, // Page
    body: networkingWeek10HttpMessagesMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10MethodsStatusCodes: ProgramContent & { slug: string } = {
    id: 'week-10-methods-status-codes',
    slug: 'methods-and-status-codes',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '3. HTTP Methods and Status Codes',
    description: 'CRUD mapping, safe vs idempotent, 5 status code families, key codes to know.',
    type: 0, // Page
    body: networkingWeek10MethodsStatusCodesMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10UrlsHeaders: ProgramContent & { slug: string } = {
    id: 'week-10-urls-headers',
    slug: 'urls-and-headers',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '4. URLs, Headers, and Content Negotiation',
    description: 'URL structure, percent encoding, Boost.URL, header categories, content negotiation.',
    type: 0, // Page
    body: networkingWeek10UrlsHeadersMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10RestConstraints: ProgramContent & { slug: string } = {
    id: 'week-10-rest-constraints',
    slug: 'rest-constraints',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '5. REST Architectural Constraints',
    description: 'Fielding\'s six REST constraints, Richardson Maturity Model, REST vs RPC.',
    type: 0, // Page
    body: networkingWeek10RestConstraintsMd,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10HttpCaching: ProgramContent & { slug: string } = {
    id: 'week-10-http-caching',
    slug: 'http-caching',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '6. HTTP Caching',
    description: 'Cache-Control directives, ETags, conditional requests, freshness vs validation.',
    type: 0, // Page
    body: networkingWeek10HttpCachingMd,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10HttpEvolution: ProgramContent & { slug: string } = {
    id: 'week-10-http-evolution',
    slug: 'http-evolution',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '7. Evolution of HTTP: 1.0 → 1.1 → 2 → 3',
    description: 'HTTP/1.0 to HTTP/3, persistent connections, multiplexing, QUIC, 0-RTT.',
    type: 0, // Page
    body: networkingWeek10HttpEvolutionMd,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10HttpCppBoostBeast: ProgramContent & { slug: string } = {
    id: 'week-10-http-cpp-boost-beast',
    slug: 'http-cpp-boost-beast',
    programId: 'networking-program-1',
    parentId: 'week-10-lecture',
    title: '8. HTTP in C++ with Boost.Beast',
    description: 'Beast architecture, sync client/server, headers API, body types, cpp-httplib alternative.',
    type: 0, // Page
    body: networkingWeek10HttpCppBoostBeastMd,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 20,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10Readings: ProgramContent & { slug: string } = {
    id: 'week-10-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-10',
    title: 'Readings',
    description: 'MDN HTTP overview, messages, status codes, Fielding REST, HTTP evolution, caching.',
    type: 0, // Page
    body: networkingWeek10ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek10Quiz: ProgramContent & { slug: string } = {
    id: 'week-10-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-10',
    title: 'Quiz 10',
    description: 'Test your understanding of HTTP fundamentals, methods, status codes, REST, caching, and HTTP evolution.',
    type: 0, // Page
    body: networkingWeek10QuizMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek10Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek10Intro.children = [networkingWeek10LectureContent, networkingWeek10Readings, networkingWeek10Assignment, networkingWeek10Quiz];
networkingWeek10LectureContent.children = [
    networkingWeek10HttpFundamentals,
    networkingWeek10HttpMessages,
    networkingWeek10MethodsStatusCodes,
    networkingWeek10UrlsHeaders,
    networkingWeek10RestConstraints,
    networkingWeek10HttpCaching,
    networkingWeek10HttpEvolution,
    networkingWeek10HttpCppBoostBeast,
];

// Week 11 — Non-Blocking I/O and Concurrency + Checkpoint 2: Architecture Design
export const networkingWeek11Intro: ProgramContent & { slug: string } = {
    id: 'week-11',
    slug: 'week-11',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 11 — Non-Blocking I/O and Concurrency',
    description: 'Blocking vs non-blocking sockets, select/poll/epoll, multithreading, async patterns.',
    type: 0, // Page
    body: '# Week 11 — Non-Blocking I/O and Concurrency\n\nThis week covers blocking vs non-blocking sockets, select/poll/epoll, multithreading basics, and async patterns. See the lecture and readings for details.\n\n**Project Milestone 02:** Architecture document.',
    sortOrder: 11,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11LectureSlides: ProgramContent & { slug: string } = {
    id: 'week-11-slides',
    slug: 'slides',
    programId: 'networking-program-1',
    parentId: 'week-11',
    title: 'Lecture Slides',
    description: 'Presentation slides for Week 11 on non-blocking I/O, event loops, worker patterns, and modern concurrency.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek11RevealMd,
    sortOrder: 0,
    isRequired: false,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11LectureContent: ProgramContent & { slug: string } = {
    id: 'week-11-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-11',
    title: 'Lecture 11: Non-Blocking I/O, Parallelism, and Concurrency',
    description:
        'Core concepts: blocking vs non-blocking sockets, I/O multiplexing, reactor pattern, worker threads, thread safety, and modern C++ async tools.',
    type: 0, // Page
    body: networkingWeek11LectureMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11ParallelismVsConcurrency: ProgramContent & { slug: string } = {
    id: 'week-11-parallelism-vs-concurrency',
    slug: 'parallelism-vs-concurrency',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '1. Parallelism vs Concurrency Fundamentals',
    description: 'Conceptual differences, architectural implications, and when to use each model.',
    type: 0, // Page
    body: networkingWeek11ParallelismVsConcurrencyMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11BlockingVsNonblocking: ProgramContent & { slug: string } = {
    id: 'week-11-blocking-vs-nonblocking',
    slug: 'blocking-vs-nonblocking',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '2. Blocking vs Non-Blocking Sockets',
    description: 'Behavioral contract, trade-offs, and orchestration requirements for non-blocking design.',
    type: 0, // Page
    body: networkingWeek11BlockingVsNonblockingMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11IOMultiplexing: ProgramContent & { slug: string } = {
    id: 'week-11-io-multiplexing',
    slug: 'io-multiplexing',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '3. I/O Multiplexing Concepts: select, poll, epoll',
    description: 'Readiness-based orchestration and platform backend abstractions for scalable loops.',
    type: 0, // Page
    body: networkingWeek11IOMultiplexingMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11EventLoopReactor: ProgramContent & { slug: string } = {
    id: 'week-11-event-loop-reactor',
    slug: 'event-loop-reactor',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '4. Event Loops and Reactor-Style Architecture',
    description: 'Reactor flow, handler rules, backpressure, and cancellation-aware non-blocking design.',
    type: 0, // Page
    body: networkingWeek11EventLoopReactorMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11WorkerThreadManagers: ProgramContent & { slug: string } = {
    id: 'week-11-worker-thread-managers',
    slug: 'worker-thread-managers',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '5. Worker Threads and Thread Managers',
    description: 'Worker-pool orchestration, result handoff, lifecycle and cancellation responsibilities.',
    type: 0, // Page
    body: networkingWeek11WorkerThreadManagersMd,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11ThreadSafety: ProgramContent & { slug: string } = {
    id: 'week-11-thread-safety',
    slug: 'thread-safety',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '6. Thread Safety and Shared-State Ownership',
    description: 'Ownership-first concurrency design, serialized execution, and race-condition prevention.',
    type: 0, // Page
    body: networkingWeek11ThreadSafetyMd,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11ModernCppConcurrency: ProgramContent & { slug: string } = {
    id: 'week-11-modern-cpp-concurrency',
    slug: 'modern-cpp-concurrency',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '7. Modern C++ Concurrency: jthread, Stop Tokens, Coroutines',
    description: 'RAII thread lifecycle, cooperative cancellation, and coroutine-based async orchestration.',
    type: 0, // Page
    body: networkingWeek11ModernCppConcurrencyMd,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11CsiVsGprPatterns: ProgramContent & { slug: string } = {
    id: 'week-11-csi-vs-gpr-patterns',
    slug: 'csi-vs-gpr-patterns',
    programId: 'networking-program-1',
    parentId: 'week-11-lecture',
    title: '8. CSI vs GPR Architecture Patterns',
    description: 'Applying shared non-blocking and concurrency primitives under different system constraints.',
    type: 0, // Page
    body: networkingWeek11CsiVsGprPatternsMd,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11Readings: ProgramContent & { slug: string } = {
    id: 'week-11-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-11',
    title: 'Readings',
    description: 'Required readings for non-blocking I/O architecture, worker models, and modern C++ concurrency.',
    type: 0, // Page
    body: networkingWeek11ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek11Assignment: ProgramContent & { slug: string } = {
    id: 'week-11-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-11',
    title: 'Checkpoint 2: Architecture Design',
    description: 'Network protocol design document and architecture diagram.',
    type: 0, // Page
    body: networkingWeek11AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek11Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek11Intro.children = [
    networkingWeek11LectureSlides,
    networkingWeek11LectureContent,
    networkingWeek11Readings,
    networkingWeek11Assignment,
];
networkingWeek11LectureContent.children = [
    networkingWeek11ParallelismVsConcurrency,
    networkingWeek11BlockingVsNonblocking,
    networkingWeek11IOMultiplexing,
    networkingWeek11EventLoopReactor,
    networkingWeek11WorkerThreadManagers,
    networkingWeek11ThreadSafety,
    networkingWeek11ModernCppConcurrency,
    networkingWeek11CsiVsGprPatterns,
];

// Week 12 — Performance, Simulation Frequency, and Reliability + Checkpoint 3: Networking Prototype
export const networkingWeek12Intro: ProgramContent & { slug: string } = {
    id: 'week-12',
    slug: 'week-12',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 12 — Performance, Simulation Frequency, and Reliability',
    description: 'Latency, jitter, packet loss measurement, tick rates, reliable UDP, bandwidth management.',
    type: 0, // Page
    body: '# Week 12 — Performance, Simulation Frequency, and Reliability\n\nThis week covers latency/jitter/packet loss measurement, tick rates and simulation frequency, reliable UDP implementation, and bandwidth management. See the lecture and readings for details.\n\n**Project Milestone 03:** Networking prototype.',
    sortOrder: 12,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12LectureSlides: ProgramContent & { slug: string } = {
    id: 'week-12-slides',
    slug: 'slides',
    programId: 'networking-program-1',
    parentId: 'week-12',
    title: 'Lecture Slides',
    description: 'Presentation slides for Week 12 on performance measurement, reliability, congestion, and packet budgets.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek12RevealMd,
    sortOrder: 0,
    isRequired: false,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12LectureContent: ProgramContent & { slug: string } = {
    id: 'week-12-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-12',
    title: 'Lecture 12: Performance, Reliability, and Packet Budgets',
    description:
        'Core concepts: latency/jitter/loss measurement, tick rate tradeoffs, interpolation buffers, reliable UDP, retransmission strategy, congestion control, and packet budgeting.',
    type: 0, // Page
    body: networkingWeek12LectureMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12MeasuringLatencyJitterLoss: ProgramContent & { slug: string } = {
    id: 'week-12-measuring-latency-jitter-loss',
    slug: 'measuring-latency-jitter-loss',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '1. Measuring the Right Signals: Latency, Jitter, and Packet Loss',
    description: 'Measurement-first mindset, RTT/jitter/loss distinctions, and tail-metric instrumentation.',
    type: 0, // Page
    body: networkingWeek12MeasuringLatencyJitterLossMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12TickRateAndSimulationFrequency: ProgramContent & { slug: string } = {
    id: 'week-12-tick-rate-and-simulation-frequency',
    slug: 'tick-rate-and-simulation-frequency',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '2. Tick Rate and Simulation Frequency as Budget Decisions',
    description: 'How update cadence trades freshness against bandwidth, packet pressure, and CPU cost.',
    type: 0, // Page
    body: networkingWeek12TickRateAndSimulationFrequencyMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12InterpolationAndJitterBuffers: ProgramContent & { slug: string } = {
    id: 'week-12-interpolation-and-jitter-buffers',
    slug: 'interpolation-and-jitter-buffers',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '3. Interpolation, Jitter Buffers, and Player-Visible Smoothness',
    description: 'Buffer sizing, delay-for-smoothness tradeoffs, and handling packet clumping/loss artifacts.',
    type: 0, // Page
    body: networkingWeek12InterpolationAndJitterBuffersMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12ReliableUdpSequenceAcks: ProgramContent & { slug: string } = {
    id: 'week-12-reliable-udp-sequence-acks',
    slug: 'reliable-udp-sequence-acks',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '4. Reliable UDP Fundamentals: Sequence Numbers, ACKs, and Selective Reliability',
    description: 'Sequence windows, ACK bitfields, message reliability classes, and selective guarantees.',
    type: 0, // Page
    body: networkingWeek12ReliableUdpSequenceAcksMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12RetransmissionAndLossDetection: ProgramContent & { slug: string } = {
    id: 'week-12-retransmission-and-loss-detection',
    slug: 'retransmission-and-loss-detection',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '5. Retransmission and Loss Detection Strategy',
    description: 'Timeout and NACK inference tradeoffs, RTO estimation, backoff, and recovery safety.',
    type: 0, // Page
    body: networkingWeek12RetransmissionAndLossDetectionMd,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12CongestionPacingAndFairness: ProgramContent & { slug: string } = {
    id: 'week-12-congestion-pacing-and-fairness',
    slug: 'congestion-pacing-and-fairness',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '6. Congestion, Pacing, and Fairness Under Load',
    description: 'Hybrid congestion signals, paced sending, send-rate adaptation, and coexistence fairness.',
    type: 0, // Page
    body: networkingWeek12CongestionPacingAndFairnessMd,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12PacketBudgetsPrioritizationDegradation: ProgramContent & { slug: string } = {
    id: 'week-12-packet-budgets-prioritization-degradation',
    slug: 'packet-budgets-prioritization-degradation',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '7. Packet Budgets, Prioritization, and Degradation Strategy',
    description: 'Budget equations, class-based prioritization, anti-starvation, and graceful degradation.',
    type: 0, // Page
    body: networkingWeek12PacketBudgetsPrioritizationDegradationMd,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12CsiVsGprPerformancePatterns: ProgramContent & { slug: string } = {
    id: 'week-12-csi-vs-gpr-performance-patterns',
    slug: 'csi-vs-gpr-performance-patterns',
    programId: 'networking-program-1',
    parentId: 'week-12-lecture',
    title: '8. CSI vs GPR Decision Patterns',
    description: 'Reconciling systems-level constraints with player-experience goals for network tuning.',
    type: 0, // Page
    body: networkingWeek12CsiVsGprPerformancePatternsMd,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12Readings: ProgramContent & { slug: string } = {
    id: 'week-12-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-12',
    title: 'Readings',
    description: 'Required readings on latency/jitter/loss, reliable UDP, retransmission timing, pacing, and packet budgets.',
    type: 0, // Page
    body: networkingWeek12ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12Assignment: ProgramContent & { slug: string } = {
    id: 'week-12-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-12',
    title: 'Checkpoint 3: Networking Prototype',
    description: 'Core networking implemented and demonstrable.',
    type: 0, // Page
    body: networkingWeek12AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 300,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek12Quiz: ProgramContent & { slug: string } = {
    id: 'week-12-quiz',
    slug: 'quiz',
    programId: 'networking-program-1',
    parentId: 'week-12',
    title: 'Quiz 12',
    description: 'Test your understanding of latency/jitter/loss, tick rate, interpolation, reliable UDP, retransmission, congestion, packet budgets, and CSI vs GPR patterns.',
    type: 0, // Page
    body: networkingWeek12QuizMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 30,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek12Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek12Intro.children = [
    networkingWeek12LectureSlides,
    networkingWeek12LectureContent,
    networkingWeek12Readings,
    networkingWeek12Assignment,
    networkingWeek12Quiz,
];
networkingWeek12LectureContent.children = [
    networkingWeek12MeasuringLatencyJitterLoss,
    networkingWeek12TickRateAndSimulationFrequency,
    networkingWeek12InterpolationAndJitterBuffers,
    networkingWeek12ReliableUdpSequenceAcks,
    networkingWeek12RetransmissionAndLossDetection,
    networkingWeek12CongestionPacingAndFairness,
    networkingWeek12PacketBudgetsPrioritizationDegradation,
    networkingWeek12CsiVsGprPerformancePatterns,
];

// Week 13 — Client Prediction and Interpolation + Checkpoint 4: Alpha Build
export const networkingWeek13Intro: ProgramContent & { slug: string } = {
    id: 'week-13',
    slug: 'week-13',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 13 — Client Prediction and Interpolation',
    description: 'Client-side prediction, entity interpolation, dead reckoning, input handling.',
    type: 0, // Page
    body: '# Week 13 — Client Prediction and Interpolation\n\nThis week covers client-side prediction, entity interpolation/smoothing, dead reckoning, and input handling. Guest lecturer: Photon Quantum.\n\n**Project Milestone 04:** Alpha build (in-class testing session).',
    sortOrder: 13,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek13Assignment: ProgramContent & { slug: string } = {
    id: 'week-13-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-13',
    title: 'Checkpoint 4: Alpha Build',
    description: 'In-class testing session with peer feedback.',
    type: 0, // Page
    body: networkingWeek13AssignmentMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek13Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek13Intro.children = [networkingWeek13Assignment];

// Week 14 — Server Architecture and Session Management + Checkpoint 5: Beta Build & Feature Freeze
export const networkingWeek14Intro: ProgramContent & { slug: string } = {
    id: 'week-14',
    slug: 'week-14',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 14 — Server Architecture and Session Management',
    description: 'Authoritative servers, dedicated vs listen servers, rollback, session management, matchmaking.',
    type: 0, // Page
    body: '# Week 14 — Server Architecture and Session Management\n\nThis week covers authoritative servers, dedicated vs listen servers, rollback networking, session management, matchmaking, and scaling considerations. See the lecture and readings for details.\n\n**Project Milestone 05:** Beta build & feature freeze (in-class testing session).',
    sortOrder: 14,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14Assignment: ProgramContent & { slug: string } = {
    id: 'week-14-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-14',
    title: 'Checkpoint 5: Beta Build & Feature Freeze',
    description: 'Second testing session. Feature freeze after this week.',
    type: 0, // Page
    body: networkingWeek14AssignmentMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 120,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14LectureSlides: ProgramContent & { slug: string } = {
    id: 'week-14-slides',
    slug: 'slides',
    programId: 'networking-program-1',
    parentId: 'week-14',
    title: 'Lecture Slides',
    description: 'Presentation slides for Week 14 on server architecture, session management, matchmaking, and scaling.',
    type: ProgramContentType.REVEAL,
    body: networkingWeek14RevealMd,
    sortOrder: 0,
    isRequired: false,
    estimatedMinutes: 45,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14LectureContent: ProgramContent & { slug: string } = {
    id: 'week-14-lecture',
    slug: 'lecture',
    programId: 'networking-program-1',
    parentId: 'week-14',
    title: 'Lecture 14: Server Architecture and Session Management',
    description:
        'Core concepts: authority models, dedicated vs listen servers, rollback networking, session management, matchmaking, scaling, distributed systems foundations, and architecture decision patterns.',
    type: 0, // Page
    body: networkingWeek14LectureMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14AuthorityModels: ProgramContent & { slug: string } = {
    id: 'week-14-authority-models',
    slug: 'authority-models',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '1. Authority Models: Who Owns the Truth?',
    description: 'Client vs server authority, distributed authority, genre fitness, and the trust boundary.',
    type: 0, // Page
    body: networkingWeek14AuthorityModelsMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14DedicatedVsListenServers: ProgramContent & { slug: string } = {
    id: 'week-14-dedicated-vs-listen-servers',
    slug: 'dedicated-vs-listen-servers',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '2. Dedicated vs Listen Servers',
    description: 'Server types, host advantage, host migration, hybrid models, and decision framework.',
    type: 0, // Page
    body: networkingWeek14DedicatedVsListenServersMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14RollbackNetworking: ProgramContent & { slug: string } = {
    id: 'week-14-rollback-networking',
    slug: 'rollback-networking',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '3. Rollback Networking Concepts',
    description: 'Rollback algorithm, determinism requirements, state save/restore, fighting vs shooter rollback.',
    type: 0, // Page
    body: networkingWeek14RollbackNetworkingMd,
    sortOrder: 3,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14SessionManagement: ProgramContent & { slug: string } = {
    id: 'week-14-session-management',
    slug: 'session-management',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '4. Session Management and Connection Lifecycle',
    description: 'Session lifecycle, discovery, connection brokering, and platform services.',
    type: 0, // Page
    body: networkingWeek14SessionManagementMd,
    sortOrder: 4,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14Matchmaking: ProgramContent & { slug: string } = {
    id: 'week-14-matchmaking',
    slug: 'matchmaking',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '5. Matchmaking: Finding Fair, Fast, Fun Games',
    description: 'Skill rating systems, expanding windows, matchmaking architecture, and population health.',
    type: 0, // Page
    body: networkingWeek14MatchmakingMd,
    sortOrder: 5,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14ScalingGameServers: ProgramContent & { slug: string } = {
    id: 'week-14-scaling-game-servers',
    slug: 'scaling-game-servers',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '6. Scaling Game Servers',
    description: 'Stateful vs stateless, fleet management, Agones/PlayFab, multi-region, and monitoring.',
    type: 0, // Page
    body: networkingWeek14ScalingGameServersMd,
    sortOrder: 6,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14DistributedSystemsFoundations: ProgramContent & { slug: string } = {
    id: 'week-14-distributed-systems-foundations',
    slug: 'distributed-systems-foundations',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '7. Distributed Systems Foundations for Game Networking',
    description: 'Consensus, failure detection, CAP theorem, replication strategies, and coordination avoidance.',
    type: 0, // Page
    body: networkingWeek14DistributedSystemsFoundationsMd,
    sortOrder: 7,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14ArchitectureDecisionPatterns: ProgramContent & { slug: string } = {
    id: 'week-14-architecture-decision-patterns',
    slug: 'architecture-decision-patterns',
    programId: 'networking-program-1',
    parentId: 'week-14-lecture',
    title: '8. Architecture Decision Patterns: Putting It All Together',
    description: 'Genre-driven patterns, decision flow, common mistakes, and cost estimation frameworks.',
    type: 0, // Page
    body: networkingWeek14ArchitectureDecisionPatternsMd,
    sortOrder: 8,
    isRequired: true,
    estimatedMinutes: 15,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14LectureContent,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek14Readings: ProgramContent & { slug: string } = {
    id: 'week-14-readings',
    slug: 'readings',
    programId: 'networking-program-1',
    parentId: 'week-14',
    title: 'Readings',
    description: 'Required readings on authority models, rollback networking, matchmaking, server scaling, and distributed systems.',
    type: 0, // Page
    body: networkingWeek14ReadingsMd,
    sortOrder: 2,
    isRequired: true,
    estimatedMinutes: 124,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek14Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek14Intro.children = [
    networkingWeek14LectureSlides,
    networkingWeek14LectureContent,
    networkingWeek14Readings,
    networkingWeek14Assignment,
];
networkingWeek14LectureContent.children = [
    networkingWeek14AuthorityModels,
    networkingWeek14DedicatedVsListenServers,
    networkingWeek14RollbackNetworking,
    networkingWeek14SessionManagement,
    networkingWeek14Matchmaking,
    networkingWeek14ScalingGameServers,
    networkingWeek14DistributedSystemsFoundations,
    networkingWeek14ArchitectureDecisionPatterns,
];

// Week 15 — NAT Traversal and Security + Checkpoint 6: Peer Evaluation & Code Freeze
export const networkingWeek15Intro: ProgramContent & { slug: string } = {
    id: 'week-15',
    slug: 'week-15',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 15 — NAT Traversal and Security',
    description: 'NAT types, hole punching, STUN/TURN/ICE, network security, encryption, anti-cheat.',
    type: 0, // Page
    body: '# Week 15 — NAT Traversal and Security\n\nThis week covers NAT types, hole punching, STUN/TURN/ICE concepts, network security, encryption basics, authentication, and anti-cheat principles. See the lecture and readings for details.\n\n**Project Milestone 06:** Peer evaluation & code freeze.',
    sortOrder: 15,
    isRequired: true,
    estimatedMinutes: 90,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek15Assignment: ProgramContent & { slug: string } = {
    id: 'week-15-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-15',
    title: 'Checkpoint 6: Peer Evaluation & Code Freeze',
    description: 'Peer code review, technical essay draft, and code freeze.',
    type: 0, // Page
    body: networkingWeek15AssignmentMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 180,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek15Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek15Intro.children = [networkingWeek15Assignment];

// Week 16 — Final Presentations + Checkpoint 7
export const networkingWeek16Intro: ProgramContent & { slug: string } = {
    id: 'week-16',
    slug: 'week-16',
    programId: 'networking-program-1',
    parentId: undefined,
    title: 'Week 16 — Final Project Delivery',
    description: 'Final presentations, live demos, and all deliverables due.',
    type: 0, // Page
    body: '# Week 16 — Final Project Delivery\n\nThis is the final week. Each team delivers a 10-minute presentation with live demo, followed by 5-minute Q&A. All final deliverables are due by Thursday 2026/04/30.',
    sortOrder: 16,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: undefined,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

export const networkingWeek16Assignment: ProgramContent & { slug: string } = {
    id: 'week-16-assignment',
    slug: 'assignment',
    programId: 'networking-program-1',
    parentId: 'week-16',
    title: 'Checkpoint 7: Final Presentations',
    description: 'Final presentations, live demos, and all deliverables submission.',
    type: 0, // Page
    body: networkingWeek16AssignmentMd,
    sortOrder: 1,
    isRequired: true,
    estimatedMinutes: 60,
    visibility: 1, // Published
    program: networkingProgram,
    parent: networkingWeek16Intro,
    children: [],
    contentInteractions: [],
    createdAt: '2023-01-01T00:00:00Z',
    updatedAt: '2023-01-01T00:00:00Z',
};

networkingWeek16Intro.children = [networkingWeek16Assignment];

networkingProgram.programContents = [networkingSyllabusContent, networkingWeek01Intro, networkingWeek02Intro, networkingWeek03Intro, networkingWeek04Intro, networkingWeek05Intro, networkingWeek06Intro, networkingWeek07Intro, networkingWeek09FinalProjectContent, networkingWeek10Intro, networkingWeek11Intro, networkingWeek12Intro, networkingWeek13Intro, networkingWeek14Intro, networkingWeek15Intro, networkingWeek16Intro];
networkingProduct.productPrograms = [networkingProductProgram];

export default networkingProgram;
