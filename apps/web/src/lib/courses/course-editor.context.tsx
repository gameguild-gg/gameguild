'use client';

import React, { createContext, Dispatch, ReactNode, useContext, useReducer } from 'react';
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
  enrollmentWindow?: {
    opensAt?: Date;
    closesAt?: Date;
  };
}

export interface CourseSession {
  id: string;
  title: string;
  startDate: Date;
  endDate: Date;
  location?: {
    type: 'physical' | 'virtual';
    address?: string;
    roomDetails?: string;
    meetingUrl?: string;
  };
  capacity: number;
  enrolled: number;
  isRecurring?: boolean;
  recurringPattern?: {
    frequency: 'weekly' | 'biweekly' | 'monthly';
    dayOfWeek: number;
    endDate?: Date;
  };
}

export interface CourseDelivery {
  mode: 'online' | 'live' | 'hybrid' | 'offline';
  accessWindow?: {
    startDate?: Date;
    endDate?: Date;
  };
  sessions: CourseSession[];
  timezone?: string;
}

export interface CourseSkill {
  id: string;
  name: string;
  category: string;
  level: 'beginner' | 'intermediate' | 'advanced' | 'expert';
  description?: string;
}

export interface CourseCertificate {
  id: string;
  name: string;
  description: string;
  imageUrl?: string;
  requirements: {
    completionPercentage: number;
    passingGrade?: number;
    requiredSkills: string[];
  };
  template?: string;
}

export interface CourseCertificates {
  skillsRequired: CourseSkill[];
  skillsProvided: CourseSkill[];
  certificates: CourseCertificate[];
}

export interface CourseSEO {
  metaTitle?: string;
  metaDescription?: string;
  keywords: string[];
  ogTitle?: string;
  ogDescription?: string;
  ogImage?: string;
  twitterTitle?: string;
  twitterDescription?: string;
  twitterImage?: string;
  canonicalUrl?: string;
  structuredData?: Record<string, unknown>;
}

export interface CourseMetadata {
  seo: CourseSEO;
  customFields: Record<string, unknown>;
  analytics?: {
    trackingId?: string;
    events?: string[];
  };
}

export interface CourseVersion {
  id: string;
  version: string;
  changes: string;
  createdAt: Date;
  createdBy: string;
  snapshot: Partial<CourseEditorData>;
}

export interface CoursePublishSettings {
  autoPublish: boolean;
  publishAt?: Date;
  visibility: 'public' | 'private' | 'unlisted';
  accessType: 'free' | 'paid' | 'enrollment';
  requireApproval: boolean;
  notifications: {
    email: boolean;
    slack: boolean;
    webhooks: string[];
  };
}

export interface CoursePreview {
  mode: 'desktop' | 'tablet' | 'mobile';
  theme: 'light' | 'dark' | 'auto';
  showComments: boolean;
  showAnalytics: boolean;
  livePreview: boolean;
}

// Content Structure Types
export interface CourseLesson {
  id: string;
  title: string;
  description: string;
  type: 'text' | 'video' | 'quiz' | 'assignment' | 'file' | 'interactive';
  content: {
    text?: string;
    videoUrl?: string;
    videoEmbedId?: string;
    videoPlatform?: 'youtube' | 'vimeo' | 'direct';
    files?: Array<{
      id: string;
      name: string;
      url: string;
      type: string;
      size: number;
    }>;
    quiz?: {
      questions: Array<{
        id: string;
        type: 'multiple-choice' | 'true-false' | 'short-answer' | 'essay';
        question: string;
        options?: string[];
        correctAnswer?: string | number;
        explanation?: string;
      }>;
      passingScore: number;
      timeLimit?: number;
    };
    assignment?: {
      instructions: string;
      submissionType: 'text' | 'file' | 'link';
      dueDate?: Date;
      maxScore: number;
      rubric?: Array<{
        criteria: string;
        points: number;
        description: string;
      }>;
    };
  };
  duration: number; // in minutes
  sortOrder: number;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'premium';
  isRequired: boolean;
  prerequisites: string[]; // lesson IDs
  completionCriteria: {
    type: 'view' | 'quiz-pass' | 'assignment-submit' | 'time-spent';
    threshold?: number;
  };
  metadata: {
    estimatedReadTime?: number;
    difficulty?: 'easy' | 'medium' | 'hard';
    tags: string[];
    lastModified: Date;
    modifiedBy: string;
  };
  isSelected?: boolean; // for bulk operations
}

export interface CourseModule {
  id: string;
  title: string;
  description: string;
  sortOrder: number;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'premium';
  lessons: CourseLesson[];
  submodules: CourseModule[];
  isExpanded?: boolean; // for tree view
  isSelected?: boolean; // for bulk operations
  estimatedDuration: number; // calculated from lessons
  completionRate?: number; // for reporting
  metadata: {
    createdAt: Date;
    updatedAt: Date;
    createdBy: string;
    lastModified: Date;
    modifiedBy: string;
  };
}

