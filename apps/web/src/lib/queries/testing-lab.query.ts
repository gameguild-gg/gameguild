import { queryOptions } from '@tanstack/react-query';

export interface TestingLabStats {
    totalRequests: number;
    activeRequests: number;
    totalSessions: number;
    upcomingSessions: number;
    totalFeedback: number;
    pendingFeedback: number;
    mySubmissions: number;
    myTestingAssignments: number;
}

export interface TestingSession {
    id: string;
    title: string;
    description: string;
    scheduledAt: string;
    duration: number;
    status: 'scheduled' | 'active' | 'completed' | 'cancelled';
    participants: number;
    maxParticipants: number;
}

export interface TestingRequest {
    id: string;
    title: string;
    description: string;
    submittedBy: string;
    submittedAt: string;
    status: 'pending' | 'in-progress' | 'completed' | 'rejected';
    priority: 'low' | 'medium' | 'high';
    tags: string[];
}

// Mock data fetching functions - replace with actual API calls
async function fetchTestingLabStats(userRole: 'student' | 'professor' | 'admin'): Promise<TestingLabStats> {
    // Simulate API delay
    await new Promise(resolve => setTimeout(resolve, 100));

    if (userRole === 'student') {
        return {
            totalRequests: 8,
            activeRequests: 3,
            totalSessions: 12,
            upcomingSessions: 2,
            totalFeedback: 6,
            pendingFeedback: 1,
            mySubmissions: 4,
            myTestingAssignments: 8,
        };
    } else {
        return {
            totalRequests: 120,
            activeRequests: 45,
            totalSessions: 24,
            upcomingSessions: 6,
            totalFeedback: 89,
            pendingFeedback: 12,
            mySubmissions: 0,
            myTestingAssignments: 0,
        };
    }
}

async function fetchTestingSessions(): Promise<TestingSession[]> {
    await new Promise(resolve => setTimeout(resolve, 150));

    return [
        {
            id: '1',
            title: 'Game Project Alpha Testing',
            description: 'Testing the core gameplay mechanics',
            scheduledAt: '2025-09-20T14:00:00Z',
            duration: 90,
            status: 'scheduled',
            participants: 3,
            maxParticipants: 6,
        },
        {
            id: '2',
            title: 'UI/UX Feedback Session',
            description: 'Focus on user interface improvements',
            scheduledAt: '2025-09-22T10:00:00Z',
            duration: 60,
            status: 'scheduled',
            participants: 5,
            maxParticipants: 8,
        },
    ];
}

async function fetchTestingRequests(): Promise<TestingRequest[]> {
    await new Promise(resolve => setTimeout(resolve, 120));

    return [
        {
            id: '1',
            title: 'Mobile Game Performance Test',
            description: 'Need testing on various Android devices',
            submittedBy: 'John Doe',
            submittedAt: '2025-09-15T09:30:00Z',
            status: 'pending',
            priority: 'high',
            tags: ['mobile', 'performance', 'android'],
        },
        {
            id: '2',
            title: 'Multiplayer Functionality Test',
            description: 'Test network synchronization and lag handling',
            submittedBy: 'Jane Smith',
            submittedAt: '2025-09-14T16:45:00Z',
            status: 'in-progress',
            priority: 'medium',
            tags: ['multiplayer', 'network', 'synchronization'],
        },
    ];
}

// Query options for React Query
export const testingLabQueries = {
    all: () => ['testing-lab'] as const,

    stats: (userRole: 'student' | 'professor' | 'admin') =>
        queryOptions({
            queryKey: [...testingLabQueries.all(), 'stats', userRole] as const,
            queryFn: () => fetchTestingLabStats(userRole),
            staleTime: 5 * 60 * 1000, // 5 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),

    sessions: () =>
        queryOptions({
            queryKey: [...testingLabQueries.all(), 'sessions'] as const,
            queryFn: fetchTestingSessions,
            staleTime: 2 * 60 * 1000, // 2 minutes
            gcTime: 5 * 60 * 1000, // 5 minutes
        }),

    requests: () =>
        queryOptions({
            queryKey: [...testingLabQueries.all(), 'requests'] as const,
            queryFn: fetchTestingRequests,
            staleTime: 1 * 60 * 1000, // 1 minute
            gcTime: 5 * 60 * 1000, // 5 minutes
        }),
} as const;