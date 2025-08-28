'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteTestingSessionsById,
  deleteTestingSessionsBySessionIdRegister,
  deleteTestingSessionsBySessionIdWaitlist,
  getTestingSessions,
  getTestingSessionsById,
  getTestingSessionsByIdDetails,
  getTestingSessionsByLocationByLocationId,
  getTestingSessionsByManagerByManagerId,
  getTestingSessionsByRequestByTestingRequestId,
  getTestingSessionsBySessionIdRegistrations,
  getTestingSessionsBySessionIdStatistics,
  getTestingSessionsBySessionIdWaitlist,
  getTestingSessionsByStatusByStatus,
  getTestingSessionsSearch,
  postTestingSessions,
  postTestingSessionsByIdRestore,
  postTestingSessionsBySessionIdAttendance,
  postTestingSessionsBySessionIdRegister,
  postTestingSessionsBySessionIdWaitlist,
  putTestingSessionsById,
} from '@/lib/api/generated/sdk.gen';
import type { SessionRegistration, SessionStatus, SessionWaitlist, TestingSession } from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// TESTING SESSION CRUD OPERATIONS
// =============================================================================

/**
 * Get all testing sessions with optional filtering
 */
export async function getTestingSessionsData(params?: { testingRequestId?: string; locationId?: string; managerId?: string; status?: string; skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessions({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return {
      testingSessions: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching testing sessions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

/**
 * Create a new testing session
 */
export async function createTestingSession(sessionData: { testingRequestId: string; locationId?: string; sessionName: string; sessionDate: string; startTime: string; endTime: string; maxTesters: number; managerId?: string }) {
  await configureAuthenticatedClient();

  try {
    // Convert time format to ISO datetime strings
    const sessionDateTime = new Date(sessionData.sessionDate);
    const [startHours, startMinutes] = sessionData.startTime.split(':');
    const [endHours, endMinutes] = sessionData.endTime.split(':');

    if (!startHours || !startMinutes || !endHours || !endMinutes) {
      throw new Error('Invalid time format');
    }

    const startDateTime = new Date(sessionDateTime);
    startDateTime.setHours(parseInt(startHours), parseInt(startMinutes), 0, 0);

    const endDateTime = new Date(sessionDateTime);
    endDateTime.setHours(parseInt(endHours), parseInt(endMinutes), 0, 0);

    const testingSessionPayload = {
      testingRequestId: sessionData.testingRequestId,
      locationId: sessionData.locationId || undefined,
      sessionName: sessionData.sessionName,
      sessionDate: sessionDateTime.toISOString(),
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString(),
      maxTesters: sessionData.maxTesters,
      status: 0 as SessionStatus, // SessionStatus.Scheduled
      managerId: sessionData.managerId || undefined,
      managerUserId: sessionData.managerId || undefined,
    } as TestingSession;

    const response = await postTestingSessions({
      body: testingSessionPayload,
    });

    if (!response.data) {
      throw new Error('Failed to create testing session');
    }

    revalidateTag('testing-sessions');
    return response.data as TestingSession;
  } catch (error) {
    console.error('Error creating testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create testing session');
  }
}

/**
 * Delete a testing session by ID
 */
export async function deleteTestingSession(id: string) {
  await configureAuthenticatedClient();

  try {
    await deleteTestingSessionsById({
      path: { id },
    });

    revalidateTag('testing-sessions');
    return { success: true };
  } catch (error) {
    console.error('Error deleting testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete testing session');
  }
}

/**
 * Get a single testing session by ID
 */
export async function getTestingSessionById(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsById({
      path: { id },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Testing session not found');
    }

    return response.data as TestingSession;
  } catch (error) {
    console.error('Error fetching testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing session');
  }
}

/**
 * Update an existing testing session
 */
export async function updateTestingSession(
  id: string,
  sessionData: {
    sessionName?: string;
    sessionDate?: string;
    startTime?: string;
    endTime?: string;
    maxTesters?: number;
    status?: number;
    locationId?: string;
    managerId?: string; // alias for managerUserId
  },
) {
  await configureAuthenticatedClient();

  try {
    // Map provided partial into TestingSession shape (only sending changed fields)
    const payload: Partial<TestingSession> = { ...sessionData } as Partial<TestingSession>;
    if (sessionData.managerId && !('managerUserId' in payload)) {
      (payload as any).managerUserId = sessionData.managerId;
    }

    const response = await putTestingSessionsById({
      path: { id },
      body: payload as TestingSession,
    });

    if (!response.data) {
      throw new Error('Failed to update testing session');
    }

    revalidateTag('testing-sessions');
    return response.data as TestingSession;
  } catch (error) {
    console.error('Error updating testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update testing session');
  }
}

/**
 * Get detailed information about a testing session
 */
export async function getTestingSessionDetails(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsByIdDetails({
      path: { id },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Testing session details not found');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing session details:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing session details');
  }
}

/**
 * Restore a deleted testing session
 */
export async function restoreTestingSession(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSessionsByIdRestore({
      path: { id },
    });

    if (!response.data) {
      throw new Error('Failed to restore testing session');
    }

    revalidateTag('testing-sessions');
    return response.data;
  } catch (error) {
    console.error('Error restoring testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to restore testing session');
  }
}

// =============================================================================
// TESTING SESSION FILTERING & SEARCH
// =============================================================================

/**
 * Get testing sessions by request
 */
export async function getTestingSessionsByRequest(testingRequestId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsByRequestByTestingRequestId({
      path: { testingRequestId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing sessions by request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

/**
 * Get testing sessions by location
 */
export async function getTestingSessionsByLocation(locationId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsByLocationByLocationId({
      path: { locationId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing sessions by location:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

/**
 * Get testing sessions by status
 */
export async function getTestingSessionsByStatus(status: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsByStatusByStatus({
      path: { status: status as unknown as SessionStatus },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing sessions by status:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

/**
 * Get testing sessions by manager
 */
export async function getTestingSessionsByManager(managerId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsByManagerByManagerId({
      path: { managerId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing sessions by manager:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

/**
 * Search testing sessions
 */
export async function searchTestingSessions(searchTerm: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsSearch({
      query: { searchTerm },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to search testing sessions');
    }

    return response.data;
  } catch (error) {
    console.error('Error searching testing sessions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search testing sessions');
  }
}

// =============================================================================
// TESTING SESSION REGISTRATION MANAGEMENT
// =============================================================================

/**
 * Unregister from a testing session
 */
export async function unregisterFromTestingSession(sessionId: string) {
  await configureAuthenticatedClient();

  try {
    await deleteTestingSessionsBySessionIdRegister({
      path: { sessionId },
    });

    revalidateTag('testing-sessions');
    return { success: true };
  } catch (error) {
    console.error('Error unregistering from testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to unregister from testing session');
  }
}

/**
 * Register for a testing session
 */
export async function registerForTestingSession(sessionId: string, notes?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSessionsBySessionIdRegister({
      path: { sessionId },
      body: {
        notes,
      },
    });

    if (!response.data) {
      throw new Error('Failed to register for testing session');
    }

    revalidateTag('testing-sessions');
    return response.data;
  } catch (error) {
    console.error('Error registering for testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to register for testing session');
  }
}

/**
 * Get registrations for a testing session
 */
export async function getTestingSessionRegistrations(sessionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsBySessionIdRegistrations({
      path: { sessionId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch session registrations');
    }

    return response.data as SessionRegistration[];
  } catch (error) {
    console.error('Error fetching session registrations:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session registrations');
  }
}

// =============================================================================
// TESTING SESSION WAITLIST MANAGEMENT
// =============================================================================

/**
 * Remove from testing session waitlist
 */
export async function removeFromTestingSessionWaitlist(sessionId: string) {
  await configureAuthenticatedClient();

  try {
    await deleteTestingSessionsBySessionIdWaitlist({
      path: { sessionId },
    });

    revalidateTag('testing-sessions');
    return { success: true };
  } catch (error) {
    console.error('Error removing from waitlist:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove from waitlist');
  }
}

/**
 * Get waitlist for a testing session
 */
export async function getTestingSessionWaitlist(sessionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsBySessionIdWaitlist({
      path: { sessionId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch session waitlist');
    }

    return response.data as SessionWaitlist[];
  } catch (error) {
    console.error('Error fetching session waitlist:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session waitlist');
  }
}

/**
 * Add to testing session waitlist
 */
export async function addToTestingSessionWaitlist(sessionId: string, notes?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSessionsBySessionIdWaitlist({
      path: { sessionId },
      body: {
        notes,
      },
    });

    if (!response.data) {
      throw new Error('Failed to add to waitlist');
    }

    revalidateTag('testing-sessions');
    return response.data;
  } catch (error) {
    console.error('Error adding to waitlist:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to add to waitlist');
  }
}

// =============================================================================
// TESTING SESSION STATISTICS & ATTENDANCE
// =============================================================================

/**
 * Get statistics for a testing session
 */
export async function getTestingSessionStatistics(sessionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsBySessionIdStatistics({
      path: { sessionId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch session statistics');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching session statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session statistics');
  }
}

/**
 * Mark attendance for a testing session
 */
export async function markTestingSessionAttendance(
  sessionId: string,
  attendanceData: {
    userId?: string;
    attended: boolean;
    arrivalTime?: string;
    departureTime?: string;
    notes?: string;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSessionsBySessionIdAttendance({
      path: { sessionId },
      body: attendanceData,
    });

    if (!response.data) {
      throw new Error('Failed to mark attendance');
    }

    revalidateTag('testing-sessions');
    return response.data;
  } catch (error) {
    console.error('Error marking attendance:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to mark attendance');
  }
}
