'use server';

// STUB: User testing actions disabled when backend endpoints are unavailable
export async function getUserTestingActivity(_userId: string, _params?: any) { throw new Error('Not implemented (STUB)'); }
export async function getUserFeedbackHistory(_userId: string, _params?: any) { throw new Error('Not implemented (STUB)'); }
export async function getCurrentUserTestingRequests(_params?: any) { throw new Error('Not implemented (STUB)'); }
export async function getAvailableTestingForUser(_params?: any) { throw new Error('Not implemented (STUB)'); }
export async function registerUserForSession(_sessionId: string, _userData?: any) { throw new Error('Not implemented (STUB)'); }
export async function unregisterUserFromSession(_sessionId: string, _userId?: string) { throw new Error('Not implemented (STUB)'); }
export async function getSessionRegistrations(_sessionId: string, _params?: any) { throw new Error('Not implemented (STUB)'); }
export async function addUserToSessionWaitlist(_sessionId: string, _userData?: any) { throw new Error('Not implemented (STUB)'); }
export async function removeUserFromSessionWaitlist(_sessionId: string, _userId?: string) { throw new Error('Not implemented (STUB)'); }
export async function getSessionWaitlist(_sessionId: string, _params?: any) { throw new Error('Not implemented (STUB)'); }
export async function addUserToTestingRequest(_requestId: string, _userId: string) { throw new Error('Not implemented (STUB)'); }
export async function removeUserFromTestingRequest(_requestId: string, _userId: string) { throw new Error('Not implemented (STUB)'); }
export async function getTestingRequestParticipantsEnhanced(_requestId: string, _params?: any) { throw new Error('Not implemented (STUB)'); }
export async function checkUserParticipationInRequest(_requestId: string, _userId: string) { throw new Error('Not implemented (STUB)'); }
export async function getComprehensiveUserTestingDashboard(_userId: string) { throw new Error('Not implemented (STUB)'); }
export async function getUserTestingStats(_userId: string, _params?: any) { throw new Error('Not implemented (STUB)'); }
