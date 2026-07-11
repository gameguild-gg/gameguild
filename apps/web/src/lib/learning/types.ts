// =============================================================================
// LEARNING MODULE TYPES
// =============================================================================
// Pure type definitions — NO runtime imports. Safe to import from 'use client'
// components without pulling in server-only modules (auth, next/headers).
// =============================================================================

export type LearningCoursesProgramContentType =
  | 'Module'
  | 'Lesson'
  | 'Assignment'
  | 'Questionnaire'
  | 'Discussion'
  | 'Code'
  | 'Reflection'
  | 'Survey'
  | 'Project';

/**
 * Course delivery mode - determines available features and routes
 */
export type CourseDeliveryMode =
  | 'on-demand'
  | 'live'
  | 'presential'
  | 'hybrid';

/**
 * Course pricing model
 */
export type CoursePricingModel =
  | 'free'
  | 'paid'
  | 'subscription'
  | 'freemium';

/**
 * Feature flags derived from delivery mode and pricing
 */
export interface CourseFeatures {
  hasClasses: boolean;
  hasRecordings: boolean;
  hasSchedule: boolean;
  hasOnDemandContent: boolean;
  hasPricing: boolean;
  hasCertificate: boolean;
  hasAssessments: boolean;
  hasDiscussions: boolean;
}

/**
 * Course full details
 */
export interface CourseDetails {
  id: string;
  creatorId: string | null;
  creatorHandle: string | null;
  title: string;
  description: string;
  metadata: string | null;
  slug: string;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'unlisted';
  thumbnail: string | null;
  videoShowcaseUrl: string | null;
  estimatedHours: number | null;
  category: string;
  difficulty: string;
  skillsRequired: string | null;
  skillsProvided: string | null;
  enrollmentStatus: string;
  maxEnrollments: number | null;
  enrollmentDeadline: string | null;
  currentEnrollments: number;
  averageRating: number;
  totalRatings: number;
  isEnrollmentOpen: boolean;
  deliveryMode: CourseDeliveryMode;
  pricingModel: CoursePricingModel;
  features: CourseFeatures;
  createdAt: string;
  updatedAt: string;
}

/**
 * Course analytics raw data
 */
export interface CourseAnalytics {
  totalUsers: number;
  activeUsers: number;
  completedUsers: number;
  completionRate: number;
  averageCompletionTime: string | null;
  totalViews: number;
  lastActivity: string | null;
  enrollments: Array<{
    id: string;
    enrolledAt: string;
    completedAt: string | null;
    progress: number;
  }>;
  ratings: Array<{
    score: number;
    createdAt: string;
  }>;
  revenue: Array<{
    amount: number;
    currency: string;
    createdAt: string;
  }>;
}

/**
 * Content item for tree rendering
 */
export interface ContentItem {
  id: string;
  parentId: string | null;
  order: number;
  type: LearningCoursesProgramContentType;
  title: string;
  description: string | null;
  status: 'draft' | 'published' | 'archived';
  duration: number | null;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/**
 * Course content (flat list for tree rendering)
 */
export interface CourseContent {
  items: ContentItem[];
  total: number;
}

/**
 * Content item detail (full data for editing)
 */
export interface ContentItemDetail extends ContentItem {
  content: string | null;
  settings: Record<string, unknown>;
}

/**
 * Course student data
 */
export interface CourseStudents {
  students: Array<{
    id: string;
    userId: string;
    name: string;
    email: string;
    enrolledAt: string;
    progress: number;
    completedAt: string | null;
    lastActivity: string;
  }>;
  total: number;
}
