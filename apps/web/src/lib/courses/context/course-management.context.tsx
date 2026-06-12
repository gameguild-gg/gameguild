'use client';

import { courseService } from '@/lib/courses/services/course.service';
import { createContext, ReactNode, useCallback, useContext, useMemo, useState } from 'react';

interface Course {
  id: string;
  title: string;
  area: string;
  level: number;
  slug: string;
  image?: string;
  description?: string;
}

interface CoursesSyncState {
  isLoading: boolean;
  error: string | null;
  syncStatus: 'idle' | 'syncing' | 'synced' | 'error';
  lastSync: Date | null;
  lastSyncTime: Date | null;
  pendingChanges: Map<string, unknown>;
}

interface CoursesSyncContextType {
  state: CoursesSyncState;
  getEnhancedCourses: () => Course[];
  syncWithServer: () => Promise<void>;
}

type CourseSource = Awaited<ReturnType<typeof courseService.getCourses>>[number];

const initialState: CoursesSyncState = {
  isLoading: false,
  error: null,
  syncStatus: 'idle',
  lastSync: null,
  lastSyncTime: null,
  pendingChanges: new Map(),
};

const CoursesSyncContext = createContext<CoursesSyncContextType | undefined>(undefined);

function mapDifficultyToLevel(difficulty: unknown): number {
  if (typeof difficulty === 'number') {
    if (difficulty <= 0) return 1;
    if (difficulty === 1) return 2;
    return 3;
  }

  const normalized = typeof difficulty === 'string' ? difficulty.trim().toLowerCase() : '';
  if (normalized === 'intermediate') return 2;
  if (normalized === 'advanced' || normalized === 'expert') return 3;
  return 1;
}

function mapCourse(course: CourseSource, index: number): Course {
  const id = course.id == null ? `course-${index + 1}` : String(course.id);
  const slug = typeof course.slug === 'string' && course.slug.length > 0 ? course.slug : id;

  return {
    id,
    title: typeof course.title === 'string' && course.title.length > 0 ? course.title : `Course ${index + 1}`,
    area: typeof course.category === 'string' && course.category.length > 0 ? course.category : 'General',
    level: mapDifficultyToLevel(course.difficulty),
    slug,
    image: typeof course.thumbnail === 'string' && course.thumbnail.length > 0 ? course.thumbnail : undefined,
    description: typeof course.description === 'string' ? course.description : undefined,
  };
}

export function CoursesSyncProvider({
  children,
  initialCourses = [],
}: {
  children: ReactNode;
  initialCourses?: CourseSource[];
}) {
  const [state, setState] = useState<CoursesSyncState>(initialState);
  const [courses, setCourses] = useState<Course[]>(() => initialCourses.map(mapCourse));

  const syncWithServer = useCallback(async () => {
    setState((current) => ({
      ...current,
      isLoading: true,
      error: null,
      syncStatus: 'syncing',
    }));

    try {
      const liveCourses = await courseService.getCourses();
      const syncedAt = new Date();
      setCourses(liveCourses.map(mapCourse));
      setState((current) => ({
        ...current,
        isLoading: false,
        error: null,
        syncStatus: 'synced',
        lastSync: syncedAt,
        lastSyncTime: syncedAt,
      }));
    } catch (error) {
      setState((current) => ({
        ...current,
        isLoading: false,
        error: error instanceof Error ? error.message : 'Unable to sync courses.',
        syncStatus: 'error',
      }));
    }
  }, []);

  const value = useMemo<CoursesSyncContextType>(
    () => ({
      state,
      getEnhancedCourses: () => courses,
      syncWithServer,
    }),
    [courses, state, syncWithServer],
  );

  return <CoursesSyncContext.Provider value={value}>{children}</CoursesSyncContext.Provider>;
}

export function useCoursesSync(): CoursesSyncContextType {
  const context = useContext(CoursesSyncContext);
  if (!context) {
    throw new Error('useCoursesSync must be used within a CoursesSyncProvider.');
  }

  return context;
}
