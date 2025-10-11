export interface Program {
  id: string;
  version: number;
  title: string;
  description?: string;
  slug: string;
  thumbnail?: string;
  videoShowcaseUrl?: string;
  estimatedHours?: number;
  enrollmentStatus: EnrollmentStatus;
  maxEnrollments?: number;
  category: ProgramCategory;
  difficulty: ProgramDifficulty;
  visibility: ContentVisibility;
  status: ContentStatus;
  isPublished: boolean;
  publishedAt?: string;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  isDeleted: boolean;
  creatorId: string;
  creatorName?: string;
  enrollmentCount?: number;
  completionRate?: number;
  averageRating?: number;
  ratingCount?: number;
  totalRevenue?: number;
}

export enum EnrollmentStatus {
  Open = 'Open',
  Closed = 'Closed',
  Waitlist = 'Waitlist',
}

export enum ProgramCategory {
  GameDevelopment = 'GameDevelopment',
  Programming = 'Programming',
  Art = 'Art',
  Design = 'Design',
  Audio = 'Audio',
  Business = 'Business',
  Marketing = 'Marketing',
  Other = 'Other',
}

export enum ProgramDifficulty {
  Beginner = 'Beginner',
  Intermediate = 'Intermediate',
  Advanced = 'Advanced',
  Expert = 'Expert',
}

export enum ContentVisibility {
  Public = 'Public',
  Private = 'Private',
  Unlisted = 'Unlisted',
}

export enum ContentStatus {
  Draft = 'Draft',
  InReview = 'InReview',
  Published = 'Published',
  Archived = 'Archived',
}

export interface CreateProgramRequest {
  title: string;
  description?: string;
  slug: string;
  thumbnail?: string;
}

export interface UpdateProgramRequest {
  title?: string;
  description?: string;
  slug?: string;
  thumbnail?: string;
  videoShowcaseUrl?: string;
  estimatedHours?: number;
  enrollmentStatus?: EnrollmentStatus;
  maxEnrollments?: number;
  category?: ProgramCategory;
  difficulty?: ProgramDifficulty;
  visibility?: ContentVisibility;
  expectedVersion?: number;
}

export interface CloneProgramRequest {
  originalProgramId: string;
  newTitle: string;
  newSlug: string;
  includeContent?: boolean;
}

export interface ProgramStatistics {
  totalPrograms: number;
  publishedPrograms: number;
  draftPrograms: number;
  archivedPrograms: number;
  totalEnrollments: number;
  totalRevenue: number;
  averageCompletionRate: number;
}

export interface SearchProgramsParams {
  searchTerm?: string;
  category?: ProgramCategory;
  difficulty?: ProgramDifficulty;
  visibility?: ContentVisibility;
  status?: ContentStatus;
  creatorId?: string;
  isPublished?: boolean;
  minEstimatedHours?: number;
  maxEstimatedHours?: number;
  createdAfter?: string;
  createdBefore?: string;
  skip?: number;
  take?: number;
  sortBy?: ProgramSortField;
  sortDirection?: SortDirection;
}

export enum ProgramSortField {
  Title = 'Title',
  CreatedAt = 'CreatedAt',
  UpdatedAt = 'UpdatedAt',
  PublishedAt = 'PublishedAt',
  EnrollmentCount = 'EnrollmentCount',
  CompletionRate = 'CompletionRate',
  Rating = 'Rating',
  Revenue = 'Revenue',
}

export enum SortDirection {
  Ascending = 'Ascending',
  Descending = 'Descending',
}

export interface BulkProgramOperation {
  programIds: string[];
  reason?: string;
}

export interface BulkOperationResult {
  successCount: number;
  failureCount: number;
  errors: string[];
  results?: any[];
}
