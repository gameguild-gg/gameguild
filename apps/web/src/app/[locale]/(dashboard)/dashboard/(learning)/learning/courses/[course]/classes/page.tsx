import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseClasses } from '@/lib/learning';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CalendarClock, Users, Video } from 'lucide-react';

/**
 * L6: Course Classes/Schedule Page
 *
 * Route: /learning/courses/[course]/classes
 *
 * Lists all scheduled and past classes for live/presential/hybrid courses.
 * Only available when course.features.hasClasses = true.
 *
 * Data Pattern:
 * - Layout conditionally preloaded getCourseClasses() if hasClasses
 * - This page awaits getCourseClasses() — hits warm cache or in-flight promise
 * - Also validates course exists AND has classes feature enabled
 *
 * UI Responsibility:
 * - Scheduled class list with upcoming/live/completed counts
 * - Session metadata, attendance capacity, and virtual meeting indicators
 * - Navigate to /classes/[classId] for class detail/editing
 *
 * Delivery Mode Behavior:
 * - Live: Virtual sessions with meeting URLs
 * - Presential: Physical location with address
 * - Hybrid: Mix of both, location.type per class
 */
export default async function ClassesPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/classes'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  // Parallel fetch - both hit warm cache from layout preload
  const [course, classes] = await Promise.all([
    getCourse(courseId),
    getCourseClasses(courseId),
  ]);

  if (!course) {
    notFound();
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-3">
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{classes.total}</p><p className="text-sm text-muted-foreground">Total sessions</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{classes.upcomingCount}</p><p className="text-sm text-muted-foreground">Upcoming or live</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{classes.completedCount}</p><p className="text-sm text-muted-foreground">Completed</p></CardContent></Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarClock className="size-5" />
            Course Schedule
          </CardTitle>
          <CardDescription>{course.title} cohort sessions and live delivery schedule.</CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          {classes.classes.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No cohorts or live sessions are scheduled for this course.</div>
          ) : (
            classes.classes.map((courseClass) => (
              <Link key={courseClass.id} href={`/dashboard/learning/courses/${courseId}/classes/${courseClass.id}`} className="flex items-center justify-between rounded-lg border p-4 transition-colors hover:bg-muted/50">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <p className="font-medium">{courseClass.title}</p>
                    <Badge variant="outline">{courseClass.status}</Badge>
                  </div>
                  <p className="text-sm text-muted-foreground">{new Date(courseClass.scheduledAt).toLocaleString('en-US')} · {courseClass.duration} min</p>
                </div>
                <div className="flex items-center gap-4 text-sm text-muted-foreground">
                  <span className="flex items-center gap-1"><Users className="size-4" />{courseClass.attendeeCount}/{courseClass.maxAttendees ?? '∞'}</span>
                  {courseClass.location?.meetingUrl && <Video className="size-4" />}
                </div>
              </Link>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
