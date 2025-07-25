'use client';

import React, { createContext, useCallback, useContext, useEffect, useReducer } from 'react';
import {
  CourseChapter,
  CourseEditorActionType,
  CourseEditorConfig,
  CourseEditorContextValue,
  CourseEditorMode,
  CourseEditorState,
  CourseEditorTab,
  CourseLesson,
  CoursePreviewMode,
  CourseValidationError,
  defaultCourseEditorState,
  EnhancedCourseContent,
} from '../types';
import { Course } from '@/lib/courses';
import { courseEditorReducer, createEmptyCourse, createInitialCourseEditorState, validateCourse } from './course-editor-reducer';
import { getCourseBySlug, publishCourse as publishCourseAction, saveCourse as saveCourseAction } from '../actions';

/**
 * Course Editor Context
 */
const CourseEditorContext = createContext<CourseEditorContextValue | undefined>(undefined);

interface CourseEditorProviderProps {
  children: React.ReactNode;
  config?: Partial<CourseEditorConfig>;
  course?: Course | null;
  onSave?: (course: Course) => Promise<void>;
  onAutoSave?: (course: Course) => Promise<void>;
  onValidate?: (course: Course) => CourseValidationError[];
}

export const CourseEditorProvider = ({ children, course = null, ...props }: CourseEditorProviderProps) => {
  const { config = {}, course = course || createEmptyCourse(), onSave, onAutoSave, onValidate } = props;

  const [state, dispatch] = useReducer(
    courseEditorReducer,
    createInitialCourseEditorState({
      course: initialCourse,
      originalCourse: initialCourse,
      config: {
        ...defaultCourseEditorState.config,
        ...config,
      },
    }),
  );
  //
  // // // Auto-save timer ref
  // // const autoSaveTimerRef = useRef<NodeJS.Timeout | null>(null);
  // // const lastAutoSaveRef = useRef<string | null>(null);
  //
  // // /**
  // //  * Clear auto-save timer
  // //  */
  // // const clearAutoSaveTimer = useCallback(() => {
  // //   if (autoSaveTimerRef.current) {
  // //     clearTimeout(autoSaveTimerRef.current);
  // //     autoSaveTimerRef.current = null;
  // //   }
  // // }, []);
  //
  // /**
  //  * Schedule auto-save
  //  */
  // const scheduleAutoSave = useCallback(() => {
  //   if (!state.config.autoSave || !state.course || !onAutoSave) return;
  //
  //   clearAutoSaveTimer();
  //
  //   autoSaveTimerRef.current = setTimeout(async () => {
  //     const courseJson = JSON.stringify(state.course);
  //
  //     // Only auto-save if course has changed since last auto-save
  //     if (courseJson !== lastAutoSaveRef.current && state.hasUnsavedChanges) {
  //       try {
  //         dispatch({ type: CourseEditorActionType.SET_SAVE_STATUS, payload: 'saving' });
  //         await onAutoSave(state.course!);
  //         dispatch({ type: CourseEditorActionType.SET_LAST_SAVED, payload: new Date() });
  //         lastAutoSaveRef.current = courseJson;
  //       } catch (error) {
  //         dispatch({
  //           type: CourseEditorActionType.SET_ERROR,
  //           payload: error instanceof Error ? error.message : 'Auto-save failed',
  //         });
  //         dispatch({ type: CourseEditorActionType.SET_SAVE_STATUS, payload: 'error' });
  //       }
  //     }
  //   }, state.config.autoSaveInterval);
  // }, [state.config.autoSave, state.config.autoSaveInterval, state.course, state.hasUnsavedChanges, onAutoSave]);
  //
  // /**
  //  * Set loading state
  //  */
  // const setLoading = useCallback((loading: boolean) => {
  //   dispatch({ type: CourseEditorActionType.SET_LOADING, payload: loading });
  // }, []);
  //
  // /**
  //  * Set error state
  //  */
  // const setError = useCallback((error: string | null) => {
  //   if (error) {
  //     dispatch({ type: CourseEditorActionType.SET_ERROR, payload: error });
  //   } else {
  //     dispatch({ type: CourseEditorActionType.CLEAR_ERROR });
  //   }
  // }, []);
  //
  // /**
  //  * Load course into editor
  //  */
  // const loadCourse = useCallback((course: Course) => {
  //   dispatch({ type: CourseEditorActionType.SET_COURSE, payload: course });
  // }, []);
  //
  // /**
  //  * Clear current course
  //  */
  // const clearCourse = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.CLEAR_COURSE });
  //   clearAutoSaveTimer();
  // }, [clearAutoSaveTimer]);
  //
  // /**
  //  * Create new course
  //  */
  // const createNewCourse = useCallback(() => {
  //   const newCourse = createEmptyCourse();
  //   dispatch({ type: CourseEditorActionType.SET_COURSE, payload: newCourse });
  //   dispatch({ type: CourseEditorActionType.SET_EDITOR_MODE, payload: 'create' });
  // }, []);
  //
  // /**
  //  * Update course field
  //  */
  // const updateCourseField = useCallback((field: keyof Course, value: unknown) => {
  //   dispatch({
  //     type: CourseEditorActionType.UPDATE_COURSE_FIELD,
  //     payload: { field, value },
  //   });
  // }, []);
  //
  // /**
  //  * Update course content
  //  */
  // const updateCourseContent = useCallback((updates: Partial<Course['content']>) => {
  //   dispatch({ type: CourseEditorActionType.UPDATE_COURSE_CONTENT, payload: updates });
  // }, []);
  //
  // /**
  //  * Update course metadata
  //  */
  // const updateCourseMetadata = useCallback((metadata: Partial<Course>) => {
  //   dispatch({ type: CourseEditorActionType.UPDATE_COURSE_METADATA, payload: metadata });
  // }, []);
  //
  // /**
  //  * Add new chapter
  //  */
  // const addChapter = useCallback((chapter: Omit<CourseChapter, 'id' | 'order' | 'lessons' | 'createdAt' | 'updatedAt'>) => {
  //   dispatch({ type: CourseEditorActionType.ADD_CHAPTER, payload: chapter });
  // }, []);
  //
  // /**
  //  * Update chapter
  //  */
  // const updateChapter = useCallback((chapterId: string, chapter: Partial<CourseChapter>) => {
  //   dispatch({
  //     type: CourseEditorActionType.UPDATE_CHAPTER,
  //     payload: { chapterId, chapter },
  //   });
  // }, []);
  //
  // /**
  //  * Delete chapter
  //  */
  // const deleteChapter = useCallback((chapterId: string) => {
  //   dispatch({ type: CourseEditorActionType.DELETE_CHAPTER, payload: chapterId });
  // }, []);
  //
  // /**
  //  * Reorder chapters
  //  */
  // const reorderChapters = useCallback((chapterIds: string[]) => {
  //   dispatch({ type: CourseEditorActionType.REORDER_CHAPTERS, payload: chapterIds });
  // }, []);
  //
  // /**
  //  * Add new lesson to chapter
  //  */
  // const addLesson = useCallback((chapterId: string, lesson: Omit<CourseLesson, 'id' | 'order' | 'createdAt' | 'updatedAt'>) => {
  //   dispatch({
  //     type: CourseEditorActionType.ADD_LESSON,
  //     payload: { chapterId, lesson },
  //   });
  // }, []);
  //
  // /**
  //  * Update lesson
  //  */
  // const updateLesson = useCallback((chapterId: string, lessonId: string, lesson: Partial<CourseLesson>) => {
  //   dispatch({
  //     type: CourseEditorActionType.UPDATE_LESSON,
  //     payload: { chapterId, lessonId, lesson },
  //   });
  // }, []);
  //
  // /**
  //  * Delete lesson
  //  */
  // const deleteLesson = useCallback((chapterId: string, lessonId: string) => {
  //   dispatch({
  //     type: CourseEditorActionType.DELETE_LESSON,
  //     payload: { chapterId, lessonId },
  //   });
  // }, []);
  //
  // /**
  //  * Reorder lessons within chapter
  //  */
  // const reorderLessons = useCallback((chapterId: string, lessonIds: string[]) => {
  //   dispatch({
  //     type: CourseEditorActionType.REORDER_LESSONS,
  //     payload: { chapterId, lessonIds },
  //   });
  // }, []);
  //
  // /**
  //  * Save course
  //  */
  // const saveCourse = useCallback(async () => {
  //   if (!state.course || !onSave) return;
  //
  //   try {
  //     dispatch({ type: CourseEditorActionType.SET_SAVING, payload: true });
  //     await onSave(state.course);
  //     dispatch({ type: CourseEditorActionType.SET_LAST_SAVED, payload: new Date() });
  //     lastAutoSaveRef.current = JSON.stringify(state.course);
  //   } catch (error) {
  //     dispatch({
  //       type: CourseEditorActionType.SET_ERROR,
  //       payload: error instanceof Error ? error.message : 'Save failed',
  //     });
  //   } finally {
  //     dispatch({ type: CourseEditorActionType.SET_SAVING, payload: false });
  //   }
  // }, [state.course, onSave]);
  //
  // /**
  //  * Validate course
  //  */
  // const validateCourseData = useCallback(() => {
  //   if (!state.course) return;
  //
  //   const errors = onValidate ? onValidate(state.course) : validateCourse(state.course);
  //   dispatch({ type: CourseEditorActionType.SET_VALIDATION_ERRORS, payload: errors });
  // }, [state.course, onValidate]);
  //
  // /**
  //  * Clear validation errors
  //  */
  // const clearValidationErrors = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.CLEAR_VALIDATION_ERRORS });
  // }, []);
  //
  // /**
  //  * Set editor mode
  //  */
  // const setEditorMode = useCallback((mode: 'create' | 'edit') => {
  //   dispatch({ type: CourseEditorActionType.SET_EDITOR_MODE, payload: mode });
  // }, []);
  //
  // /**
  //  * Set active tab
  //  */
  // const setActiveTab = useCallback((tab: string) => {
  //   dispatch({ type: CourseEditorActionType.SET_ACTIVE_TAB, payload: tab });
  // }, []);
  //
  // /**
  //  * Set preview mode
  //  */
  // const setPreviewMode = useCallback((enabled: boolean) => {
  //   dispatch({ type: CourseEditorActionType.SET_PREVIEW_MODE, payload: enabled });
  // }, []);
  //
  // /**
  //  * Undo last action
  //  */
  // const undo = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.UNDO });
  // }, []);
  //
  // /**
  //  * Redo last undone action
  //  */
  // const redo = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.REDO });
  // }, []);
  //
  // /**
  //  * Clear history
  //  */
  // const clearHistory = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.CLEAR_HISTORY });
  // }, []);
  //
  // /**
  //  * Enable auto-save
  //  */
  // const enableAutoSave = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.ENABLE_AUTO_SAVE });
  // }, []);
  //
  // /**
  //  * Disable auto-save
  //  */
  // const disableAutoSave = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.DISABLE_AUTO_SAVE });
  //   clearAutoSaveTimer();
  // }, [clearAutoSaveTimer]);
  //
  // /**
  //  * Set auto-save interval
  //  */
  // const setAutoSaveInterval = useCallback((interval: number) => {
  //   dispatch({ type: CourseEditorActionType.SET_AUTO_SAVE_INTERVAL, payload: interval });
  // }, []);
  //
  // /**
  //  * Reset editor to initial state
  //  */
  // const resetEditor = useCallback(() => {
  //   dispatch({ type: CourseEditorActionType.RESET_EDITOR });
  //   clearAutoSaveTimer();
  // }, [clearAutoSaveTimer]);
  //
  // // Schedule auto-save when course changes
  // useEffect(() => {
  //   if (state.hasUnsavedChanges) {
  //     scheduleAutoSave();
  //   }
  // }, [state.hasUnsavedChanges, scheduleAutoSave]);
  //
  // // Validate course when it changes
  // useEffect(() => {
  //   if (state.course) {
  //     validateCourseData();
  //   }
  // }, [state.course, validateCourseData]);
  //
  // // Cleanup auto-save timer on unmount
  // useEffect(() => {
  //   return () => {
  //     clearAutoSaveTimer();
  //   };
  // }, [clearAutoSaveTimer]);

  // // Context value
  // const contextValue: CourseEditorContextValue = {
  //   // State (as required by interface)
  //   state,
  //
  //   // Course operations (matching interface signatures)
  //   loadCourse: async (courseId: string) => {
  //     setLoading(true);
  //     try {
  //       // TODO: Replace with actual API call to load course by ID
  //       const course = await getCourseBySlug(courseId); // Using getCourseBySlug as placeholder
  //       if (course) {
  //         dispatch({ type: CourseEditorActionType.SET_COURSE, payload: course });
  //       } else {
  //         setError('Course not found');
  //       }
  //     } catch (error) {
  //       setError(error instanceof Error ? error.message : 'Failed to load course');
  //     } finally {
  //       setLoading(false);
  //     }
  //   },
  //
  //   loadCourseBySlug: async (slug: string) => {
  //     setLoading(true);
  //     try {
  //       const course = await getCourseBySlug(slug);
  //       if (course) {
  //         dispatch({ type: CourseEditorActionType.SET_COURSE, payload: course });
  //       } else {
  //         setError('Course not found');
  //       }
  //     } catch (error) {
  //       setError(error instanceof Error ? error.message : 'Failed to load course');
  //     } finally {
  //       setLoading(false);
  //     }
  //   },
  //
  //   createNewCourse,
  //
  //   saveCourse: async (): Promise<boolean> => {
  //     if (!state.course) return false;
  //
  //     try {
  //       dispatch({ type: CourseEditorActionType.SET_SAVING, payload: true });
  //       const result = await saveCourseAction(state.course);
  //       if (result) {
  //         dispatch({ type: CourseEditorActionType.SET_LAST_SAVED, payload: new Date() });
  //         lastAutoSaveRef.current = JSON.stringify(state.course);
  //       }
  //       return result;
  //     } catch (error) {
  //       dispatch({
  //         type: CourseEditorActionType.SET_ERROR,
  //         payload: error instanceof Error ? error.message : 'Save failed',
  //       });
  //       return false;
  //     } finally {
  //       dispatch({ type: CourseEditorActionType.SET_SAVING, payload: false });
  //     }
  //   },
  //
  //   publishCourse: async (): Promise<boolean> => {
  //     if (!state.course) return false;
  //
  //     try {
  //       setLoading(true);
  //       const result = await publishCourseAction(state.course.id);
  //       if (result) {
  //         updateCourseField('status', 'published');
  //       }
  //       return result;
  //     } catch (error) {
  //       setError(error instanceof Error ? error.message : 'Publish failed');
  //       return false;
  //     } finally {
  //       setLoading(false);
  //     }
  //   },
  //
  //   resetCourse: clearCourse,
  //
  //   // Course editing (match interface)
  //   updateCourseField,
  //   updateCourseContent: (content: Partial<EnhancedCourseContent>) => {
  //     dispatch({ type: CourseEditorActionType.UPDATE_COURSE_CONTENT, payload: content });
  //   },
  //   updateCourseMetadata,
  //
  //   // Content management (match interface)
  //   addChapter: (chapter: Omit<CourseChapter, 'id' | 'order'>) => {
  //     dispatch({ type: CourseEditorActionType.ADD_CHAPTER, payload: chapter as CourseChapter });
  //   },
  //   updateChapter,
  //   deleteChapter,
  //   reorderChapters,
  //
  //   addLesson: (chapterId: string, lesson: Omit<CourseLesson, 'id' | 'order'>) => {
  //     dispatch({
  //       type: CourseEditorActionType.ADD_LESSON,
  //       payload: { chapterId, lesson: lesson as CourseLesson },
  //     });
  //   },
  //   updateLesson,
  //   deleteLesson,
  //   reorderLessons,
  //
  //   // Validation (match interface)
  //   validateCourse: (): boolean => {
  //     if (!state.course) return false;
  //     const errors = onValidate ? onValidate(state.course) : validateCourse(state.course);
  //     dispatch({ type: CourseEditorActionType.SET_VALIDATION_ERRORS, payload: errors });
  //     return errors.length === 0;
  //   },
  //   clearValidationErrors,
  //
  //   // Editor state (match interface)
  //   setEditorMode: (mode: CourseEditorMode) => {
  //     dispatch({ type: CourseEditorActionType.SET_EDITOR_MODE, payload: mode });
  //   },
  //   setActiveTab: (tab: CourseEditorTab) => {
  //     dispatch({ type: CourseEditorActionType.SET_ACTIVE_TAB, payload: tab });
  //   },
  //   setPreviewMode: (mode: CoursePreviewMode) => {
  //     dispatch({ type: CourseEditorActionType.SET_PREVIEW_MODE, payload: mode });
  //   },
  //
  //   // History (match interface)
  //   undo,
  //   redo,
  //   clearHistory,
  //
  //   // Auto-save (match interface)
  //   enableAutoSave,
  //   disableAutoSave,
  //   setAutoSaveInterval,
  //
  //   // Reset (match interface)
  //   resetEditor,
  // };

  return <CourseEditorContext.Provider value={contextValue}>{children}</CourseEditorContext.Provider>;
};

