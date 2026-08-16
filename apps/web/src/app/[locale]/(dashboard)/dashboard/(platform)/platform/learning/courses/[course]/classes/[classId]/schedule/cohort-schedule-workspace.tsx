'use client';

import type {
  LearningCohortsCohortSchedule,
  LearningCohortsCohortScheduleItem,
  LearningCohortsCohortScheduleItemStatus,
  LearningCohortsCohortVisibilityOverride,
  LearningCohortsScheduleShiftScope,
} from '@game-guild/client';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { RadioGroup, RadioGroupItem } from '@game-guild/ui/components/radio-group';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@game-guild/ui/components/tabs';
import { AlertTriangle, CalendarDays, Clock3, ListTree, Loader2, MoveRight, Pencil, ShieldCheck } from 'lucide-react';
import { useEffect, useState } from 'react';

import { shiftCohortScheduleItem, updateCohortScheduleItem } from '@/lib/learning/actions/cohorts';
import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { CalendarView } from './calendar-view';
import { ScheduleBuilderSheet } from './schedule-builder-sheet';
import { SyllabusView } from './syllabus-view';
import { TimelineView } from './timeline-view';

type ScheduleView = 'syllabus' | 'calendar' | 'timeline';

interface CohortScheduleWorkspaceProps {
  courseId: string;
  cohort: CourseCohortSummary;
  initialSchedule: LearningCohortsCohortSchedule | null;
}

interface EditItemForm {
  title: string;
  location: string;
  meetingUrl: string;
  status: LearningCohortsCohortScheduleItemStatus;
  visibilityOverride: LearningCohortsCohortVisibilityOverride;
}

function editForm(item: LearningCohortsCohortScheduleItem): EditItemForm {
  return {
    title: item.title?.trim() || '',
    location: item.location?.trim() || '',
    meetingUrl: item.meetingUrl?.trim() || '',
    status: item.status ?? 'Scheduled',
    visibilityOverride: item.visibilityOverride ?? 'Inherited',
  };
}

