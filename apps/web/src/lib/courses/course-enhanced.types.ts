/**
 * Enhanced types and interfaces for Course Provider Refactoring
 */

import { ReactNode } from 'react';

/**
 * Course status types
 */
export type CourseStatus = 'draft' | 'published' | 'archived';

/**
 * Course view mode types
 */
export type CourseViewMode = 'grid' | 'list' | 'table';

/**
 * Course sync status types
 */
export type CourseSyncStatus = 'idle' | 'syncing' | 'synced' | 'error';

/**
 * Course Action Types
 */
export const CourseActionTypes = {
  // Loading states
  SET_LOADING: 'SET_LOADING',
  SET_ERROR: 'SET_ERROR',
  CLEAR_ERROR: 'CLEAR_ERROR',

  // Course data operations
  SET_COURSES: 'SET_COURSES',
  ADD_COURSE: 'ADD_COURSE',
  UPDATE_COURSE: 'UPDATE_COURSE',
  DELETE_COURSE: 'DELETE_COURSE',

  // Selection operations
  SET_SELECTED_COURSES: 'SET_SELECTED_COURSES',
  TOGGLE_COURSE_SELECTION: 'TOGGLE_COURSE_SELECTION',
  SELECT_ALL_COURSES: 'SELECT_ALL_COURSES',
  CLEAR_SELECTION: 'CLEAR_SELECTION',

  // Filter operations
  SET_SEARCH: 'SET_SEARCH',
  SET_AREA_FILTER: 'SET_AREA_FILTER',
  SET_LEVEL_FILTER: 'SET_LEVEL_FILTER',
  SET_STATUS_FILTER: 'SET_STATUS_FILTER',
  SET_INSTRUCTOR_FILTER: 'SET_INSTRUCTOR_FILTER',
  SET_TAG_FILTER: 'SET_TAG_FILTER',
  SET_SORT: 'SET_SORT',
  RESET_FILTERS: 'RESET_FILTERS',

  // Pagination operations
  SET_PAGE: 'SET_PAGE',
  SET_PAGE_SIZE: 'SET_PAGE_SIZE',
  SET_PAGINATION: 'SET_PAGINATION',

  // View operations
  SET_VIEW_MODE: 'SET_VIEW_MODE',
  TOGGLE_VIEW_MODE: 'TOGGLE_VIEW_MODE',

  // Sync operations
  SET_SYNC_STATUS: 'SET_SYNC_STATUS',
  SET_LAST_SYNC_TIME: 'SET_LAST_SYNC_TIME',
  ADD_PENDING_CHANGE: 'ADD_PENDING_CHANGE',
  REMOVE_PENDING_CHANGE: 'REMOVE_PENDING_CHANGE',
  CLEAR_PENDING_CHANGES: 'CLEAR_PENDING_CHANGES',

  // Optimistic updates
  ADD_OPTIMISTIC_UPDATE: 'ADD_OPTIMISTIC_UPDATE',
  REMOVE_OPTIMISTIC_UPDATE: 'REMOVE_OPTIMISTIC_UPDATE',
  CLEAR_OPTIMISTIC_UPDATES: 'CLEAR_OPTIMISTIC_UPDATES',

  // Configuration
  UPDATE_CONFIG: 'UPDATE_CONFIG',
  RESET_STATE: 'RESET_STATE',
} as const;

/**
 * Course Area Types
 */
export type CourseArea = 'programming' | 'art' | 'design' | 'audio';

/**
 * Course Level Types
 */
export type CourseLevel = 1 | 2 | 3 | 4;
export type CourseLevelName = 'Beginner' | 'Intermediate' | 'Advanced' | 'Arcane';

/**
 * Course Analytics
 */
export interface CourseAnalytics {
  enrollments: number;
  completions: number;
  averageRating: number;
  revenue: number;
  viewCount: number;
  completionRate: number;
}

/**
 * Course Content
 */
export interface CourseContent {
  id: string;
  title: string;
  type: 'lesson' | 'quiz' | 'assignment' | 'project';
  duration: number;
  isRequired: boolean;
  order: number;
  description?: string;
  videoUrl?: string;
  materials?: string[];
}

/**
 * Enhanced Course Interface
 */
export interface Course {
  id: string;
  title: string;
  description: string;
  area: CourseArea;
  level: CourseLevel;
  tools: string[];
  image: string;
  slug: string;
  progress?: number;
  analytics?: CourseAnalytics;
  content?: CourseContent[];
  instructors?: string[];
  tags?: string[];
  publishedAt?: string;
  updatedAt?: string;
  createdAt?: string;
  status?: CourseStatus;
  enrollmentCount?: number;
  estimatedHours?: number;
  difficulty?: number;
  prerequisites?: string[];
  learningObjectives?: string[];
  price?: number;
  currency?: string;
  isPublic?: boolean;
  isFeatured?: boolean;
}

/**
 * Tools by Area
 */
export interface ToolsByArea {
  programming: string[];
  art: string[];
  design: string[];
  audio: string[];
}

/**
 * Course Data
 */
export interface CourseData {
  courses: Course[];
  toolsByArea: ToolsByArea;
  categories?: CourseArea[];
  totalCount?: number;
}

