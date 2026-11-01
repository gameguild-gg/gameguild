'use server';

/**
 * Server actions for the content-creator application pipeline (issue #105).
 *
 * The multi-step "be a content creator" wizard (application, creator profile,
 * first course setup) composes into a course proposal submitted through the
 * public creator pipeline (same backend as lib/proposals/actions.ts), with the
 * wizard's structured answers preserved in the proposal metadata.
 *
 * The wizard's shared input type and synchronous step validator live in
 * `./validation` — a `"use server"` module may only export async functions, so
 * the client-side validator must not be re-exported from here.
 */

import {
    validateContentCreatorApplicationStep,
    type ContentCreatorApplicationInput,
} from '@/lib/content-creators/validation';

export type ContentCreatorApplicationResult =
    | { success: true; message: string }
    | { success: false; error: string };

function getApiUrl(): string {
    return (process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080').replace(/\/$/, '');
}

function getFirstProblemDetailError(payload: unknown): string | null {
    if (!payload || typeof payload !== 'object') {
        return null;
    }

    const candidate = payload as {
        title?: unknown;
        detail?: unknown;
        error?: unknown;
        errors?: Record<string, unknown>;
    };

    if (candidate.errors && typeof candidate.errors === 'object') {
        for (const value of Object.values(candidate.errors)) {
            if (Array.isArray(value) && value.length > 0 && typeof value[0] === 'string') {
                return value[0];
            }
        }
    }

    if (typeof candidate.detail === 'string' && candidate.detail.trim().length > 0) {
        return candidate.detail;
    }

    if (typeof candidate.title === 'string' && candidate.title.trim().length > 0) {
        return candidate.title;
    }

    if (typeof candidate.error === 'string' && candidate.error.trim().length > 0) {
        return candidate.error;
    }

    return null;
}

/** Submits the completed wizard as a course proposal with structured metadata. */
export async function submitContentCreatorApplicationAction(
    input: ContentCreatorApplicationInput,
): Promise<ContentCreatorApplicationResult> {
    for (const step of [1, 2, 3] as const) {
        const stepError = validateContentCreatorApplicationStep(step, input);
        if (stepError) {
            return { success: false, error: stepError };
        }
    }

    const pitch = [
        `Motivation: ${input.motivation.trim()}`,
        '',
        `First course outline: ${input.courseOutline.trim()}`,
    ].join('\n');

    const metadata = JSON.stringify({
        source: 'content-creator-application',
        focusArea: input.focusArea.trim(),
        portfolioUrl: input.portfolioUrl?.trim() || undefined,
        creatorHandle: input.creatorHandle.trim(),
        bio: input.bio.trim(),
        links: input.links?.trim() || undefined,
        preferredFormats: input.preferredFormats,
        courseOutline: input.courseOutline.trim(),
        pagePath: input.pagePath,
        submittedAt: new Date().toISOString(),
    });

    try {
        const response = await fetch(`${getApiUrl()}/v1/content/course-proposals`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                courseTitle: input.courseTitle.trim(),
                pitch,
                audience: input.courseAudience.trim(),
                proposerName: input.name.trim(),
                proposerEmail: input.email.trim(),
                metadata,
            }),
            cache: 'no-store',
        });

        if (!response.ok) {
            let payload: unknown = null;
            try {
                payload = await response.json();
            } catch {
                payload = null;
            }
            return {
                success: false,
                error: getFirstProblemDetailError(payload) || 'Failed to submit your application. Please try again.',
            };
        }

        return {
            success: true,
            message: 'Application received! Our team will review it and reach out with next steps.',
        };
    } catch (error) {
        return {
            success: false,
            error: error instanceof Error ? error.message : 'Failed to submit your application. Please try again.',
        };
    }
}
