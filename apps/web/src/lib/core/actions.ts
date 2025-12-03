'use server';

/**
 * Stub implementations for core actions.
 * General utility actions used across the application.
 */

export async function reportContent(_data: {
    contentType: string;
    contentId: string;
    reason: string;
    description?: string;
}) {
    return { success: true, message: 'Report submitted (stub)' };
}

export async function submitReport(_data: {
    reportType?: string;
    contentType?: string;
    targetId?: string;
    contentId?: string;
    targetTitle?: string;
    reason: string;
    description?: string;
}) {
    return { success: true, message: 'Report submitted (stub)' };
}

export async function getContentReports(_contentType: string, _contentId: string) {
    return { data: [], error: null };
}
