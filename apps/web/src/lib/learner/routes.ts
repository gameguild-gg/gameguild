import { getPathname } from "@/i18n/navigation";
import { routing } from "@/i18n/routing";
import type { LearnerRoutes } from "@game-guild/courses/components/learner";
import { hasLocale } from "next-intl";

export { getLearnerCourseContentHref, getLearnerSignInHref } from "./paths";

export function normalizeLearnerPathname(pathname: string): string {
  const segments = pathname.split("/").filter(Boolean);

  if (
    routing.locales.includes(segments[0] as (typeof routing.locales)[number])
  ) {
    segments.shift();
  }

  return segments.length > 0 ? `/${segments.join("/")}` : "/";
}

export interface LearningNavigationRoutes extends LearnerRoutes {
  home: string;
  courses: string;
  calendar: string;
  grades: string;
  certificates: string;
  lesson: (slug: string, lessonId: string) => string;
}

export function createLearnerRoutes(
  locale: string = routing.defaultLocale,
): LearningNavigationRoutes {
  const resolvedLocale = hasLocale(routing.locales, locale)
    ? locale
    : routing.defaultLocale;
  const path = (href: string) => getPathname({ href, locale: resolvedLocale });

  return {
    home: path("/learn"),
    courses: path("/learn/courses"),
    calendar: path("/learn/calendar"),
    grades: path("/learn/grades"),
    certificates: path("/learn/certificates"),
    catalog: "/courses",
    course: (slug) => path(`/learn/courses/${slug}`),
    content: (slug) => path(`/learn/courses/${slug}/content`),
    lesson: (slug, lessonId) =>
      path(`/learn/courses/${slug}/lessons/${lessonId}`),
    activities: (slug) => path(`/learn/courses/${slug}/activities`),
    activity: (slug, activityId) =>
      path(`/learn/courses/${slug}/activities/${activityId}`),
    community: (slug) => path(`/learn/courses/${slug}/community`),
  };
}
