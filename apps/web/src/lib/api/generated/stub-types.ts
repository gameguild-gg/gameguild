export enum ModulesContentsContentStatus {
    DRAFT = 'Draft',
    UNDER_REVIEW = 'UnderReview',
    PUBLISHED = 'Published',
    ARCHIVED = 'Archived',
}

export enum ModulesProgramsProgramDifficulty {
    BEGINNER = 'Beginner',
    INTERMEDIATE = 'Intermediate',
    ADVANCED = 'Advanced',
    EXPERT = 'Expert',
}

export async function getApiProjectsSlugBySlug(..._args: unknown[]): Promise<{ data: Record<string, unknown> | null }> {
    return { data: null };
}

export async function postApiProjects(..._args: unknown[]): Promise<{ data: Record<string, unknown> | null }> {
    return { data: null };
}