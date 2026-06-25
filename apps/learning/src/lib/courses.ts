import {
    createServerClient,
    GeneratedApi,
    type LearningCoursesContentProgress,
    type LearningCoursesProgramContent,
    type LearningCoursesProgressStatus,
} from '@game-guild/client';

export interface LearningCourseSummary {
    id: string;
    title: string;
    slug: string;
    description: string;
    thumbnail: string | null;
    category: string;
    difficulty: string;
    estimatedHours: number | null;
    currentEnrollments: number;
    averageRating: number;
    isEnrollmentOpen: boolean;
}

export interface CourseAttendanceItem {
    id: string;
    title: string;
    type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
    status: 'locked' | 'available' | 'in-progress' | 'completed';
    duration?: number;
    description?: string;
    order: number;
    isRequired: boolean;
    content?: unknown;
}

export interface CourseAttendanceModule {
    id: string;
    title: string;
    description: string;
    order: number;
    items: CourseAttendanceItem[];
    progress: number;
}

export interface CourseAttendanceData {
    id: string;
    title: string;
    slug: string;
    description: string;
    thumbnail: string | null;
    modules: CourseAttendanceModule[];
    overallProgress: number;
    totalItems: number;
    completedItems: number;
    currentItem?: CourseAttendanceItem;
    remainingMinutes: number;
}

interface CourseAttendanceModuleSource {
    id: string;
    title: string;
    description: string;
    order: number;
    items: Array<LearningCoursesProgramContent & { id: string }>;
}

type ProgressItemStatus = 'not-started' | 'in-progress' | 'completed';

async function getOptionalToken(): Promise<string | null> {
    if (!process.env.AUTH_SECRET) {
        return null;
    }

    const { getToken } = await import('@/auth');
    return getToken();
}

function getApiClient(getAccessToken?: () => Promise<string | null>) {
    const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    return createServerClient({
        baseUrl: apiUrl,
        auth: getAccessToken ? { getAccessToken } : undefined,
    });
}

function createCourseModules(getAccessToken?: () => Promise<string | null>) {
    const client = getApiClient(getAccessToken);

    return {
        programs: new GeneratedApi.LearningCoursesProgramModule(client),
        content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
    };
}

function flattenContent(items: LearningCoursesProgramContent[]): LearningCoursesProgramContent[] {
    const flattened: LearningCoursesProgramContent[] = [];

    const visit = (item: LearningCoursesProgramContent) => {
        flattened.push(item);

        for (const child of item.children ?? []) {
            visit(child);
        }
    };

    for (const item of items) {
        visit(item);
    }

    return flattened.sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0));
}

function mapCourse(program: GeneratedApi.LearningCoursesProgram): LearningCourseSummary {
    return {
        id: program.id ?? '',
        title: program.title ?? 'Untitled course',
        slug: program.slug ?? '',
        description: program.description ?? '',
        thumbnail: program.thumbnail ?? null,
        category: String(program.category ?? 'General'),
        difficulty: String(program.difficulty ?? 'Beginner'),
        estimatedHours: program.estimatedHours ?? null,
        currentEnrollments: program.currentEnrollments ?? 0,
        averageRating: program.averageRating ?? 0,
        isEnrollmentOpen: program.isEnrollmentOpen ?? false,
    };
}

function getContentProgressMap(contentProgress: LearningCoursesContentProgress[] | null | undefined) {
    return new Map(
        (contentProgress ?? [])
            .filter((entry): entry is LearningCoursesContentProgress & { contentId: string } => Boolean(entry.contentId))
            .map((entry) => [entry.contentId, entry]),
    );
}

function mapProgressStatus(status?: LearningCoursesProgressStatus): ProgressItemStatus {
    switch (status) {
        case 'Completed':
        case 'Submitted':
            return 'completed';
        case 'InProgress':
            return 'in-progress';
        case 'NotStarted':
        default:
            return 'not-started';
    }
}

function mapAttendanceStatus(progressStatus: ProgressItemStatus | undefined, unlocked: boolean): CourseAttendanceItem['status'] {
    if (progressStatus === 'completed') {
        return 'completed';
    }

    if (progressStatus === 'in-progress') {
        return 'in-progress';
    }

    return unlocked ? 'available' : 'locked';
}

function mapItemType(type: LearningCoursesProgramContent['type']): CourseAttendanceItem['type'] {
    switch (type) {
        case 'Assignment':
            return 'assignment';
        case 'Questionnaire':
            return 'quiz';
        case 'Discussion':
            return 'peer-review';
        case 'Code':
        case 'Project':
        case 'Reflection':
        case 'Survey':
            return 'activity';
        case 'Lesson':
        default:
            return 'lesson';
    }
}

export async function getPublicCourses(): Promise<LearningCourseSummary[]> {
    try {
        const { programs } = createCourseModules();
        const result = await programs.getCoursesPublic();

        if (!result.ok || !Array.isArray(result.data)) {
            return [];
        }

        return result.data.map(mapCourse);
    } catch (error) {
        console.error('[learning] Failed to fetch public courses', error);
        return [];
    }
}

