import { getAllProducts, getAllPrograms, getProductBySlug, getProgramBySlug } from '@/data/courses/mock-data';
import { Product, Program, ProgramContent } from '@/lib/api/generated';

export interface ProgramService {
    getProgramBySlug(slug: string): Promise<Program | null>;
    getProductBySlug(slug: string): Promise<Product | null>;
    getAllPrograms(): Promise<Program[]>;
    getAllProducts(): Promise<Product[]>;
    getProgramContent(programId: string): Promise<ProgramContent[]>;
}

export interface ProgramLevelConfig {
    name: string;
    color: string;
    bgColor: string;
}

export const programService: ProgramService = {
    async getProgramBySlug(slug: string) {
        return getProgramBySlug(slug);
    },

    async getProductBySlug(slug: string) {
        return getProductBySlug(slug);
    },

    async getAllPrograms() {
        return getAllPrograms();
    },

    async getAllProducts() {
        return getAllProducts();
    },

    async getProgramContent(programId: string) {
        const program = await this.getProgramBySlug(programId);
        return program?.programContents || [];
    },
};

// Named exports for direct import
export const getProgramBySlug = programService.getProgramBySlug;
export const getProductBySlug = programService.getProductBySlug;
export const getAllPrograms = programService.getAllPrograms;
export const getAllProducts = programService.getAllProducts;
export const getProgramContent = programService.getProgramContent;

export function getProgramLevelConfig(difficulty: number): ProgramLevelConfig {
    const configs: Record<number, ProgramLevelConfig> = {
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

export function getProgramCategoryName(category: number): string {
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