import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type ContentStatus,
  type ContentVisibility,
  type LearningCoursesProgram,
  type LearningCoursesProgramContent
} from '@game-guild/client';
import { cache } from 'react';

// Types are defined in a separate file so client components can import them
// without pulling in server-only modules (auth, next/headers).
export type {
  ContentItem, ContentItemDetail, CourseAnalytics, CourseContent, CourseDeliveryMode, CourseDetails, CourseFeatures, CoursePricingModel, CourseStudents
} from '@/lib/learning/types';

import type {
  ContentItem,
  ContentItemDetail,
  CourseAnalytics,
  CourseContent,
  CourseDetails,
  CourseStudents,
  LearningCoursesProgramContentType,
} from '@/lib/learning/types';
import { learningApiGet } from './http';

// Re-export generated types for consumers
export type { LearningCoursesProgram, LearningCoursesProgramContent, LearningCoursesProgramContentType };

function getApiClient() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  return createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });
}

function createCourseModules() {
  const client = getApiClient();

  return {
    programs: new GeneratedApi.LearningCoursesProgramModule(client),
    content: new GeneratedApi.LearningCoursesProgramcontentModule(client),
  };
}

function emptyCourseAnalytics(): CourseAnalytics {
  return {
    totalUsers: 0,
    activeUsers: 0,
    completedUsers: 0,
    completionRate: 0,
    averageCompletionTime: null,
    totalViews: 0,
    lastActivity: null,
    enrollments: [],
    ratings: [],
    revenue: [],
  };
}

// Map ContentStatus string union to simplified frontend status
function mapStatus(s: ContentStatus | undefined): 'draft' | 'published' | 'archived' {
  if (s === 'Published') return 'published';
  if (s === 'Archived') return 'archived';
  return 'draft';
}

// Map ContentVisibility string union to simplified frontend visibility
function mapVisibility(v: ContentVisibility | undefined): 'public' | 'private' | 'unlisted' {
  if (v === 'Public') return 'public';
  return 'private';
}

/**
 * Fetch course details from the API.
 */
export const getCourse = cache(async (courseId: string): Promise<CourseDetails | null> => {
  try {
    const { programs } = createCourseModules();
    const result = await programs.getCourses1(courseId);

    if (!result.ok) {
      const err = result.error as { status?: number; code?: string; message?: string; detail?: string } | undefined;
      console.error(`[getCourse] Failed for ${courseId}: status=${err?.status}, code=${err?.code}, detail=${err?.detail || err?.message}`);
      return null;
    }

    const dto = result.data;
    return {
      id: dto.id!,
      title: dto.title ?? '',
      description: dto.description ?? '',
      metadata: dto.metadata ?? null,
      slug: dto.slug ?? '',
      status: mapStatus(dto.status),
      visibility: mapVisibility(dto.visibility),
      thumbnail: dto.thumbnail ?? null,
      videoShowcaseUrl: dto.videoShowcaseUrl ?? null,
      estimatedHours: dto.estimatedHours ?? null,
      category: dto.category ?? 'GeneralEducation',
      difficulty: dto.difficulty ?? 'Beginner',
      skillsRequired: dto.skillsRequired ?? null,
      skillsProvided: dto.skillsProvided ?? null,
      enrollmentStatus: dto.enrollmentStatus ?? 'Open',
      maxEnrollments: dto.maxEnrollments ?? null,
      enrollmentDeadline: dto.enrollmentDeadline ?? null,
      currentEnrollments: dto.currentEnrollments ?? 0,
      averageRating: dto.averageRating ?? 0,
      totalRatings: dto.totalRatings ?? 0,
      isEnrollmentOpen: dto.isEnrollmentOpen ?? true,
      deliveryMode: 'on-demand',
      pricingModel: 'free',
      features: {
        hasClasses: true,
        hasRecordings: true,
        hasSchedule: true,
        hasOnDemandContent: true,
        hasPricing: true,
        hasCertificate: true,
        hasAssessments: true,
        hasDiscussions: true,
      },
      createdAt: dto.createdAt ?? new Date().toISOString(),
      updatedAt: dto.updatedAt ?? dto.createdAt ?? new Date().toISOString(),
    };
  } catch {
    return null;
  }
});

