"use client";

import {
  archiveTestingEvent,
  addTestingEventCommitteeMember,
  assignTestedProjectToRegistration,
  approveTestingEventApplication,
  beginTestingEventApplicationReview,
  configureTestingEventLearning,
  createTestingEvent,
  createTestingEventSlots,
  deleteTestingEvent,
  deleteTestingEventSlot,
  rejectTestingEventApplication,
  removeTestingEventCommitteeMember,
  restoreTestingEvent,
  transitionTestingEvent,
  updateTestingEventAttendance,
  updateTestingEvent,
  updateTestingEventSlot,
  voteOnTestingEventApplication,
  waitlistTestingEventApplication,
  type TestingEventActionResult,
} from "@/lib/testing-lab/events-actions";
import {
  browserTimeZone,
  formatWallClockInTimeZone,
  wallClockToUtcIso,
} from "@/lib/date-time-zone";
import { formatEventDateTime, formatEventDateRange } from "@/lib/testing-lab/event-workspace";
import { formatTestingEventStatus } from "@/lib/testing-lab/format";
import type {
  TestingLabTestingEventCommitteeMemberProjection,
  TestingLabTestingEventProjection,
  TestingLabTestingEventSlotProjection,
  TestingLabTestingEventStatus,
  TestingLabTestingProjectApplicationProjection,
  TestingLabTestingSlotRegistrationProjection,
  TestingLabTestingEventTemplateProjection,
} from "@game-guild/client";
import { Alert, AlertDescription } from "@game-guild/ui/components/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@game-guild/ui/components/alert-dialog";
import { Badge } from "@game-guild/ui/components/badge";
import { Button } from "@game-guild/ui/components/button";
import { buttonVariants } from "@game-guild/ui/components/button-variants";
import { DateTimePicker } from "@/components/ui/date-time-picker";
import { DateTimeRangePicker } from "@/components/ui/date-time-range-picker";
import { TimeZoneCombobox } from "@/components/ui/time-zone-combobox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@game-guild/ui/components/dialog";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import { Textarea } from "@game-guild/ui/components/textarea";
import {
  AlertCircle,
  Archive,
  CheckCircle2,
  CircleStop,
  ClipboardCheck,
  Clock3,
  Pencil,
  Play,
  Plus,
  RotateCcw,
  Send,
  ShieldCheck,
  Trash2,
  UserRoundCheck,
} from "lucide-react";
import { useRouter } from "next/navigation";
import {
  useCallback,
  useRef,
  useMemo,
  useState,
  useSyncExternalStore,
  useTransition,
  type FormEvent,
  type ReactElement,
  type ReactNode,
} from "react";

type Action = (
  formData: FormData,
) => Promise<TestingEventActionResult<unknown>>;

const subscribeToTimeZonePreference = () => () => undefined;

export interface TestingLabMemberOption {
  id: string;
  label: string;
}

export interface TestingLabApprovedApplicationOption {
  id: string;
  label: string;
  slotId?: string | null;
  eligibleTesterUserIds: string[];
}

export interface TestingLabLearningActivityOption {
  id: string;
  courseId: string;
  label: string;
}

export function apiDatetimeLocal(value?: string | null, timeZoneId = "UTC") {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return "";
  return formatWallClockInTimeZone(date, timeZoneId);
}

type TestingEventSchedule = {
  applicationsOpenAt: string;
  applicationsCloseAt: string;
  startsAt: string;
  endsAt: string;
};

const Hour = 60 * 60 * 1000;

function wallClockDate(value: string) {
  if (!/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}$/.test(value)) return null;
  const date = new Date(`${value}:00.000Z`);
  return Number.isNaN(date.valueOf()) || date.toISOString().slice(0, 16) !== value
    ? null
    : date;
}

function addWallClockMinutes(value: string, minutes: number) {
  const date = wallClockDate(value);
  if (!date) return "";
  date.setUTCMinutes(date.getUTCMinutes() + minutes);
  return date.toISOString().slice(0, 16);
}

function nextValidWallClock(value: string, timeZoneId: string, direction = 1) {
  for (let offset = 0; offset <= 180; offset += 1) {
    const candidate = addWallClockMinutes(value, offset * direction);
    if (candidate && wallClockToUtcIso(candidate, timeZoneId)) return candidate;
  }
  return "";
}

