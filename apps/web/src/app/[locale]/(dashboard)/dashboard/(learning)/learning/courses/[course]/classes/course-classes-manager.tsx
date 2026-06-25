'use client';

import {
  createCourseClass,
  deleteCourseClass,
  updateCourseClassStatus,
  type CourseClassStatusAction,
} from '@/lib/learning/actions';
import type { CourseClass } from '@/lib/learning/queries/course';
import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { CalendarClock, Loader2, Plus, Trash2, Users, Video } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useMemo, useState, useTransition } from 'react';

interface CourseClassesManagerProps {
  courseId: string;
  courseTitle: string;
  classes: CourseClass[];
}

function toDateTimeInputValue(date: Date): string {
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60000);
  return local.toISOString().slice(0, 16);
}

function getInitialWindow() {
  const start = new Date();
  start.setDate(start.getDate() + 7);
  start.setMinutes(0, 0, 0);

  const end = new Date(start);
  end.setHours(end.getHours() + 2);

  return {
    start: toDateTimeInputValue(start),
    end: toDateTimeInputValue(end),
  };
}

const statusActions: Array<{ value: CourseClassStatusAction; label: string }> = [
  { value: 'open', label: 'Open' },
  { value: 'close', label: 'Close' },
  { value: 'complete', label: 'Complete' },
  { value: 'cancel', label: 'Cancel' },
];

