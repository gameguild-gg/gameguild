'use server';

// STUB: Testing requests actions disabled when backend endpoints are unavailable
export type TestingRequestActionResult<T = unknown> = { success: boolean; data?: T; error?: string };
export type EnhancedTestingRequest = any;
export async function getTestingRequestsAction(): Promise<TestingRequestActionResult<any[]>> { throw new Error('Not implemented (STUB)'); }
export async function searchTestingRequestsAction(_args: { query: { searchTerm: string } }): Promise<TestingRequestActionResult<any[]>> { throw new Error('Not implemented (STUB)'); }
export async function getTestingRequestsWithDetailsAction(): Promise<TestingRequestActionResult<EnhancedTestingRequest[]>> { throw new Error('Not implemented (STUB)'); }
