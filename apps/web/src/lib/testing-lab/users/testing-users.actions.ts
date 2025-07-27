'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  // User Activity & Feedback
  getTestingUsersByUserIdActivity,
  getTestingFeedbackByUserByUserId,
  // Session Registration Management
  deleteTestingSessionsBySessionIdRegister,
  postTestingSessionsBySessionIdRegister,
  getTestingSessionsBySessionIdRegistrations,
  // Session Waitlist Management
  deleteTestingSessionsBySessionIdWaitlist,
  getTestingSessionsBySessionIdWaitlist,
  postTestingSessionsBySessionIdWaitlist,
  // User-focused data
  getTestingMyRequests,
  getTestingAvailableForTesting,
  // Participant management
  deleteTestingRequestsByRequestIdParticipantsByUserId,
  postTestingRequestsByRequestIdParticipantsByUserId,
  getTestingRequestsByRequestIdParticipants,
  getTestingRequestsByRequestIdParticipantsByUserIdCheck,
} from '@/lib/api/generated/sdk.gen';

// =============================================================================
// USER ACTIVITY & ANALYTICS
// =============================================================================

/**
 * Get comprehensive testing activity for a specific user
 */
export async function getUserTestingActivity(userId: string, params?: {
  skip?: number;
  take?: number;
  fromDate?: string;
  toDate?: string;
  requestStatus?: string;
  sessionStatus?: string;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingUsersByUserIdActivity({
      path: { userId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user testing activity');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user testing activity:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user testing activity');
  }
}

/**
 * Get all feedback provided by a specific user
 */
export async function getUserFeedbackHistory(userId: string, params?: {
  skip?: number;
  take?: number;
  fromDate?: string;
  toDate?: string;
  requestId?: string;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingFeedbackByUserByUserId({
      path: { userId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user feedback history');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user feedback history:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user feedback history');
  }
}

/**
 * Get current user's testing requests
 */
export async function getCurrentUserTestingRequests(params?: {
  skip?: number;
  take?: number;
  status?: string;
  fromDate?: string;
  toDate?: string;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingMyRequests({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching current user testing requests:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user testing requests');
  }
}

/**
 * Get available testing opportunities for current user
 */
export async function getAvailableTestingForUser(params?: {
  skip?: number;
  take?: number;
  category?: string;
  difficulty?: string;
  estimatedDuration?: number;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAvailableForTesting({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching available testing opportunities:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch available testing opportunities');
  }
}

// =============================================================================
// USER SESSION REGISTRATION MANAGEMENT  
// =============================================================================

/**
 * Register user for a testing session
 */
export async function registerUserForSession(sessionId: string, userData?: {
  userId?: string;
  notes?: string;
  priority?: number;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSessionsBySessionIdRegister({
      path: { sessionId },
      body: userData,
    });

    if (!response.data) {
      throw new Error('Failed to register user for session');
    }

    revalidateTag('testing-sessions');
    revalidateTag('user-registrations');
    return response.data;
  } catch (error) {
    console.error('Error registering user for session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to register user for session');
  }
}

/**
 * Unregister user from a testing session
 */
export async function unregisterUserFromSession(sessionId: string, userId?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteTestingSessionsBySessionIdRegister({
      path: { sessionId },
      query: userId ? { userId } : undefined,
    });

    if (response.error) {
      throw new Error('Failed to unregister user from session');
    }

    revalidateTag('testing-sessions');
    revalidateTag('user-registrations');
    return { success: true };
  } catch (error) {
    console.error('Error unregistering user from session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to unregister user from session');
  }
}

/**
 * Get all registrations for a testing session
 */
export async function getSessionRegistrations(sessionId: string, params?: {
  skip?: number;
  take?: number;
  status?: string;
  includeUserDetails?: boolean;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsBySessionIdRegistrations({
      path: { sessionId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch session registrations');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching session registrations:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session registrations');
  }
}

// =============================================================================
// USER WAITLIST MANAGEMENT
// =============================================================================

/**
 * Add user to session waitlist
 */
export async function addUserToSessionWaitlist(sessionId: string, userData?: {
  userId?: string;
  priority?: number;
  notes?: string;
  notifyWhenAvailable?: boolean;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSessionsBySessionIdWaitlist({
      path: { sessionId },
      body: userData,
    });

    if (!response.data) {
      throw new Error('Failed to add user to session waitlist');
    }

    revalidateTag('testing-sessions');
    revalidateTag('user-waitlist');
    return response.data;
  } catch (error) {
    console.error('Error adding user to session waitlist:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to add user to session waitlist');
  }
}

/**
 * Remove user from session waitlist
 */
export async function removeUserFromSessionWaitlist(sessionId: string, userId?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteTestingSessionsBySessionIdWaitlist({
      path: { sessionId },
      query: userId ? { userId } : undefined,
    });

    if (response.error) {
      throw new Error('Failed to remove user from session waitlist');
    }

    revalidateTag('testing-sessions');
    revalidateTag('user-waitlist');
    return { success: true };
  } catch (error) {
    console.error('Error removing user from session waitlist:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove user from session waitlist');
  }
}

/**
 * Get session waitlist
 */
export async function getSessionWaitlist(sessionId: string, params?: {
  skip?: number;
  take?: number;
  orderBy?: string;
  includeUserDetails?: boolean;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsBySessionIdWaitlist({
      path: { sessionId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch session waitlist');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching session waitlist:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session waitlist');
  }
}

// =============================================================================
// USER PARTICIPANT MANAGEMENT (REQUESTS)
// =============================================================================

/**
 * Add user as participant to testing request
 */
export async function addUserToTestingRequest(requestId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingRequestsByRequestIdParticipantsByUserId({
      path: { requestId, userId },
    });

    if (!response.data) {
      throw new Error('Failed to add user to testing request');
    }

    revalidateTag('testing-requests');
    revalidateTag('user-participants');
    return response.data;
  } catch (error) {
    console.error('Error adding user to testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to add user to testing request');
  }
}

/**
 * Remove user from testing request participants
 */
export async function removeUserFromTestingRequest(requestId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteTestingRequestsByRequestIdParticipantsByUserId({
      path: { requestId, userId },
    });

    if (response.error) {
      throw new Error('Failed to remove user from testing request');
    }

    revalidateTag('testing-requests');
    revalidateTag('user-participants');
    return { success: true };
  } catch (error) {
    console.error('Error removing user from testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove user from testing request');
  }
}

/**
 * Get all participants for a testing request (enhanced with user details)
 */
export async function getTestingRequestParticipantsEnhanced(requestId: string, params?: {
  skip?: number;
  take?: number;
  includeUserDetails?: boolean;
  status?: string;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByRequestIdParticipants({
      path: { requestId },
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing request participants');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request participants:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing request participants');
  }
}

/**
 * Check if user is participant in testing request
 */
export async function checkUserParticipationInRequest(requestId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByRequestIdParticipantsByUserIdCheck({
      path: { requestId, userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return !!response.data;
  } catch (error) {
    console.error('Error checking user participation in request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to check user participation in request');
  }
}

// =============================================================================
// COMPREHENSIVE USER TESTING DASHBOARD
// =============================================================================

/**
 * Get comprehensive user testing dashboard data (enhanced version)
 */
export async function getComprehensiveUserTestingDashboard(userId: string) {
  await configureAuthenticatedClient();

  try {
    const [activity, feedbackHistory, myRequests, availableTests] = await Promise.all([
      getUserTestingActivity(userId, { take: 10 }),
      getUserFeedbackHistory(userId, { take: 5 }),
      getCurrentUserTestingRequests({ take: 10 }),
      getAvailableTestingForUser({ take: 10 }),
    ]);

    return {
      activity,
      feedbackHistory,
      myRequests,
      availableTests,
      summary: {
        totalActivity: Array.isArray(activity) ? activity.length : 0,
        totalFeedback: Array.isArray(feedbackHistory) ? feedbackHistory.length : 0,
        activeRequests: Array.isArray(myRequests) ? myRequests.length : 0,
        availableOpportunities: Array.isArray(availableTests) ? availableTests.length : 0,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating user testing dashboard:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate user testing dashboard');
  }
}

/**
 * Get user testing statistics and achievements
 */
export async function getUserTestingStats(userId: string, params?: {
  fromDate?: string;
  toDate?: string;
  includeDetailed?: boolean;
}) {
  await configureAuthenticatedClient();

  try {
    const [activity, feedback] = await Promise.all([
      getUserTestingActivity(userId, {
        fromDate: params?.fromDate,
        toDate: params?.toDate,
        take: 1000, // Get all for stats
      }),
      getUserFeedbackHistory(userId, {
        fromDate: params?.fromDate,
        toDate: params?.toDate,
        take: 1000, // Get all for stats
      }),
    ]);

    const activityArray = Array.isArray(activity) ? activity : [];
    const feedbackArray = Array.isArray(feedback) ? feedback : [];

    return {
      stats: {
        totalTestingSessions: activityArray.length,
        totalFeedbackProvided: feedbackArray.length,
        averageSessionsPerMonth: activityArray.length > 0 ? Math.round(activityArray.length / 12) : 0,
        lastActivity: activityArray.length > 0 ? activityArray[0] : null,
        mostActiveMonth: 'TBD', // Would need date analysis
      },
      recentActivity: activityArray.slice(0, 10),
      recentFeedback: feedbackArray.slice(0, 10),
      generatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error generating user testing statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate user testing statistics');
  }
}
