export type PeriodType = 'day' | 'week' | 'month' | 'year';

export const PERIOD_OPTIONS: ReadonlyArray<{ value: PeriodType; label: string }> = [
    { value: 'day', label: 'Day' },
    { value: 'week', label: 'Week' },
    { value: 'month', label: 'Month' },
    { value: 'year', label: 'Year' },
];