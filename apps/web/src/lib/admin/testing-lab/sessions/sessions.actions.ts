'use server';

import type { TestingSession } from '@/lib/api/testing-types';

export interface TestingSessionActionResult<T = unknown> {
    success: boolean;
    data?: T;
    error?: string;
}

/**
 * Get all testing sessions with optional filtering
 */
export async function getTestingSessionsData(params?: { status?: string; testingRequestId?: string; locationId?: string; managerId?: string; skip?: number; take?: number }) {
<<<<<<< Updated upstream
    try {
        // For now, return sample data to avoid API errors
        // This can be updated later when the API endpoint is properly configured
        console.log('Loading testing sessions...');

        const testingSessions: TestingSession[] = [
            {
                id: 'session-1',
                sessionName: 'Mobile Game Alpha Testing',
                sessionDate: new Date(Date.now() + 86400000).toISOString().split('T')[0]!, // tomorrow
                startTime: '14:00',
                endTime: '15:00',
                location: { name: 'Remote - Discord', maxTestersCapacity: 12, maxProjectsCapacity: 5, status: 1 },
                maxTesters: 12,
                registeredTesterCount: 8,
                status: 1, // Active
                testingRequest: { title: 'Action RPG Mobile Game' },
                createdBy: { id: 'user-1', name: 'John Smith' },
                createdAt: new Date().toISOString(),
            },
            {
                id: 'session-2',
                sessionName: 'Web Platform UX Testing',
                sessionDate: new Date(Date.now() + 172800000).toISOString().split('T')[0]!, // 2 days from now
                startTime: '10:00',
                endTime: '11:00',
                location: { name: 'Testing Lab - Room A', maxTestersCapacity: 6, maxProjectsCapacity: 2, status: 1 },
                maxTesters: 6,
                registeredTesterCount: 4,
                status: 0, // Draft/Scheduled
                testingRequest: { title: 'E-commerce Platform Redesign' },
                createdBy: { id: 'user-2', name: 'Sarah Johnson' },
                createdAt: new Date(Date.now() - 86400000).toISOString(), // 1 day ago
            },
            {
                id: 'session-3',
                sessionName: 'VR Game Performance Testing',
                sessionDate: new Date(Date.now() - 86400000).toISOString().split('T')[0]!, // yesterday
                startTime: '16:00',
                endTime: '17:00',
                location: { name: 'VR Lab - Building B', maxTestersCapacity: 4, maxProjectsCapacity: 1, status: 1 },
                maxTesters: 4,
                registeredTesterCount: 4,
                status: 2, // Completed
                testingRequest: { title: 'Adventure VR Game' },
                createdBy: { id: 'user-3', name: 'Mike Chen' },
                createdAt: new Date(Date.now() - 604800000).toISOString(), // 1 week ago
            },
            {
                id: 'session-4',
                sessionName: 'Puzzle Game Accessibility Testing',
                sessionDate: new Date(Date.now() + 259200000).toISOString().split('T')[0]!, // 3 days from now
                startTime: '13:00',
                endTime: '14:00',
                location: { name: 'Hybrid - Zoom & Local', maxTestersCapacity: 10, maxProjectsCapacity: 3, status: 1 },
                maxTesters: 10,
                registeredTesterCount: 6,
                status: 0, // Draft/Scheduled
                testingRequest: { title: 'Educational Puzzle Game' },
                createdBy: { id: 'user-4', name: 'Lisa Wang' },
                createdAt: new Date(Date.now() - 172800000).toISOString(), // 2 days ago
            },
            {
                id: 'session-5',
                sessionName: 'Strategy Game Balance Testing',
                sessionDate: new Date(Date.now() + 432000000).toISOString().split('T')[0]!, // 5 days from now
                startTime: '15:00',
                endTime: '16:00',
                location: { name: 'Remote - Teams', maxTestersCapacity: 8, maxProjectsCapacity: 2, status: 1 },
                maxTesters: 8,
                registeredTesterCount: 2,
                status: 0, // Draft/Scheduled
                testingRequest: { title: 'Turn-Based Strategy Game' },
                createdBy: { id: 'user-5', name: 'Alex Rodriguez' },
                createdAt: new Date(Date.now() - 432000000).toISOString(), // 5 days ago
            },
        ];

        return {
            testingSessions,
            total: testingSessions.length,
        };
    } catch (error) {
        console.error('Error loading testing sessions:', error);
        return {
            testingSessions: [],
            total: 0,
        };
    }
=======
  try {
    // For now, return sample data to avoid API errors
    // This can be updated later when the API endpoint is properly configured
    console.log('Loading testing sessions...');

    const testingSessions: TestingSession[] = [
      {
        id: 'session-1',
        sessionName: 'Mobile Game Alpha Testing',
        sessionDate: new Date(Date.now() + 86400000).toISOString().split('T')[0]!, // tomorrow
        startTime: '14:00',
        endTime: '15:00',
        location: { name: 'Remote - Discord', maxTestersCapacity: 12, maxProjectsCapacity: 5, status: 1 },
        maxTesters: 12,
        registeredTesterCount: 8,
        status: 1, // Active
        testingRequest: { title: 'Action RPG Mobile Game' },
        createdBy: { id: 'user-1', name: 'John Smith' },
        createdAt: new Date().toISOString(),
      },
      {
        id: 'session-2',
        sessionName: 'Web Platform UX Testing',
        sessionDate: new Date(Date.now() + 172800000).toISOString().split('T')[0]!, // 2 days from now
        startTime: '10:00',
        endTime: '11:00',
        location: { name: 'Testing Lab - Room A', maxTestersCapacity: 6, maxProjectsCapacity: 2, status: 1 },
        maxTesters: 6,
        registeredTesterCount: 4,
        status: 0, // Draft/Scheduled
        testingRequest: { title: 'E-commerce Platform Redesign' },
        createdBy: { id: 'user-2', name: 'Sarah Johnson' },
        createdAt: new Date(Date.now() - 86400000).toISOString(), // 1 day ago
      },
      {
        id: 'session-3',
        sessionName: 'VR Game Performance Testing',
        sessionDate: new Date(Date.now() - 86400000).toISOString().split('T')[0]!, // yesterday
        startTime: '16:00',
        endTime: '17:00',
        location: { name: 'VR Lab - Building B', maxTestersCapacity: 4, maxProjectsCapacity: 1, status: 1 },
        maxTesters: 4,
        registeredTesterCount: 4,
        status: 2, // Completed
        testingRequest: { title: 'Adventure VR Game' },
        createdBy: { id: 'user-3', name: 'Mike Chen' },
        createdAt: new Date(Date.now() - 604800000).toISOString(), // 1 week ago
      },
      {
        id: 'session-4',
        sessionName: 'Puzzle Game Accessibility Testing',
        sessionDate: new Date(Date.now() + 259200000).toISOString().split('T')[0]!, // 3 days from now
        startTime: '13:00',
        endTime: '14:00',
        location: { name: 'Hybrid - Zoom & Local', maxTestersCapacity: 10, maxProjectsCapacity: 3, status: 1 },
        maxTesters: 10,
        registeredTesterCount: 6,
        status: 0, // Draft/Scheduled
        testingRequest: { title: 'Educational Puzzle Game' },
        createdBy: { id: 'user-4', name: 'Lisa Wang' },
        createdAt: new Date(Date.now() - 172800000).toISOString(), // 2 days ago
      },
      {
        id: 'session-5',
        sessionName: 'Strategy Game Balance Testing',
        sessionDate: new Date(Date.now() + 432000000).toISOString().split('T')[0]!, // 5 days from now
        startTime: '15:00',
        endTime: '16:00',
        location: { name: 'Remote - Teams', maxTestersCapacity: 8, maxProjectsCapacity: 2, status: 1 },
        maxTesters: 8,
        registeredTesterCount: 2,
        status: 0, // Draft/Scheduled
        testingRequest: { title: 'Turn-Based Strategy Game' },
        createdBy: { id: 'user-5', name: 'Alex Rodriguez' },
        createdAt: new Date(Date.now() - 432000000).toISOString(), // 5 days ago
      },
    ];

    return {
      testingSessions,
      total: testingSessions.length,
    };
  } catch (error) {
    console.error('Error loading testing sessions:', error);
    return {
      testingSessions: [],
      total: 0,
    };
  }
>>>>>>> Stashed changes
}

