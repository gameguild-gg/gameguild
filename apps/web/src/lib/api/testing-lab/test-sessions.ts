export interface TestSession {
  id: string;
  slug: string;
  title: string;
  description: string;
  gameTitle: string;
  gameDeveloper: string;
  sessionDate: string;
  duration: number; // in minutes
  maxTesters: number;
  currentTesters: number;
  maxGames: number; // Maximum number of games that can be tested in this session
  currentGames: number; // Current number of games registered for testing
  requirements: string[];
  status: 'open' | 'full' | 'in-progress' | 'completed';
  sessionType: 'gameplay' | 'usability' | 'feedback' | 'bug-testing';
  platform: string[];
  skillLevel: 'beginner' | 'intermediate' | 'advanced' | 'any';
  isOnline: boolean; // true for online sessions, false for on-site sessions
  rewards?: {
    type: 'credits' | 'certificate' | 'game-key' | 'feedback';
    value: string;
  };
  featuredGames?: FeaturedGame[]; // Additional games being tested in this session
}

export interface FeaturedGame {
  id: string;
  title: string;
  developer: string;
  description: string;
  genre: string[];
  platform: string[];
  bannerImage?: string;
  status: 'primary' | 'secondary' | 'bonus';
  testingFocus: string[];
}

// Mock data for now - in production this would fetch from your API
export async function getAvailableTestSessions(): Promise<TestSession[]> {
  // Simulate API delay
  await new Promise((resolve) => setTimeout(resolve, 100));

  return [
    {
      id: '1',
      slug: 'stellar-odyssey-alpha-gameplay',
      title: 'Alpha Gameplay Testing',
      description: 'Test core mechanics and provide feedback on game flow and difficulty balance.',
      gameTitle: 'Stellar Odyssey',
      gameDeveloper: 'Team Alpha Studios',
      sessionDate: '2025-07-20T14:00:00Z',
      duration: 90,
      maxTesters: 8,
      currentTesters: 3,
      maxGames: 3,
      currentGames: 1,
      requirements: ['PC with Windows 10+', 'Reliable internet connection', 'Gaming experience'],
      status: 'open',
      sessionType: 'gameplay',
      platform: ['PC', 'Steam'],
      skillLevel: 'intermediate',
      isOnline: true,
      rewards: {
        type: 'credits',
        value: '500 Testing Credits',
      },
      featuredGames: [
        {
          id: 'stellar-1',
          title: 'Stellar Odyssey',
          developer: 'Team Alpha Studios',
          description: 'Epic space exploration RPG with stunning visuals and immersive gameplay mechanics.',
          genre: ['RPG', 'Space', 'Adventure'],
          platform: ['PC', 'Steam'],
          status: 'primary',
          testingFocus: ['Combat System', 'Space Navigation', 'UI/UX']
        },
        {
          id: 'cosmic-2',
          title: 'Cosmic Raiders',
          developer: 'Nebula Interactive',
          description: 'Fast-paced space combat with procedural generation and multiplayer battles.',
          genre: ['Action', 'Shooter', 'Multiplayer'],
          platform: ['PC', 'Steam'],
          status: 'secondary',
          testingFocus: ['Multiplayer Balance', 'Performance', 'Matchmaking']
        },
        {
          id: 'void-3',
          title: 'Void Miners',
          developer: 'Deep Space Games',
          description: 'Resource management and base building in the depths of space.',
          genre: ['Strategy', 'Simulation', 'Building'],
          platform: ['PC'],
          status: 'bonus',
          testingFocus: ['Resource Systems', 'Building Mechanics', 'Tutorial']
        }
      ]
    },
    {
      id: '2',
      slug: 'puzzle-quest-mobile-ux-study',
      title: 'UI/UX Usability Study',
      description: 'Evaluate user interface design and user experience flow for mobile puzzle game.',
      gameTitle: 'Puzzle Quest Mobile',
      gameDeveloper: 'Indie Game Collective',
      sessionDate: '2025-07-21T16:30:00Z',
      duration: 60,
      maxTesters: 6,
      currentTesters: 6,
      maxGames: 1,
      currentGames: 1,
      requirements: ['Mobile device (iOS or Android)', 'Quiet testing environment'],
      status: 'full',
      sessionType: 'usability',
      platform: ['iOS', 'Android'],
      skillLevel: 'any',
      isOnline: false,
      rewards: {
        type: 'certificate',
        value: 'UX Testing Certificate',
      },
    },
    {
      id: '3',
      slug: 'arena-champions-multiplayer-beta',
      title: 'Multiplayer Beta Testing',
      description: 'Test online multiplayer features, server stability, and competitive balance.',
      gameTitle: 'Arena Champions',
      gameDeveloper: 'Competitive Gaming Labs',
      sessionDate: '2025-07-22T19:00:00Z',
      duration: 120,
      maxTesters: 12,
      currentTesters: 8,
      maxGames: 5,
      currentGames: 2,
      requirements: ['High-speed internet', 'Discord for communication', 'Competitive gaming experience'],
      status: 'open',
      sessionType: 'gameplay',
      platform: ['PC', 'PlayStation', 'Xbox'],
      skillLevel: 'advanced',
      isOnline: true,
      rewards: {
        type: 'game-key',
        value: 'Free game copy on release',
      },
    },
    {
      id: '4',
      slug: 'adventure-quest-accessibility-testing',
      title: 'Accessibility Testing Session',
      description: 'Test game accessibility features and provide feedback on inclusivity options.',
      gameTitle: 'Adventure Quest',
      gameDeveloper: 'Inclusive Games Studio',
      sessionDate: '2025-07-23T13:00:00Z',
      duration: 75,
      maxTesters: 5,
      currentTesters: 2,
      maxGames: 2,
      currentGames: 1,
      requirements: ['Experience with accessibility tools', 'PC or console'],
      status: 'open',
      sessionType: 'usability',
      platform: ['PC', 'PlayStation', 'Xbox'],
      skillLevel: 'any',
      isOnline: false,
      rewards: {
        type: 'feedback',
        value: 'Developer feedback session',
      },
    },
    {
      id: '5',
      slug: 'mystic-realms-bug-hunting',
      title: 'Bug Hunting Session',
      description: 'Systematically test for bugs, glitches, and edge cases in pre-release build.',
      gameTitle: 'Mystic Realms',
      gameDeveloper: 'Fantasy World Games',
      sessionDate: '2025-07-24T10:00:00Z',
      duration: 180,
      maxTesters: 10,
      currentTesters: 7,
      maxGames: 10,
      currentGames: 4,
      requirements: ['Attention to detail', 'Bug reporting experience', 'PC gaming setup'],
      status: 'open',
      sessionType: 'bug-testing',
      platform: ['PC'],
      skillLevel: 'intermediate',
      isOnline: true,
      rewards: {
        type: 'credits',
        value: '75 testing credits + Bug bounty',
      },
    },
  ];
}

export async function getTestSession(id: string): Promise<TestSession | null> {
  const sessions = await getAvailableTestSessions();
  return sessions.find((session) => session.id === id) || null;
}

export async function getTestSessionBySlug(slug: string): Promise<TestSession | null> {
  const sessions = await getAvailableTestSessions();
  return sessions.find((session) => session.slug === slug) || null;
}
