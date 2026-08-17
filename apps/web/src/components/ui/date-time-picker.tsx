"use client"

import * as React from "react"
import { format } from "date-fns"
import { CalendarIcon, Clock3Icon } from "lucide-react"

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
  timezoneLabel?: string
  className?: string
  "aria-invalid"?: boolean | "true" | "false"
}

function parseDateTime(value: string | undefined): Date | undefined {
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

function formatDateTime(value: Date): string {
  const pad = (part: number) => String(part).padStart(2, "0")
  return `${value.getFullYear()}-${pad(value.getMonth() + 1)}-${pad(value.getDate())}T${pad(value.getHours())}:${pad(value.getMinutes())}`
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
  timezoneLabel = "UTC",
  className,
  "aria-invalid": ariaInvalid,
}: DateTimePickerProps) {
  const controlled = value !== undefined
  const rootRef = React.useRef<HTMLDivElement>(null)
  const triggerRef = React.useRef<HTMLButtonElement>(null)
  const [internalValue, setInternalValue] = React.useState(defaultValue)
  const committedValue = controlled ? value : internalValue
  const committedDate = parseDateTime(committedValue)
  const [open, setOpen] = React.useState(false)
  const [draftDate, setDraftDate] = React.useState<Date | undefined>(
    committedDate,
  )
  const [draftHour, setDraftHour] = React.useState(
    committedDate ? String(committedDate.getHours()).padStart(2, "0") : "00",
  )
  const [draftMinute, setDraftMinute] = React.useState(
    committedDate ? String(committedDate.getMinutes()).padStart(2, "0") : "00",
  )

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
    const current = parseDateTime(committedValue) ?? new Date()
    current.setSeconds(0, 0)
    setDraftDate(current)
    setDraftHour(String(current.getHours()).padStart(2, "0"))
    setDraftMinute(String(current.getMinutes()).padStart(2, "0"))
  }, [committedValue])

  const handleOpenChange = (nextOpen: boolean) => {
    if (nextOpen) resetDraft()
    setOpen(nextOpen)
  }

  const applyDraft = () => {
    if (!draftDate) return
    const nextDate = new Date(draftDate)
    nextDate.setHours(
      boundedPart(draftHour, 23),
      boundedPart(draftMinute, 59),
      0,
      0,
    )
    commit(formatDateTime(nextDate))
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
        <PopoverTrigger asChild>
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
          >
            <CalendarIcon aria-hidden="true" />
            <span className="min-w-0 flex-1 truncate">
              {committedDate
                ? format(committedDate, "PPP 'at' HH:mm")
                : placeholder}
            </span>
            <span className="shrink-0 text-xs text-muted-foreground">
              {timezoneLabel}
            </span>
          </Button>
        </PopoverTrigger>
        <PopoverContent align="start" className="w-auto p-0">
          <Calendar
            mode="single"
            selected={draftDate}
            defaultMonth={draftDate}
            onSelect={(selected) => {
              if (!selected) return
              selected.setHours(
                boundedPart(draftHour, 23),
                boundedPart(draftMinute, 59),
                0,
                0,
              )
              setDraftDate(selected)
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