/**
 * Enhanced Course Filters
 */
export interface EnhancedCourseFilters {
  search: string;
  area: CourseArea | 'all';
  level: CourseLevelName | 'all';
  status: CourseStatus | 'all';
  instructor: string;
  tag: string;
  tool: string;
  difficulty: number | 'all';
  priceRange: { min: number; max: number } | null;
  sortBy: 'title' | 'createdAt' | 'updatedAt' | 'enrollments' | 'rating' | 'price' | 'difficulty';
  sortOrder: 'asc' | 'desc';
  showFeatured: boolean;
  showPublicOnly: boolean;
}

/**
 * Enhanced Course Pagination
 */
export interface EnhancedCoursePagination {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Course Configuration
 */
export interface CourseConfig {
  enableOptimisticUpdates?: boolean;
  enableCaching?: boolean;
  cacheTimeout?: number;
  autoSync?: boolean;
  syncInterval?: number;
  defaultPageSize?: number;
  maxPageSize?: number;
  enableAnalytics?: boolean;
  persistFilters?: boolean;
  persistViewMode?: boolean;
}

/**
 * Enhanced Course State
 */
export interface EnhancedCourseState {
  // Course data
  courses: Course[];
  filteredCourses: Course[];
  totalCount: number;

  // Selection
  selectedCourses: string[];

  // Filters and sorting
  filters: EnhancedCourseFilters;
  appliedFilters: EnhancedCourseFilters;

  // Pagination
  pagination: EnhancedCoursePagination;

  // View state
  viewMode: 'grid' | 'list' | 'table';
  isLoading: boolean;
  isLoadingMore: boolean;
  error: string | null;

  // Sync state
  syncStatus: 'idle' | 'syncing' | 'synced' | 'error';
  lastSyncTime: Date | null;
  pendingChanges: Set<string>;
  optimisticUpdates: Map<string, Course>;

  // Metadata
  lastUpdated: Date | null;
  toolsByArea: ToolsByArea;

  // Configuration
  config: CourseConfig;
}

/**
 * Enhanced Course Actions
 */
export type EnhancedCourseAction =
  // Loading and error actions
  | { type: typeof CourseActionTypes.SET_LOADING; payload: boolean }
  | { type: typeof CourseActionTypes.SET_ERROR; payload: string | null }
  | { type: typeof CourseActionTypes.CLEAR_ERROR }

  // Course data actions
  | { type: typeof CourseActionTypes.SET_COURSES; payload: { courses: Course[]; totalCount?: number } }
  | { type: typeof CourseActionTypes.ADD_COURSE; payload: Course }
  | { type: typeof CourseActionTypes.UPDATE_COURSE; payload: Course }
  | { type: typeof CourseActionTypes.DELETE_COURSE; payload: string }

  // Selection actions
  | { type: typeof CourseActionTypes.SET_SELECTED_COURSES; payload: string[] }
  | { type: typeof CourseActionTypes.TOGGLE_COURSE_SELECTION; payload: string }
  | { type: typeof CourseActionTypes.SELECT_ALL_COURSES }
  | { type: typeof CourseActionTypes.CLEAR_SELECTION }

  // Filter actions
  | { type: typeof CourseActionTypes.SET_SEARCH; payload: string }
  | { type: typeof CourseActionTypes.SET_AREA_FILTER; payload: CourseArea | 'all' }
  | { type: typeof CourseActionTypes.SET_LEVEL_FILTER; payload: CourseLevelName | 'all' }
  | { type: typeof CourseActionTypes.SET_STATUS_FILTER; payload: CourseStatus | 'all' }
  | { type: typeof CourseActionTypes.SET_INSTRUCTOR_FILTER; payload: string }
  | { type: typeof CourseActionTypes.SET_TAG_FILTER; payload: string }
  | { type: typeof CourseActionTypes.SET_SORT; payload: { sortBy: EnhancedCourseFilters['sortBy']; sortOrder: EnhancedCourseFilters['sortOrder'] } }
  | { type: typeof CourseActionTypes.RESET_FILTERS }

  // Pagination actions
  | { type: typeof CourseActionTypes.SET_PAGE; payload: number }
  | { type: typeof CourseActionTypes.SET_PAGE_SIZE; payload: number }
  | { type: typeof CourseActionTypes.SET_PAGINATION; payload: Partial<EnhancedCoursePagination> }

  // View actions
  | { type: typeof CourseActionTypes.SET_VIEW_MODE; payload: 'grid' | 'list' | 'table' }
  | { type: typeof CourseActionTypes.TOGGLE_VIEW_MODE }

  // Sync actions
  | { type: typeof CourseActionTypes.SET_SYNC_STATUS; payload: 'idle' | 'syncing' | 'synced' | 'error' }
  | { type: typeof CourseActionTypes.SET_LAST_SYNC_TIME; payload: Date }
  | { type: typeof CourseActionTypes.ADD_PENDING_CHANGE; payload: string }
  | { type: typeof CourseActionTypes.REMOVE_PENDING_CHANGE; payload: string }
  | { type: typeof CourseActionTypes.CLEAR_PENDING_CHANGES }