export function CourseClassesManager({ courseId, courseTitle, classes }: CourseClassesManagerProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const initialWindow = useMemo(() => getInitialWindow(), []);
  const [items, setItems] = useState(classes);
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [startDate, setStartDate] = useState(initialWindow.start);
  const [endDate, setEndDate] = useState(initialWindow.end);
  const [maxCapacity, setMaxCapacity] = useState('24');
  const [meetingSchedule, setMeetingSchedule] = useState('');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const submitClass = () => {
    setMessage(null);
    const submittedName = name.trim();
    const submittedDescription = description.trim();
    const submittedMeeting = meetingSchedule.trim();
    const submittedCapacity = Number.parseInt(maxCapacity, 10);
    const submittedStart = startDate;
    const submittedEnd = endDate;
    startTransition(async () => {
      const result = await createCourseClass({
        courseId,
        name: submittedName,
        description: submittedDescription,
        startDate: submittedStart,
        endDate: submittedEnd,
        maxCapacity: submittedCapacity,
        meetingSchedule: submittedMeeting,
      });

      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      const duration = Math.max(0, Math.round((new Date(submittedEnd).getTime() - new Date(submittedStart).getTime()) / 60000));
      const now = new Date().toISOString();
      setItems((current) => [
        ...current,
        {
          id: result.data.id,
          title: submittedName,
          description: submittedDescription,
          status: 'scheduled',
          scheduledAt: new Date(submittedStart).toISOString(),
          duration,
          timezone: 'UTC',
          location: submittedMeeting ? { type: submittedMeeting.startsWith('http') ? 'virtual' : 'physical', meetingUrl: submittedMeeting } : undefined,
          attendeeCount: 0,
          maxAttendees: submittedCapacity,
          materials: [],
          createdAt: now,
          updatedAt: now,
        },
      ]);
      setName('');
      setDescription('');
      setMeetingSchedule('');
      setMessage({ type: 'success', text: 'Class scheduled.' });
      router.refresh();
    });
  };

  const runStatusAction = (classId: string, statusAction: CourseClassStatusAction) => {
    setMessage(null);
    startTransition(async () => {
      const result = await updateCourseClassStatus(courseId, classId, statusAction);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      const nextStatus = statusAction === 'open' ? 'live' : statusAction === 'complete' ? 'completed' : statusAction === 'cancel' ? 'cancelled' : 'scheduled';
      setItems((current) => current.map((item) => item.id === classId ? { ...item, status: nextStatus } : item));
      setMessage({ type: 'success', text: 'Class status updated.' });
      router.refresh();
    });
  };

  const removeClass = (classId: string) => {
    setMessage(null);
    startTransition(async () => {
      const result = await deleteCourseClass(courseId, classId);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setItems((current) => current.filter((item) => item.id !== classId));
      setMessage({ type: 'success', text: 'Class deleted.' });
      router.refresh();
    });
  };

  return (
    <div className="grid min-w-0 max-w-full gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(280px,360px)]">
      <Card className="min-w-0">
        <CardHeader className="min-w-0">
          <CardTitle className="flex items-center gap-2">
            <CalendarClock className="size-5" />
            Course Schedule
          </CardTitle>
          <CardDescription className="break-words">{courseTitle} cohort sessions and live delivery schedule.</CardDescription>
        </CardHeader>
        <CardContent className="min-w-0 space-y-3">
          {items.length === 0 ? (
            <div className="min-w-0 rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No cohorts or live sessions are scheduled for this course.</div>
          ) : (
            items.map((courseClass) => (
              <div key={courseClass.id} className="min-w-0 space-y-3 rounded-lg border p-4">
                <div className="flex min-w-0 flex-col gap-3 md:flex-row md:items-start md:justify-between">
                  <Link href={`/dashboard/learning/courses/${courseId}/classes/${courseClass.id}`} className="min-w-0 flex-1 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="break-words font-medium">{courseClass.title}</p>
                      <Badge variant="outline">{courseClass.status}</Badge>
                    </div>
                    <p className="text-sm text-muted-foreground">{new Date(courseClass.scheduledAt).toLocaleString('en-US')} - {courseClass.duration} min</p>
                    {courseClass.description ? <p className="line-clamp-2 text-sm text-muted-foreground">{courseClass.description}</p> : null}
                  </Link>
                  <div className="flex items-center gap-4 text-sm text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Users className="size-4" />
                      {courseClass.attendeeCount}/{courseClass.maxAttendees ?? 'Unlimited'}
                    </span>
                    {courseClass.location?.meetingUrl ? <Video className="size-4" /> : null}
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  {statusActions.map((action) => (
                    <Button
                      key={action.value}
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={isPending}
                      onClick={() => runStatusAction(courseClass.id, action.value)}
                    >
                      {action.label}
                    </Button>
                  ))}
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isPending || courseClass.attendeeCount > 0}
                    onClick={() => removeClass(courseClass.id)}
                    aria-label={`Delete ${courseClass.title}`}
                  >
                    <Trash2 className="mr-2 size-4" />
                    Delete
                  </Button>
                </div>
              </div>
            ))
          )}
        </CardContent>
      </Card>

      <Card className="min-w-0">
        <CardHeader className="min-w-0">
          <CardTitle className="flex items-center gap-2 text-lg">
            <Plus className="size-4" />
            Schedule class
          </CardTitle>
          <CardDescription className="break-words">Create a cohort/live session through the Learning.Cohorts API.</CardDescription>
        </CardHeader>
        <CardContent className="min-w-0 space-y-4">
          <div className="space-y-2">
            <Label htmlFor="class-name">Name</Label>
            <Input id="class-name" value={name} onChange={(event) => setName(event.target.value)} placeholder="June production cohort" disabled={isPending} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="class-description">Description</Label>
            <Textarea id="class-description" value={description} onChange={(event) => setDescription(event.target.value)} rows={3} disabled={isPending} />
          </div>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="class-start">Start</Label>
              <Input id="class-start" type="datetime-local" value={startDate} onChange={(event) => setStartDate(event.target.value)} disabled={isPending} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="class-end">End</Label>
              <Input id="class-end" type="datetime-local" value={endDate} onChange={(event) => setEndDate(event.target.value)} disabled={isPending} />
            </div>
          </div>
          <div className="space-y-2">
            <Label htmlFor="class-capacity">Capacity</Label>
            <Input id="class-capacity" type="number" min={1} value={maxCapacity} onChange={(event) => setMaxCapacity(event.target.value)} disabled={isPending} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="class-meeting">Meeting URL or room</Label>
            <Input id="class-meeting" value={meetingSchedule} onChange={(event) => setMeetingSchedule(event.target.value)} placeholder="https://meet.example/session" disabled={isPending} />
          </div>
          {message ? (
            <p role={message.type === 'success' ? 'status' : 'alert'} className={message.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>
              {message.text}
            </p>
          ) : null}
          <Button type="button" onClick={submitClass} disabled={isPending} className="w-full">
            {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Plus className="mr-2 size-4" />}
            Schedule class
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
