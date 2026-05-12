import type { CourseContent, CourseDetails } from '@/lib/learning/types';

export type StorefrontState = 'hidden' | 'teaser' | 'enrollment-open' | 'enrollment-closed';
export type AcademyState = 'hidden' | 'scheduled' | 'live';
export type CourseReadinessState = 'incomplete' | 'storefront-ready' | 'academy-ready' | 'live';
export type CourseReadinessArea = 'storefront' | 'academy';

export interface CourseStructureMetrics {
  modules: number;
  lessons: number;
  totalDurationMinutes: number;
}

export interface CourseReadinessCheck {
  key: string;
  label: string;
  done: boolean;
  area: CourseReadinessArea;
}

export interface CourseLaunchSummary {
  storefrontState: StorefrontState;
  academyState: AcademyState;
  readinessState: CourseReadinessState;
  structure: CourseStructureMetrics;
  checks: CourseReadinessCheck[];
  blockers: string[];
  enrollmentDeadlinePassed: boolean;
}

function normalizeStatus(value: string | null | undefined): string {
  return value?.trim().toLowerCase() ?? '';
}

function getEnrollmentDeadlinePassed(deadline: string | null): boolean {
  if (!deadline) {
    return false;
  }

  const date = new Date(deadline);
  if (Number.isNaN(date.getTime())) {
    return false;
  }

  return date.getTime() <= Date.now();
}

export function getCourseStructureMetrics(content: CourseContent): CourseStructureMetrics {
  return {
    modules: content.items.filter((item) => !item.parentId).length,
    lessons: content.items.filter((item) => !!item.parentId).length,
    totalDurationMinutes: content.items.reduce((total, item) => total + (item.duration ?? 0), 0),
  };
}

export function deriveCourseLaunchSummary(course: CourseDetails, content: CourseContent): CourseLaunchSummary {
  const structure = getCourseStructureMetrics(content);
  const deadlinePassed = getEnrollmentDeadlinePassed(course.enrollmentDeadline);
  const normalizedStatus = normalizeStatus(course.status);
  const normalizedVisibility = normalizeStatus(course.visibility);
  const normalizedEnrollmentStatus = normalizeStatus(course.enrollmentStatus);

  const checks: CourseReadinessCheck[] = [
    {
      key: 'title',
      label: 'Set a clear course title',
      done: course.title.trim().length > 0,
      area: 'storefront',
    },
    {
      key: 'description',
      label: 'Add a course description',
      done: course.description.trim().length > 0,
      area: 'storefront',
    },
    {
      key: 'slug',
      label: 'Set the course slug',
      done: course.slug.trim().length > 0,
      area: 'storefront',
    },
    {
      key: 'thumbnail',
      label: 'Upload a cover image',
      done: Boolean(course.thumbnail),
      area: 'storefront',
    },
    {
      key: 'module',
      label: 'Create at least one module',
      done: structure.modules > 0,
      area: 'academy',
    },
    {
      key: 'lesson',
      label: 'Add at least one lesson',
      done: structure.lessons > 0,
      area: 'academy',
    },
  ];

  const storefrontReady = checks.filter((check) => check.area === 'storefront').every((check) => check.done);
  const academyReady = checks.every((check) => check.done);

  let storefrontState: StorefrontState = 'hidden';
  if (normalizedStatus === 'published' && normalizedVisibility !== 'private') {
    if (normalizedVisibility === 'unlisted') {
      storefrontState = 'teaser';
    } else if (course.isEnrollmentOpen && normalizedEnrollmentStatus !== 'closed' && !deadlinePassed) {
      storefrontState = 'enrollment-open';
    } else {
      storefrontState = 'enrollment-closed';
    }
  }

  let academyState: AcademyState = 'hidden';
  if (normalizedStatus === 'published') {
    academyState = academyReady ? 'live' : 'scheduled';
  }

  let readinessState: CourseReadinessState = 'incomplete';
  if (storefrontReady) {
    readinessState = 'storefront-ready';
  }
  if (academyReady) {
    readinessState = 'academy-ready';
  }
  if (normalizedStatus === 'published' && storefrontState !== 'hidden' && academyState === 'live') {
    readinessState = 'live';
  }

  return {
    storefrontState,
    academyState,
    readinessState,
    structure,
    checks,
    blockers: checks.filter((check) => !check.done).map((check) => check.label),
    enrollmentDeadlinePassed: deadlinePassed,
  };
}

export function formatDurationLabel(totalDurationMinutes: number): string {
  if (totalDurationMinutes >= 60) {
    const hours = Math.floor(totalDurationMinutes / 60);
    const minutes = totalDurationMinutes % 60;
    return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`;
  }

  return `${totalDurationMinutes}m`;
}
