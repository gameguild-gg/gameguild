import { Program, ProgramContent, ProgramContentType } from '@/lib/api/generated';
import { createServerClient, GeneratedApi, type ApiError } from '@game-guild/client';

export interface CourseService {
    getCourseBySlug(slug: string): Promise<CourseLookupResult>;
    getCourses(): Promise<Program[]>;
    getPublicCourseCatalog(): Promise<CourseCatalogResult>;
}

export type CourseLookupFailureReason = 'not-found' | 'unavailable';

export interface CourseLookupResult {
    success: boolean;
    data?: Program;
    error?: string;
    reason?: CourseLookupFailureReason;
}

export interface CourseCatalogResult {
    success: boolean;
    data: Program[];
    error?: string;
    source?: 'api';
}

export interface CourseLevelConfig {
    name: string;
    color: string;
    bgColor: string;
}

type PublicCourseDto = GeneratedApi.LearningCoursesProgram;
type PublicCourseContentDto = GeneratedApi.LearningCoursesProgramContent;

const DEFAULT_API_URL = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
const PUBLIC_COURSE_API_TIMEOUT_MS = Number(process.env.PUBLIC_COURSE_API_TIMEOUT_MS ?? 10_000);

function getApiUrl(): string {
    return DEFAULT_API_URL.replace(/\/$/, '');
}

function createPublicApiClient() {
    return createServerClient({
        baseUrl: getApiUrl(),
        timeout: Number.isFinite(PUBLIC_COURSE_API_TIMEOUT_MS) ? PUBLIC_COURSE_API_TIMEOUT_MS : 10_000,
    });
}

function createPublicCourseModules() {
    const client = createPublicApiClient();

    return {
        programs: new GeneratedApi.LearningCoursesProgramModule(client),
        content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
    };
}

function formatApiError(error: ApiError | undefined): string {
    if (!error) {
        return 'Unknown error';
    }

    const status = typeof error.status === 'number' ? error.status : 'unknown';
    const code = error.code ? ` ${error.code}` : '';
    const detail = 'detail' in error && typeof error.detail === 'string' ? error.detail : undefined;

    return `[${status}${code}] ${detail || error.message || 'Request failed'}`;
}

function mapContentType(type: string | number | null | undefined): ProgramContentType {
    if (typeof type === 'number') {
        if (type === 1) return ProgramContentType.Lesson;
        if (type === 6) return ProgramContentType.Assignment;
        return type as ProgramContentType;
    }

    switch (type) {
        case 'Assignment':
            return ProgramContentType.Assignment;
        case 'Questionnaire':
            return ProgramContentType.Questionnaire;
        case 'Discussion':
            return ProgramContentType.Discussion;
        case 'Code':
            return ProgramContentType.Code;
        case 'Reflection':
            return ProgramContentType.Reflection;
        case 'Survey':
            return ProgramContentType.Survey;
        case 'Project':
            return ProgramContentType.Project;
        case 'Lesson':
        default:
            return ProgramContentType.Lesson;
    }
}

function mapContentBody(body: unknown): string | null {
    if (body == null) {
        return null;
    }

    if (typeof body === 'string') {
        return body;
    }

    return JSON.stringify(body, null, 2);
}

function mapProgramContent(content: PublicCourseContentDto): ProgramContent {
    return {
        id: content.id,
        title: content.title,
        description: content.description ?? null,
        body: mapContentBody(content.body),
        parentId: content.parentId ?? null,
        type: mapContentType(content.type),
        estimatedMinutes: content.estimatedMinutes ?? null,
        isRequired: content.isRequired ?? false,
        children: content.children?.map(mapProgramContent) ?? [],
    };
}

function mapProgram(dto: PublicCourseDto, programContents?: ProgramContent[]): Program {
    return {
        id: dto.id,
        title: dto.title,
        slug: dto.slug ?? null,
        description: dto.description ?? null,
        metadata: dto.metadata ?? null,
        category: dto.category ?? 'General',
        difficulty: dto.difficulty ?? 'Beginner',
        estimatedHours: dto.estimatedHours ?? null,
        currentEnrollments: dto.currentEnrollments ?? 0,
        averageRating: dto.averageRating ?? 0,
        totalRatings: dto.totalRatings ?? 0,
        isEnrollmentOpen: dto.isEnrollmentOpen ?? false,
        thumbnail: dto.thumbnail ?? null,
        videoShowcaseUrl: dto.videoShowcaseUrl ?? null,
        visibility: dto.visibility ?? null,
        status: dto.status ?? null,
        maxEnrollments: dto.maxEnrollments ?? null,
        enrollmentDeadline: dto.enrollmentDeadline ?? null,
        skillsRequired: dto.skillsRequired ?? null,
        skillsProvided: dto.skillsProvided ?? null,
        programContents: programContents ?? null,
    };
}

