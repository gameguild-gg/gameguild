// NOTE: These types are stubs for modules NOT enabled in GameGuild.Production.sln
// They represent types from disabled backend modules

// ProductProgram stub (for mock data)
export interface ProductProgram {
  id?: string;
  productId?: string;
  programId?: string;
  order?: number;
  sortOrder?: number;
  createdAt?: string;
  updatedAt?: string;
  product?: any;
  program?: any;
}

// Testing Lab Module (NOT ENABLED)
export enum SessionStatus {
  Pending = 'Pending',
  InProgress = 'InProgress',
  Completed = 'Completed',
  Cancelled = 'Cancelled',
  Failed = 'Failed',
  // Additional status variants used by components (aliases with same string values)
  SCHEDULED = 'SCHEDULED',
  ACTIVE = 'ACTIVE',
  // Uppercase variants for compatibility
  COMPLETED_UPPER = 'COMPLETED',
  CANCELLED_UPPER = 'CANCELLED',
  PENDING = 'PENDING',
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
  // Also add uppercase aliases for compatibility
  PUBLIC = 'Public',
  PRIVATE = 'Private',
  RESTRICTED = 'Restricted',
}

// STUB: Content status placeholder for legacy references
export enum ContentStatus {
  Draft = 'Draft',
  Published = 'Published',
  Archived = 'Archived',
  // Also add uppercase and other values for compatibility
  DRAFT = 'Draft',
  PUBLISHED = 'Published',
  ARCHIVED = 'Archived',
  UNDER_REVIEW = 'Under Review',
}

// STUB: Testing Session interface for disabled testing-lab module
export interface TestingSession {
  id: string;
  title?: string;
  description?: string;
  sessionType?: string;
  sessionDate?: string;
  maxTesters?: number;
  currentTesters?: number;
  gameTitle?: string;
  gameDeveloper?: string;
  platform?: string[];
  featuredGames?: any[];
  skillLevel?: string;
  duration?: number;
  currentGames?: number;
  maxGames?: number;
  status?: SessionStatus;
  location?: TestingLocation;
  participants?: any[];
  feedback?: any[];
  createdAt?: string;
  updatedAt?: string;
  [key: string]: any; // Allow any additional properties
}

// STUB: Testing Request interface
export interface TestingRequest {
  id: string;
  title?: string;
  description?: string;
  status?: SessionStatus;
  requestedBy?: string;
  assignedTo?: string[];
  gameId?: string;
  gameName?: string;
  platform?: string[];
  requirements?: string;
  deadline?: string;
  createdAt?: string;
  updatedAt?: string;
  [key: string]: any;
}

// STUB: Testing Location interface
export interface TestingLocation {
  id: string;
  name?: string;
  address?: string;
  capacity?: number;
  isOnline?: boolean;
  status?: LocationStatus;
  equipment?: string[];
  [key: string]: any;
}

// STUB: Test Session for api/testing-lab
export interface TestSession {
  id: string;
  title?: string;
  description?: string;
  sessionType?: string;
  sessionDate?: string;
  maxTesters?: number;
  currentTesters?: number;
  gameTitle?: string;
  gameDeveloper?: string;
  platform?: string[];
  featuredGames?: any[];
  skillLevel?: string;
  duration?: number;
  currentGames?: number;
  maxGames?: number;
  status?: SessionStatus;
  location?: TestingLocation;
  [key: string]: any;
}

// STUB: Project types
export type ProjectReadable = any;

// STUB: Post types
export type PostDto = any;
export type GetApiPostsData = any;
export type PostApiPostsData = any;
export const postApiPosts = async (_options?: any): Promise<{ data?: any; error?: { message: string } }> => ({ data: {} });

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
  ACTIVE = 0,
  INACTIVE = 2,
  MAINTENANCE = 1,
}
export type UserRoleAssignment = any;

// STUB: Commerce/Pricing types
export type PricingDto = any;

// STUB: Tenant types
export type Tenant = any;
export type ModulesTenantsTenant = any;
export type ModulesTenantsTenantDomain = any;
export type ModulesTenantsTenantUserGroup = any;

// ================================
// Additional Stub Types
// ================================

// STUB: User-related types
export type UserResponseDtoPagedResult = any;
export type UpdateUserDto = any;
export type CreateUserDto = any;
export type AssignRoleRequest = any;
export type CreateRoleRequest = any;
export type UserPermission = any;

