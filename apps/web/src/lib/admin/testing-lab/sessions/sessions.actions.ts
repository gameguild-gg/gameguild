'use server'

import { getTestingSessions, getTestingSessionsSearch } from '@/lib/api/generated/sdk.gen'
import { SessionStatus, TestingSession } from '@/lib/api/generated/types.gen'
import { configureAuthenticatedClient } from '@/lib/core/api/authenticated-client'

// Action to get all testing sessions
export async function getTestingSessionsAction(): Promise<TestingSession[]> {
    try {
        console.log('Fetching testing sessions from API...')

        // Configure the client with authentication and base URL
        await configureAuthenticatedClient()

        const response = await getTestingSessions({
            query: {
                skip: 0,
                take: 100 // Adjust based on your pagination needs
            }
        })

        if (response.data) {
            return response.data
        }

        return []
    } catch (error) {
        console.error('Error fetching testing sessions:', error)
        throw new Error('Failed to fetch testing sessions')
    }
}

// Action to search testing sessions
export async function searchTestingSessionsAction(query: string): Promise<TestingSession[]> {
    try {
        console.log('Searching testing sessions for:', query)

        if (!query.trim()) {
            // If no search term, return all sessions
            return await getTestingSessionsAction()
        }

        // Configure the client with authentication and base URL
        await configureAuthenticatedClient()

        const response = await getTestingSessionsSearch({
            query: {
                searchTerm: query.trim()
            }
        })

        if (response.data) {
            return response.data
        }

        return []
    } catch (error) {
        console.error('Error searching testing sessions:', error)
        throw new Error('Failed to search testing sessions')
    }
}

// Action to get available test sessions (for the main testing-lab page)
export async function getAvailableTestSessions(): Promise<TestingSession[]> {
    try {
        console.log('Fetching available test sessions from API...')

        // Configure the client with authentication and base URL
        await configureAuthenticatedClient()

        const response = await getTestingSessions({
            query: {
                skip: 0,
                take: 100 // Adjust based on your needs
            }
        })

        if (response.data) {
            // Filter for sessions that are scheduled or active
            return response.data.filter(session =>
                session.status === SessionStatus.SCHEDULED ||
                session.status === SessionStatus.ACTIVE
            )
        }

        return []
    } catch (error) {
        console.error('Error fetching available test sessions:', error)
        throw new Error('Failed to fetch available test sessions')
    }
}
