import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Intro to Game Programming markdown content
import intro2gproSyllabus from './syllabus.md';
import intro2gproExpectations from './week01/expectations.md';
import intro2gproInterview from './week01/interview-a-gamedev.md';
import intro2gproGamedevTools from './week02/gamedev-tools.md';
import intro2gproGamedevCareers from './week03/gamedev-careers.md';
import intro2gproGamedevIssues from './week04/gamedev-issues.md';
import intro2gproGamedevIssuesPresentations from './week05/gamedev-issues-presentations.md';
import intro2gproAutomation from './week06/automation.md';
import intro2gproUnityPlatformer from './week07/unity-platformer.md';
import intro2gproAssignment from './week08/assignment.md';
import intro2gproProduction from './week08/production.md';
import intro2gproGameMechanics from './week09/game-mechanics.md';
import intro2gproTestingSession from './week10/testing-session.md';

// Program
export const intro2gproProgram: Program = {
  id: 'intro2gpro-program-1',
  title: 'Introduction to Game Programming',
  description:
    'Students will be introduced to and familiarized with their roles as Game Programmers. The course explores the various disciplines and vocations within game programming, provides an overview of the skills that make a game programmer successful, and presents both industry and academic contexts for their duties.',
  slug: 'intro2gpro',
  thumbnail: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Intro+to+Game+Programming',
  videoShowcaseUrl: null,
  estimatedHours: 45,
  enrollmentStatus: 0,
  maxEnrollments: null,
  enrollmentDeadline: null,
  category: 1,
  difficulty: 0,
  visibility: 0,
  status: 1,
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
  programContents: [],
  programUsers: [],
  programRatings: [],
  programWishlists: [],
};

// Product
export const intro2gproProduct: Product = {
  id: 'intro2gpro-product-1',
  title: 'Introduction to Game Programming Course',
  name: 'Introduction to Game Programming',
  description:
    'Learn the fundamentals of game programming and explore various disciplines within game development',
  shortDescription: 'Get introduced to game programming roles, tools, and industry contexts',
  imageUrl: 'https://placehold.co/400x225/1f2937/ffffff.png?text=Intro+to+Game+Programming',
  type: 0,
  isBundle: false,
  creatorId: '1',
  bundleItems: null,
  referralCommissionPercentage: 0,
  maxAffiliateDiscount: 0,
  affiliateCommissionPercentage: 0,
  visibility: 0,
  status: 1,
  slug: 'intro2gpro',
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
  productPrograms: [],
  productPricings: [],
  subscriptionPlans: [],
  userProducts: [],
  promoCodes: [],
};

