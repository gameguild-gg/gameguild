import { cache } from 'react';
import { getCourse } from './course';

// =============================================================================
// COURSE SETTINGS QUERIES
// =============================================================================
// Course configuration and settings management.
// =============================================================================

/**
 * Access and enrollment settings
 */
export interface CourseAccessSettings {
  courseId: string;
  
  // Visibility
  visibility: 'public' | 'private' | 'unlisted';
  password?: string;          // For password-protected courses
  
  // Enrollment
  enrollmentType: 'open' | 'approval' | 'invite-only' | 'closed';
  maxEnrollments?: number;
  enrollmentStart?: string;
  enrollmentEnd?: string;
  
  // Access control
  requiresVerification: boolean;
  allowedDomains?: string[];  // e.g., ["company.com"] for corporate courses
  prerequisiteCourses: string[];
  
  // Completion
  completionCriteria: 'all-content' | 'percentage' | 'assessment' | 'manual';
  completionThreshold?: number; // Percentage if criteria = percentage
  
  updatedAt: string;
}

/**
 * Notification settings
 */
export interface CourseNotificationSettings {
  courseId: string;
  
  // Student notifications
  studentNotifications: {
    enrollmentConfirmation: boolean;
    courseUpdates: boolean;
    newContent: boolean;
    upcomingClasses: boolean;      // If hasClasses
    classReminders: number[];       // Minutes before class
    assignmentDue: boolean;
    assessmentResults: boolean;
    certificateReady: boolean;
    discussionReplies: boolean;    // If hasDiscussions
  };
  
  // Instructor notifications
  instructorNotifications: {
    newEnrollment: boolean;
    newReview: boolean;
    supportTicket: boolean;
    discussionMention: boolean;
    lowRating: boolean;            // Alert on ratings below threshold
    lowRatingThreshold: number;
  };
  
  // Email templates (customizable)
  templates: Array<{
    id: string;
    type: string;
    subject: string;
    enabled: boolean;
  }>;
  
  updatedAt: string;
}

/**
 * Integration settings
 */
export interface CourseIntegrationSettings {
  courseId: string;
  
  integrations: Array<{
    id: string;
    type: 'zoom' | 'teams' | 'slack' | 'discord' | 'lti' | 'webhook' | 'zapier';
    name: string;
    enabled: boolean;
    config: Record<string, unknown>;
    lastSyncAt?: string;
    status: 'connected' | 'disconnected' | 'error';
  }>;
  
  // Webhooks
  webhooks: Array<{
    id: string;
    url: string;
    events: string[];
    enabled: boolean;
    secret?: string;
  }>;
  
  updatedAt: string;
}

// =============================================================================
// FETCH FUNCTIONS
// =============================================================================

/**
 * Fetch course access settings.
 * Cache: revalidate 300s (stable)
 */
export const getCourseAccessSettings = cache(async (courseId: string): Promise<CourseAccessSettings | null> => {
  const course = await getCourse(courseId);
  if (!course) return null;

  return {
    courseId,
    visibility: course.visibility,
    enrollmentType: course.enrollmentStatus === 'Open' ? 'open' : course.enrollmentStatus === 'Closed' ? 'closed' : 'approval',
    maxEnrollments: course.maxEnrollments ?? undefined,
    enrollmentEnd: course.enrollmentDeadline ?? undefined,
    requiresVerification: false,
    prerequisiteCourses: [],
    completionCriteria: 'all-content',
    updatedAt: course.updatedAt,
  };
});

/**
 * Fetch course notification settings.
 * Cache: revalidate 300s (stable)
 */
export const getCourseNotificationSettings = cache(async (courseId: string): Promise<CourseNotificationSettings | null> => {
  const course = await getCourse(courseId);
  if (!course) return null;

  return {
    courseId,
    studentNotifications: {
      enrollmentConfirmation: true,
      courseUpdates: true,
      newContent: true,
      upcomingClasses: course.features.hasClasses,
      classReminders: course.features.hasClasses ? [1440, 60, 10] : [],
      assignmentDue: true,
      assessmentResults: course.features.hasAssessments,
      certificateReady: course.features.hasCertificate,
      discussionReplies: course.features.hasDiscussions,
    },
    instructorNotifications: {
      newEnrollment: true,
      newReview: true,
      supportTicket: true,
      discussionMention: true,
      lowRating: true,
      lowRatingThreshold: 3,
    },
    templates: [
      { id: `${courseId}-enrollment`, type: 'enrollment-confirmation', subject: `Welcome to ${course.title}`, enabled: true },
      { id: `${courseId}-certificate`, type: 'certificate-ready', subject: `Your ${course.title} certificate is ready`, enabled: course.features.hasCertificate },
      { id: `${courseId}-discussion`, type: 'discussion-reply', subject: `New reply in ${course.title}`, enabled: course.features.hasDiscussions },
    ],
    updatedAt: course.updatedAt,
  };
});

/**
 * Fetch course integration settings.
 * Cache: revalidate 300s (stable)
 */
export const getCourseIntegrationSettings = cache(async (courseId: string): Promise<CourseIntegrationSettings | null> => {
  const course = await getCourse(courseId);
  if (!course) return null;

  return {
    courseId,
    integrations: [
      {
        id: `${courseId}-video`,
        type: 'webhook',
        name: 'Course media pipeline',
        enabled: Boolean(course.videoShowcaseUrl),
        config: { videoShowcaseUrl: course.videoShowcaseUrl },
        status: course.videoShowcaseUrl ? 'connected' : 'disconnected',
        lastSyncAt: course.updatedAt,
      },
      {
        id: `${courseId}-classes`,
        type: 'zoom',
        name: 'Live class provider',
        enabled: course.features.hasClasses,
        config: {},
        status: course.features.hasClasses ? 'connected' : 'disconnected',
      },
    ],
    webhooks: [],
    updatedAt: course.updatedAt,
  };
});
