'use client';

import type {
  LearningCohortsCohortPacingMode,
  LearningCohortsCohortReleasePolicy,
  LearningCohortsCohortSchedule,
  LearningCohortsCohortSchedulePreview,
  LearningCohortsPreviewCohortScheduleInput,
  SystemDayOfWeek,
} from '@game-guild/client';
import { Alert, AlertDescription, AlertTitle } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Checkbox } from '@game-guild/ui/components/checkbox';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@game-guild/ui/components/select';
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@game-guild/ui/components/sheet';
import { AlertTriangle, ArrowLeft, CalendarRange, CheckCircle2, Loader2, WandSparkles } from 'lucide-react';
import { useState } from 'react';

import { applyCohortSchedule, previewCohortSchedule } from '@/lib/learning/actions/cohorts';
import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { formatScheduleDate } from './schedule-view-utils';

interface ScheduleBuilderSheetProps {
  courseId: string;
  cohort: CourseCohortSummary;
  schedule: LearningCohortsCohortSchedule | null;
  readOnly?: boolean;
  onApplied: (schedule: LearningCohortsCohortSchedule) => void;
}

interface ScheduleRulesForm {
  firstInstructionalDate: string;
  cohortEndDate: string;
  timezoneId: string;
  meetingDays: SystemDayOfWeek[];
  meetingStartTime: string;
  meetingDurationMinutes: number;
  pacingMode: LearningCohortsCohortPacingMode;
  unitsPerPeriod: number;
  releasePolicy: LearningCohortsCohortReleasePolicy;
  skippedDates: string;
  assessmentDueOffsetDays: number;
}

const meetingDayOptions: Array<{ value: SystemDayOfWeek; label: string }> = [
  { value: 'Monday', label: 'Mon' },
  { value: 'Tuesday', label: 'Tue' },
  { value: 'Wednesday', label: 'Wed' },
  { value: 'Thursday', label: 'Thu' },
  { value: 'Friday', label: 'Fri' },
  { value: 'Saturday', label: 'Sat' },
  { value: 'Sunday', label: 'Sun' },
];

function datePart(value: string | null | undefined): string {
  return value?.slice(0, 10) || '';
}

function initialRules(cohort: CourseCohortSummary, schedule: LearningCohortsCohortSchedule | null): ScheduleRulesForm {
  return {
    firstInstructionalDate: datePart(cohort.period.startsAt),
    cohortEndDate: datePart(cohort.period.endsAt),
    timezoneId: schedule?.timezoneId?.trim() || 'UTC',
    meetingDays: schedule?.meetingDays?.length ? [...schedule.meetingDays] : ['Monday'],
    meetingStartTime: schedule?.meetingStartTime?.slice(0, 5) || '09:00',
    meetingDurationMinutes: schedule?.meetingDurationMinutes ?? 90,
    pacingMode: schedule?.pacingMode ?? 'OneModulePerWeek',
    unitsPerPeriod: schedule?.unitsPerPeriod ?? 1,
    releasePolicy: schedule?.releasePolicy ?? 'Weekly',
    skippedDates: '',
    assessmentDueOffsetDays: 0,
  };
}

function toPreviewInput(rules: ScheduleRulesForm): LearningCohortsPreviewCohortScheduleInput {
  return {
    firstInstructionalDate: rules.firstInstructionalDate,
    cohortEndDate: rules.cohortEndDate,
    timezoneId: rules.timezoneId.trim(),
    meetingDays: rules.meetingDays,
    meetingStartTime: `${rules.meetingStartTime}:00`,
    meetingDurationMinutes: rules.meetingDurationMinutes,
    pacingMode: rules.pacingMode,
    unitsPerPeriod: rules.unitsPerPeriod,
    releasePolicy: rules.releasePolicy,
    skippedDates: rules.skippedDates
      .split(/[\s,]+/)
      .map((value) => value.trim())
      .filter(Boolean),
    assessmentDueOffsetDays: rules.assessmentDueOffsetDays,
  };
}

