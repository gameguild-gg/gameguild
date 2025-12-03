'use server';

// STUB: Testing attendance actions disabled when backend endpoints are unavailable
export async function getStudentAttendanceData() { throw new Error('Not implemented (STUB)'); }
export async function getSessionAttendanceData() { throw new Error('Not implemented (STUB)'); }
export async function markSessionAttendance(_sessionId: string, _attendanceData: any) { throw new Error('Not implemented (STUB)'); }
export async function getComprehensiveAttendanceReport() { throw new Error('Not implemented (STUB)'); }
export async function getTestingAttendanceBySession(_sessionSlug: string): Promise<{ students: unknown[]; sessions: unknown[]; session?: { id: string; sessionName: string; } }> {
  console.warn('Testing Lab attendance stub called');
  return { students: [], sessions: [] };
}
