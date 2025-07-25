/**
 * Enhanced Course Reducer Implementation
 */

import {
  EnhancedCourseAction,
  CourseActionTypes,
  EnhancedCourseState,
  Course,
  defaultCourseState,
  defaultCourseFilters,
  EnhancedCourseFilters,
} from './course-enhanced.types';

/**
 * Apply filters to courses
 */
export const applyFilters = (courses: Course[], filters: EnhancedCourseFilters): Course[] => {
  let filtered = [...courses];

  // Search filter
  if (filters.search.trim()) {
    const searchTerm = filters.search.toLowerCase();
    filtered = filtered.filter(
      (course) =>
        course.title.toLowerCase().includes(searchTerm) ||
        course.description.toLowerCase().includes(searchTerm) ||
        course.instructors?.some((instructor) => instructor.toLowerCase().includes(searchTerm)) ||
        course.tags?.some((tag) => tag.toLowerCase().includes(searchTerm)) ||
        course.tools.some((tool) => tool.toLowerCase().includes(searchTerm)),
    );
  }

  // Area filter
  if (filters.area !== 'all') {
    filtered = filtered.filter((course) => course.area === filters.area);
  }

  // Level filter
  if (filters.level !== 'all') {
    const levelMap: Record<string, number> = {
      Beginner: 1,
      Intermediate: 2,
      Advanced: 3,
      Arcane: 4,
    };
    const targetLevel = levelMap[filters.level];
    if (targetLevel) {
      filtered = filtered.filter((course) => course.level === targetLevel);
    }
  }

  // Status filter
  if (filters.status !== 'all') {
    filtered = filtered.filter((course) => course.status === filters.status);
  }

  // Instructor filter
  if (filters.instructor.trim()) {
    const instructorTerm = filters.instructor.toLowerCase();
    filtered = filtered.filter((course) => course.instructors?.some((instructor) => instructor.toLowerCase().includes(instructorTerm)));
  }

  // Tag filter
  if (filters.tag.trim()) {
    const tagTerm = filters.tag.toLowerCase();
    filtered = filtered.filter((course) => course.tags?.some((tag) => tag.toLowerCase().includes(tagTerm)));
  }

  // Tool filter
  if (filters.tool.trim()) {
    const toolTerm = filters.tool.toLowerCase();
    filtered = filtered.filter((course) => course.tools.some((tool) => tool.toLowerCase().includes(toolTerm)));
  }

  // Difficulty filter
  if (filters.difficulty !== 'all') {
    filtered = filtered.filter((course) => course.difficulty === filters.difficulty);
  }

  // Price range filter
  if (filters.priceRange) {
    filtered = filtered.filter((course) => {
      const price = course.price || 0;
      return price >= filters.priceRange!.min && price <= filters.priceRange!.max;
    });
  }

  // Featured filter
  if (filters.showFeatured) {
    filtered = filtered.filter((course) => course.isFeatured);
  }

  // Public only filter
  if (filters.showPublicOnly) {
    filtered = filtered.filter((course) => course.isPublic);
  }

  // Sort
  filtered.sort((a, b) => {
    const { sortBy, sortOrder } = filters;
    let aValue: string | number | Date;
    let bValue: string | number | Date;

    switch (sortBy) {
      case 'title':
        aValue = a.title.toLowerCase();
        bValue = b.title.toLowerCase();
        break;
      case 'createdAt':
        aValue = new Date(a.createdAt || 0).getTime();
        bValue = new Date(b.createdAt || 0).getTime();
        break;
      case 'updatedAt':
        aValue = new Date(a.updatedAt || 0).getTime();
        bValue = new Date(b.updatedAt || 0).getTime();
        break;
      case 'enrollments':
        aValue = a.enrollmentCount || 0;
        bValue = b.enrollmentCount || 0;
        break;
      case 'rating':
        aValue = a.analytics?.averageRating || 0;
        bValue = b.analytics?.averageRating || 0;
        break;
      case 'price':
        aValue = a.price || 0;
        bValue = b.price || 0;
        break;
      case 'difficulty':
        aValue = a.difficulty || 0;
        bValue = b.difficulty || 0;
        break;
      default:
        aValue = a.title.toLowerCase();
        bValue = b.title.toLowerCase();
    }

    if (aValue < bValue) return sortOrder === 'asc' ? -1 : 1;
    if (aValue > bValue) return sortOrder === 'asc' ? 1 : -1;
    return 0;
  });

  return filtered;
};

