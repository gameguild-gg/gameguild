'use client';

import React, { createContext, useContext, useEffect, useReducer, useCallback, ReactNode } from 'react';
import { courseReducer, initialCourseState } from '../reducers/courses.reducer';
import { CourseAction, CourseState, CourseData } from '@/components/legacy/types/courses';
import { EnhancedCourse } from '../courses-enhanced.context';
import { 
  fetchCoursesWithCache, 
  createCourseWithCache, 
  updateCourseWithCache, 
  deleteCourseWithCache 
} from '../actions/courses.actions';

// Extended state to include sync status while maintaining compatibility
export interface CourseSyncState extends CourseState {
  syncStatus: 'idle' | 'syncing' | 'synced' | 'error';
  lastSyncTime: Date | null;
  pendingChanges: Set<string>; // Track which courses have pending changes
  optimisticUpdates: Map<string, EnhancedCourse>; // Store optimistic updates
}

// Extended actions for sync operations
export type CourseSyncAction = 
  | CourseAction
  | { type: 'SET_SYNC_STATUS'; payload: 'idle' | 'syncing' | 'synced' | 'error' }
  | { type: 'SET_LAST_SYNC_TIME'; payload: Date }
  | { type: 'ADD_PENDING_CHANGE'; payload: string }
  | { type: 'REMOVE_PENDING_CHANGE'; payload: string }
  | { type: 'CLEAR_PENDING_CHANGES' }
  | { type: 'ADD_OPTIMISTIC_UPDATE'; payload: { id: string; course: EnhancedCourse } }
  | { type: 'REMOVE_OPTIMISTIC_UPDATE'; payload: string }
  | { type: 'CLEAR_OPTIMISTIC_UPDATES' }
  | { type: 'SYNC_FROM_STORAGE'; payload: EnhancedCourse[] }
  | { type: 'SET_COURSES_FROM_ENHANCED'; payload: EnhancedCourse[] };

const initialSyncState: CourseSyncState = {
  ...initialCourseState,
  syncStatus: 'idle',
  lastSyncTime: null,
  pendingChanges: new Set(),
  optimisticUpdates: new Map(),
};

function courseSyncReducer(state: CourseSyncState, action: CourseSyncAction): CourseSyncState {
  switch (action.type) {
    case 'SET_SYNC_STATUS':
      return {
        ...state,
        syncStatus: action.payload,
      };

    case 'SET_LAST_SYNC_TIME':
      return {
        ...state,
        lastSyncTime: action.payload,
      };

    case 'ADD_PENDING_CHANGE':
      return {
        ...state,
        pendingChanges: new Set([...state.pendingChanges, action.payload]),
      };

    case 'REMOVE_PENDING_CHANGE': {
      const newPendingChanges = new Set(state.pendingChanges);
      newPendingChanges.delete(action.payload);
      return {
        ...state,
        pendingChanges: newPendingChanges,
      };
    }

    case 'CLEAR_PENDING_CHANGES':
      return {
        ...state,
        pendingChanges: new Set(),
      };

    case 'ADD_OPTIMISTIC_UPDATE': {
      const newOptimisticUpdates = new Map(state.optimisticUpdates);
      newOptimisticUpdates.set(action.payload.id, action.payload.course);
      return {
        ...state,
        optimisticUpdates: newOptimisticUpdates,
      };
    }

    case 'REMOVE_OPTIMISTIC_UPDATE': {
      const newOptimisticUpdates = new Map(state.optimisticUpdates);
      newOptimisticUpdates.delete(action.payload);
      return {
        ...state,
        optimisticUpdates: newOptimisticUpdates,
      };
    }

    case 'CLEAR_OPTIMISTIC_UPDATES':
      return {
        ...state,
        optimisticUpdates: new Map(),
      };

    case 'SYNC_FROM_STORAGE':
    case 'SET_COURSES_FROM_ENHANCED': {
      // Convert EnhancedCourse[] to CourseData structure to maintain compatibility
      const courses = action.payload;
      const courseData: CourseData = {
        courses,
        toolsByArea: {
          programming: [],
          art: [],
          design: [],
          audio: []
        }
      };
      
      return {
        ...state,
        data: courseData,
        isLoading: false,
        error: null,
        syncStatus: 'synced',
        lastSyncTime: new Date(),
      };
    }

    default: {
      // Handle regular course actions through the original reducer
      const baseState: CourseState = {
        data: state.data,
        filters: state.filters,
        isLoading: state.isLoading,
        error: state.error,
      };
      
      const newBaseState = courseReducer(baseState, action as CourseAction);
      
      return {
        ...state,
        ...newBaseState,
      };
    }
  }
}

