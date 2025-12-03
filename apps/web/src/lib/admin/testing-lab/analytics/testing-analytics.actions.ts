'use server';

// STUB: Testing analytics actions disabled when backend endpoints are unavailable
export async function getTestingRequestAnalytics(_requestId: string) { throw new Error('Not implemented (STUB)'); }
export async function getTestingSessionAnalytics(_sessionId: string) { throw new Error('Not implemented (STUB)'); }
export async function getStudentAttendanceAnalytics() { throw new Error('Not implemented (STUB)'); }
export async function getSessionAttendanceAnalytics() { throw new Error('Not implemented (STUB)'); }
export async function getProjectVersionTestingAnalytics(_projectVersionId: string) { throw new Error('Not implemented (STUB)'); }
export async function getCreatorPerformanceAnalytics(_creatorId: string) { throw new Error('Not implemented (STUB)'); }
export async function getManagerPerformanceAnalytics(_managerId: string) { throw new Error('Not implemented (STUB)'); }
export async function getUserActivityAnalytics(_userId: string) { throw new Error('Not implemented (STUB)'); }
export async function generateBasicTestingReport() { throw new Error('Not implemented (STUB)'); }