/**
 * Calculate pagination
 */
export const calculatePagination = (totalItems: number, currentPage: number, pageSize: number) => {
  const totalPages = Math.ceil(totalItems / pageSize);
  const hasNextPage = currentPage < totalPages;
  const hasPreviousPage = currentPage > 1;

  return {
    page: currentPage,
    limit: pageSize,
    total: totalItems,
    totalPages,
    hasNextPage,
    hasPreviousPage,
  };
};

/**
 * Enhanced course reducer function to handle state updates
 */
export const enhancedCourseReducer = (state: EnhancedCourseState, action: EnhancedCourseAction): EnhancedCourseState => {
  switch (action.type) {
    case CourseActionTypes.SET_LOADING: {
      return {
        ...state,
        isLoading: action.payload,
        error: action.payload ? null : state.error,
      };
    }

    case CourseActionTypes.SET_ERROR: {
      return {
        ...state,
        error: action.payload,
        isLoading: false,
      };
    }

    case CourseActionTypes.CLEAR_ERROR: {
      return {
        ...state,
        error: null,
      };
    }

    case CourseActionTypes.SET_COURSES: {
      const { courses, totalCount } = action.payload;
      const filteredCourses = applyFilters(courses, state.appliedFilters);
      const pagination = calculatePagination(totalCount || courses.length, state.pagination.page, state.pagination.limit);

      return {
        ...state,
        courses,
        filteredCourses,
        totalCount: totalCount || courses.length,
        pagination,
        lastUpdated: new Date(),
        isLoading: false,
        error: null,
      };
    }

    case CourseActionTypes.ADD_COURSE: {
      const newCourses = [...state.courses, action.payload];
      const filteredCourses = applyFilters(newCourses, state.appliedFilters);
      const pagination = calculatePagination(newCourses.length, state.pagination.page, state.pagination.limit);

      return {
        ...state,
        courses: newCourses,
        filteredCourses,
        totalCount: newCourses.length,
        pagination,
        lastUpdated: new Date(),
      };
    }

    case CourseActionTypes.UPDATE_COURSE: {
      const updatedCourses = state.courses.map((course) => (course.id === action.payload.id ? action.payload : course));
      const filteredCourses = applyFilters(updatedCourses, state.appliedFilters);

      return {
        ...state,
        courses: updatedCourses,
        filteredCourses,
        lastUpdated: new Date(),
      };
    }

    case CourseActionTypes.DELETE_COURSE: {
      const filteredCourses = state.courses.filter((course) => course.id !== action.payload);
      const appliedFilteredCourses = applyFilters(filteredCourses, state.appliedFilters);
      const pagination = calculatePagination(filteredCourses.length, state.pagination.page, state.pagination.limit);

      return {
        ...state,
        courses: filteredCourses,
        filteredCourses: appliedFilteredCourses,
        totalCount: filteredCourses.length,
        pagination,
        selectedCourses: state.selectedCourses.filter((id) => id !== action.payload),
        lastUpdated: new Date(),
      };
    }

    case CourseActionTypes.SET_SELECTED_COURSES: {
      return {
        ...state,
        selectedCourses: action.payload,
      };
    }

    case CourseActionTypes.TOGGLE_COURSE_SELECTION: {
      const courseId = action.payload;
      const isSelected = state.selectedCourses.includes(courseId);
      const selectedCourses = isSelected ? state.selectedCourses.filter((id) => id !== courseId) : [...state.selectedCourses, courseId];

      return {
        ...state,
        selectedCourses,
      };
    }

    case CourseActionTypes.SELECT_ALL_COURSES: {
      const allCourseIds = state.filteredCourses.map((course) => course.id);
      return {
        ...state,
        selectedCourses: allCourseIds,
      };
    }

    case CourseActionTypes.CLEAR_SELECTION: {
      return {
        ...state,
        selectedCourses: [],
      };
    }

    case CourseActionTypes.SET_SEARCH: {
      const newFilters = { ...state.filters, search: action.payload };
      const filteredCourses = applyFilters(state.courses, newFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
        pagination,
      };
    }

    case CourseActionTypes.SET_AREA_FILTER: {
      const newFilters = { ...state.filters, area: action.payload };
      const filteredCourses = applyFilters(state.courses, newFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
        pagination,
      };
    }

    case CourseActionTypes.SET_LEVEL_FILTER: {
      const newFilters = { ...state.filters, level: action.payload };
      const filteredCourses = applyFilters(state.courses, newFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
        pagination,
      };
    }

    case CourseActionTypes.SET_STATUS_FILTER: {
      const newFilters = { ...state.filters, status: action.payload };
      const filteredCourses = applyFilters(state.courses, newFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
        pagination,
      };
    }

    case CourseActionTypes.SET_INSTRUCTOR_FILTER: {
      const newFilters = { ...state.filters, instructor: action.payload };
      const filteredCourses = applyFilters(state.courses, newFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
        pagination,
      };
    }

    case CourseActionTypes.SET_TAG_FILTER: {
      const newFilters = { ...state.filters, tag: action.payload };
      const filteredCourses = applyFilters(state.courses, newFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
        pagination,
      };
    }

    case CourseActionTypes.SET_SORT: {
      const newFilters = {
        ...state.filters,
        sortBy: action.payload.sortBy,
        sortOrder: action.payload.sortOrder,
      };
      const filteredCourses = applyFilters(state.courses, newFilters);

      return {
        ...state,
        filters: newFilters,
        appliedFilters: newFilters,
        filteredCourses,
      };
    }

    case CourseActionTypes.RESET_FILTERS: {
      const filteredCourses = applyFilters(state.courses, defaultCourseFilters);
      const pagination = calculatePagination(filteredCourses.length, 1, state.pagination.limit);

      return {
        ...state,
        filters: defaultCourseFilters,
        appliedFilters: defaultCourseFilters,
        filteredCourses,
        pagination,
        selectedCourses: [],
      };
    }

    case CourseActionTypes.SET_PAGE: {
      const newPagination = { ...state.pagination, page: action.payload };
      return {
        ...state,
        pagination: newPagination,
      };
    }

    case CourseActionTypes.SET_PAGE_SIZE: {
      const pagination = calculatePagination(state.totalCount, 1, action.payload);

      return {
        ...state,
        pagination,
      };
    }

    case CourseActionTypes.SET_PAGINATION: {
      return {
        ...state,
        pagination: { ...state.pagination, ...action.payload },
      };
    }

    case CourseActionTypes.SET_VIEW_MODE: {
      return {
        ...state,
        viewMode: action.payload,
      };
    }

    case CourseActionTypes.TOGGLE_VIEW_MODE: {
      const nextMode = state.viewMode === 'grid' ? 'list' : state.viewMode === 'list' ? 'table' : 'grid';
      return {
        ...state,
        viewMode: nextMode,
      };
    }

    case CourseActionTypes.SET_SYNC_STATUS: {
      return {
        ...state,
        syncStatus: action.payload,
      };
    }

    case CourseActionTypes.SET_LAST_SYNC_TIME: {
      return {
        ...state,
        lastSyncTime: action.payload,
      };
    }

    case CourseActionTypes.ADD_PENDING_CHANGE: {
      const newPendingChanges = new Set(state.pendingChanges);
      newPendingChanges.add(action.payload);
      return {
        ...state,
        pendingChanges: newPendingChanges,
      };
    }

    case CourseActionTypes.REMOVE_PENDING_CHANGE: {
      const newPendingChanges = new Set(state.pendingChanges);
      newPendingChanges.delete(action.payload);
      return {
        ...state,
        pendingChanges: newPendingChanges,
      };
    }

    case CourseActionTypes.CLEAR_PENDING_CHANGES: {
      return {
        ...state,
        pendingChanges: new Set(),
      };
    }

    case CourseActionTypes.ADD_OPTIMISTIC_UPDATE: {
      const newOptimisticUpdates = new Map(state.optimisticUpdates);
      newOptimisticUpdates.set(action.payload.id, action.payload.course);
      return {
        ...state,
        optimisticUpdates: newOptimisticUpdates,
      };
    }

    case CourseActionTypes.REMOVE_OPTIMISTIC_UPDATE: {
      const newOptimisticUpdates = new Map(state.optimisticUpdates);
      newOptimisticUpdates.delete(action.payload);
      return {
        ...state,
        optimisticUpdates: newOptimisticUpdates,
      };
    }

    case CourseActionTypes.CLEAR_OPTIMISTIC_UPDATES: {
      return {
        ...state,
        optimisticUpdates: new Map(),
      };
    }

    case CourseActionTypes.UPDATE_CONFIG: {
      return {
        ...state,
        config: { ...state.config, ...action.payload },
      };
    }

    case CourseActionTypes.RESET_STATE: {
      return {
        ...defaultCourseState,
        config: state.config, // Preserve config on reset
        toolsByArea: state.toolsByArea, // Preserve tools
      };
    }

    default: {
      console.warn(`Unhandled course action type: ${(action as { type: string }).type}`);
      return state;
    }
  }
};

