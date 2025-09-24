import type { ModulesProgramsProgram } from '@/lib/api/generated/types.gen'
import { ModulesContentsContentStatus, ModulesProgramsProgramDifficulty, ProgramCategory } from '@/lib/api/generated/types.gen'
import { notFound } from 'next/navigation'
import { getCourseBySlug } from '../actions'
import { CourseDetails } from './course-details.client'

interface PageProps {
    params: Promise<{ slug: string }>;
}

// Helper function to map course category to ProgramCategory enum
function mapCategoryToEnum(category: string): ProgramCategory | undefined {
    const categoryMap: Record<string, ProgramCategory> = {
        'programming': ProgramCategory.PROGRAMMING,
        'data-science': ProgramCategory.DATA_SCIENCE,
        'web-development': ProgramCategory.WEB_DEVELOPMENT,
        'mobile-development': ProgramCategory.MOBILE_DEVELOPMENT,
        'game-development': ProgramCategory.GAME_DEVELOPMENT,
        'ai': ProgramCategory.AI,
        'cybersecurity': ProgramCategory.CYBERSECURITY,
        'devops': ProgramCategory.DEV_OPS,
        'database': ProgramCategory.DATABASE,
        'business': ProgramCategory.BUSINESS,
        'design': ProgramCategory.DESIGN,
        'marketing': ProgramCategory.MARKETING,
        'project-management': ProgramCategory.PROJECT_MANAGEMENT,
        'personal-development': ProgramCategory.PERSONAL_DEVELOPMENT,
        'creative-arts': ProgramCategory.CREATIVE_ARTS,
        'science': ProgramCategory.SCIENCE,
        'language': ProgramCategory.LANGUAGE,
        'other': ProgramCategory.OTHER,
    };
    return categoryMap[category.toLowerCase()] || ProgramCategory.OTHER;
}

// Helper function to map course level to difficulty enum
function mapLevelToDifficulty(level: string): ModulesProgramsProgramDifficulty | undefined {
    const difficultyMap: Record<string, ModulesProgramsProgramDifficulty> = {
        'beginner': ModulesProgramsProgramDifficulty.BEGINNER,
        'intermediate': ModulesProgramsProgramDifficulty.INTERMEDIATE,
        'advanced': ModulesProgramsProgramDifficulty.ADVANCED,
        'expert': ModulesProgramsProgramDifficulty.EXPERT,
    };
    return difficultyMap[level.toLowerCase()];
}

// Helper function to map course status to content status enum
function mapStatusToEnum(status: string): ModulesContentsContentStatus | undefined {
    const statusMap: Record<string, ModulesContentsContentStatus> = {
        'draft': ModulesContentsContentStatus.DRAFT,
        'under-review': ModulesContentsContentStatus.UNDER_REVIEW,
        'published': ModulesContentsContentStatus.PUBLISHED,
        'archived': ModulesContentsContentStatus.ARCHIVED,
    };
    return statusMap[status.toLowerCase()];
}

export default async function Page({ params }: PageProps) {
    const { slug } = await params;

    // Fetch course data server-side using GraphQL
    const course = await getCourseBySlug(slug);

    if (!course) {
        notFound();
    }

    // Transform course to Program type for the CourseDetails component
    const program: ModulesProgramsProgram = {
        id: course.id,
        title: course.title,
        description: course.description,
        slug: course.slug,
        thumbnail: course.thumbnailUrl || null,
        videoShowcaseUrl: course.trailerUrl || null,
        category: mapCategoryToEnum(course.category),
        difficulty: mapLevelToDifficulty(course.level),
        estimatedHours: course.duration,
        status: mapStatusToEnum(course.status),
        visibility: 'PUBLIC' as any, // Will need to import proper enum
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString()
    }

    return (
        <div className="container mx-auto py-8">
            <CourseDetails course={program} />
        </div>
    )
}