/**
 * Hook to use Course Editor context
 */
export const useCourseEditor = (): CourseEditorContextValue => {
  const context = useContext(CourseEditorContext);

  if (context === undefined) {
    throw new Error('useCourseEditor must be used within a CourseEditorContext');
  }

  return context;
};

/**
 * Hook to use Course Editor state only (for read-only access)
 */
export const useCourseEditorState = (): CourseEditorState => {
  const { state } = useCourseEditor();
  return state;
};

/**
 * Hook to use Course Editor actions only
 */
export const useCourseEditorActions = () => {
  const {
    setLoading,
    setError,
    loadCourse,
    clearCourse,
    createNewCourse,
    updateCourseField,
    updateCourseContent,
    updateCourseMetadata,
    addChapter,
    updateChapter,
    deleteChapter,
    reorderChapters,
    addLesson,
    updateLesson,
    deleteLesson,
    reorderLessons,
    saveCourse,
    validateCourse,
    clearValidationErrors,
    setEditorMode,
    setActiveTab,
    setPreviewMode,
    undo,
    redo,
    clearHistory,
    enableAutoSave,
    disableAutoSave,
    setAutoSaveInterval,
    resetEditor,
  } = useCourseEditor();

  return {
    setLoading,
    setError,
    loadCourse,
    clearCourse,
    createNewCourse,
    updateCourseField,
    updateCourseContent,
    updateCourseMetadata,
    addChapter,
    updateChapter,
    deleteChapter,
    reorderChapters,
    addLesson,
    updateLesson,
    deleteLesson,
    reorderLessons,
    saveCourse,
    validateCourse,
    clearValidationErrors,
    setEditorMode,
    setActiveTab,
    setPreviewMode,
    undo,
    redo,
    clearHistory,
    enableAutoSave,
    disableAutoSave,
    setAutoSaveInterval,
    resetEditor,
  };
};
