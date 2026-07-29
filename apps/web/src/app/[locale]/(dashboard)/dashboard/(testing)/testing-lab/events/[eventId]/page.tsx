import {
  CreateTestingEventSlotDialog,
  EditTestingEventDialog,
  ManageTestingEventSlotDialog,
  TestingEventApplications,
  TestingEventCommittee,
  TestingEventLearningDialog,
  TestingEventLifecycleActions,
  TestingSlotRegistrations,
} from '@/components/testing-lab/testing-event-management';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues } from '@/components/testing-lab/testing-lab-state';
import { getMembers } from '@/lib/community/queries/members';
import { getCourses } from '@/lib/courses/services/course.service';
import { getCourseContent } from '@/lib/learning/queries/course';
import { getTestingEventManagerData } from '@/lib/testing-lab/events-queries';
import { formatTestingEventStatus } from '@/lib/testing-lab/format';
import type { TestingLabTestingApplicationStatus } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { CalendarDays, FlaskConical, MapPin } from 'lucide-react';
import { notFound } from 'next/navigation';

function dateTime(value?: string | null) {
  if (!value) return 'Not scheduled';
  const date = new Date(value);
  return Number.isNaN(date.valueOf())
    ? 'Not scheduled'
    : new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short' }).format(date);
}

function capacity(current?: number, maximum?: number | null) {
  return maximum ? `${current ?? 0}/${maximum}` : `${current ?? 0}/unlimited`;
}

