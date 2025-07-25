/**
 * Validate course data
 */
export const validateCourse = (course: EnhancedCourse): CourseValidationError[] => {
  const errors: CourseValidationError[] = [];

  // Title validation
  if (!course.title) {
    errors.push({
      field: 'title',
      message: 'Course title is required',
      type: 'required',
    });
  } else if (course.title.length < courseEditorValidationRules.title.minLength) {
    errors.push({
      field: 'title',
      message: `Title must be at least ${ courseEditorValidationRules.title.minLength } characters`,
      type: 'length',
    });
  } else if (course.title.length > courseEditorValidationRules.title.maxLength) {
    errors.push({
      field: 'title',
      message: `Title must be no more than ${ courseEditorValidationRules.title.maxLength } characters`,
      type: 'length',
    });
  }

  // Description validation
  if (!course.description) {
    errors.push({
      field: 'description',
      message: 'Course description is required',
      type: 'required',
    });
  } else if (course.description.length < courseEditorValidationRules.description.minLength) {
    errors.push({
      field: 'description',
      message: `Description must be at least ${ courseEditorValidationRules.description.minLength } characters`,
      type: 'length',
    });
  } else if (course.description.length > courseEditorValidationRules.description.maxLength) {
    errors.push({
      field: 'description',
      message: `Description must be no more than ${ courseEditorValidationRules.description.maxLength } characters`,
      type: 'length',
    });
  }

  // Area validation
  if (!course.area) {
    errors.push({
      field: 'area',
      message: 'Course area is required',
      type: 'required',
    });
  }

  // Level validation
  if (!course.level) {
    errors.push({
      field: 'level',
      message: 'Course level is required',
      type: 'required',
    });
  }

  // Tools validation
  if (!course.tools || course.tools.length === 0) {
    errors.push({
      field: 'tools',
      message: 'At least one tool is required',
      type: 'required',
    });
  }

  return errors;
};

/**
 * Create history entry for tracking changes
 */
const createHistoryEntry = (course: EnhancedCourse, action: string, description: string): CourseEditorHistoryEntry => ({
  id: generateId(),
  timestamp: new Date(),
  action,
  course: deepClone(course),
  description,
});

/**
 * Update course content helper
 */
const updateCourseContent = (course: EnhancedCourse, updates: Partial<EnhancedCourseContent>): EnhancedCourse => {
  const updatedCourse = { ...course };

  if (!updatedCourse.content) {
    updatedCourse.content = {
      chapters: [],
      syllabus: '',
      prerequisites: [],
      objectives: [],
      totalDuration: 0,
      totalLessons: 0,
    };
  }

  updatedCourse.content = {
    ...updatedCourse.content,
    ...updates,
  };

  // Recalculate totals if chapters were updated
  if (updates.chapters) {
    const totalLessons = updates.chapters.reduce((total, chapter) => total + chapter.lessons.length, 0);
    const totalDuration = updates.chapters.reduce((total, chapter) => {
      return total + chapter.lessons.reduce((chapterTotal, lesson) => chapterTotal + (lesson.duration || 0), 0);
    }, 0);

    updatedCourse.content.totalLessons = totalLessons;
    updatedCourse.content.totalDuration = totalDuration;
  }

  return updatedCourse;
};

/**
 * Find chapter by ID
 */
const findChapterById = (chapters: CourseChapter[], chapterId: string): CourseChapter | undefined => {
  return chapters.find(chapter => chapter.id === chapterId);
};

/**
 * Find lesson by ID within a chapter
 */
const findLessonById = (chapter: CourseChapter, lessonId: string): CourseLesson | undefined => {
  return chapter.lessons.find(lesson => lesson.id === lessonId);
};

/**
 * Check if course has unsaved changes
 */
const hasUnsavedChanges = (current: EnhancedCourse | null, original: EnhancedCourse | null): boolean => {
  if (!current || !original) return current !== original;

  // Simple comparison - in production, you might want a more sophisticated deep comparison
  return JSON.stringify(current) !== JSON.stringify(original);
};

/**
 * Course Editor Reducer
 */
