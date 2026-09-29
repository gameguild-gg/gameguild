"use client";

import { calendarDatePart, wallClockDate, wallClockToUtcIso } from "@/lib/date-time-zone";
import { Button } from "@game-guild/ui/components/button";
import { Calendar } from "@game-guild/ui/components/calendar";
import { Input } from "@game-guild/ui/components/input";
import { Popover, PopoverContent, PopoverTrigger } from "@game-guild/ui/components/popover";
import { cn } from "@game-guild/ui/lib/utils";
import { CalendarDays } from "lucide-react";
import * as React from "react";
import type { DateRange } from "react-day-picker";

export type DateTimeRangeValue = {
  start: string;
  end: string;
};

export interface DateTimeRangePickerProps {
  id: string;
  label: string;
  startLabel?: string;
  endLabel?: string;
  startName: string;
  endName: string;
  timeZoneId: string;
  value?: DateTimeRangeValue;
  defaultValue?: DateTimeRangeValue;
  onValueChange?: (value: DateTimeRangeValue) => void;
  required?: boolean;
  disabled?: boolean;
  className?: string;
  compact?: boolean;
}

function wallClockParts(value: string) {
  const [date = "", time = ""] = value.split("T", 2);
  return { date, time };
}

export function DateTimeRangePicker({ compact = false, ...props }: DateTimeRangePickerProps) {
  return compact ? <CompactDateTimeRangePicker {...props} /> : <DateTimeRangeFields {...props} />;
}

function DateTimeRangeFields({
  id,
  label,
  startLabel = "Starts",
  endLabel = "Ends",
  startName,
  endName,
  timeZoneId,
  value,
  defaultValue = { start: "", end: "" },
  onValueChange,
  required = false,
  disabled = false,
  className,
}: DateTimeRangePickerProps) {
  const controlled = value !== undefined;
  const current = value ?? defaultValue;
  const [internalValue, setInternalValue] = React.useState(defaultValue);
  const displayed = controlled ? current : internalValue;
  const rootRef = React.useRef<HTMLDivElement>(null);

  React.useEffect(() => {
    const form = rootRef.current?.closest("form");
    if (!form || controlled) return;

    const reset = () => setInternalValue(defaultValue);
    form.addEventListener("reset", reset);
    return () => form.removeEventListener("reset", reset);
  }, [controlled, defaultValue]);

  function updateValue(field: "start" | "end", part: "date" | "time", nextPart: string) {
    const parts = wallClockParts(displayed[field]);
    const nextValue = `${part === "date" ? nextPart : parts.date}T${part === "time" ? nextPart : parts.time}`;
    const next = {
      ...displayed,
      [field]: nextValue === "T" ? "" : nextValue,
    };

    if (!controlled) setInternalValue(next);
    onValueChange?.(next);
  }

  function renderEndpoint(field: "start" | "end", endpointLabel: string) {
    const endpoint = wallClockParts(displayed[field]);
    const dateId = `${id}-${field}-date`;
    const timeId = `${id}-${field}-time`;

    return (
      <fieldset key={field} className="@container/endpoint min-w-0 space-y-1.5">
        <legend className="text-sm font-medium">{endpointLabel}</legend>
        <div className="grid min-w-0 gap-2 @min-[16rem]/endpoint:grid-cols-[minmax(0,1fr)_5.5rem]">
          <div className="grid min-w-0 gap-1">
            <label
              htmlFor={dateId}
              className="text-xs text-muted-foreground"
            >
              Date
            </label>
            <Input
              id={dateId}
              type="date"
              value={endpoint.date}
              onInput={(event) => updateValue(field, "date", event.currentTarget.value)}
              onChange={(event) =>
                updateValue(field, "date", event.target.value)
              }
              required={required}
              disabled={disabled}
              aria-label={`${endpointLabel} date`}
              className="w-full min-w-0 tabular-nums"
            />
          </div>
          <div className="grid min-w-0 gap-1">
            <label
              htmlFor={timeId}
              className="text-xs text-muted-foreground"
            >
              Time
            </label>
            <Input
              id={timeId}
              type="text"
              inputMode="numeric"
              autoComplete="off"
              placeholder="HH:MM"
              maxLength={5}
              pattern="(?:[01][0-9]|2[0-3]):[0-5][0-9]"
              value={endpoint.time}
              onChange={(event) =>
                updateValue(field, "time", event.target.value)
              }
              required={required}
              disabled={disabled}
              aria-label={`${endpointLabel} time`}
              className="w-full min-w-0 tabular-nums"
            />
          </div>
        </div>
      </fieldset>
    );
  }

  return (
    <div
      ref={rootRef}
      data-slot="date-time-range-picker"
      role="group"
      aria-label={`${label}, ${timeZoneId}, 24-hour clock`}
      className={cn("grid w-full min-w-0 grid-cols-[repeat(auto-fit,minmax(min(100%,16rem),1fr))] gap-3", className)}
    >
      <input type="hidden" name={startName} value={displayed.start} />
      <input type="hidden" name={endName} value={displayed.end} />
      {renderEndpoint("start", startLabel)}
      {renderEndpoint("end", endLabel)}
    </div>
  );
}

