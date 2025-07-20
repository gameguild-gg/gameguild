'use client';

import React, { createContext, useContext, useReducer, ReactNode, Dispatch } from 'react';
import { CourseArea, CourseLevel } from '@/components/legacy/types/courses';

// Course Editor Types
export interface CourseMedia {
  thumbnail?: {
    url: string;
    alt: string;
    file?: File;
  };
  showcaseVideo?: {
    url: string;
    platform: 'youtube' | 'vimeo' | 'direct';
    embedId?: string;
  };
}

export interface CourseProduct {
  id: string;
  name: string;
  price: number;
  currency: string;
  type: 'course' | 'bundle' | 'subscription';
  stripeProductId?: string;
}

export interface CourseEnrollment {
  isOpen: boolean;
  maxEnrollments?: number;
  deadline?: Date;
  currentEnrollments: number;
}

export interface CourseEditorData {
  // General Details
  id?: string;
  title: string;
  slug: string;
  description: string;
  summary: string;
  category: CourseArea;
  difficulty: CourseLevel;
  
  // Media
  media: CourseMedia;
  
  // Sales & Showcase
  products: CourseProduct[];
  enrollment: CourseEnrollment;
  estimatedHours: number;
  tags: string[];
  
  // Metadata
  status: 'draft' | 'published' | 'archived';
  publishedAt?: Date;
  createdAt?: Date;
  updatedAt?: Date;
  
  // Validation
  isValid: boolean;
  errors: Record<string, string>;
  
  // UI State
  manualSlugEdit: boolean;
}

// Action Types
export type CourseEditorAction =
  | { type: 'SET_FIELD'; field: keyof CourseEditorData; value: any }
  | { type: 'SET_TITLE'; value: string }
  | { type: 'SET_SLUG'; value: string }
  | { type: 'SET_DESCRIPTION'; value: string }
  | { type: 'SET_SUMMARY'; value: string }
  | { type: 'SET_CATEGORY'; value: CourseArea }
  | { type: 'SET_DIFFICULTY'; value: CourseLevel }
  | { type: 'SET_THUMBNAIL'; value: CourseMedia['thumbnail'] }
  | { type: 'SET_SHOWCASE_VIDEO'; value: CourseMedia['showcaseVideo'] }
  | { type: 'ADD_PRODUCT'; product: CourseProduct }
  | { type: 'REMOVE_PRODUCT'; productId: string }
  | { type: 'UPDATE_PRODUCT'; productId: string; updates: Partial<CourseProduct> }
  | { type: 'SET_ENROLLMENT_STATUS'; isOpen: boolean }
  | { type: 'SET_MAX_ENROLLMENTS'; value: number | undefined }
  | { type: 'SET_ENROLLMENT_DEADLINE'; value: Date | undefined }
  | { type: 'SET_ESTIMATED_HOURS'; value: number }
  | { type: 'ADD_TAG'; tag: string }
  | { type: 'REMOVE_TAG'; tag: string }
  | { type: 'SET_TAGS'; tags: string[] }
  | { type: 'SET_STATUS'; status: CourseEditorData['status'] }
  | { type: 'SET_ERROR'; field: string; error: string }
  | { type: 'CLEAR_ERROR'; field: string }
  | { type: 'VALIDATE' }
  | { type: 'RESET' }
  | { type: 'LOAD_COURSE'; course: Partial<CourseEditorData> };

// Helper Functions
const generateSlug = (title: string): string => {
  return title
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .replace(/\s+/g, '-')
    .replace(/-+/g, '-')
    .trim();
};

