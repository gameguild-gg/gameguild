"use client";

import { Input } from "@game-guild/ui/components/input";
import { cn } from "@game-guild/ui/lib/utils";
import * as React from "react";

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
}

function wallClockParts(value: string) {
  const [date = "", time = ""] = value.split("T", 2);
  return { date, time };
}

export function DateTimeRangePicker({
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
      <fieldset key={field} className="grid min-w-0 gap-1.5">
        <legend className="text-sm font-medium">{endpointLabel}</legend>
        <div className="grid min-w-0 grid-cols-[minmax(0,1fr)_7rem] gap-2">
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
              onChange={(event) =>
                updateValue(field, "date", event.target.value)
              }
              required={required}
              disabled={disabled}
              aria-label={`${endpointLabel} date`}
              className="min-w-0 tabular-nums"
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
              className="min-w-0 tabular-nums"
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
      className={cn("grid w-full gap-3", className)}
    >
      <input type="hidden" name={startName} value={displayed.start} />
      <input type="hidden" name={endName} value={displayed.end} />
      {renderEndpoint("start", startLabel)}
      {renderEndpoint("end", endLabel)}
    </div>
  );
}