/**
 * Fetch course analytics data from the API.
 */
export const getCourseAnalytics = cache(async (courseId: string): Promise<CourseAnalytics> => {
  const empty = emptyCourseAnalytics();
  try {
    const { programs } = createCourseModules();
    const result = await programs.getCoursesAnalytics(courseId);

    if (!result.ok) return empty;

    const dto = result.data;
    const totalUsers = Math.max(0, dto.totalUsers ?? 0);
    const completedUsers = Math.max(0, dto.completedUsers ?? 0);
    const completionRate =
      dto.completionRate ?? (totalUsers > 0 ? (completedUsers / totalUsers) * 100 : 0);

    return {
      totalUsers,
      activeUsers: Math.max(0, dto.activeUsers ?? 0),
      completedUsers,
      completionRate: Math.max(0, Math.min(100, completionRate)),
      averageCompletionTime: dto.averageCompletionTime ?? null,
      totalViews: Math.max(0, dto.totalViews ?? 0),
      lastActivity: dto.lastActivity ?? null,
      enrollments: [],
      ratings: [],
      revenue: [],
    };
  } catch {
    return empty;
  }
});

/**
 * Map a LearningCoursesProgramContent DTO to the frontend ContentItem shape.
 */
function mapContentDto(dto: LearningCoursesProgramContent): ContentItem {
  return {
    id: dto.id!,
    parentId: dto.parentId ?? null,
    order: dto.sortOrder ?? 0,
    type: dto.type ?? 'Lesson',
    title: dto.title ?? '',
    description: dto.description ?? null,
    status: dto.visibility === 'Public' ? 'published' : 'draft',
    duration: dto.estimatedMinutes ?? null,
    metadata: {},
    createdAt: dto.createdAt ?? new Date().toISOString(),
    updatedAt: dto.updatedAt ?? dto.createdAt ?? new Date().toISOString(),
  };
}

/**
 * Fetch course content items from the API (flat list for tree rendering).
 */
export const getCourseContent = cache(async (courseId: string): Promise<CourseContent> => {
  try {
    const { content } = createCourseModules();
    const result = await content.getCoursesContent(courseId);

    if (!result.ok) return { items: [], total: 0 };

    // Flatten the tree if children are nested
    const items: ContentItem[] = [];
    for (const dto of result.data) {
      items.push(mapContentDto(dto));
      if (dto.children) {
        for (const child of dto.children) {
          items.push(mapContentDto(child));
        }
      }
    }

    return { items, total: items.length };
  } catch {
    return { items: [], total: 0 };
  }
});

/**
 * Fetch single content item detail.
 */
export const getContentItem = cache(async (courseId: string, contentId: string): Promise<ContentItemDetail | null> => {
  try {
    const { content } = createCourseModules();
    const result = await content.getCoursesContent1(courseId, contentId);

    if (!result.ok) return null;

    const dto = result.data;
    return {
      ...mapContentDto(dto),
      content: dto.body != null ? (typeof dto.body === 'string' ? dto.body : JSON.stringify(dto.body)) : null,
      settings: {
        isRequired: dto.isRequired,
        gradingMethod: dto.gradingMethod ?? null,
        maxPoints: dto.maxPoints ?? null,
      },
    };
  } catch {
    return null;
  }
});

/**
 * Fetch course students from the API.
 */
export const getCourseStudents = cache(async (courseId: string): Promise<CourseStudents> => {
  try {
    const { programs } = createCourseModules();
    const result = await programs.getCoursesUsers(courseId, { take: 200 });

    if (!result.ok) return { students: [], total: 0 };

    const students = (result.data as Array<GeneratedApi.LearningCoursesUserProgress & {
      userId?: string;
      userName?: string;
      userEmail?: string;
    }>).map((dto, i) => ({
      id: dto.userId ?? `user-${i}`,
      name: dto.userName ?? `Student ${i + 1}`,
      email: dto.userEmail ?? '',
      enrolledAt: dto.startedAt ?? new Date().toISOString(),
      progress: Math.round(dto.completionPercentage ?? 0),
      completedAt: dto.completedAt ?? null,
      lastActivity: dto.lastAccessedAt ?? new Date().toISOString(),
    }));

    return { students, total: students.length };
  } catch {
    return { students: [], total: 0 };
  }
});

