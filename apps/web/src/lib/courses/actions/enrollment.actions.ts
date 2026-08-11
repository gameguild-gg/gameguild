'use server';

import { auth, getToken } from '@/auth';
import { createServerClient, GeneratedApi, type ApiError } from '@game-guild/client';
import { getLearnerCourseContentHref } from '@/lib/learner/paths';

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

export interface EnrollmentResult {
    success: boolean;
    message: string;
    enrollmentId?: string;
    learningUrl?: string;
}

export interface CourseCheckoutResult {
    success: boolean;
    message: string;
    learningUrl?: string;
    amount?: number;
    currency?: string;
    entitlementId?: string;
    alreadyHadAccess?: boolean;
}

type PublicCourseDto = GeneratedApi.LearningCoursesProgram;
type PublicProductDto = GeneratedApi.CommerceProductsProduct;

const DEFAULT_API_URL = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';

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
    const client = createAuthenticatedClient(accessToken);

    return new GeneratedApi.LearningCoursesProgramModule(client);
}

function createAuthenticatedClient(accessToken: string) {
    return createServerClient({
        baseUrl: getApiUrl(),
        auth: { getAccessToken: async () => accessToken },
    });
}

function createPublicProductsModule() {
    const client = createServerClient({
        baseUrl: getApiUrl(),
    });

    return new GeneratedApi.CommerceProductsModule(client);
}

function mapStorefrontProduct(product: PublicProductDto): Product | null {
    if (!product.id || !product.name) {
        return null;
    }

    const pricing = product.pricing ?? [];
    const primaryPricing =
        pricing.find((entry) => entry.isDefault) ??
        pricing.find((entry) => typeof entry.currentPrice === 'number') ??
        pricing[0];

    const price =
        primaryPricing?.currentPrice ??
        primaryPricing?.salePrice ??
        primaryPricing?.basePrice ??
        0;

    return {
        id: product.id,
        name: product.name,
        type: product.type,
        price,
        currency: primaryPricing?.currency ?? 'USD',
        description: product.description ?? product.shortDescription ?? undefined,
    };
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
    try {
        const { course } = await resolvePublishedCourseBySlug(courseSlug);
        if (!course?.id) {
            return [];
        }

        const programs = createPublicProgramsModule();
        const products = createPublicProductsModule();
        const courseProductsResult = await programs.getCoursesProducts(course.id);

        if (!courseProductsResult.ok) {
            return [];
        }

        const productIds = [...new Set(courseProductsResult.data.filter((productId): productId is string => typeof productId === 'string' && productId.length > 0))];
        if (productIds.length === 0) {
            return [];
        }

        const productResults = await Promise.all(
            productIds.map(async (productId) => {
                const result = await products.getProductsByProductId(productId, { includePricing: true });
                return result.ok ? mapStorefrontProduct(result.data) : null;
            })
        );

        return productResults.filter((product): product is Product => product !== null);
    } catch {
        return [];
    }
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

        const programs = createAuthenticatedProgramsModule(accessToken);
        const enrollmentResult = await programs.postCoursesSelfEnroll(course.id);

        if (!enrollmentResult.ok) {
            return {
                success: false,
                message: formatApiError(enrollmentResult.error),
            };
        }

        return {
            success: true,
            message: 'Enrollment complete. You can continue in the learning app now.',
            learningUrl: getLearnerCourseContentHref(courseSlug),
        };
    } catch (error) {
        return {
            success: false,
            message: error instanceof Error ? error.message : 'Failed to enroll in course.',
        };
    }
}

export async function completeCourseCheckout(courseSlug: string, productId: string): Promise<CourseCheckoutResult> {
    try {
        const accessToken = await getAuthenticatedAccessToken();
        if (!accessToken) {
            return {
                success: false,
                message: 'You must be signed in to complete checkout.',
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
                message: 'This course is not currently open for checkout.',
            };
        }

        const client = createAuthenticatedClient(accessToken);
        const result = await client.request<{
            learningUrl?: string;
            amount?: number;
            currency?: string;
            entitlementId?: string;
            alreadyHadAccess?: boolean;
        }>({
            method: 'POST',
            path: `/v1/courses/${course.id}/checkout/complete`,
            body: {
                productId,
                paymentProviderReference: `gameguild-checkout-${course.id}-${productId}`,
                paymentMethod: 'test_card',
            },
        });

        if (!result.ok) {
            return {
                success: false,
                message: formatApiError(result.error),
            };
        }

        return {
            success: true,
            message: 'Checkout complete. Your course access is active.',
            learningUrl: getLearnerCourseContentHref(courseSlug),
            amount: result.data.amount,
            currency: result.data.currency,
            entitlementId: result.data.entitlementId,
            alreadyHadAccess: result.data.alreadyHadAccess,
        };
    } catch (error) {
        return {
            success: false,
            message: error instanceof Error ? error.message : 'Failed to complete checkout.',
        };
    }
}