  // Optimistic update actions
  | { type: typeof CourseActionTypes.ADD_OPTIMISTIC_UPDATE; payload: { id: string; course: Course } }
  | { type: typeof CourseActionTypes.REMOVE_OPTIMISTIC_UPDATE; payload: string }
  | { type: typeof CourseActionTypes.CLEAR_OPTIMISTIC_UPDATES }

  // Configuration actions
  | { type: typeof CourseActionTypes.UPDATE_CONFIG; payload: Partial<CourseConfig> }
  | { type: typeof CourseActionTypes.RESET_STATE };

/**
 * Course Reducer
 */
export type EnhancedCourseReducer = (state: EnhancedCourseState, action: EnhancedCourseAction) => EnhancedCourseState;

/**
 * Enhanced Course Context Value
 */
export interface EnhancedCourseContextValue {
  state: EnhancedCourseState;
  dispatch: React.Dispatch<EnhancedCourseAction>;

  // Course operations
  loadCourses: () => Promise<void>;
  createCourse: (course: Omit<Course, 'id'>) => Promise<Course>;
  updateCourse: (course: Course) => Promise<Course>;
  deleteCourse: (id: string) => Promise<void>;
  getCourse: (id: string) => Course | undefined;

  // Filter operations
  setSearch: (search: string) => void;
  setAreaFilter: (area: CourseArea | 'all') => void;
  setLevelFilter: (level: CourseLevelName | 'all') => void;
  setStatusFilter: (status: CourseStatus | 'all') => void;
  resetFilters: () => void;
  applyFilters: () => void;

  // Selection operations
  selectCourse: (id: string) => void;
  deselectCourse: (id: string) => void;
  toggleSelection: (id: string) => void;
  selectAll: () => void;
  clearSelection: () => void;

  // Pagination operations
  setPage: (page: number) => void;
  setPageSize: (size: number) => void;
  nextPage: () => void;
  previousPage: () => void;

  // View operations
  setViewMode: (mode: 'grid' | 'list' | 'table') => void;
  toggleViewMode: () => void;

  // Sync operations
  syncCourses: () => Promise<void>;

  // Utility operations
  clearError: () => void;
  resetState: () => void;
  updateConfig: (config: Partial<CourseConfig>) => void;
  exportData: () => Course[];
  importData: (courses: Course[]) => void;
}

/**
 * Enhanced Course Provider Props
 */
export interface EnhancedCourseProviderProps {
  children: ReactNode;
  config?: Partial<CourseConfig>;
  initialState?: Partial<EnhancedCourseState>;
  initialCourses?: Course[];
  toolsByArea?: ToolsByArea;
}

/**
 * Constants
 */
export const COURSE_AREAS: readonly CourseArea[] = ['programming', 'art', 'design', 'audio'] as const;

export const COURSE_LEVEL_NAMES: readonly CourseLevelName[] = ['Beginner', 'Intermediate', 'Advanced', 'Arcane'] as const;

export const COURSE_STATUSES: readonly CourseStatus[] = ['draft', 'published', 'archived'] as const;

/**
 * Default Configuration
 */
export const defaultCourseConfig: CourseConfig = {
  enableOptimisticUpdates: true,
  enableCaching: true,
  cacheTimeout: 300000, // 5 minutes
  autoSync: false,
  syncInterval: 60000, // 1 minute
  defaultPageSize: 12,
  maxPageSize: 100,
  enableAnalytics: true,
  persistFilters: true,
  persistViewMode: true,
};

/**
 * Default Filters
 */
export const defaultCourseFilters: EnhancedCourseFilters = {
  search: '',
  area: 'all',
  level: 'all',
  status: 'all',
  instructor: '',
  tag: '',
  tool: '',
  difficulty: 'all',
  priceRange: null,
  sortBy: 'title',
  sortOrder: 'asc',
  showFeatured: false,
  showPublicOnly: false,
};

/**
 * Default Tools by Area
 */
export const defaultToolsByArea: ToolsByArea = {
  programming: ['JavaScript', 'Python', 'React', 'Node.js', 'TypeScript'],
  art: ['Photoshop', 'Illustrator', 'Blender', 'Maya', 'Figma'],
  design: ['Figma', 'Sketch', 'Adobe XD', 'Framer', 'InVision'],
  audio: ['Pro Tools', 'Logic Pro', 'Ableton Live', 'FL Studio', 'Audacity'],
};

/**
 * Default Course State
 */
export const defaultCourseState: EnhancedCourseState = {
  courses: [],
  filteredCourses: [],
  totalCount: 0,
  selectedCourses: [],
  filters: defaultCourseFilters,
  appliedFilters: defaultCourseFilters,
  pagination: {
    page: 1,
    limit: 12,
    total: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  },
  viewMode: 'grid',
  isLoading: false,
  isLoadingMore: false,
  error: null,
  syncStatus: 'idle',
  lastSyncTime: null,
  pendingChanges: new Set(),
  optimisticUpdates: new Map(),
  lastUpdated: null,
  toolsByArea: defaultToolsByArea,
  config: defaultCourseConfig,
};
