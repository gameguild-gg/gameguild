/**
 * Enhanced Course Context Provider
 *
 * This provider offers a modern, comprehensive course management system with:
 * - Advanced filtering and search capabilities
 * - Optimistic updates for better UX
 * - Persistent state management with localStorage
 * - Pagination and view modes (grid, list, table)
 * - Real-time sync capabilities
 * - Comprehensive error handling
 */

'use client';

import { createContext, useContext, useReducer, useEffect, useCallback, ReactNode } from 'react';
import {
  EnhancedCourseState,
  Course,
  CourseActionTypes,
  CourseViewMode,
  CourseSyncStatus,
  CourseArea,
  CourseLevelName,
  CourseStatus,
} from './course-enhanced.types';
import { enhancedCourseReducer, createInitialCourseState, persistCourseState, clearPersistedCourseState } from './course-enhanced.reducer';

// Provider props
interface EnhancedCourseProviderProps {
  children: ReactNode;
  initialState?: Partial<EnhancedCourseState>;
  onStateChange?: (state: EnhancedCourseState) => void;
}

// Context definition
interface EnhancedCourseContextType {
  // State
  state: EnhancedCourseState;

  // Basic actions
  setCourses: (courses: Course[], totalCount?: number) => void;
  addCourse: (course: Course) => void;
  updateCourse: (course: Course) => void;
  deleteCourse: (courseId: string) => void;

  // Selection actions
  setSelectedCourses: (courseIds: string[]) => void;
  toggleCourseSelection: (courseId: string) => void;
  selectAllCourses: () => void;
  clearSelection: () => void;

  // Filter actions
  setSearch: (search: string) => void;
  setAreaFilter: (area: CourseArea | 'all') => void;
  setLevelFilter: (level: CourseLevelName | 'all') => void;
  setStatusFilter: (status: CourseStatus | 'all') => void;
  setInstructorFilter: (instructor: string) => void;
  setTagFilter: (tag: string) => void;
  setSort: (sortBy: 'title' | 'createdAt' | 'updatedAt' | 'enrollments' | 'rating' | 'price' | 'difficulty', sortOrder: 'asc' | 'desc') => void;
  resetFilters: () => void;

  // Pagination actions
  setPage: (page: number) => void;
  setPageSize: (pageSize: number) => void;
  setPagination: (pagination: Partial<EnhancedCourseState['pagination']>) => void;

  // View actions
  setViewMode: (viewMode: CourseViewMode) => void;
  toggleViewMode: () => void;

  // Loading and error states
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  clearError: () => void;

  // Sync actions
  setSyncStatus: (status: CourseSyncStatus) => void;
  setLastSyncTime: (time: Date) => void;

  // Optimistic updates
  addOptimisticUpdate: (id: string, course: Course) => void;
  removeOptimisticUpdate: (id: string) => void;
  clearOptimisticUpdates: () => void;

  // Pending changes
  addPendingChange: (changeId: string) => void;
  removePendingChange: (changeId: string) => void;
  clearPendingChanges: () => void;

  // Configuration
  updateConfig: (config: Partial<EnhancedCourseState['config']>) => void;
  resetState: () => void;

  // Helper functions
  getSelectedCoursesData: () => Course[];
  getFilteredCount: () => number;
  getTotalCount: () => number;
  hasSelection: () => boolean;
  isAllSelected: () => boolean;

  // Persistence
  persistState: () => void;
  clearPersistedState: () => void;
}

const EnhancedCourseContext = createContext<EnhancedCourseContextType | undefined>(undefined);

