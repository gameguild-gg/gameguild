/**
 * Shared, synchronous validation for the content-creator application wizard
 * (issue #105). Kept out of `actions.ts` because a `"use server"` module may
 * only export async functions; the wizard runs this validator on the client
 * between steps, before any server action is invoked.
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

/**
 * Validates one wizard step of the content-creator application.
 * Returns a user-facing error message, or null when the step is complete.
 */
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
