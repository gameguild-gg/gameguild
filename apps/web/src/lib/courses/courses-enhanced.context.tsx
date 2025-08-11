'use client';

import { Program } from '@/lib/api/generated';
import React, { createContext, useContext, useEffect, useMemo, useReducer } from 'react';

interface CourseFilters {
  search: string;
  category: string;
  level: string;
  instructor: string;
  enrollment: string;
}

interface CourseState {
  courses: Program[];
  filteredCourses: Program[];
  filters: CourseFilters;
  isLoading: boolean;
  error: string | null;
  currentPage: number;
  itemsPerPage: number;
}

const initialFilters: CourseFilters = {
  search: '',
  category: 'all',
  level: 'all',
  instructor: 'all',
  enrollment: 'all',
};

const initialState: CourseState = {
  courses: [],
  filteredCourses: [],
  filters: initialFilters,
  isLoading: true,
  error: null,
  currentPage: 1,
  itemsPerPage: 12,
};

type CourseAction =
  | { type: 'SET_COURSES'; payload: Program[] }
  | { type: 'SET_FILTERS'; payload: Partial<CourseFilters> }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_PAGE'; payload: number };

function courseReducer(state: CourseState, action: CourseAction): CourseState {
  switch (action.type) {
    case 'SET_COURSES':
      return {
        ...state,
        courses: action.payload,
        isLoading: false,
      };
    case 'SET_FILTERS':
      return {
        ...state,
        filters: { ...state.filters, ...action.payload },
        currentPage: 1, // Reset to first page when filters change
      };
    case 'SET_LOADING':
      return {
        ...state,
        isLoading: action.payload,
      };
    case 'SET_ERROR':
      return {
        ...state,
        error: action.payload,
        isLoading: false,
      };
    case 'SET_PAGE':
      return {
        ...state,
        currentPage: action.payload,
      };
    default:
      return state;
  }
}

interface CourseContextType {
  state: CourseState;
  paginatedCourses: Program[];
  dispatch: React.Dispatch<CourseAction>;
  setFilters: (filters: Partial<CourseFilters>) => void;
  setPage: (page: number) => void;
}

const CourseContext = createContext<CourseContextType | undefined>(undefined);

export function CourseProvider({ children, initialCourses = [] }: { children: React.ReactNode; initialCourses?: Program[] }) {
  const [state, dispatch] = useReducer(courseReducer, initialState);

  // Load initial courses
  useEffect(() => {
    if (initialCourses.length > 0) {
      dispatch({ type: 'SET_COURSES', payload: initialCourses });
    }
  }, [initialCourses]);

  // Filter courses based on current filters
  const filteredCourses = useMemo(() => {
    let filtered = state.courses;

    if (state.filters.search) {
      const searchTerm = state.filters.search.toLowerCase();
      filtered = filtered.filter((course) =>
        course.title?.toLowerCase().includes(searchTerm) ||
        course.description?.toLowerCase().includes(searchTerm)
      );
    }

    if (state.filters.category !== 'all') {
      filtered = filtered.filter((course) => course.category?.toString() === state.filters.category);
    }

    if (state.filters.level !== 'all') {
      filtered = filtered.filter((course) => course.difficulty?.toString() === state.filters.level);
    }

    return filtered;
  }, [state.courses, state.filters]);

  // No need to update courses when filtered courses change - this was causing an infinite loop

  // Paginate filtered courses
  const paginatedCourses = useMemo(() => {
    const startIndex = (state.currentPage - 1) * state.itemsPerPage;
    const endIndex = startIndex + state.itemsPerPage;
    return filteredCourses.slice(startIndex, endIndex);
  }, [filteredCourses, state.currentPage, state.itemsPerPage]);

  const setFilters = (filters: Partial<CourseFilters>) => {
    dispatch({ type: 'SET_FILTERS', payload: filters });
  };

  const setPage = (page: number) => {
    dispatch({ type: 'SET_PAGE', payload: page });
  };

  return <CourseContext.Provider value={{ state: { ...state, filteredCourses }, paginatedCourses, dispatch, setFilters, setPage }}>{children}</CourseContext.Provider>;
}

export function useCourseContext() {
  const context = useContext(CourseContext);
  if (context === undefined) {
    throw new Error('useCourseContext must be used within a CourseProvider');
  }
  return context;
}
