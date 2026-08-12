import { Product, ProductProgram, Program, ProgramContent } from '@/lib/api/generated';

// Markdown content imports
import gamePublishingNintendoSwitch from './console/nintendo-store/guide.md';
import gamePublishingOculusQuest from './console/oculus-quest/guide.md';
import gamePublishingPlaystationStore from './console/playstation-store/guide.md';
import gamePublishingXbox from './console/xbox/guide.md';
import gamePublishingEpicGamesStore from './desktop/epic-games-store/guide.md';
import gamePublishingGog from './desktop/gog/guide.md';
import gamePublishingMicrosoftStore from './desktop/microsoft-store/guide.md';
import gamePublishingSteam from './desktop/steam/guide.md';
import gamePublishingCrazyGames from './html5/crazy-games/guide.md';
import gamePublishingFirebaseHosting from './html5/firebase-hosting/guide.md';
import gamePublishingGithubPages from './html5/github-pages/guide.md';
import gamePublishingItchio from './html5/itchio/guide.md';
import gamePublishingNetlify from './html5/netlify/guide.md';
import gamePublishingPoki from './html5/poki/guide.md';
import gamePublishingSelfHosting from './html5/self-hosting/guide.md';
import gamePublishingVercel from './html5/vercel/guide.md';
import gamePublishingAppStore from './mobile/app-store/guide.md';
import gamePublishingGooglePlay from './mobile/google-play/guide.md';
import gamePublishingSyllabus from './syllabus.md';

