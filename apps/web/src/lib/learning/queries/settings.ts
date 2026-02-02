import { cache } from 'react';

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
  void courseId;
  return null;
});

/**
 * Fetch course notification settings.
 * Cache: revalidate 300s (stable)
 */
export const getCourseNotificationSettings = cache(async (courseId: string): Promise<CourseNotificationSettings | null> => {
  void courseId;
  return null;
});

/**
 * Fetch course integration settings.
 * Cache: revalidate 300s (stable)
 */
export const getCourseIntegrationSettings = cache(async (courseId: string): Promise<CourseIntegrationSettings | null> => {
  void courseId;
  return null;
});
