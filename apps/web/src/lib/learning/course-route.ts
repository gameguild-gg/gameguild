export interface CourseRouteSource {
  id?: string | null;
  slug?: string | null;
}

export function getCourseRouteParam(course: CourseRouteSource): string {
  const slug = course.slug?.trim();
  if (slug) return slug;

  return String(course.id ?? '').trim();
}

export function buildDashboardCoursePath(course: CourseRouteSource | string, segment?: string): string {
  const routeParam = typeof course === 'string' ? course : getCourseRouteParam(course);
  const basePath = `/dashboard/learning/courses/${encodeURIComponent(routeParam)}`;

  if (!segment) return basePath;

  return `${basePath}/${segment.replace(/^\/+/, '')}`;
}