async function fetchCourseBySlugResult(slug: string): Promise<CourseLookupResult> {
    try {
        const { programs, content } = createPublicCourseModules();
        const programResult = await programs.getCoursesSlug(encodeURIComponent(slug));

        if (!programResult.ok) {
            return {
                success: false,
                error: formatApiError(programResult.error),
                reason: programResult.error?.status === 404 ? 'not-found' : 'unavailable',
            };
        }

        const program = programResult.data;

        let programContents: ProgramContent[] | undefined;
        if (program.id) {
            const contentResult = await content.getCoursesByProgramIdContent(program.id);

            if (contentResult.ok && Array.isArray(contentResult.data)) {
                programContents = contentResult.data.map(mapProgramContent);
            }
        }

        if (!programContents) {
            programContents = undefined;
        }

        return { success: true, data: mapProgram(program, programContents) };
    } catch (error) {
        return {
            success: false,
            error: error instanceof Error ? error.message : 'Unknown error',
            reason: 'unavailable',
        };
    }
}

async function fetchPublicCourseCatalog(): Promise<CourseCatalogResult> {
    try {
        const { programs } = createPublicCourseModules();
        const result = await programs.getCoursesPublic();

        if (!result.ok || !Array.isArray(result.data)) {
            return {
                success: false,
                data: [],
                error: formatApiError(result.error),
            };
        }

        if (result.data.length === 0) {
            return {
                success: true,
                data: [],
                source: 'api',
            };
        }

        return {
            success: true,
            data: result.data.map((program) => mapProgram(program)),
            source: 'api',
        };
    } catch (error) {
        return {
            success: false,
            data: [],
            error: error instanceof Error ? error.message : 'Unknown error',
        };
    }
}

export const courseService: CourseService = {
    getCourseBySlug: fetchCourseBySlugResult,

    getPublicCourseCatalog: fetchPublicCourseCatalog,

    async getCourses() {
        const result = await fetchPublicCourseCatalog();

        if (!result.success) {
            console.warn(`[courses] Failed to fetch public catalog from ${getApiUrl()}: ${result.error ?? 'Unknown error'}`);
        }

        return result.data;
    },
};

// Named exports for direct import
export const getCourseBySlug = fetchCourseBySlugResult;
export const getPublicCourseCatalog = fetchPublicCourseCatalog;
export const getCourses = courseService.getCourses;

export function getCourseLevelConfig(difficulty: number | string | null | undefined): CourseLevelConfig {
    if (typeof difficulty === 'string') {
        const normalizedDifficulty = difficulty.trim().toLowerCase();

        if (normalizedDifficulty === 'intermediate') {
            return {
                name: 'Intermediate',
                color: 'text-blue-400',
                bgColor: 'bg-blue-500/10 border-blue-500',
            };
        }

        if (normalizedDifficulty === 'advanced') {
            return {
                name: 'Advanced',
                color: 'text-orange-400',
                bgColor: 'bg-orange-500/10 border-orange-500',
            };
        }

        if (normalizedDifficulty === 'expert') {
            return {
                name: 'Expert',
                color: 'text-red-400',
                bgColor: 'bg-red-500/10 border-red-500',
            };
        }
    }

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

    if (typeof difficulty === 'number') {
        return configs[difficulty] || configs[0]!;
    }

    return configs[0]!;
}

export function getCourseCategoryName(category: number | string | null | undefined): string {
    if (typeof category === 'string') {
        const mappedCategories: Record<string, string> = {
            general: 'General',
            programming: 'Programming',
            datascience: 'Data Science',
            webdevelopment: 'Web Development',
            mobiledevelopment: 'Mobile Development',
            gamedevelopment: 'Game Development',
            ai: 'AI',
            cybersecurity: 'Cybersecurity',
            devops: 'DevOps',
            database: 'Database',
            business: 'Business',
            design: 'Design',
            marketing: 'Marketing',
            projectmanagement: 'Project Management',
            personaldevelopment: 'Personal Development',
            creativearts: 'Creative Arts',
            science: 'Science',
            language: 'Language',
            other: 'Other',
        };

        const normalizedCategory = category.replace(/[^a-zA-Z]/g, '').toLowerCase();
        return mappedCategories[normalizedCategory] || category;
    }

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

    if (typeof category === 'number') {
        return categories[category] || 'Other';
    }

    return 'Other';
}
