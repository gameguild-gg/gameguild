'use client';

import {
  updateCourseClass,
  updateCourseClassStatus,
  type CourseClassStatusAction,
} from '@/lib/learning/actions';
import type { CourseClassDetail } from '@/lib/learning/queries/course';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Loader2, Save, Settings } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useMemo, useState, useTransition } from 'react';

interface ClassDetailActionsProps {
  courseId: string;
  classDetail: CourseClassDetail;
}

const statusActions: Array<{ value: CourseClassStatusAction; label: string }> = [
  { value: 'open', label: 'Open enrollment' },
  { value: 'close', label: 'Close enrollment' },
  { value: 'complete', label: 'Mark complete' },
  { value: 'cancel', label: 'Cancel class' },
];

function toDateTimeInputValue(date: Date): string {
  const offset = date.getTimezoneOffset();
  const local = new Date(date.getTime() - offset * 60000);
  return local.toISOString().slice(0, 16);
}

export function ClassDetailActions({ courseId, classDetail }: ClassDetailActionsProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const initialEnd = useMemo(() => new Date(new Date(classDetail.scheduledAt).getTime() + classDetail.duration * 60000), [classDetail.duration, classDetail.scheduledAt]);
  const [title, setTitle] = useState(classDetail.title);
  const [description, setDescription] = useState(classDetail.description);
  const [startDate, setStartDate] = useState(toDateTimeInputValue(new Date(classDetail.scheduledAt)));
  const [endDate, setEndDate] = useState(toDateTimeInputValue(initialEnd));
  const [capacity, setCapacity] = useState(String(classDetail.maxAttendees ?? 24));
  const [meeting, setMeeting] = useState(classDetail.location?.meetingUrl ?? '');
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const saveChanges = () => {
    setMessage(null);
    startTransition(async () => {
      const result = await updateCourseClass({
        courseId,
        classId: classDetail.id,
        name: title,
        description,
        startDate,
        endDate,
        maxCapacity: Number.parseInt(capacity, 10),
        meetingSchedule: meeting,
      });

      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setMessage({ type: 'success', text: 'Class updated.' });
      router.refresh();
    });
  };

  const runStatusAction = (statusAction: CourseClassStatusAction) => {
    setMessage(null);
    startTransition(async () => {
      const result = await updateCourseClassStatus(courseId, classDetail.id, statusAction);
      if (!result.success) {
        setMessage({ type: 'error', text: result.error });
        return;
      }

      setMessage({ type: 'success', text: 'Class status updated.' });
      router.refresh();
    });
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-lg">
          <Settings className="size-4" />
          Class controls
        </CardTitle>
        <CardDescription>Edit schedule details and manage enrollment status.</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex flex-wrap gap-2">
          <Badge>{classDetail.status}</Badge>
          {statusActions.map((action) => (
            <Button key={action.value} type="button" size="sm" variant="outline" disabled={isPending} onClick={() => runStatusAction(action.value)}>
              {action.label}
            </Button>
          ))}
        </div>
        <div className="space-y-2">
          <Label htmlFor="class-detail-title">Name</Label>
          <Input id="class-detail-title" value={title} onChange={(event) => setTitle(event.target.value)} disabled={isPending} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="class-detail-description">Description</Label>
          <Textarea id="class-detail-description" value={description} onChange={(event) => setDescription(event.target.value)} rows={3} disabled={isPending} />
        </div>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="class-detail-start">Start</Label>
            <Input id="class-detail-start" type="datetime-local" value={startDate} onChange={(event) => setStartDate(event.target.value)} disabled={isPending} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="class-detail-end">End</Label>
            <Input id="class-detail-end" type="datetime-local" value={endDate} onChange={(event) => setEndDate(event.target.value)} disabled={isPending} />
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="class-detail-capacity">Capacity</Label>
          <Input id="class-detail-capacity" type="number" min={1} value={capacity} onChange={(event) => setCapacity(event.target.value)} disabled={isPending} />
        </div>
        <div className="space-y-2">
          <Label htmlFor="class-detail-meeting">Meeting URL or room</Label>
          <Input id="class-detail-meeting" value={meeting} onChange={(event) => setMeeting(event.target.value)} disabled={isPending} />
        </div>
        {message ? (
          <p role={message.type === 'success' ? 'status' : 'alert'} className={message.type === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>
            {message.text}
          </p>
        ) : null}
        <Button type="button" onClick={saveChanges} disabled={isPending} className="w-full">
          {isPending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Save className="mr-2 size-4" />}
          Save class
        </Button>
      </CardContent>
    </Card>
  );
}
