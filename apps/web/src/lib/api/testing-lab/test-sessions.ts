/**
 * Stub for testing lab test sessions API.
 */

export interface FeaturedGame {
    id: string;
    name?: string;
    title?: string;
    developer?: string;
    description?: string;
    genre?: string[];
    status?: string;
    testingFocus?: string[];
    platforms?: string[];
    platform?: string[];
    imageUrl?: string;
}

export interface TestSession {
    id: string;
    slug: string;
    name: string;
    title: string;
    description: string;
    status: 'pending' | 'in-progress' | 'completed' | 'cancelled' | 'open' | 'closed' | 'full';
    sessionType: string;
    sessionDate: string | Date;
    startDate?: string | Date;
    endDate?: string | Date;
    duration: number;
    projectId?: string;
    // Tester capacity
    maxTesters: number;
    currentTesters: number;
    // Game info
    gameTitle: string;
    gameDeveloper: string;
    gameId?: string;
    platform: string[];
    featuredGames: FeaturedGame[];
    currentGames: number;
    maxGames: number;
    // Session details
    skillLevel: string;
    isOnline: boolean;
    location?: { id: string; name?: string; isOnline?: boolean; address?: string };
    participants?: Array<{ id: string; name?: string; email?: string }>;
    feedback?: any[];
    requirements: string[];
    rewards?: { type: string; amount: number; value?: string };
    // Timestamps
    createdAt?: string | Date;
    updatedAt?: string | Date;
    // Allow additional properties
    [key: string]: any;
}

export async function getTestSessions() {
    return { data: [] as TestSession[], error: null };
}

export async function getTestSessionById(_id: string) {
    return { data: null as TestSession | null, error: null };
}

export async function createTestSession(_data: Partial<TestSession>) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function updateTestSession(_id: string, _data: Partial<TestSession>) {
    return { success: false, error: 'Testing Lab module is disabled' };
}

export async function deleteTestSession(_id: string) {
    return { success: false, error: 'Testing Lab module is disabled' };
}