export interface CourseContent {
  modules: CourseModule[];
  selectedItems: string[]; // for bulk operations
  bulkEditMode: boolean;
  dragDropEnabled: boolean;
  treeViewCollapsed: boolean;
  currentEditingLesson?: string;
  lessonEditHistory: Array<{
    lessonId: string;
    action: string;
    timestamp: Date;
    previousState: unknown;
    newState: unknown;
  }>;
}

// Undo/Redo System
export interface ActionHistoryItem {
  id: string;
  type: string;
  description: string;
  timestamp: Date;
  previousState: unknown;
  newState: unknown;
  affectedFields: string[];
}

export interface UndoRedoState {
  history: ActionHistoryItem[];
  currentIndex: number;
  maxHistorySize: number;
  canUndo: boolean;
  canRedo: boolean;
}

export interface CoursePublishing {
  settings: CoursePublishSettings;
  versions: CourseVersion[];
  currentVersion: string;
  preview: CoursePreview;
  lastPublishedAt?: Date;
  publishingStatus: 'idle' | 'publishing' | 'published' | 'failed';
  publishingError?: string;
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

  // Delivery & Schedule
  delivery: CourseDelivery;

  // Certificates & Skills
  certificates: CourseCertificates;

  // SEO & Metadata
  metadata: CourseMetadata;

  // Publishing & Preview
  publishing: CoursePublishing;

  // Content Structure
  content: CourseContent;

  // Undo/Redo System
  undoRedo: UndoRedoState;

  // Metadata
  tags: string[];
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
  | { type: 'SET_FIELD'; field: keyof CourseEditorData; value: unknown }
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
  | { type: 'LOAD_COURSE'; course: Partial<CourseEditorData> }
  // Delivery & Schedule Actions
  | { type: 'SET_DELIVERY_MODE'; mode: CourseDelivery['mode'] }
  | { type: 'SET_ACCESS_WINDOW'; window: CourseDelivery['accessWindow'] }
  | { type: 'SET_ENROLLMENT_WINDOW'; window: CourseEnrollment['enrollmentWindow'] }
  | { type: 'ADD_SESSION'; session: Omit<CourseSession, 'id'> }
  | { type: 'UPDATE_SESSION'; sessionId: string; updates: Partial<CourseSession> }
  | { type: 'REMOVE_SESSION'; sessionId: string }
  | { type: 'SET_TIMEZONE'; timezone: string }
  // Certificates & Skills Actions
  | { type: 'ADD_REQUIRED_SKILL'; skill: CourseSkill }
  | { type: 'REMOVE_REQUIRED_SKILL'; skillId: string }
  | { type: 'ADD_PROVIDED_SKILL'; skill: CourseSkill }
  | { type: 'REMOVE_PROVIDED_SKILL'; skillId: string }
  | { type: 'REORDER_REQUIRED_SKILLS'; skillIds: string[] }
  | { type: 'REORDER_PROVIDED_SKILLS'; skillIds: string[] }
  | { type: 'ADD_CERTIFICATE'; certificate: CourseCertificate }
  | { type: 'REMOVE_CERTIFICATE'; certificateId: string }
  | { type: 'UPDATE_CERTIFICATE'; certificateId: string; updates: Partial<CourseCertificate> }
  // SEO & Metadata Actions
  | { type: 'SET_META_TITLE'; title: string }
  | { type: 'SET_META_DESCRIPTION'; description: string }
  | { type: 'ADD_SEO_KEYWORD'; keyword: string }
  | { type: 'REMOVE_SEO_KEYWORD'; keyword: string }
  | { type: 'SET_SEO_KEYWORDS'; keywords: string[] }
  | { type: 'SET_OG_DATA'; field: 'ogTitle' | 'ogDescription' | 'ogImage'; value: string }
  | { type: 'SET_TWITTER_DATA'; field: 'twitterTitle' | 'twitterDescription' | 'twitterImage'; value: string }
  | { type: 'SET_CANONICAL_URL'; url: string }
  | { type: 'SET_CUSTOM_FIELD'; key: string; value: unknown }
  | { type: 'REMOVE_CUSTOM_FIELD'; key: string }
  // Publishing & Preview Actions
  | { type: 'SET_PUBLISH_SETTINGS'; settings: Partial<CoursePublishSettings> }
  | { type: 'SET_VISIBILITY'; visibility: CoursePublishSettings['visibility'] }
  | { type: 'SET_ACCESS_TYPE'; accessType: CoursePublishSettings['accessType'] }
  | { type: 'TOGGLE_AUTO_PUBLISH' }
  | { type: 'SET_PUBLISH_DATE'; date: Date | undefined }
  | { type: 'CREATE_VERSION'; changes: string; createdBy: string }
  | { type: 'RESTORE_VERSION'; versionId: string }
  | { type: 'DELETE_VERSION'; versionId: string }
  | { type: 'SET_PREVIEW_MODE'; mode: CoursePreview['mode'] }
  | { type: 'SET_PREVIEW_THEME'; theme: CoursePreview['theme'] }
  | { type: 'TOGGLE_LIVE_PREVIEW' }
  | { type: 'PUBLISH_COURSE' }
  | { type: 'UNPUBLISH_COURSE' }
  | { type: 'SET_PUBLISHING_STATUS'; status: CoursePublishing['publishingStatus']; error?: string }
  // Content Structure Actions
  | { type: 'ADD_MODULE'; module: Omit<CourseModule, 'id' | 'metadata'> }
  | { type: 'UPDATE_MODULE'; moduleId: string; updates: Partial<CourseModule> }
  | { type: 'REMOVE_MODULE'; moduleId: string }
  | { type: 'REORDER_MODULES'; moduleIds: string[] }
  | { type: 'TOGGLE_MODULE_EXPANDED'; moduleId: string }
  | { type: 'ADD_SUBMODULE'; parentModuleId: string; submodule: Omit<CourseModule, 'id' | 'metadata'> }
  | { type: 'ADD_LESSON'; moduleId: string; lesson: Omit<CourseLesson, 'id' | 'metadata'> }
  | { type: 'UPDATE_LESSON'; lessonId: string; updates: Partial<CourseLesson> }
  | { type: 'REMOVE_LESSON'; lessonId: string }
  | { type: 'REORDER_LESSONS'; moduleId: string; lessonIds: string[] }
  | { type: 'DUPLICATE_LESSON'; lessonId: string }
  | { type: 'MOVE_LESSON'; lessonId: string; targetModuleId: string; targetIndex: number }
  | { type: 'SET_LESSON_CONTENT'; lessonId: string; content: CourseLesson['content'] }
  | { type: 'SET_LESSON_PREREQUISITES'; lessonId: string; prerequisites: string[] }
  | { type: 'TOGGLE_LESSON_VISIBILITY'; lessonId: string }
  | { type: 'SET_LESSON_STATUS'; lessonId: string; status: CourseLesson['status'] }
  // Bulk Operations
  | { type: 'SELECT_ITEM'; itemId: string; itemType: 'module' | 'lesson' }
  | { type: 'DESELECT_ITEM'; itemId: string; itemType: 'module' | 'lesson' }
  | { type: 'SELECT_ALL_ITEMS'; itemType: 'module' | 'lesson' }
  | { type: 'DESELECT_ALL_ITEMS' }
  | { type: 'BULK_DELETE'; itemIds: string[]; itemType: 'module' | 'lesson' }
  | { type: 'BULK_UPDATE_STATUS'; itemIds: string[]; status: 'draft' | 'published' | 'archived' }
  | { type: 'BULK_UPDATE_VISIBILITY'; itemIds: string[]; visibility: 'public' | 'private' | 'premium' }
  | { type: 'TOGGLE_BULK_EDIT_MODE' }
  | { type: 'TOGGLE_TREE_VIEW_COLLAPSED' }
  // Undo/Redo Actions
  | { type: 'UNDO' }
  | { type: 'REDO' }
  | { type: 'ADD_TO_HISTORY'; action: ActionHistoryItem }
  | { type: 'CLEAR_HISTORY' };

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
    errors,
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
  delivery: {
    mode: 'online',
    sessions: [],
    timezone: 'UTC',
  },
  certificates: {
    skillsRequired: [],
    skillsProvided: [],
    certificates: [],
  },
  metadata: {
    seo: {
      keywords: [],
    },
    customFields: {},
  },
  publishing: {
    settings: {
      autoPublish: false,
      visibility: 'private',
      accessType: 'paid',
      requireApproval: false,
      notifications: {
        email: true,
        slack: false,
        webhooks: [],
      },
    },
    versions: [],
    currentVersion: '1.0.0',
    preview: {
      mode: 'desktop',
      theme: 'light',
      showComments: false,
      showAnalytics: false,
      livePreview: false,
    },
    publishingStatus: 'idle',
  },
  tags: [],
  status: 'draft',
  isValid: false,
  errors: {},
  manualSlugEdit: false,