export function CohortScheduleWorkspace({ courseId, cohort, initialSchedule }: CohortScheduleWorkspaceProps) {
  const [schedule, setSchedule] = useState(initialSchedule);
  const [view, setView] = useState<ScheduleView>('syllabus');
  const [shiftItem, setShiftItem] = useState<LearningCohortsCohortScheduleItem | null>(null);
  const [shiftDays, setShiftDays] = useState('7');
  const [shiftScope, setShiftScope] = useState<LearningCohortsScheduleShiftScope>('Single');
  const [editItem, setEditItem] = useState<LearningCohortsCohortScheduleItem | null>(null);
  const [editValues, setEditValues] = useState<EditItemForm | null>(null);
  const [pending, setPending] = useState<'shift' | 'edit' | null>(null);
  const [mutationError, setMutationError] = useState<string | null>(null);
  const readOnly = cohort.status === 'completed' || cohort.status === 'cancelled';

  useEffect(() => {
    const query = window.matchMedia('(max-width: 767px)');
    if (query.matches) setView('timeline');
  }, []);

  const openEdit = (item: LearningCohortsCohortScheduleItem) => {
    setEditItem(item);
    setEditValues(editForm(item));
    setMutationError(null);
  };

  const submitShift = async () => {
    if (!schedule || !shiftItem?.id) return;
    const days = Number(shiftDays);
    if (!Number.isInteger(days) || days === 0) {
      setMutationError('Enter a non-zero whole number of days.');
      return;
    }
    setPending('shift');
    setMutationError(null);
    const result = await shiftCohortScheduleItem(courseId, cohort.id, shiftItem.id, {
      expectedVersion: schedule.version ?? 0,
      days,
      scope: shiftScope,
    });
    setPending(null);
    if (!result.success) {
      setMutationError(result.error);
      return;
    }
    setSchedule(result.data);
    setShiftItem(null);
  };

  const submitEdit = async () => {
    if (!schedule || !editItem?.id || !editValues) return;
    if (!editValues.title.trim()) {
      setMutationError('Schedule item title is required.');
      return;
    }
    setPending('edit');
    setMutationError(null);
    const result = await updateCohortScheduleItem(courseId, cohort.id, editItem.id, {
      expectedVersion: schedule.version ?? 0,
      item: {
        title: editValues.title.trim(),
        startsAt: editItem.startsAt,
        endsAt: editItem.endsAt,
        availableFrom: editItem.availableFrom,
        availableUntil: editItem.availableUntil,
        dueAt: editItem.dueAt,
        location: editValues.location.trim() || null,
        meetingUrl: editValues.meetingUrl.trim() || null,
        status: editValues.status,
        visibilityOverride: editValues.visibilityOverride,
      },
    });
    setPending(null);
    if (!result.success) {
      setMutationError(result.error);
      return;
    }
    setSchedule(result.data);
    setEditItem(null);
    setEditValues(null);
  };

  return (
    <div className="min-w-0 space-y-5">
      <header className="flex flex-col gap-3 border-b pb-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-xl font-semibold">Class schedule</h2>
            {schedule ? <Badge variant="outline">Version {schedule.version ?? 0}</Badge> : <Badge variant="secondary">Not configured</Badge>}
          </div>
          <p className="mt-1 text-sm text-muted-foreground">
            Plan content release, live meetings, and assessments for this class without changing the canonical course.
          </p>
        </div>
        {!readOnly ? (
          <ScheduleBuilderSheet courseId={courseId} cohort={cohort} schedule={schedule} onApplied={setSchedule} />
        ) : null}
      </header>

      {readOnly ? (
        <Alert>
          <ShieldCheck />
          <AlertTitle>Completed classes are read only.</AlertTitle>
          <AlertDescription>The schedule remains available for records and reporting, but it can no longer be changed.</AlertDescription>
        </Alert>
      ) : null}

      {mutationError && !shiftItem && !editItem ? (
        <Alert variant="destructive">
          <AlertTriangle />
          <AlertTitle>Schedule update failed</AlertTitle>
          <AlertDescription>{mutationError}</AlertDescription>
        </Alert>
      ) : null}

      {schedule ? (
        <>
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="flex items-center gap-3 rounded-md border px-3 py-2.5">
              <Clock3 className="size-4 text-muted-foreground" />
              <div className="min-w-0"><p className="text-xs text-muted-foreground">Timezone</p><p className="truncate text-sm font-medium">{schedule.timezoneId || 'UTC'}</p></div>
            </div>
            <div className="flex items-center gap-3 rounded-md border px-3 py-2.5">
              <CalendarDays className="size-4 text-muted-foreground" />
              <div className="min-w-0"><p className="text-xs text-muted-foreground">Meeting days</p><p className="truncate text-sm font-medium">{schedule.meetingDays?.join(', ') || 'Manual'}</p></div>
            </div>
            <div className="flex items-center gap-3 rounded-md border px-3 py-2.5">
              <ListTree className="size-4 text-muted-foreground" />
              <div className="min-w-0"><p className="text-xs text-muted-foreground">Scheduled items</p><p className="text-sm font-medium">{schedule.items?.length ?? 0}</p></div>
            </div>
          </div>

          {(schedule.unscheduledContentIds?.length ?? 0) > 0 ? (
            <Alert>
              <AlertTriangle />
              <AlertTitle>Curriculum changed after this schedule was built</AlertTitle>
              <AlertDescription>{schedule.unscheduledContentIds?.length} course items still need dates. Generate a new preview before the class starts.</AlertDescription>
            </Alert>
          ) : null}

          <Tabs value={view} onValueChange={(value) => setView(value as ScheduleView)}>
            <TabsList variant="line" className="w-full justify-start overflow-x-auto border-b">
              <TabsTrigger value="syllabus"><ListTree />Syllabus</TabsTrigger>
              <TabsTrigger value="calendar"><CalendarDays />Calendar</TabsTrigger>
              <TabsTrigger value="timeline"><Clock3 />Timeline</TabsTrigger>
            </TabsList>
            <TabsContent value="syllabus" className="pt-3">
              <SyllabusView schedule={schedule} readOnly={readOnly} onEdit={openEdit} onShift={(item) => { setShiftItem(item); setMutationError(null); }} />
            </TabsContent>
            <TabsContent value="calendar" className="pt-3"><CalendarView schedule={schedule} /></TabsContent>
            <TabsContent value="timeline" className="pt-3"><TimelineView schedule={schedule} /></TabsContent>
          </Tabs>
        </>
      ) : (
        <div className="border-y border-dashed py-16 text-center">
          <CalendarDays className="mx-auto size-8 text-muted-foreground" />
          <h3 className="mt-4 font-medium">This class does not have a schedule yet</h3>
          <p className="mx-auto mt-1 max-w-lg text-sm text-muted-foreground">Define its cadence once, review the generated syllabus, and apply it only when the dates are correct.</p>
        </div>
      )}

      <Dialog open={Boolean(shiftItem)} onOpenChange={(open) => { if (!open) { setShiftItem(null); setMutationError(null); } }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Shift {shiftItem?.title || 'schedule item'}</DialogTitle>
            <DialogDescription>Move this date only, or preserve the cadence by moving this item and everything after it.</DialogDescription>
          </DialogHeader>
          {mutationError ? <p role="alert" className="text-sm text-destructive">{mutationError}</p> : null}
          <div className="space-y-2">
            <Label htmlFor="shift-days">Days to shift</Label>
            <Input id="shift-days" type="number" step={1} value={shiftDays} onChange={(event) => setShiftDays(event.target.value)} />
            <p className="text-xs text-muted-foreground">Use a negative value to move the schedule earlier.</p>
          </div>
          <RadioGroup value={shiftScope} onValueChange={(value) => setShiftScope(value as LearningCohortsScheduleShiftScope)}>
            <div className="flex items-start gap-3 rounded-md border p-3">
              <RadioGroupItem id="shift-single" value="Single" aria-label="Only this item" />
              <Label htmlFor="shift-single" className="cursor-pointer font-normal"><span className="block text-sm font-medium">Only this item</span><span className="block text-xs text-muted-foreground">Other schedule dates stay unchanged.</span></Label>
            </div>
            <div className="flex items-start gap-3 rounded-md border p-3">
              <RadioGroupItem id="shift-following" value="Following" aria-label="This and following items" />
              <Label htmlFor="shift-following" className="cursor-pointer font-normal"><span className="block text-sm font-medium">This and following items</span><span className="block text-xs text-muted-foreground">Keeps the remaining sequence aligned.</span></Label>
            </div>
          </RadioGroup>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setShiftItem(null)}>Cancel</Button>
            <Button type="button" onClick={submitShift} disabled={pending !== null}>
              {pending === 'shift' ? <Loader2 className="size-4 animate-spin" /> : <MoveRight className="size-4" />}
              Shift schedule item
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={Boolean(editItem)} onOpenChange={(open) => { if (!open) { setEditItem(null); setEditValues(null); setMutationError(null); } }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Edit schedule item</DialogTitle>
            <DialogDescription>Change delivery metadata for this class. Use Shift when dates need to move.</DialogDescription>
          </DialogHeader>
          {mutationError ? <p role="alert" className="text-sm text-destructive">{mutationError}</p> : null}
          {editValues ? (
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="schedule-item-title">Schedule item title</Label>
                <Input id="schedule-item-title" value={editValues.title} onChange={(event) => setEditValues({ ...editValues, title: event.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="schedule-item-status">Status</Label>
                <Select value={editValues.status} onValueChange={(value) => setEditValues({ ...editValues, status: value as LearningCohortsCohortScheduleItemStatus })}>
                  <SelectTrigger id="schedule-item-status" className="w-full"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Draft">Draft</SelectItem><SelectItem value="Scheduled">Scheduled</SelectItem><SelectItem value="Published">Published</SelectItem><SelectItem value="Completed">Completed</SelectItem><SelectItem value="Cancelled">Cancelled</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="schedule-item-visibility">Student visibility</Label>
                <Select value={editValues.visibilityOverride} onValueChange={(value) => setEditValues({ ...editValues, visibilityOverride: value as LearningCohortsCohortVisibilityOverride })}>
                  <SelectTrigger id="schedule-item-visibility" className="w-full"><SelectValue /></SelectTrigger>
                  <SelectContent><SelectItem value="Inherited">Follow course content</SelectItem><SelectItem value="Visible">Force visible</SelectItem><SelectItem value="Hidden">Hide from students</SelectItem></SelectContent>
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="schedule-item-location">Location</Label>
                <Input id="schedule-item-location" value={editValues.location} onChange={(event) => setEditValues({ ...editValues, location: event.target.value })} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="schedule-item-url">Meeting URL</Label>
                <Input id="schedule-item-url" type="url" value={editValues.meetingUrl} onChange={(event) => setEditValues({ ...editValues, meetingUrl: event.target.value })} />
              </div>
            </div>
          ) : null}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setEditItem(null)}>Cancel</Button>
            <Button type="button" onClick={submitEdit} disabled={pending !== null}>
              {pending === 'edit' ? <Loader2 className="size-4 animate-spin" /> : <Pencil className="size-4" />}
              Save schedule item
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
