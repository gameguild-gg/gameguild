import type { Course } from '@/lib/courses';
import {
  CourseEditorActionType,
  courseEditorValidationRules,
  defaultCourseEditorState,
  type CourseChapter,
  type CourseEditorAction,
  type CourseEditorHistoryEntry,
  type CourseEditorState,
  type CourseLesson,
  type CourseValidationError,
  type EnhancedCourseContent,
} from '../types';

type EditableCourse = Course & {
  content?: EnhancedCourseContent;
};

let fallbackIdSequence = 0;

function createId(prefix: string): string {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return `${prefix}-${crypto.randomUUID()}`;
  }

  fallbackIdSequence += 1;
  return `${prefix}-${Date.now()}-${fallbackIdSequence}`;
}

function cloneCourse<T>(course: T): T {
  return JSON.parse(JSON.stringify(course)) as T;
}

function normalizeDate(date = new Date()): string {
  return date.toISOString();
}

function getCourseContent(course: Course): EnhancedCourseContent {
  const editable = course as EditableCourse;

  return {
    chapters: editable.content?.chapters ?? [],
    syllabus: editable.content?.syllabus ?? '',
    prerequisites: editable.content?.prerequisites ?? [],
    objectives: editable.content?.objectives ?? [],
    totalDuration: editable.content?.totalDuration ?? 0,
    totalLessons: editable.content?.totalLessons ?? 0,
    completionRate: editable.content?.completionRate,
  };
}

function summarizeContent(chapters: CourseChapter[]) {
  return {
    totalLessons: chapters.reduce((total, chapter) => total + chapter.lessons.length, 0),
    totalDuration: chapters.reduce(
      (total, chapter) =>
        total + chapter.lessons.reduce((chapterTotal, lesson) => chapterTotal + (lesson.duration ?? 0), 0),
      0,
    ),
  };
}

function updateCourseContent(course: Course, updates: Partial<EnhancedCourseContent>): Course {
  const currentContent = getCourseContent(course);
  const chapters = updates.chapters ?? currentContent.chapters;
  const totals = summarizeContent(chapters);

  return {
    ...course,
    content: {
      ...currentContent,
      ...updates,
      chapters,
      totalDuration: totals.totalDuration,
      totalLessons: totals.totalLessons,
    },
  } as Course;
}

function hasUnsavedChanges(current: Course | null, original: Course | null): boolean {
  if (!current || !original) return current !== original;
  return JSON.stringify(current) !== JSON.stringify(original);
}

function createHistoryEntry(course: Course, action: string, description: string): CourseEditorHistoryEntry {
  return {
    id: createId('history'),
    timestamp: new Date(),
    action,
    course: cloneCourse(course),
    description,
  };
}

function withHistory(
  state: CourseEditorState,
  courseBeforeChange: Course,
  action: string,
  description: string,
): Pick<CourseEditorState, 'history' | 'historyIndex' | 'canUndo' | 'canRedo'> {
  if (!state.config.enableHistory) {
    return {
      history: state.history,
      historyIndex: state.historyIndex,
      canUndo: state.canUndo,
      canRedo: state.canRedo,
    };
  }

  const entry = createHistoryEntry(courseBeforeChange, action, description);
  const nextHistory = [...state.history.slice(0, state.historyIndex + 1), entry].slice(-state.config.maxHistorySteps);

  return {
    history: nextHistory,
    historyIndex: nextHistory.length - 1,
    canUndo: nextHistory.length > 0,
    canRedo: false,
  };
}