// STUB: Testing Lab additional types
export type CreateTestingLocationDto = any;
export type UpdateTestingLocationDto = any;
export type TestingLabPermissions = any;
export type TestingLabFilterControls = any;

// STUB: Activity/Grading types
export type ActivityGradeDto = any;

// STUB: Posts types
export type CreatePostDto = any;
export type PostsPageDto = any;

// STUB: Commerce/Subscription types
export enum SubscriptionStatus {
  ACTIVE = 'active',
  CANCELLED = 'cancelled',
  EXPIRED = 'expired',
  PENDING = 'pending',
}

// STUB: Module permission types
export type ModuleAction = any;
export type ModulePermission = any;

// STUB: Product (commerce)
export type Product = any;

// STUB: Visibility enum
export enum Visibility {
  PUBLIC = 'public',
  PRIVATE = 'private',
  UNLISTED = 'unlisted',
}

// ================================
// Stub SDK Functions (disabled modules)
// ================================

// Projects API stubs
type StubApiResult<T> = Promise<{ data?: T; error?: { message: string } }>;
export const getApiProjects = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const getApiProjectsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiProjectsByIdStatistics = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiProjectsFeatured = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const getApiProjectsPopular = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const getApiProjectsRecent = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const getApiProjectsSearch = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const postApiProjects = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiProjectsByIdArchive = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiProjectsByIdPublish = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiProjectsByIdUnpublish = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const deleteApiProjectsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const putApiProjectsById = async (_options?: any): StubApiResult<any> => ({ data: {} });

// Permissions API stubs
export const getApiAdminPermissionsUsersByUserIdPermissions = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiAdminPermissionsUsersByUserIdPermissions = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const deleteApiAdminPermissionsUsersByUserIdPermissions = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiAdminPermissionsUsersByUserIdRoles = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const deleteApiAdminPermissionsUsersByUserIdRolesByRoleName = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiAdminPermissionsUsersByUserIdCheck = async (_options?: any): StubApiResult<any> => ({ data: {} });

// Payments API stubs
export const getApiPaymentMethodsMe = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiPaymentsMyPayments = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const getApiPaymentsProductsByProductId = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiPaymentsRevenueReport = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiPaymentsUsersByUserId = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const getApiPaymentUserByUserId = async (_options?: any): StubApiResult<any> => ({ data: {} });

// Posts API stubs
export const getApiPosts = async (_options?: any): StubApiResult<any[]> => ({ data: [] });

// Users API stubs
export const getApiUsersById = async (_options?: any): StubApiResult<any> => ({ data: {} });

// ================================
// Stub Data Types for API functions
// ================================
export type GetApiPaymentsMyPaymentsData = any;
export type GetApiPaymentsProductsByProductIdData = any;
export type GetApiPaymentsRevenueReportData = any;
export type GetApiPaymentUserByUserIdData = any;
// GetApiPostsData defined above
export type PostApiPaymentByIdProcessData = any;
export type PostApiPaymentByIdRefundData = any;

// ================================
// Additional Missing Stub Types
// ================================

// STUB: Module/Role types
export type ModuleRole = any;
export type PermissionConstraint = any;

// STUB: Payment types
export enum PaymentStatus {
  PENDING = 'pending',
  PROCESSING = 'processing',
  COMPLETED = 'completed',
  FAILED = 'failed',
  CANCELLED = 'cancelled',
  REFUNDED = 'refunded',
}

// ================================
// Additional Stub SDK Functions
// ================================

// Testing Lab API stubs
export const getTestingRequestsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiTestingRequests = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const putApiTestingRequestsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const deleteApiTestingRequestsById = async (_options?: any): StubApiResult<any> => ({ data: {} });

// Additional Payment API stubs
export const postApiPaymentIntent = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiPaymentByIdProcess = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiPaymentByIdRefund = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiPaymentsByIdCancel = async (_options?: any): StubApiResult<any> => ({ data: {} });

// Subscriptions API stubs
export const getApiSubscriptions = async (_options?: any): StubApiResult<any[]> => ({ data: [] });
export const getApiSubscriptionsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiSubscriptions = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const putApiSubscriptionsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const deleteApiSubscriptionsById = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiSubscriptionsByIdCancel = async (_options?: any): StubApiResult<any> => ({ data: {} });
export const postApiSubscriptionsByIdRenew = async (_options?: any): StubApiResult<any> => ({ data: {} });
