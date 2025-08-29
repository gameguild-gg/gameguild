'use server'

import { TestingSession } from '@/lib/admin/testing-lab/types'
import { SessionStatus } from '@/lib/api/generated/types.gen'

// Sample testing sessions data - using API-compatible structure
const sampleTestingSessions: TestingSession[] = [
    {
        id: '1',
        sessionName: 'RPG Combat System Testing',
        sessionDate: '2024-01-15',
        startTime: '10:00:00',
        endTime: '12:00:00',
        status: SessionStatus.ACTIVE,
        maxTesters: 8,
        maxProjects: 2,
        registeredTesterCount: 5,
        registeredProjectMemberCount: 3,
        registeredProjectCount: 2,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-15T09:00:00Z'
    },
    {
        id: '2',
        sessionName: 'Mobile UI/UX Testing Session',
        sessionDate: '2024-01-20',
        startTime: '14:00:00',
        endTime: '16:00:00',
        status: SessionStatus.SCHEDULED,
        maxTesters: 12,
        maxProjects: 3,
        registeredTesterCount: 7,
        registeredProjectMemberCount: 4,
        registeredProjectCount: 2,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-20T13:00:00Z'
    },
    {
        id: '3',
        sessionName: 'Performance Stress Testing',
        sessionDate: '2024-01-10',
        startTime: '09:00:00',
        endTime: '11:00:00',
        status: SessionStatus.COMPLETED,
        maxTesters: 20,
        maxProjects: 5,
        registeredTesterCount: 18,
        registeredProjectMemberCount: 8,
        registeredProjectCount: 4,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-10T11:00:00Z'
    },
    {
        id: '4',
        sessionName: 'Accessibility Features Testing',
        sessionDate: '2024-02-05',
        startTime: '13:00:00',
        endTime: '15:30:00',
        status: SessionStatus.CANCELLED,
        maxTesters: 6,
        maxProjects: 1,
        registeredTesterCount: 0,
        registeredProjectMemberCount: 0,
        registeredProjectCount: 0,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-02-05T12:00:00Z'
    },
    {
        id: '5',
        sessionName: 'Beta Release Final Testing',
        sessionDate: '2024-02-25',
        startTime: '10:00:00',
        endTime: '17:00:00',
        status: SessionStatus.SCHEDULED,
        maxTesters: 50,
        maxProjects: 10,
        registeredTesterCount: 23,
        registeredProjectMemberCount: 12,
        registeredProjectCount: 6,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-02-25T09:00:00Z'
    }
]

// Action to get all testing sessions
export async function getTestingSessionsAction(): Promise<TestingSession[]> {
    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 100))

    return sampleTestingSessions
}

// Action to search testing sessions
export async function searchTestingSessionsAction(query: string): Promise<TestingSession[]> {
    // Simulate network delay
    await new Promise(resolve => setTimeout(resolve, 150))

    if (!query.trim()) {
        return sampleTestingSessions
    }

    const searchTerm = query.toLowerCase().trim()

    return sampleTestingSessions.filter(session =>
        session.sessionName.toLowerCase().includes(searchTerm) ||
        (session.id || '').toLowerCase().includes(searchTerm) ||
        session.sessionDate.includes(searchTerm)
    )
}
