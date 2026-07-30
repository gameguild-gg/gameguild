import type { LearnerRoutes } from '@game-guild/courses/components/learner';

export interface LearningNavigationRoutes extends LearnerRoutes {
  home: string;
  courses: string;
  calendar: string;
  grades: string;
  certificates: string;
  lesson: (slug: string, lessonId: string) => string;
}

export function createLearnerRoutes(webOrigin = process.env.NEXT_PUBLIC_WEB_URL || 'https://gameguild.gg'): LearningNavigationRoutes {
  return {
    home: '/',
    courses: '/courses',
    calendar: '/calendar',
    grades: '/grades',
    certificates: '/certificates',
    catalog: new URL('/courses', webOrigin).toString().replace(/\/$/, ''),
    course: (slug) => `/courses/${slug}`,
    content: (slug) => `/courses/${slug}/content`,
    lesson: (slug, lessonId) => `/courses/${slug}/lessons/${lessonId}`,
    activities: (slug) => `/courses/${slug}/activities`,
    activity: (slug, activityId) => `/courses/${slug}/activities/${activityId}`,
    community: (slug) => `/courses/${slug}/community`,
  };
}

interface CentralSignInInput {
  learningOrigin: string;
  pathname: string;
  webOrigin: string;
}

export function getCentralSignInUrl({
  learningOrigin,
  pathname,
  webOrigin,
}: CentralSignInInput): string {
  const allowedLearningOrigin = new URL(learningOrigin);
  const returnUrl = new URL(pathname, allowedLearningOrigin);
  const signInUrl = new URL('/sign-in', webOrigin);
  signInUrl.searchParams.set('redirectTo', returnUrl.toString());

  return signInUrl.toString();
}
