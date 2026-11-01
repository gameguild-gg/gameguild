'use server';

/**
 * Server actions for the content-creator application pipeline (issue #105).
 *
 * The multi-step "be a content creator" wizard (application, creator profile,
 * first course setup) composes into a course proposal submitted through the
 * public creator pipeline (same backend as lib/proposals/actions.ts), with the
 * wizard's structured answers preserved in the proposal metadata.
 */

export interface ContentCreatorApplicationInput {
    /** Step 1 — application. */
    name: string;
    email: string;
    focusArea: string;
    portfolioUrl?: string;
    motivation: string;
    /** Step 2 — creator profile. */
    creatorHandle: string;
    bio: string;
    links?: string;
    preferredFormats: string[];
    /** Step 3 — first course setup. */
    courseTitle: string;
    courseAudience: string;
    courseOutline: string;
    pagePath?: string;
    locale?: string;
}

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

export function validateContentCreatorApplicationStep(
    step: 1 | 2 | 3,
    input: ContentCreatorApplicationInput,
): string | null {
    if (step === 1) {
        if (!input.name?.trim() || input.name.trim().length < 2) {
            return 'Your name is required.';
        }
        if (!input.email?.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(input.email.trim())) {
            return 'A valid email is required.';
        }
        if (!input.focusArea?.trim()) {
            return 'Choose the content area you want to create for.';
        }
        if (!input.motivation?.trim() || input.motivation.trim().length < 20) {
            return 'Tell us why you want to create content (at least 20 characters).';
        }
        return null;
    }

    if (step === 2) {
        if (!input.creatorHandle?.trim() || input.creatorHandle.trim().length < 3) {
            return 'A creator handle of at least 3 characters is required.';
        }
        if (!input.bio?.trim() || input.bio.trim().length < 30) {
            return 'Your creator bio needs at least 30 characters.';
        }
        if (input.preferredFormats.length === 0) {
            return 'Select at least one content format.';
        }
        return null;
    }

    if (!input.courseTitle?.trim() || input.courseTitle.trim().length < 4) {
        return 'Your first course needs a working title of at least 4 characters.';
    }
    if (!input.courseAudience?.trim() || input.courseAudience.trim().length < 10) {
        return 'Describe who the course is for.';
    }
    if (!input.courseOutline?.trim() || input.courseOutline.trim().length < 20) {
        return 'Sketch the course outline (at least 20 characters).';
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
