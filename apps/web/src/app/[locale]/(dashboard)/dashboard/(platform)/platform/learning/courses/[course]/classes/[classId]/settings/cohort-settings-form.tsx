'use client';

import { updateCohort, updateCohortStatus } from '@/lib/learning/actions/cohorts';
import type { CourseCohortDetail } from '@/lib/learning/queries/cohorts';
import { useRouter } from '@/i18n/navigation';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@game-guild/ui/components/alert-dialog';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { Ban, CheckCircle2, Loader2, Lock, Save, Unlock } from 'lucide-react';
import { FormEvent, useState, useTransition } from 'react';

interface CohortSettingsFormProps {
  courseId: string;
  cohort: CourseCohortDetail;
}

export function CohortSettingsForm({ courseId, cohort }: CohortSettingsFormProps) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [message, setMessage] = useState<{ kind: 'success' | 'error'; text: string } | null>(null);

  const save = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setMessage(null);
    const data = new FormData(event.currentTarget);
    const startDate = String(data.get('startDate') ?? '');
    const endDate = String(data.get('endDate') ?? '');

    startTransition(async () => {
      const result = await updateCohort({
        courseId,
        cohortId: cohort.id,
        name: String(data.get('name') ?? ''),
        description: String(data.get('description') ?? ''),
        startDate: `${startDate}T00:00:00`,
        endDate: `${endDate}T23:59:59`,
        maxCapacity: Number.parseInt(String(data.get('capacity') ?? ''), 10),
        meetingSchedule: String(data.get('meetingPattern') ?? ''),
      });

      setMessage(result.success ? { kind: 'success', text: 'Class settings saved.' } : { kind: 'error', text: result.error });
      if (result.success) router.refresh();
    });
  };

  const runStatus = (action: 'open' | 'close' | 'complete' | 'cancel') => {
    setMessage(null);
    startTransition(async () => {
      const result = await updateCohortStatus(courseId, cohort.id, action);
      setMessage(result.success ? { kind: 'success', text: 'Class status updated.' } : { kind: 'error', text: result.error });
      if (result.success) router.refresh();
    });
  };

  return (
    <div className="space-y-6">
      <form onSubmit={save} className="space-y-5 rounded-lg border p-5">
        <div className="flex items-center justify-between gap-4">
          <div><h3 className="font-medium">Class settings</h3><p className="mt-1 text-sm text-muted-foreground">Period, capacity, and meeting identity.</p></div>
          <Badge variant="outline" className="capitalize">{cohort.status}</Badge>
        </div>

        <div className="space-y-2"><Label htmlFor="settings-name">Name</Label><Input id="settings-name" name="name" defaultValue={cohort.name} minLength={3} required disabled={pending} /></div>
        <div className="space-y-2"><Label htmlFor="settings-description">Description</Label><Textarea id="settings-description" name="description" defaultValue={cohort.description} rows={3} disabled={pending} /></div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2"><Label htmlFor="settings-start">Start date</Label><Input id="settings-start" name="startDate" type="date" defaultValue={cohort.period.startsAt.slice(0, 10)} required disabled={pending} /></div>
          <div className="space-y-2"><Label htmlFor="settings-end">End date</Label><Input id="settings-end" name="endDate" type="date" defaultValue={cohort.period.endsAt.slice(0, 10)} required disabled={pending} /></div>
        </div>
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2"><Label htmlFor="settings-capacity">Capacity</Label><Input id="settings-capacity" name="capacity" type="number" min={1} defaultValue={cohort.enrollment.capacity ?? 24} required disabled={pending} /></div>
          <div className="space-y-2"><Label htmlFor="settings-pattern">Meeting pattern</Label><Input id="settings-pattern" name="meetingPattern" defaultValue={cohort.meetingPattern ?? ''} placeholder="Tue/Thu - 19:00" disabled={pending} /></div>
        </div>
        {message ? <p role={message.kind === 'success' ? 'status' : 'alert'} className={message.kind === 'success' ? 'text-sm text-emerald-600' : 'text-sm text-destructive'}>{message.text}</p> : null}
        <Button type="submit" disabled={pending}>{pending ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}Save settings</Button>
      </form>

      <section className="space-y-4 rounded-lg border p-5">
        <div><h3 className="font-medium">Enrollment and lifecycle</h3><p className="mt-1 text-sm text-muted-foreground">Current enrollment: {cohort.enrollment.current} students.</p></div>
        <div className="flex flex-wrap gap-2">
          {cohort.isOpen ? (
            <Button variant="outline" onClick={() => runStatus('close')} disabled={pending}><Lock className="size-4" />Close enrollment</Button>
          ) : (
            <Button variant="outline" onClick={() => runStatus('open')} disabled={pending}><Unlock className="size-4" />Open enrollment</Button>
          )}
          <Button variant="outline" onClick={() => runStatus('complete')} disabled={pending || cohort.status === 'completed'}><CheckCircle2 className="size-4" />Mark complete</Button>
          <AlertDialog>
            <AlertDialogTrigger asChild><Button variant="destructive" disabled={pending || cohort.status === 'cancelled'}><Ban className="size-4" />Cancel class</Button></AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader><AlertDialogTitle>Cancel {cohort.name}?</AlertDialogTitle><AlertDialogDescription>Enrollment will close and the class schedule will no longer be active. Existing student records remain preserved.</AlertDialogDescription></AlertDialogHeader>
              <AlertDialogFooter><AlertDialogCancel>Keep class</AlertDialogCancel><AlertDialogAction variant="destructive" onClick={() => runStatus('cancel')}>Cancel class</AlertDialogAction></AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </section>
    </div>
  );
}
