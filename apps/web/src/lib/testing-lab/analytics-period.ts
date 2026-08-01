const DAY_IN_MS = 24 * 60 * 60 * 1000;
const PRESET_DAYS = new Set([7, 30, 90]);

export interface TestingLabAnalyticsPeriodParams {
  range?: string;
  from?: string;
  to?: string;
}

export interface TestingLabAnalyticsPeriod {
  range: "7" | "30" | "90" | "custom";
  fromInput: string;
  toInput: string;
  fromDate: string;
  toDate: string;
}

function startOfUtcDay(value: Date): Date {
  return new Date(
    Date.UTC(value.getUTCFullYear(), value.getUTCMonth(), value.getUTCDate()),
  );
}

function parseDateInput(value?: string): Date | null {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const parsed = new Date(`${value}T00:00:00.000Z`);
  return Number.isNaN(parsed.getTime()) ||
    parsed.toISOString().slice(0, 10) !== value
    ? null
    : parsed;
}

function toDateInput(value: Date): string {
  return value.toISOString().slice(0, 10);
}

export function resolveTestingLabAnalyticsPeriod(
  params: TestingLabAnalyticsPeriodParams,
  now: Date = new Date(),
): TestingLabAnalyticsPeriod {
  const customFrom = parseDateInput(params.from);
  const customTo = parseDateInput(params.to);

  if (customFrom && customTo && customFrom <= customTo) {
    const exclusiveTo = new Date(customTo.getTime() + DAY_IN_MS);
    return {
      range: "custom",
      fromInput: toDateInput(customFrom),
      toInput: toDateInput(customTo),
      fromDate: customFrom.toISOString(),
      toDate: exclusiveTo.toISOString(),
    };
  }

  const requestedDays = Number(params.range);
  const days = PRESET_DAYS.has(requestedDays) ? requestedDays : 30;
  const exclusiveTo = new Date(startOfUtcDay(now).getTime() + DAY_IN_MS);
  const from = new Date(exclusiveTo.getTime() - days * DAY_IN_MS);
  const inclusiveTo = new Date(exclusiveTo.getTime() - DAY_IN_MS);

  return {
    range: String(days) as "7" | "30" | "90",
    fromInput: toDateInput(from),
    toInput: toDateInput(inclusiveTo),
    fromDate: from.toISOString(),
    toDate: exclusiveTo.toISOString(),
  };
}