function localCalendarDatePart(date: Date) {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function createTestingEventSchedule(
  now = new Date(),
  eventDate?: Date,
  timeZoneId = browserTimeZone(),
): TestingEventSchedule {
  const currentWallClock = formatWallClockInTimeZone(now, timeZoneId);
  const currentWallClockDate = wallClockDate(currentWallClock);
  if (!currentWallClockDate) {
    return createTestingEventSchedule(now, eventDate, "UTC");
  }

  currentWallClockDate.setUTCMinutes(0, 0, 0);
  currentWallClockDate.setUTCHours(currentWallClockDate.getUTCHours() + 1);
  const applicationsOpenAt = nextValidWallClock(
    currentWallClockDate.toISOString().slice(0, 16),
    timeZoneId,
  );
  const openInstant = wallClockToUtcIso(applicationsOpenAt, timeZoneId);
  if (!applicationsOpenAt || !openInstant) {
    return createTestingEventSchedule(now, eventDate, "UTC");
  }

  const selectedDate = eventDate ? localCalendarDatePart(eventDate) : null;
  const requestedStart = selectedDate
    ? `${selectedDate}T10:00`
    : `${addWallClockMinutes(applicationsOpenAt, 48 * 60).slice(0, 10)}T10:00`;
  const earliestStart = Date.parse(openInstant) + 2 * Hour;
  let startsAt = nextValidWallClock(requestedStart, timeZoneId);
  let startInstant = startsAt ? wallClockToUtcIso(startsAt, timeZoneId) : null;

  if (!startInstant || Date.parse(startInstant) < earliestStart) {
    const fallbackDate = addWallClockMinutes(applicationsOpenAt, 48 * 60);
    startsAt = nextValidWallClock(`${fallbackDate.slice(0, 10)}T10:00`, timeZoneId);
    startInstant = startsAt ? wallClockToUtcIso(startsAt, timeZoneId) : null;
  }

  if (!startsAt || !startInstant) {
    return createTestingEventSchedule(now, eventDate, "UTC");
  }

  let applicationsCloseAt = "";
  for (let offset = 60; offset <= 180; offset += 1) {
    const candidate = addWallClockMinutes(startsAt, -offset);
    const candidateInstant = candidate
      ? wallClockToUtcIso(candidate, timeZoneId)
      : null;
    if (candidateInstant && Date.parse(candidateInstant) > Date.parse(openInstant)) {
      applicationsCloseAt = candidate;
      break;
    }
  }
  if (!applicationsCloseAt) {
    const candidate = addWallClockMinutes(applicationsOpenAt, 60);
    applicationsCloseAt = nextValidWallClock(candidate, timeZoneId);
  }

  const endsAt = formatWallClockInTimeZone(
    new Date(Date.parse(startInstant) + 2 * Hour),
    timeZoneId,
  );

  return {
    applicationsOpenAt,
    applicationsCloseAt,
    startsAt,
    endsAt,
  };
}

export function scheduleDate(value: string) {
  if (!value) return null;
  return wallClockDate(value);
}

export function updateTestingEventSchedule(
  current: TestingEventSchedule,
  field: keyof TestingEventSchedule,
  value: string,
): TestingEventSchedule {
  return { ...current, [field]: value };
}

export function validateTestingEventSchedule(
  schedule: TestingEventSchedule,
  timeZoneId: string,
) {
  const values = [
    schedule.applicationsOpenAt,
    schedule.applicationsCloseAt,
    schedule.startsAt,
    schedule.endsAt,
  ];

  if (
    values.some(
      (value) => !/^\d{4}-\d{2}-\d{2}T(?:[01]\d|2[0-3]):[0-5]\d$/.test(value),
    )
  ) {
    return "Enter a date and a 24-hour time for every schedule field.";
  }

  const instants = values.map((value) => wallClockToUtcIso(value, timeZoneId));
  if (instants.some((value) => value === null)) {
    return `One of these times does not exist in ${timeZoneId}. Choose another time, especially around daylight-saving changes.`;
  }

  const [openAt, closeAt, startsAt, endsAt] = instants.map((value) =>
    Date.parse(value!),
  );
  if (openAt! >= closeAt!) {
    return "Applications must close after they open.";
  }
  if (closeAt! > startsAt!) {
    return "The playtest must start after applications close.";
  }
  if (startsAt! >= endsAt!) {
    return "The playtest end must be later than its start.";
  }

  return null;
}

function ActionMessage({
  result,
}: {
  result: TestingEventActionResult<unknown> | null;
}) {
  if (!result) return null;
  return (
    <Alert
      variant={result.success ? "default" : "destructive"}
      aria-live="polite"
    >
      {result.success ? (
        <CheckCircle2 className="size-4" />
      ) : (
        <AlertCircle className="size-4" />
      )}
      <AlertDescription>
        {result.success ? result.message : result.error}
      </AlertDescription>
    </Alert>
  );
}

function actionFailure(error: unknown): TestingEventActionResult<unknown> {
  return {
    success: false,
    error:
      error instanceof Error
        ? error.message
        : "The Testing Lab operation failed.",
  };
}

function EventActionDialog({
  trigger,
  title,
  description,
  submitLabel,
  action,
  children,
  destructive = false,
  successHref,
  submitDisabled = false,
}: {
  trigger: ReactElement;
  title: string;
  description: string;
  submitLabel: string;
  action: Action;
  children: ReactNode;
  destructive?: boolean;
  successHref?: string;
  submitDisabled?: boolean;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [result, setResult] =
    useState<TestingEventActionResult<unknown> | null>(null);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    startTransition(async () => {
      try {
        const next = await action(data);
        setResult(next);
        if (next.success) {
          setOpen(false);
          if (successHref) router.push(successHref);
          else router.refresh();
        }
      } catch (error) {
        setResult(actionFailure(error));
      }
    });
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (pending) return;
        setOpen(next);
        setResult(null);
      }}
    >
      <DialogTrigger render={trigger} />
      <DialogContent className="max-h-[calc(100dvh-2rem)] overflow-y-auto sm:max-w-3xl">
        <form onSubmit={submit} className="space-y-5">
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>
          {children}
          <ActionMessage result={result} />
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              disabled={pending}
              onClick={() => setOpen(false)}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              variant={destructive ? "destructive" : "default"}
              disabled={pending || submitDisabled}
            >
              {pending ? "Working..." : submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

type EventFieldsProps = {
  event?: TestingLabTestingEventProjection;
  schedule?: TestingEventSchedule;
  onScheduleChange?: (field: keyof TestingEventSchedule, value: string) => void;
  timeZoneId?: string;
};

type EventTimelineFieldsProps = Omit<EventFieldsProps, "timeZoneId"> & {
  timeZoneId: string;
  compact?: boolean;
};

function EventIdentityFields({
  event,
  includeBrief = true,
  compact = false,
  templates = [],
}: {
  event?: TestingLabTestingEventProjection;
  includeBrief?: boolean;
  compact?: boolean;
  templates?: TestingLabTestingEventTemplateProjection[];
}) {
  const fieldSuffix = event?.id ?? "new";
  const compactRow =
    "grid gap-1.5 sm:grid-cols-[7rem_minmax(0,1fr)] sm:items-center sm:gap-3";
  const availableTemplates = templates.filter(
    (template) => template.currentRevision?.id,
  );

  return (
    <div className={compact ? "grid gap-2.5" : "grid gap-4 sm:grid-cols-2"}>
      <div className={compact ? "mb-1" : "space-y-2 sm:col-span-2"}>
        <Label
          className={compact ? "sr-only" : undefined}
          htmlFor={`event-name-${fieldSuffix}`}
        >
          Event name
        </Label>
        <Input
          id={`event-name-${fieldSuffix}`}
          name="name"
          required
          placeholder={compact ? "Event name" : undefined}
          defaultValue={event?.name ?? ""}
          className={
            compact
              ? "h-10 rounded-none border-x-0 border-t-0 bg-transparent px-0 text-lg font-semibold shadow-none focus-visible:ring-0"
              : undefined
          }
        />
      </div>
      {availableTemplates.length > 0 ? (
        <div className={compactRow}>
          <Label
            className="text-xs text-muted-foreground"
            htmlFor={`event-calendar-${fieldSuffix}`}
          >
            Calendar
          </Label>
          <Select
            name="templateRevisionId"
            defaultValue={availableTemplates[0]!.currentRevision!.id}
          >
            <SelectTrigger
              id={`event-calendar-${fieldSuffix}`}
              aria-label="Event calendar"
              className="h-10 w-full"
            >
              <SelectValue>
                {(value: string | null) => {
                  const selected = availableTemplates.find(
                    (template) => template.currentRevision?.id === value,
                  );
                  return selected
                    ? selected.name?.trim() || "Untitled calendar"
                    : "Choose a calendar";
                }}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              {availableTemplates.map((template) => (
                <SelectItem
                  key={template.id}
                  value={template.currentRevision!.id!}
                >
                  {template.name?.trim() || "Untitled calendar"}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      ) : null}
      <div className={compact ? compactRow : "space-y-2"}>
        <Label
          className={compact ? "text-xs text-muted-foreground" : undefined}
          htmlFor={`event-format-${fieldSuffix}`}
        >
          Event format
        </Label>
        <Select name="mode" defaultValue={event?.mode ?? "Online"}>
          <SelectTrigger
            id={`event-format-${fieldSuffix}`}
            className="h-10 w-full"
          >
            <SelectValue>
              {(value: string | null) =>
                value === "InPerson"
                  ? "In person"
                  : value === "Hybrid"
                    ? "Hybrid"
                    : "Online"
              }
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Online">Online</SelectItem>
            <SelectItem value="InPerson">In person</SelectItem>
            <SelectItem value="Hybrid">Hybrid</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className={compact ? compactRow : "space-y-2"}>
        <Label
          className={compact ? "text-xs text-muted-foreground" : undefined}
          htmlFor={`project-review-${fieldSuffix}`}
        >
          Project review
        </Label>
        <Select
          name="approvalMode"
          defaultValue={event?.approvalMode ?? "ManagerOnly"}
        >
          <SelectTrigger
            id={`project-review-${fieldSuffix}`}
            className="h-10 w-full"
          >
            <SelectValue>
              {(value: string | null) =>
                value === "Committee"
                  ? "Review committee votes"
                  : "Event managers decide"
              }
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ManagerOnly">Event managers decide</SelectItem>
            <SelectItem value="Committee">Review committee votes</SelectItem>
          </SelectContent>
        </Select>
      </div>
      {includeBrief ? (
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`event-description-${fieldSuffix}`}>
            Purpose and tester brief
          </Label>
          <Textarea
            id={`event-description-${fieldSuffix}`}
            name="description"
            rows={3}
            defaultValue={event?.description ?? ""}
          />
        </div>
      ) : null}
    </div>
  );
}

function EventTimelineFields({
  event,
  schedule,
  onScheduleChange,
  timeZoneId,
  compact = false,
}: EventTimelineFieldsProps) {
  const eventTimeZone = timeZoneId;
  const applicationsOpenAt =
    schedule?.applicationsOpenAt ??
    apiDatetimeLocal(event?.applicationsOpenAt, eventTimeZone);
  const applicationsCloseAt =
    schedule?.applicationsCloseAt ??
    apiDatetimeLocal(event?.applicationsCloseAt, eventTimeZone);
  const startsAt =
    schedule?.startsAt ?? apiDatetimeLocal(event?.startsAt, eventTimeZone);
  const endsAt =
    schedule?.endsAt ?? apiDatetimeLocal(event?.endsAt, eventTimeZone);

  const fieldSuffix = event?.id ?? "new";

  function changeRange(
    startField: "applicationsOpenAt" | "startsAt",
    endField: "applicationsCloseAt" | "endsAt",
    next: { start: string; end: string },
    currentStart: string,
    currentEnd: string,
  ) {
    if (next.start !== currentStart) {
      onScheduleChange?.(startField, next.start);
    }
    if (next.end !== currentEnd) {
      onScheduleChange?.(endField, next.end);
    }
  }

  return (
    <div className={compact ? "grid min-w-0 gap-2.5" : "grid min-w-0 gap-5"}>
      <p className="text-xs text-muted-foreground">
        All times use {eventTimeZone} and a 24-hour clock.
      </p>
      <section aria-label="Application window" className={compact ? "grid min-w-0 gap-1.5 sm:grid-cols-[7rem_minmax(0,1fr)] sm:items-center sm:gap-3" : "min-w-0 space-y-3 border-t pt-4"}>
        {compact ? <Label htmlFor={`applications-window-${fieldSuffix}`} className="text-xs text-muted-foreground">Applications</Label> : <h3 className="text-sm font-semibold">Sign-up window</h3>}
        <DateTimeRangePicker
          compact={compact}
          id={`applications-window-${fieldSuffix}`}
          label="Application window"
          startLabel="Applications open"
          endLabel="Applications close"
          startName="applicationsOpenAt"
          endName="applicationsCloseAt"
          timeZoneId={eventTimeZone}
          required
          value={
            schedule
              ? { start: applicationsOpenAt, end: applicationsCloseAt }
              : undefined
          }
          defaultValue={{
            start: applicationsOpenAt,
            end: applicationsCloseAt,
          }}
          onValueChange={(next) =>
            changeRange(
              "applicationsOpenAt",
              "applicationsCloseAt",
              next,
              applicationsOpenAt,
              applicationsCloseAt,
            )
          }
        />
      </section>
      <section aria-label="Testing session" className={compact ? "grid min-w-0 gap-1.5 sm:grid-cols-[7rem_minmax(0,1fr)] sm:items-center sm:gap-3" : "min-w-0 space-y-3 border-t pt-4"}>
        {compact ? <Label htmlFor={`event-schedule-${fieldSuffix}`} className="text-xs text-muted-foreground">Session</Label> : <h3 className="text-sm font-semibold">Testing session</h3>}
        <DateTimeRangePicker
          compact={compact}
          id={`event-schedule-${fieldSuffix}`}
          label="Event schedule"
          startLabel="Session starts"
          endLabel="Session ends"
          startName="startsAt"
          endName="endsAt"
          timeZoneId={eventTimeZone}
          required
          value={schedule ? { start: startsAt, end: endsAt } : undefined}
          defaultValue={{ start: startsAt, end: endsAt }}
          onValueChange={(next) =>
            changeRange("startsAt", "endsAt", next, startsAt, endsAt)
          }
        />
      </section>
    </div>
  );
}

function EventFeedbackField({
  event,
}: {
  event: TestingLabTestingEventProjection;
}) {
  return (
    <label className="flex items-start gap-3 rounded-md bg-muted/30 p-3 text-sm">
      <input
        name="requiresFeedback"
        type="checkbox"
        defaultChecked={event?.requiresFeedback ?? true}
        className="mt-1"
      />
      <span>
        <strong className="block font-medium">Require tester feedback</strong>
        <span className="text-muted-foreground">
          Testers must submit project feedback before attendance is complete.
        </span>
      </span>
    </label>
  );
}

function EventFields({
  event,
  timeZoneId,
}: {
  event: TestingLabTestingEventProjection;
  timeZoneId: string;
}) {
  return (
    <div className="space-y-6">
      <input type="hidden" name="eventId" value={event.id} />
      <input type="hidden" name="timeZoneId" value={timeZoneId} />
      <EventIdentityFields event={event} />
      <EventTimelineFields event={event} timeZoneId={timeZoneId} />
      <EventFeedbackField event={event} />
    </div>
  );
}

export function testingEventRecurrenceStart(startDate: string) {
  const allDays = [
    "Sunday",
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
  ];
  const datePart = startDate.match(/^(\d{4}-\d{2}-\d{2})/)?.[1];
  const parsedStart = datePart ? new Date(`${datePart}T00:00:00.000Z`) : null;
  return !parsedStart || Number.isNaN(parsedStart.valueOf())
    ? { day: "Monday", dayOfMonth: 1 }
    : {
        day: allDays[parsedStart.getUTCDay()]!,
        dayOfMonth: parsedStart.getUTCDate(),
      };
}

function EventRecurrenceFields({
  onDirty,
  startDate,
  timeZoneId,
}: {
  onDirty: () => void;
  startDate: string;
  timeZoneId: string;
}) {
  const displayDays = [
    "Monday",
    "Tuesday",
    "Wednesday",
    "Thursday",
    "Friday",
    "Saturday",
    "Sunday",
  ];
  const { day: startDay, dayOfMonth: startDayOfMonth } =
    testingEventRecurrenceStart(startDate);
  const [repeatOption, setRepeatOption] = useState("none");
  const [customFrequency, setCustomFrequency] = useState("Weekly");
  const [customDays, setCustomDays] = useState<string[]>([startDay]);
  const [endMode, setEndMode] = useState("count");
  const frequency =
    repeatOption === "custom"
      ? customFrequency
      : repeatOption === "none"
        ? ""
        : repeatOption;

  function toggleCustomDay(day: string) {
    setCustomDays((current) =>
      current.includes(day)
        ? current.filter((value) => value !== day)
        : [...current, day],
    );
    onDirty();
  }

  return (
    <section aria-labelledby="event-recurrence-heading" className="space-y-2.5">
      <div className="grid gap-1.5 sm:grid-cols-[7rem_minmax(0,1fr)] sm:items-center sm:gap-3">
        <Label
          id="event-recurrence-heading"
          className="text-xs text-muted-foreground"
          htmlFor="event-recurrence"
        >
          Repeats
        </Label>
        <Select
          value={repeatOption}
          onValueChange={(value) => {
            setRepeatOption(value ?? "none");
            onDirty();
          }}
        >
          <SelectTrigger id="event-recurrence" className="h-10 w-full">
            <SelectValue>
              {(value: string | null) =>
                value === "Daily"
                  ? "Daily"
                  : value === "Weekly"
                    ? `Weekly on ${startDay}`
                    : value === "Monthly"
                      ? `Monthly on day ${startDayOfMonth}`
                      : value === "custom"
                        ? "Custom…"
                        : "Does not repeat"
              }
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="none">Does not repeat</SelectItem>
            <SelectItem value="Daily">Daily</SelectItem>
            <SelectItem value="Weekly">Weekly on {startDay}</SelectItem>
            <SelectItem value="Monthly">
              Monthly on day {startDayOfMonth}
            </SelectItem>
            <SelectItem value="custom">Custom…</SelectItem>
          </SelectContent>
        </Select>
        <input type="hidden" name="recurrenceFrequency" value={frequency} />
      </div>

      {frequency ? (
        <div className="space-y-3 rounded-md bg-muted/30 p-3 sm:ml-[7.75rem]">
          {repeatOption === "custom" ? (
            <div className="space-y-2">
              <Label htmlFor="recurrence-interval">Repeat every</Label>
              <div className="grid grid-cols-[5rem_minmax(0,1fr)] gap-2">
                <Input
                  id="recurrence-interval"
                  name="recurrenceInterval"
                  type="number"
                  min="1"
                  max="52"
                  defaultValue="1"
                  required
                />
                <Select
                  value={customFrequency}
                  onValueChange={(value) => {
                    setCustomFrequency(value ?? "Weekly");
                    onDirty();
                  }}
                >
                  <SelectTrigger aria-label="Repeat unit" className="w-full">
                    <SelectValue>
                      {(value: string | null) =>
                        value === "Daily"
                          ? "Day(s)"
                          : value === "Monthly"
                            ? "Month(s)"
                            : "Week(s)"
                      }
                    </SelectValue>
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Daily">Day(s)</SelectItem>
                    <SelectItem value="Weekly">Week(s)</SelectItem>
                    <SelectItem value="Monthly">Month(s)</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          ) : (
            <input type="hidden" name="recurrenceInterval" value="1" />
          )}

          {frequency === "Weekly" ? (
            repeatOption === "custom" ? (
              <fieldset className="space-y-2">
                <legend className="text-sm font-medium">Repeat on</legend>
                <div className="flex flex-wrap gap-1.5">
                  {displayDays.map((day) => {
                    const selected = customDays.includes(day);
                    return (
                      <label
                        key={day}
                        className={`flex size-9 cursor-pointer items-center justify-center rounded-full text-xs font-medium transition-colors ${selected ? "bg-primary text-primary-foreground" : "bg-background text-muted-foreground hover:text-foreground"}`}
                      >
                        <input
                          className="sr-only"
                          name="recurrenceDaysOfWeek"
                          type="checkbox"
                          value={day}
                          checked={selected}
                          onChange={() => toggleCustomDay(day)}
                        />
                        {day.slice(0, 1)}
                      </label>
                    );
                  })}
                </div>
              </fieldset>
            ) : (
              <input
                type="hidden"
                name="recurrenceDaysOfWeek"
                value={startDay}
              />
            )
          ) : null}

          <div className="grid gap-3 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="recurrence-end-mode">Ends</Label>
              <Select
                value={endMode}
                onValueChange={(value) => {
                  setEndMode(value ?? "count");
                  onDirty();
                }}
              >
                <SelectTrigger id="recurrence-end-mode" className="w-full">
                  <SelectValue>
                    {(value: string | null) =>
                      value === "date" ? "On a date" : "After"
                    }
                  </SelectValue>
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="count">After</SelectItem>
                  <SelectItem value="date">On a date</SelectItem>
                </SelectContent>
              </Select>
              <input type="hidden" name="recurrenceEndMode" value={endMode} />
            </div>
            {endMode === "date" ? (
              <div className="space-y-2">
                <Label htmlFor="recurrence-ends-at">End date</Label>
                <DateTimePicker
                  id="recurrence-ends-at"
                  name="recurrenceEndsAt"
                  required
                  timeZoneId={timeZoneId}
                  timezoneLabel={timeZoneId}
                  onValueChange={onDirty}
                />
              </div>
            ) : (
              <div className="space-y-2">
                <Label htmlFor="recurrence-count">Number of events</Label>
                <Input
                  id="recurrence-count"
                  name="recurrenceOccurrenceCount"
                  type="number"
                  min="1"
                  max="104"
                  defaultValue="4"
                  required
                />
              </div>
            )}
          </div>
        </div>
      ) : null}
    </section>
  );
}

export interface CreateTestingEventDialogProps {
  initialDate?: Date;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  showTrigger?: boolean;
  templates?: TestingLabTestingEventTemplateProjection[];
  defaultTimeZone?: string;
}

export function preferredNewEventTimeZone(defaultTimeZone: string) {
  return defaultTimeZone === "UTC"
    ? browserTimeZone(defaultTimeZone)
    : defaultTimeZone;
}

export function CreateTestingEventDialog({
  initialDate,
  open: controlledOpen,
  onOpenChange,
  showTrigger = true,
  templates = [],
  defaultTimeZone = "UTC",
}: CreateTestingEventDialogProps = {}) {
  const router = useRouter();
  const formRef = useRef<HTMLFormElement>(null);
  const [internalOpen, setInternalOpen] = useState(false);
  const open = controlledOpen ?? internalOpen;
  const [dirty, setDirty] = useState(false);
  const [discardOpen, setDiscardOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [result, setResult] =
    useState<TestingEventActionResult<unknown> | null>(null);
  const browserPreferredTimeZone = useSyncExternalStore(
    subscribeToTimeZonePreference,
    useCallback(
      () => preferredNewEventTimeZone(defaultTimeZone),
      [defaultTimeZone],
    ),
    useCallback(() => defaultTimeZone, [defaultTimeZone]),
  );
  const [timeZoneOverride, setTimeZoneOverride] = useState<string | null>(null);
  const timeZoneId = timeZoneOverride ?? browserPreferredTimeZone;
  const [scheduleError, setScheduleError] = useState<string | null>(null);
  const defaultSchedule = useMemo(
    () =>
      createTestingEventSchedule(
        new Date(),
        initialDate,
        timeZoneId,
      ),
    [initialDate, timeZoneId],
  );
  const [scheduleOverride, setScheduleOverride] =
    useState<TestingEventSchedule | null>(null);
  const schedule = scheduleOverride ?? defaultSchedule;

  function setOpen(next: boolean) {
    if (controlledOpen === undefined) setInternalOpen(next);
    onOpenChange?.(next);
  }

  function resetDraft() {
    formRef.current?.reset();
    const preferredTimeZone = preferredNewEventTimeZone(defaultTimeZone);
    setScheduleOverride(
      createTestingEventSchedule(new Date(), initialDate, preferredTimeZone),
    );
    setTimeZoneOverride(null);
    setScheduleError(null);
    setDirty(false);
    setResult(null);
  }

  function closeDrawer() {
    resetDraft();
    setDiscardOpen(false);
    setOpen(false);
  }

  function requestClose() {
    if (pending) return;
    if (dirty) setDiscardOpen(true);
    else closeDrawer();
  }

  function trackChanges() {
    setDirty(true);
  }

  function changeSchedule(field: keyof TestingEventSchedule, value: string) {
    setDirty(true);
    setScheduleError(null);
    setScheduleOverride((current) =>
      updateTestingEventSchedule(current ?? schedule, field, value),
    );
  }

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const invalidSchedule = validateTestingEventSchedule(schedule, timeZoneId);
    if (invalidSchedule) {
      setScheduleError(invalidSchedule);
      return;
    }

    setScheduleError(null);
    const form = event.currentTarget;
    const data = new FormData(form);
    data.set("timeZoneId", timeZoneId);
    startTransition(async () => {
      try {
        const next = await createTestingEvent(data);
        if (next.success) {
          closeDrawer();
          if (next.data?.id) router.push(`/workspace/testing-lab/events/${next.data.id}/overview`);
          router.refresh();
          return;
        }
        setResult(next);
      } catch (error) {
        setResult(actionFailure(error));
      }
    });
  }

  return (
    <>
      {showTrigger ? (
        <Button
          onClick={() => {
            resetDraft();
            setOpen(true);
          }}
        >
          <Plus className="mr-2 size-4" />
          New event
        </Button>
      ) : null}
      <Dialog open={open} onOpenChange={requestClose}>
        <DialogContent className="flex max-h-[calc(100dvh-2rem)] flex-col gap-0 overflow-hidden p-0 sm:max-w-lg">
          <form
            ref={formRef}
            onSubmit={submit}
            onChange={trackChanges}
            className="flex min-h-0 flex-1 flex-col"
          >
            <DialogHeader className="px-5 pb-3 pt-4 text-left">
              <DialogTitle>New testing event</DialogTitle>
              <DialogDescription className="sr-only">
                Set the schedule, then finish the rules and publish from the event workspace.
              </DialogDescription>
            </DialogHeader>
            <div className="min-h-0 flex-1 overflow-y-auto px-5 pb-5">
              {result ? <ActionMessage result={result} /> : null}
              <div className="space-y-3">
                <EventIdentityFields
                  includeBrief={false}
                  compact
                  templates={templates}
                />

                <section aria-label="Schedule" className="space-y-2.5">
                  <div className="grid min-w-0 gap-1.5 sm:grid-cols-[7rem_minmax(0,1fr)] sm:items-center sm:gap-3">
                    <Label
                      htmlFor="new-event-time-zone"
                      className="text-xs text-muted-foreground"
                    >
                      Time zone
                    </Label>
                    <TimeZoneCombobox
                      id="new-event-time-zone"
                      value={timeZoneId}
                      onValueChange={(value) => {
                        setScheduleOverride(schedule);
                        setTimeZoneOverride(value);
                        setScheduleError(null);
                        setDirty(true);
                      }}
                    />
                  </div>
                  <EventTimelineFields
                    compact
                    schedule={schedule}
                    onScheduleChange={changeSchedule}
                    timeZoneId={timeZoneId}
                  />
                  {scheduleError ? (
                    <p role="alert" className="text-sm text-destructive">
                      {scheduleError}
                    </p>
                  ) : null}
                </section>

                <EventRecurrenceFields
                  startDate={schedule.startsAt}
                  timeZoneId={timeZoneId}
                  onDirty={() => setDirty(true)}
                />
                <input type="hidden" name="requiresFeedback" value="true" />
              </div>
            </div>
            <DialogFooter className="px-5 pb-4 sm:flex-row sm:justify-end">
              <Button
                type="button"
                variant="outline"
                disabled={pending}
                onClick={requestClose}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={pending}>
                {pending ? "Creating event..." : "Create event"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <AlertDialog open={discardOpen} onOpenChange={setDiscardOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Discard testing event draft?</AlertDialogTitle>
            <AlertDialogDescription>
              Your unsaved event details and recurrence schedule will be lost.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Keep editing</AlertDialogCancel>
            <AlertDialogAction variant="destructive" onClick={closeDrawer}>
              Discard draft
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}

export function EditTestingEventDialog({
  event,
}: {
  event: TestingLabTestingEventProjection;
}) {
  return (
    <EventActionDialog
      trigger={
        <Button variant="outline">
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      }
      title="Edit testing event"
      description="Adjust the event brief, review model, application window, and delivery dates."
      submitLabel="Save event"
      action={updateTestingEvent}
    >
      <EventFields event={event} timeZoneId={event.timeZoneId ?? "UTC"} />
    </EventActionDialog>
  );
}

export function CreateTestingEventSlotDialog({
  event,
  existingSlots = [],
}: {
  event: TestingLabTestingEventProjection;
  existingSlots?: TestingLabTestingEventSlotProjection[];
}) {
  const eventId = event.id ?? "";
  const timeZoneId = event.timeZoneId ?? "UTC";
  const eventStartsAt = apiDatetimeLocal(event.startsAt, timeZoneId);
  const eventEndsAt = apiDatetimeLocal(event.endsAt, timeZoneId);

  return (
    <TestingTimeSlotBuilder
      event={event}
      eventId={eventId}
      eventStartsAt={eventStartsAt}
      eventEndsAt={eventEndsAt}
      timeZoneId={timeZoneId}
      existingSlots={existingSlots}
      presentation="dialog"
    />
  );
}

export function TestingTimeSlotPlanner({
  event,
  existingSlots = [],
}: {
  event: TestingLabTestingEventProjection;
  existingSlots?: TestingLabTestingEventSlotProjection[];
}) {
  const eventId = event.id ?? "";
  const timeZoneId = event.timeZoneId ?? "UTC";

  return (
    <TestingTimeSlotBuilder
      event={event}
      eventId={eventId}
      eventStartsAt={apiDatetimeLocal(event.startsAt, timeZoneId)}
      eventEndsAt={apiDatetimeLocal(event.endsAt, timeZoneId)}
      timeZoneId={timeZoneId}
      existingSlots={existingSlots}
      presentation="inline"
    />
  );
}

export interface TestingTimeSlotSchedule {
  startsAt: string;
  endsAt: string;
}

const wallClockPattern = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/;

function wallClockMilliseconds(value: string) {
  const match = wallClockPattern.exec(value);
  if (!match) return null;
  const [, year, month, day, hour, minute] = match;
  const result = Date.UTC(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
  );
  const date = new Date(result);
  if (
    date.getUTCFullYear() !== Number(year) ||
    date.getUTCMonth() !== Number(month) - 1 ||
    date.getUTCDate() !== Number(day) ||
    date.getUTCHours() !== Number(hour) ||
    date.getUTCMinutes() !== Number(minute)
  )
    return null;
  return result;
}

function wallClockFromMilliseconds(value: number) {
  const date = new Date(value);
  const pad = (part: number) => String(part).padStart(2, "0");
  return `${date.getUTCFullYear()}-${pad(date.getUTCMonth() + 1)}-${pad(date.getUTCDate())}T${pad(date.getUTCHours())}:${pad(date.getUTCMinutes())}`;
}

export function defaultTestingSessionEnd(
  eventStartsAt: string,
  eventEndsAt: string,
) {
  const start = wallClockMilliseconds(eventStartsAt);
  const end = wallClockMilliseconds(eventEndsAt);
  if (start == null || end == null || end <= start) return eventEndsAt;

  const startsOn = eventStartsAt.slice(0, 10);
  const endsOn = eventEndsAt.slice(0, 10);
  if (startsOn === endsOn) return eventEndsAt;

  return wallClockFromMilliseconds(Math.min(start + 4 * 60 * 60_000, end));
}

export function buildTestingTimeSlots(
  startsAt: string,
  endsAt: string,
  durationMinutes: number,
  breakMinutes: number,
): TestingTimeSlotSchedule[] {
  const rangeStart = wallClockMilliseconds(startsAt);
  const rangeEnd = wallClockMilliseconds(endsAt);
  if (
    rangeStart == null ||
    rangeEnd == null ||
    rangeEnd <= rangeStart ||
    !Number.isInteger(durationMinutes) ||
    durationMinutes < 1 ||
    !Number.isInteger(breakMinutes) ||
    breakMinutes < 0
  )
    return [];

  const duration = durationMinutes * 60_000;
  const step = (durationMinutes + breakMinutes) * 60_000;
  const slots: TestingTimeSlotSchedule[] = [];
  for (
    let cursor = rangeStart;
    cursor + duration <= rangeEnd && slots.length < 200;
    cursor += step
  ) {
    slots.push({
      startsAt: wallClockFromMilliseconds(cursor),
      endsAt: wallClockFromMilliseconds(cursor + duration),
    });
  }
  return slots;
}

export function validateTestingSlotWindow(start: string, end: string, eventStart: string, eventEnd: string, timeZoneId: string) {
  const values = [start, end, eventStart, eventEnd].map((value) => wallClockToUtcIso(value, timeZoneId));
  if (values.some((value) => !value)) return "Enter valid start and end dates and times.";
  const [startsAt, endsAt, startsBound, endsBound] = values.map((value) => Date.parse(value!));
  if (endsAt! <= startsAt!) return "The session must end after it starts.";
  if (startsAt! < startsBound! || endsAt! > endsBound!) return "Keep the session within the event start and end dates.";
  return null;
}

function timeSlotLabel(slot: TestingTimeSlotSchedule) {
  // These values are already in the event's wall clock; UTC here only formats
  // the calendar components without applying a second timezone conversion.
  return formatEventDateRange(`${slot.startsAt}Z`, `${slot.endsAt}Z`, "UTC").replace(" · UTC", "");
}

function TestingTimeSlotBuilder({
  event,
  eventId,
  eventStartsAt,
  eventEndsAt,
  timeZoneId,
  existingSlots,
  presentation,
}: {
  event: TestingLabTestingEventProjection;
  eventId: string;
  eventStartsAt: string;
  eventEndsAt: string;
  timeZoneId: string;
  existingSlots: TestingLabTestingEventSlotProjection[];
  presentation: "dialog" | "inline";
}) {
  const [startsAt, setStartsAt] = useState(eventStartsAt);
  const [endsAt, setEndsAt] = useState(() =>
    defaultTestingSessionEnd(eventStartsAt, eventEndsAt),
  );
  const [durationMinutes, setDurationMinutes] = useState(45);
  const [breakMinutes, setBreakMinutes] = useState(15);
  const [mode, setMode] = useState<
    NonNullable<TestingLabTestingEventProjection["mode"]>
  >(event.mode ?? "Online");
  const windowError = validateTestingSlotWindow(startsAt, endsAt, eventStartsAt, eventEndsAt, timeZoneId);
  const plannedSlots = useMemo(
    () =>
      buildTestingTimeSlots(startsAt, endsAt, durationMinutes, breakMinutes),
    [breakMinutes, durationMinutes, endsAt, startsAt],
  );
  const existingKeys = useMemo(
    () =>
      new Set(
        existingSlots.map(
          (slot) =>
            `${apiDatetimeLocal(slot.startsAt, timeZoneId)}|${apiDatetimeLocal(slot.endsAt, timeZoneId)}`,
        ),
      ),
    [existingSlots, timeZoneId],
  );
  const slots = useMemo(
    () =>
      plannedSlots.filter(
        (slot) => !windowError && !existingKeys.has(`${slot.startsAt}|${slot.endsAt}`),
      ),
    [existingKeys, plannedSlots, windowError],
  );
  const duplicateCount = windowError ? 0 : plannedSlots.length - slots.length;
  const previewTitle = slots.length > 0
    ? `${slots.length} slot${slots.length === 1 ? "" : "s"} ready`
    : windowError ? "Check the session dates"
      : duplicateCount > 0 ? "Schedule saved" : "No slots to create";
  const createSlotsLabel = slots.length > 0
    ? `Create ${slots.length} time slot${slots.length === 1 ? "" : "s"}`
    : duplicateCount > 0 ? "Slots already saved" : "Create time slots";
  const rangeEnd = wallClockMilliseconds(endsAt);
  const lastPlannedEnd = plannedSlots.at(-1)?.endsAt;
  const unusedMinutes =
    rangeEnd != null && lastPlannedEnd
      ? Math.max(
          0,
          Math.round(
            (rangeEnd - (wallClockMilliseconds(lastPlannedEnd) ?? rangeEnd)) /
              60_000,
          ),
        )
      : 0;

  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [result, setResult] =
    useState<TestingEventActionResult<unknown> | null>(null);

  function submitInline(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (windowError || slots.length === 0 || pending) return;
    const form = event.currentTarget;
    const data = new FormData(form);
    startTransition(async () => {
      try {
        const next = await createTestingEventSlots(data);
        setResult(next);
        if (next.success) router.refresh();
      } catch (error) {
        setResult(actionFailure(error));
      }
    });
  }

  const hiddenFields = (
    <>
      <input type="hidden" name="eventId" value={eventId} />
      <input type="hidden" name="timeZoneId" value={timeZoneId} />
      <input type="hidden" name="slotsJson" value={JSON.stringify(slots)} />
    </>
  );

  const windowFields = (
    <section aria-labelledby="slot-window-heading" className="space-y-4">
      <div>
        <h3 id="slot-window-heading" className="text-sm font-semibold">
          Session window
        </h3>
        <p className="mt-0.5 text-xs text-muted-foreground">
          Choose when testing runs. The range cannot leave the event window.
        </p>
      </div>
      <DateTimeRangePicker
        id={`slot-plan-${eventId}`}
        label="Session window"
        startLabel="Starts"
        endLabel="Ends"
        startName="planStartsAt"
        endName="planEndsAt"
        timeZoneId={timeZoneId}
        value={{ start: startsAt, end: endsAt }}
        onValueChange={(next) => { setStartsAt(next.start); setEndsAt(next.end); }}
        required
      />
      <p className="text-xs text-muted-foreground">24-hour clock · {timeZoneId}</p>
      {windowError ? <p role="alert" className="text-sm text-destructive">{windowError}</p> : null}
      <div className="grid gap-3 sm:grid-cols-2">
        <div className="space-y-1.5">
          <Label htmlFor="slot-duration">Testing per slot</Label>
          <div className="relative">
            <Input
              id="slot-duration"
              type="number"
              min="1"
              max="1440"
              value={durationMinutes}
              onChange={(event) =>
                setDurationMinutes(Number(event.target.value))
              }
              className="pr-12"
            />
            <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-xs text-muted-foreground">
              min
            </span>
          </div>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="slot-break">Reset between slots</Label>
          <div className="relative">
            <Input
              id="slot-break"
              type="number"
              min="0"
              max="1440"
              value={breakMinutes}
              onChange={(event) => setBreakMinutes(Number(event.target.value))}
              className="pr-12"
            />
            <span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-xs text-muted-foreground">
              min
            </span>
          </div>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="slot-testers">Testers per slot</Label>
          <Input
            id="slot-testers"
            name="maxTesters"
            type="number"
            min="1"
            placeholder="Unlimited"
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="slot-projects">Projects per slot</Label>
          <Input
            id="slot-projects"
            name="maxProjects"
            type="number"
            min="1"
            placeholder="Unlimited"
          />
        </div>
      </div>
    </section>
  );

  const deliveryFields = (
    <section aria-labelledby="slot-delivery-heading" className="space-y-4">
      <div>
        <h3 id="slot-delivery-heading" className="text-sm font-semibold">
          Where testing happens
        </h3>
        <p className="mt-0.5 text-xs text-muted-foreground">
          This delivery setup is applied to every slot in this batch.
        </p>
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <div className="space-y-1.5">
          <Label htmlFor="session-mode">Format</Label>
          <Select
            name="mode"
            value={mode}
            onValueChange={(value) =>
              setMode(
                value as NonNullable<TestingLabTestingEventProjection["mode"]>,
              )
            }
          >
            <SelectTrigger id="session-mode">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="InPerson">In person</SelectItem>
              <SelectItem value="Online">Online</SelectItem>
              <SelectItem value="Hybrid">Hybrid</SelectItem>
            </SelectContent>
          </Select>
        </div>
        {mode !== "InPerson" ? (
          <div className="space-y-1.5">
            <Label htmlFor="slot-url">Meeting URL</Label>
            <Input
              id="slot-url"
              name="meetingUrl"
              type="url"
              placeholder="https://"
              required={mode === "Online"}
            />
          </div>
        ) : null}
        {mode !== "Online" ? (
          <>
            <div className="space-y-1.5">
              <Label htmlFor="slot-campus">Campus</Label>
              <Input id="slot-campus" name="campusName" required />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="slot-room">Room</Label>
              <Input id="slot-room" name="roomName" required />
            </div>
          </>
        ) : null}
      </div>
    </section>
  );

  const preview = (
    <section aria-labelledby="slot-preview-heading">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="text-xs font-medium text-muted-foreground">
            Schedule preview
          </p>
          <h3 id="slot-preview-heading" className="mt-1 text-xl font-semibold">
            {previewTitle}
          </h3>
          <p className="mt-1 text-sm text-muted-foreground">
            {durationMinutes || 0} min testing, {breakMinutes || 0} min reset
          </p>
        </div>
        <span className="text-xs text-muted-foreground">{timeZoneId}</span>
      </div>

      {slots.length > 0 ? (
        <ol className="mt-5 max-h-[22rem] space-y-1 overflow-y-auto pr-1">
          {slots.map((slot, index) => (
            <li
              key={`${slot.startsAt}-${slot.endsAt}`}
              className="grid grid-cols-[2rem_minmax(0,1fr)] gap-3 rounded-md px-2 py-2.5 hover:bg-background/70"
            >
              <span className="flex size-8 items-center justify-center rounded-full bg-background text-xs font-semibold tabular-nums ring-1 ring-border">
                {index + 1}
              </span>
              <div className="min-w-0">
                <p className="text-sm font-medium tabular-nums">
                  {timeSlotLabel(slot)}
                </p>
                <p className="mt-0.5 text-xs text-muted-foreground">
                  {mode === "InPerson"
                    ? "In-person testing"
                    : mode === "Hybrid"
                      ? "Hybrid testing"
                      : "Online testing"}
                </p>
              </div>
            </li>
          ))}
        </ol>
      ) : (
        <div className="mt-5 rounded-md border border-dashed px-4 py-8 text-center">
          <Clock3 className="mx-auto size-5 text-muted-foreground" />
          <p className="mt-2 text-sm font-medium">{windowError ? "Check the session dates" : duplicateCount > 0 ? "Already in the schedule" : "No complete slot fits"}</p>
          <p className="mt-1 text-xs text-muted-foreground">
            {windowError ?? (duplicateCount > 0 ? "These time slots are saved. Change the time window to add another session." : "Increase the session window or shorten the testing time.")}
          </p>
        </div>
      )}

      <div className="mt-4 space-y-1 text-xs text-muted-foreground">
        {unusedMinutes > 0 ? (
          <p>{unusedMinutes} min remain after the last complete slot.</p>
        ) : null}
        {duplicateCount > 0 ? (
          <p>
            {duplicateCount} existing slot
            {duplicateCount === 1 ? " was" : "s were"} skipped.
          </p>
        ) : null}
        {plannedSlots.length === 200 ? (
          <p className="text-amber-600 dark:text-amber-400">
            Preview limited to 200 slots. Shorten the range or increase the
            testing time.
          </p>
        ) : null}
      </div>

      {presentation === "inline" ? (
        <div className="mt-5 border-t pt-4">
          <ActionMessage result={result} />
          <Button
            type="submit"
            disabled={pending || slots.length === 0 || !eventId}
            className="w-full"
          >
            {pending
              ? "Creating slots..."
              : createSlotsLabel}
          </Button>
          <p className="mt-2 text-center text-xs text-muted-foreground">
            You can edit individual slots after creation.
          </p>
        </div>
      ) : null}
    </section>
  );

  if (presentation === "inline") {
    return (
      <div className="overflow-hidden rounded-lg border bg-card/30">
        <div className="flex flex-col gap-3 border-b bg-muted/20 px-5 py-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="font-semibold">Plan a testing session</h2>
            <p className="mt-0.5 text-sm text-muted-foreground">
              Set one timebox and preview every bookable slot before saving.
            </p>
          </div>
          <div className="text-sm sm:text-right">
            <p className="font-medium">Event window</p>
            <p className="mt-0.5 text-xs text-muted-foreground">
              {formatEventDateRange(event.startsAt, event.endsAt, timeZoneId)}
            </p>
          </div>
        </div>

        <form onSubmit={submitInline}>
          {hiddenFields}
          <div className="grid lg:grid-cols-[minmax(0,1.15fr)_minmax(20rem,0.85fr)]">
            <div className="space-y-6 p-5 lg:p-6">
              {windowFields}
              <div className="border-t pt-6">{deliveryFields}</div>
            </div>
            <aside className="border-t bg-muted/20 p-5 lg:border-l lg:border-t-0 lg:p-6">
              {preview}
            </aside>
          </div>
        </form>
      </div>
    );
  }

  return (
    <EventActionDialog
      trigger={
        <Button size="sm" disabled={!eventId}>
          <Plus className="mr-2 size-4" />
          Build time slots
        </Button>
      }
      title="Build testing time slots"
      description="Choose one test timebox and let Testing Lab divide the session into bookable blocks."
      submitLabel={createSlotsLabel}
      action={createTestingEventSlots}
      submitDisabled={slots.length === 0}
    >
      {hiddenFields}
      <div className="space-y-4">
        <div className="flex gap-2 rounded-md bg-muted/50 px-3 py-2.5 text-sm">
          <Clock3 className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
          <div>
            <p className="font-medium">Event window</p>
            <p className="text-muted-foreground">
              {formatEventDateRange(event.startsAt, event.endsAt, timeZoneId)}
            </p>
          </div>
        </div>

        {windowFields}
        {deliveryFields}
        <div className="rounded-md bg-muted/35 p-3">{preview}</div>
      </div>
    </EventActionDialog>
  );
}

export function ManageTestingEventSlotDialog({
  eventId,
  slot,
  eventStartsAt,
  eventEndsAt,
  timeZoneId = "UTC",
}: {
  eventId: string;
  slot: TestingLabTestingEventSlotProjection;
  eventStartsAt?: string | null;
  eventEndsAt?: string | null;
  timeZoneId?: string;
}) {
  if (!slot.id) return null;
  return (
    <EventActionDialog
      trigger={
        <Button size="sm" variant="outline">
          <Pencil className="mr-2 size-4" />
          Edit time slot
        </Button>
      }
      title="Edit testing time slot"
      description="Change this bookable block without affecting the other time slots in the event."
      submitLabel="Save time slot"
      action={updateTestingEventSlot}
    >
      <input type="hidden" name="eventId" value={eventId} />
      <input type="hidden" name="slotId" value={slot.id} />
      <input type="hidden" name="timeZoneId" value={timeZoneId} />
      <TestingSlotEditorFields slot={slot} eventStartsAt={eventStartsAt} eventEndsAt={eventEndsAt} timeZoneId={timeZoneId} />
      <div className="border-t pt-4">
        <EventActionDialog
          trigger={
            <Button type="button" size="sm" variant="destructive">
              <Trash2 className="mr-2 size-4" />
              Delete time slot
            </Button>
          }
          title="Delete this testing time slot?"
          description="A time slot with approved projects or tester registrations cannot be deleted."
          submitLabel="Delete time slot"
          action={deleteTestingEventSlot}
          destructive
        >
          <input type="hidden" name="eventId" value={eventId} />
          <input type="hidden" name="slotId" value={slot.id} />
        </EventActionDialog>
      </div>
    </EventActionDialog>
  );
}

function TestingSlotEditorFields({ slot, eventStartsAt, eventEndsAt, timeZoneId }: {
  slot: TestingLabTestingEventSlotProjection;
  eventStartsAt?: string | null;
  eventEndsAt?: string | null;
  timeZoneId: string;
}) {
  // The dialog mounts these fields on open, so cancelling resets unsaved delivery choices.
  const [mode, setMode] = useState(slot.mode ?? "Online");
  return (
    <div className="space-y-5">
      <input type="hidden" name="locationId" value={mode === "Online" ? "" : slot.locationId ?? ""} />
      {eventStartsAt && eventEndsAt ? (
        <p className="text-xs text-muted-foreground">
          Event window: {formatEventDateRange(eventStartsAt, eventEndsAt, timeZoneId)}
        </p>
      ) : null}
      <section aria-label="Time slot dates" className="space-y-2">
        <DateTimeRangePicker
          id={`slot-range-${slot.id}`}
          label="Time slot"
          startLabel="Slot starts"
          endLabel="Slot ends"
          startName="startsAt"
          endName="endsAt"
          required
          timeZoneId={timeZoneId}
          defaultValue={{ start: apiDatetimeLocal(slot.startsAt, timeZoneId), end: apiDatetimeLocal(slot.endsAt, timeZoneId) }}
        />
        <p className="text-xs text-muted-foreground">24-hour clock · {timeZoneId}</p>
      </section>
      <section aria-label="Time slot delivery" className="grid gap-4 border-t pt-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor={`slot-mode-${slot.id}`}>Format</Label>
          <Select name="mode" value={mode} onValueChange={(value) => setMode(value as typeof mode)}>
            <SelectTrigger id={`slot-mode-${slot.id}`} className="w-full"><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="Online">Online</SelectItem>
              <SelectItem value="InPerson">In person</SelectItem>
              <SelectItem value="Hybrid">Hybrid</SelectItem>
            </SelectContent>
          </Select>
        </div>
        {mode !== "InPerson" ? (
          <div className="space-y-2">
            <Label htmlFor={`slot-url-${slot.id}`}>Meeting URL</Label>
            <Input id={`slot-url-${slot.id}`} name="meetingUrl" type="url" required={mode === "Online"} defaultValue={slot.meetingUrl ?? ""} placeholder="https://" />
          </div>
        ) : null}
        {mode !== "Online" ? (
          <>
            <div className="space-y-2">
              <Label htmlFor={`slot-campus-${slot.id}`}>Campus</Label>
              <Input id={`slot-campus-${slot.id}`} name="campusName" required defaultValue={slot.campusName ?? ""} />
            </div>
            <div className="space-y-2">
              <Label htmlFor={`slot-room-${slot.id}`}>Room</Label>
              <Input id={`slot-room-${slot.id}`} name="roomName" required defaultValue={slot.roomName ?? ""} />
            </div>
          </>
        ) : null}
      </section>
      <section aria-label="Time slot capacity" className="grid gap-4 border-t pt-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label htmlFor={`slot-testers-${slot.id}`}>Tester capacity</Label>
          <Input id={`slot-testers-${slot.id}`} name="maxTesters" type="number" min="1" defaultValue={slot.maxTesters ?? ""} placeholder="Unlimited" />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-projects-${slot.id}`}>Project capacity</Label>
          <Input id={`slot-projects-${slot.id}`} name="maxProjects" type="number" min="1" defaultValue={slot.maxProjects ?? ""} placeholder="Unlimited" />
        </div>
      </section>
    </div>
  );
}

export function TestingEventLifecycleActions({
  event,
}: {
  event: TestingLabTestingEventProjection;
}) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [result, setResult] =
    useState<TestingEventActionResult<unknown> | null>(null);
  const nextByStatus: Partial<
    Record<TestingLabTestingEventStatus, [string, string, typeof Send]>
  > = {
    Draft: ["open-applications", "Publish and open sign-ups", Send],
    ApplicationsOpen: ["close-applications", "Close game submissions", CircleStop],
    ApplicationsClosed: ["schedule", "Publish session schedule", Clock3],
    Scheduled: ["activate", "Start playtest", Play],
    Active: ["complete", "Complete playtest", CheckCircle2],
  };
  const next = nextByStatus[event.status ?? "Draft"];
  const configuration = event.configuration;
  const draftConfigurationReady = Boolean(
    configuration?.generalRules?.trim() &&
    configuration.candidateInstructions?.trim() &&
    configuration.testerInstructions?.trim() &&
    configuration.projectApplicationSchema &&
    configuration.testerRegistrationSchema,
  );

  if (!event.id) return null;

  function run(transition: string) {
    const form = new FormData();
    form.set("eventId", event.id!);
    form.set("transition", transition);
    startTransition(async () => {
      try {
        const nextResult = await transitionTestingEvent(form);
        setResult(nextResult);
        if (nextResult.success) router.refresh();
      } catch (error) {
        setResult(actionFailure(error));
      }
    });
  }

  const NextIcon = next?.[2];
  const lifecycleGuidance: Record<string, string> = {
    Draft: draftConfigurationReady
      ? "This event is private. Publish it when the rules and sign-up details are ready."
      : "Finish the event rules and sign-up forms before publishing this event.",
    ApplicationsOpen: "Game submissions are open. Close them when you are ready to open tester sign-ups.",
    ApplicationsClosed: "Game submissions are closed. Testers can join saved slots. Finish project review and publish the schedule.",
    Scheduled: "The schedule is published. Start the playtest when the first session begins.",
    Active: "The playtest is underway. Complete it when testing and attendance updates are finished.",
    Completed: "This playtest is complete. Its applications, attendance, and feedback remain available below.",
    Cancelled: "This event was cancelled. Its history remains available for reference.",
  };
  return (
    <div className="grid w-full gap-3 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
      <div className="min-w-0 max-w-2xl">
        <p className="text-sm text-muted-foreground">{lifecycleGuidance[event.status ?? "Draft"]}</p>
      </div>
      <div className="flex flex-wrap gap-2">
        {event.status === "Draft" && !draftConfigurationReady && event.id ? (
          <a
            href={`/workspace/testing-lab/events/${event.id}/overview#event-configuration-heading`}
            className={buttonVariants({ size: "sm" })}
          >
            <Pencil className="mr-2 size-4" />
            Finish event setup
          </a>
        ) : next && NextIcon ? (
          <Button size="sm" disabled={pending} onClick={() => run(next[0])}>
            <NextIcon className="mr-2 size-4" />
            {next[1]}
          </Button>
        ) : null}
        {!["Completed", "Cancelled"].includes(event.status ?? "") ? (
          <EventActionDialog
            trigger={
              <Button size="sm" variant="ghost">
                <CircleStop className="mr-2 size-4" />
                Cancel event
              </Button>
            }
            title="Cancel this testing event?"
            description="Cancellation preserves applications, attendance, feedback, and audit history."
            submitLabel="Cancel event"
            action={transitionTestingEvent}
            destructive
          >
            <input type="hidden" name="eventId" value={event.id} />
            <input type="hidden" name="transition" value="cancel" />
            <div className="space-y-2">
              <Label htmlFor="event-cancellation-reason">
                Cancellation reason
              </Label>
              <Textarea
                id="event-cancellation-reason"
                name="reason"
                required
                rows={4}
              />
            </div>
          </EventActionDialog>
        ) : null}
        {event.status === "Draft" && event.id ? (
          <EventActionDialog
            trigger={
              <Button size="sm" variant="ghost" className="text-muted-foreground">
                <Trash2 className="mr-2 size-4" />
                Delete draft
              </Button>
            }
            title="Delete this draft event?"
            description="Only an unused draft can be deleted. This action cannot be undone."
            submitLabel="Delete draft"
            action={deleteTestingEvent}
            destructive
            successHref="/workspace/testing-lab/events"
          >
            <input type="hidden" name="eventId" value={event.id} />
          </EventActionDialog>
        ) : null}
        {["Completed", "Cancelled"].includes(event.status ?? "") && event.id ? (
          <EventActionDialog
            trigger={
              <Button size="sm" variant="outline">
                <Archive className="mr-2 size-4" />
                Archive event
              </Button>
            }
            title="Archive this testing event?"
            description="The event leaves the active directory while its audit history remains available for restoration."
            submitLabel="Archive event"
            action={archiveTestingEvent}
            successHref="/workspace/testing-lab/events"
          >
            <input type="hidden" name="eventId" value={event.id} />
          </EventActionDialog>
        ) : null}
      </div>
      {result ? <div className="lg:col-span-2"><ActionMessage result={result} /></div> : null}
    </div>
  );
}

export function RestoreTestingEventDialog({
  event,
}: {
  event: TestingLabTestingEventProjection;
}) {
  if (!event.id) return null;
  return (
    <EventActionDialog
      trigger={
        <Button variant="outline">
          <RotateCcw className="mr-2 size-4" />
          Restore event
        </Button>
      }
      title="Restore this testing event?"
      description="The event returns to the active directory with its terminal status and audit history intact."
      submitLabel="Restore event"
      action={restoreTestingEvent}
    >
      <input type="hidden" name="eventId" value={event.id} />
    </EventActionDialog>
  );
}

export function TestingEventCommittee({
  event,
  members,
  committee,
  readOnly = false,
}: {
  event: TestingLabTestingEventProjection;
  members: TestingLabMemberOption[];
  committee: TestingLabTestingEventCommitteeMemberProjection[];
  readOnly?: boolean;
}) {
  return (
    <section>
      <div className="mb-3 flex items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold">Review committee</h2>
          <p className="text-sm text-muted-foreground">
            {event.approvalMode === "Committee"
              ? "Committee votes inform approval; the manager resolves ties."
              : "Manager-only approval is active."}
          </p>
        </div>
        {event.id && !readOnly ? (
          <EventActionDialog
            trigger={
              <Button size="sm" variant="outline">
                <UserRoundCheck className="mr-2 size-4" />
                Add reviewer
              </Button>
            }
            title="Add committee reviewer"
            description="Only active tenant members can review project applications."
            submitLabel="Add reviewer"
            action={addTestingEventCommitteeMember}
          >
            <input type="hidden" name="eventId" value={event.id} />
            <div className="space-y-2">
              <Label>Member</Label>
              <Select name="userId" required>
                <SelectTrigger aria-label="Committee member">
                  <SelectValue placeholder="Choose a member" />
                </SelectTrigger>
                <SelectContent>
                  {members.map((member) => (
                    <SelectItem key={member.id} value={member.id}>
                      {member.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" name="isChair" /> Committee chair
            </label>
          </EventActionDialog>
        ) : null}
      </div>
      {committee.length === 0 ? (
        <p className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">
          No reviewers assigned.
        </p>
      ) : (
        <div className="divide-y rounded-md border">
          {committee.map((reviewer) => (
            <div
              key={reviewer.id ?? reviewer.userId}
              className="flex items-center justify-between gap-3 p-3"
            >
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">
                  {reviewer.userName ?? reviewer.userEmail ?? reviewer.userId}
                </p>
                <p className="truncate text-xs text-muted-foreground">
                  {reviewer.userEmail}
                </p>
              </div>
              <div className="flex items-center gap-2">
                {reviewer.isChair ? (
                  <Badge variant="secondary">Chair</Badge>
                ) : null}
                {event.id && reviewer.userId && !readOnly ? (
                  <EventActionDialog
                    trigger={
                      <Button
                        size="icon"
                        variant="ghost"
                        aria-label={`Remove ${reviewer.userName ?? "reviewer"}`}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    }
                    title="Remove this reviewer?"
                    description="A reviewer with recorded votes cannot be removed from the audit trail."
                    submitLabel="Remove reviewer"
                    action={removeTestingEventCommitteeMember}
                    destructive
                  >
                    <input type="hidden" name="eventId" value={event.id} />
                    <input
                      type="hidden"
                      name="userId"
                      value={reviewer.userId}
                    />
                  </EventActionDialog>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

export interface TestingEventApplicationAccess {
  canViewApplications?: boolean;
  canManageApplications?: boolean;
  canVote?: boolean;
}

export function TestingEventApplications({
  eventId,
  access,
  applications,
  slots,
  projectLabels = {},
  memberLabels = {},
  readOnly = false,
}: {
  eventId: string;
  access: TestingEventApplicationAccess | null;
  applications: TestingLabTestingProjectApplicationProjection[];
  slots: TestingLabTestingEventSlotProjection[];
  readOnly?: boolean;
  projectLabels?: Record<string, string>;
  memberLabels?: Record<string, string>;
}) {
  const router = useRouter();
  const [reviewingApplicationId, setReviewingApplicationId] = useState<
    string | null
  >(null);
  const [reviewPending, startReviewTransition] = useTransition();
  const [reviewResult, setReviewResult] = useState<{
    applicationId: string;
    result: TestingEventActionResult<unknown>;
  } | null>(null);

  if (applications.length === 0) {
    return (
      <p className="rounded-md border border-dashed p-5 text-center text-sm text-muted-foreground">
        No project applications yet.
      </p>
    );
  }

  return (
    <div className="divide-y rounded-md border">
      {applications.map((application, index) => {
        const status = application.status ?? "Pending";
        const projectLabel = application.projectId
          ? (projectLabels[application.projectId] ??
            "Project details unavailable")
          : "Project details unavailable";
        const memberLabel = application.submittedByUserId
          ? (memberLabels[application.submittedByUserId] ??
            "Member details unavailable")
          : "Member details unavailable";
        return (
          <div
            key={
              application.id ?? `${application.projectId ?? "unknown"}:${index}`
            }
            className="flex flex-col gap-3 p-4 lg:flex-row lg:items-center lg:justify-between"
          >
            <div className="min-w-0">
              <p className="truncate font-medium">{projectLabel}</p>
              <p className="text-xs text-muted-foreground">
                Submitted by {memberLabel}
              </p>
              {application.decisionRationale ? (
                <p className="mt-1 text-sm text-muted-foreground">
                  {application.decisionRationale}
                </p>
              ) : null}
              {reviewResult?.applicationId === application.id ? (
                <div className="mt-3">
                  <ActionMessage result={reviewResult?.result ?? null} />
                </div>
              ) : null}
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="outline">
                {formatTestingEventStatus(status)}
              </Badge>
              {!readOnly &&
              access?.canManageApplications &&
              status === "Pending" &&
              application.id ? (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={
                    reviewPending && reviewingApplicationId === application.id
                  }
                  onClick={() => {
                    setReviewingApplicationId(application.id!);
                    startReviewTransition(async () => {
                      try {
                        const form = new FormData();
                        form.set("eventId", eventId);
                        form.set("applicationId", application.id!);
                        const result =
                          await beginTestingEventApplicationReview(form);
                        setReviewResult({
                          applicationId: application.id!,
                          result,
                        });
                        if (result.success) router.refresh();
                      } catch (error) {
                        setReviewResult({
                          applicationId: application.id!,
                          result: actionFailure(error),
                        });
                      } finally {
                        setReviewingApplicationId(null);
                      }
                    });
                  }}
                >
                  {reviewPending && reviewingApplicationId === application.id
                    ? "Starting..."
                    : "Review"}
                </Button>
              ) : null}
              {!readOnly &&
              ["Pending", "UnderReview", "Waitlisted"].includes(status) &&
              application.id ? (
                <>
                  {access?.canManageApplications ? (
                    <>
                      <EventActionDialog
                        trigger={
                          <Button size="sm">
                            <CheckCircle2 className="mr-2 size-4" />
                            Approve
                          </Button>
                        }
                        title="Approve project application"
                        description="Capacity is reserved only after this approval is accepted."
                        submitLabel="Approve project"
                        action={approveTestingEventApplication}
                      >
                        <input type="hidden" name="eventId" value={eventId} />
                        <input
                          type="hidden"
                          name="applicationId"
                          value={application.id}
                        />
                        <div className="space-y-2">
                          <Label htmlFor={`approve-slot-${application.id}`}>
                            Testing slot
                          </Label>
                          <Select name="slotId" required>
                            <SelectTrigger
                              id={`approve-slot-${application.id}`}
                              aria-label="Testing slot"
                            >
                              <SelectValue placeholder="Choose a slot" />
                            </SelectTrigger>
                            <SelectContent>
                              {slots
                                .filter((slot) => slot.id)
                                .map((slot) => (
                                  <SelectItem key={slot.id} value={slot.id!}>
                                    {formatEventDateTime(slot.startsAt)} ·{" "}
                                    {slot.campusName ??
                                      slot.meetingUrl ??
                                      slot.mode}
                                  </SelectItem>
                                ))}
                            </SelectContent>
                          </Select>
                        </div>
                        <div className="space-y-2">
                          <Label
                            htmlFor={`approve-rationale-${application.id}`}
                          >
                            Decision notes
                          </Label>
                          <Textarea
                            id={`approve-rationale-${application.id}`}
                            name="rationale"
                            rows={3}
                          />
                        </div>
                      </EventActionDialog>
                      <EventActionDialog
                        trigger={
                          <Button size="sm" variant="outline">
                            Waitlist
                          </Button>
                        }
                        title="Waitlist project application"
                        description="The application remains eligible but does not consume project capacity."
                        submitLabel="Add to waitlist"
                        action={waitlistTestingEventApplication}
                      >
                        <input type="hidden" name="eventId" value={eventId} />
                        <input
                          type="hidden"
                          name="applicationId"
                          value={application.id}
                        />
                        <div className="space-y-2">
                          <Label
                            htmlFor={`waitlist-rationale-${application.id}`}
                          >
                            Notes
                          </Label>
                          <Textarea
                            id={`waitlist-rationale-${application.id}`}
                            name="rationale"
                            rows={3}
                          />
                        </div>
                      </EventActionDialog>
                      <EventActionDialog
                        trigger={
                          <Button size="sm" variant="destructive">
                            Reject
                          </Button>
                        }
                        title="Reject project application"
                        description="A clear rationale is required and remains in the application audit trail."
                        submitLabel="Reject project"
                        action={rejectTestingEventApplication}
                        destructive
                      >
                        <input type="hidden" name="eventId" value={eventId} />
                        <input
                          type="hidden"
                          name="applicationId"
                          value={application.id}
                        />
                        <div className="space-y-2">
                          <Label htmlFor={`reject-rationale-${application.id}`}>
                            Rejection rationale
                          </Label>
                          <Textarea
                            id={`reject-rationale-${application.id}`}
                            name="rationale"
                            required
                            rows={4}
                          />
                        </div>
                      </EventActionDialog>
                    </>
                  ) : null}
                  {access?.canVote && status === "UnderReview" ? (
                    <EventActionDialog
                      trigger={
                        <Button size="sm" variant="ghost">
                          <ShieldCheck className="mr-2 size-4" />
                          Vote
                        </Button>
                      }
                      title="Record committee vote"
                      description="Votes are auditable and cannot be silently replaced by another reviewer."
                      submitLabel="Record vote"
                      action={voteOnTestingEventApplication}
                    >
                      <input type="hidden" name="eventId" value={eventId} />
                      <input
                        type="hidden"
                        name="applicationId"
                        value={application.id}
                      />
                      <div className="space-y-2">
                        <Label>Vote</Label>
                        <Select name="decision" required>
                          <SelectTrigger>
                            <SelectValue placeholder="Choose decision" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="Approve">Approve</SelectItem>
                            <SelectItem value="Reject">Reject</SelectItem>
                          </SelectContent>
                        </Select>
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor={`vote-comments-${application.id}`}>
                          Comments
                        </Label>
                        <Textarea
                          id={`vote-comments-${application.id}`}
                          name="comments"
                          rows={3}
                        />
                      </div>
                    </EventActionDialog>
                  ) : null}
                </>
              ) : null}
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function TestingSlotRegistrations({
  eventId,
  registrations,
  memberLabels,
  approvedApplications,
  readOnly = false,
}: {
  eventId: string;
  registrations: TestingLabTestingSlotRegistrationProjection[];
  memberLabels: Record<string, string>;
  approvedApplications: TestingLabApprovedApplicationOption[];
  readOnly?: boolean;
}) {
  if (registrations.length === 0)
    return (
      <p className="text-sm text-muted-foreground">No tester registrations.</p>
    );
  return (
    <div className="mt-3 divide-y border-t">
      {registrations.map((registration, index) => {
        const testerLabel = registration.userId
          ? memberLabels[registration.userId]
          : undefined;
        const assignableProjects = approvedApplications.filter(
          (application) => {
            if (!registration.userId) return false;
            return (
              application.eligibleTesterUserIds.includes(registration.userId) &&
              (!application.slotId ||
                application.slotId === registration.slotId)
            );
          },
        );
        const canAssignProject = ["CheckedIn", "Attended"].includes(
          registration.status ?? "",
        );
        const isTerminalRegistration = [
          "Cancelled",
          "Completed",
          "NoShow",
        ].includes(registration.status ?? "");
        const pendingFeedbackCount = isTerminalRegistration
          ? 0
          : (registration.pendingFeedbackCount ?? 0);

        return (
          <div
            key={
              registration.id ?? `${registration.userId ?? "unknown"}:${index}`
            }
            className="flex flex-col gap-3 py-3 lg:flex-row lg:items-center lg:justify-between"
          >
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">
                {testerLabel ?? "Unknown tester"}
              </p>
              <p className="text-xs text-muted-foreground">
                {pendingFeedbackCount} pending feedback /{" "}
                {formatTestingEventStatus(registration.status)}
              </p>
            </div>
            {registration.id && !readOnly && !isTerminalRegistration ? (
              <div className="flex flex-wrap items-center gap-2">
                {canAssignProject && assignableProjects.length > 0 ? (
                  <EventActionDialog
                    trigger={
                      <Button size="sm" variant="outline">
                        <ClipboardCheck className="mr-2 size-4" />
                        Assign tested project
                      </Button>
                    }
                    title="Assign a tested project"
                    description="Create the feedback obligation for this tester after check-in."
                    submitLabel="Assign project"
                    action={assignTestedProjectToRegistration}
                  >
                    <input type="hidden" name="eventId" value={eventId} />
                    <input
                      type="hidden"
                      name="registrationId"
                      value={registration.id}
                    />
                    <div className="space-y-2">
                      <Label>Approved project</Label>
                      <Select name="applicationId" required>
                        <SelectTrigger aria-label="Approved project">
                          <SelectValue placeholder="Choose a project" />
                        </SelectTrigger>
                        <SelectContent>
                          {assignableProjects.map((application) => (
                            <SelectItem
                              key={application.id}
                              value={application.id}
                            >
                              {application.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </EventActionDialog>
                ) : null}
                <form
                  className="flex items-center gap-2"
                  action={async (formData) => {
                    await updateTestingEventAttendance(formData);
                  }}
                >
                  <input type="hidden" name="eventId" value={eventId} />
                  <input
                    type="hidden"
                    name="registrationId"
                    value={registration.id}
                  />
                  <Select name="attendance" required>
                    <SelectTrigger className="w-36" aria-label="Attendance">
                      <SelectValue placeholder="Attendance" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="check-in">Check in</SelectItem>
                      <SelectItem value="check-out">Check out</SelectItem>
                      <SelectItem value="no-show">No show</SelectItem>
                      <SelectItem value="complete">Complete</SelectItem>
                    </SelectContent>
                  </Select>
                  <Button size="sm" type="submit">
                    Update
                  </Button>
                </form>
              </div>
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
export function TestingEventLearningDialog({
  event,
  activities,
  readOnly = false,
}: {
  event: TestingLabTestingEventProjection;
  activities: TestingLabLearningActivityOption[];
  readOnly?: boolean;
}) {
  const initialActivity = activities.find(
    (activity) => activity.id === event.learningActivityId,
  );
  const [selectedActivityId, setSelectedActivityId] = useState(
    initialActivity?.id ?? "",
  );
  const selectedActivity = activities.find(
    (activity) => activity.id === selectedActivityId,
  );

  if (!event.id || readOnly) return null;
  return (
    <EventActionDialog
      trigger={
        <Button size="sm" variant="outline">
          <Pencil className="mr-2 size-4" />
          Configure learning
        </Button>
      }
      title="Connect learning evidence"
      description="Testing Lab publishes completion evidence; Learning remains responsible for enrollment and grades."
      submitLabel="Save learning link"
      action={configureTestingEventLearning}
    >
      <input type="hidden" name="eventId" value={event.id} />
      <input
        type="hidden"
        name="courseId"
        value={selectedActivity?.courseId ?? event.courseId ?? ""}
      />
      <div className="space-y-2">
        <Label>Course activity</Label>
        <Select
          name="learningActivityId"
          required
          value={selectedActivityId}
          onValueChange={(value) => setSelectedActivityId(value ?? "")}
        >
          <SelectTrigger aria-label="Course activity">
            <SelectValue placeholder="Choose a lesson or graded activity" />
          </SelectTrigger>
          <SelectContent>
            {activities.map((activity) => (
              <SelectItem
                key={`${activity.courseId}:${activity.id}`}
                value={activity.id}
              >
                {activity.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label htmlFor="learning-cohort">Cohort id</Label>
        <Input
          id="learning-cohort"
          name="cohortId"
          defaultValue={event.cohortId ?? ""}
          placeholder="Optional: restrict evidence to one cohort"
        />
      </div>
      <div className="space-y-2">
        <Label>Completion requirement</Label>
        <Select
          name="requirement"
          defaultValue={
            event.learningCompletionRequirement ?? "AttendanceAndFeedback"
          }
        >
          <SelectTrigger aria-label="Completion requirement">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Attendance">Attendance</SelectItem>
            <SelectItem value="Feedback">Required feedback</SelectItem>
            <SelectItem value="AttendanceAndFeedback">
              Attendance and feedback
            </SelectItem>
            <SelectItem value="ProjectTested">
              Assigned project tested
            </SelectItem>
          </SelectContent>
        </Select>
      </div>
    </EventActionDialog>
  );
}
