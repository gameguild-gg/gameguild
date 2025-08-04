// =============================================================================
// TESTING LAB TYPES
// =============================================================================

// Re-export types from the generated API
export type { TestingSession, TestingRequest, TestingFeedback, SessionStatus } from '@/lib/api/generated/types.gen';

// Import for local use
import type { TestingSession } from '@/lib/api/generated/types.gen';

// Alias for backward compatibility
export type TestSession = TestingSession;

// SessionStatus constants for easier use
export const SESSION_STATUS = {
  SCHEDULED: 0,
  ACTIVE: 1,
  COMPLETED: 2,
  CANCELLED: 3,
} as const;

// Helper function to convert API TestingSession to component-friendly format
export function adaptTestingSessionForComponent(session: TestingSession): TestSession & {
  title: string;
  slug: string;
  currentTesters: number;
  isOnline: boolean;
  description: string;
  currentGames: number;
  maxGames: number;
  sessionType: string;
  duration: number;
  platform: string[];
  rewards?: { value: number };
} {
  return {
    ...session,
    title: session.sessionName || 'Untitled Session',
    slug: session.id || '',
    currentTesters: session.registeredTesterCount || 0,
    isOnline: true, // Default to online since API doesn't have this field
    description: 'Testing session', // Default description since API doesn't have this field
    currentGames: 0, // Default value since API doesn't have this field
    maxGames: 1, // Default value since API doesn't have this field
    sessionType: 'general', // Default type since API doesn't have this field
    duration: 60, // Default duration since API doesn't have this field
    platform: ['PC'], // Default platform since API doesn't have this field
    rewards: undefined, // Default since API doesn't have this field
  };
}

export interface TestingAttendance {
  id: string;

  sessionId: string;

  userId: string;

  status: string;

  checkedInAt?: string;

  checkedOutAt?: string;
}