export default async function TestingEventDetailPage({
  params,
  searchParams,
}: {
  params: Promise<{ eventId: string }>;
  searchParams: Promise<{ applicationStatus?: string }>;
}) {
  const [{ eventId }, query] = await Promise.all([params, searchParams]);
  const applicationStatuses: TestingLabTestingApplicationStatus[] = ['Pending', 'UnderReview', 'Approved', 'Rejected', 'Waitlisted', 'Withdrawn'];
  const applicationStatus = applicationStatuses.find((status) => status === query.applicationStatus);
  const [detail, memberDirectory, courses] = await Promise.all([
    getTestingEventManagerData(eventId, { applicationStatus }),
    getMembers({ page: 1, limit: 100 }),
    getCourses(),
  ]);
  const courseContent = await Promise.all(
    courses
      .filter((course) => course.id)
      .slice(0, 50)
      .map(async (course) => ({
        courseId: String(course.id),
        courseTitle: course.title ?? course.slug ?? 'Untitled course',
        content: await getCourseContent(String(course.id)),
      })),
  );
  const learningActivities = courseContent.flatMap((course) =>
    course.content.items.map((item) => ({
      id: item.id,
      courseId: course.courseId,
      label: `${course.courseTitle} · ${item.title || item.type}`,
    })),
  );
  if (!detail.event && detail.accessIssues.length === 0) notFound();
  if (!detail.event) {
    return <main className="p-6"><TestingLabAccessIssues issues={detail.accessIssues} /></main>;
  }
  const event = detail.event;

  return (
    <main className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={FlaskConical}
        title={event.name ?? 'Testing event'}
        description={event.description ?? 'Project applications, independent tester slots, attendance, and required feedback.'}
        actions={<EditTestingEventDialog event={event} />}
      />
      <TestingLabAccessIssues issues={detail.accessIssues} />
      <section className="flex flex-col gap-4 rounded-md border p-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-wrap items-center gap-2">
          <Badge>{formatTestingEventStatus(event.status)}</Badge>
          <Badge variant="outline">{event.mode}</Badge>
          <Badge variant="outline">{event.approvalMode === 'Committee' ? 'Committee review' : 'Manager decision'}</Badge>
          {event.requiresFeedback ? <Badge variant="secondary">Feedback required</Badge> : null}
        </div>
        <TestingEventLifecycleActions event={event} />
      </section>

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {[
          ['Applications', detail.applications.length, 'Project candidates'],
          ['Slots', detail.slots.length, 'Independent schedules'],
          ['Registered testers', Object.values(detail.registrationsBySlot).flat().length, 'Across all slots'],
          ['Event starts', dateTime(event.startsAt), dateTime(event.endsAt)],
        ].map(([label, value, note]) => (
          <div key={label} className="rounded-md border p-4">
            <p className="text-sm text-muted-foreground">{label}</p>
            <p className="mt-2 text-xl font-semibold">{value}</p>
            <p className="mt-1 text-xs text-muted-foreground">{note}</p>
          </div>
        ))}
      </section>

      <section>
        <div className="mb-3 flex items-end justify-between gap-3">
          <div>
            <h2 className="text-lg font-semibold">Schedule and capacity</h2>
            <p className="text-sm text-muted-foreground">Each slot has its own time, location, tester capacity, and approved-project capacity.</p>
          </div>
          {event.id ? <CreateTestingEventSlotDialog eventId={event.id} /> : null}
        </div>
        {detail.slots.length === 0 ? (
          <p className="rounded-md border border-dashed p-6 text-center text-sm text-muted-foreground">No testing slots scheduled.</p>
        ) : (
          <div className="space-y-3">
            {detail.slots.map((slot) => (
              <article key={slot.id} className="rounded-md border p-4">
                <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="font-semibold">{dateTime(slot.startsAt)}</h3>
                      <Badge variant="outline">{slot.mode}</Badge>
                    </div>
                    <p className="mt-1 flex items-center gap-2 text-sm text-muted-foreground">
                      <MapPin className="size-4" />
                      {slot.mode === 'Online'
                        ? slot.meetingUrl ?? 'Online link not configured'
                        : `${slot.campusName ?? 'Campus not set'} · ${slot.roomName ?? 'Room not set'}`}
                    </p>
                  </div>
                  <div className="flex gap-5 text-sm">
                    <span><strong>{capacity(slot.registeredTesterCount, slot.maxTesters)}</strong><br /><span className="text-xs text-muted-foreground">testers</span></span>
                    <span><strong>{capacity(slot.approvedProjectCount, slot.maxProjects)}</strong><br /><span className="text-xs text-muted-foreground">projects</span></span>
                  </div>
                </div>
                {event.id ? (
                  <div className="mt-3 flex justify-end border-t pt-3">
                    <ManageTestingEventSlotDialog eventId={event.id} slot={slot} />
                  </div>
                ) : null}
                {event.id && slot.id ? (
                  <TestingSlotRegistrations eventId={event.id} registrations={detail.registrationsBySlot[slot.id] ?? []} />
                ) : null}
              </article>
            ))}
          </div>
        )}
      </section>

      <section>
        <div className="mb-3">
          <h2 className="text-lg font-semibold">Project applications</h2>
          <p className="text-sm text-muted-foreground">Applications do not consume slot capacity until the manager approves and assigns one.</p>
        </div>
        {event.id ? <TestingEventApplications eventId={event.id} applications={detail.applications} slots={detail.slots} /> : null}
      </section>

      <section className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_420px]">
        <div className="rounded-md border p-4">
          <div className="mb-3 flex items-center justify-between gap-3">
            <div className="flex items-center gap-2">
              <CalendarDays className="size-4" />
              <h2 className="font-semibold">Learning evidence</h2>
            </div>
            <TestingEventLearningDialog event={event} activities={learningActivities} />
          </div>
          <p className="text-sm text-muted-foreground">
            {event.courseId
              ? `Linked to course ${event.courseId}. Completion requirement: ${formatTestingEventStatus(event.learningCompletionRequirement)}.`
              : 'This event is not linked to a course activity. Attendance and feedback remain available as Testing Lab evidence.'}
          </p>
        </div>
        <div className="rounded-md border p-4">
          <TestingEventCommittee
            event={event}
            members={memberDirectory.members.map((member) => ({ id: member.id, label: `${member.displayName} · ${member.email}` }))}
            committee={detail.committee}
          />
        </div>
      </section>
    </main>
  );
}
