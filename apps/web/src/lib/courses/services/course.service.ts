import { getAllPrograms, getProgramBySlug } from '@/data/courses/mock-data';
import { Program } from '@/lib/api/generated';

export interface CourseService {
    getCourseBySlug(slug: string): Promise<{ success: boolean; data?: Program; error?: string }>;
    getCourses(): Promise<Program[]>;
}

export interface CourseLevelConfig {
    name: string;
    color: string;
    bgColor: string;
}

export const courseService: CourseService = {
    async getCourseBySlug(slug: string) {
        try {
            const program = await getProgramBySlug(slug);
            if (!program) {
                return { success: false, error: 'Course not found' };
            }
            return { success: true, data: program };
        } catch (error) {
            return { success: false, error: error instanceof Error ? error.message : 'Unknown error' };
        }
    },

    async getCourses() {
        return getAllPrograms();
    },
};

// Named exports for direct import
export const getCourseBySlug = courseService.getCourseBySlug;
export const getCourses = courseService.getCourses;

export function getCourseLevelConfig(difficulty: number): CourseLevelConfig {
    const configs: Record<number, CourseLevelConfig> = {
        0: {
            name: 'Beginner',
            color: 'text-green-400',
            bgColor: 'bg-green-500/10 border-green-500',
        },
        1: {
            name: 'Intermediate',
            color: 'text-blue-400',
            bgColor: 'bg-blue-500/10 border-blue-500',
        },
        2: {
            name: 'Advanced',
            color: 'text-orange-400',
            bgColor: 'bg-orange-500/10 border-orange-500',
        },
        3: {
            name: 'Expert',
            color: 'text-red-400',
            bgColor: 'bg-red-500/10 border-red-500',
        },
    };

    return configs[difficulty] || configs[0]!;
}

export function getCourseCategoryName(category: number): string {
    const categories: Record<number, string> = {
        0: 'Programming',
        1: 'Art & Design',
        2: 'Game Design',
        3: 'Audio',
        4: 'Business',
        5: 'Marketing',
        6: 'Production',
        7: 'Quality Assurance',
        8: 'Writing',
        9: 'Animation',
        10: 'VFX',
        11: 'UI/UX',
        12: 'Mobile Development',
        13: 'Web Development',
        14: 'Data Science',
        15: 'AI/ML',
        16: 'DevOps',
        17: 'Other',
    };

    return categories[category] || 'Other';
}