const validateCourse = (course: CourseEditorData): { isValid: boolean; errors: Record<string, string> } => {
  const errors: Record<string, string> = {};

  // Required fields
  if (!course.title.trim()) {
    errors.title = 'Title is required';
  }
  
  if (!course.slug.trim()) {
    errors.slug = 'Slug is required';
  } else if (!/^[a-z0-9-]+$/.test(course.slug)) {
    errors.slug = 'Slug can only contain lowercase letters, numbers, and hyphens';
  }
  
  if (!course.description.trim()) {
    errors.description = 'Description is required';
  }
  
  if (!course.summary.trim()) {
    errors.summary = 'Summary is required';
  }
  
  if (course.estimatedHours <= 0) {
    errors.estimatedHours = 'Estimated hours must be greater than 0';
  }
  
  // Enrollment validation
  if (course.enrollment.maxEnrollments && course.enrollment.maxEnrollments <= 0) {
    errors.maxEnrollments = 'Max enrollments must be greater than 0';
  }
  
  if (course.enrollment.deadline && course.enrollment.deadline <= new Date()) {
    errors.enrollmentDeadline = 'Enrollment deadline must be in the future';
  }
  
  // Media validation
  if (course.media.showcaseVideo?.url && !isValidUrl(course.media.showcaseVideo.url)) {
    errors.showcaseVideo = 'Invalid video URL';
  }

  return {
    isValid: Object.keys(errors).length === 0,
    errors
  };
};

const isValidUrl = (url: string): boolean => {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
};

