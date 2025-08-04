'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getTestingAttendanceSessions, getTestingAttendanceStudents, postTestingSessionsBySessionIdAttendance } from '@/lib/api/generated/sdk.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// TESTING ATTENDANCE MANAGEMENT
// =============================================================================

/**
 * Get attendance data for students
 */
export async function getStudentAttendanceData() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAttendanceStudents({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching student attendance data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch student attendance data');
  }
}

/**
 * Get attendance data for sessions
 */
export async function getSessionAttendanceData() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAttendanceSessions({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching session attendance data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch session attendance data');
  }
}

/**
 * Mark attendance for a testing session
 */
export async function markSessionAttendance(
  sessionId: string,
  attendanceData: {
    userId: string;
    isPresent: boolean;
    checkInTime?: string;
    checkOutTime?: string;
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

    revalidateTag('testing-attendance');
    revalidateTag('testing-sessions');
    return response.data;
  } catch (error) {
    console.error('Error marking session attendance:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to mark attendance');
  }
}

/**
 * Get comprehensive attendance report
 */
export async function getComprehensiveAttendanceReport() {
  await configureAuthenticatedClient();

  try {
    const [studentData, sessionData] = await Promise.all([getStudentAttendanceData(), getSessionAttendanceData()]);

    return {
      students: studentData,
      sessions: sessionData,
      summary: {
        totalStudents: studentData.length,
        totalSessions: sessionData.length,
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating attendance report:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate attendance report');
  }
}

/**
 * Get testing attendance data by session
 */
export async function getTestingAttendanceBySession(sessionSlug: string) {
  try {
    console.log('🔄 Fetching attendance data for session:', sessionSlug);

    await configureAuthenticatedClient();

    // Try to get attendance data for the specific session
    const studentsResponse = await getTestingAttendanceStudents();
    const sessionsResponse = await getTestingAttendanceSessions();

    // Extract data from SDK responses
    const studentsData = studentsResponse?.data || [];
    const sessionsData = sessionsResponse?.data || [];

    // Return the data structure expected by AttendanceTracker component
    return {
      students: Array.isArray(studentsData) ? studentsData : [],
      sessions: Array.isArray(sessionsData) ? sessionsData : [],
      session: { id: sessionSlug, sessionName: `Session ${sessionSlug}` },
    };
  } catch (error: unknown) {
    console.error('❌ Error fetching attendance data:', error);

    if (error instanceof Error) {
      console.error('Error details:', {
        message: error.message,
      });
    }

    // Return default structure on error
    return {
      students: [],
      sessions: [],
      session: { id: sessionSlug, sessionName: `Session ${sessionSlug}` },
    };
  }
}