export async function getPublicCourseBySlug(slug: string): Promise<LearningCourseSummary | null> {
    try {
        const { programs } = createCourseModules();
        const result = await programs.getCoursesSlug(encodeURIComponent(slug));

        if (!result.ok) {
            return null;
        }

        return mapCourse(result.data);
    } catch (error) {
        console.error('[learning] Failed to fetch course by slug', error);
        return null;
    }
}

export async function getCourseAttendanceData(
    courseSlug: string,
    options?: { includeProgress?: boolean },
): Promise<CourseAttendanceData | null> {
    try {
        const includeProgress = options?.includeProgress ?? false;
        const token = includeProgress ? await getOptionalToken() : null;

        if (includeProgress && !token) {
            return null;
        }

        const publicModules = createCourseModules();
        const authenticatedModules = token ? createCourseModules(async () => token) : null;
        const programs = authenticatedModules?.programs ?? publicModules.programs;
        const content = authenticatedModules?.content ?? publicModules.content;
        const courseResult = await programs.getCoursesSlug(encodeURIComponent(courseSlug));

        if (!courseResult.ok || !courseResult.data.id) {
            return null;
        }

        const course = mapCourse(courseResult.data);
        const courseId = course.id;
        const [contentResult, progressResult] = await Promise.all([
            content.getCoursesContent(courseId),
            authenticatedModules?.programs
                ? authenticatedModules.programs.getCoursesMeProgress(courseId)
                : Promise.resolve(undefined),
        ]);

        if (includeProgress && (!progressResult || !progressResult.ok)) {
            return null;
        }

        if (!contentResult.ok) {
            if (includeProgress) {
                return null;
            }

            return {
                ...course,
                modules: [],
                overallProgress: 0,
                totalItems: 0,
                completedItems: 0,
                remainingMinutes: 0,
            };
        }

        const flatContent = flattenContent(contentResult.data).filter(
            (item): item is LearningCoursesProgramContent & { id: string } => Boolean(item.id),
        );
        const progress = progressResult && progressResult.ok ? progressResult.data : undefined;
        const progressByContentId = getContentProgressMap(progress?.contentProgress);
        const topLevelContent = flatContent
            .filter((item) => !item.parentId)
            .sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0));
        const hasNestedContent = flatContent.some((item) => Boolean(item.parentId));

        const moduleSources: CourseAttendanceModuleSource[] = hasNestedContent
            ? topLevelContent.map((module) => ({
                id: module.id,
                title: module.title ?? 'Untitled module',
                description: module.description ?? '',
                order: module.sortOrder ?? 0,
                items: flatContent
                    .filter((item) => item.parentId === module.id)
                    .sort((left, right) => (left.sortOrder ?? 0) - (right.sortOrder ?? 0)),
            }))
            : [
                {
                    id: `${courseId}-content`,
                    title: 'Course Content',
                    description: course.description,
                    order: 0,
                    items: topLevelContent,
                },
            ];

        let nextUnlockedAssigned = false;
        const modules = moduleSources.map((module) => {
            const items = module.items.map((item) => {
                const itemProgress = progressByContentId.get(item.id);
                const progressStatus = mapProgressStatus(itemProgress?.status);
                const unlocked = !nextUnlockedAssigned;
                const status = mapAttendanceStatus(progressStatus, unlocked);

                if (status === 'available' || status === 'in-progress') {
                    nextUnlockedAssigned = true;
                }

                return {
                    id: item.id,
                    title: item.title ?? 'Untitled content',
                    type: mapItemType(item.type),
                    status,
                    duration: item.estimatedMinutes ?? undefined,
                    description: item.description ?? undefined,
                    order: item.sortOrder ?? 0,
                    isRequired: item.isRequired ?? false,
                    content: item.body ?? undefined,
                } satisfies CourseAttendanceItem;
            });

            const completedItems = items.filter((item) => item.status === 'completed').length;

            return {
                id: module.id,
                title: module.title,
                description: module.description,
                order: module.order,
                items,
                progress: items.length > 0 ? Math.round((completedItems / items.length) * 100) : 0,
            } satisfies CourseAttendanceModule;
        });

        const allItems = modules.flatMap((module) => module.items);
        const completedItems = allItems.filter((item) => item.status === 'completed').length;
        const remainingMinutes = allItems
            .filter((item) => item.status !== 'completed')
            .reduce((total, item) => total + (item.duration ?? 0), 0);
        const currentItem = allItems.find((item) => item.status === 'in-progress' || item.status === 'available');

        return {
            ...course,
            modules,
            overallProgress: allItems.length > 0 ? Math.round((completedItems / allItems.length) * 100) : 0,
            totalItems: allItems.length,
            completedItems,
            currentItem,
            remainingMinutes,
        };
    } catch (error) {
        console.error('[learning] Failed to build course attendance data', error);
        return null;
    }
}
