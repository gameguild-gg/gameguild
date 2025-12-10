'use server';

// STUB: Testing request participant actions disabled when backend endpoints are unavailable
export async function removeParticipantFromRequest(_requestId: string, _userId: string) { throw new Error('Not implemented (STUB)'); }
export async function addParticipantToRequest(_requestId: string, _userId: string) { throw new Error('Not implemented (STUB)'); }
export async function getTestingRequestParticipants(_requestId: string) { throw new Error('Not implemented (STUB)'); }
export async function checkUserParticipation(_requestId: string, _userId: string) { throw new Error('Not implemented (STUB)'); }
export const joinTestingRequest = addParticipantToRequest;
export const leaveTestingRequest = removeParticipantFromRequest;
export const checkTestingRequestParticipation = checkUserParticipation;
