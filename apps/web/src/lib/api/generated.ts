export enum ProgramContentType {
    Lesson = 0,
    Assignment = 2,
    Questionnaire = 3,
    Discussion = 4,
    Code = 5,
    Reflection = 7,
    Survey = 8,
    Project = 9,
    PAGE = 0,
    REVEAL = 0,
}

export enum ProgramCategory {
    General = 'General',
    Programming = 'Programming',
    DataScience = 'DataScience',
    WebDevelopment = 'WebDevelopment',
    MobileDevelopment = 'MobileDevelopment',
    GameDevelopment = 'GameDevelopment',
    AI = 'AI',
    Cybersecurity = 'Cybersecurity',
    DevOps = 'DevOps',
    Database = 'Database',
    Business = 'Business',
    Design = 'Design',
    Marketing = 'Marketing',
    ProjectManagement = 'ProjectManagement',
    PersonalDevelopment = 'PersonalDevelopment',
    CreativeArts = 'CreativeArts',
    Science = 'Science',
    Language = 'Language',
    Other = 'Other',
    GENERAL = 'General',
    PROGRAMMING = 'Programming',
    DATA_SCIENCE = 'DataScience',
    WEB_DEVELOPMENT = 'WebDevelopment',
    MOBILE_DEVELOPMENT = 'MobileDevelopment',
    GAME_DEVELOPMENT = 'GameDevelopment',
    CYBERSECURITY = 'Cybersecurity',
    DEVOPS = 'DevOps',
    DATABASE = 'Database',
    BUSINESS = 'Business',
    DESIGN = 'Design',
    MARKETING = 'Marketing',
    PROJECT_MANAGEMENT = 'ProjectManagement',
    PERSONAL_DEVELOPMENT = 'PersonalDevelopment',
    CREATIVE_ARTS = 'CreativeArts',
    SCIENCE = 'Science',
    LANGUAGE = 'Language',
    OTHER = 'Other',
}

export interface ProgramContent {
    id?: string | number;
    title?: string | null;
    slug?: string | null;
    description?: string | null;
    body?: string | null;
    parent?: ProgramContent | string | number | null;
    parentId?: string | number | null;
    type?: ProgramContentType | number | null;
    estimatedMinutes?: number | null;
    isRequired?: boolean | null;
    children?: ProgramContent[] | null;
    [key: string]: unknown;
}

export interface Program {
    id?: string | number;
    title?: string | null;
    slug?: string | null;
    description?: string | null;
    category?: string | number | null;
    difficulty?: string | number | null;
    estimatedHours?: number | null;
    currentEnrollments?: number | null;
    programContents?: ProgramContent[] | null;
    [key: string]: unknown;
}

export interface ProductProgram {
    id?: string | number;
    productId?: string | number | null;
    programId?: string | number | null;
    [key: string]: unknown;
}

export interface Product {
    id?: string | number;
    title?: string | null;
    slug?: string | null;
    productPrograms?: ProductProgram[] | null;
    [key: string]: unknown;
}

export interface UserResponseDto {
    id?: string | number;
    name?: string | null;
    username?: string | null;
    email?: string | null;
    role?: string | null;
    subscriptionType?: string | null;
    imageUrl?: string | null;
    [key: string]: unknown;
}

export interface ProjectReadable {
    id?: string | number;
    slug?: string | null;
    title?: string | null;
    shortDescription?: string | null;
    status?: string | number | null;
    imageUrl?: string | null;
    [key: string]: unknown;
}

export async function getApiUsersById(..._args: unknown[]): Promise<{ data: UserResponseDto | null }> {
    return { data: null };
}
