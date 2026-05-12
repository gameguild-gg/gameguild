'use server';

import { getToken } from '@/auth';
import { createServerClient, GeneratedApi } from '@game-guild/client';

type ActionResult =
    | { success: true }
    | { success: false; error: string };

function getApiClient() {
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    return createServerClient({
        baseUrl: apiUrl,
        auth: { getAccessToken: () => getToken() },
    });
}

function createCourseModules() {
    const client = getApiClient();

    return {
        programs: new GeneratedApi.LearningCoursesProgramModule(client),
    };
}

function extractError(error: unknown): string {
    const candidate = error as { message?: string; detail?: string } | undefined;
    return candidate?.detail || candidate?.message || 'Unable to update course progress.';
}

export async function beginCourseContent(courseId: string, contentId: string): Promise<ActionResult> {
    try {
        const { programs } = createCourseModules();
        const result = await programs.putCoursesMeProgress(courseId, {
            status: 'InProgress',
            lastAccessedAt: new Date().toISOString(),
            additionalData: {
                content: {
                    contentId,
                    completionPercentage: 1,
                },
            },
        });

        if (!result.ok) {
            return { success: false, error: extractError(result.error) };
        }

        return { success: true };
    } catch (error) {
        return { success: false, error: extractError(error) };
    }
}

export async function completeCourseContent(courseId: string, contentId: string): Promise<ActionResult> {
    try {
        const { programs } = createCourseModules();
        const result = await programs.postCoursesMeContentComplete(courseId, contentId);

        if (!result.ok) {
            return { success: false, error: extractError(result.error) };
        }

        return { success: true };
    } catch (error) {
        return { success: false, error: extractError(error) };
    }
}
