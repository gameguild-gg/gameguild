'use server';

// STUB: User testing submissions/feedback actions disabled when backend endpoints are unavailable
export async function submitTestingFeedback(_feedbackData: any) { throw new Error('Not implemented (STUB)'); }
export async function reportFeedbackQuality(_feedbackId: string, _reportData: any) { throw new Error('Not implemented (STUB)'); }
export async function rateFeedbackQuality(_feedbackId: string, _qualityData: any) { throw new Error('Not implemented (STUB)'); }
export async function completeTestingSession(_sessionData: any) { throw new Error('Not implemented (STUB)'); }
export async function submitQuickTestingFeedback(_quickFeedbackData: any) { throw new Error('Not implemented (STUB)'); }
