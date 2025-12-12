'use server';

import { getApolloClient } from '@/lib/graphql/client';
import type { GetMyProductsWithProgramsQuery } from '@/lib/graphql/generated/graphql';
import { CREATE_PRODUCT } from '@/lib/graphql/mutations/products';
import { GET_MY_PRODUCTS_WITH_PROGRAMS } from '@/lib/graphql/queries/products';
import type { Course } from '@/lib/types';
import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';

/**
 * Server action to get a program/course by slug using GraphQL
 */
export async function getCourseBySlug(slug: string): Promise<Course | null> {
    try {
        const client = getApolloClient();
        const { data } = await client.query({
            query: GET_MY_PRODUCTS_WITH_PROGRAMS,
            variables: { skip: 0, take: 100 }, // Get more to search through
        });

        // Find the product with matching program slug
        const product = data.myProducts.find((product: GetMyProductsWithProgramsQuery['myProducts'][0]) =>
            product.productPrograms?.some((pp: any) => pp.program?.slug === slug)
        ); if (product && product.productPrograms?.[0]?.program) {
            // Transform the product's first program to a course
            const program = product.productPrograms[0].program;
            const course: Course = {
                id: product.id,
                title: product.title || product.name,
                slug: program.slug,
                description: program.description || product.description || '',
                shortDescription: product.shortDescription || '',
                coverUrl: program.thumbnail || product.imageUrl || undefined,
                thumbnailUrl: program.thumbnail || product.imageUrl || undefined,
                trailerUrl: program.videoShowcaseUrl || undefined,
                level: transformDifficultyToLevel(program.difficulty),
                status: transformStatusToStatus(product.status),
                category: transformCategoryToString(program.category),
                tags: [],
                deliveryMethod: 'self-paced' as const,
                duration: program.estimatedHours ? Math.round(program.estimatedHours * 60) : 0,
                pricing: {
                    type: product.currentPricing?.basePrice && product.currentPricing.basePrice > 0 ? 'paid' : 'free',
                    currency: product.currentPricing?.currency || 'USD',
                    price: product.currentPricing?.basePrice || undefined,
                },
                certificateType: 'completion' as const,
                modules: [],
                learningObjectives: [],
                enrollments: [],
                totalStudents: 0,
                averageRating: 0,
                totalReviews: 0,
                team: [],
                teamInvites: [],
                instructor: product.creator?.name || 'Game Guild',
                createdAt: product.createdAt ? new Date(product.createdAt).getTime() : Date.now(),
                updatedAt: product.updatedAt ? new Date(product.updatedAt).getTime() : Date.now(),
            };
            return course;
        }

        return null;
    } catch (error) {
        console.error('Error fetching course by slug:', error);
        return null;
    }
}

// Helper transformation functions
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

/**
 * Server action to create a new course/program using GraphQL
 */
export async function createCourse(formData: FormData) {
    try {
        const title = formData.get('title') as string;
        const description = formData.get('description') as string;
        const category = formData.get('category') as string;
        const difficulty = formData.get('difficulty') as string;

        if (!title || !description) {
            throw new Error('Title and description are required');
        }

        // Generate slug
        const slug = title.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');

        const client = getApolloClient();

        // Create a product using GraphQL
        const { data } = await client.mutate({
            mutation: CREATE_PRODUCT,
            variables: {
                input: {
                    name: title,
                    shortDescription: description,
                    type: 'PROGRAM', // Use PROGRAM as the ProductType
                    isBundle: false,
                }
            },
        }); if (!data?.createProduct) {
            throw new Error('Failed to create product');
        }

        const product = data.createProduct;
        console.log('Created product with GraphQL:', product);

        // Revalidate the courses pages to show the new course
        revalidatePath('/dashboard/courses');
        revalidatePath('/en/dashboard/courses');
        revalidatePath(`/dashboard/courses/${product.slug || slug}`);

        // Redirect to the new course detail page
        redirect(`/dashboard/courses/${product.slug || slug}`);

    } catch (error) {
        console.error('Error creating course with GraphQL:', error);
        throw error;
    }
}

/**
 * Server action to update a course/program
 */
export async function updateCourse(courseId: string, formData: FormData) {
    try {
        const title = formData.get('title') as string;
        const description = formData.get('description') as string;
        const category = formData.get('category') as string;
        const difficulty = formData.get('difficulty') as string;

        if (!courseId || !title || !description) {
            throw new Error('Course ID, title and description are required');
        }

        // TODO: Implement GraphQL mutation for updating a program
        console.log('Updating course:', { courseId, title, description, category, difficulty });

        // Revalidate the course page to show the updates
        revalidatePath(`/dashboard/courses/${courseId}`);

        return { success: true };
    } catch (error) {
        console.error('Error updating course:', error);
        throw error;
    }
}

/**
 * Server action to delete a course/program
 */
export async function deleteCourse(courseId: string) {
    try {
        if (!courseId) {
            throw new Error('Course ID is required');
        }

        // TODO: Implement GraphQL mutation for deleting a program
        console.log('Deleting course:', courseId);

        // Revalidate the courses page
        revalidatePath('/dashboard/courses');

        // Redirect to courses list after deletion
        redirect('/dashboard/courses');
    } catch (error) {
        console.error('Error deleting course:', error);
        throw error;
    }
}