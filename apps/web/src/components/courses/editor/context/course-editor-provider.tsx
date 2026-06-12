'use client';

import React, { createContext, useContext, useMemo, useState } from 'react';

type CourseStatus = 'draft' | 'published' | 'archived';
type Visibility = 'public' | 'private' | 'premium';
type LessonType = 'text' | 'video' | 'quiz' | 'assignment' | 'file' | 'interactive';

interface CourseSession {
  id: string;
  title: string;
  startDate: string;
  endDate: string;
  capacity: number;
}

interface CourseProduct {
  id: string;
  name: string;
  price: number;
  currency: string;
  type: string;
}

interface CourseMediaAsset {
  url: string;
  alt?: string;
  file?: File;
  platform?: string;
  embedId?: string;
}

interface CourseLesson {
  id: string;
  title: string;
  description: string;
  type: LessonType;
  visibility: Visibility;
  status: CourseStatus;
  isRequired: boolean;
  duration: number;
  sortOrder: number;
}

interface CourseModule {
  id: string;
  title: string;
  description: string;
  visibility: Visibility;
  status: CourseStatus;
  sortOrder: number;
  estimatedDuration: number;
  isExpanded: boolean;
  lessons: CourseLesson[];
  submodules: CourseModule[];
}

interface CourseEditorState {
  title: string;
  slug: string;
  summary: string;
  description: string;
  category: string;
  difficulty: number;
  manualSlugEdit: boolean;
  status: CourseStatus;
  isValid: boolean;
  errors: Record<string, string>;
  media: {
    thumbnail?: CourseMediaAsset;
    showcaseVideo?: CourseMediaAsset;
  };
  products: CourseProduct[];
  enrollment: {
    isOpen: boolean;
    maxEnrollments?: number;
    currentEnrollments: number;
    deadline: string;
  };
  estimatedHours: number;
  tags: string[];
  content: {
    modules: CourseModule[];
    selectedItems: string[];
  };
  undoRedo: {
    canUndo: boolean;
    canRedo: boolean;
  };
  delivery: {
    mode: string;
    sessions: CourseSession[];
    timezone: string;
  };
  accessWindow: {
    start: string;
    end: string;
  };
  enrollmentWindow: {
    start: string;
    end: string;
  };
}

type ContentAction =
  | { type: 'ADD_MODULE'; module: Omit<CourseModule, 'id' | 'isExpanded'> & Partial<Pick<CourseModule, 'isExpanded'>> }
  | { type: 'REMOVE_MODULE'; moduleId: string }
  | { type: 'UPDATE_MODULE'; moduleId: string; updates: Partial<CourseModule> }
  | { type: 'TOGGLE_MODULE_EXPANDED'; moduleId: string }
  | { type: 'DUPLICATE_MODULE'; moduleId: string }
  | { type: 'ADD_LESSON'; moduleId: string; lesson: Omit<CourseLesson, 'id' | 'sortOrder'> & Partial<Pick<CourseLesson, 'sortOrder'>> }
  | { type: 'REMOVE_LESSON'; lessonId: string }
  | { type: 'UPDATE_LESSON'; lessonId: string; updates: Partial<CourseLesson> }
  | { type: 'DUPLICATE_LESSON'; lessonId: string }
  | { type: 'UNDO' }
  | { type: 'REDO' };

interface CourseEditorContextType {
  state: CourseEditorState;
  updateTitle: (title: string) => void;
  updateSlug: (slug: string) => void;
  updateSummary: (summary: string) => void;
  updateDescription: (description: string) => void;
  updateCategory: (category: string) => void;
  updateDifficulty: (difficulty: number) => void;
  setThumbnail: (asset?: CourseMediaAsset) => void;
  setShowcaseVideo: (asset?: CourseMediaAsset) => void;
  addProduct: (product: CourseProduct) => void;
  removeProduct: (productId: string) => void;
  updateProduct: (productId: string, updates: Partial<CourseProduct>) => void;
  setEnrollmentStatus: (isOpen: boolean) => void;
  setMaxEnrollments: (maxEnrollments?: number) => void;
  setEnrollmentDeadline: (deadline: string) => void;
  setEstimatedHours: (hours: number) => void;
  addTag: (tag: string) => void;
  removeTag: (tag: string) => void;
  setStatus: (status: CourseStatus) => void;
  dispatch: (action: ContentAction) => void;
  setDeliveryMode: (mode: string) => void;
  setAccessWindow: (window: { start: string; end: string }) => void;
  setEnrollmentWindow: (window: { start: string; end: string }) => void;
  addSession: (session: Omit<CourseSession, 'id'>) => void;
  removeSession: (sessionId: string) => void;
  setTimezone: (timezone: string) => void;
  validate: () => { isValid: boolean; errors: string[] };
}

