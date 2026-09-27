type TestingEventDateRangeProps = {
  startsAt?: string | null;
  endsAt?: string | null;
  timeZone?: string;
  locale?: string;
  hour12?: boolean;
};

function safeDate(value?: string | null) {
  if (!value) return null;
  const date = new Date(value);
  return Number.isNaN(date.valueOf()) ? null : date;
}

function safeTimeZone(value: string) {
  try {
    new Intl.DateTimeFormat('en-US', { timeZone: value });
    return value;
  } catch {
    return 'UTC';
  }
}

function safeLocale(value: string) {
  try {
    new Intl.DateTimeFormat(value);
    return value;
  } catch {
    return 'en-US';
  }
}

export function TestingEventDateRange({
  startsAt,
  endsAt,
  timeZone = 'UTC',
  locale = 'en-US',
  hour12 = true,
}: TestingEventDateRangeProps) {
  const start = safeDate(startsAt);
  const end = safeDate(endsAt);

  if (!start) return <p className="text-sm text-muted-foreground">Schedule pending</p>;

  const resolvedTimeZone = safeTimeZone(timeZone);
  const resolvedLocale = safeLocale(locale);
  const dateFormatter = new Intl.DateTimeFormat(resolvedLocale, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    timeZone: resolvedTimeZone,
  });
  const timeFormatter = new Intl.DateTimeFormat(resolvedLocale, {
    hour: 'numeric',
    minute: '2-digit',
    hour12,
    timeZone: resolvedTimeZone,
  });
  const startDate = dateFormatter.format(start);
  const endDate = end ? dateFormatter.format(end) : null;
  const sameDay = end !== null && startDate === endDate;
  const zoneLabel = new Intl.DateTimeFormat(resolvedLocale, {
    timeZone: resolvedTimeZone,
    timeZoneName: 'short',
  }).formatToParts(start).find((part) => part.type === 'timeZoneName')?.value ?? resolvedTimeZone;

  return (
    <div
      role="group"
      aria-label={end
        ? `${startDate}, ${timeFormatter.format(start)} to ${endDate}, ${timeFormatter.format(end)} ${zoneLabel}`
        : `${startDate}, ${timeFormatter.format(start)} ${zoneLabel}`}
      className="grid min-w-0 gap-y-1 text-sm"
    >
      <p className="text-xs leading-5 text-muted-foreground">
        {sameDay || !endDate ? startDate : `${startDate} → ${endDate}`}
      </p>
      <p className="font-medium tabular-nums">
        <time dateTime={start.toISOString()}>{timeFormatter.format(start)}</time>
        {end ? (
          <>
            <span aria-hidden="true" className="px-1.5 text-muted-foreground">–</span>
            <time dateTime={end.toISOString()}>{timeFormatter.format(end)}</time>
          </>
        ) : null}
      </p>
      <p className="text-xs leading-5 text-muted-foreground">{zoneLabel}</p>
    </div>
  );
}
