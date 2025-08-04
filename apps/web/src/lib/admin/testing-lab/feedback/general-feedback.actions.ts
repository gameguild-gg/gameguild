'use server';

import { environment } from '@/configs/environment';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getTestingAttendanceSessions,
  getTestingAttendanceStudents,
  getTestingFeedbackByUserByUserId,
  getTestingUsersByUserIdActivity,
  postTestingFeedback,
  postTestingFeedbackByFeedbackIdQuality,
  postTestingFeedbackByFeedbackIdReport,
  postTestingSubmitSimple,
} from '@/lib/api/generated/sdk.gen';
import type { CreateSimpleTestingRequestDto, TestingFeedback, TestingRequest } from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';
import { getAvailableTestingOpportunities, getMyTestingRequests } from '../requests/requests.actions';

// =============================================================================
// TESTING FEEDBACK MANAGEMENT
// =============================================================================

/**
 * Get testing feedback for a specific user
 */
export async function getTestingFeedbackByUser(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingFeedbackByUserByUserId({
      path: { userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user feedback');
    }

    return response.data as TestingFeedback[];
  } catch (error) {
    console.error('Error fetching user feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user feedback');
  }
}

/**
 * Get current user's testing feedback
 */
export async function getMyTestingFeedback() {
  // Configure client and get session in one call
  const session = await configureAuthenticatedClient();

  if (!session?.user?.id) {
    throw new Error('User ID not found in session');
  }

  try {
    console.log('Making request to getTestingFeedbackByUserByUserId with userId:', session.user.id);
    console.log('Session token exists:', !!session?.api?.accessToken);
    console.log('Current tenant ID:', session?.currentTenant?.id);

    // First, let's try a simple API call to test if authentication works
    console.log('Testing authentication with a simple API call...');

    const response = await getTestingFeedbackByUserByUserId({
      path: { userId: session.user.id },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    console.log('Response received:', response);
    console.log('Response status:', response?.response?.status);
    console.log('Response data:', response?.data);

    if (!response.data) {
      console.log('No data in response, but no error thrown - this might be normal if user has no feedback yet');
      return {
        testingFeedbacks: [],
        total: 0,
      };
    }

    return {
      testingFeedbacks: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching testing feedbacks:', error);
    console.error('Error details:', {
      message: error instanceof Error ? error.message : 'Unknown error',
      stack: error instanceof Error ? error.stack : undefined,
      cause: error instanceof Error ? error.cause : undefined,
      name: error instanceof Error ? error.name : undefined,
    });
    console.error('User ID:', session?.user?.id);
    console.error('API Base URL from env:', process.env.NEXT_PUBLIC_API_URL);
    console.error('API Base URL from config:', environment.apiBaseUrl);

    // If it's a 401 error, it's likely authentication issue
    if (error instanceof Error && error.message.includes('401')) {
      throw new Error('Authentication failed - please try signing in again');
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing feedbacks');
  }
}

/**
 * Submit general testing feedback
 */
export async function submitGeneralTestingFeedback(feedbackData: { testingRequestId: string; feedbackResponses: string; overallRating?: number; wouldRecommend?: boolean; additionalNotes?: string; sessionId?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingFeedback({
      body: feedbackData,
    });

    if (!response.data) {
      throw new Error('Failed to submit feedback');
    }

    revalidateTag('testing-requests');
    return response.data;
  } catch (error) {
    console.error('Error submitting feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit feedback');
  }
}

/**
 * Report feedback as inappropriate or spam
 */
export async function reportTestingFeedback(
  feedbackId: string,
  reportData: {
    reason: string;
    details?: string;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingFeedbackByFeedbackIdReport({
      path: { feedbackId },
      body: reportData,
    });

    if (!response.data) {
      throw new Error('Failed to report feedback');
    }

    revalidateTag('testing-requests');
    return response.data;
  } catch (error) {
    console.error('Error reporting feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to report feedback');
  }
}

/**
 * Rate the quality of feedback
 */
export async function rateTestingFeedbackQuality(
  feedbackId: string,
  quality: 1 | 2 | 3, // FeedbackQuality enum values
) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingFeedbackByFeedbackIdQuality({
      path: { feedbackId },
      body: { quality },
    });

    if (!response.data) {
      throw new Error('Failed to rate feedback quality');
    }

    revalidateTag('testing-requests');
    return response.data;
  } catch (error) {
    console.error('Error rating feedback quality:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to rate feedback quality');
  }
}

// =============================================================================
// USER ACTIVITY & DASHBOARD
// =============================================================================

/**
 * Get testing activity for a specific user
 */
export async function getTestingUserActivity(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingUsersByUserIdActivity({
      path: { userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user activity');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user activity:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user activity');
  }
}

// =============================================================================
// TESTING ATTENDANCE MANAGEMENT
// =============================================================================

/**
 * Get student attendance data
 */
export async function getTestingAttendanceStudentsData() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAttendanceStudents({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching student attendance data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch student attendance data');
  }
}

/**
 * Get session attendance data
 */
export async function getTestingAttendanceSessionsData() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAttendanceSessions({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data || [];
  } catch (error) {
    console.error('Error fetching session attendance data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session attendance data');
  }
}

// =============================================================================
// TESTING SUBMISSIONS
// =============================================================================

/**
 * Submit a simple testing request (quick creation)
 */
export async function submitSimpleTestingRequest(requestData: {
  title: string;
  description?: string;
  versionNumber: string;
  downloadUrl?: string;
  instructionsType: 'inline' | 'file' | 'url';
  instructionsContent?: string;
  instructionsUrl?: string;
  feedbackFormContent?: string;
  maxTesters?: number;
  startDate?: string;
  endDate?: string;
  teamIdentifier: string;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingSubmitSimple({
      body: requestData as unknown as CreateSimpleTestingRequestDto,
    });

    if (!response.data) {
      throw new Error('Failed to submit simple testing request');
    }

    revalidateTag('testing-requests');
    return response.data as TestingRequest;
  } catch (error) {
    console.error('Error submitting simple testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit simple testing request');
  }
}

// =============================================================================
// UTILITY FUNCTIONS
// =============================================================================

/**
 * Get comprehensive user testing dashboard data
 */
export async function getUserTestingDashboard(userId?: string) {
  await configureAuthenticatedClient();

  try {
    const [myRequests, availableOpportunities, myFeedback, userActivity] = await Promise.all([getMyTestingRequests(), getAvailableTestingOpportunities(), getMyTestingFeedback(), userId ? getTestingUserActivity(userId) : null]);

    return {
      myRequests,
      availableOpportunities,
      myFeedback,
      userActivity,
    };
  } catch (error) {
    console.error('Error fetching user testing dashboard:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user testing dashboard');
  }
}

/**
 * Get comprehensive attendance data
 */
export async function getComprehensiveAttendanceData() {
  await configureAuthenticatedClient();

  try {
    const [studentData, sessionData] = await Promise.all([getTestingAttendanceStudentsData(), getTestingAttendanceSessionsData()]);

    return {
      students: Array.isArray(studentData) ? studentData : [],
      sessions: Array.isArray(sessionData) ? sessionData : [],
    };
  } catch (error) {
    console.error('Error fetching comprehensive attendance data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch comprehensive attendance data');
  }
}

// =============================================================================
// COMPONENT-FRIENDLY ALIASES
// =============================================================================

/**
 * Get testing feedbacks (alias for getMyTestingFeedback)
 */
export const getTestingFeedbacks = getMyTestingFeedback;

/**
 * Get testing feedback by ID
 */
export async function getTestingFeedbackById(feedbackId: string) {
  const session = await configureAuthenticatedClient();

  try {
    console.log('Fetching testing feedback by ID:', feedbackId);
    console.log('Session token exists:', !!session?.api?.accessToken);
    console.log('Current tenant ID:', session?.currentTenant?.id);

    // For now, get user's feedback and find the one with matching ID
    // This could be optimized with a specific endpoint when available
    const response = await getTestingFeedbackByUserByUserId({
      path: { userId: session?.user?.id || '' },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    console.log('Feedback response received:', response);

    if (!response.data || !Array.isArray(response.data)) {
      return null;
    }

    // Find the feedback with the matching ID
    const feedback = response.data.find((item: { id?: string }) => item.id === feedbackId);
    return feedback || null;
  } catch (error) {
    console.error('Error fetching testing feedback by ID:', error);

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

    return null; // Return null instead of throwing to handle gracefully
  }
}

/**
 * Get testing feedbacks by session
 */
export async function getTestingFeedbacksBySession(sessionSlug: string) {
  const session = await configureAuthenticatedClient();

  try {
    console.log('Fetching testing feedbacks by session:', sessionSlug);
    console.log('Session token exists:', !!session?.api?.accessToken);
    console.log('Current tenant ID:', session?.currentTenant?.id);

    // For now, get user's feedback since we don't have session-specific endpoint
    // This could be enhanced when such an endpoint becomes available
    const response = await getTestingFeedbackByUserByUserId({
      path: { userId: session?.user?.id || '' },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    console.log('Feedback response received:', response);

    if (!response.data) {
      return {
        testingFeedbacks: [],
        total: 0,
      };
    }

    return {
      testingFeedbacks: Array.isArray(response.data) ? response.data : [response.data],
      total: Array.isArray(response.data) ? response.data.length : 1,
    };
  } catch (error) {
    console.error('Error fetching testing feedbacks by session:', error);

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

    return {
      testingFeedbacks: [],
      total: 0,
    };
  }
}