// Program definition
export const gamePublishingProgram: Program = {
  id: 'game-publishing-program-1',
  title: 'Game Publishing Mastery',
  description:
    'Comprehensive guide to publishing games across all major platforms including Steam, mobile app stores, console platforms, and web publishing solutions. Learn the complete publishing pipeline from submission to marketing.',
  slug: 'game-publishing',
  thumbnail:
    'https://images.unsplash.com/photo-1556075798-4825dfaaf498?w=400&h=300&fit=crop',
  videoShowcaseUrl: null,
  estimatedHours: 25,
  enrollmentStatus: 0, // Open
  maxEnrollments: null,
  enrollmentDeadline: null,
  category: 2, // Business
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

// Course content
const gamePublishingSyllabusContent: ProgramContent = {
  id: 'game-publishing-syllabus',
  programId: 'game-publishing-program-1',
  parentId: undefined,
  title: 'Course Syllabus',
  description: 'Game Publishing course syllabus and overview',
  type: 0,
  body: gamePublishingSyllabus,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: undefined,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingDesktopContent: ProgramContent = {
  id: 'game-publishing-desktop',
  programId: 'game-publishing-program-1',
  parentId: undefined,
  title: 'Desktop Publishing',
  description: 'Publishing games on desktop platforms',
  type: 0,
  body: 'Desktop publishing platforms overview...',
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: undefined,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingMobileContent: ProgramContent = {
  id: 'game-publishing-mobile',
  programId: 'game-publishing-program-1',
  parentId: undefined,
  title: 'Mobile Publishing',
  description: 'Publishing games on mobile platforms',
  type: 0,
  body: 'Mobile publishing platforms overview...',
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: undefined,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingConsoleContent: ProgramContent = {
  id: 'game-publishing-console',
  programId: 'game-publishing-program-1',
  parentId: undefined,
  title: 'Console Publishing',
  description: 'Publishing games on console platforms',
  type: 0,
  body: 'Console publishing platforms overview...',
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: undefined,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingHtml5Content: ProgramContent = {
  id: 'game-publishing-html5',
  programId: 'game-publishing-program-1',
  parentId: undefined,
  title: 'HTML5 Publishing',
  description: 'Publishing HTML5 games on web platforms',
  type: 0,
  body: 'HTML5 publishing platforms overview...',
  sortOrder: 5,
  isRequired: true,
  estimatedMinutes: 15,
  visibility: 1,
  program: undefined,
  parent: undefined,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingSteamContent: ProgramContent = {
  id: 'game-publishing-steam',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-desktop',
  title: 'Steam Publishing Guide',
  description: 'Complete guide to publishing games on Steam',
  type: 0,
  body: gamePublishingSteam,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: undefined,
  parent: gamePublishingDesktopContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingEpicGamesStoreContent: ProgramContent = {
  id: 'game-publishing-epic-games-store',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-desktop',
  title: 'Epic Games Store Publishing Guide',
  description: 'Guide to publishing games on Epic Games Store',
  type: 0,
  body: gamePublishingEpicGamesStore,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 40,
  visibility: 1,
  program: undefined,
  parent: gamePublishingDesktopContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingGogContent: ProgramContent = {
  id: 'game-publishing-gog',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-desktop',
  title: 'GOG Publishing Guide',
  description: 'Guide to publishing DRM-free games on GOG',
  type: 0,
  body: gamePublishingGog,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 35,
  visibility: 1,
  program: undefined,
  parent: gamePublishingDesktopContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingMicrosoftStoreContent: ProgramContent = {
  id: 'game-publishing-microsoft-store',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-desktop',
  title: 'Microsoft Store Publishing Guide',
  description: 'Guide to publishing games on Microsoft Store',
  type: 0,
  body: gamePublishingMicrosoftStore,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 40,
  visibility: 1,
  program: undefined,
  parent: gamePublishingDesktopContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingGooglePlayContent: ProgramContent = {
  id: 'game-publishing-google-play',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-mobile',
  title: 'Google Play Store Publishing Guide',
  description: 'Guide to publishing mobile games on Google Play Store',
  type: 0,
  body: gamePublishingGooglePlay,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: undefined,
  parent: gamePublishingMobileContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingAppStoreContent: ProgramContent = {
  id: 'game-publishing-app-store',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-mobile',
  title: 'Apple App Store Publishing Guide',
  description: 'Guide to publishing mobile games on Apple App Store',
  type: 0,
  body: gamePublishingAppStore,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: undefined,
  parent: gamePublishingMobileContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingNintendoSwitchContent: ProgramContent = {
  id: 'game-publishing-nintendo-switch',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-console',
  title: 'Nintendo Store Publishing Guide',
  description: 'Guide to publishing games on Nintendo Store',
  type: 0,
  body: gamePublishingNintendoSwitch,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 50,
  visibility: 1,
  program: undefined,
  parent: gamePublishingConsoleContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingPlaystationStoreContent: ProgramContent = {
  id: 'game-publishing-playstation-store',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-console',
  title: 'PlayStation Store Publishing Guide',
  description: 'Guide to publishing games on PlayStation Store',
  type: 0,
  body: gamePublishingPlaystationStore,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 50,
  visibility: 1,
  program: undefined,
  parent: gamePublishingConsoleContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingXboxContent: ProgramContent = {
  id: 'game-publishing-xbox',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-console',
  title: 'Xbox Store Publishing Guide',
  description: 'Guide to publishing games on Xbox Store',
  type: 0,
  body: gamePublishingXbox,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 45,
  visibility: 1,
  program: undefined,
  parent: gamePublishingConsoleContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingOculusQuestContent: ProgramContent = {
  id: 'game-publishing-oculus-quest',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-console',
  title: 'Oculus Quest Store Publishing Guide',
  description: 'Guide to publishing VR games on Oculus Quest Store',
  type: 0,
  body: gamePublishingOculusQuest,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 40,
  visibility: 1,
  program: undefined,
  parent: gamePublishingConsoleContent,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingPokiContent: ProgramContent = {
  id: 'game-publishing-poki',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'Poki Publishing Guide',
  description: 'Guide to publishing HTML5 games on Poki',
  type: 0,
  body: gamePublishingPoki,
  sortOrder: 1,
  isRequired: true,
  estimatedMinutes: 35,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingCrazyGamesContent: ProgramContent = {
  id: 'game-publishing-crazy-games',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'CrazyGames Publishing Guide',
  description: 'Guide to publishing HTML5 games on CrazyGames',
  type: 0,
  body: gamePublishingCrazyGames,
  sortOrder: 2,
  isRequired: true,
  estimatedMinutes: 35,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingSelfHostingContent: ProgramContent = {
  id: 'game-publishing-self-hosting',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'Self-Hosting Games Guide',
  description: 'Guide to self-hosting HTML5 games',
  type: 0,
  body: gamePublishingSelfHosting,
  sortOrder: 3,
  isRequired: true,
  estimatedMinutes: 40,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingGithubPagesContent: ProgramContent = {
  id: 'game-publishing-github-pages',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'GitHub Pages Hosting Guide',
  description: 'Guide to hosting HTML5 games on GitHub Pages',
  type: 0,
  body: gamePublishingGithubPages,
  sortOrder: 4,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingFirebaseHostingContent: ProgramContent = {
  id: 'game-publishing-firebase-hosting',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'Firebase Hosting Guide',
  description: 'Guide to hosting HTML5 games on Firebase Hosting',
  type: 0,
  body: gamePublishingFirebaseHosting,
  sortOrder: 5,
  isRequired: true,
  estimatedMinutes: 35,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingNetlifyContent: ProgramContent = {
  id: 'game-publishing-netlify',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'Netlify Hosting Guide',
  description: 'Guide to hosting HTML5 games on Netlify',
  type: 0,
  body: gamePublishingNetlify,
  sortOrder: 6,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingVercelContent: ProgramContent = {
  id: 'game-publishing-vercel',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'Vercel Hosting Guide',
  description: 'Guide to hosting HTML5 games on Vercel',
  type: 0,
  body: gamePublishingVercel,
  sortOrder: 7,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

const gamePublishingItchioContent: ProgramContent = {
  id: 'game-publishing-itchio',
  programId: 'game-publishing-program-1',
  parentId: 'game-publishing-html5',
  title: 'Itch.io Publishing Guide',
  description: 'Guide to publishing HTML5 games on Itch.io',
  type: 0,
  body: gamePublishingItchio,
  sortOrder: 8,
  isRequired: true,
  estimatedMinutes: 30,
  visibility: 1,
  program: undefined,
  parent: gamePublishingHtml5Content,
  children: [],
  contentInteractions: [],
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

// Wire program contents
gamePublishingProgram.programContents = [
  gamePublishingSyllabusContent,
  // Categories
  gamePublishingDesktopContent,
  gamePublishingMobileContent,
  gamePublishingConsoleContent,
  gamePublishingHtml5Content,
  // Desktop platforms
  gamePublishingSteamContent,
  gamePublishingEpicGamesStoreContent,
  gamePublishingGogContent,
  gamePublishingMicrosoftStoreContent,
  // Console platforms
  gamePublishingPlaystationStoreContent,
  gamePublishingNintendoSwitchContent,
  gamePublishingXboxContent,
  gamePublishingOculusQuestContent,
  // HTML5 platforms
  gamePublishingPokiContent,
  gamePublishingCrazyGamesContent,
  gamePublishingSelfHostingContent,
  gamePublishingGithubPagesContent,
  gamePublishingFirebaseHostingContent,
  gamePublishingNetlifyContent,
  gamePublishingVercelContent,
  gamePublishingItchioContent,
  // Mobile platforms
  gamePublishingGooglePlayContent,
  gamePublishingAppStoreContent,
];

// Set program references
gamePublishingSyllabusContent.program = gamePublishingProgram;
gamePublishingSteamContent.program = gamePublishingProgram;
gamePublishingEpicGamesStoreContent.program = gamePublishingProgram;
gamePublishingGogContent.program = gamePublishingProgram;
gamePublishingMicrosoftStoreContent.program = gamePublishingProgram;
gamePublishingItchioContent.program = gamePublishingProgram;
gamePublishingPlaystationStoreContent.program = gamePublishingProgram;
gamePublishingNintendoSwitchContent.program = gamePublishingProgram;
gamePublishingXboxContent.program = gamePublishingProgram;
gamePublishingOculusQuestContent.program = gamePublishingProgram;
gamePublishingPokiContent.program = gamePublishingProgram;
gamePublishingCrazyGamesContent.program = gamePublishingProgram;
gamePublishingSelfHostingContent.program = gamePublishingProgram;
gamePublishingGithubPagesContent.program = gamePublishingProgram;
gamePublishingFirebaseHostingContent.program = gamePublishingProgram;
gamePublishingNetlifyContent.program = gamePublishingProgram;
gamePublishingVercelContent.program = gamePublishingProgram;
gamePublishingGooglePlayContent.program = gamePublishingProgram;
gamePublishingAppStoreContent.program = gamePublishingProgram;

// Set program references for platform categories
gamePublishingDesktopContent.program = gamePublishingProgram;
gamePublishingMobileContent.program = gamePublishingProgram;
gamePublishingConsoleContent.program = gamePublishingProgram;
gamePublishingHtml5Content.program = gamePublishingProgram;

// Parent-child relationships
gamePublishingDesktopContent.children = [
  gamePublishingSteamContent,
  gamePublishingEpicGamesStoreContent,
  gamePublishingGogContent,
  gamePublishingMicrosoftStoreContent,
];

gamePublishingMobileContent.children = [
  gamePublishingGooglePlayContent,
  gamePublishingAppStoreContent,
];

gamePublishingConsoleContent.children = [
  gamePublishingNintendoSwitchContent,
  gamePublishingPlaystationStoreContent,
  gamePublishingXboxContent,
  gamePublishingOculusQuestContent,
];

gamePublishingHtml5Content.children = [
  gamePublishingPokiContent,
  gamePublishingCrazyGamesContent,
  gamePublishingSelfHostingContent,
  gamePublishingGithubPagesContent,
  gamePublishingFirebaseHostingContent,
  gamePublishingNetlifyContent,
  gamePublishingVercelContent,
  gamePublishingItchioContent,
];

// Product definitions
export const gamePublishingProduct: Product = {
  id: 'game-publishing-product-1',
  name: 'Game Publishing Mastery',
  title: 'Game Publishing Mastery',
  description:
    'Master the art of game publishing across multiple platforms including Steam, mobile stores, console platforms, and web distribution channels.',
  slug: 'game-publishing',
  imageUrl:
    'https://placehold.co/400x225/1f2937/ffffff.png?text=Game+Publishing',
  visibility: 0, // Public
  status: 1, // Published
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
  productPrograms: [],
  userProducts: [],
  promoCodes: [],
};

export const gamePublishingProductProgram: ProductProgram = {
  id: 'game-publishing-product-program-1',
  productId: 'game-publishing-product-1',
  product: gamePublishingProduct,
  programId: 'game-publishing-program-1',
  program: gamePublishingProgram,
  sortOrder: 1,
  createdAt: '2023-01-01T00:00:00Z',
  updatedAt: '2023-01-01T00:00:00Z',
};

gamePublishingProduct.productPrograms = [gamePublishingProductProgram];

export default gamePublishingProgram;