/**
 * Initialize course state with potential localStorage values
 */
export const createInitialCourseState = (initialState: Partial<EnhancedCourseState>): EnhancedCourseState => {
  let enhancedState = { ...defaultCourseState, ...initialState };

  // Check for persisted configuration in localStorage (client-side only)
  if (typeof window !== 'undefined' && enhancedState.config.persistFilters) {
    try {
      const persistedState = localStorage.getItem('course-state');
      if (persistedState) {
        const parsed = JSON.parse(persistedState);

        // Only restore certain safe properties
        if (parsed.filters && parsed.viewMode) {
          enhancedState = {
            ...enhancedState,
            filters: {
              ...enhancedState.filters,
              ...parsed.filters,
            },
            appliedFilters: {
              ...enhancedState.appliedFilters,
              ...parsed.filters,
            },
            viewMode: parsed.viewMode,
            config: {
              ...enhancedState.config,
              ...parsed.config,
            },
          };
        }
      }
    } catch (error) {
      console.warn('Failed to load course configuration from localStorage:', error);
    }
  }

  return enhancedState;
};

/**
 * Helper function to save course state to localStorage
 */
export const persistCourseState = (state: EnhancedCourseState): void => {
  if (typeof window !== 'undefined' && state.config.persistFilters) {
    try {
      const stateToSave = {
        filters: state.filters,
        viewMode: state.viewMode,
        config: state.config,
      };
      localStorage.setItem('course-state', JSON.stringify(stateToSave));
    } catch (error) {
      console.warn('Failed to persist course state to localStorage:', error);
    }
  }
};

/**
 * Helper function to clear persisted course state
 */
export const clearPersistedCourseState = (): void => {
  if (typeof window !== 'undefined') {
    try {
      localStorage.removeItem('course-state');
    } catch (error) {
      console.warn('Failed to clear persisted course state:', error);
    }
  }
};