function displayDate(value: string) {
  const [year, month, day] = value.split("T", 1)[0]!.split("-");
  return year && month && day ? `${day}/${month}/${year}` : "Choose a date";
}

function displayRange(value: DateTimeRangeValue) {
  const start = wallClockParts(value.start);
  const end = wallClockParts(value.end);
  return `${displayDate(value.start)} · ${start.time} → ${displayDate(value.end)} · ${end.time}`;
}

function CompactDateTimeRangePicker({
  id, label, startLabel = "Starts", endLabel = "Ends", startName, endName,
  timeZoneId, value, defaultValue = { start: "", end: "" }, onValueChange,
  required = false, disabled = false, className,
}: DateTimeRangePickerProps) {
  const controlled = value !== undefined;
  const [internalValue, setInternalValue] = React.useState(defaultValue);
  const current = value ?? internalValue;
  const rootRef = React.useRef<HTMLDivElement>(null);
  const triggerRef = React.useRef<HTMLButtonElement>(null);
  const [open, setOpen] = React.useState(false);
  const [draftRange, setDraftRange] = React.useState<DateRange | undefined>();
  const [selectingEnd, setSelectingEnd] = React.useState(false);
  const [startTime, setStartTime] = React.useState("09:00");
  const [endTime, setEndTime] = React.useState("17:00");
  const [error, setError] = React.useState<string | null>(null);
  const [monthCount, setMonthCount] = React.useState(1);

  React.useEffect(() => {
    const media = window.matchMedia("(min-width: 768px)");
    const update = () => setMonthCount(media.matches ? 2 : 1);
    update();
    media.addEventListener?.("change", update);
    return () => media.removeEventListener?.("change", update);
  }, []);

  React.useEffect(() => {
    const form = rootRef.current?.closest("form");
    if (!form || controlled) return;
    const reset = () => {
      setInternalValue(defaultValue);
      setOpen(false);
    };
    form.addEventListener("reset", reset);
    return () => form.removeEventListener("reset", reset);
  }, [controlled, defaultValue]);

  function handleOpenChange(next: boolean) {
    if (next) {
      const from = wallClockDate(current.start, timeZoneId);
      const to = wallClockDate(current.end, timeZoneId);
      setDraftRange(from ? { from, to } : undefined);
      setSelectingEnd(false);
      setStartTime(wallClockParts(current.start).time || "09:00");
      setEndTime(wallClockParts(current.end).time || "17:00");
      setError(null);
    }
    setOpen(next);
  }

  function applyRange() {
    if (!draftRange?.from || !draftRange.to) return;
    if (![startTime, endTime].every((time) => /^(?:[01]\d|2[0-3]):[0-5]\d$/.test(time))) {
      setError("Enter both times as HH:MM (24-hour).");
      return;
    }
    const next = {
      start: `${calendarDatePart(draftRange.from, timeZoneId)}T${startTime}`,
      end: `${calendarDatePart(draftRange.to, timeZoneId)}T${endTime}`,
    };
    const startInstant = wallClockToUtcIso(next.start, timeZoneId);
    const endInstant = wallClockToUtcIso(next.end, timeZoneId);
    if (!startInstant || !endInstant) {
      setError(`Choose valid dates and times in ${timeZoneId}.`);
      return;
    }
    if (Date.parse(endInstant) <= Date.parse(startInstant)) {
      setError(`${endLabel} must be after ${startLabel.toLowerCase()}.`);
      return;
    }
    if (!controlled) setInternalValue(next);
    onValueChange?.(next);
    setOpen(false);
  }

  function renderTime(endpoint: "start" | "end", endpointLabel: string, date?: Date) {
    return (
      <div className="grid min-w-0 gap-1">
        <label htmlFor={`${id}-${endpoint}-time`} className="text-xs font-medium">{endpointLabel}</label>
        <span className="text-xs text-muted-foreground tabular-nums">
          {date ? displayDate(calendarDatePart(date, timeZoneId)) : "Choose a date"}
        </span>
        <Input
          id={`${id}-${endpoint}-time`} aria-label={`${endpointLabel} time`}
          type="text" inputMode="numeric" autoComplete="off" placeholder="HH:MM" maxLength={5}
          pattern="(?:[01][0-9]|2[0-3]):[0-5][0-9]" className="w-full min-w-0 tabular-nums"
          value={endpoint === "start" ? startTime : endTime}
          onChange={(event) => {
            event.stopPropagation();
            (endpoint === "start" ? setStartTime : setEndTime)(event.target.value);
            setError(null);
          }}
        />
      </div>
    );
  }

  const hasValue = Boolean(current.start && current.end);
  return (
    <div ref={rootRef} data-slot="date-time-range-picker" role="group"
      aria-label={`${label}, ${timeZoneId}, 24-hour clock`} className={cn("w-full min-w-0", className)}>
      {(["start", "end"] as const).map((endpoint) => (
        <input key={endpoint} type="text" name={endpoint === "start" ? startName : endName}
          value={current[endpoint]} onChange={() => undefined} required={required} disabled={disabled}
          tabIndex={-1} aria-label={`${label} ${endpoint} value`} className="sr-only"
          onInvalid={(event) => {
            event.preventDefault();
            handleOpenChange(true);
            triggerRef.current?.focus();
          }}
        />
      ))}
      <Popover open={open} onOpenChange={handleOpenChange}>
        <PopoverTrigger render={
          <Button ref={triggerRef} id={id} type="button" variant="outline" disabled={disabled}
            aria-label={label} aria-required={required}
            className={cn("h-auto min-h-10 w-full justify-start gap-2 px-3 py-2 text-left font-normal", !hasValue && "text-muted-foreground")}>
            <CalendarDays className="size-4 shrink-0" aria-hidden="true" />
            <span className="min-w-0 flex-1 whitespace-normal text-xs leading-relaxed tabular-nums">
              {hasValue ? displayRange(current) : `Choose ${label.toLowerCase()}`}
            </span>
          </Button>
        } />
        <PopoverContent align="start" aria-label={`${label} dates and times`}
          className="max-h-[calc(100dvh-2rem)] w-auto max-w-[calc(100vw-2rem)] gap-0 overflow-y-auto p-0">
          <Calendar mode="range" selected={draftRange} defaultMonth={draftRange?.from}
            onSelect={(next, date) => {
              // Start a fresh interval instead of extending the prefilled dates.
              setDraftRange(selectingEnd ? next : { from: date, to: date });
              setSelectingEnd(!selectingEnd);
              setError(null);
            }}
            numberOfMonths={monthCount} timeZone={timeZoneId} />
          <div className="space-y-3 border-t p-3">
            <div className="grid grid-cols-2 gap-3">
              {renderTime("start", startLabel, draftRange?.from)}
              {renderTime("end", endLabel, draftRange?.to)}
            </div>
            <p className="text-xs text-muted-foreground">{timeZoneId} · 24-hour clock</p>
            {error ? <p role="alert" className="text-xs text-destructive">{error}</p> : null}
            <div className="flex justify-end gap-2">
              <Button type="button" variant="ghost" size="sm" onClick={() => setOpen(false)}>Cancel</Button>
              <Button type="button" size="sm" disabled={!draftRange?.from || !draftRange.to}
                onClick={applyRange} aria-label={`Apply ${label.toLowerCase()}`}>Apply</Button>
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  );
}
