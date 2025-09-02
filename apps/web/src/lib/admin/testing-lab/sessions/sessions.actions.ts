'use server'

import { deleteTestingSessionsById, getTestingLocations, getTestingRequests, getTestingSessions, getTestingSessionsById, getTestingSessionsSearch, postTestingSessions } from '@/lib/api/generated/sdk.gen'
import { SessionStatus, TestingLocation, TestingRequest, TestingSession } from '@/lib/api/generated/types.gen'
import { configureAuthenticatedClient } from '@/lib/core/api/authenticated-client'
import { TestingSessionCreateData } from '@/lib/schemas/testing-sessions.schema'

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

// Action to get testing locations
export async function getTestingLocationsAction(): Promise<TestingLocation[]> {
    try {
        console.log('Fetching testing locations from API...')

        await configureAuthenticatedClient()

        const response = await getTestingLocations({
            query: {
                skip: 0,
                take: 100
            }
        })

        if (response.data) {
            return response.data
        }

        return []
    } catch (error) {
        console.error('Error fetching testing locations:', error)
        throw new Error('Failed to fetch testing locations')
    }
}

// Action to get testing requests
export async function getTestingRequestsAction(): Promise<TestingRequest[]> {
    try {
        console.log('Fetching testing requests from API...')

        await configureAuthenticatedClient()

        const response = await getTestingRequests({
            query: {
                skip: 0,
                take: 100
            }
        })

        if (response.data) {
            return response.data
        }

        return []
    } catch (error) {
        console.error('Error fetching testing requests:', error)
        throw new Error('Failed to fetch testing requests')
    }
}

// Action to create a new testing session (without testing requests initially)
export async function createTestingSessionAction(data: TestingSessionCreateData): Promise<{ success: boolean; data?: TestingSession; error?: string }> {
    try {
        console.log('Creating testing session:', data)

        await configureAuthenticatedClient()

        // Create the testing session without any testing requests initially
        const sessionData: Partial<TestingSession> = {
            sessionName: data.sessionName,
            sessionDate: data.sessionDate,
            startTime: data.startTime,
            endTime: data.endTime,
            maxTesters: data.maxTesters,
            maxProjects: data.maxProjects,
            locationId: data.locationId,
            managerUserId: data.managerUserId || undefined,
            status: data.status,
        }

        const response = await postTestingSessions({
            body: sessionData as TestingSession
        })

        if (response.data) {
            return {
                success: true,
                data: response.data
            }
        }

        return {
            success: false,
            error: 'Failed to create session'
        }
    } catch (error) {
        console.error('Error creating testing session:', error)

        if (error instanceof Error) {
            return {
                success: false,
                error: error.message
            }
        }

        return {
            success: false,
            error: 'Failed to create testing session'
        }
    }
}// Action to delete a testing session
export async function deleteTestingSessionAction(sessionId: string): Promise<{ success: boolean; error?: string }> {
    try {
        console.log('Deleting testing session:', sessionId)

        await configureAuthenticatedClient()

        await deleteTestingSessionsById({
            path: {
                id: sessionId
            }
        })

        return {
            success: true
        }
    } catch (error) {
        console.error('Error deleting testing session:', error)

        if (error instanceof Error) {
            return {
                success: false,
                error: error.message
            }
        }

        return {
            success: false,
            error: 'Failed to delete testing session'
        }
    }
}

// Action to get pending enrollment requests for sessions
export async function getSessionEnrollmentRequestsAction(sessionId: string): Promise<{ success: boolean; data?: any[]; error?: string }> {
    try {
        console.log('Fetching enrollment requests for session:', sessionId)

        await configureAuthenticatedClient()

        // Note: This would use a real API endpoint for enrollment requests
        // For now, we'll return empty array until the API is available
        const enrollmentRequests: any[] = []

        return {
            success: true,
            data: enrollmentRequests
        }
    } catch (error) {
        console.error('Error fetching enrollment requests:', error)

        return {
            success: false,
            error: 'Failed to fetch enrollment requests'
        }
    }
}

// Action to approve/reject enrollment requests
export async function processEnrollmentDecisionAction(
    enrollmentId: string,
    decision: 'approved' | 'rejected',
    adminMessage?: string
): Promise<{ success: boolean; error?: string }> {
    try {
        console.log('Processing enrollment decision:', { enrollmentId, decision, adminMessage })

        await configureAuthenticatedClient()

        // Note: This would use a real API endpoint for processing enrollment decisions
        // Implementation depends on your backend API structure

        return {
            success: true
        }
    } catch (error) {
        console.error('Error processing enrollment decision:', error)

        return {
            success: false,
            error: 'Failed to process enrollment decision'
        }
    }
}

// Action to get a single testing session by ID
export async function getTestingSessionByIdAction(sessionId: string): Promise<TestingSession | null> {
    try {
        console.log('Fetching testing session by ID:', sessionId)

        await configureAuthenticatedClient()

        const response = await getTestingSessionsById({
            path: {
                id: sessionId
            }
        })

        if (response.data) {
            return response.data
        }

        return null
    } catch (error) {
        console.error('Error fetching testing session by ID:', error)
        return null
    }
}

// Action to get a testing session by slug (could be ID or session name)
export async function getTestSessionBySlug(slug: string): Promise<TestingSession | null> {
    try {
        console.log('Fetching testing session by slug:', slug)

        await configureAuthenticatedClient()

        // First try to get by ID
        try {
            const response = await getTestingSessionsById({
                path: {
                    id: slug
                }
            })

            if (response.data) {
                return response.data
            }
        } catch (error) {
            // ID lookup failed, try searching by session name
            console.log('ID lookup failed, trying session name search...')
        }

        // If ID lookup fails, search by session name
        const searchResponse = await getTestingSessionsSearch({
            query: {
                searchTerm: slug
            }
        })

        if (searchResponse.data && searchResponse.data.length > 0) {
            // Return the first match that has a matching session name
            const exactMatch = searchResponse.data.find(session =>
                session.sessionName?.toLowerCase().replace(/\s+/g, '-') === slug.toLowerCase()
            )

            if (exactMatch) {
                return exactMatch
            }

            // If no exact match, return the first result
            return searchResponse.data[0] || null
        }

        return null
    } catch (error) {
        console.error('Error fetching testing session by slug:', error)
        return null
    }
}