const CourseEditorContext = createContext<CourseEditorContextType | undefined>(undefined);

function createId(prefix: string) {
  if (typeof crypto !== 'undefined' && 'randomUUID' in crypto) {
    return `${prefix}-${crypto.randomUUID()}`;
  }

  return `${prefix}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
}

function slugify(value: string) {
  return value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .replace(/^-|-$/g, '');
}

const initialState: CourseEditorState = {
  title: '',
  slug: '',
  summary: '',
  description: '',
  category: '',
  difficulty: 1,
  manualSlugEdit: false,
  status: 'draft',
  isValid: true,
  errors: {},
  media: {},
  products: [],
  enrollment: {
    isOpen: false,
    currentEnrollments: 0,
    deadline: '',
  },
  estimatedHours: 1,
  tags: [],
  content: {
    modules: [],
    selectedItems: [],
  },
  undoRedo: {
    canUndo: false,
    canRedo: false,
  },
  delivery: {
    mode: 'self-paced',
    sessions: [],
    timezone: 'UTC',
  },
  accessWindow: {
    start: '',
    end: '',
  },
  enrollmentWindow: {
    start: '',
    end: '',
  },
};

function applyContentAction(state: CourseEditorState, action: ContentAction): CourseEditorState {
  switch (action.type) {
    case 'ADD_MODULE': {
      const newModule: CourseModule = {
        id: createId('module'),
        title: action.module.title,
        description: action.module.description,
        visibility: action.module.visibility,
        status: action.module.status,
        sortOrder: action.module.sortOrder,
        estimatedDuration: action.module.estimatedDuration,
        isExpanded: action.module.isExpanded ?? true,
        lessons: action.module.lessons ?? [],
        submodules: action.module.submodules ?? [],
      };

      return {
        ...state,
        content: {
          ...state.content,
          modules: [...state.content.modules, newModule],
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };
    }

    case 'REMOVE_MODULE':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.filter((module) => module.id !== action.moduleId),
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };

    case 'UPDATE_MODULE':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) =>
            module.id === action.moduleId ? { ...module, ...action.updates } : module,
          ),
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };

    case 'TOGGLE_MODULE_EXPANDED':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) =>
            module.id === action.moduleId ? { ...module, isExpanded: !module.isExpanded } : module,
          ),
        },
      };

    case 'DUPLICATE_MODULE': {
      const module = state.content.modules.find((item) => item.id === action.moduleId);
      if (!module) return state;

      return {
        ...state,
        content: {
          ...state.content,
          modules: [
            ...state.content.modules,
            {
              ...module,
              id: createId('module'),
              title: `${module.title} copy`,
              lessons: module.lessons.map((lesson) => ({ ...lesson, id: createId('lesson') })),
            },
          ],
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };
    }

    case 'ADD_LESSON':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => {
            if (module.id !== action.moduleId) return module;

            const lesson: CourseLesson = {
              id: createId('lesson'),
              title: action.lesson.title,
              description: action.lesson.description,
              type: action.lesson.type,
              visibility: action.lesson.visibility,
              status: action.lesson.status,
              isRequired: action.lesson.isRequired,
              duration: action.lesson.duration,
              sortOrder: action.lesson.sortOrder ?? module.lessons.length,
            };

            return { ...module, isExpanded: true, lessons: [...module.lessons, lesson] };
          }),
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };

    case 'REMOVE_LESSON':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => ({
            ...module,
            lessons: module.lessons.filter((lesson) => lesson.id !== action.lessonId),
          })),
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };

    case 'UPDATE_LESSON':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => ({
            ...module,
            lessons: module.lessons.map((lesson) =>
              lesson.id === action.lessonId ? { ...lesson, ...action.updates } : lesson,
            ),
          })),
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };

    case 'DUPLICATE_LESSON':
      return {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => {
            const lesson = module.lessons.find((item) => item.id === action.lessonId);
            if (!lesson) return module;

            return {
              ...module,
              lessons: [...module.lessons, { ...lesson, id: createId('lesson'), title: `${lesson.title} copy` }],
            };
          }),
        },
        undoRedo: { ...state.undoRedo, canUndo: true },
      };

    case 'UNDO':
    case 'REDO':
    default:
      return state;
  }
}

export function CourseEditorProvider({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<CourseEditorState>(initialState);

  const value = useMemo<CourseEditorContextType>(
    () => ({
      state,
      updateTitle: (title) =>
        setState((prev) => ({
          ...prev,
          title,
          slug: prev.manualSlugEdit ? prev.slug : slugify(title),
        })),
      updateSlug: (slug) => setState((prev) => ({ ...prev, slug: slugify(slug), manualSlugEdit: true })),
      updateSummary: (summary) => setState((prev) => ({ ...prev, summary: summary.slice(0, 200) })),
      updateDescription: (description) => setState((prev) => ({ ...prev, description })),
      updateCategory: (category) => setState((prev) => ({ ...prev, category })),
      updateDifficulty: (difficulty) => setState((prev) => ({ ...prev, difficulty })),
      setThumbnail: (asset) => setState((prev) => ({ ...prev, media: { ...prev.media, thumbnail: asset } })),
      setShowcaseVideo: (asset) => setState((prev) => ({ ...prev, media: { ...prev.media, showcaseVideo: asset } })),
      addProduct: (product) => setState((prev) => ({ ...prev, products: [...prev.products, product] })),
      removeProduct: (productId) =>
        setState((prev) => ({ ...prev, products: prev.products.filter((product) => product.id !== productId) })),
      updateProduct: (productId, updates) =>
        setState((prev) => ({
          ...prev,
          products: prev.products.map((product) => (product.id === productId ? { ...product, ...updates } : product)),
        })),
      setEnrollmentStatus: (isOpen) =>
        setState((prev) => ({ ...prev, enrollment: { ...prev.enrollment, isOpen } })),
      setMaxEnrollments: (maxEnrollments) =>
        setState((prev) => ({ ...prev, enrollment: { ...prev.enrollment, maxEnrollments } })),
      setEnrollmentDeadline: (deadline) =>
        setState((prev) => ({ ...prev, enrollment: { ...prev.enrollment, deadline } })),
      setEstimatedHours: (hours) => setState((prev) => ({ ...prev, estimatedHours: Math.max(1, hours) })),
      addTag: (tag) =>
        setState((prev) => {
          const normalized = tag.trim();
          if (!normalized || prev.tags.includes(normalized)) return prev;
          return { ...prev, tags: [...prev.tags, normalized] };
        }),
      removeTag: (tag) => setState((prev) => ({ ...prev, tags: prev.tags.filter((item) => item !== tag) })),
      setStatus: (status) => setState((prev) => ({ ...prev, status })),
      dispatch: (action) => setState((prev) => applyContentAction(prev, action)),
      setDeliveryMode: (mode) =>
        setState((prev) => ({
          ...prev,
          delivery: { ...prev.delivery, mode },
        })),
      setAccessWindow: (window) => setState((prev) => ({ ...prev, accessWindow: window })),
      setEnrollmentWindow: (window) => setState((prev) => ({ ...prev, enrollmentWindow: window })),
      addSession: (sessionData) =>
        setState((prev) => ({
          ...prev,
          delivery: {
            ...prev.delivery,
            sessions: [...prev.delivery.sessions, { ...sessionData, id: createId('session') }],
          },
        })),
      removeSession: (sessionId) =>
        setState((prev) => ({
          ...prev,
          delivery: {
            ...prev.delivery,
            sessions: prev.delivery.sessions.filter((session) => session.id !== sessionId),
          },
        })),
      setTimezone: (timezone) =>
        setState((prev) => ({
          ...prev,
          delivery: { ...prev.delivery, timezone },
        })),
      validate: () => {
        const errors: Record<string, string> = {};
        if (!state.title.trim()) errors.title = 'Course title is required';
        if (!state.slug.trim()) errors.slug = 'Course slug is required';
        if (!state.summary.trim()) errors.summary = 'Course summary is required';
        if (!state.category.trim()) errors.category = 'Course category is required';

        const isValid = Object.keys(errors).length === 0;
        setState((prev) => ({ ...prev, isValid, errors }));

        return { isValid, errors: Object.values(errors) };
      },
    }),
    [state],
  );

  return <CourseEditorContext.Provider value={value}>{children}</CourseEditorContext.Provider>;
}

export function useCourseEditor() {
  const context = useContext(CourseEditorContext);
  if (context === undefined) {
    throw new Error('useCourseEditor must be used within a CourseEditorProvider');
  }
  return context;
}
