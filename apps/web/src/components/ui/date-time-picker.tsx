"use client"

import * as React from "react"
import { format } from "date-fns"
import { CalendarIcon, Clock3Icon } from "lucide-react"

import {
  calendarDatePart,
  formatWallClockInTimeZone,
  wallClockDate,
} from "@/lib/date-time-zone"
import { Button } from "@game-guild/ui/components/button"
import { Calendar } from "@game-guild/ui/components/calendar"
import { Input } from "@game-guild/ui/components/input"
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@game-guild/ui/components/popover"
import { cn } from "@game-guild/ui/lib/utils"

const dateTimePattern = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/

export interface DateTimePickerProps {
  id: string
  name: string
  value?: string
  defaultValue?: string
  onValueChange?: (value: string) => void
  required?: boolean
  disabled?: boolean
  placeholder?: string
  timeZoneId?: string
  timezoneLabel?: string
  displayFormat?: string
  minValue?: string
  maxValue?: string
  className?: string
  "aria-invalid"?: boolean | "true" | "false"
}

function parseDateTime(
  value: string | undefined,
  timeZoneId?: string,
): Date | undefined {
  if (timeZoneId) return wallClockDate(value ?? "", timeZoneId)

  const match = value?.match(dateTimePattern)
  if (!match) return undefined

  const [, year, month, day, hour, minute] = match
  const parsed = new Date(
    Number(year),
    Number(month) - 1,
    Number(day),
    Number(hour),
    Number(minute),
  )

  if (
    parsed.getFullYear() !== Number(year) ||
    parsed.getMonth() !== Number(month) - 1 ||
    parsed.getDate() !== Number(day) ||
    parsed.getHours() !== Number(hour) ||
    parsed.getMinutes() !== Number(minute)
  ) {
    return undefined
  }

  return parsed
}