const extractVideoId = (url: string, platform: 'youtube' | 'vimeo'): string | undefined => {
  if (platform === 'youtube') {
    const match = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/)([^&\n?#]+)/);
    return match?.[1];
  } else if (platform === 'vimeo') {
    const match = url.match(/vimeo\.com\/(\d+)/);
    return match?.[1];
  }
  return undefined;
};

// Initial State
const initialState: CourseEditorData = {
  title: '',
  slug: '',
  description: '',
  summary: '',
  category: 'programming' as CourseArea,
  difficulty: 1 as CourseLevel,
  media: {
    thumbnail: undefined,
    showcaseVideo: undefined,
  },
  products: [],
  enrollment: {
    isOpen: true,
    currentEnrollments: 0,
  },
  estimatedHours: 1,
  tags: [],
  status: 'draft',
  isValid: false,
  errors: {},
  manualSlugEdit: false,
};

// Reducer
export const courseEditorReducer = (
  state: CourseEditorData,
  action: CourseEditorAction
): CourseEditorData => {
  let newState: CourseEditorData;

  switch (action.type) {
    case 'SET_FIELD':
      newState = {
        ...state,
        [action.field]: action.value,
      };
      break;

    case 'SET_TITLE':
      newState = {
        ...state,
        title: action.value,
        slug: state.slug === generateSlug(state.title) ? generateSlug(action.value) : state.slug,
      };
      break;

    case 'SET_SLUG':
      newState = {
        ...state,
        slug: action.value,
      };
      break;

    case 'SET_DESCRIPTION':
      newState = {
        ...state,
        description: action.value,
      };
      break;

    case 'SET_SUMMARY':
      newState = {
        ...state,
        summary: action.value,
      };
      break;

    case 'SET_CATEGORY':
      newState = {
        ...state,
        category: action.value,
      };
      break;

    case 'SET_DIFFICULTY':
      newState = {
        ...state,
        difficulty: action.value,
      };
      break;

    case 'SET_THUMBNAIL':
      newState = {
        ...state,
        media: {
          ...state.media,
          thumbnail: action.value,
        },
      };
      break;

    case 'SET_SHOWCASE_VIDEO':
      const video = action.value;
      let embedId: string | undefined;
      
      if (video?.url) {
        if (video.url.includes('youtube.com') || video.url.includes('youtu.be')) {
          embedId = extractVideoId(video.url, 'youtube');
          video.platform = 'youtube';
        } else if (video.url.includes('vimeo.com')) {
          embedId = extractVideoId(video.url, 'vimeo');
          video.platform = 'vimeo';
        } else {
          video.platform = 'direct';
        }
        video.embedId = embedId;
      }

      newState = {
        ...state,
        media: {
          ...state.media,
          showcaseVideo: video,
        },
      };
      break;

    case 'ADD_PRODUCT':
      newState = {
        ...state,
        products: [...state.products, action.product],
      };
      break;

    case 'REMOVE_PRODUCT':
      newState = {
        ...state,
        products: state.products.filter(p => p.id !== action.productId),
      };
      break;

    case 'UPDATE_PRODUCT':
      newState = {
        ...state,
        products: state.products.map(p =>
          p.id === action.productId ? { ...p, ...action.updates } : p
        ),
      };
      break;

    case 'SET_ENROLLMENT_STATUS':
      newState = {
        ...state,
        enrollment: {
          ...state.enrollment,
          isOpen: action.isOpen,
        },
      };
      break;

    case 'SET_MAX_ENROLLMENTS':
      newState = {
        ...state,
        enrollment: {
          ...state.enrollment,
          maxEnrollments: action.value,
        },
      };
      break;

    case 'SET_ENROLLMENT_DEADLINE':
      newState = {
        ...state,
        enrollment: {
          ...state.enrollment,
          deadline: action.value,
        },
      };
      break;

    case 'SET_ESTIMATED_HOURS':
      newState = {
        ...state,
        estimatedHours: Math.max(0.5, action.value),
      };
      break;

    case 'ADD_TAG':
      if (!state.tags.includes(action.tag)) {
        newState = {
          ...state,
          tags: [...state.tags, action.tag],
        };
      } else {
        newState = state;
      }
      break;

    case 'REMOVE_TAG':
      newState = {
        ...state,
        tags: state.tags.filter(tag => tag !== action.tag),
      };
      break;

    case 'SET_TAGS':
      newState = {
        ...state,
        tags: action.tags,
      };
      break;

    case 'SET_STATUS':
      newState = {
        ...state,
        status: action.status,
        publishedAt: action.status === 'published' && !state.publishedAt ? new Date() : state.publishedAt,
      };
      break;

    case 'SET_ERROR':
      newState = {
        ...state,
        errors: {
          ...state.errors,
          [action.field]: action.error,
        },
      };
      break;

    case 'CLEAR_ERROR':
      const { [action.field]: _, ...remainingErrors } = state.errors;
      newState = {
        ...state,
        errors: remainingErrors,
      };
      break;

    case 'VALIDATE':
      const validation = validateCourse(state);
      newState = {
        ...state,
        isValid: validation.isValid,
        errors: validation.errors,
      };
      break;

    case 'RESET':
      newState = { ...initialState };
      break;

    case 'LOAD_COURSE':
      newState = {
        ...initialState,
        ...action.course,
        updatedAt: new Date(),
      };
      break;

    default:
      newState = state;
  }

  // Auto-validate after most actions
  if (action.type !== 'VALIDATE' && action.type !== 'SET_ERROR' && action.type !== 'CLEAR_ERROR') {
    const validation = validateCourse(newState);
    newState.isValid = validation.isValid;
    newState.errors = validation.errors;
    newState.updatedAt = new Date();
  }

  return newState;
};

// Context
interface CourseEditorContextType {
  state: CourseEditorData;
  dispatch: Dispatch<CourseEditorAction>;
  
  // Convenience methods
  setTitle: (title: string) => void;
  setSlug: (slug: string) => void;
  setDescription: (description: string) => void;
  setSummary: (summary: string) => void;
  setCategory: (category: CourseArea) => void;
  setDifficulty: (difficulty: CourseLevel) => void;
  setThumbnail: (thumbnail: CourseMedia['thumbnail']) => void;
  setShowcaseVideo: (video: CourseMedia['showcaseVideo']) => void;
  addProduct: (product: CourseProduct) => void;
  removeProduct: (productId: string) => void;
  updateProduct: (productId: string, updates: Partial<CourseProduct>) => void;
  setEnrollmentStatus: (isOpen: boolean) => void;
  setMaxEnrollments: (max: number | undefined) => void;
  setEnrollmentDeadline: (deadline: Date | undefined) => void;
  setEstimatedHours: (hours: number) => void;
  addTag: (tag: string) => void;
  removeTag: (tag: string) => void;
  setTags: (tags: string[]) => void;
  setStatus: (status: CourseEditorData['status']) => void;
  validate: () => void;
  reset: () => void;
  loadCourse: (course: Partial<CourseEditorData>) => void;
  
  // Aliases for component compatibility
  updateTitle: (title: string) => void;
  updateSlug: (slug: string) => void;
  updateDescription: (description: string) => void;
  updateSummary: (summary: string) => void;
  updateCategory: (category: CourseArea) => void;
  updateDifficulty: (difficulty: CourseLevel) => void;
}

const CourseEditorContext = createContext<CourseEditorContextType | undefined>(undefined);

// Provider Component
interface CourseEditorProviderProps {
  children: ReactNode;
  initialCourse?: Partial<CourseEditorData>;
}

export const CourseEditorProvider: React.FC<CourseEditorProviderProps> = ({
  children,
  initialCourse,
}) => {
  const [state, dispatch] = useReducer(courseEditorReducer, {
    ...initialState,
    ...initialCourse,
  });

  const contextValue: CourseEditorContextType = {
    state,
    dispatch,
    
    // Convenience methods
    setTitle: (title: string) => dispatch({ type: 'SET_TITLE', value: title }),
    setSlug: (slug: string) => dispatch({ type: 'SET_SLUG', value: slug }),
    setDescription: (description: string) => dispatch({ type: 'SET_DESCRIPTION', value: description }),
    setSummary: (summary: string) => dispatch({ type: 'SET_SUMMARY', value: summary }),
    setCategory: (category: CourseArea) => dispatch({ type: 'SET_CATEGORY', value: category }),
    setDifficulty: (difficulty: CourseLevel) => dispatch({ type: 'SET_DIFFICULTY', value: difficulty }),
    setThumbnail: (thumbnail: CourseMedia['thumbnail']) => dispatch({ type: 'SET_THUMBNAIL', value: thumbnail }),
    setShowcaseVideo: (video: CourseMedia['showcaseVideo']) => dispatch({ type: 'SET_SHOWCASE_VIDEO', value: video }),
    addProduct: (product: CourseProduct) => dispatch({ type: 'ADD_PRODUCT', product }),
    removeProduct: (productId: string) => dispatch({ type: 'REMOVE_PRODUCT', productId }),
    updateProduct: (productId: string, updates: Partial<CourseProduct>) => 
      dispatch({ type: 'UPDATE_PRODUCT', productId, updates }),
    setEnrollmentStatus: (isOpen: boolean) => dispatch({ type: 'SET_ENROLLMENT_STATUS', isOpen }),
    setMaxEnrollments: (max: number | undefined) => dispatch({ type: 'SET_MAX_ENROLLMENTS', value: max }),
    setEnrollmentDeadline: (deadline: Date | undefined) => dispatch({ type: 'SET_ENROLLMENT_DEADLINE', value: deadline }),
    setEstimatedHours: (hours: number) => dispatch({ type: 'SET_ESTIMATED_HOURS', value: hours }),
    addTag: (tag: string) => dispatch({ type: 'ADD_TAG', tag }),
    removeTag: (tag: string) => dispatch({ type: 'REMOVE_TAG', tag }),
    setTags: (tags: string[]) => dispatch({ type: 'SET_TAGS', tags }),
    setStatus: (status: CourseEditorData['status']) => dispatch({ type: 'SET_STATUS', status }),
    validate: () => dispatch({ type: 'VALIDATE' }),
    reset: () => dispatch({ type: 'RESET' }),
    loadCourse: (course: Partial<CourseEditorData>) => dispatch({ type: 'LOAD_COURSE', course }),
    
    // Aliases for component compatibility
    updateTitle: (title: string) => dispatch({ type: 'SET_TITLE', value: title }),
    updateSlug: (slug: string) => dispatch({ type: 'SET_SLUG', value: slug }),
    updateDescription: (description: string) => dispatch({ type: 'SET_DESCRIPTION', value: description }),
    updateSummary: (summary: string) => dispatch({ type: 'SET_SUMMARY', value: summary }),
    updateCategory: (category: CourseArea) => dispatch({ type: 'SET_CATEGORY', value: category }),
    updateDifficulty: (difficulty: CourseLevel) => dispatch({ type: 'SET_DIFFICULTY', value: difficulty }),
  };

  return (
    <CourseEditorContext.Provider value={contextValue}>
      {children}
    </CourseEditorContext.Provider>
  );
};

// Hook
export const useCourseEditor = (): CourseEditorContextType => {
  const context = useContext(CourseEditorContext);
  if (!context) {
    throw new Error('useCourseEditor must be used within a CourseEditorProvider');
  }
  return context;
};

// Export types for use in components
