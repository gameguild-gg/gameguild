'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { createTestingSession as createTestingSessionApi, getPublicTestingSessions, getTestingSessions as getTestingSessionsApi } from '@/lib/api/testing-lab';
import type { TestingSession } from '@/lib/api/testing-types';

/**
 * Get all testing sessions with optional filtering
 */
export async function getTestingSessionsData(params?: { status?: string; testingRequestId?: string; locationId?: string; managerId?: string; skip?: number; take?: number }) {
  const session = await configureAuthenticatedClient();

  try {
    console.log('Making request to getTestingSessions with params:', params);
    console.log('Session token exists:', !!session?.api?.accessToken);
    console.log('Current tenant ID:', session?.currentTenant?.id);

    const sessions = await getTestingSessionsApi({ tenantId: session?.currentTenant?.id });

    console.log('Sessions fetched:', sessions);
    console.log('Sessions count:', sessions?.length);

    if (!sessions) {
      console.log('No sessions returned from API');
      return {
        testingSessions: [],
        total: 0,
      };
    }

    // For now, return all sessions without filtering
    // TODO: Implement server-side filtering when backend supports it
    let filteredSessions = sessions;

    if (params) {
      // Apply client-side filtering as a temporary solution
      if (params.status) {
        filteredSessions = filteredSessions.filter((session) => session.status?.toString() === params.status);
      }
      if (params.testingRequestId && params.testingRequestId !== 'none') {
        filteredSessions = filteredSessions.filter((session) => session.testingRequestId === params.testingRequestId);
      }
      if (params.locationId && params.locationId !== 'none') {
        filteredSessions = filteredSessions.filter((session) => session.locationId === params.locationId);
      }
      if (params.managerId) {
        filteredSessions = filteredSessions.filter((session) => session.managerId === params.managerId);
      }
    }

    return {
      testingSessions: filteredSessions,
      total: filteredSessions.length,
    };

    return {
      testingSessions: sessions,
      total: sessions.length,
    };
  } catch (error) {
    console.error('Error fetching testing sessions:', error);

    if (error instanceof Error) {
      console.error('Error message:', error.message);
      console.error('Error stack:', error.stack);
    }

    // Check for authentication errors
    if (error && typeof error === 'object' && 'status' in error) {
      const statusError = error as { status: number; message?: string };
      if (statusError.status === 401) {
        console.error('Authentication error - invalid or expired token');
        throw new Error('Authentication failed. Please log in again.');
      }
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

// =============================================================================
// COMPONENT-FRIENDLY ALIASES
// =============================================================================

/**
 * Get all testing sessions (alias for getTestingSessionsData)
 */
export const getAllTestingSessions = getTestingSessionsData;

/**
 * Unified getter used by both dashboard and public pages so we can ensure the same base dataset
 * before role-based filtering. When publicOnly=true, we hide cancelled / completed sessions and
 * only show sessions with status Scheduled (0) or Active (1) and that still have capacity.
 */
export async function getUnifiedTestingSessions(options?: { publicOnly?: boolean; status?: number }) {
  const base = await getTestingSessionsData(options?.status !== undefined ? { status: options.status.toString() } : undefined);
  const sessions = base.testingSessions || [];
  if (!options?.publicOnly) {
    return { testingSessions: sessions, total: sessions.length };
  }
  const filtered = sessions.filter(s => {
    const status = (s as any).status;
    const max = (s as any).maxTesters ?? 0;
    const registered = (s as any).registeredTesterCount ?? 0;
    const hasCapacity = max === 0 || registered < max; // max === 0 treated as unlimited
    return [0, 1].includes(status) && hasCapacity;
  });
  return { testingSessions: filtered, total: filtered.length };
}

/**
 * Get available test sessions for the landing page
 */
export async function getAvailableTestSessions() {
  try {
    const unified = await getUnifiedTestingSessions({ publicOnly: true });
    return unified.testingSessions;
  } catch (e) {
    console.warn('Unified public fetch failed, falling back to public endpoint:', e instanceof Error ? e.message : e);
    try {
      return await getPublicTestingSessions(100);
    } catch (error) {
      console.error('Public fetch of available test sessions failed (returning empty list):', error instanceof Error ? error.message : error);
      return [];
    }
  }
}

/**
 * Get test session by slug (alias for getTestingSessionBySlug)
 */
export const getTestSessionBySlug = getTestingSessionBySlug;

/**
 * Get testing session by slug (treating slug as ID for now)
 */
export async function getTestingSessionBySlug(slug: string) {
  // For now, treat slug as ID since testing sessions don't seem to have slug endpoints
  // This could be updated if a proper slug-based endpoint becomes available
  await configureAuthenticatedClient();

  try {
    console.log('Fetching testing session by slug (as ID):', slug);

    const sessions = await getTestingSessionsApi(); // public slug resolution keeps existing behavior (no tenant filter)

    if (!sessions || !Array.isArray(sessions)) {
      return null;
    }

    // For now, find by ID if slug looks like an ID, otherwise return first session
    const foundSession = sessions.find((session: { id?: string; sessionName?: string }) => session.id === slug || session.sessionName?.toLowerCase().includes(slug.toLowerCase()));

    return foundSession || null;
  } catch (error) {
    console.error('Error fetching testing session by slug:', error);
    return null;
  }
}

/**
 * Create a new testing session
 */
export async function createTestingSession(sessionData: Partial<TestingSession>) {
  await configureAuthenticatedClient();

  try {
    console.log('Creating testing session with data:', sessionData);

    // Convert form data to API request format
    const requestData = {
      sessionName: sessionData.sessionName || '',
      sessionDate: sessionData.sessionDate || '',
      startTime: sessionData.startTime || '',
      endTime: sessionData.endTime || '',
      locationId: sessionData.locationId || '',
      maxTesters: sessionData.maxTesters || 10,
      testingRequestId: sessionData.testingRequestId,
      managerId: sessionData.managerId,
    };

    const newSession = await createTestingSessionApi(requestData);

    console.log('Testing session created successfully:', newSession);
    return {
      success: true,
      data: newSession,
      error: null,
    };
  } catch (error) {
    console.error('Error creating testing session:', error);

    const errorMessage = error instanceof Error ? error.message : 'Failed to create testing session';
    return {
      success: false,
      data: null,
      error: errorMessage,
    };
  }
}
