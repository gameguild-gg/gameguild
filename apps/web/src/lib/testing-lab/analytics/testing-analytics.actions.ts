'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getTestingRequestsByRequestIdStatistics,
  getTestingSessionsBySessionIdStatistics,
  getTestingAttendanceStudents,
  getTestingAttendanceSessions,
  getTestingRequestsByCreatorByCreatorId,
  getTestingSessionsByManagerByManagerId,
  getTestingRequestsByProjectVersionByProjectVersionId,
  getTestingUsersByUserIdActivity,
} from '@/lib/api/generated/sdk.gen';

// =============================================================================
// TESTING ANALYTICS & STATISTICS
// =============================================================================

/**
 * Get comprehensive statistics for a testing request
 */
export async function getTestingRequestAnalytics(requestId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByRequestIdStatistics({
      path: { requestId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch request statistics');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch request analytics');
  }
}

/**
 * Get comprehensive statistics for a testing session
 */
export async function getTestingSessionAnalytics(sessionId: string) {
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
    console.error('Error fetching testing session analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session analytics');
  }
}

/**
 * Get basic attendance analytics for students
 */
export async function getStudentAttendanceAnalytics() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAttendanceStudents({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    const attendanceData = Array.isArray(response.data) ? response.data : [];

    return {
      totalStudents: attendanceData.length,
      attendanceData,
      summary: 'Student attendance data retrieved successfully',
    };
  } catch (error) {
    console.error('Error fetching student attendance analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch student attendance analytics');
  }
}

/**
 * Get basic attendance analytics for sessions
 */
export async function getSessionAttendanceAnalytics() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAttendanceSessions({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    const sessionData = Array.isArray(response.data) ? response.data : [];

    return {
      totalSessions: sessionData.length,
      sessionData,
      summary: 'Session attendance data retrieved successfully',
    };
  } catch (error) {
    console.error('Error fetching session attendance analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session attendance analytics');
  }
}

/**
 * Get analytics for a specific project version
 */
export async function getProjectVersionTestingAnalytics(projectVersionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByProjectVersionByProjectVersionId({
      path: { projectVersionId },
      headers: { 'Cache-Control': 'no-store' },
    });

    const requestsData = response.data || [];

    return {
      projectVersionId,
      summary: {
        totalRequests: requestsData.length,
        requestsData,
      },
      generatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error fetching project version analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch project version analytics');
  }
}

/**
 * Get creator performance analytics
 */
export async function getCreatorPerformanceAnalytics(creatorId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByCreatorByCreatorId({
      path: { creatorId },
      headers: { 'Cache-Control': 'no-store' },
    });

    const requestsData = response.data || [];

    return {
      creatorId,
      performance: {
        totalRequestsCreated: requestsData.length,
        requests: requestsData,
      },
      generatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error fetching creator performance analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch creator performance analytics');
  }
}

/**
 * Get manager performance analytics
 */
export async function getManagerPerformanceAnalytics(managerId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingSessionsByManagerByManagerId({
      path: { managerId },
      headers: { 'Cache-Control': 'no-store' },
    });

    const sessionsData = response.data || [];

    return {
      managerId,
      performance: {
        totalSessionsManaged: sessionsData.length,
        sessions: sessionsData,
      },
      generatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error fetching manager performance analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch manager performance analytics');
  }
}

/**
 * Get user activity analytics for a specific user
 */
export async function getUserActivityAnalytics(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingUsersByUserIdActivity({
      path: { userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user activity data');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user activity analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user activity analytics');
  }
}

/**
 * Generate a basic testing performance report
 */
export async function generateBasicTestingReport() {
  await configureAuthenticatedClient();

  try {
    const [studentAttendance, sessionAttendance] = await Promise.all([getStudentAttendanceAnalytics(), getSessionAttendanceAnalytics()]);

    return {
      summary: {
        totalStudentsTracked: studentAttendance.totalStudents,
        totalSessionsTracked: sessionAttendance.totalSessions,
      },
      attendanceAnalytics: {
        students: studentAttendance,
        sessions: sessionAttendance,
      },
      generatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error generating testing performance report:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate performance report');
  }
}
