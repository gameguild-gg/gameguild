import { getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type LearningCohortsCohort,
  type LearningCohortsCohortSchedule,
  type LearningCohortsCourseCohortCalendar,
  type LearningEnrollmentsEnrollment,
  type SystemDayOfWeek,
} from '@game-guild/client';
import { cache } from 'react';

import { resolveCourseId } from './course';

export type CourseCohortStatus = 'scheduled' | 'active' | 'completed' | 'cancelled';

export interface CourseCohortScheduleSummary {
  version: number;
  timezoneId: string;
  meetingDays: SystemDayOfWeek[];
  meetingStartTime: string | null;
  pacingMode: string | null;
  releasePolicy: string | null;
  itemCount: number;
}

export interface CourseCohortSummary {
  id: string;
  courseId: string;
  name: string;
  description: string;
  instructor: { id: string; name: string | null } | null;
  period: { startsAt: string; endsAt: string };
  meetingPattern: string | null;
  enrollment: { current: number; capacity: number | null };
  nextMeetingAt: string | null;
  conflictCount: number;
  status: CourseCohortStatus;
  isOpen: boolean;
  schedule: CourseCohortScheduleSummary | null;
  createdAt: string;
}

export interface CourseCohortAttendee {
  id: string;
  userId: string;
  status: 'active' | 'paused' | 'completed' | 'dropped' | 'expired';
  progress: number;
  enrolledAt: string;
  completedAt: string | null;
  lastActivityAt: string | null;
}

export interface CourseCohortDetail extends CourseCohortSummary {
  attendees: CourseCohortAttendee[];
}

export interface CourseCohortCollection {
  cohorts: CourseCohortSummary[];
  total: number;
  scheduledCount: number;
  activeCount: number;
  completedCount: number;
}

function createCohortModules() {
  const apiUrl = process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
  const client = createServerClient({
    baseUrl: apiUrl,
    auth: { getAccessToken: () => getToken() },
  });

  return {
    cohorts: new GeneratedApi.LearningCohortsModule(client),
    schedules: new GeneratedApi.LearningCohortsSchedulesModule(client),
    enrollments: new GeneratedApi.LearningEnrollmentsModule(client),
  };
}

function mapStatus(status: LearningCohortsCohort['status']): CourseCohortStatus {
  if (status === 'Active') return 'active';
  if (status === 'Completed') return 'completed';
  if (status === 'Cancelled') return 'cancelled';
  return 'scheduled';
}

export function mapCohort(cohort: LearningCohortsCohort): CourseCohortSummary {
  const instructorId = cohort.instructorId ?? null;

  return {
    id: cohort.id ?? '',
    courseId: cohort.courseId ?? '',
    name: cohort.name?.trim() || 'Untitled class',
    description: cohort.description?.trim() || '',
    instructor: instructorId ? { id: instructorId, name: null } : null,
    period: {
      startsAt: cohort.startDate ?? '',
      endsAt: cohort.endDate ?? '',
    },
    meetingPattern: cohort.meetingSchedule?.trim() || null,
    enrollment: {
      current: cohort.currentEnrollmentCount ?? 0,
      capacity: cohort.maxCapacity ?? null,
    },
    nextMeetingAt: cohort.nextMeetingAt ?? null,
    conflictCount: cohort.conflictCount ?? 0,
    status: mapStatus(cohort.status),
    isOpen: cohort.isOpen ?? false,
    schedule: cohort.schedule
      ? {
          version: cohort.schedule.version ?? 0,
          timezoneId: cohort.schedule.timezoneId?.trim() || 'UTC',
          meetingDays: cohort.schedule.meetingDays ?? [],
          meetingStartTime: cohort.schedule.meetingStartTime ?? null,
          pacingMode: cohort.schedule.pacingMode ?? null,
          releasePolicy: cohort.schedule.releasePolicy ?? null,
          itemCount: cohort.schedule.itemCount ?? 0,
        }
      : null,
    createdAt: cohort.createdAt ?? '',
  };
}

function mapEnrollmentStatus(status: LearningEnrollmentsEnrollment['status']): CourseCohortAttendee['status'] {
  if (status === 'Paused') return 'paused';
  if (status === 'Completed') return 'completed';
  if (status === 'Dropped') return 'dropped';
  if (status === 'Expired') return 'expired';
  return 'active';
}

function mapAttendee(enrollment: LearningEnrollmentsEnrollment, index: number): CourseCohortAttendee {
  return {
    id: enrollment.id ?? `enrollment-${index}`,
    userId: enrollment.userId ?? 'unknown-user',
    status: mapEnrollmentStatus(enrollment.status),
    progress: Math.max(0, Math.min(100, Math.round(enrollment.progress ?? 0))),
    enrolledAt: enrollment.enrolledAt ?? '',
    completedAt: enrollment.completedAt ?? null,
    lastActivityAt: enrollment.lastActivityAt ?? null,
  };
}

export const getCourseCohorts = cache(async (courseIdentifier: string): Promise<CourseCohortCollection> => {
  const courseId = await resolveCourseId(courseIdentifier);
  const result = await createCohortModules().cohorts.getApiCohortsCourse(courseId);
  const cohorts = result.ok ? result.data.map(mapCohort) : [];

  return {
    cohorts,
    total: cohorts.length,
    scheduledCount: cohorts.filter((cohort) => cohort.status === 'scheduled').length,
    activeCount: cohorts.filter((cohort) => cohort.status === 'active').length,
    completedCount: cohorts.filter((cohort) => cohort.status === 'completed').length,
  };
});

export const getCohort = cache(async (cohortId: string): Promise<CourseCohortDetail | null> => {
  const modules = createCohortModules();
  const cohortResult = await modules.cohorts.getApiCohorts(cohortId);
  if (!cohortResult.ok) return null;

  const cohort = mapCohort(cohortResult.data);
  const enrollmentResult = await modules.enrollments.getApiLearningEnrollmentsCourses(cohort.courseId);
  const attendees = enrollmentResult.ok
    ? (enrollmentResult.data ?? [])
        .filter((enrollment) => enrollment.cohortId === cohort.id)
        .map(mapAttendee)
    : [];

  return { ...cohort, attendees };
});

export const getCohortSchedule = cache(async (courseIdentifier: string, cohortId: string): Promise<LearningCohortsCohortSchedule | null> => {
  const courseId = await resolveCourseId(courseIdentifier);
  const result = await createCohortModules().schedules.getCoursesCohortsSchedule(courseId, cohortId);
  return result.ok ? result.data : null;
});

export const getCourseCohortCalendar = cache(
  async (
    courseIdentifier: string,
    query?: { cohortId?: string; from?: string; to?: string },
  ): Promise<LearningCohortsCourseCohortCalendar | null> => {
    const courseId = await resolveCourseId(courseIdentifier);
    const result = await createCohortModules().schedules.getCoursesCohortsCalendar(courseId, query);
    return result.ok ? result.data : null;
  },
);