// Provider component
export const EnhancedCourseProvider: React.FC<EnhancedCourseProviderProps> = ({ children, initialState = {}, onStateChange }) => {
  const [state, dispatch] = useReducer(enhancedCourseReducer, createInitialCourseState(initialState));

  // Persist state changes to localStorage when configured
  useEffect(() => {
    if (state.config.persistFilters) {
      persistCourseState(state);
    }

    // Call external state change handler
    onStateChange?.(state);
  }, [state, onStateChange]);

  // Basic actions
  const setCourses = useCallback((courses: Course[], totalCount?: number) => {
    dispatch({
      type: CourseActionTypes.SET_COURSES,
      payload: { courses, totalCount },
    });
  }, []);

  const addCourse = useCallback((course: Course) => {
    dispatch({
      type: CourseActionTypes.ADD_COURSE,
      payload: course,
    });
  }, []);

  const updateCourse = useCallback((course: Course) => {
    dispatch({
      type: CourseActionTypes.UPDATE_COURSE,
      payload: course,
    });
  }, []);

  const deleteCourse = useCallback((courseId: string) => {
    dispatch({
      type: CourseActionTypes.DELETE_COURSE,
      payload: courseId,
    });
  }, []);

  // Selection actions
  const setSelectedCourses = useCallback((courseIds: string[]) => {
    dispatch({
      type: CourseActionTypes.SET_SELECTED_COURSES,
      payload: courseIds,
    });
  }, []);

  const toggleCourseSelection = useCallback((courseId: string) => {
    dispatch({
      type: CourseActionTypes.TOGGLE_COURSE_SELECTION,
      payload: courseId,
    });
  }, []);

  const selectAllCourses = useCallback(() => {
    dispatch({ type: CourseActionTypes.SELECT_ALL_COURSES });
  }, []);

  const clearSelection = useCallback(() => {
    dispatch({ type: CourseActionTypes.CLEAR_SELECTION });
  }, []);

  // Filter actions
  const setSearch = useCallback((search: string) => {
    dispatch({
      type: CourseActionTypes.SET_SEARCH,
      payload: search,
    });
  }, []);

  const setAreaFilter = useCallback((area: CourseArea | 'all') => {
    dispatch({
      type: CourseActionTypes.SET_AREA_FILTER,
      payload: area,
    });
  }, []);

  const setLevelFilter = useCallback((level: CourseLevelName | 'all') => {
    dispatch({
      type: CourseActionTypes.SET_LEVEL_FILTER,
      payload: level,
    });
  }, []);

  const setStatusFilter = useCallback((status: CourseStatus | 'all') => {
    dispatch({
      type: CourseActionTypes.SET_STATUS_FILTER,
      payload: status,
    });
  }, []);

  const setInstructorFilter = useCallback((instructor: string) => {
    dispatch({
      type: CourseActionTypes.SET_INSTRUCTOR_FILTER,
      payload: instructor,
    });
  }, []);

  const setTagFilter = useCallback((tag: string) => {
    dispatch({
      type: CourseActionTypes.SET_TAG_FILTER,
      payload: tag,
    });
  }, []);

  const setSort = useCallback((sortBy: 'title' | 'createdAt' | 'updatedAt' | 'enrollments' | 'rating' | 'price' | 'difficulty', sortOrder: 'asc' | 'desc') => {
    dispatch({
      type: CourseActionTypes.SET_SORT,
      payload: { sortBy, sortOrder },
    });
  }, []);

  const resetFilters = useCallback(() => {
    dispatch({ type: CourseActionTypes.RESET_FILTERS });
  }, []);

  // Pagination actions
  const setPage = useCallback((page: number) => {
    dispatch({
      type: CourseActionTypes.SET_PAGE,
      payload: page,
    });
  }, []);

  const setPageSize = useCallback((pageSize: number) => {
    dispatch({
      type: CourseActionTypes.SET_PAGE_SIZE,
      payload: pageSize,
    });
  }, []);

  const setPagination = useCallback((pagination: Partial<EnhancedCourseState['pagination']>) => {
    dispatch({
      type: CourseActionTypes.SET_PAGINATION,
      payload: pagination,
    });
  }, []);

  // View actions
  const setViewMode = useCallback((viewMode: CourseViewMode) => {
    dispatch({
      type: CourseActionTypes.SET_VIEW_MODE,
      payload: viewMode,
    });
  }, []);

  const toggleViewMode = useCallback(() => {
    dispatch({ type: CourseActionTypes.TOGGLE_VIEW_MODE });
  }, []);

  // Loading and error states
  const setLoading = useCallback((loading: boolean) => {
    dispatch({
      type: CourseActionTypes.SET_LOADING,
      payload: loading,
    });
  }, []);

  const setError = useCallback((error: string | null) => {
    dispatch({
      type: CourseActionTypes.SET_ERROR,
      payload: error,
    });
  }, []);

  const clearError = useCallback(() => {
    dispatch({ type: CourseActionTypes.CLEAR_ERROR });
  }, []);

  // Sync actions
  const setSyncStatus = useCallback((status: CourseSyncStatus) => {
    dispatch({
      type: CourseActionTypes.SET_SYNC_STATUS,
      payload: status,
    });
  }, []);

  const setLastSyncTime = useCallback((time: Date) => {
    dispatch({
      type: CourseActionTypes.SET_LAST_SYNC_TIME,
      payload: time,
    });
  }, []);

  // Optimistic updates
  const addOptimisticUpdate = useCallback((id: string, course: Course) => {
    dispatch({
      type: CourseActionTypes.ADD_OPTIMISTIC_UPDATE,
      payload: { id, course },
    });
  }, []);

  const removeOptimisticUpdate = useCallback((id: string) => {
    dispatch({
      type: CourseActionTypes.REMOVE_OPTIMISTIC_UPDATE,
      payload: id,
    });
  }, []);

  const clearOptimisticUpdates = useCallback(() => {
    dispatch({ type: CourseActionTypes.CLEAR_OPTIMISTIC_UPDATES });
  }, []);

  // Pending changes
  const addPendingChange = useCallback((changeId: string) => {
    dispatch({
      type: CourseActionTypes.ADD_PENDING_CHANGE,
      payload: changeId,
    });
  }, []);

  const removePendingChange = useCallback((changeId: string) => {
    dispatch({
      type: CourseActionTypes.REMOVE_PENDING_CHANGE,
      payload: changeId,
    });
  }, []);

  const clearPendingChanges = useCallback(() => {
    dispatch({ type: CourseActionTypes.CLEAR_PENDING_CHANGES });
  }, []);

  // Configuration
  const updateConfig = useCallback((config: Partial<EnhancedCourseState['config']>) => {
    dispatch({
      type: CourseActionTypes.UPDATE_CONFIG,
      payload: config,
    });
  }, []);

  const resetState = useCallback(() => {
    dispatch({ type: CourseActionTypes.RESET_STATE });
  }, []);

  // Helper functions
  const getSelectedCoursesData = useCallback((): Course[] => {
    return state.courses.filter((course) => state.selectedCourses.includes(course.id));
  }, [state.courses, state.selectedCourses]);

  const getFilteredCount = useCallback((): number => {
    return state.filteredCourses.length;
  }, [state.filteredCourses.length]);

  const getTotalCount = useCallback((): number => {
    return state.totalCount;
  }, [state.totalCount]);

  const hasSelection = useCallback((): boolean => {
    return state.selectedCourses.length > 0;
  }, [state.selectedCourses.length]);

  const isAllSelected = useCallback((): boolean => {
    return (
      state.filteredCourses.length > 0 &&
      state.selectedCourses.length === state.filteredCourses.length &&
      state.filteredCourses.every((course) => state.selectedCourses.includes(course.id))
    );
  }, [state.filteredCourses, state.selectedCourses]);

  // Persistence
  const persistState = useCallback(() => {
    persistCourseState(state);
  }, [state]);

  const clearPersistedState = useCallback(() => {
    clearPersistedCourseState();
  }, []);

  const contextValue: EnhancedCourseContextType = {
    // State
    state,

    // Basic actions
    setCourses,
    addCourse,
    updateCourse,
    deleteCourse,

    // Selection actions
    setSelectedCourses,
    toggleCourseSelection,
    selectAllCourses,
    clearSelection,

    // Filter actions
    setSearch,
    setAreaFilter,
    setLevelFilter,
    setStatusFilter,
    setInstructorFilter,
    setTagFilter,
    setSort,
    resetFilters,

    // Pagination actions
    setPage,
    setPageSize,
    setPagination,

    // View actions
    setViewMode,
    toggleViewMode,

    // Loading and error states
    setLoading,
    setError,
    clearError,

    // Sync actions
    setSyncStatus,
    setLastSyncTime,

    // Optimistic updates
    addOptimisticUpdate,
    removeOptimisticUpdate,
    clearOptimisticUpdates,

    // Pending changes
    addPendingChange,
    removePendingChange,
    clearPendingChanges,

    // Configuration
    updateConfig,
    resetState,

    // Helper functions
    getSelectedCoursesData,
    getFilteredCount,
    getTotalCount,
    hasSelection,
    isAllSelected,

    // Persistence
    persistState,
    clearPersistedState,
  };

  return <EnhancedCourseContext.Provider value={contextValue}>{children}</EnhancedCourseContext.Provider>;
};

// Hook to use the enhanced course context
export const useEnhancedCourses = (): EnhancedCourseContextType => {
  const context = useContext(EnhancedCourseContext);

  if (!context) {
    throw new Error('useEnhancedCourses must be used within an EnhancedCourseProvider');
  }

  return context;
};

// Export context for advanced use cases
export { EnhancedCourseContext };

// Named exports for convenience
export type { EnhancedCourseContextType };
