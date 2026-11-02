'use server';

/**
 * Server actions and queries for the mentorship program (issue #102).
 *
 * Uses plain fetch against the public API endpoints (same pattern as
 * lib/proposals/actions.ts). Public directory reads need no session; profile,
 * availability, and session mutations attach the signed-in user's bearer token.
 */

import { getToken } from '@/auth';

export interface MentorSkillView {
    id: string;
    skill: string;
}

export interface MentorProfileView {
    id: string;
    userId: string;
    displayName: string;
    headline: string | null;
    bio: string | null;
    role: number;
    pricingModel: number;
    freeContributionReason: string | null;
    status: number;
    bookingMode: number;
    offersIntroductoryCall: boolean;
    introductoryCallMinutes: number;
    timezoneId: string;
    skills: MentorSkillView[];
    publishedAt: string | null;
    createdAt: string;
    updatedAt: string;
}

export interface MentorAvailabilitySlotView {
    id: string;
    mentorProfileId: string;
    startsAt: string;
    endsAt: string;
    status: number;
}

export interface MentorshipSessionView {
    id: string;
    mentorProfileId: string;
    mentorUserId: string;
    menteeUserId: string;
    slotId: string | null;
    topic: string;
    notes: string | null;
    startsAt: string;
    endsAt: string;
    status: number;
    meetingChannel: number;
    meetingUrl: string | null;
    isIntroductory: boolean;
    cancellationReason: string | null;
    cancelledAt: string | null;
    completedAt: string | null;
    createdAt: string;
}

export interface MentorDashboardView {
    profile: MentorProfileView | null;
    upcomingSessionCount: number;
    openSlotCount: number;
    completedSessionCount: number;
    totalSessionCount: number;
    upcomingSessions: MentorshipSessionView[];
}

export interface MentorProfileInput {
    displayName: string;
    headline?: string;
    bio?: string;
    role: number;
    pricingModel: number;
    freeContributionReason?: string;
    bookingMode: number;
    offersIntroductoryCall: boolean;
    introductoryCallMinutes: number;
    timezoneId?: string;
    skills: string[];
}

export interface AvailabilitySlotInput {
    startsAt: string;
    endsAt: string;
}

type ActionResult = { success: true; message: string } | { success: false; error: string };

type DataResult<T> = { success: true; data: T } | { success: false; error: string };

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

async function requestJson<T>(
    path: string,
    init: { method: 'GET' | 'POST' | 'PUT' | 'DELETE'; body?: unknown; auth?: boolean },
): Promise<DataResult<T>> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };

    if (init.auth) {
        const token = await getToken();
        if (!token) {
            return { success: false, error: 'Sign in to continue.' };
        }
        headers.Authorization = `Bearer ${token}`;
    }

    try {
        const response = await fetch(`${getApiUrl()}${path}`, {
            method: init.method,
            headers,
            body: init.body === undefined ? undefined : JSON.stringify(init.body),
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
                error: getFirstProblemDetailError(payload) || `Request failed (${response.status}).`,
            };
        }

        if (response.status === 204) {
            return { success: true, data: undefined as T };
        }

        return { success: true, data: (await response.json()) as T };
    } catch (error) {
        return {
            success: false,
            error: error instanceof Error ? error.message : 'Request failed. Please try again.',
        };
    }
}

// ─────────────────────────── Public directory ───────────────────────────

/** Loads published mentors, optionally filtered by skill (issue #102 matching). */
export async function listMentors(filters: { skill?: string; search?: string } = {}): Promise<MentorProfileView[]> {
    const params = new URLSearchParams();
    if (filters.skill?.trim()) params.set('skill', filters.skill.trim());
    if (filters.search?.trim()) params.set('search', filters.search.trim());
    const query = params.size > 0 ? `?${params.toString()}` : '';

    const result = await requestJson<MentorProfileView[]>(`/v1/mentorship/mentors${query}`, { method: 'GET' });
    return result.success ? result.data : [];
}

/** Loads one published mentor profile; null when absent or unpublished. */
export async function getMentor(profileId: string): Promise<MentorProfileView | null> {
    if (!/^[0-9a-f-]{36}$/i.test(profileId)) return null;
    const result = await requestJson<MentorProfileView>(
        `/v1/mentorship/mentors/${encodeURIComponent(profileId)}`,
        { method: 'GET' },
    );
    return result.success ? result.data : null;
}

/** Loads the open availability slots of a published mentor. */
export async function listMentorAvailability(profileId: string): Promise<MentorAvailabilitySlotView[]> {
    if (!/^[0-9a-f-]{36}$/i.test(profileId)) return [];
    const result = await requestJson<MentorAvailabilitySlotView[]>(
        `/v1/mentorship/mentors/${encodeURIComponent(profileId)}/availability`,
        { method: 'GET' },
    );
    return result.success ? result.data : [];
}

// ─────────────────────────── Self-service ───────────────────────────

export async function getMyMentorProfileAction(): Promise<MentorProfileView | null> {
    const result = await requestJson<MentorProfileView>('/v1/mentorship/profile', { method: 'GET', auth: true });
    return result.success ? result.data : null;
}

