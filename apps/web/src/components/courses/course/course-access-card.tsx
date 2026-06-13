import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { Program } from '@/lib/api/generated';
import type { CourseViewerAccess } from '@/lib/courses/services/course-viewer-access';
import { CalendarClock, CheckCircle2, Globe, Lock, ShieldCheck, Users } from 'lucide-react';
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
    return 'No deadline published';
  }

  const normalizedDeadline = typeof deadline === 'string' || typeof deadline === 'number' || deadline instanceof Date ? deadline : String(deadline);

  const parsedDate = new Date(normalizedDeadline);
  if (Number.isNaN(parsedDate.getTime())) {
    return 'Deadline pending';
  }

  return parsedDate.toLocaleDateString();
}

function getViewerSummary(course: Program, viewerAccess: CourseViewerAccess): string {
  switch (viewerAccess.state) {
    case 'has-access':
      return 'Your learner access is active. Continue into the classroom to resume progress.';
    case 'no-access':
      return course.isEnrollmentOpen
        ? 'You are signed in but not enrolled yet. Enrollment is currently open.'
        : 'You are signed in, but enrollment is currently closed for this course.';
    case 'unavailable':
      return 'Access verification is temporarily unavailable. Public course details are still visible.';
    case 'signed-out':
    default:
      return 'Sign in to verify access, enroll when available, and continue in the learning app.';
  }
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

export default function CourseAccessCard({ course, viewerAccess }: CourseAccessCardProps) {
  const maxEnrollments = typeof course.maxEnrollments === 'number' && course.maxEnrollments > 0 ? course.maxEnrollments : null;
  const learningHref = course.slug ? `/courses/${course.slug}/content` : null;

  return (
    <section className="overflow-hidden rounded-[2rem] border border-white/10 bg-white/[0.06] text-white shadow-2xl shadow-black/30 backdrop-blur">
      <div className="border-b border-white/10 bg-white/[0.035] p-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight">Enrollment</h2>
            <p className="mt-2 text-sm leading-6 text-slate-400">{getViewerSummary(course, viewerAccess)}</p>
          </div>
          <div className="rounded-2xl border border-white/10 bg-black/20 p-3 text-emerald-200">
            <ShieldCheck />
          </div>
        </div>
        <div className="mt-5 flex flex-wrap gap-2">
          <Badge variant={course.isEnrollmentOpen ? 'default' : 'destructive'}>{course.isEnrollmentOpen ? 'Open' : 'Closed'}</Badge>
          <Badge variant="outline" className="border-white/15 text-slate-200">
            {viewerAccess.state === 'has-access'
              ? 'Access granted'
              : viewerAccess.state === 'no-access'
                ? 'Not enrolled'
                : viewerAccess.state === 'unavailable'
                  ? 'Check unavailable'
                  : 'Sign in required'}
          </Badge>
        </div>
      </div>

      <div className="flex flex-col gap-5 p-6">
        <div className="grid gap-3">
          {viewerAccess.state === 'has-access' && learningHref ? (
            <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
              <Link href={learningHref}>
                Continue learning
                <CheckCircle2 />
              </Link>
            </Button>
          ) : null}

          {viewerAccess.state === 'signed-out' ? (
            <Button asChild size="lg" className="bg-white text-slate-950 hover:bg-slate-200">
              <Link href="/sign-in">Sign in to check access</Link>
            </Button>
          ) : null}

          {viewerAccess.state === 'no-access' && course.isEnrollmentOpen && course.slug ? <CourseSelfEnrollButton courseSlug={course.slug} /> : null}

          <Button asChild variant="outline" className="border-white/15 bg-white/5 text-white hover:bg-white/10 hover:text-white">
            <Link href="/courses">Browse more courses</Link>
          </Button>
        </div>

        {viewerAccess.error ? (
          <p className="rounded-2xl border border-amber-300/20 bg-amber-300/10 p-3 text-xs leading-5 text-amber-100">
            Latest access error: {viewerAccess.error}
          </p>
        ) : null}

        <div className="flex flex-col gap-3 text-sm">
          <div className="flex items-start justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 p-3">
            <span className="flex items-center gap-2 text-slate-400">
              <Globe />
              Visibility
            </span>
            <span className="text-right text-white">{formatVisibility(course.visibility)}</span>
          </div>
          <div className="flex items-start justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 p-3">
            <span className="flex items-center gap-2 text-slate-400">
              <Users />
              Capacity
            </span>
            <span className="text-right text-white">{maxEnrollments ? `${course.currentEnrollments ?? 0}/${maxEnrollments}` : 'No limit'}</span>
          </div>
          <div className="flex items-start justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 p-3">
            <span className="flex items-center gap-2 text-slate-400">
              <Lock />
              Access
            </span>
            <span className="text-right text-white">{viewerAccess.state === 'has-access' ? 'Active' : 'Account required'}</span>
          </div>
          <div className="flex items-start justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 p-3">
            <span className="flex items-center gap-2 text-slate-400">
              <CalendarClock />
              Deadline
            </span>
            <span className="text-right text-white">{formatEnrollmentDeadline(course.enrollmentDeadline)}</span>
          </div>
          {viewerAccess.state === 'has-access' ? (
            <div className="flex items-start justify-between gap-4 rounded-2xl border border-white/10 bg-black/20 p-3">
              <span className="flex items-center gap-2 text-slate-400">
                <CalendarClock />
                Last activity
              </span>
              <span className="text-right text-white">{formatLastAccessed(viewerAccess.lastAccessedAt)}</span>
            </div>
          ) : null}
        </div>
      </div>
    </section>
  );
}
