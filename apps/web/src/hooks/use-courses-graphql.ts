import {
    GET_ALL_PRODUCTS_WITH_PROGRAMS,
    GET_MY_PRODUCTS_WITH_PROGRAMS,
    GET_PUBLISHED_PRODUCTS_WITH_PROGRAMS,
    SEARCH_PRODUCTS_WITH_PROGRAMS
} from '@/lib/graphql/queries/products';
import type { Course } from '@/lib/types';
import { useQuery } from '@apollo/client';

// Transform GraphQL Product data to Course format for UI compatibility
function transformProductToCourse(product: any): Course {
    // Get the main program from productPrograms (assuming first one is primary)
    const primaryProgram = product.productPrograms?.[0]?.program;

    return {
        id: product.id || '',
        title: product.title || product.name || '',
        slug: product.slug || '',
        description: primaryProgram?.description || product.description || '',
        shortDescription: product.shortDescription || '',
        coverUrl: primaryProgram?.thumbnail || product.imageUrl || undefined,
        thumbnailUrl: primaryProgram?.thumbnail || product.imageUrl || undefined,
        trailerUrl: undefined,
        level: transformDifficultyToLevel(primaryProgram?.difficulty || primaryProgram?.level),
        status: transformStatusToStatus(product.status || primaryProgram?.status),
        category: transformCategoryToString(primaryProgram?.category),
        tags: primaryProgram?.tags || [],
        deliveryMethod: 'self-paced' as const,
        duration: primaryProgram?.estimatedHours ? Math.round(primaryProgram.estimatedHours * 60) : 0,
        pricing: {
            type: product.currentPricing?.basePrice > 0 ? 'paid' : 'free',
            currency: product.currentPricing?.currency || 'USD',
            price: product.currentPricing?.basePrice || undefined,
        },
        certificateType: 'completion' as const,
        modules: [],
        learningObjectives: primaryProgram?.learningObjectives || [],
        enrollments: [],
        totalStudents: 0, // This would need to come from enrollment data
        averageRating: 0, // This would need to come from review data
        totalReviews: 0, // This would need to come from review data
        team: [],
        teamInvites: [],
        instructor: product.creator?.name || 'Game Guild',
        createdAt: product.createdAt ? new Date(product.createdAt).getTime() : Date.now(),
        updatedAt: product.updatedAt ? new Date(product.updatedAt).getTime() : Date.now(),
    };
}

function transformDifficultyToLevel(difficulty: any): Course['level'] {
    if (typeof difficulty === 'string') {
        switch (difficulty.toLowerCase()) {
            case 'beginner': return 'beginner';
            case 'intermediate': return 'intermediate';
            case 'advanced': return 'advanced';
            default: return 'beginner';
        }
    }

    switch (difficulty) {
        case 0: return 'beginner';
        case 1: return 'intermediate';
        case 2: return 'advanced';
        default: return 'beginner';
    }
}

function transformStatusToStatus(status: any): Course['status'] {
    if (typeof status === 'string') {
        switch (status.toLowerCase()) {
            case 'published': return 'published';
            case 'draft': return 'draft';
            case 'archived': return 'archived';
            default: return 'draft';
        }
    }

    switch (status) {
        case 1: return 'published';
        case 0: return 'draft';
        case 2: return 'archived';
        default: return 'draft';
    }
}

function transformCategoryToString(category: any): string {
    if (typeof category === 'string') {
        return category;
    }

    switch (category) {
        case 0: return 'Game Development';
        case 1: return 'Business';
        case 2: return 'Design';
        case 3: return 'Technology';
        default: return 'General';
    }
}

// Hook to fetch all published products as courses
export function usePublishedCourses() {
    const { data, loading, error, refetch } = useQuery(GET_PUBLISHED_PRODUCTS_WITH_PROGRAMS, {
        errorPolicy: 'all',
        notifyOnNetworkStatusChange: true,
    });

    const courses: Course[] = data?.publishedProducts?.map(transformProductToCourse) || [];

    return {
        courses,
        loading,
        error,
        refetch,
    };
}

// Hook to fetch all products as courses (including drafts)
export function useAllCourses() {
    const { data, loading, error, refetch } = useQuery(GET_ALL_PRODUCTS_WITH_PROGRAMS, {
        errorPolicy: 'all',
        notifyOnNetworkStatusChange: true,
    });

    const courses: Course[] = data?.products?.map(transformProductToCourse) || [];

    return {
        courses,
        loading,
        error,
        refetch,
    };
}

// Hook to fetch user's own products as courses (for dashboard)
export function useMyProducts() {
    const { data, loading, error, refetch } = useQuery(GET_MY_PRODUCTS_WITH_PROGRAMS, {
        variables: { skip: 0, take: 50 },
        errorPolicy: 'all',
        notifyOnNetworkStatusChange: true,
    });

    const courses: Course[] = data?.myProducts?.map(transformProductToCourse) || [];

    return {
        courses,
        loading,
        error,
        refetch,
    };
}

// Hook to search products as courses
export function useSearchCourses(searchTerm: string) {
    const { data, loading, error, refetch } = useQuery(SEARCH_PRODUCTS_WITH_PROGRAMS, {
        variables: { searchTerm },
        skip: !searchTerm,
        errorPolicy: 'all',
        notifyOnNetworkStatusChange: true,
    });

    const courses: Course[] = data?.searchProducts?.map(transformProductToCourse) || [];

    return {
        courses,
        loading,
        error,
        refetch,
    };
}