function formatDateTime(value: Date, timeZoneId?: string): string {
  if (timeZoneId) return formatWallClockInTimeZone(value, timeZoneId)

  const pad = (part: number) => String(part).padStart(2, "0")
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`
}

function displayDateTime(
  value: Date,
  displayFormat: string,
  timeZoneId?: string,
) {
  if (!timeZoneId) return format(value, displayFormat)

  return new Intl.DateTimeFormat(undefined, {
    timeZone: timeZoneId,
    dateStyle: "medium",
    timeStyle: "short",
    hourCycle: "h23",
  }).format(value)
}

function boundedPart(value: string, maximum: number): number {
  const parsed = Number.parseInt(value, 10)
  if (!Number.isFinite(parsed)) return 0
  return Math.min(maximum, Math.max(0, parsed))
}

export function DateTimePicker({
  id,
  name,
  value,
  defaultValue = "",
  onValueChange,
  required = false,
  disabled = false,
  placeholder = "Choose date and time",
  timeZoneId,
  timezoneLabel = timeZoneId ?? "UTC",
  displayFormat = "PPP 'at' HH:mm",
  minValue,
  maxValue,
  className,
  "aria-invalid": ariaInvalid,
}: DateTimePickerProps) {
  const controlled = value !== undefined
  const rootRef = React.useRef<HTMLDivElement>(null)
  const triggerRef = React.useRef<HTMLButtonElement>(null)
  const [internalValue, setInternalValue] = React.useState(defaultValue)
  const committedValue = controlled ? value : internalValue
  const committedDate = parseDateTime(committedValue, timeZoneId)
  const minimumDate = parseDateTime(minValue, timeZoneId)
  const maximumDate = parseDateTime(maxValue, timeZoneId)
  const [open, setOpen] = React.useState(false)
  const [draftDate, setDraftDate] = React.useState<Date | undefined>(
    committedDate,
  )
  const committedWallClock = committedDate
    ? formatDateTime(committedDate, timeZoneId)
    : ""
  const [draftHour, setDraftHour] = React.useState(
    committedWallClock ? committedWallClock.slice(11, 13) : "00",
  )
  const [draftMinute, setDraftMinute] = React.useState(
    committedWallClock ? committedWallClock.slice(14, 16) : "00",
  )
  const draftValue = React.useMemo(() => {
    if (!draftDate) return ""
    const day = timeZoneId
      ? calendarDatePart(draftDate, timeZoneId)
      : formatDateTime(draftDate).slice(0, 10)
    if (!day) return ""
    const pad = (part: number) => String(part).padStart(2, "0")
    return `${day}T${pad(boundedPart(draftHour, 23))}:${pad(boundedPart(draftMinute, 59))}`
  }, [draftDate, draftHour, draftMinute, timeZoneId])
  const draftOutsideRange =
    (Boolean(minValue) && draftValue < minValue!) ||
    (Boolean(maxValue) && draftValue > maxValue!)
  const draftIsInvalidInTimeZone = Boolean(
    timeZoneId && draftValue && !wallClockDate(draftValue, timeZoneId),
  )

  const rangeDescription = React.useMemo(() => {
    if (minimumDate && maximumDate) {
      return `Choose a date and time between ${displayDateTime(minimumDate, displayFormat, timeZoneId)} and ${displayDateTime(maximumDate, displayFormat, timeZoneId)} (${timezoneLabel}).`
    }
    if (minimumDate) {
      return `Choose a date and time on or after ${displayDateTime(minimumDate, displayFormat, timeZoneId)} (${timezoneLabel}).`
    }
    if (maximumDate) {
      return `Choose a date and time on or before ${displayDateTime(maximumDate, displayFormat, timeZoneId)} (${timezoneLabel}).`
    }
    return ""
  }, [displayFormat, maximumDate, minimumDate, timeZoneId, timezoneLabel])

  const commit = React.useCallback(
    (nextValue: string) => {
      if (!controlled) setInternalValue(nextValue)
      onValueChange?.(nextValue)
    },
    [controlled, onValueChange],
  )

  React.useEffect(() => {
    const form = rootRef.current?.closest("form")
    if (!form || controlled) return

    const handleReset = () => setInternalValue(defaultValue)
    form.addEventListener("reset", handleReset)
    return () => form.removeEventListener("reset", handleReset)
  }, [controlled, defaultValue])

  const resetDraft = React.useCallback(() => {
    const current = parseDateTime(committedValue, timeZoneId) ?? new Date()
    current.setSeconds(0, 0)
    setDraftDate(current)
    const wallClock = formatDateTime(current, timeZoneId)
    setDraftHour(wallClock.slice(11, 13))
    setDraftMinute(wallClock.slice(14, 16))
  }, [committedValue, setDraftDate, setDraftHour, setDraftMinute, timeZoneId])

  const handleOpenChange = (nextOpen: boolean) => {
    if (nextOpen) resetDraft()
    setOpen(nextOpen)
  }

  const applyDraft = () => {
    if (!draftDate || draftOutsideRange || draftIsInvalidInTimeZone) return
    commit(draftValue)
    setOpen(false)
  }

  const clearValue = () => {
    commit("")
    setOpen(false)
  }

  return (
    <div
      ref={rootRef}
      className={cn("w-full", className)}
      data-slot="date-time-picker"
    >
      <input
        type="text"
        name={name}
        value={committedValue}
        onChange={() => undefined}
        required={required}
        disabled={disabled}
        tabIndex={-1}
        aria-label="Selected date and time value"
        className="sr-only"
        onInvalid={(event) => {
          event.preventDefault()
          setOpen(true)
          triggerRef.current?.focus()
        }}
        data-slot="date-time-picker-value"
      />
      <Popover open={open} onOpenChange={handleOpenChange}>
        <PopoverTrigger
          render={
            <Button
              ref={triggerRef}
              id={id}
              type="button"
              variant="outline"
              disabled={disabled}
              aria-required={required}
              aria-invalid={ariaInvalid}
              className={cn(
                "w-full justify-start overflow-hidden text-left font-normal",
                !committedDate && "text-muted-foreground",
              )}
            />
          }
        >
          <CalendarIcon aria-hidden="true" />
          <span className="min-w-0 flex-1 truncate">
            {committedDate
              ? displayDateTime(committedDate, displayFormat, timeZoneId)
              : placeholder}
          </span>
          <span className="shrink-0 text-xs text-muted-foreground">
            {timezoneLabel}
          </span>
        </PopoverTrigger>
        <PopoverContent align="start" className="w-auto p-0">
          <Calendar
            mode="single"
            selected={draftDate}
            defaultMonth={draftDate ?? minimumDate}
            timeZone={timeZoneId}
            disabled={(date) => {
              if (timeZoneId) {
                const day = calendarDatePart(date, timeZoneId)
                return Boolean(
                  (minValue && day < minValue.slice(0, 10)) ||
                    (maxValue && day > maxValue.slice(0, 10)),
                )
              }

              const day = new Date(date.getFullYear(), date.getMonth(), date.getDate())
              const minimumDay = minimumDate
                ? new Date(minimumDate.getFullYear(), minimumDate.getMonth(), minimumDate.getDate())
                : null
              const maximumDay = maximumDate
                ? new Date(maximumDate.getFullYear(), maximumDate.getMonth(), maximumDate.getDate())
                : null
              return Boolean(
                (minimumDay && day < minimumDay) ||
                  (maximumDay && day > maximumDay),
              )
            }}
            onSelect={(selected) => {
              if (!selected) return
              const day = timeZoneId
                ? calendarDatePart(selected, timeZoneId)
                : formatDateTime(selected).slice(0, 10)
              const pad = (part: number) => String(part).padStart(2, "0")
              const nextValue = `${day}T${pad(boundedPart(draftHour, 23))}:${pad(boundedPart(draftMinute, 59))}`
              setDraftDate(parseDateTime(nextValue, timeZoneId) ?? selected)
            }}
          />
          <div className="border-t p-3">
            <div className="mb-3 flex items-end gap-2">
              <Clock3Icon
                className="mb-2 size-4 text-muted-foreground"
                aria-hidden="true"
              />
              <div className="grid gap-1">
                <label className="text-xs font-medium" htmlFor={`${id}-hour`}>
                  Hour
                </label>
                <Input
                  id={`${id}-hour`}
                  type="number"
                  inputMode="numeric"
                  min={0}
                  max={23}
                  value={draftHour}
                  onChange={(event) => setDraftHour(event.target.value)}
                  className="w-20"
                />
              </div>
              <span className="mb-2" aria-hidden="true">
                :
              </span>
              <div className="grid gap-1">
                <label className="text-xs font-medium" htmlFor={`${id}-minute`}>
                  Minute
                </label>
                <Input
                  id={`${id}-minute`}
                  type="number"
                  inputMode="numeric"
                  min={0}
                  max={59}
                  value={draftMinute}
                  onChange={(event) => setDraftMinute(event.target.value)}
                  className="w-20"
                />
              </div>
              <span className="mb-2 text-xs text-muted-foreground">
                {timezoneLabel}
              </span>
            </div>
            {draftOutsideRange ? (
              <p role="alert" className="mb-3 max-w-xs text-xs text-destructive">
                {rangeDescription}
              </p>
            ) : null}
            {draftIsInvalidInTimeZone ? (
              <p role="alert" className="mb-3 max-w-xs text-xs text-destructive">
                That time does not exist in {timeZoneId} because of a daylight-saving change. Choose another time.
              </p>
            ) : null}
            <div className="flex items-center justify-between gap-2">
              <div>
                {!required ? (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={clearValue}
                    aria-label="Clear date and time"
                  >
                    Clear
                  </Button>
                ) : null}
              </div>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    resetDraft()
                    setOpen(false)
                  }}
                  aria-label="Cancel date and time changes"
                >
                  Cancel
                </Button>
                <Button
                  type="button"
                  size="sm"
                  onClick={applyDraft}
                  disabled={!draftDate || draftOutsideRange || draftIsInvalidInTimeZone}
                  aria-label="Apply date and time"
                >
                  Apply
                </Button>
              </div>
            </div>
          </div>
        </PopoverContent>
      </Popover>
    </div>
  )
}

export {
  formatDateTime as formatDateTimePickerValue,
  parseDateTime as parseDateTimePickerValue,
}
