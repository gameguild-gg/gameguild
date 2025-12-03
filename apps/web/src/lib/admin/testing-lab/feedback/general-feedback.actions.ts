'use server';

// STUB: Testing feedback/general actions disabled when backend endpoints are unavailable
export async function getTestingFeedbackByUser(_userId: string) { throw new Error('Not implemented (STUB)'); }
export async function getMyTestingFeedback() { throw new Error('Not implemented (STUB)'); }
export async function submitGeneralTestingFeedback(_feedbackData: any) { throw new Error('Not implemented (STUB)'); }
export async function reportTestingFeedback(_feedbackId: string, _reportData: any) { throw new Error('Not implemented (STUB)'); }
export async function rateTestingFeedbackQuality(_feedbackId: string, _quality: any) { throw new Error('Not implemented (STUB)'); }
export async function getTestingUserActivity(_userId: string) { throw new Error('Not implemented (STUB)'); }
export async function getTestingAttendanceStudentsData() { throw new Error('Not implemented (STUB)'); }
export async function getTestingAttendanceSessionsData() { throw new Error('Not implemented (STUB)'); }
export async function submitSimpleTestingRequest(_requestData: any) { throw new Error('Not implemented (STUB)'); }
export async function getUserTestingDashboard(_userId?: string) { throw new Error('Not implemented (STUB)'); }
export async function getComprehensiveAttendanceData() { throw new Error('Not implemented (STUB)'); }
export const getTestingFeedbacks = getMyTestingFeedback;
export async function getTestingFeedbackById(_feedbackId: string) { throw new Error('Not implemented (STUB)'); }
export async function getTestingFeedbacksBySession(_sessionSlug: string) { throw new Error('Not implemented (STUB)'); }