export const courseEditorReducer = (state: CourseEditorState, action: CourseEditorAction): CourseEditorState => {
  switch (action.type) {
    case CourseEditorActionTypes.SET_LOADING: {
      return {
        ...state,
        isLoading: action.payload,
        error: action.payload ? null : state.error,
      };
    }

    case CourseEditorActionTypes.SET_ERROR: {
      return {
        ...state,
        error: action.payload,
        isLoading: false,
      };
    }

    case CourseEditorActionTypes.CLEAR_ERROR: {
      return {
        ...state,
        error: null,
      };
    }

    case CourseEditorActionTypes.SET_COURSE: {
      const course = action.payload;
      const validationErrors = validateCourse(course);

      return {
        ...state,
        course: deepClone(course),
        originalCourse: deepClone(course),
        validationErrors,
        isValid: validationErrors.length === 0,
        hasUnsavedChanges: false,
        isLoading: false,
        error: null,
        mode: 'edit',
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.CLEAR_COURSE: {
      return {
        ...state,
        course: null,
        originalCourse: null,
        validationErrors: [],
        isValid: true,
        hasUnsavedChanges: false,
        history: [],
        historyIndex: -1,
        canUndo: false,
        canRedo: false,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.UPDATE_COURSE_FIELD: {
      if (!state.course) return state;

      const { field, value } = action.payload;
      const updatedCourse = {
        ...state.course,
        [field]: value,
      };

      const validationErrors = validateCourse(updatedCourse);
      const unsavedChanges = hasUnsavedChanges(updatedCourse, state.originalCourse);

      return {
        ...state,
        course: updatedCourse,
        validationErrors,
        isValid: validationErrors.length === 0,
        hasUnsavedChanges: unsavedChanges,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.UPDATE_COURSE_CONTENT: {
      if (!state.course) return state;

      const updatedCourse = updateCourseContent(state.course, action.payload);
      const validationErrors = validateCourse(updatedCourse);
      const unsavedChanges = hasUnsavedChanges(updatedCourse, state.originalCourse);

      return {
        ...state,
        course: updatedCourse,
        validationErrors,
        isValid: validationErrors.length === 0,
        hasUnsavedChanges: unsavedChanges,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.UPDATE_COURSE_METADATA: {
      if (!state.course) return state;

      const updatedCourse = {
        ...state.course,
        ...action.payload,
      };

      const validationErrors = validateCourse(updatedCourse);
      const unsavedChanges = hasUnsavedChanges(updatedCourse, state.originalCourse);

      return {
        ...state,
        course: updatedCourse,
        validationErrors,
        isValid: validationErrors.length === 0,
        hasUnsavedChanges: unsavedChanges,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.ADD_CHAPTER: {
      if (!state.course) return state;

      const newChapter: CourseChapter = {
        ...action.payload,
        id: generateId(),
        order: (state.course.content?.chapters?.length || 0) + 1,
        lessons: [],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };

      const chapters = [ ...(state.course.content?.chapters || []), newChapter ];
      const updatedCourse = updateCourseContent(state.course, { chapters });

      // Add to history
      const historyEntry = createHistoryEntry(
        state.course,
        'ADD_CHAPTER',
        `Added chapter "${ newChapter.title }"`,
      );

      const newHistory = [ ...state.history.slice(0, state.historyIndex + 1), historyEntry ];

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        history: newHistory.slice(-state.config.maxHistorySteps),
        historyIndex: Math.min(newHistory.length - 1, state.config.maxHistorySteps - 1),
        canUndo: true,
        canRedo: false,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.UPDATE_CHAPTER: {
      if (!state.course?.content?.chapters) return state;

      const { chapterId, chapter } = action.payload;
      const chapters = state.course.content.chapters.map(ch =>
        ch.id === chapterId
          ? { ...ch, ...chapter, updatedAt: new Date().toISOString() }
          : ch,
      );

      const updatedCourse = updateCourseContent(state.course, { chapters });

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.DELETE_CHAPTER: {
      if (!state.course?.content?.chapters) return state;

      const chapterId = action.payload;
      const chapterToDelete = findChapterById(state.course.content.chapters, chapterId);

      if (!chapterToDelete) return state;

      const chapters = state.course.content.chapters
      .filter(ch => ch.id !== chapterId)
      .map((ch, index) => ({ ...ch, order: index + 1 }));

      const updatedCourse = updateCourseContent(state.course, { chapters });

      // Add to history
      const historyEntry = createHistoryEntry(
        state.course,
        'DELETE_CHAPTER',
        `Deleted chapter "${ chapterToDelete.title }"`,
      );

      const newHistory = [ ...state.history.slice(0, state.historyIndex + 1), historyEntry ];

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        history: newHistory.slice(-state.config.maxHistorySteps),
        historyIndex: Math.min(newHistory.length - 1, state.config.maxHistorySteps - 1),
        canUndo: true,
        canRedo: false,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.REORDER_CHAPTERS: {
      if (!state.course?.content?.chapters) return state;

      const chapterIds = action.payload;
      const chapters = chapterIds
      .map(id => state.course!.content!.chapters!.find(ch => ch.id === id))
      .filter(Boolean) as CourseChapter[];
    .
      map((ch, index) => ({ ...ch, order: index + 1 }));

      const updatedCourse = updateCourseContent(state.course, { chapters });

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.ADD_LESSON: {
      if (!state.course?.content?.chapters) return state;

      const { chapterId, lesson } = action.payload;
      const chapters = state.course.content.chapters.map(chapter => {
        if (chapter.id === chapterId) {
          const newLesson: CourseLesson = {
            ...lesson,
            id: generateId(),
            order: chapter.lessons.length + 1,
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          };

          return {
            ...chapter,
            lessons: [ ...chapter.lessons, newLesson ],
            updatedAt: new Date().toISOString(),
          };
        }
        return chapter;
      });

      const updatedCourse = updateCourseContent(state.course, { chapters });

      // Add to history
      const historyEntry = createHistoryEntry(
        state.course,
        'ADD_LESSON',
        `Added lesson "${ lesson.title }" to chapter`,
      );

      const newHistory = [ ...state.history.slice(0, state.historyIndex + 1), historyEntry ];

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        history: newHistory.slice(-state.config.maxHistorySteps),
        historyIndex: Math.min(newHistory.length - 1, state.config.maxHistorySteps - 1),
        canUndo: true,
        canRedo: false,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.UPDATE_LESSON: {
      if (!state.course?.content?.chapters) return state;

      const { chapterId, lessonId, lesson } = action.payload;
      const chapters = state.course.content.chapters.map(chapter => {
        if (chapter.id === chapterId) {
          return {
            ...chapter,
            lessons: chapter.lessons.map(l =>
              l.id === lessonId
                ? { ...l, ...lesson, updatedAt: new Date().toISOString() }
                : l,
            ),
            updatedAt: new Date().toISOString(),
          };
        }
        return chapter;
      });

      const updatedCourse = updateCourseContent(state.course, { chapters });

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.DELETE_LESSON: {
      if (!state.course?.content?.chapters) return state;

      const { chapterId, lessonId } = action.payload;
      let lessonToDelete: CourseLesson | undefined;

      const chapters = state.course.content.chapters.map(chapter => {
        if (chapter.id === chapterId) {
          lessonToDelete = findLessonById(chapter, lessonId);
          return {
            ...chapter,
            lessons: chapter.lessons
            .filter(l => l.id !== lessonId)
            .map((l, index) => ({ ...l, order: index + 1 })),
            updatedAt: new Date().toISOString(),
          };
        }
        return chapter;
      });

      const updatedCourse = updateCourseContent(state.course, { chapters });

      // Add to history
      const historyEntry = createHistoryEntry(
        state.course,
        'DELETE_LESSON',
        `Deleted lesson "${ lessonToDelete?.title || 'Unknown' }"`,
      );

      const newHistory = [ ...state.history.slice(0, state.historyIndex + 1), historyEntry ];

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        history: newHistory.slice(-state.config.maxHistorySteps),
        historyIndex: Math.min(newHistory.length - 1, state.config.maxHistorySteps - 1),
        canUndo: true,
        canRedo: false,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.REORDER_LESSONS: {
      if (!state.course?.content?.chapters) return state;

      const { chapterId, lessonIds } = action.payload;
      const chapters = state.course.content.chapters.map(chapter => {
        if (chapter.id === chapterId) {
          const lessons = lessonIds
          .map(id => chapter.lessons.find(l => l.id === id))
          .filter(Boolean) as CourseLesson[];
        .
          map((l, index) => ({ ...l, order: index + 1 }));

          return {
            ...chapter,
            lessons,
            updatedAt: new Date().toISOString(),
          };
        }
        return chapter;
      });

      const updatedCourse = updateCourseContent(state.course, { chapters });

      return {
        ...state,
        course: updatedCourse,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.SET_SAVING: {
      return {
        ...state,
        isSaving: action.payload,
      };
    }

    case CourseEditorActionTypes.SET_SAVE_STATUS: {
      return {
        ...state,
        saveStatus: action.payload,
        isSaving: action.payload === 'saving',
      };
    }

    case CourseEditorActionTypes.SET_LAST_SAVED: {
      return {
        ...state,
        lastSaved: action.payload,
        hasUnsavedChanges: false,
        saveStatus: 'saved',
      };
    }

    case CourseEditorActionTypes.SET_VALIDATION_ERRORS: {
      return {
        ...state,
        validationErrors: action.payload,
        isValid: action.payload.length === 0,
      };
    }

    case CourseEditorActionTypes.CLEAR_VALIDATION_ERRORS: {
      return {
        ...state,
        validationErrors: [],
        isValid: true,
      };
    }

    case CourseEditorActionTypes.SET_EDITOR_MODE: {
      return {
        ...state,
        mode: action.payload,
      };
    }

    case CourseEditorActionTypes.SET_ACTIVE_TAB: {
      return {
        ...state,
        activeTab: action.payload,
      };
    }

    case CourseEditorActionTypes.SET_PREVIEW_MODE: {
      return {
        ...state,
        previewMode: action.payload,
      };
    }

    case CourseEditorActionTypes.ADD_TO_HISTORY: {
      const newHistory = [ ...state.history.slice(0, state.historyIndex + 1), action.payload ];
      const trimmedHistory = newHistory.slice(-state.config.maxHistorySteps);

      return {
        ...state,
        history: trimmedHistory,
        historyIndex: trimmedHistory.length - 1,
        canUndo: true,
        canRedo: false,
      };
    }

    case CourseEditorActionTypes.UNDO: {
      if (!state.canUndo || state.historyIndex < 0) return state;

      const previousEntry = state.history[state.historyIndex];
      if (!previousEntry) return state;

      const newIndex = state.historyIndex - 1;

      return {
        ...state,
        course: deepClone(previousEntry.course),
        historyIndex: newIndex,
        canUndo: newIndex >= 0,
        canRedo: true,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.REDO: {
      if (!state.canRedo || state.historyIndex >= state.history.length - 1) return state;

      const nextIndex = state.historyIndex + 1;
      const nextEntry = state.history[nextIndex];
      if (!nextEntry) return state;

      return {
        ...state,
        course: deepClone(nextEntry.course),
        historyIndex: nextIndex,
        canUndo: true,
        canRedo: nextIndex < state.history.length - 1,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionTypes.CLEAR_HISTORY: {
      return {
        ...state,
        history: [],
        historyIndex: -1,
        canUndo: false,
        canRedo: false,
      };
    }

    case CourseEditorActionTypes.ENABLE_AUTO_SAVE: {
      return {
        ...state,
        config: {
          ...state.config,
          autoSave: true,
        },
      };
    }

    case CourseEditorActionTypes.DISABLE_AUTO_SAVE: {
      return {
        ...state,
        config: {
          ...state.config,
          autoSave: false,
        },
      };
    }

    case CourseEditorActionTypes.SET_AUTO_SAVE_INTERVAL: {
      return {
        ...state,
        config: {
          ...state.config,
          autoSaveInterval: action.payload,
        },
      };
    }

    case CourseEditorActionTypes.RESET_EDITOR: {
      return {
        ...defaultCourseEditorState,
        config: state.config, // Preserve configuration
      };
    }

    default: {
      console.warn(`Unhandled course editor action type: ${ (action as { type: string }).type }`);
      return state;
    }
  }
};

/**
 * Initialize course editor state with optional overrides
 */
export const createInitialCourseEditorState = (initialState: Partial<CourseEditorState>): CourseEditorState => {
  return {
    ...defaultCourseEditorState,
    ...initialState,
    lastUpdated: new Date(),
  };
};

/**
 * Utility function to create a new empty course
 */
export const createEmptyCourse = (): EnhancedCourse => ({
  id: generateId(),
  title: '',
  description: '',
  area: 'programming',
  level: 1,
  difficulty: 1,
  status: 'draft',
  tools: [],
  tags: [],
  instructors: [],
  isPublic: false,
  isFeatured: false,
  content: {
    chapters: [],
    syllabus: '',
    prerequisites: [],
    objectives: [],
    totalDuration: 0,
    totalLessons: 0,
  },
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
});
