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
      className={end
        ? 'grid min-w-0 grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-start gap-x-2 text-sm'
        : 'grid min-w-0 grid-cols-1 gap-y-1 text-sm'}
    >
      <time
        dateTime={start.toISOString()}
        className={`grid min-w-0 gap-y-1 font-medium tabular-nums ${end ? 'text-right' : 'text-left'}`}
      >
        <span className="block text-xs font-normal text-muted-foreground">{startDate}</span>
        <span className="block">{timeFormatter.format(start)}</span>
      </time>
      {end ? <span className="pt-0.5 text-center text-xs text-muted-foreground">to</span> : null}
      {end ? (
        <time dateTime={end.toISOString()} className="grid min-w-0 gap-y-1 text-left font-medium tabular-nums">
          <span className="block text-xs font-normal text-muted-foreground">{endDate}</span>
          <span className="block">{timeFormatter.format(end)}</span>
        </time>
      ) : null}
      <p className={`${end ? 'col-span-3 text-center' : 'text-left'} text-xs text-muted-foreground`}>{zoneLabel}</p>
    </div>
  );
}
