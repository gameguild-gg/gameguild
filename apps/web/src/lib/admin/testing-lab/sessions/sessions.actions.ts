'use server'

// STUB: Testing sessions actions disabled when backend endpoints are unavailable
export async function getTestingSessionsAction(): Promise<any[]> { throw new Error('Not implemented (STUB)'); }
export async function searchTestingSessionsAction(_query: string): Promise<any[]> { throw new Error('Not implemented (STUB)'); }
export async function getAvailableTestSessions(): Promise<any[]> { throw new Error('Not implemented (STUB)'); }
export async function getTestingLocationsAction(): Promise<any[]> { throw new Error('Not implemented (STUB)'); }
export async function createTestingSessionAction(_data: any): Promise<{ success: boolean; data?: any; error?: string }> { throw new Error('Not implemented (STUB)'); }
export async function deleteTestingSessionAction(_sessionId: string): Promise<{ success: boolean; error?: string }> { throw new Error('Not implemented (STUB)'); }
export async function getSessionEnrollmentRequestsAction(_sessionId: string): Promise<{ success: boolean; data?: any[]; error?: string }> { throw new Error('Not implemented (STUB)'); }
export async function processEnrollmentDecisionAction(_enrollmentId: string, _decision: 'approved' | 'rejected', _adminMessage?: string): Promise<{ success: boolean; error?: string }> { throw new Error('Not implemented (STUB)'); }
export async function getTestingSessionByIdAction(_sessionId: string): Promise<any | null> { throw new Error('Not implemented (STUB)'); }
export async function getTestSessionBySlug(_slug: string): Promise<any | null> { throw new Error('Not implemented (STUB)'); }
// Alias to satisfy import sites expecting the old name
export const getTestingSessionBySlug = getTestSessionBySlug;
