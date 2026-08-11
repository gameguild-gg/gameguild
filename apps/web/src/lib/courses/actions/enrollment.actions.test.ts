import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
    auth: vi.fn(),
    getToken: vi.fn(),
    createServerClient: vi.fn(),
    getCoursesSlug: vi.fn(),
    getCoursesProducts: vi.fn(),
    postCoursesSelfEnroll: vi.fn(),
    getProductsByProductId: vi.fn(),
    request: vi.fn(),
}));

vi.mock('@/auth', () => ({
    auth: mocks.auth,
    getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
    createServerClient: mocks.createServerClient,
    GeneratedApi: {
        LearningCoursesProgramModule: class {
            getCoursesSlug = mocks.getCoursesSlug;
            getCoursesProducts = mocks.getCoursesProducts;
            postCoursesSelfEnroll = mocks.postCoursesSelfEnroll;
        },
        CommerceProductsModule: class {
            getProductsByProductId = mocks.getProductsByProductId;
        },
    },
}));

import { completeCourseCheckout, enrollInFreeCourse, getProductsContainingCourse } from './enrollment.actions';

describe('enrollInFreeCourse', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
        mocks.getToken.mockResolvedValue('access-token');
        mocks.createServerClient.mockReturnValue({ request: mocks.request });
        mocks.getCoursesSlug.mockResolvedValue({
            ok: true,
            data: {
                id: 'course-1',
                isEnrollmentOpen: true,
            },
        });
        mocks.getCoursesProducts.mockResolvedValue({
            ok: true,
            data: ['product-1'],
        });
        mocks.postCoursesSelfEnroll.mockResolvedValue({
            ok: true,
            data: {
                id: 'enrollment-1',
            },
        });
        mocks.getProductsByProductId.mockResolvedValue({
            ok: true,
            data: {
                id: 'product-1',
                name: 'Course Access',
                type: 'course',
                description: 'Unlocks the course.',
                pricing: [
                    {
                        isDefault: true,
                        currentPrice: 49,
                        currency: 'USD',
                    },
                ],
            },
        });
    });

    it('uses the generated client for self-enrollment instead of a raw fetch call', async () => {
        const fetchSpy = vi.spyOn(global, 'fetch');

        const result = await enrollInFreeCourse('intro-to-game-dev');

        expect(result).toEqual({
            success: true,
            message: 'Enrollment complete. You can continue in the learning app now.',
            learningUrl: '/learn/courses/intro-to-game-dev/content',
        });
        expect(mocks.getCoursesSlug).toHaveBeenCalledWith('intro-to-game-dev');
        expect(mocks.postCoursesSelfEnroll).toHaveBeenCalledWith('course-1');
        expect(fetchSpy).not.toHaveBeenCalled();

        fetchSpy.mockRestore();
    });

    it('returns the generated client error when self-enrollment fails', async () => {
        mocks.postCoursesSelfEnroll.mockResolvedValue({
            ok: false,
            error: {
                status: 403,
                message: 'Forbidden',
                detail: 'Learner access denied',
            },
        });

        const result = await enrollInFreeCourse('intro-to-game-dev');

        expect(result).toEqual({
            success: false,
            message: '[403] Learner access denied',
        });
    });
});

