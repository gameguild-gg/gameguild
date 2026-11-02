export interface CourseRouteSource {
  id?: string | null;
  slug?: string | null;
  creatorId?: string | null;
  creatorHandle?: string | null;
  creatorName?: string | null;
  creatorEmail?: string | null;
}
const COURSE_AUTHOR_SEPARATOR = '-by-';

export function slugifyRoutePart(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80);
}

/**
 * '' when no usable author identity is known: never fall back to the raw
 * creator ID (leaks owner UUID prefix); a suffix-less slug resolves
 * identically via getCourseLookupSlug.
 */
export function getCourseAuthorHandle(course: CourseRouteSource): string {
  const fromExplicitHandle = course.creatorHandle ? slugifyRoutePart(course.creatorHandle) : '';
  if (fromExplicitHandle) return fromExplicitHandle;

  const fromName = course.creatorName ? slugifyRoutePart(course.creatorName) : '';
  if (fromName) return fromName;

  const emailLocalPart = course.creatorEmail?.split('@')[0] ?? '';
  const fromEmail = emailLocalPart ? slugifyRoutePart(emailLocalPart) : '';
  if (fromEmail) return fromEmail;

  return '';
}

export function getCourseRouteParam(course: CourseRouteSource): string {
  const slug = course.slug?.trim();
  if (slug) {
    const authorHandle = getCourseAuthorHandle(course);
    return authorHandle
      ? `${slug}${COURSE_AUTHOR_SEPARATOR}${authorHandle}`
      : slug;
  }

  return String(course.id ?? '').trim();
}

export function getCourseLookupSlug(routeParam: string): string {
  const value = routeParam.trim();
  const separatorIndex = value.lastIndexOf(COURSE_AUTHOR_SEPARATOR);

  if (separatorIndex <= 0) {
    return value;
  }

  return value.slice(0, separatorIndex);
}

export function buildDashboardCoursePath(
  course: CourseRouteSource | string,
  segment?: string,
  surface: 'workspace' | 'console' = 'workspace',
): string {
  const routeParam = typeof course === 'string' ? course : getCourseRouteParam(course);
  const coursePath = `/${surface}/learning/courses/${encodeURIComponent(routeParam)}`;

  if (!segment) return coursePath;

  return `${coursePath}/${segment.replace(/^\/+/, '')}`;
}
