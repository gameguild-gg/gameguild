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

// NOTE: Avoid exporting enums that conflict with generated OpenAPI types.

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
// Note: AccessLevel, ContentStatus, and ProgramCategory are available from the generated types.gen module

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

// Note: Program, ProgramContentDto, ProgramContent, CreateContentDto, Project,
// PricingDto, Tenant and related types are available from the generated types.gen module

// STUB: Project type for disabled modules (keeping for legacy compatibility)
export type Project = any;
export type ProjectVersion = any;

// STUB: User types (may be partially available)
export type UserResponseDto = any;

// STUB: Program alias for compatibility
export type ModulesProgramsProgram = any;

// ProgramDifficulty stub removed to avoid conflicts with generated enums

// STUB: Testing Lab types
export type TestingFeedback = any;
export enum LocationStatus {
  ACTIVE = 0,
  INACTIVE = 2,
  MAINTENANCE = 1,
}
export type UserRoleAssignment = any;

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

// Note: ActivityGradeDto is available from the generated types.gen module

// STUB: Posts types
export type CreatePostDto = any;
export type PostsPageDto = any;

// STUB: Commerce/Subscription types
// SubscriptionStatus stub removed to avoid conflicts with generated enums

// STUB: Module permission types
export type ModuleAction = any;
export type ModulePermission = any;

// STUB: Product (commerce)
export type Product = any;

// Visibility stub removed to avoid conflicts with generated enums

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

// PaymentStatus stub removed to avoid conflicts with generated enums

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

// ================================
// Legacy Stubs (from legacy-stubs.ts)
// ================================
// Temporary legacy stubs to satisfy legacy project form imports.
// These endpoints are no longer present in the generated API client.
// TODO: Remove this section once the legacy project form is migrated to the current API.

export interface LegacyProject {
  imageUrl?: string;
  [key: string]: unknown;
}

export interface LegacyApiResponse<T = unknown> {
  data?: T;
}

export interface LegacyRequestOptions {
  // Generic shape matching what legacy callers expect
  path?: Record<string, unknown>;
  headers?: Record<string, string>;
  body?: unknown;
}

export async function getApiProjectsSlugBySlug(_options: LegacyRequestOptions = {}): Promise<LegacyApiResponse<LegacyProject>> {
  // Placeholder implementation; returns empty response to avoid runtime failures.
  return { data: undefined };
}
