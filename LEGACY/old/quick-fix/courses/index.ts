/**
 * Enhanced Course Provider - Main Export File
 *
 * This module provides a comprehensive course management system with:
 * - Modern React useReducer pattern for state management
 * - Advanced filtering and search capabilities
 * - Optimistic updates for better UX
 * - Persistent state management with localStorage
 * - Pagination and multiple view modes
 * - Real-time sync capabilities
 * - Comprehensive error handling
 */

// Enhanced Course Provider exports (NEW - RECOMMENDED)
export { EnhancedCourseProvider, useEnhancedCourses, EnhancedCourseContext } from './course-enhanced.context';
export type { EnhancedCourseContextType } from './course-enhanced.context';

// State management exports
export {
  enhancedCourseReducer,
  createInitialCourseState,
  persistCourseState,
  clearPersistedCourseState,
  applyFilters,
  calculatePagination,
} from './course-enhanced.reducer';

// Enhanced Type exports
export type {
  Course,
  EnhancedCourseState,
  EnhancedCourseAction,
  EnhancedCourseFilters,
  EnhancedCoursePagination,
  CourseConfig,
  CourseAnalytics,
  CourseContent,
  ToolsByArea,
  CourseData,
  CourseArea,
  CourseLevel,
  CourseLevelName,
  CourseStatus,
  CourseViewMode,
  CourseSyncStatus,
  EnhancedCourseReducer,
  EnhancedCourseContextValue,
  EnhancedCourseProviderProps,
} from './course-enhanced.types';

// Enhanced Constants
export {
  CourseActionTypes,
  COURSE_AREAS,
  COURSE_LEVEL_NAMES,
  COURSE_STATUSES,
  defaultCourseFilters,
  defaultCourseConfig,
  defaultCourseState,
  defaultToolsByArea,
} from './course-enhanced.types';

// Import types for local use
import type { CourseConfig, Course, EnhancedCourseState } from './course-enhanced.types';
import { EnhancedCourseProvider, useEnhancedCourses } from './course-enhanced.context';

// Legacy exports (DEPRECATED - for backward compatibility)
// Note: Some legacy exports may conflict with enhanced exports
export * from './utils';
export { CourseProvider, useCourseContext, useCourseFilters, useCourseSelection, useCoursePagination } from './courses-enhanced.context';

// Selective legacy exports to avoid conflicts
export type { Course } from './courses.types';

// Helper functions for easy setup
export const createBasicCourseConfig = (overrides?: Partial<CourseConfig>) => ({
  persistFilters: true,
  persistViewMode: true,
  autoSync: false,
  syncInterval: 30000,
  optimisticUpdates: true,
  debounceSearch: 300,
  defaultPageSize: 12,
  maxPageSize: 100,
  enableSelection: true,
  enableBulkOperations: true,
  enableOptimisticUpdates: true,
  enablePersistence: true,
  ...overrides,
});

// Quick start function for basic setup
export const createEnhancedCourseProvider = (config?: Partial<CourseConfig>, initialCourses?: Course[]) => {
  const initialState: Partial<EnhancedCourseState> = {
    config: createBasicCourseConfig(config),
    courses: initialCourses || [],
    filteredCourses: initialCourses || [],
    totalCount: initialCourses?.length || 0,
  };

  return {
    initialState,
    EnhancedCourseProvider,
    useEnhancedCourses,
  };
};