export function ScheduleBuilderSheet({
  courseId,
  cohort,
  schedule,
  readOnly = false,
  onApplied,
}: ScheduleBuilderSheetProps) {
  const [open, setOpen] = useState(false);
  const [rules, setRules] = useState<ScheduleRulesForm>(() => initialRules(cohort, schedule));
  const [preview, setPreview] = useState<LearningCohortsCohortSchedulePreview | null>(null);
  const [pending, setPending] = useState<'preview' | 'apply' | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [advisoriesConfirmed, setAdvisoriesConfirmed] = useState(false);

  const updateRules = <K extends keyof ScheduleRulesForm>(key: K, value: ScheduleRulesForm[K]) => {
    setRules((current) => ({ ...current, [key]: value }));
    setPreview(null);
    setAdvisoriesConfirmed(false);
    setError(null);
  };

  const toggleMeetingDay = (day: SystemDayOfWeek, checked: boolean) => {
    updateRules(
      'meetingDays',
      checked ? [...new Set([...rules.meetingDays, day])] : rules.meetingDays.filter((value) => value !== day),
    );
  };

  const generatePreview = async () => {
    if (rules.meetingDays.length === 0) {
      setError('Select at least one meeting day.');
      return;
    }
    setPending('preview');
    setError(null);
    const result = await previewCohortSchedule(courseId, cohort.id, toPreviewInput(rules));
    setPending(null);
    if (!result.success) {
      setError(result.error);
      return;
    }
    setPreview(result.data);
  };

  const conflicts = preview?.conflicts ?? [];
  const hasBlockingConflicts = preview?.hasBlockingConflicts || conflicts.some((conflict) => conflict.severity === 'Blocking');
  const hasAdvisoryConflicts = conflicts.some((conflict) => conflict.severity === 'Advisory');
  const canApply = Boolean(preview) && !hasBlockingConflicts && (!hasAdvisoryConflicts || advisoriesConfirmed) && pending === null;

  const applyPreview = async () => {
    if (!preview || !canApply) return;
    setPending('apply');
    setError(null);
    const result = await applyCohortSchedule(courseId, cohort.id, {
      expectedVersion: schedule?.version ?? 0,
      rules: toPreviewInput(rules),
      confirmAdvisories: advisoriesConfirmed,
    });
    setPending(null);
    if (!result.success) {
      setError(result.error);
      return;
    }
    onApplied(result.data);
    setOpen(false);
    setPreview(null);
  };

  if (readOnly) return null;

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button variant={schedule ? 'outline' : 'default'}>
          <CalendarRange className="size-4" />
          {schedule ? 'Edit schedule' : 'Build schedule'}
        </Button>
      </SheetTrigger>
      <SheetContent className="w-full overflow-y-auto sm:max-w-2xl">
        <SheetHeader className="border-b">
          <div className="flex items-center gap-2 text-xs font-medium text-muted-foreground">
            <span className={preview ? '' : 'text-foreground'}>1. Rules</span>
            <span aria-hidden="true">/</span>
            <span className={preview ? 'text-foreground' : ''}>2. Preview</span>
          </div>
          <SheetTitle>{schedule ? 'Edit class schedule' : 'Build class schedule'}</SheetTitle>
          <SheetDescription>
            Configure the delivery cadence for {cohort.name}. Previewing never changes the live schedule.
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 px-4 pb-4">
          {error ? (
            <Alert variant="destructive" className="mb-4">
              <AlertTriangle />
              <AlertTitle>Schedule could not be processed</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : null}

          {!preview ? (
            <div className="space-y-6">
              <section aria-labelledby="calendar-rules-heading">
                <h3 id="calendar-rules-heading" className="text-sm font-semibold">Calendar</h3>
                <div className="mt-3 grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="first-instructional-date">First instructional date</Label>
                    <Input id="first-instructional-date" type="date" value={rules.firstInstructionalDate} onChange={(event) => updateRules('firstInstructionalDate', event.target.value)} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="cohort-end-date">Class end date</Label>
                    <Input id="cohort-end-date" type="date" value={rules.cohortEndDate} onChange={(event) => updateRules('cohortEndDate', event.target.value)} />
                  </div>
                  <div className="space-y-2 sm:col-span-2">
                    <Label htmlFor="schedule-timezone">Timezone</Label>
                    <Input id="schedule-timezone" value={rules.timezoneId} onChange={(event) => updateRules('timezoneId', event.target.value)} placeholder="America/Sao_Paulo" />
                  </div>
                </div>
              </section>

              <section aria-labelledby="meeting-rules-heading">
                <h3 id="meeting-rules-heading" className="text-sm font-semibold">Meeting pattern</h3>
                <div className="mt-3 flex flex-wrap gap-2">
                  {meetingDayOptions.map((day) => (
                    <label key={day.value} className="flex cursor-pointer items-center gap-2 rounded-md border px-3 py-2 text-sm has-[[data-state=checked]]:border-primary has-[[data-state=checked]]:bg-primary/5">
                      <Checkbox checked={rules.meetingDays.includes(day.value)} onCheckedChange={(checked) => toggleMeetingDay(day.value, checked === true)} />
                      {day.label}
                    </label>
                  ))}
                </div>
                <div className="mt-4 grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="meeting-start-time">Meeting start time</Label>
                    <Input id="meeting-start-time" type="time" value={rules.meetingStartTime} onChange={(event) => updateRules('meetingStartTime', event.target.value)} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="meeting-duration">Meeting duration (minutes)</Label>
                    <Input id="meeting-duration" type="number" min={15} step={15} value={rules.meetingDurationMinutes} onChange={(event) => updateRules('meetingDurationMinutes', Number(event.target.value))} />
                  </div>
                </div>
              </section>

              <section aria-labelledby="pacing-rules-heading">
                <h3 id="pacing-rules-heading" className="text-sm font-semibold">Pacing and release</h3>
                <div className="mt-3 grid gap-4 sm:grid-cols-2">
                  <div className="space-y-2">
                    <Label htmlFor="pacing-mode">Pacing mode</Label>
                    <Select value={rules.pacingMode} onValueChange={(value) => updateRules('pacingMode', value as LearningCohortsCohortPacingMode)}>
                      <SelectTrigger id="pacing-mode" className="w-full"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="OneModulePerWeek">One module per week</SelectItem>
                        <SelectItem value="OneLessonPerMeeting">One lesson per meeting</SelectItem>
                        <SelectItem value="FixedLessonsPerWeek">Fixed lessons per week</SelectItem>
                        <SelectItem value="Manual">Manual</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="units-per-period">Units per period</Label>
                    <Input id="units-per-period" type="number" min={1} value={rules.unitsPerPeriod} onChange={(event) => updateRules('unitsPerPeriod', Number(event.target.value))} />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="release-policy">Release policy</Label>
                    <Select value={rules.releasePolicy} onValueChange={(value) => updateRules('releasePolicy', value as LearningCohortsCohortReleasePolicy)}>
                      <SelectTrigger id="release-policy" className="w-full"><SelectValue /></SelectTrigger>
                      <SelectContent>
                        <SelectItem value="Weekly">At the start of the instructional week</SelectItem>
                        <SelectItem value="BeforeMeeting">Before each meeting</SelectItem>
                        <SelectItem value="Manual">Manual release</SelectItem>
                        <SelectItem value="Immediately">Immediately</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="assessment-offset">Assessment due offset (days)</Label>
                    <Input id="assessment-offset" type="number" value={rules.assessmentDueOffsetDays} onChange={(event) => updateRules('assessmentDueOffsetDays', Number(event.target.value))} />
                  </div>
                  <div className="space-y-2 sm:col-span-2">
                    <Label htmlFor="skipped-dates">Skipped dates</Label>
                    <Input id="skipped-dates" value={rules.skippedDates} onChange={(event) => updateRules('skippedDates', event.target.value)} placeholder="2026-09-07, 2026-10-12" />
                    <p className="text-xs text-muted-foreground">Separate holidays and breaks with commas.</p>
                  </div>
                </div>
              </section>
            </div>
          ) : (
            <div className="space-y-5">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <Button type="button" variant="ghost" size="sm" onClick={() => setPreview(null)}>
                  <ArrowLeft className="size-4" /> Back to rules
                </Button>
                <Badge variant="outline">Ends {preview.calculatedEndDate || rules.cohortEndDate}</Badge>
              </div>

              <section aria-labelledby="preview-summary-heading">
                <h3 id="preview-summary-heading" className="text-sm font-semibold">Generated schedule</h3>
                <div className="mt-3 divide-y rounded-md border">
                  {(preview.items ?? []).slice(0, 8).map((item, index) => (
                    <div key={`${item.programContentId ?? item.assessmentId ?? 'item'}-${index}`} className="flex items-center justify-between gap-3 px-3 py-2.5 text-sm">
                      <div className="min-w-0">
                        <p className="truncate font-medium">{item.title?.trim() || 'Untitled schedule item'}</p>
                        <p className="text-xs text-muted-foreground">Week {Math.max(1, item.instructionalWeek ?? 1)}</p>
                      </div>
                      <span className="shrink-0 text-xs text-muted-foreground">
                        {formatScheduleDate(item.availableFrom ?? item.startsAt ?? item.dueAt, rules.timezoneId) || 'Date not set'}
                      </span>
                    </div>
                  ))}
                </div>
                {(preview.items?.length ?? 0) > 8 ? <p className="mt-2 text-xs text-muted-foreground">And {(preview.items?.length ?? 0) - 8} more items.</p> : null}
              </section>

              {conflicts.length > 0 ? (
                <section aria-labelledby="conflicts-heading">
                  <h3 id="conflicts-heading" className="text-sm font-semibold">Conflicts to review</h3>
                  <div className="mt-3 space-y-2">
                    {conflicts.map((conflict, index) => (
                      <Alert key={`${conflict.code ?? 'conflict'}-${index}`} variant={conflict.severity === 'Blocking' ? 'destructive' : 'default'}>
                        <AlertTriangle />
                        <AlertTitle>{conflict.severity === 'Blocking' ? 'Blocking conflict' : 'Advisory'}</AlertTitle>
                        <AlertDescription>{conflict.message || 'Review this schedule conflict.'}</AlertDescription>
                      </Alert>
                    ))}
                  </div>
                  {hasAdvisoryConflicts && !hasBlockingConflicts ? (
                    <div className="mt-3 flex items-start gap-2 rounded-md border p-3">
                      <Checkbox id="confirm-advisories" checked={advisoriesConfirmed} onCheckedChange={(checked) => setAdvisoriesConfirmed(checked === true)} />
                      <Label htmlFor="confirm-advisories" className="cursor-pointer font-normal leading-4">I reviewed the advisory conflicts</Label>
                    </div>
                  ) : null}
                </section>
              ) : (
                <Alert>
                  <CheckCircle2 />
                  <AlertTitle>Ready to apply</AlertTitle>
                  <AlertDescription>No schedule conflicts were found.</AlertDescription>
                </Alert>
              )}
            </div>
          )}
        </div>

        <SheetFooter className="border-t bg-background">
          {!preview ? (
            <Button type="button" onClick={generatePreview} disabled={pending !== null}>
              {pending === 'preview' ? <Loader2 className="size-4 animate-spin" /> : <WandSparkles className="size-4" />}
              Generate preview
            </Button>
          ) : (
            <Button type="button" onClick={applyPreview} disabled={!canApply}>
              {pending === 'apply' ? <Loader2 className="size-4 animate-spin" /> : <CheckCircle2 className="size-4" />}
              Apply schedule
            </Button>
          )}
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}
