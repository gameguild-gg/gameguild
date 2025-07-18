export interface TestSession {
  id: string;
  title: string;
  description: string;
  gameTitle: string;
  gameDeveloper: string;
  sessionDate: string;
  duration: number; // in minutes
  maxTesters: number;
  currentTesters: number;
  requirements: string[];
  status: 'open' | 'full' | 'in-progress' | 'completed';
  sessionType: 'gameplay' | 'usability' | 'feedback' | 'bug-testing';
  platform: string[];
  skillLevel: 'beginner' | 'intermediate' | 'advanced' | 'any';
  rewards?: {
    type: 'credits' | 'certificate' | 'game-key' | 'feedback';
    value: string;
  };
}

// Mock data for now - in production this would fetch from your API
export async function getAvailableTestSessions(): Promise<TestSession[]> {
  // Simulate API delay
  await new Promise(resolve => setTimeout(resolve, 100));

  return [
    {
      id: '1',
      title: 'Alpha Gameplay Testing',
      description: 'Test core mechanics and provide feedback on game flow and difficulty balance.',
      gameTitle: 'Stellar Odyssey',
      gameDeveloper: 'Team Alpha Studios',
      sessionDate: '2025-07-20T14:00:00Z',
      duration: 90,
      maxTesters: 8,
      currentTesters: 3,
      requirements: ['PC with Windows 10+', 'Reliable internet connection', 'Gaming experience'],
      status: 'open',
      sessionType: 'gameplay',
      platform: ['PC', 'Steam'],
      skillLevel: 'intermediate',
      rewards: {
        type: 'credits',
        value: '50 testing credits'
      }
    },
    {
      id: '2',
      title: 'UI/UX Usability Study',
      description: 'Evaluate user interface design and user experience flow for mobile puzzle game.',
      gameTitle: 'Puzzle Quest Mobile',
      gameDeveloper: 'Indie Game Collective',
      sessionDate: '2025-07-21T16:30:00Z',
      duration: 60,
      maxTesters: 6,
      currentTesters: 6,
      requirements: ['Mobile device (iOS or Android)', 'Quiet testing environment'],
      status: 'full',
      sessionType: 'usability',
      platform: ['iOS', 'Android'],
      skillLevel: 'any',
      rewards: {
        type: 'certificate',
        value: 'UX Testing Certificate'
      }
    },
    {
      id: '3',
      title: 'Multiplayer Beta Testing',
      description: 'Test online multiplayer features, server stability, and competitive balance.',
      gameTitle: 'Arena Champions',
      gameDeveloper: 'Competitive Gaming Labs',
      sessionDate: '2025-07-22T19:00:00Z',
      duration: 120,
      maxTesters: 12,
      currentTesters: 8,
      requirements: ['High-speed internet', 'Discord for communication', 'Competitive gaming experience'],
      status: 'open',
      sessionType: 'gameplay',
      platform: ['PC', 'PlayStation', 'Xbox'],
      skillLevel: 'advanced',
      rewards: {
        type: 'game-key',
        value: 'Free game copy on release'
      }
    },
    {
      id: '4',
      title: 'Accessibility Testing Session',
      description: 'Test game accessibility features and provide feedback on inclusivity options.',
      gameTitle: 'Adventure Quest',
      gameDeveloper: 'Inclusive Games Studio',
      sessionDate: '2025-07-23T13:00:00Z',
      duration: 75,
      maxTesters: 5,
      currentTesters: 2,
      requirements: ['Experience with accessibility tools', 'PC or console'],
      status: 'open',
      sessionType: 'usability',
      platform: ['PC', 'PlayStation', 'Xbox'],
      skillLevel: 'any',
      rewards: {
        type: 'feedback',
        value: 'Developer feedback session'
      }
    },
    {
      id: '5',
      title: 'Bug Hunting Session',
      description: 'Systematically test for bugs, glitches, and edge cases in pre-release build.',
      gameTitle: 'Mystic Realms',
      gameDeveloper: 'Fantasy World Games',
      sessionDate: '2025-07-24T10:00:00Z',
      duration: 180,
      maxTesters: 10,
      currentTesters: 7,
      requirements: ['Attention to detail', 'Bug reporting experience', 'PC gaming setup'],
      status: 'open',
      sessionType: 'bug-testing',
      platform: ['PC'],
      skillLevel: 'intermediate',
      rewards: {
        type: 'credits',
        value: '75 testing credits + Bug bounty'
      }
    }
  ];
}

export async function getTestSession(id: string): Promise<TestSession | null> {
  const sessions = await getAvailableTestSessions();
  return sessions.find(session => session.id === id) || null;
}
