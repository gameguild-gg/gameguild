// NOTE: These types are stubs for modules NOT enabled in GameGuild.Production.sln
// They represent types from disabled backend modules

// Testing Lab Module (NOT ENABLED)
export enum SessionStatus {
    Pending = 'Pending',
    InProgress = 'InProgress',
    Completed = 'Completed',
    Cancelled = 'Cancelled',
    Failed = 'Failed',
}

export enum ModuleType {
    Program = 'Program',
    Project = 'Project',
    Course = 'Course',
    Assessment = 'Assessment',
    TESTING_LAB = 'TestingLab',
}

// Add other stub types as needed for disabled modules

// STUB: Added to satisfy missing imports from disabled/legacy modules
export enum AccessLevel {
    Public = 'Public',
    Private = 'Private',
    Restricted = 'Restricted',
}

// STUB: Content status placeholder for legacy references
export enum ContentStatus {
    Draft = 'Draft',
    Published = 'Published',
    Archived = 'Archived',
}
export type TestingSession = any;
export type TestingRequest = any;
export type TestingLocation = any;

// STUB: Program-related enums referenced in dashboard course pages
export enum ProgramCategory {
    PROGRAMMING = 'programming',
    DATA_SCIENCE = 'data-science',
    WEB_DEVELOPMENT = 'web-development',
    MOBILE_DEVELOPMENT = 'mobile-development',
    GAME_DEVELOPMENT = 'game-development',
    AI = 'ai',
    CYBERSECURITY = 'cybersecurity',
    DEV_OPS = 'devops',
    DATABASE = 'database',
    BUSINESS = 'business',
    DESIGN = 'design',
    MARKETING = 'marketing',
    PROJECT_MANAGEMENT = 'project-management',
    PERSONAL_DEVELOPMENT = 'personal-development',
    CREATIVE_ARTS = 'creative-arts',
    SCIENCE = 'science',
    LANGUAGE = 'language',
    OTHER = 'other',
}

export enum ModulesProgramsProgramDifficulty {
    BEGINNER = 'beginner',
    INTERMEDIATE = 'intermediate',
    ADVANCED = 'advanced',
    EXPERT = 'expert',
}

export enum ModulesContentsContentStatus {
    DRAFT = 'draft',
    UNDER_REVIEW = 'under-review',
    PUBLISHED = 'published',
    ARCHIVED = 'archived',
}

// STUB: Program type placeholder for disabled/legacy modules
export type Program = any;

// STUB: ProgramContentDto for course content pages
export type ProgramContentDto = any;

// STUB: ProgramContent for course content layout
export type ProgramContent = any;

// STUB: CreateContentDto for creating course content
export type CreateContentDto = any;

// STUB: Project type for disabled modules
export type Project = any;

// STUB: ProjectVersion type for disabled modules
export type ProjectVersion = any;

// STUB: User types (may be partially available)
export type UserResponseDto = any;

// STUB: Program alias for compatibility
export type ModulesProgramsProgram = any;

// STUB: ProgramDifficulty alias
export enum ProgramDifficulty {
    BEGINNER = 'beginner',
    INTERMEDIATE = 'intermediate',
    ADVANCED = 'advanced',
    EXPERT = 'expert',
}

// STUB: Testing Lab types
export type TestingFeedback = any;
export enum LocationStatus {
    ACTIVE = 'active',
    INACTIVE = 'inactive',
    MAINTENANCE = 'maintenance',
}
export type UserRoleAssignment = any;

// STUB: Commerce/Pricing types
export type PricingDto = any;

// STUB: Tenant types
export type Tenant = any;
export type ModulesTenantsTenant = any;
export type ModulesTenantsTenantDomain = any;
export type ModulesTenantsTenantUserGroup = any;
