'use client';

import React, { createContext, useContext, useReducer, useEffect, useMemo } from 'react';
import { CourseState, CourseFilters, EnhancedCourse } from './types';

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
  | { type: 'SET_COURSES'; payload: EnhancedCourse[] }
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
  paginatedCourses: EnhancedCourse[];
  dispatch: React.Dispatch<CourseAction>;
  setFilters: (filters: Partial<CourseFilters>) => void;
  setPage: (page: number) => void;
}

const CourseContext = createContext<CourseContextType | undefined>(undefined);

export type { EnhancedCourse } from './types';

export function CourseProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(courseReducer, initialState);

  // Filter courses based on current filters
  const filteredCourses = useMemo(() => {
    let filtered = state.courses;

    if (state.filters.search) {
      const searchTerm = state.filters.search.toLowerCase();
      filtered = filtered.filter((course) => course.title.toLowerCase().includes(searchTerm) || course.description.toLowerCase().includes(searchTerm) || course.area.toLowerCase().includes(searchTerm));
    }

    if (state.filters.category !== 'all') {
      filtered = filtered.filter((course) => course.area === state.filters.category);
    }

    if (state.filters.level !== 'all') {
      filtered = filtered.filter((course) => course.level.toString() === state.filters.level);
    }

    return filtered;
  }, [state.courses, state.filters]);

  // Update filtered courses when they change
  useEffect(() => {
    dispatch({ type: 'SET_COURSES', payload: state.courses });
  }, [filteredCourses, state.courses]);

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

  // Load mock courses data
  useEffect(() => {
    const loadCourses = async () => {
      dispatch({ type: 'SET_LOADING', payload: true });
      try {
        // Mock courses data - replace with actual API call
        const mockCourses: EnhancedCourse[] = [
          {
            id: 1,
            title: 'Introduction to Game Development',
            description: 'Learn the fundamentals of game development using modern tools and techniques.',
            area: 'Programming',
            level: 1,
            estimatedHours: 40,
            enrollmentCount: 150,
            analytics: { averageRating: 4.5 },
            image: '/placeholder-course.jpg',
            slug: 'introduction-to-game-development',
            instructors: ['John Doe'],
            progress: 0,
          },
          {
            id: 2,
            title: 'Advanced 3D Modeling',
            description: 'Master advanced 3D modeling techniques for games and animations.',
            area: 'Art',
            level: 3,
            estimatedHours: 60,
            enrollmentCount: 85,
            analytics: { averageRating: 4.8 },
            image: '/placeholder-course.jpg',
            slug: 'advanced-3d-modeling',
            instructors: ['Jane Smith'],
            progress: 0,
          },
          {
            id: 3,
            title: 'UI/UX Design for Games',
            description: 'Design compelling user interfaces and experiences for games.',
            area: 'Design',
            level: 2,
            estimatedHours: 35,
            enrollmentCount: 120,
            analytics: { averageRating: 4.3 },
            image: '/placeholder-course.jpg',
            slug: 'ui-ux-design-for-games',
            instructors: ['Alex Johnson'],
            progress: 0,
          },
        ];

        setTimeout(() => {
          dispatch({ type: 'SET_COURSES', payload: mockCourses });
        }, 1000); // Simulate loading time
      } catch {
        dispatch({ type: 'SET_ERROR', payload: 'Failed to load courses' });
      }
    };

    loadCourses();
  }, []);

  return <CourseContext.Provider value={{ state: { ...state, filteredCourses }, paginatedCourses, dispatch, setFilters, setPage }}>{children}</CourseContext.Provider>;
}

export function useCourseContext() {
  const context = useContext(CourseContext);
  if (context === undefined) {
    throw new Error('useCourseContext must be used within a CourseProvider');
  }
  return context;
}
