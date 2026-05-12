'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi, type ApiError } from '@game-guild/client';

export type EnrollmentStatusCode = 0 | 1 | 2;

export interface EnrollmentStatus {
    status: EnrollmentStatusCode;
    isEnrolled?: boolean;
    progress?: number;
    enrollmentDate?: string;
    completionDate?: string;
    courseId?: string;
    error?: string;
}

export interface Product {
    id: string;
    name: string;
    type?: string;
    price: number;
    currency: string;
    description?: string;
    courseCount?: number;
    courses?: string[];
}

export interface PaymentIntentResult {
    success?: boolean;
    clientSecret: string;
    paymentIntentId: string;
    paymentUrl?: string;
    message?: string;
}

export interface EnrollmentResult {
    success: boolean;
    message: string;
    enrollmentId?: string;
}

type PublicCourseDto = GeneratedApi.LearningCoursesProgram;

const DEFAULT_API_URL = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

function getApiUrl(): string {
    return DEFAULT_API_URL.replace(/\/$/, '');
}

function formatApiError(error: ApiError | undefined): string {
    if (!error) {
        return 'Unknown error';
    }

    const status = typeof error.status === 'number' ? error.status : 'unknown';
    const detail = 'detail' in error && typeof error.detail === 'string' ? error.detail : undefined;

    return `[${status}] ${detail || error.message || 'Request failed'}`;
}

function createPublicProgramsModule() {
    const client = createServerClient({
        baseUrl: getApiUrl(),
    });

    return new GeneratedApi.LearningCoursesProgramModule(client);
}

function createAuthenticatedProgramsModule(accessToken: string) {
    const client = createServerClient({
        baseUrl: getApiUrl(),
        auth: { getAccessToken: async () => accessToken },
    });

    return new GeneratedApi.LearningCoursesProgramModule(client);
}

async function resolvePublishedCourseBySlug(courseSlug: string): Promise<{ course?: PublicCourseDto; error?: string }> {
    try {
        const programs = createPublicProgramsModule();
        const result = await programs.getCoursesSlug(encodeURIComponent(courseSlug));

        if (!result.ok) {
            return { error: formatApiError(result.error) };
        }

        return { course: result.data };
    } catch (error) {
        return { error: error instanceof Error ? error.message : 'Unknown error' };
    }
}

async function getAuthenticatedAccessToken(): Promise<string | null> {
    const [accessToken, session] = await Promise.all([getToken(), auth()]);

    if (!accessToken || !session?.user?.id) {
        return null;
    }

    return accessToken;
}

async function readProblemDetailMessage(response: Response): Promise<string> {
    try {
        const payload = (await response.json()) as { detail?: string; title?: string; message?: string };
        return payload.detail || payload.message || payload.title || `${response.status} ${response.statusText}`;
    } catch {
        return `${response.status} ${response.statusText}`;
    }
}

export async function getCourseEnrollmentStatus(courseSlug: string): Promise<EnrollmentStatus> {
    try {
        const accessToken = await getAuthenticatedAccessToken();
        if (!accessToken) {
            return {
                status: 0,
                isEnrolled: false,
            };
        }

        const { course, error: courseError } = await resolvePublishedCourseBySlug(courseSlug);
        if (!course?.id) {
            return {
                status: 0,
                isEnrolled: false,
                error: courseError || 'Course not found',
            };
        }

        const programs = createAuthenticatedProgramsModule(accessToken);
        const progressResult = await programs.getCoursesMeProgress(course.id);

        if (progressResult.ok) {
            const progress = progressResult.data.completionPercentage ?? 0;

            return {
                status: progress >= 100 ? 2 : 1,
                isEnrolled: true,
                progress,
                completionDate: progressResult.data.completedAt ?? undefined,
                courseId: course.id,
            };
        }

        if (progressResult.error?.status === 401 || progressResult.error?.status === 403 || progressResult.error?.status === 404) {
            return {
                status: 0,
                isEnrolled: false,
                courseId: course.id,
            };
        }

        return {
            status: 0,
            isEnrolled: false,
            courseId: course.id,
            error: formatApiError(progressResult.error),
        };
    } catch (error) {
        return {
            status: 0,
            isEnrolled: false,
            error: error instanceof Error ? error.message : 'Unknown error',
        };
    }
}

export async function getProductsContainingCourse(courseSlug: string): Promise<Product[]> {
    void courseSlug;
    return [];
}

export async function enrollInFreeCourse(courseSlug: string): Promise<EnrollmentResult> {
    try {
        const accessToken = await getAuthenticatedAccessToken();
        if (!accessToken) {
            return {
                success: false,
                message: 'You must be signed in to enroll in this course.',
            };
        }

        const { course, error: courseError } = await resolvePublishedCourseBySlug(courseSlug);
        if (!course?.id) {
            return {
                success: false,
                message: courseError || 'Course not found.',
            };
        }

        if (!course.isEnrollmentOpen) {
            return {
                success: false,
                message: 'This course is not currently open for self-enrollment.',
            };
        }

        const response = await fetch(`${getApiUrl()}/v1/courses/${course.id}:self-enroll`, {
            method: 'POST',
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
            cache: 'no-store',
        });

        if (!response.ok) {
            return {
                success: false,
                message: await readProblemDetailMessage(response),
            };
        }

        const enrollment = (await response.json()) as { id?: string };

        return {
            success: true,
            message: 'Enrollment complete. You can continue in the learning app now.',
            enrollmentId: enrollment.id,
        };
    } catch (error) {
        return {
            success: false,
            message: error instanceof Error ? error.message : 'Failed to enroll in course.',
        };
    }
}

export async function createPaymentIntent(productId: string): Promise<PaymentIntentResult> {
    void productId;

    throw new Error('Paid storefront checkout is not wired yet. Only direct course self-enrollment is currently available.');
}