// Context
interface CourseSyncContextType {
  state: CourseSyncState;
  dispatch: (action: CourseSyncAction) => void;
  // Course management with sync
  loadCourses: () => Promise<void>;
  createCourse: (course: Omit<EnhancedCourse, 'id'>) => Promise<void>;
  updateCourse: (id: string, updates: Partial<EnhancedCourse>) => Promise<void>;
  deleteCourse: (id: string) => Promise<void>;
  // Sync operations
  syncWithServer: () => Promise<void>;
  exportData: () => Promise<EnhancedCourse[]>;
  importData: (data: EnhancedCourse[]) => Promise<void>;
  clearData: () => Promise<void>;
  // Enhanced features
  getEnhancedCourses: () => EnhancedCourse[];
}

const CourseSyncContext = createContext<CourseSyncContextType | undefined>(undefined);

// Provider component
interface CourseSyncProviderProps {
  children: ReactNode;
}

export function CourseManagementProvider({ children }: CourseSyncProviderProps) {
  const [state, dispatch] = useReducer(courseSyncReducer, initialSyncState);

  // Load courses from cache/server
  const loadCourses = useCallback(async () => {
    try {
      dispatch({ type: 'SET_LOADING', payload: true });
      dispatch({ type: 'SET_SYNC_STATUS', payload: 'syncing' });

      // Fetch from server with cache
      const serverData = await fetchCoursesWithCache();
      if (serverData) {
        dispatch({ type: 'SET_COURSES_FROM_ENHANCED', payload: serverData });
      }

    } catch (error) {
      dispatch({ type: 'SET_ERROR', payload: (error as Error).message });
      dispatch({ type: 'SET_SYNC_STATUS', payload: 'error' });
    } finally {
      dispatch({ type: 'SET_LOADING', payload: false });
    }
  }, []);

  // Create course with optimistic updates
  const createCourse = useCallback(async (course: Omit<EnhancedCourse, 'id'>) => {
    const tempId = `temp-${Date.now()}`;
    const optimisticCourse: EnhancedCourse = {
      ...course,
      id: tempId,
    };

    try {
      // Add optimistic update
      dispatch({ type: 'ADD_OPTIMISTIC_UPDATE', payload: { id: tempId, course: optimisticCourse } });
      dispatch({ type: 'ADD_PENDING_CHANGE', payload: tempId });

      // Create on server
      const createdCourse = await createCourseWithCache(course);
      
      // Remove optimistic update and add real course
      dispatch({ type: 'REMOVE_OPTIMISTIC_UPDATE', payload: tempId });
      dispatch({ type: 'REMOVE_PENDING_CHANGE', payload: tempId });
      
      // Reload courses to get updated data
      await loadCourses();

    } catch (error) {
      dispatch({ type: 'REMOVE_OPTIMISTIC_UPDATE', payload: tempId });
      dispatch({ type: 'REMOVE_PENDING_CHANGE', payload: tempId });
      dispatch({ type: 'SET_ERROR', payload: (error as Error).message });
    }
  }, [loadCourses]);

  // Update course with optimistic updates
  const updateCourse = useCallback(async (id: string, updates: Partial<EnhancedCourse>) => {
    try {
      // Get current course data for optimistic update
      const currentCourses = getEnhancedCourses();
      const currentCourse = currentCourses.find(c => c.id === id);
      
      if (currentCourse) {
        const optimisticCourse = { ...currentCourse, ...updates };
        dispatch({ type: 'ADD_OPTIMISTIC_UPDATE', payload: { id, course: optimisticCourse } });
      }
      
      dispatch({ type: 'ADD_PENDING_CHANGE', payload: id });

      // Update on server
      await updateCourseWithCache(id, updates);
      
      // Remove optimistic update
      dispatch({ type: 'REMOVE_OPTIMISTIC_UPDATE', payload: id });
      dispatch({ type: 'REMOVE_PENDING_CHANGE', payload: id });
      
      // Reload courses to get updated data
      await loadCourses();

    } catch (error) {
      dispatch({ type: 'REMOVE_OPTIMISTIC_UPDATE', payload: id });
      dispatch({ type: 'REMOVE_PENDING_CHANGE', payload: id });
      dispatch({ type: 'SET_ERROR', payload: (error as Error).message });
    }
  }, [loadCourses]);

  // Delete course
  const deleteCourse = useCallback(async (id: string) => {
    try {
      dispatch({ type: 'ADD_PENDING_CHANGE', payload: id });

      await deleteCourseWithCache(id);
      
      dispatch({ type: 'REMOVE_PENDING_CHANGE', payload: id });
      
      // Reload courses to get updated data
      await loadCourses();

    } catch (error) {
      dispatch({ type: 'REMOVE_PENDING_CHANGE', payload: id });
      dispatch({ type: 'SET_ERROR', payload: (error as Error).message });
    }
  }, [loadCourses]);

  // Sync with server
  const syncWithServer = useCallback(async () => {
    await loadCourses();
  }, [loadCourses]);

  // Get enhanced courses (combines base data with optimistic updates)
  const getEnhancedCourses = useCallback((): EnhancedCourse[] => {
    const baseCourses = state.data?.courses || [];
    const enhanced = [...baseCourses];

    // Apply optimistic updates
    state.optimisticUpdates.forEach((course, id) => {
      const existingIndex = enhanced.findIndex(c => c.id === id);
      if (existingIndex >= 0) {
        enhanced[existingIndex] = course;
      } else {
        enhanced.push(course);
      }
    });

    return enhanced;
  }, [state.data, state.optimisticUpdates]);

  // Export data
  const exportData = useCallback(async (): Promise<EnhancedCourse[]> => {
    return getEnhancedCourses();
  }, [getEnhancedCourses]);

  // Import data
  const importData = useCallback(async (data: EnhancedCourse[]) => {
    dispatch({ type: 'SET_COURSES_FROM_ENHANCED', payload: data });
  }, []);

  // Clear data
  const clearData = useCallback(async () => {
    dispatch({ type: 'SET_DATA', payload: { courses: [], toolsByArea: { programming: [], art: [], design: [], audio: [] } } });
    dispatch({ type: 'CLEAR_PENDING_CHANGES' });
    dispatch({ type: 'CLEAR_OPTIMISTIC_UPDATES' });
  }, []);

  // Auto-load courses on mount
  useEffect(() => {
    loadCourses();
  }, [loadCourses]);

  const contextValue: CourseSyncContextType = {
    state,
    dispatch,
    loadCourses,
    createCourse,
    updateCourse,
    deleteCourse,
    syncWithServer,
    exportData,
    importData,
    clearData,
    getEnhancedCourses,
  };

  return (
    <CourseSyncContext.Provider value={contextValue}>
      {children}
    </CourseSyncContext.Provider>
  );
}

// Hook to use the context
export function useCourseManagement() {
  const context = useContext(CourseSyncContext);
  if (context === undefined) {
    throw new Error('useCourseManagement must be used within a CourseManagementProvider');
  }
  return context;
}

// Keep backward compatibility
export const useCoursesSync = useCourseManagement;