/**
 * Get all testing sessions (alias for getTestingSessionsData)
 */
export const getAllTestingSessions = getTestingSessionsData;

/**
 * Get available test sessions for the landing page
 */
export async function getAvailableTestSessions() {
<<<<<<< Updated upstream
    try {
        const result = await getTestingSessionsData();
        // Filter for scheduled sessions with capacity
        const availableSessions = result.testingSessions.filter(session => {
            const hasCapacity = !session.maxTesters || (session.registeredTesterCount || 0) < session.maxTesters;
            return session.status === 0 && hasCapacity; // Status 0 = Draft/Scheduled
        });
        return availableSessions;
    } catch (error) {
        console.error('Error getting available test sessions:', error);
        return [];
    }
=======
  try {
    const result = await getTestingSessionsData();
    // Filter for scheduled sessions with capacity
    const availableSessions = result.testingSessions.filter(session => {
      const hasCapacity = !session.maxTesters || (session.registeredTesterCount || 0) < session.maxTesters;
      return session.status === 0 && hasCapacity; // Status 0 = Draft/Scheduled
    });
    return availableSessions;
  } catch (error) {
    console.error('Error getting available test sessions:', error);
    return [];
  }
>>>>>>> Stashed changes
}

/**
 * Get testing session by slug (treating slug as ID for now)
 */
