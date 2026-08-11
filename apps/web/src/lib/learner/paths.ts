export function getLearnerCourseContentHref(courseSlug: string): string {
  return `/learn/courses/${encodeURIComponent(courseSlug)}/content`;
}

interface LearnerSignInInput {
  pathname: string;
  search?: string;
}

export function getLearnerSignInHref({
  pathname,
  search = "",
}: LearnerSignInInput): string {
  const safePathname =
    pathname.startsWith("/") && !pathname.startsWith("//")
      ? pathname
      : "/learn";
  const normalizedSearch = search.replace(/^\?/, "");
  const redirectTo = normalizedSearch
    ? `${safePathname}?${normalizedSearch}`
    : safePathname;
  const params = new URLSearchParams({ redirectTo });

  return `/sign-in?${params.toString()}`;
}
