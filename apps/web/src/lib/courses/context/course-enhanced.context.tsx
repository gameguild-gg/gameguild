'use client';

import React, { createContext, ReactNode, useContext, useEffect, useReducer } from 'react';
import { CourseArea, CourseLevel } from '@/components/legacy/types/courses';

// Enhanced course types
export interface CourseAnalytics {
  enrollments: number;
  completions: number;
  averageRating: number;
  revenue: number;
}

export interface CourseContent {
  id: string;
  title: string;
  type: 'lesson' | 'quiz' | 'assignment' | 'project';
  duration: number;
  isRequired: boolean;
  order: number;
}

export interface EnhancedCourse {
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
  status?: 'draft' | 'published' | 'archived';
  enrollmentCount?: number;
  estimatedHours?: number;
}

export interface CourseFilters {
  search: string;
  area: CourseArea | 'all';
  level: CourseLevel | 'all';
  status: 'draft' | 'published' | 'archived' | 'all';
  instructor: string;
  tag: string;
  sortBy: 'title' | 'createdAt' | 'updatedAt' | 'enrollments' | 'rating';
  sortOrder: 'asc' | 'desc';
}

export interface CoursePagination {
  page: number;
  limit: number;
  total: number;
  totalPages: number;
}

export interface CourseState {
  courses: EnhancedCourse[];
  filteredCourses: EnhancedCourse[];
  selectedCourses: string[];
  filters: CourseFilters;
  pagination: CoursePagination;
  isLoading: boolean;
  error: string | null;
  lastUpdated: Date | null;
  viewMode: 'grid' | 'list';
}

export type CourseAction =
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_COURSES'; payload: EnhancedCourse[] }
  | { type: 'SET_FILTERED_COURSES'; payload: EnhancedCourse[] }
  | { type: 'ADD_COURSE'; payload: EnhancedCourse }
  | { type: 'UPDATE_COURSE'; payload: EnhancedCourse }
  | { type: 'DELETE_COURSE'; payload: string }
  | { type: 'SET_SELECTED_COURSES'; payload: string[] }
  | { type: 'TOGGLE_COURSE_SELECTION'; payload: string }
  | { type: 'CLEAR_SELECTION' }
  | { type: 'SET_SEARCH'; payload: string }
  | { type: 'SET_AREA_FILTER'; payload: CourseArea | 'all' }
  | { type: 'SET_LEVEL_FILTER'; payload: CourseLevel | 'all' }
  | { type: 'SET_STATUS_FILTER'; payload: 'draft' | 'published' | 'archived' | 'all' }
  | { type: 'SET_INSTRUCTOR_FILTER'; payload: string }
  | { type: 'SET_TAG_FILTER'; payload: string }
  | { type: 'SET_SORT'; payload: { sortBy: CourseFilters['sortBy']; sortOrder: CourseFilters['sortOrder'] } }
  | { type: 'SET_PAGINATION'; payload: Partial<CoursePagination> }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_VIEW_MODE'; payload: 'grid' | 'list' }
  | { type: 'RESET_FILTERS' }
  | { type: 'SET_LAST_UPDATED'; payload: Date };

// Initial state
export const initialCourseState: CourseState = {
  courses: [],
  filteredCourses: [],
  selectedCourses: [],
  filters: {
    search: '',
    area: 'all',
    level: 'all',
    status: 'all',
    instructor: '',
    tag: '',
    sortBy: 'title',
    sortOrder: 'asc',
  },
  pagination: {
    page: 1,
    limit: 12,
    total: 0,
    totalPages: 0,
  },
  isLoading: false,
  error: null,
  lastUpdated: null,
  viewMode: 'grid',
};

