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

export function getCourseAuthorHandle(course: CourseRouteSource): string {
  const fromExplicitHandle = course.creatorHandle ? slugifyRoutePart(course.creatorHandle) : '';
  if (fromExplicitHandle) return fromExplicitHandle;

  const fromName = course.creatorName ? slugifyRoutePart(course.creatorName) : '';
  if (fromName) return fromName;

  const emailLocalPart = course.creatorEmail?.split('@')[0] ?? '';
  const fromEmail = emailLocalPart ? slugifyRoutePart(emailLocalPart) : '';
  if (fromEmail) return fromEmail;

  const creatorId = course.creatorId?.trim();
  if (creatorId) return slugifyRoutePart(creatorId).slice(0, 12) || 'gameguild';

  return 'gameguild';
}

export function getCourseRouteParam(course: CourseRouteSource): string {
  const slug = course.slug?.trim();
  if (slug) return `${slug}${COURSE_AUTHOR_SEPARATOR}${getCourseAuthorHandle(course)}`;

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

export function buildDashboardCoursePath(course: CourseRouteSource | string, segment?: string): string {
  const routeParam = typeof course === 'string' ? course : getCourseRouteParam(course);
  const basePath = `/dashboard/platform/learning/courses/${encodeURIComponent(routeParam)}`;

  if (!segment) return basePath;

  return `${basePath}/${segment.replace(/^\/+/, '')}`;
}
