import { cache } from 'react';

// =============================================================================
// SINGLE COURSE DATA QUERIES
// =============================================================================
// All functions are wrapped with React's cache() for request deduplication.
// When layout preloads data, pages calling the same function with same args
// will either get cached data instantly or await the same in-flight promise.
// =============================================================================

/**
 * Course delivery mode - determines available features and routes
 */
export type CourseDeliveryMode =
  | 'on-demand'   // Self-paced, no live sessions
  | 'live'        // Scheduled live sessions (virtual)
  | 'presential'  // In-person classes
  | 'hybrid';     // Mix of live/presential + on-demand content

/**
 * Course pricing model
 */
export type CoursePricingModel =
  | 'free'         // No payment required
  | 'paid'         // One-time purchase
  | 'subscription' // Access via subscription plan
  | 'freemium';    // Free with paid upgrades/certificates

/**
 * Feature flags derived from delivery mode and pricing
 * These determine which subroutes are available
 */
export interface CourseFeatures {
  hasClasses: boolean;        // live, presential, hybrid → true
  hasRecordings: boolean;     // live, hybrid → true (if recordings enabled)
  hasSchedule: boolean;       // live, presential, hybrid → true
  hasOnDemandContent: boolean; // on-demand, hybrid → true
  hasPricing: boolean;        // paid, subscription, freemium → true
  hasCertificate: boolean;    // Configurable per course
  hasAssessments: boolean;    // Configurable per course
  hasDiscussions: boolean;    // Configurable per course
}

/**
 * Course full details
 */
