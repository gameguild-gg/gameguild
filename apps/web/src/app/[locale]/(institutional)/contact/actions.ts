'use server';

type ContactLeadInput = {
    name: string;
    email: string;
    company?: string;
    topic: string;
    message: string;
    pagePath?: string;
    locale?: string;
};

type ActionResult = { success: true; message: string } | { success: false; error: string };

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

export async function submitContactLeadAction(input: ContactLeadInput): Promise<ActionResult> {
    if (!input.name?.trim()) {
        return { success: false, error: 'Name is required.' };
    }

    if (!input.email?.trim()) {
        return { success: false, error: 'Email is required.' };
    }

    if (!input.topic?.trim()) {
        return { success: false, error: 'Topic is required.' };
    }

    if (!input.message?.trim() || input.message.trim().length < 10) {
        return { success: false, error: 'Message must be at least 10 characters.' };
    }

    try {
        const response = await fetch(`${getApiUrl()}/v1/marketing/leads`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                source: 'contact',
                name: input.name.trim(),
                email: input.email.trim(),
                company: input.company?.trim() || undefined,
                topic: input.topic.trim(),
                message: input.message.trim(),
                pagePath: input.pagePath,
                locale: input.locale,
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
                error: getFirstProblemDetailError(payload) || 'Failed to submit your message. Please try again.',
            };
        }

        return {
            success: true,
            message: 'Your message has been sent. We will get back to you soon.',
        };
    } catch (error) {
        return {
            success: false,
            error: error instanceof Error ? error.message : 'Failed to submit your message. Please try again.',
        };
    }
}