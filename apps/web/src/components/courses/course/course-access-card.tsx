import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Link } from '@/i18n/navigation';
import { Program } from '@/lib/api/generated';
import type { CourseViewerAccess } from '@/lib/courses/services/course-viewer-access';
import { CalendarClock, Globe, Lock, Users } from 'lucide-react';
import { CourseSelfEnrollButton } from './course-self-enroll-button';

interface CourseAccessCardProps {
  readonly course: Program;
  readonly viewerAccess: CourseViewerAccess;
}

function formatVisibility(visibility: Program['visibility']): string {
  if (typeof visibility !== 'string' || visibility.length === 0) {
    return 'Catalog visibility unavailable';
  }

  return visibility.charAt(0).toUpperCase() + visibility.slice(1).toLowerCase();
}

function formatEnrollmentDeadline(deadline: Program['enrollmentDeadline']): string {
  if (!deadline) {
    return 'No enrollment deadline published';
  }

  const normalizedDeadline =
    typeof deadline === 'string' || typeof deadline === 'number' || deadline instanceof Date ? deadline : String(deadline);

  const parsedDate = new Date(normalizedDeadline);
  if (Number.isNaN(parsedDate.getTime())) {
    return 'Enrollment deadline pending';
  }

  return parsedDate.toLocaleDateString();
}

function getAvailabilitySummary(course: Program): string {
  const visibility = typeof course.visibility === 'string' ? course.visibility.trim().toLowerCase() : '';

  if (course.isEnrollmentOpen) {
    return 'Published in the live catalog. Enrollment is open, and attendance continues in the learning app once learner access is confirmed.';
  }

  return (
    visibility === 'public'
      ? 'This course is visible in the live catalog, but enrollment is currently closed.'
      : 'This course is not fully public yet. Availability details are limited to the catalog metadata below.'
  );
}

function formatLastAccessed(lastAccessedAt: string | null | undefined): string {
  if (!lastAccessedAt) {
    return 'No recent activity';
  }

  const parsedDate = new Date(lastAccessedAt);
  if (Number.isNaN(parsedDate.getTime())) {
    return 'Recent activity unavailable';
  }

  return parsedDate.toLocaleString();
}

function getViewerSummary(course: Program, viewerAccess: CourseViewerAccess): string {
  switch (viewerAccess.state) {
    case 'has-access':
      return 'Your learner access is active for this course. Continue in the learning app to resume attendance and progress tracking.';
    case 'no-access':
      return course.isEnrollmentOpen
        ? 'You are signed in, but this account does not have learner access for this course yet. You can self-enroll from this storefront while enrollment remains open.'
        : 'You are signed in, but this course is not currently available for learner access.';
    case 'unavailable':
      return 'The storefront could not verify your learner access right now. Catalog metadata is still available below.';
    case 'signed-out':
    default:
      return 'Sign in to check whether this account already has learner access and to continue in the dedicated learning app.';
  }
}

export default function CourseAccessCard({ course, viewerAccess }: CourseAccessCardProps) {
  const visibility = typeof course.visibility === 'string' ? course.visibility.trim().toLowerCase() : '';
  const maxEnrollments = typeof course.maxEnrollments === 'number' && course.maxEnrollments > 0 ? course.maxEnrollments : null;
  const learningHref = course.slug ? `/courses/${course.slug}/content` : null;

  return (
    <Card className="bg-gray-800 border-gray-700">
      <CardHeader>
        <CardTitle className="text-xl">Course Access</CardTitle>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="flex flex-wrap gap-2">
          <Badge variant={course.isEnrollmentOpen ? 'default' : 'destructive'}>{course.isEnrollmentOpen ? 'Enrollment Open' : 'Enrollment Closed'}</Badge>
          <Badge variant="outline" className="border-gray-600 text-gray-200">
            {formatVisibility(course.visibility)}
          </Badge>
          <Badge variant="outline" className="border-gray-600 text-gray-200">
            {viewerAccess.state === 'has-access'
              ? 'Access Granted'
              : viewerAccess.state === 'no-access'
                ? 'No Learner Access'
                : viewerAccess.state === 'unavailable'
                  ? 'Access Check Unavailable'
                  : 'Sign In Required'}
          </Badge>
          {typeof course.status === 'string' && course.status.length > 0 ? (
            <Badge variant="outline" className="border-gray-600 text-gray-200">
              {course.status}
            </Badge>
          ) : null}
        </div>

        <p className="text-sm leading-6 text-gray-300">{getAvailabilitySummary(course)}</p>

        <div className="rounded-xl border border-gray-700 bg-gray-900/60 p-4">
          <p className="text-sm leading-6 text-gray-200">{getViewerSummary(course, viewerAccess)}</p>
          {viewerAccess.error ? <p className="mt-2 text-xs text-amber-300">Latest access error: {viewerAccess.error}</p> : null}
          <div className="mt-4 flex flex-wrap gap-3">
            {viewerAccess.state === 'has-access' && learningHref ? (
              <Button asChild className="bg-blue-600 text-white hover:bg-blue-500">
                <Link href={learningHref}>Continue in learning app</Link>
              </Button>
            ) : null}

            {viewerAccess.state === 'signed-out' ? (
              <Button asChild variant="outline" className="border-gray-600 bg-gray-800/60 text-gray-100 hover:bg-gray-700/60 hover:text-white">
                <Link href="/sign-in">Sign in to check access</Link>
              </Button>
            ) : null}

            {viewerAccess.state === 'no-access' && course.isEnrollmentOpen && course.slug ? (
              <CourseSelfEnrollButton courseSlug={course.slug} />
            ) : null}
          </div>
        </div>

        <div className="space-y-3 text-sm text-gray-200">
          <div className="flex items-start justify-between gap-4">
            <span className="flex items-center gap-2 text-gray-400">
              <Globe className="h-4 w-4" />
              Catalog visibility
            </span>
            <span className="text-right">{formatVisibility(course.visibility)}</span>
          </div>

          <div className="flex items-start justify-between gap-4">
            <span className="flex items-center gap-2 text-gray-400">
              <Users className="h-4 w-4" />
              Current learners
            </span>
            <span className="text-right">{course.currentEnrollments ?? 0}</span>
          </div>

          <div className="flex items-start justify-between gap-4">
            <span className="flex items-center gap-2 text-gray-400">
              <Lock className="h-4 w-4" />
              Capacity
            </span>
            <span className="text-right">{maxEnrollments ?? 'No limit published'}</span>
          </div>

          <div className="flex items-start justify-between gap-4">
            <span className="flex items-center gap-2 text-gray-400">
              <CalendarClock className="h-4 w-4" />
              Enrollment deadline
            </span>
            <span className="text-right">{formatEnrollmentDeadline(course.enrollmentDeadline)}</span>
          </div>

          {viewerAccess.state === 'has-access' ? (
            <div className="flex items-start justify-between gap-4">
              <span className="flex items-center gap-2 text-gray-400">
                <CalendarClock className="h-4 w-4" />
                Last activity
              </span>
              <span className="text-right">{formatLastAccessed(viewerAccess.lastAccessedAt)}</span>
            </div>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}