export interface CourseDetails {
  id: string;
  title: string;
  description: string;
  status: 'draft' | 'published' | 'archived';
  visibility: 'public' | 'private' | 'unlisted';
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
 * Content item types for flexible course structure
 */
export type ContentItemType =
  | 'module'
  | 'chapter'
  | 'section'
  | 'lesson'
  | 'video'
  | 'article'
  | 'quiz'
  | 'assessment'
  | 'assignment'
  | 'resource'
  | 'discussion';

/**
 * Content item in the learning sequence (flat list with parent references)
 * Client builds tree structure from parentId relationships
 */
export interface ContentItem {
  id: string;
  parentId: string | null; // null = root level item
  order: number;
  type: ContentItemType;
  title: string;
  description: string | null;
  status: 'draft' | 'published' | 'archived';
  duration: number | null; // minutes, if applicable
  metadata: Record<string, unknown>; // type-specific metadata
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
  content: string | null; // Rich text content for lessons/articles
  settings: Record<string, unknown>; // Type-specific settings
  // Type-specific data loaded based on item.type:
  // - quiz: questions[], passingScore, timeLimit
  // - assignment: rubric, dueDate, maxAttempts
  // - video: videoUrl, transcript, captions
  // - etc.
}

/**
 * Course student data
 */
export interface CourseStudents {
  students: Array<{
    id: string;
    name: string;
    email: string;
    enrolledAt: string;
    progress: number;
    completedAt: string | null;
    lastActivity: string;
  }>;
  total: number;
}

/**
 * Fetch course details.
 *
 * @param courseId - The course ID from route params
 * @returns Course with full details
 *
 * Fetch Type: GraphQL
 * Cache: revalidate 120s, deduplicated via React cache()
 * Endpoint: TBD - GraphQL query `course`
 */
export const getCourse = cache(async (courseId: string): Promise<CourseDetails | null> => {
  // TODO: Implement GraphQL fetch
  // const query = gql`
  //   query Course($courseId: ID!) {
  //     course(id: $courseId) {
  //       id
  //       title
  //       description
  //       status
  //       visibility
  //       createdAt
  //       updatedAt
  //     }
  //   }
  // `;
  // return graphqlClient.request(query, { courseId }, { next: { revalidate: 120 } });

  void courseId; // Suppress unused warning in stub
  return null;
});

/**
 * Fetch course analytics data.
 *
 * @param courseId - The course ID from route params
 * @returns Raw analytics data for computation
 *
 * Fetch Type: GraphQL
 * Cache: revalidate 120s, deduplicated via React cache()
 * Endpoint: TBD - GraphQL query `courseAnalytics`
 *
 * Computed client-side:
 * - enrollmentTrend: groupByDate(enrollments, 'enrolledAt')
 * - completionFunnel: { enrolled, started, completed } stages
 * - ratingDistribution: groupByScore(ratings)
 * - totalRevenue: sum(revenue[].amount)
 * - revenueOverTime: groupByDate(revenue, 'createdAt')
 */
export const getCourseAnalytics = cache(async (courseId: string): Promise<CourseAnalytics> => {
  // TODO: Implement GraphQL fetch
  // const query = gql`
  //   query CourseAnalytics($courseId: ID!) {
  //     courseAnalytics(courseId: $courseId) {
  //       enrollments { id enrolledAt completedAt progress }
  //       ratings { score createdAt }
  //       revenue { amount currency createdAt }
  //     }
  //   }
  // `;
  // return graphqlClient.request(query, { courseId }, { next: { revalidate: 120 } });

  void courseId; // Suppress unused warning in stub
  return { enrollments: [], ratings: [], revenue: [] };
});

/**
 * Fetch course content items (flat list for tree rendering).
 *
 * @param courseId - The course ID from route params
 * @returns Flat list of content items with parent references
 *
 * Fetch Type: REST
 * Cache: revalidate 120s, deduplicated via React cache()
 * Endpoint: GET /api/learning/courses/:courseId/content
 *
 * Client-side: Build tree from parentId relationships for UI rendering
 */
export const getCourseContent = cache(async (courseId: string): Promise<CourseContent> => {
  // TODO: Implement REST fetch
  // const response = await fetch(`${API_BASE_URL}/api/learning/courses/${courseId}/content`, {
  //   next: { revalidate: 120 },
  //   headers: { Authorization: `Bearer ${token}` },
  // });
  // if (!response.ok) throw new Error('Failed to fetch content');
  // return response.json();

  void courseId; // Suppress unused warning in stub
  return { items: [], total: 0 };
});

/**
 * Fetch single content item detail for editing.
 *
 * @param contentId - The content item ID from route params
 * @returns Full content item data including type-specific fields
 *
 * Fetch Type: REST
 * Cache: revalidate 120s, deduplicated via React cache()
 * Endpoint: GET /api/learning/content/:contentId
 */
export const getContentItem = cache(async (contentId: string): Promise<ContentItemDetail | null> => {
  // TODO: Implement REST fetch
  // const response = await fetch(`${API_BASE_URL}/api/learning/content/${contentId}`, {
  //   next: { revalidate: 120 },
  //   headers: { Authorization: `Bearer ${token}` },
  // });
  // if (response.status === 404) return null;
  // if (!response.ok) throw new Error('Failed to fetch content item');
  // return response.json();

  void contentId; // Suppress unused warning in stub
  return null;
});

/**
 * Fetch course students list.
 *
 * @param courseId - The course ID from route params
 * @returns Students enrolled in the course with progress
 *
 * Fetch Type: REST
 * Cache: revalidate 60s, deduplicated via React cache()
 * Endpoint: GET /api/learning/courses/:courseId/students
 */
export const getCourseStudents = cache(async (courseId: string): Promise<CourseStudents> => {
  // TODO: Implement REST fetch
  // const response = await fetch(`${API_BASE_URL}/api/learning/courses/${courseId}/students`, {
  //   next: { revalidate: 60 },
  //   headers: { Authorization: `Bearer ${token}` },
  // });
  // if (!response.ok) throw new Error('Failed to fetch students');
  // return response.json();

  void courseId; // Suppress unused warning in stub
  return { students: [], total: 0 };
});

// =============================================================================
// LIVE / PRESENTIAL COURSE DATA (only applicable when hasClasses = true)
// =============================================================================

/**
 * Class/session status
 */
export type ClassStatus =
  | 'scheduled'  // Upcoming, not started
  | 'live'       // Currently in progress
  | 'completed'  // Finished
  | 'cancelled'  // Was cancelled
  | 'rescheduled'; // Moved to different time

/**
 * A single class/session in a live or presential course
 */
export interface CourseClass {
  id: string;
  title: string;
  description: string;
  status: ClassStatus;
  scheduledAt: string;      // ISO datetime
  duration: number;         // minutes
  timezone: string;         // IANA timezone
  location?: {              // For presential/hybrid
    type: 'physical' | 'virtual' | 'hybrid';
    address?: string;       // Physical location
    roomName?: string;
    meetingUrl?: string;    // Zoom, Teams, etc.
    meetingId?: string;
  };
  instructor?: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  attendeeCount: number;
  maxAttendees?: number;
  recordingUrl?: string;    // Available after class ends (if recorded)
  materials: Array<{
    id: string;
    title: string;
    type: 'slides' | 'document' | 'video' | 'link';
    url: string;
  }>;
  createdAt: string;
  updatedAt: string;
}

/**
 * Course classes list response
 */
export interface CourseClasses {
  classes: CourseClass[];
  total: number;
  upcomingCount: number;
  completedCount: number;
}

/**
 * Single class detail (extended info for editing)
 */
export interface CourseClassDetail extends CourseClass {
  attendees: Array<{
    id: string;
    userId: string;
    userName: string;
    status: 'registered' | 'attended' | 'absent' | 'excused';
    joinedAt?: string;
    leftAt?: string;
  }>;
  settings: {
    allowLateJoin: boolean;
    recordSession: boolean;
    enableChat: boolean;
    enableQA: boolean;
    reminderSchedule: number[]; // minutes before class to send reminders
  };
}

/**
 * Fetch course classes/sessions.
 *
 * @param courseId - The course ID from route params
 * @returns List of scheduled and past classes
 *
 * Fetch Type: REST
 * Cache: revalidate 60s (volatile - schedules change frequently)
 * Endpoint: GET /api/learning/courses/:courseId/classes
 *
 * Only applicable for courses with deliveryMode: live | presential | hybrid
 * Check course.features.hasClasses before calling
 */
export const getCourseClasses = cache(async (courseId: string): Promise<CourseClasses> => {
  // TODO: Implement REST fetch
  // const response = await fetch(`${API_BASE_URL}/api/learning/courses/${courseId}/classes`, {
  //   next: { revalidate: 60 },
  //   headers: { Authorization: `Bearer ${token}` },
  // });
  // if (!response.ok) throw new Error('Failed to fetch classes');
  // return response.json();

  void courseId; // Suppress unused warning in stub
  return { classes: [], total: 0, upcomingCount: 0, completedCount: 0 };
});

/**
 * Fetch single class detail for viewing/editing.
 *
 * @param classId - The class ID from route params
 * @returns Full class data including attendees and settings
 *
 * Fetch Type: REST
 * Cache: revalidate 60s, deduplicated via React cache()
 * Endpoint: GET /api/learning/classes/:classId
 */
export const getCourseClass = cache(async (classId: string): Promise<CourseClassDetail | null> => {
  // TODO: Implement REST fetch
  // const response = await fetch(`${API_BASE_URL}/api/learning/classes/${classId}`, {
  //   next: { revalidate: 60 },
  //   headers: { Authorization: `Bearer ${token}` },
  // });
  // if (response.status === 404) return null;
  // if (!response.ok) throw new Error('Failed to fetch class');
  // return response.json();

  void classId; // Suppress unused warning in stub
  return null;
});

