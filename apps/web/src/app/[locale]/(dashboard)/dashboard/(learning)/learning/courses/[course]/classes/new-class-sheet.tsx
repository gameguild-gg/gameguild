'use client';

import { createCohort } from '@/lib/learning/actions/cohorts';
import { useRouter } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@game-guild/ui/components/sheet';
import { Textarea } from '@game-guild/ui/components/textarea';
import { ArrowRight, Loader2, Plus } from 'lucide-react';
import { FormEvent, useState, useTransition } from 'react';

interface NewClassSheetProps {
  courseId: string;
}

export function NewClassSheet({ courseId }: NewClassSheetProps) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    const data = new FormData(event.currentTarget);
    const startDate = String(data.get('startDate') ?? '');
    const endDate = String(data.get('endDate') ?? '');
    const capacity = Number.parseInt(String(data.get('capacity') ?? ''), 10);

    startTransition(async () => {
      const result = await createCohort({
        courseId,
        name: String(data.get('name') ?? ''),
        description: String(data.get('description') ?? ''),
        startDate: `${startDate}T00:00:00`,
        endDate: `${endDate}T23:59:59`,
        maxCapacity: capacity,
        meetingSchedule: String(data.get('meetingPattern') ?? ''),
      });

      if (!result.success) {
        setError(result.error);
        return;
      }

      setOpen(false);
      router.refresh();
      router.push(`/dashboard/learning/courses/${courseId}/classes/${result.data.id}/schedule`);
    });
  };

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button>
          <Plus className="size-4" />
          New class
        </Button>
      </SheetTrigger>
      <SheetContent className="w-full overflow-y-auto sm:max-w-xl">
        <SheetHeader className="border-b px-6 py-5">
          <SheetTitle>Create class</SheetTitle>
          <SheetDescription>
            Define the cohort period first. Build its meetings, releases, and due dates in the next step.
          </SheetDescription>
        </SheetHeader>

        <form onSubmit={submit} className="flex min-h-0 flex-1 flex-col">
          <div className="flex-1 space-y-5 px-6 py-5">
            <div className="space-y-2">
              <Label htmlFor="cohort-name">Class name</Label>
              <Input id="cohort-name" name="name" placeholder="2026.2 - Evening" minLength={3} required disabled={pending} />
              <p className="text-xs text-muted-foreground">Use a name that distinguishes period, shift, or audience.</p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="cohort-description">Description</Label>
              <Textarea id="cohort-description" name="description" rows={3} placeholder="Evening cohort for working professionals." disabled={pending} />
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="cohort-start">Start date</Label>
                <Input id="cohort-start" name="startDate" type="date" required disabled={pending} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="cohort-end">End date</Label>
                <Input id="cohort-end" name="endDate" type="date" required disabled={pending} />
              </div>
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="cohort-capacity">Capacity</Label>
                <Input id="cohort-capacity" name="capacity" type="number" min={1} defaultValue={24} required disabled={pending} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="cohort-meeting-pattern">Meeting pattern</Label>
                <Input id="cohort-meeting-pattern" name="meetingPattern" placeholder="Tue/Thu - 19:00" disabled={pending} />
              </div>
            </div>

            <div className="rounded-md border bg-muted/30 p-4 text-sm">
              <p className="font-medium">Next: build the class schedule</p>
              <p className="mt-1 text-muted-foreground">Choose the timezone, meeting days, weekly pacing, content release policy, and skipped dates.</p>
            </div>

            {error ? <p role="alert" className="text-sm text-destructive">{error}</p> : null}
          </div>

          <SheetFooter className="border-t px-6 py-4 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={() => setOpen(false)} disabled={pending}>Cancel</Button>
            <Button type="submit" disabled={pending}>
              {pending ? <Loader2 className="size-4 animate-spin" /> : <ArrowRight className="size-4" />}
              Create and build schedule
            </Button>
          </SheetFooter>
        </form>
      </SheetContent>
    </Sheet>
  );
}