export async function getMentorDashboardAction(): Promise<MentorDashboardView | null> {
    const result = await requestJson<MentorDashboardView>('/v1/mentorship/profile/dashboard', {
        method: 'GET',
        auth: true,
    });
    return result.success ? result.data : null;
}

export async function upsertMentorProfileAction(input: MentorProfileInput): Promise<ActionResult> {
    if (!input.displayName?.trim() || input.displayName.trim().length < 2) {
        return { success: false, error: 'A display name of at least 2 characters is required.' };
    }

    if (input.pricingModel === 0 && !input.freeContributionReason?.trim()) {
        return {
            success: false,
            error: 'Explain why you mentor for free — it is required for free programs (issue #102).',
        };
    }

    const result = await requestJson<MentorProfileView>('/v1/mentorship/profile', {
        method: 'PUT',
        auth: true,
        body: {
            displayName: input.displayName.trim(),
            headline: input.headline?.trim() || null,
            bio: input.bio?.trim() || null,
            role: input.role,
            pricingModel: input.pricingModel,
            freeContributionReason: input.freeContributionReason?.trim() || null,
            bookingMode: input.bookingMode,
            offersIntroductoryCall: input.offersIntroductoryCall,
            introductoryCallMinutes: input.introductoryCallMinutes,
            timezoneId: input.timezoneId?.trim() || 'UTC',
            skills: input.skills.map((skill) => skill.trim()).filter(Boolean),
        },
    });

    return result.success
        ? { success: true, message: 'Mentor profile saved.' }
        : { success: false, error: result.error };
}

export async function setMentorProfileStatusAction(status: number): Promise<ActionResult> {
    if (![0, 1, 2].includes(status)) {
        return { success: false, error: 'Unknown mentor profile status.' };
    }

    const result = await requestJson<MentorProfileView>('/v1/mentorship/profile/status', {
        method: 'POST',
        auth: true,
        body: { status },
    });

    return result.success
        ? { success: true, message: 'Mentor profile updated.' }
        : { success: false, error: result.error };
}

export async function publishMentorAvailabilityAction(slots: AvailabilitySlotInput[]): Promise<ActionResult> {
    if (slots.length === 0) {
        return { success: false, error: 'Add at least one availability range.' };
    }

    const result = await requestJson<MentorAvailabilitySlotView[]>('/v1/mentorship/profile/availability', {
        method: 'POST',
        auth: true,
        body: { slots: slots.map((slot) => ({ startsAt: slot.startsAt, endsAt: slot.endsAt })) },
    });

    return result.success
        ? { success: true, message: 'Availability published.' }
        : { success: false, error: result.error };
}

export async function removeMentorAvailabilitySlotAction(slotId: string): Promise<ActionResult> {
    const result = await requestJson<unknown>(
        `/v1/mentorship/profile/availability/${encodeURIComponent(slotId)}`,
        { method: 'DELETE', auth: true },
    );

    return result.success
        ? { success: true, message: 'Availability slot removed.' }
        : { success: false, error: result.error };
}

// ─────────────────────────── Sessions ───────────────────────────

export async function listMyMentorshipSessionsAction(): Promise<MentorshipSessionView[]> {
    const result = await requestJson<MentorshipSessionView[]>('/v1/mentorship/sessions', {
        method: 'GET',
        auth: true,
    });
    return result.success ? result.data : [];
}

export async function bookMentorshipSessionAction(input: {
    slotId: string;
    topic: string;
    notes?: string;
}): Promise<ActionResult> {
    if (!input.slotId) {
        return { success: false, error: 'Choose an availability slot to book.' };
    }
    if (!input.topic?.trim() || input.topic.trim().length < 4) {
        return { success: false, error: 'Describe the topic for the session (at least 4 characters).' };
    }

    const result = await requestJson<MentorshipSessionView>('/v1/mentorship/sessions', {
        method: 'POST',
        auth: true,
        body: {
            slotId: input.slotId,
            topic: input.topic.trim(),
            notes: input.notes?.trim() || null,
        },
    });

    return result.success
        ? { success: true, message: 'Session booked. Check your sessions list for details.' }
        : { success: false, error: result.error };
}

export async function cancelMentorshipSessionAction(sessionId: string, reason?: string): Promise<ActionResult> {
    const result = await requestJson<MentorshipSessionView>(
        `/v1/mentorship/sessions/${encodeURIComponent(sessionId)}/cancel`,
        { method: 'POST', auth: true, body: { reason: reason?.trim() || null } },
    );

    return result.success
        ? { success: true, message: 'Session cancelled.' }
        : { success: false, error: result.error };
}

export async function completeMentorshipSessionAction(sessionId: string): Promise<ActionResult> {
    const result = await requestJson<MentorshipSessionView>(
        `/v1/mentorship/sessions/${encodeURIComponent(sessionId)}/complete`,
        { method: 'POST', auth: true },
    );

    return result.success
        ? { success: true, message: 'Session completed.' }
        : { success: false, error: result.error };
}