  // Content Structure
  content: {
    modules: [],
    selectedItems: [],
    bulkEditMode: false,
    dragDropEnabled: true,
    treeViewCollapsed: false,
    lessonEditHistory: [],
  },

  // Undo/Redo State
  undoRedo: {
    history: [],
    currentIndex: -1,
    maxHistorySize: 50,
    canUndo: false,
    canRedo: false,
  },
};

// Reducer
export const courseEditorReducer = (state: CourseEditorData, action: CourseEditorAction): CourseEditorData => {
  let newState: CourseEditorData = state;

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
        products: state.products.filter((p) => p.id !== action.productId),
      };
      break;

    case 'UPDATE_PRODUCT':
      newState = {
        ...state,
        products: state.products.map((p) => (p.id === action.productId ? { ...p, ...action.updates } : p)),
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
        tags: state.tags.filter((tag) => tag !== action.tag),
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

    // Delivery & Schedule Actions
    case 'SET_DELIVERY_MODE':
      newState = {
        ...state,
        delivery: {
          ...state.delivery,
          mode: action.mode,
          // Clear sessions if switching from live/offline to online
          sessions: action.mode === 'online' ? [] : state.delivery.sessions,
        },
      };
      break;

    case 'SET_ACCESS_WINDOW':
      newState = {
        ...state,
        delivery: {
          ...state.delivery,
          accessWindow: action.window,
        },
      };
      break;

    case 'SET_ENROLLMENT_WINDOW':
      newState = {
        ...state,
        enrollment: {
          ...state.enrollment,
          enrollmentWindow: action.window,
        },
      };
      break;

    case 'ADD_SESSION':
      newState = {
        ...state,
        delivery: {
          ...state.delivery,
          sessions: [
            ...state.delivery.sessions,
            {
              ...action.session,
              id: `session_${Date.now()}`,
            },
          ],
        },
      };
      break;

    case 'UPDATE_SESSION':
      newState = {
        ...state,
        delivery: {
          ...state.delivery,
          sessions: state.delivery.sessions.map((session) => (session.id === action.sessionId ? { ...session, ...action.updates } : session)),
        },
      };
      break;

    case 'REMOVE_SESSION':
      newState = {
        ...state,
        delivery: {
          ...state.delivery,
          sessions: state.delivery.sessions.filter((session) => session.id !== action.sessionId),
        },
      };
      break;

    case 'SET_TIMEZONE':
      newState = {
        ...state,
        delivery: {
          ...state.delivery,
          timezone: action.timezone,
        },
      };
      break;

    // Certificates & Skills Actions
    case 'ADD_REQUIRED_SKILL':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          skillsRequired: [...state.certificates.skillsRequired, action.skill],
        },
      };
      break;

    case 'REMOVE_REQUIRED_SKILL':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          skillsRequired: state.certificates.skillsRequired.filter((skill) => skill.id !== action.skillId),
        },
      };
      break;

    case 'ADD_PROVIDED_SKILL':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          skillsProvided: [...state.certificates.skillsProvided, action.skill],
        },
      };
      break;

    case 'REMOVE_PROVIDED_SKILL':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          skillsProvided: state.certificates.skillsProvided.filter((skill) => skill.id !== action.skillId),
        },
      };
      break;

    case 'REORDER_REQUIRED_SKILLS':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          skillsRequired: action.skillIds.map((id) => state.certificates.skillsRequired.find((skill) => skill.id === id)!),
        },
      };
      break;

    case 'REORDER_PROVIDED_SKILLS':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          skillsProvided: action.skillIds.map((id) => state.certificates.skillsProvided.find((skill) => skill.id === id)!),
        },
      };
      break;

    case 'ADD_CERTIFICATE':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          certificates: [...state.certificates.certificates, action.certificate],
        },
      };
      break;

    case 'REMOVE_CERTIFICATE':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          certificates: state.certificates.certificates.filter((cert) => cert.id !== action.certificateId),
        },
      };
      break;

    case 'UPDATE_CERTIFICATE':
      newState = {
        ...state,
        certificates: {
          ...state.certificates,
          certificates: state.certificates.certificates.map((cert) => (cert.id === action.certificateId ? { ...cert, ...action.updates } : cert)),
        },
      };
      break;

    // SEO & Metadata Actions
    case 'SET_META_TITLE':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            metaTitle: action.title,
          },
        },
      };
      break;

    case 'SET_META_DESCRIPTION':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            metaDescription: action.description,
          },
        },
      };
      break;

    case 'ADD_SEO_KEYWORD':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            keywords: [...state.metadata.seo.keywords, action.keyword],
          },
        },
      };
      break;

    case 'REMOVE_SEO_KEYWORD':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            keywords: state.metadata.seo.keywords.filter((keyword) => keyword !== action.keyword),
          },
        },
      };
      break;

    case 'SET_SEO_KEYWORDS':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            keywords: action.keywords,
          },
        },
      };
      break;

    case 'SET_OG_DATA':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            [action.field]: action.value,
          },
        },
      };
      break;

    case 'SET_TWITTER_DATA':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            [action.field]: action.value,
          },
        },
      };
      break;

    case 'SET_CANONICAL_URL':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          seo: {
            ...state.metadata.seo,
            canonicalUrl: action.url,
          },
        },
      };
      break;

    case 'SET_CUSTOM_FIELD':
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          customFields: {
            ...state.metadata.customFields,
            [action.key]: action.value,
          },
        },
      };
      break;

    case 'REMOVE_CUSTOM_FIELD': {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { [action.key]: _, ...remainingFields } = state.metadata.customFields;
      newState = {
        ...state,
        metadata: {
          ...state.metadata,
          customFields: remainingFields,
        },
      };
      break;
    }

    // Publishing & Preview Actions
    case 'SET_PUBLISH_SETTINGS':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          settings: {
            ...state.publishing.settings,
            ...action.settings,
          },
        },
      };
      break;

    case 'SET_VISIBILITY':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          settings: {
            ...state.publishing.settings,
            visibility: action.visibility,
          },
        },
      };
      break;

    case 'SET_ACCESS_TYPE':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          settings: {
            ...state.publishing.settings,
            accessType: action.accessType,
          },
        },
      };
      break;

    case 'TOGGLE_AUTO_PUBLISH':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          settings: {
            ...state.publishing.settings,
            autoPublish: !state.publishing.settings.autoPublish,
          },
        },
      };
      break;

    case 'SET_PUBLISH_DATE':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          settings: {
            ...state.publishing.settings,
            publishAt: action.date,
          },
        },
      };
      break;

    case 'CREATE_VERSION': {
      const newVersion: CourseVersion = {
        id: `version_${Date.now()}`,
        version: `${state.publishing.versions.length + 1}.0.0`,
        changes: action.changes,
        createdAt: new Date(),
        createdBy: action.createdBy,
        snapshot: { ...state },
      };
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          versions: [...state.publishing.versions, newVersion],
          currentVersion: newVersion.version,
        },
      };
      break;
    }

    case 'RESTORE_VERSION': {
      const version = state.publishing.versions.find((v) => v.id === action.versionId);
      if (version && version.snapshot) {
        newState = {
          ...version.snapshot,
          publishing: {
            ...state.publishing,
            currentVersion: version.version,
          },
          updatedAt: new Date(),
        } as CourseEditorData;
      } else {
        newState = state;
      }
      break;
    }

    case 'DELETE_VERSION':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          versions: state.publishing.versions.filter((v) => v.id !== action.versionId),
        },
      };
      break;

    case 'SET_PREVIEW_MODE':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          preview: {
            ...state.publishing.preview,
            mode: action.mode,
          },
        },
      };
      break;

    case 'SET_PREVIEW_THEME':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          preview: {
            ...state.publishing.preview,
            theme: action.theme,
          },
        },
      };
      break;

    case 'TOGGLE_LIVE_PREVIEW':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          preview: {
            ...state.publishing.preview,
            livePreview: !state.publishing.preview.livePreview,
          },
        },
      };
      break;

    case 'PUBLISH_COURSE':
      newState = {
        ...state,
        status: 'published',
        publishedAt: new Date(),
        publishing: {
          ...state.publishing,
          publishingStatus: 'publishing',
          lastPublishedAt: new Date(),
        },
      };
      break;

    case 'UNPUBLISH_COURSE':
      newState = {
        ...state,
        status: 'draft',
        publishing: {
          ...state.publishing,
          publishingStatus: 'idle',
        },
      };
      break;

    case 'SET_PUBLISHING_STATUS':
      newState = {
        ...state,
        publishing: {
          ...state.publishing,
          publishingStatus: action.status,
          publishingError: action.error,
        },
      };
      break;

    // Content Structure Actions
    case 'ADD_MODULE': {
      const newModule: CourseModule = {
        ...action.module,
        id: crypto.randomUUID(),
        metadata: {
          createdAt: new Date(),
          updatedAt: new Date(),
          createdBy: 'current-user', // TODO: get from auth context
          lastModified: new Date(),
          modifiedBy: 'current-user', // TODO: get from auth context
        },
      };
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: [...state.content.modules, newModule],
        },
      };
      break;
    }

    case 'UPDATE_MODULE': {
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) =>
            module.id === action.moduleId
              ? {
                  ...module,
                  ...action.updates,
                  metadata: {
                    ...module.metadata,
                    updatedAt: new Date(),
                  },
                }
              : module,
          ),
        },
      };
      break;
    }

    case 'REMOVE_MODULE': {
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.filter((module) => module.id !== action.moduleId),
          selectedItems: state.content.selectedItems.filter((id) => id !== action.moduleId),
        },
      };
      break;
    }

    case 'REORDER_MODULES': {
      const reorderedModules = action.moduleIds.map((id) => state.content.modules.find((module) => module.id === id)!).filter(Boolean);
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: reorderedModules,
        },
      };
      break;
    }

    case 'TOGGLE_MODULE_EXPANDED': {
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => (module.id === action.moduleId ? { ...module, isExpanded: !module.isExpanded } : module)),
        },
      };
      break;
    }

    case 'ADD_SUBMODULE': {
      const newSubmodule: CourseModule = {
        ...action.submodule,
        id: crypto.randomUUID(),
        metadata: {
          createdAt: new Date(),
          updatedAt: new Date(),
          createdBy: 'current-user', // TODO: get from auth context
          lastModified: new Date(),
          modifiedBy: 'current-user', // TODO: get from auth context
        },
      };
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) =>
            module.id === action.parentModuleId
              ? {
                  ...module,
                  submodules: [...(module.submodules || []), newSubmodule],
                  metadata: {
                    ...module.metadata,
                    updatedAt: new Date(),
                  },
                }
              : module,
          ),
        },
      };
      break;
    }

    case 'ADD_LESSON': {
      const newLesson: CourseLesson = {
        ...action.lesson,
        id: crypto.randomUUID(),
        metadata: {
          tags: [],
          lastModified: new Date(),
          modifiedBy: 'current-user', // TODO: get from auth context
        },
      };
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) =>
            module.id === action.moduleId
              ? {
                  ...module,
                  lessons: [...(module.lessons || []), newLesson],
                  metadata: {
                    ...module.metadata,
                    updatedAt: new Date(),
                  },
                }
              : module,
          ),
        },
      };
      break;
    }

    case 'UPDATE_LESSON': {
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => ({
            ...module,
            lessons: (module.lessons || []).map((lesson) =>
              lesson.id === action.lessonId
                ? {
                    ...lesson,
                    ...action.updates,
                    metadata: {
                      ...lesson.metadata,
                      updatedAt: new Date(),
                    },
                  }
                : lesson,
            ),
          })),
        },
      };
      break;
    }

    case 'REMOVE_LESSON': {
      newState = {
        ...state,
        content: {
          ...state.content,
          modules: state.content.modules.map((module) => ({
            ...module,
            lessons: (module.lessons || []).filter((lesson) => lesson.id !== action.lessonId),
          })),
          selectedItems: state.content.selectedItems.filter((id) => id !== action.lessonId),
        },
      };
      break;
    }

    case 'DUPLICATE_LESSON': {
      const originalLesson = state.content.modules.flatMap((module) => module.lessons || []).find((lesson) => lesson.id === action.lessonId);

      if (originalLesson) {
        const duplicatedLesson: CourseLesson = {
          ...originalLesson,
          id: crypto.randomUUID(),
          title: `${originalLesson.title} (Copy)`,
          metadata: {
            tags: originalLesson.metadata.tags,
            lastModified: new Date(),
            modifiedBy: 'current-user', // TODO: get from auth context
          },
        };

        const moduleWithOriginal = state.content.modules.find((module) => (module.lessons || []).some((lesson) => lesson.id === action.lessonId));

        if (moduleWithOriginal) {
          newState = {
            ...state,
            content: {
              ...state.content,
              modules: state.content.modules.map((module) =>
                module.id === moduleWithOriginal.id
                  ? {
                      ...module,
                      lessons: [...(module.lessons || []), duplicatedLesson],
                      metadata: {
                        ...module.metadata,
                        updatedAt: new Date(),
                      },
                    }
                  : module,
              ),
            },
          };
        }
      }
      break;
    }

    case 'TOGGLE_BULK_EDIT_MODE': {
      newState = {
        ...state,
        content: {
          ...state.content,
          bulkEditMode: !state.content.bulkEditMode,
          selectedItems: [],
        },
      };
      break;
    }

    case 'SELECT_ITEM': {
      const itemId = action.itemId;
      newState = {
        ...state,
        content: {
          ...state.content,
          selectedItems: state.content.selectedItems.includes(itemId) ? state.content.selectedItems : [...state.content.selectedItems, itemId],
        },
      };
      break;
    }

    case 'DESELECT_ITEM': {
      newState = {
        ...state,
        content: {
          ...state.content,
          selectedItems: state.content.selectedItems.filter((id) => id !== action.itemId),
        },
      };
      break;
    }

    case 'DESELECT_ALL_ITEMS': {
      newState = {
        ...state,
        content: {
          ...state.content,
          selectedItems: [],
        },
      };
      break;
    }

    case 'TOGGLE_TREE_VIEW_COLLAPSED': {
      newState = {
        ...state,
        content: {
          ...state.content,
          treeViewCollapsed: !state.content.treeViewCollapsed,
        },
      };
      break;
    }

    // Undo/Redo Actions
    case 'UNDO': {
      if (state.undoRedo.canUndo && state.undoRedo.currentIndex >= 0) {
        const previousAction = state.undoRedo.history[state.undoRedo.currentIndex];
        newState = {
          ...(previousAction.previousState as CourseEditorData),
          undoRedo: {
            ...state.undoRedo,
            currentIndex: state.undoRedo.currentIndex - 1,
            canUndo: state.undoRedo.currentIndex - 1 >= 0,
            canRedo: true,
          },
        };
      }
      break;
    }

    case 'REDO': {
      if (state.undoRedo.canRedo && state.undoRedo.currentIndex < state.undoRedo.history.length - 1) {
        const nextAction = state.undoRedo.history[state.undoRedo.currentIndex + 1];
        newState = {
          ...(nextAction.newState as CourseEditorData),
          undoRedo: {
            ...state.undoRedo,
            currentIndex: state.undoRedo.currentIndex + 1,
            canUndo: true,
            canRedo: state.undoRedo.currentIndex + 1 < state.undoRedo.history.length - 1,
          },
        };
      }
      break;
    }

    case 'ADD_TO_HISTORY': {
      const newHistory = [...state.undoRedo.history];

      // Remove any future history if we're not at the end
      if (state.undoRedo.currentIndex < newHistory.length - 1) {
        newHistory.splice(state.undoRedo.currentIndex + 1);
      }

      // Add new action to history
      newHistory.push(action.action);

      // Limit history size
      if (newHistory.length > state.undoRedo.maxHistorySize) {
        newHistory.shift();
      }

      newState = {
        ...state,
        undoRedo: {
          ...state.undoRedo,
          history: newHistory,
          currentIndex: newHistory.length - 1,
          canUndo: newHistory.length > 0,
          canRedo: false,
        },
      };
      break;
    }

    case 'CLEAR_HISTORY': {
      newState = {
        ...state,
        undoRedo: {
          history: [],
          currentIndex: -1,
          maxHistorySize: 50,
          canUndo: false,
          canRedo: false,
        },
      };
      break;
    }

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

  // Delivery & Schedule methods
  setDeliveryMode: (mode: CourseDelivery['mode']) => void;
  setAccessWindow: (window: CourseDelivery['accessWindow']) => void;
  setEnrollmentWindow: (window: CourseEnrollment['enrollmentWindow']) => void;
  addSession: (session: Omit<CourseSession, 'id'>) => void;
  updateSession: (sessionId: string, updates: Partial<CourseSession>) => void;
  removeSession: (sessionId: string) => void;
  setTimezone: (timezone: string) => void;

  // Certificates & Skills methods
  addRequiredSkill: (skill: CourseSkill) => void;
  removeRequiredSkill: (skillId: string) => void;
  addProvidedSkill: (skill: CourseSkill) => void;
  removeProvidedSkill: (skillId: string) => void;
  reorderRequiredSkills: (skillIds: string[]) => void;
  reorderProvidedSkills: (skillIds: string[]) => void;
  addCertificate: (certificate: CourseCertificate) => void;
  removeCertificate: (certificateId: string) => void;
  updateCertificate: (certificateId: string, updates: Partial<CourseCertificate>) => void;

  // SEO & Metadata methods
  setMetaTitle: (title: string) => void;
  setMetaDescription: (description: string) => void;
  addSEOKeyword: (keyword: string) => void;
  removeSEOKeyword: (keyword: string) => void;
  setSEOKeywords: (keywords: string[]) => void;
  setOGData: (field: 'ogTitle' | 'ogDescription' | 'ogImage', value: string) => void;
  setTwitterData: (field: 'twitterTitle' | 'twitterDescription' | 'twitterImage', value: string) => void;
  setCanonicalUrl: (url: string) => void;
  setCustomField: (key: string, value: any) => void;
  removeCustomField: (key: string) => void;

  // Publishing & Preview methods
  setPublishSettings: (settings: Partial<CoursePublishSettings>) => void;
  setVisibility: (visibility: CoursePublishSettings['visibility']) => void;
  setAccessType: (accessType: CoursePublishSettings['accessType']) => void;
  toggleAutoPublish: () => void;
  setPublishDate: (date: Date | undefined) => void;
  createVersion: (changes: string, createdBy: string) => void;
  restoreVersion: (versionId: string) => void;
  deleteVersion: (versionId: string) => void;
  setPreviewMode: (mode: CoursePreview['mode']) => void;
  setPreviewTheme: (theme: CoursePreview['theme']) => void;
  toggleLivePreview: () => void;
  publishCourse: () => void;
  unpublishCourse: () => void;
  setPublishingStatus: (status: CoursePublishing['publishingStatus'], error?: string) => void;

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