describe('getProductsContainingCourse', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mocks.createServerClient.mockReturnValue({ request: mocks.request });
        mocks.getCoursesSlug.mockResolvedValue({
            ok: true,
            data: {
                id: 'course-1',
                isEnrollmentOpen: true,
            },
        });
    });

    it('uses generated clients to resolve and map storefront products for a course', async () => {
        mocks.getCoursesProducts.mockResolvedValue({
            ok: true,
            data: ['product-1', 'product-2', 'product-1'],
        });
        mocks.getProductsByProductId
            .mockResolvedValueOnce({
                ok: true,
                data: {
                    id: 'product-1',
                    name: 'Course Access',
                    type: 'course',
                    description: 'Unlocks the course.',
                    pricing: [
                        {
                            isDefault: true,
                            currentPrice: 49,
                            currency: 'USD',
                        },
                    ],
                },
            })
            .mockResolvedValueOnce({
                ok: true,
                data: {
                    id: 'product-2',
                    name: 'Bundle Access',
                    type: 'bundle',
                    shortDescription: 'Includes the course in a bundle.',
                    pricing: [
                        {
                            basePrice: 99,
                            currency: 'EUR',
                        },
                    ],
                },
            });

        const result = await getProductsContainingCourse('intro-to-game-dev');

        expect(mocks.getCoursesSlug).toHaveBeenCalledWith('intro-to-game-dev');
        expect(mocks.getCoursesProducts).toHaveBeenCalledWith('course-1');
        expect(mocks.getProductsByProductId).toHaveBeenCalledTimes(2);
        expect(mocks.getProductsByProductId).toHaveBeenNthCalledWith(1, 'product-1', { includePricing: true });
        expect(mocks.getProductsByProductId).toHaveBeenNthCalledWith(2, 'product-2', { includePricing: true });
        expect(result).toEqual([
            {
                id: 'product-1',
                name: 'Course Access',
                type: 'course',
                price: 49,
                currency: 'USD',
                description: 'Unlocks the course.',
            },
            {
                id: 'product-2',
                name: 'Bundle Access',
                type: 'bundle',
                price: 99,
                currency: 'EUR',
                description: 'Includes the course in a bundle.',
            },
        ]);
    });

    it('returns an empty list when the course-product lookup fails', async () => {
        mocks.getCoursesProducts.mockResolvedValue({
            ok: false,
            error: {
                status: 403,
                message: 'Forbidden',
            },
        });

        const result = await getProductsContainingCourse('intro-to-game-dev');

        expect(result).toEqual([]);
        expect(mocks.getProductsByProductId).not.toHaveBeenCalled();
    });
});

describe('completeCourseCheckout', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
        mocks.getToken.mockResolvedValue('access-token');
        mocks.createServerClient.mockReturnValue({ request: mocks.request });
        mocks.getCoursesSlug.mockResolvedValue({
            ok: true,
            data: {
                id: 'course-1',
                isEnrollmentOpen: true,
            },
        });
    });

    it('completes course checkout through the authenticated API client', async () => {
        const fetchSpy = vi.spyOn(global, 'fetch');
        mocks.request.mockResolvedValue({
            ok: true,
            data: {
                learningUrl: '/courses/intro-to-game-dev/content',
                amount: 49,
                currency: 'USD',
                entitlementId: 'entitlement-1',
                alreadyHadAccess: false,
            },
        });

        const result = await completeCourseCheckout('intro-to-game-dev', 'product-1');

        expect(result).toEqual({
            success: true,
            message: 'Checkout complete. Your course access is active.',
            learningUrl: '/learn/courses/intro-to-game-dev/content',
            amount: 49,
            currency: 'USD',
            entitlementId: 'entitlement-1',
            alreadyHadAccess: false,
        });
        expect(mocks.request).toHaveBeenCalledWith({
            method: 'POST',
            path: '/v1/courses/course-1/checkout/complete',
            body: {
                productId: 'product-1',
                paymentProviderReference: 'gameguild-checkout-course-1-product-1',
                paymentMethod: 'test_card',
            },
        });
        expect(fetchSpy).not.toHaveBeenCalled();

        fetchSpy.mockRestore();
    });

    it('returns checkout API errors without enrolling locally', async () => {
        mocks.request.mockResolvedValue({
            ok: false,
            error: {
                status: 400,
                detail: 'The selected product does not grant access to this course.',
            },
        });

        const result = await completeCourseCheckout('intro-to-game-dev', 'wrong-product');

        expect(result).toEqual({
            success: false,
            message: '[400] The selected product does not grant access to this course.',
        });
        expect(mocks.postCoursesSelfEnroll).not.toHaveBeenCalled();
    });
});