export async function getTestingSessionBySlug(slug: string) {
<<<<<<< Updated upstream
    try {
        const result = await getTestingSessionsData();
        const foundSession = result.testingSessions.find(session => 
            session.id === slug || 
            session.sessionName?.toLowerCase().includes(slug.toLowerCase())
        );
        return foundSession || null;
    } catch (error) {
        console.error('Error fetching testing session by slug:', error);
        return null;
    }
=======
  try {
    const result = await getTestingSessionsData();
    const foundSession = result.testingSessions.find(session =>
      session.id === slug ||
      session.sessionName?.toLowerCase().includes(slug.toLowerCase())
    );
    return foundSession || null;
  } catch (error) {
    console.error('Error fetching testing session by slug:', error);
    return null;
  }
>>>>>>> Stashed changes
}

/**
 * Search testing sessions
 */
export async function searchTestingSessionsAction(query: string): Promise<TestingSessionActionResult<TestingSession[]>> {
<<<<<<< Updated upstream
    const result = await getTestingSessionsData();

    if (!result.testingSessions) {
        return {
            success: false,
            error: 'Failed to load testing sessions',
        };
    }

    const filteredSessions = result.testingSessions.filter(session =>
        session.sessionName.toLowerCase().includes(query.toLowerCase()) ||
        session.location?.name?.toLowerCase().includes(query.toLowerCase()) ||
        session.testingRequest?.title?.toLowerCase().includes(query.toLowerCase()) ||
        session.createdBy?.name?.toLowerCase().includes(query.toLowerCase())
    );

    return {
        success: true,
        data: filteredSessions,
    };
=======
  const result = await getTestingSessionsData();

  if (!result.testingSessions) {
    return {
      success: false,
      error: 'Failed to load testing sessions',
    };
  }

  const filteredSessions = result.testingSessions.filter(session =>
    session.sessionName.toLowerCase().includes(query.toLowerCase()) ||
    session.location?.name?.toLowerCase().includes(query.toLowerCase()) ||
    session.testingRequest?.title?.toLowerCase().includes(query.toLowerCase()) ||
    session.createdBy?.name?.toLowerCase().includes(query.toLowerCase())
  );

  return {
    success: true,
    data: filteredSessions,
  };
>>>>>>> Stashed changes
}
