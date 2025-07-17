'use client';

import React, { createContext, useContext, useReducer, ReactNode, useEffect } from 'react';
import { CourseState, CourseAction, CourseData } from '@/components/legacy/types/courses';
import { courseReducer, initialCourseState } from '@/lib/courses/reducers/courses.reducer';

interface CourseContextType {
  state: CourseState;
  dispatch: React.Dispatch<CourseAction>;
}

const CourseContext = createContext<CourseContextType | undefined>(undefined);

interface CourseProviderProps {
  children: ReactNode;
  initialData?: CourseData;
}

export function CourseProvider({ children, initialData }: CourseProviderProps) {
  const [state, dispatch] = useReducer(courseReducer, {
    ...initialCourseState,
    data: initialData || null,
  });

  // Set initial data if provided
  useEffect(() => {
    if (initialData && !state.data) {
      dispatch({ type: 'SET_DATA', payload: initialData });
    }
  }, [initialData, state.data]);

  return <CourseContext.Provider value={{ state, dispatch }}>{children}</CourseContext.Provider>;
}

export function useCourseContext(): CourseContextType {
  const context = useContext(CourseContext);
  if (context === undefined) {
    throw new Error('useCourseContext must be used within a CourseProvider');
  }
  return context;
}