export function validateCourse(course: Course): CourseValidationError[] {
  const errors: CourseValidationError[] = [];
  const title = course.title?.trim() ?? '';
  const description = course.description?.trim() ?? '';
  const category = course.category?.trim() ?? '';
  const slug = course.slug?.trim() ?? '';

  if (!title) {
    errors.push({ field: 'title', message: 'Course title is required', type: 'required' });
  } else if (title.length < courseEditorValidationRules.title.minLength) {
    errors.push({
      field: 'title',
      message: `Title must be at least ${courseEditorValidationRules.title.minLength} characters`,
      type: 'length',
    });
  } else if (title.length > courseEditorValidationRules.title.maxLength) {
    errors.push({
      field: 'title',
      message: `Title must be no more than ${courseEditorValidationRules.title.maxLength} characters`,
      type: 'length',
    });
  }

  if (!description) {
    errors.push({ field: 'description', message: 'Course description is required', type: 'required' });
  } else if (description.length < courseEditorValidationRules.description.minLength) {
    errors.push({
      field: 'description',
      message: `Description must be at least ${courseEditorValidationRules.description.minLength} characters`,
      type: 'length',
    });
  } else if (description.length > courseEditorValidationRules.description.maxLength) {
    errors.push({
      field: 'description',
      message: `Description must be no more than ${courseEditorValidationRules.description.maxLength} characters`,
      type: 'length',
    });
  }

  if (!category) {
    errors.push({ field: 'category', message: 'Course category is required', type: 'required' });
  }

  if (!slug) {
    errors.push({ field: 'slug', message: 'Course slug is required', type: 'required' });
  }

  if (!['Beginner', 'Intermediate', 'Advanced'].includes(course.level)) {
    errors.push({ field: 'level', message: 'Course level is invalid', type: 'invalid' });
  }

  if (!course.instructor?.name?.trim()) {
    errors.push({ field: 'instructor.name', message: 'Instructor name is required', type: 'required' });
  }

  return errors;
}

export function createInitialCourseEditorState(initialState: Partial<CourseEditorState> = {}): CourseEditorState {
  return {
    ...defaultCourseEditorState,
    ...initialState,
    config: {
      ...defaultCourseEditorState.config,
      ...initialState.config,
    },
    lastUpdated: new Date(),
  };
}

export function createEmptyCourse(): Course {
  return {
    id: createId('course'),
    title: '',
    description: '',
    category: '',
    level: 'Beginner',
    duration: '0h',
    enrolledStudents: 0,
    rating: 0,
    price: 0,
    image: '',
    slug: '',
    instructor: {
      name: '',
      avatar: '',
    },
    isEnrolled: false,
    progress: 0,
    certification: false,
  };
}

function updateStateCourse(state: CourseEditorState, course: Course): CourseEditorState {
  const validationErrors = validateCourse(course);

  return {
    ...state,
    course,
    validationErrors,
    isValid: validationErrors.length === 0,
    hasUnsavedChanges: hasUnsavedChanges(course, state.originalCourse),
    lastUpdated: new Date(),
  };
}

