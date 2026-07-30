'use server';

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';

export type EnrollmentActionResult = { success: true } | { success: false; error: string };

function extractError(error: unknown): string {
    const candidate = error as { detail?: string; message?: string } | undefined;
    return candidate?.detail || candidate?.message || 'Enrollment could not be completed.';
}

export async function enrollInCourse(courseId: string): Promise<EnrollmentActionResult> {
    try {
        const token = await getToken();
        if (!token) {
            return { success: false, error: 'Your session expired. Sign in again.' };
        }

        const client = createServerClient({
            baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295',
            auth: { getAccessToken: async () => token },
        });
        const result = await new GeneratedApi.LearningCoursesProgramModule(client).postCoursesSelfEnroll(courseId);

        return result.ok ? { success: true } : { success: false, error: extractError(result.error) };
    } catch (error) {
        return { success: false, error: extractError(error) };
    }
}