// Reducer
export function courseReducer(state: CourseState, action: CourseAction): CourseState {
  switch (action.type) {
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };

    case 'SET_COURSES':
      return {
        ...state,
        courses: action.payload,
        filteredCourses: applyFilters(action.payload, state.filters),
        lastUpdated: new Date(),
      };

    case 'SET_FILTERED_COURSES':
      return { ...state, filteredCourses: action.payload };

    case 'ADD_COURSE':
      const newCourses = [...state.courses, action.payload];
      return {
        ...state,
        courses: newCourses,
        filteredCourses: applyFilters(newCourses, state.filters),
        lastUpdated: new Date(),
      };

    case 'UPDATE_COURSE':
      const updatedCourses = state.courses.map((course) => (course.id === action.payload.id ? action.payload : course));
      return {
        ...state,
        courses: updatedCourses,
        filteredCourses: applyFilters(updatedCourses, state.filters),
        lastUpdated: new Date(),
      };

    case 'DELETE_COURSE':
      const remainingCourses = state.courses.filter((course) => course.id !== action.payload);
      return {
        ...state,
        courses: remainingCourses,
        filteredCourses: applyFilters(remainingCourses, state.filters),
        selectedCourses: state.selectedCourses.filter((id) => id !== action.payload),
        lastUpdated: new Date(),
      };

    case 'SET_SELECTED_COURSES':
      return { ...state, selectedCourses: action.payload };

    case 'TOGGLE_COURSE_SELECTION':
      const isSelected = state.selectedCourses.includes(action.payload);
      return {
        ...state,
        selectedCourses: isSelected ? state.selectedCourses.filter((id) => id !== action.payload) : [...state.selectedCourses, action.payload],
      };

    case 'CLEAR_SELECTION':
      return { ...state, selectedCourses: [] };

    case 'SET_SEARCH':
      const searchFilters = { ...state.filters, search: action.payload };
      return {
        ...state,
        filters: searchFilters,
        filteredCourses: applyFilters(state.courses, searchFilters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_AREA_FILTER':
      const areaFilters = { ...state.filters, area: action.payload };
      return {
        ...state,
        filters: areaFilters,
        filteredCourses: applyFilters(state.courses, areaFilters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_LEVEL_FILTER':
      const levelFilters = { ...state.filters, level: action.payload };
      return {
        ...state,
        filters: levelFilters,
        filteredCourses: applyFilters(state.courses, levelFilters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_STATUS_FILTER':
      const statusFilters = { ...state.filters, status: action.payload };
      return {
        ...state,
        filters: statusFilters,
        filteredCourses: applyFilters(state.courses, statusFilters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_INSTRUCTOR_FILTER':
      const instructorFilters = { ...state.filters, instructor: action.payload };
      return {
        ...state,
        filters: instructorFilters,
        filteredCourses: applyFilters(state.courses, instructorFilters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_TAG_FILTER':
      const tagFilters = { ...state.filters, tag: action.payload };
      return {
        ...state,
        filters: tagFilters,
        filteredCourses: applyFilters(state.courses, tagFilters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_SORT':
      const sortFilters = { ...state.filters, ...action.payload };
      return {
        ...state,
        filters: sortFilters,
        filteredCourses: applyFilters(state.courses, sortFilters),
      };

    case 'SET_PAGINATION':
      return {
        ...state,
        pagination: { ...state.pagination, ...action.payload },
      };

    case 'SET_ERROR':
      return { ...state, error: action.payload };

    case 'SET_VIEW_MODE':
      return { ...state, viewMode: action.payload };

    case 'RESET_FILTERS':
      return {
        ...state,
        filters: initialCourseState.filters,
        filteredCourses: applyFilters(state.courses, initialCourseState.filters),
        pagination: { ...state.pagination, page: 1 },
      };

    case 'SET_LAST_UPDATED':
      return { ...state, lastUpdated: action.payload };

    default:
      return state;
  }
}

// Helper function to apply filters
function applyFilters(courses: EnhancedCourse[], filters: CourseFilters): EnhancedCourse[] {
  let filtered = [...courses];

  // Apply search filter
  if (filters.search) {
    const searchLower = filters.search.toLowerCase();
    filtered = filtered.filter(
      (course) =>
        course.title.toLowerCase().includes(searchLower) ||
        course.description.toLowerCase().includes(searchLower) ||
        course.instructors?.some((instructor) => instructor.toLowerCase().includes(searchLower)) ||
        course.tags?.some((tag) => tag.toLowerCase().includes(searchLower)),
    );
  }

  // Apply area filter
  if (filters.area !== 'all') {
    filtered = filtered.filter((course) => course.area === filters.area);
  }

  // Apply level filter
  if (filters.level !== 'all') {
    filtered = filtered.filter((course) => course.level === filters.level);
  }

  // Apply status filter
  if (filters.status !== 'all') {
    filtered = filtered.filter((course) => course.status === filters.status);
  }

  // Apply instructor filter
  if (filters.instructor) {
    filtered = filtered.filter((course) => course.instructors?.some((instructor) => instructor.toLowerCase().includes(filters.instructor.toLowerCase())));
  }

  // Apply tag filter
  if (filters.tag) {
    filtered = filtered.filter((course) => course.tags?.some((tag) => tag.toLowerCase().includes(filters.tag.toLowerCase())));
  }

  // Apply sorting
  filtered.sort((a, b) => {
    let aValue: string | number | Date;
    let bValue: string | number | Date;

    switch (filters.sortBy) {
      case 'title':
        aValue = a.title.toLowerCase();
        bValue = b.title.toLowerCase();
        break;
      case 'createdAt':
        aValue = new Date(a.publishedAt || '');
        bValue = new Date(b.publishedAt || '');
        break;
      case 'updatedAt':
        aValue = new Date(a.updatedAt || '');
        bValue = new Date(b.updatedAt || '');
        break;
      case 'enrollments':
        aValue = a.enrollmentCount || 0;
        bValue = b.enrollmentCount || 0;
        break;
      case 'rating':
        aValue = a.analytics?.averageRating || 0;
        bValue = b.analytics?.averageRating || 0;
        break;
      default:
        aValue = a.title.toLowerCase();
        bValue = b.title.toLowerCase();
    }

    if (aValue < bValue) return filters.sortOrder === 'asc' ? -1 : 1;
    if (aValue > bValue) return filters.sortOrder === 'asc' ? 1 : -1;
    return 0;
  });

  return filtered;
}

// Context
interface CourseContextType {
  state: CourseState;
  dispatch: React.Dispatch<CourseAction>;
  // Computed values
  paginatedCourses: EnhancedCourse[];
  hasSelection: boolean;
  allSelected: boolean;
  // Helper functions
  selectAll: () => void;
  clearSelection: () => void;
  toggleSort: (column: CourseFilters['sortBy']) => void;
  setPage: (page: number) => void;
  resetFilters: () => void;
  refreshData: () => void;
  toggleViewMode: () => void;
}

const CourseContext = createContext<CourseContextType | undefined>(undefined);

// Provider props
interface CourseProviderProps {
  children: ReactNode;
  initialCourses?: EnhancedCourse[];
}

// Provider component
export function CourseProvider({ children, initialCourses = [] }: CourseProviderProps) {
  const [state, dispatch] = useReducer(courseReducer, {
    ...initialCourseState,
    courses: initialCourses,
    filteredCourses: applyFilters(initialCourses, initialCourseState.filters),
  });

  // Computed values
  const startIndex = (state.pagination.page - 1) * state.pagination.limit;
  const endIndex = startIndex + state.pagination.limit;
  const paginatedCourses = state.filteredCourses.slice(startIndex, endIndex);

  const hasSelection = state.selectedCourses.length > 0;
  const allSelected = state.selectedCourses.length === paginatedCourses.length && paginatedCourses.length > 0;

  // Helper functions
  const selectAll = () => {
    if (allSelected) {
      dispatch({ type: 'CLEAR_SELECTION' });
    } else {
      dispatch({ type: 'SET_SELECTED_COURSES', payload: paginatedCourses.map((c) => c.id) });
    }
  };

  const clearSelection = () => {
    dispatch({ type: 'CLEAR_SELECTION' });
  };

  const toggleSort = (column: CourseFilters['sortBy']) => {
    const newOrder = state.filters.sortBy === column && state.filters.sortOrder === 'asc' ? 'desc' : 'asc';
    dispatch({ type: 'SET_SORT', payload: { sortBy: column, sortOrder: newOrder } });
  };

  const setPage = (page: number) => {
    dispatch({ type: 'SET_PAGINATION', payload: { page } });
  };

  const resetFilters = () => {
    dispatch({ type: 'RESET_FILTERS' });
  };

  const refreshData = () => {
    dispatch({ type: 'SET_LAST_UPDATED', payload: new Date() });
  };

  const toggleViewMode = () => {
    dispatch({ type: 'SET_VIEW_MODE', payload: state.viewMode === 'grid' ? 'list' : 'grid' });
  };

  // Update pagination total when filtered courses change
  useEffect(() => {
    const totalPages = Math.ceil(state.filteredCourses.length / state.pagination.limit);
    dispatch({
      type: 'SET_PAGINATION',
      payload: {
        total: state.filteredCourses.length,
        totalPages,
        // Reset to page 1 if current page is beyond the new total pages
        ...(state.pagination.page > totalPages && totalPages > 0 ? { page: 1 } : {}),
      },
    });
  }, [state.filteredCourses.length, state.pagination.limit, state.pagination.page]);

  const contextValue: CourseContextType = {
    state,
    dispatch,
    paginatedCourses,
    hasSelection,
    allSelected,
    selectAll,
    clearSelection,
    toggleSort,
    setPage,
    resetFilters,
    refreshData,
    toggleViewMode,
  };

  return <CourseContext.Provider value={contextValue}>{children}</CourseContext.Provider>;
}

// Hook to use the context
export function useCourseContext(): CourseContextType {
  const context = useContext(CourseContext);
  if (context === undefined) {
    throw new Error('useCourseContext must be used within a CourseProvider');
  }
  return context;
}

// Custom hooks for specific functionality
export function useCourseFilters() {
  const { state, dispatch } = useCourseContext();

  return {
    filters: state.filters,
    setSearch: (search: string) => dispatch({ type: 'SET_SEARCH', payload: search }),
    setAreaFilter: (area: CourseArea | 'all') => dispatch({ type: 'SET_AREA_FILTER', payload: area }),
    setLevelFilter: (level: CourseLevel | 'all') => dispatch({ type: 'SET_LEVEL_FILTER', payload: level }),
    setStatusFilter: (status: 'draft' | 'published' | 'archived' | 'all') =>
      dispatch({
        type: 'SET_STATUS_FILTER',
        payload: status,
      }),
    setInstructorFilter: (instructor: string) => dispatch({ type: 'SET_INSTRUCTOR_FILTER', payload: instructor }),
    setTagFilter: (tag: string) => dispatch({ type: 'SET_TAG_FILTER', payload: tag }),
    setSort: (sortBy: CourseFilters['sortBy'], sortOrder: CourseFilters['sortOrder']) =>
      dispatch({
        type: 'SET_SORT',
        payload: { sortBy, sortOrder },
      }),
    resetFilters: () => dispatch({ type: 'RESET_FILTERS' }),
  };
}

export function useCourseSelection() {
  const { state, dispatch, hasSelection, allSelected, selectAll, clearSelection } = useCourseContext();

  return {
    selectedCourses: state.selectedCourses,
    hasSelection,
    allSelected,
    selectAll,
    clearSelection,
    toggleCourse: (id: string) => dispatch({ type: 'TOGGLE_COURSE_SELECTION', payload: id }),
    setSelected: (ids: string[]) => dispatch({ type: 'SET_SELECTED_COURSES', payload: ids }),
  };
}

export function useCoursePagination() {
  const { state, setPage, dispatch } = useCourseContext();

  return {
    pagination: state.pagination,
    setPage,
    setLimit: (limit: number) => dispatch({ type: 'SET_PAGINATION', payload: { limit, page: 1 } }),
    nextPage: () => setPage(Math.min(state.pagination.page + 1, state.pagination.totalPages)),
    prevPage: () => setPage(Math.max(state.pagination.page - 1, 1)),
    hasNextPage: state.pagination.page < state.pagination.totalPages,
    hasPrevPage: state.pagination.page > 1,
  };
}