export function courseEditorReducer(state: CourseEditorState, action: CourseEditorAction): CourseEditorState {
  switch (action.type) {
    case CourseEditorActionType.SET_LOADING:
      return { ...state, isLoading: action.payload, error: action.payload ? null : state.error };

    case CourseEditorActionType.SET_ERROR:
      return { ...state, error: action.payload, isLoading: false, saveStatus: action.payload ? 'error' : state.saveStatus };

    case CourseEditorActionType.CLEAR_ERROR:
      return { ...state, error: null };

    case CourseEditorActionType.SET_COURSE: {
      const course = cloneCourse(action.payload);
      const validationErrors = validateCourse(course);

      return {
        ...state,
        course,
        originalCourse: cloneCourse(course),
        validationErrors,
        isValid: validationErrors.length === 0,
        hasUnsavedChanges: false,
        isLoading: false,
        error: null,
        mode: 'edit',
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionType.CLEAR_COURSE:
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

    case CourseEditorActionType.UPDATE_COURSE_FIELD:
      if (!state.course) return state;
      return updateStateCourse(state, { ...state.course, [action.payload.field]: action.payload.value });

    case CourseEditorActionType.UPDATE_COURSE_METADATA:
      if (!state.course) return state;
      return updateStateCourse(state, { ...state.course, ...action.payload });

    case CourseEditorActionType.UPDATE_COURSE_CONTENT:
      if (!state.course) return state;
      return updateStateCourse(state, updateCourseContent(state.course, action.payload));

    case CourseEditorActionType.ADD_CHAPTER: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const now = normalizeDate();
      const chapter: CourseChapter = {
        ...action.payload,
        id: action.payload.id || createId('chapter'),
        order: content.chapters.length + 1,
        lessons: action.payload.lessons ?? [],
        createdAt: action.payload.createdAt ?? now,
        updatedAt: now,
      };
      const course = updateCourseContent(state.course, { chapters: [...content.chapters, chapter] });
      return {
        ...updateStateCourse(state, course),
        ...withHistory(state, state.course, 'ADD_CHAPTER', `Added chapter "${chapter.title}"`),
      };
    }

    case CourseEditorActionType.UPDATE_CHAPTER: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const chapters = content.chapters.map((chapter) =>
        chapter.id === action.payload.chapterId
          ? { ...chapter, ...action.payload.chapter, updatedAt: normalizeDate() }
          : chapter,
      );
      return updateStateCourse(state, updateCourseContent(state.course, { chapters }));
    }

    case CourseEditorActionType.DELETE_CHAPTER: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const chapter = content.chapters.find((item) => item.id === action.payload);
      if (!chapter) return state;
      const chapters = content.chapters
        .filter((item) => item.id !== action.payload)
        .map((item, index) => ({ ...item, order: index + 1 }));
      const course = updateCourseContent(state.course, { chapters });
      return {
        ...updateStateCourse(state, course),
        ...withHistory(state, state.course, 'DELETE_CHAPTER', `Deleted chapter "${chapter.title}"`),
      };
    }

    case CourseEditorActionType.REORDER_CHAPTERS: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const byId = new Map(content.chapters.map((chapter) => [chapter.id, chapter]));
      const chapters = action.payload
        .map((id) => byId.get(id))
        .filter((chapter): chapter is CourseChapter => Boolean(chapter))
        .map((chapter, index) => ({ ...chapter, order: index + 1 }));
      return updateStateCourse(state, updateCourseContent(state.course, { chapters }));
    }

    case CourseEditorActionType.ADD_LESSON: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const now = normalizeDate();
      const chapters = content.chapters.map((chapter) => {
        if (chapter.id !== action.payload.chapterId) return chapter;

        const lesson: CourseLesson = {
          ...action.payload.lesson,
          id: action.payload.lesson.id || createId('lesson'),
          order: chapter.lessons.length + 1,
          createdAt: action.payload.lesson.createdAt ?? now,
          updatedAt: now,
        };

        return { ...chapter, lessons: [...chapter.lessons, lesson], updatedAt: now };
      });
      const course = updateCourseContent(state.course, { chapters });
      return {
        ...updateStateCourse(state, course),
        ...withHistory(state, state.course, 'ADD_LESSON', `Added lesson "${action.payload.lesson.title}"`),
      };
    }

    case CourseEditorActionType.UPDATE_LESSON: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const chapters = content.chapters.map((chapter) => {
        if (chapter.id !== action.payload.chapterId) return chapter;
        return {
          ...chapter,
          lessons: chapter.lessons.map((lesson) =>
            lesson.id === action.payload.lessonId
              ? { ...lesson, ...action.payload.lesson, updatedAt: normalizeDate() }
              : lesson,
          ),
          updatedAt: normalizeDate(),
        };
      });
      return updateStateCourse(state, updateCourseContent(state.course, { chapters }));
    }

    case CourseEditorActionType.DELETE_LESSON: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const chapters = content.chapters.map((chapter) => {
        if (chapter.id !== action.payload.chapterId) return chapter;
        return {
          ...chapter,
          lessons: chapter.lessons
            .filter((lesson) => lesson.id !== action.payload.lessonId)
            .map((lesson, index) => ({ ...lesson, order: index + 1 })),
          updatedAt: normalizeDate(),
        };
      });
      return updateStateCourse(state, updateCourseContent(state.course, { chapters }));
    }

    case CourseEditorActionType.REORDER_LESSONS: {
      if (!state.course) return state;
      const content = getCourseContent(state.course);
      const chapters = content.chapters.map((chapter) => {
        if (chapter.id !== action.payload.chapterId) return chapter;
        const byId = new Map(chapter.lessons.map((lesson) => [lesson.id, lesson]));
        return {
          ...chapter,
          lessons: action.payload.lessonIds
            .map((id) => byId.get(id))
            .filter((lesson): lesson is CourseLesson => Boolean(lesson))
            .map((lesson, index) => ({ ...lesson, order: index + 1 })),
          updatedAt: normalizeDate(),
        };
      });
      return updateStateCourse(state, updateCourseContent(state.course, { chapters }));
    }

    case CourseEditorActionType.SET_SAVING:
      return { ...state, isSaving: action.payload };

    case CourseEditorActionType.SET_SAVE_STATUS:
      return { ...state, saveStatus: action.payload, isSaving: action.payload === 'saving' };

    case CourseEditorActionType.SET_LAST_SAVED:
      return { ...state, lastSaved: action.payload, hasUnsavedChanges: false, saveStatus: 'saved' };

    case CourseEditorActionType.SET_VALIDATION_ERRORS:
      return { ...state, validationErrors: action.payload, isValid: action.payload.length === 0 };

    case CourseEditorActionType.CLEAR_VALIDATION_ERRORS:
      return { ...state, validationErrors: [], isValid: true };

    case CourseEditorActionType.SET_EDITOR_MODE:
      return { ...state, mode: action.payload };

    case CourseEditorActionType.SET_ACTIVE_TAB:
      return { ...state, activeTab: action.payload };

    case CourseEditorActionType.SET_PREVIEW_MODE:
      return { ...state, previewMode: action.payload };

    case CourseEditorActionType.ADD_TO_HISTORY:
      return {
        ...state,
        history: [...state.history.slice(0, state.historyIndex + 1), action.payload].slice(-state.config.maxHistorySteps),
        historyIndex: Math.min(state.historyIndex + 1, state.config.maxHistorySteps - 1),
        canUndo: true,
        canRedo: false,
      };

    case CourseEditorActionType.UNDO: {
      if (!state.canUndo || state.historyIndex < 0) return state;
      const entry = state.history[state.historyIndex];
      if (!entry) return state;
      const historyIndex = state.historyIndex - 1;
      return {
        ...state,
        course: cloneCourse(entry.course),
        historyIndex,
        canUndo: historyIndex >= 0,
        canRedo: true,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionType.REDO: {
      if (!state.canRedo || state.historyIndex >= state.history.length - 1) return state;
      const historyIndex = state.historyIndex + 1;
      const entry = state.history[historyIndex];
      if (!entry) return state;
      return {
        ...state,
        course: cloneCourse(entry.course),
        historyIndex,
        canUndo: true,
        canRedo: historyIndex < state.history.length - 1,
        hasUnsavedChanges: true,
        lastUpdated: new Date(),
      };
    }

    case CourseEditorActionType.CLEAR_HISTORY:
      return { ...state, history: [], historyIndex: -1, canUndo: false, canRedo: false };

    case CourseEditorActionType.ENABLE_AUTO_SAVE:
      return { ...state, config: { ...state.config, autoSave: true } };

    case CourseEditorActionType.DISABLE_AUTO_SAVE:
      return { ...state, config: { ...state.config, autoSave: false } };

    case CourseEditorActionType.SET_AUTO_SAVE_INTERVAL:
      return { ...state, config: { ...state.config, autoSaveInterval: action.payload } };

    case CourseEditorActionType.RESET_EDITOR:
      return { ...defaultCourseEditorState, config: state.config, lastUpdated: new Date() };

    default:
      return state;
  }
}