// =============================================================================
// LIVE / PRESENTIAL COURSE DATA (only applicable when hasClasses = true)
// =============================================================================

/**
 * Class/session status
 */
export type ClassStatus =
  | 'scheduled' // Upcoming, not started
  | 'live' // Currently in progress
  | 'completed' // Finished
  | 'cancelled' // Was cancelled
  | 'rescheduled'; // Moved to different time

/**
 * A single class/session in a live or presential course
 */
export interface CourseClass {
  id: string;
  title: string;
  description: string;
  status: ClassStatus;
  scheduledAt: string; // ISO datetime
  duration: number; // minutes
  timezone: string; // IANA timezone
  location?: {
    // For presential/hybrid
    type: 'physical' | 'virtual' | 'hybrid';
    address?: string; // Physical location
    roomName?: string;
    meetingUrl?: string; // Zoom, Teams, etc.
    meetingId?: string;
  };
  instructor?: {
    id: string;
    name: string;
    avatarUrl?: string;
  };
  attendeeCount: number;
  maxAttendees?: number;
  recordingUrl?: string; // Available after class ends (if recorded)
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

interface CohortDto {
  id: string;
  courseId: string;
  name: string;
  description?: string | null;
  startDate: string;
  endDate: string;
  maxCapacity: number;
  currentEnrollmentCount: number;
  availableSpots?: number;
  status: 'Scheduled' | 'Active' | 'Completed' | 'Cancelled' | string;
  isOpen: boolean;
  canEnroll?: boolean;
  instructorId?: string | null;
  meetingSchedule?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

function mapCohortStatus(cohort: CohortDto): ClassStatus {
  if (cohort.status === 'Cancelled') return 'cancelled';
  if (cohort.status === 'Completed') return 'completed';

  const startsAt = new Date(cohort.startDate).getTime();
  const endsAt = new Date(cohort.endDate).getTime();
  const now = Date.now();

  if (cohort.status === 'Active' && startsAt <= now && now <= endsAt) return 'live';
  if (cohort.status === 'Active' || cohort.status === 'Scheduled') return 'scheduled';
  return 'rescheduled';
}

function mapCohortToClass(cohort: CohortDto): CourseClass {
  const duration = Math.max(0, Math.round((new Date(cohort.endDate).getTime() - new Date(cohort.startDate).getTime()) / 60000));

  return {
    id: cohort.id,
    title: cohort.name,
    description: cohort.description ?? '',
    status: mapCohortStatus(cohort),
    scheduledAt: cohort.startDate,
    duration,
    timezone: 'UTC',
    location: {
      type: cohort.meetingSchedule ? 'virtual' : 'physical',
      meetingUrl: cohort.meetingSchedule ?? undefined,
    },
    instructor: cohort.instructorId
      ? {
          id: cohort.instructorId,
          name: 'Assigned instructor',
        }
      : undefined,
    attendeeCount: cohort.currentEnrollmentCount,
    maxAttendees: cohort.maxCapacity,
    materials: [],
    createdAt: cohort.createdAt,
    updatedAt: cohort.updatedAt ?? cohort.createdAt,
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
  const cohorts = await learningApiGet<CohortDto[]>(`/api/cohorts/course/${courseId}`, 60);
  const classes = (cohorts ?? []).map(mapCohortToClass);

  return {
    classes,
    total: classes.length,
    upcomingCount: classes.filter((courseClass) => courseClass.status === 'scheduled' || courseClass.status === 'live').length,
    completedCount: classes.filter((courseClass) => courseClass.status === 'completed').length,
  };
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
  const cohort = await learningApiGet<CohortDto>(`/api/cohorts/${classId}`, 60);
  if (!cohort) return null;

  return {
    ...mapCohortToClass(cohort),
    attendees: [],
    settings: {
      allowLateJoin: true,
      recordSession: true,
      enableChat: true,
      enableQA: true,
      reminderSchedule: [1440, 60, 10],
    },
  };
});
