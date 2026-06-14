import React from 'react';
import { notFound } from 'next/navigation';
import { getCourse, getCourseClass } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { CalendarClock, Users, Video } from 'lucide-react';
import { ClassDetailActions } from './class-detail-actions';

/**
 * L6a: Single Class Detail/Editor Page
 *
 * Route: /learning/courses/[course]/classes/[classId]
 *
 * Displays full class detail for viewing or editing.
 * Only available when course.features.hasClasses = true.
 *
 * Data Pattern:
 * - getCourseClass(classId) is NOT preloaded by layout (classId unknown)
 * - Validates course exists AND has classes feature
 * - Then fetches specific class detail
 *
 * UI Responsibility:
 * - Class detail summary with status, schedule, capacity, and cohort enrollments
 * - Course validation and class-level not-found handling
 */
export default async function ClassDetailPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/classes/[classId]'>): Promise<React.JSX.Element> {
  const { course: courseId, classId } = await params;

  // First validate course and feature access
  const course = await getCourse(courseId);

  if (!course) {
    notFound();
  }

  // Now fetch the specific class
  const classDetail = await getCourseClass(classId);

  if (!classDetail) {
    notFound();
  }

  return (
    <div className="grid gap-6 lg:grid-cols-3">
      <Card className="lg:col-span-2">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <CalendarClock className="size-5" />
            {classDetail.title}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-wrap gap-2">
            <Badge>{classDetail.status}</Badge>
            <Badge variant="outline">{course.title}</Badge>
          </div>
          <p className="text-sm text-muted-foreground">{classDetail.description || 'No session description has been added.'}</p>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-lg border p-4">
              <p className="text-sm text-muted-foreground">Scheduled</p>
              <p className="font-medium">{new Date(classDetail.scheduledAt).toLocaleString('en-US')}</p>
            </div>
            <div className="rounded-lg border p-4">
              <p className="text-sm text-muted-foreground">Capacity</p>
              <p className="font-medium">{classDetail.attendeeCount}/{classDetail.maxAttendees ?? 'Unlimited'}</p>
            </div>
          </div>
          {classDetail.location?.meetingUrl ? (
            <div className="rounded-lg border p-4">
              <p className="flex items-center gap-2 text-sm text-muted-foreground">
                <Video className="size-4" />
                Meeting URL or room
              </p>
              <p className="break-all font-medium">{classDetail.location.meetingUrl}</p>
            </div>
          ) : null}
        </CardContent>
      </Card>

      <div className="space-y-6">
        <Card>
          <CardHeader><CardTitle className="flex items-center gap-2 text-lg"><Users className="size-4" />Attendees</CardTitle></CardHeader>
          <CardContent className="space-y-3">
            {classDetail.attendees.length === 0 ? (
              <p className="text-sm text-muted-foreground">No cohort enrollments are attached to this class yet.</p>
            ) : (
              classDetail.attendees.map((attendee) => (
                <div key={attendee.id} className="rounded-lg border p-3">
                  <div className="flex items-center justify-between gap-3">
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium">{attendee.userId}</p>
                      <p className="text-xs text-muted-foreground">
                        {attendee.enrolledAt ? `Enrolled ${new Date(attendee.enrolledAt).toLocaleDateString('en-US')}` : 'Enrollment date not provided'}
                      </p>
                    </div>
                    <Badge variant="outline">{attendee.status}</Badge>
                  </div>
                  <div className="mt-3 h-2 rounded-full bg-muted">
                    <div className="h-2 rounded-full bg-primary" style={{ width: `${attendee.progress}%` }} />
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">{attendee.progress}% progress</p>
                </div>
              ))
            )}
          </CardContent>
        </Card>
        <ClassDetailActions courseId={courseId} classDetail={classDetail} />
      </div>
    </div>
  );
}