export const CourseEditorProvider: React.FC<CourseEditorProviderProps> = ({ children, initialCourse }) => {
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
    updateProduct: (productId: string, updates: Partial<CourseProduct>) => dispatch({ type: 'UPDATE_PRODUCT', productId, updates }),
    setEnrollmentStatus: (isOpen: boolean) => dispatch({ type: 'SET_ENROLLMENT_STATUS', isOpen }),
    setMaxEnrollments: (max: number | undefined) => dispatch({ type: 'SET_MAX_ENROLLMENTS', value: max }),
    setEnrollmentDeadline: (deadline: Date | undefined) =>
      dispatch({
        type: 'SET_ENROLLMENT_DEADLINE',
        value: deadline,
      }),
    setEstimatedHours: (hours: number) => dispatch({ type: 'SET_ESTIMATED_HOURS', value: hours }),
    addTag: (tag: string) => dispatch({ type: 'ADD_TAG', tag }),
    removeTag: (tag: string) => dispatch({ type: 'REMOVE_TAG', tag }),
    setTags: (tags: string[]) => dispatch({ type: 'SET_TAGS', tags }),
    setStatus: (status: CourseEditorData['status']) => dispatch({ type: 'SET_STATUS', status }),
    validate: () => dispatch({ type: 'VALIDATE' }),
    reset: () => dispatch({ type: 'RESET' }),
    loadCourse: (course: Partial<CourseEditorData>) => dispatch({ type: 'LOAD_COURSE', course }),

    // Delivery & Schedule methods
    setDeliveryMode: (mode: CourseDelivery['mode']) => dispatch({ type: 'SET_DELIVERY_MODE', mode }),
    setAccessWindow: (window: CourseDelivery['accessWindow']) => dispatch({ type: 'SET_ACCESS_WINDOW', window }),
    setEnrollmentWindow: (window: CourseEnrollment['enrollmentWindow']) =>
      dispatch({
        type: 'SET_ENROLLMENT_WINDOW',
        window,
      }),
    addSession: (session: Omit<CourseSession, 'id'>) => dispatch({ type: 'ADD_SESSION', session }),
    updateSession: (sessionId: string, updates: Partial<CourseSession>) =>
      dispatch({
        type: 'UPDATE_SESSION',
        sessionId,
        updates,
      }),
    removeSession: (sessionId: string) => dispatch({ type: 'REMOVE_SESSION', sessionId }),
    setTimezone: (timezone: string) => dispatch({ type: 'SET_TIMEZONE', timezone }),

    // Certificates & Skills methods
    addRequiredSkill: (skill: CourseSkill) => dispatch({ type: 'ADD_REQUIRED_SKILL', skill }),
    removeRequiredSkill: (skillId: string) => dispatch({ type: 'REMOVE_REQUIRED_SKILL', skillId }),
    addProvidedSkill: (skill: CourseSkill) => dispatch({ type: 'ADD_PROVIDED_SKILL', skill }),
    removeProvidedSkill: (skillId: string) => dispatch({ type: 'REMOVE_PROVIDED_SKILL', skillId }),
    reorderRequiredSkills: (skillIds: string[]) => dispatch({ type: 'REORDER_REQUIRED_SKILLS', skillIds }),
    reorderProvidedSkills: (skillIds: string[]) => dispatch({ type: 'REORDER_PROVIDED_SKILLS', skillIds }),
    addCertificate: (certificate: CourseCertificate) => dispatch({ type: 'ADD_CERTIFICATE', certificate }),
    removeCertificate: (certificateId: string) => dispatch({ type: 'REMOVE_CERTIFICATE', certificateId }),
    updateCertificate: (certificateId: string, updates: Partial<CourseCertificate>) =>
      dispatch({
        type: 'UPDATE_CERTIFICATE',
        certificateId,
        updates,
      }),

    // SEO & Metadata methods
    setMetaTitle: (title: string) => dispatch({ type: 'SET_META_TITLE', title }),
    setMetaDescription: (description: string) => dispatch({ type: 'SET_META_DESCRIPTION', description }),
    addSEOKeyword: (keyword: string) => dispatch({ type: 'ADD_SEO_KEYWORD', keyword }),
    removeSEOKeyword: (keyword: string) => dispatch({ type: 'REMOVE_SEO_KEYWORD', keyword }),
    setSEOKeywords: (keywords: string[]) => dispatch({ type: 'SET_SEO_KEYWORDS', keywords }),
    setOGData: (field: 'ogTitle' | 'ogDescription' | 'ogImage', value: string) =>
      dispatch({
        type: 'SET_OG_DATA',
        field,
        value,
      }),
    setTwitterData: (field: 'twitterTitle' | 'twitterDescription' | 'twitterImage', value: string) => dispatch({ type: 'SET_TWITTER_DATA', field, value }),
    setCanonicalUrl: (url: string) => dispatch({ type: 'SET_CANONICAL_URL', url }),
    setCustomField: (key: string, value: any) => dispatch({ type: 'SET_CUSTOM_FIELD', key, value }),
    removeCustomField: (key: string) => dispatch({ type: 'REMOVE_CUSTOM_FIELD', key }),

    // Publishing & Preview methods
    setPublishSettings: (settings: Partial<CoursePublishSettings>) =>
      dispatch({
        type: 'SET_PUBLISH_SETTINGS',
        settings,
      }),
    setVisibility: (visibility: CoursePublishSettings['visibility']) =>
      dispatch({
        type: 'SET_VISIBILITY',
        visibility,
      }),
    setAccessType: (accessType: CoursePublishSettings['accessType']) =>
      dispatch({
        type: 'SET_ACCESS_TYPE',
        accessType,
      }),
    toggleAutoPublish: () => dispatch({ type: 'TOGGLE_AUTO_PUBLISH' }),
    setPublishDate: (date: Date | undefined) => dispatch({ type: 'SET_PUBLISH_DATE', date }),
    createVersion: (changes: string, createdBy: string) => dispatch({ type: 'CREATE_VERSION', changes, createdBy }),
    restoreVersion: (versionId: string) => dispatch({ type: 'RESTORE_VERSION', versionId }),
    deleteVersion: (versionId: string) => dispatch({ type: 'DELETE_VERSION', versionId }),
    setPreviewMode: (mode: CoursePreview['mode']) => dispatch({ type: 'SET_PREVIEW_MODE', mode }),
    setPreviewTheme: (theme: CoursePreview['theme']) => dispatch({ type: 'SET_PREVIEW_THEME', theme }),
    toggleLivePreview: () => dispatch({ type: 'TOGGLE_LIVE_PREVIEW' }),
    publishCourse: () => dispatch({ type: 'PUBLISH_COURSE' }),
    unpublishCourse: () => dispatch({ type: 'UNPUBLISH_COURSE' }),
    setPublishingStatus: (status: CoursePublishing['publishingStatus'], error?: string) =>
      dispatch({
        type: 'SET_PUBLISHING_STATUS',
        status,
        error,
      }),

    // Aliases for component compatibility
    updateTitle: (title: string) => dispatch({ type: 'SET_TITLE', value: title }),
    updateSlug: (slug: string) => dispatch({ type: 'SET_SLUG', value: slug }),
    updateDescription: (description: string) => dispatch({ type: 'SET_DESCRIPTION', value: description }),
    updateSummary: (summary: string) => dispatch({ type: 'SET_SUMMARY', value: summary }),
    updateCategory: (category: CourseArea) => dispatch({ type: 'SET_CATEGORY', value: category }),
    updateDifficulty: (difficulty: CourseLevel) => dispatch({ type: 'SET_DIFFICULTY', value: difficulty }),
  };

  return <CourseEditorContext.Provider value={contextValue}>{children}</CourseEditorContext.Provider>;
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