// ProductProgram relation
export const intro2gproProductProgram: ProductProgram = {
  id: 'intro2gpro-product-program-1',
  productId: 'intro2gpro-product-1',
  product: intro2gproProduct,
  programId: 'intro2gpro-program-1',
  program: intro2gproProgram,
  sortOrder: 5,
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

// Course contents
const intro2gproSyllabusContent: ProgramContent = {
  id: 'intro2gpro-syllabus',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Course Syllabus',
  description: 'Introduction to Game Programming course syllabus',
  type: 0,
  body: intro2gproSyllabus,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproExpectationsContent: ProgramContent = {
  id: 'intro2gpro-expectations',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 01: Course Expectations Report',
  description:
    'Student expectations and feedback analysis for the Introduction to Game Programming course',
  type: 0,
  body: intro2gproExpectations,
  sortOrder: 2,
  isRequired: false,
  estimatedMinutes: 15,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproInterviewContent: ProgramContent = {
  id: 'intro2gpro-interview',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 01: Interview a Game Developer',
  description:
    'Assignment to interview a game programmer or developer and learn about their experiences and advice',
  type: 0,
  body: intro2gproInterview,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproGamedevToolsContent: ProgramContent = {
  id: 'intro2gpro-gamedev-tools',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 02: Game Development Tools',
  description: 'A brief overview of the most common tools for game development.',
  type: 0,
  body: intro2gproGamedevTools,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproGamedevCareersContent: ProgramContent = {
  id: 'intro2gpro-gamedev-careers',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 03: Game Development Careers',
  description:
    'Research and explore various career pathways in game development, including programming disciplines and job opportunities.',
  type: 0,
  body: intro2gproGamedevCareers,
  sortOrder: 5,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproGamedevIssuesContent: ProgramContent = {
  id: 'intro2gpro-gamedev-issues',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 04: Game Development Issues',
  description:
    'Explore common issues and challenges in the game development industry and learn how to address them.',
  type: 0,
  body: intro2gproGamedevIssues,
  sortOrder: 6,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproGamedevIssuesPresentationsContent: ProgramContent = {
  id: 'intro2gpro-gamedev-issues-presentations',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 05: Game Development Issues Presentations',
  description:
    'Students present their research about game development issues in peer-evaluated presentations.',
  type: 0,
  body: intro2gproGamedevIssuesPresentations,
  sortOrder: 7,
  isRequired: true,
  estimatedMinutes: 120,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproAutomationContent: ProgramContent = {
  id: 'intro2gpro-automation',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 06: Automation in Game Development',
  description:
    'Comprehensive guide to automation tools and practices in game development, including version control systems, CI/CD, and development workflows.',
  type: 0,
  body: intro2gproAutomation,
  sortOrder: 8,
  isRequired: true,
  estimatedMinutes: 180,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproUnityPlatformerContent: ProgramContent = {
  id: 'intro2gpro-unity-platformer',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 07: Building a 2D Platformer Game with Unity',
  description:
    'Create a basic 2D platformer game using Unity with player movement, collectibles, obstacles, and scene management.',
  type: 0,
  body: intro2gproUnityPlatformer,
  sortOrder: 9,
  isRequired: true,
  estimatedMinutes: 240,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproAssignmentContent: ProgramContent = {
  id: 'intro2gpro-assignment',
  programId: 'intro2gpro-program-1',
  parentId: 'intro2gpro-production',
  title: 'Week 08: Game Development Assignment',
  description: 'Hands-on assignment for game development concepts',
  type: 2,
  body: intro2gproAssignment,
  sortOrder: 10,
  isRequired: true,
  estimatedMinutes: 180,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproProductionContent: ProgramContent = {
  id: 'intro2gpro-production',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 08: Game Production',
  description: 'Game production processes and best practices',
  type: 0,
  body: intro2gproProduction,
  sortOrder: 11,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproGameMechanicsContent: ProgramContent = {
  id: 'intro2gpro-game-mechanics',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 09: Game Mechanics',
  description:
    'Explore game mechanics catalogs and incorporate selected mechanics into your game pitch.',
  type: 0,
  body: intro2gproGameMechanics,
  sortOrder: 12,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproTestingSessionContent: ProgramContent = {
  id: 'intro2gpro-testing-session',
  programId: 'intro2gpro-program-1',
  parentId: undefined,
  title: 'Week 10: Testing Session',
  description:
    'Instructions for publishing, playtesting, and preparing a core-mechanic prototype for QA.',
  type: 0,
  body: intro2gproTestingSession,
  sortOrder: 13,
  isRequired: true,
  estimatedMinutes: 90,
  visibility: 1,
  program: intro2gproProgram,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

// Child content for gamedev-issues
const intro2gproTechnicalChallengesContent: ProgramContent = {
  id: 'intro2gpro-technical-challenges',
  programId: 'intro2gpro-program-1',
  parentId: 'intro2gpro-gamedev-issues',
  title: 'Technical Challenges',
  description: 'Common technical challenges in game development',
  type: 0,
  body: 'Technical challenges content here...',
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 20,
  visibility: 1,
  program: intro2gproProgram,
  parent: intro2gproGamedevIssuesContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproDesignChallengesContent: ProgramContent = {
  id: 'intro2gpro-design-challenges',
  programId: 'intro2gpro-program-1',
  parentId: 'intro2gpro-gamedev-issues',
  title: 'Design Challenges',
  description: 'Common design challenges in game development',
  type: 0,
  body: 'Design challenges content here...',
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 20,
  visibility: 1,
  program: intro2gproProgram,
  parent: intro2gproGamedevIssuesContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const intro2gproBusinessChallengesContent: ProgramContent = {
  id: 'intro2gpro-business-challenges',
  programId: 'intro2gpro-program-1',
  parentId: 'intro2gpro-gamedev-issues',
  title: 'Business Challenges',
  description: 'Common business challenges in game development',
  type: 0,
  body: 'Business challenges content here...',
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 20,
  visibility: 1,
  program: intro2gproProgram,
  parent: intro2gproGamedevIssuesContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

// Wire up program contents
intro2gproProgram.programContents = [
  intro2gproSyllabusContent,
  intro2gproExpectationsContent,
  intro2gproInterviewContent,
  intro2gproGamedevToolsContent,
  intro2gproGamedevCareersContent,
  intro2gproGamedevIssuesContent,
  intro2gproGamedevIssuesPresentationsContent,
  intro2gproAutomationContent,
  intro2gproUnityPlatformerContent,
  intro2gproProductionContent,
  intro2gproAssignmentContent,
  intro2gproGameMechanicsContent,
  intro2gproTestingSessionContent,
  intro2gproTechnicalChallengesContent,
  intro2gproDesignChallengesContent,
  intro2gproBusinessChallengesContent,
];

// Set up parent-child relationships
intro2gproGamedevIssuesContent.children = [
  intro2gproTechnicalChallengesContent,
  intro2gproDesignChallengesContent,
  intro2gproBusinessChallengesContent,
];

// Set product-program relationship
intro2gproProduct.productPrograms = [intro2gproProductProgram];

export default intro2gproProgram;