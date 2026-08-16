'use client';

import { Link } from '@/i18n/navigation';
import type { CourseCohortSummary } from '@/lib/learning/queries/cohorts';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Input } from '@game-guild/ui/components/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { AlertTriangle, CalendarDays, Clock3, Search, Users } from 'lucide-react';
import { useMemo, useState } from 'react';

import { NewClassSheet } from './new-class-sheet';

interface ClassControlCenterProps {
  courseId: string;
  cohorts: CourseCohortSummary[];
}

const dateFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' });

function statusLabel(status: CourseCohortSummary['status']) {
  return status.charAt(0).toUpperCase() + status.slice(1);
}

function CohortStatusBadge({ status }: { status: CourseCohortSummary['status'] }) {
  const className = status === 'active'
    ? 'border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300'
    : status === 'cancelled'
      ? 'border-destructive/30 bg-destructive/10 text-destructive'
      : status === 'completed'
        ? 'border-zinc-500/30 bg-zinc-500/10 text-zinc-600 dark:text-zinc-300'
        : 'border-sky-500/30 bg-sky-500/10 text-sky-700 dark:text-sky-300';

  return <Badge variant="outline" className={className}>{statusLabel(status)}</Badge>;
}

export function ClassControlCenter({ courseId, cohorts }: ClassControlCenterProps) {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('all');

  const visible = useMemo(() => {
    const term = search.trim().toLowerCase();
    return cohorts.filter((cohort) => {
      const matchesStatus = status === 'all' || cohort.status === status;
      const matchesSearch = !term || `${cohort.name} ${cohort.meetingPattern ?? ''}`.toLowerCase().includes(term);
      return matchesStatus && matchesSearch;
    });
  }, [cohorts, search, status]);

  const enrolled = cohorts.reduce((total, cohort) => total + cohort.enrollment.current, 0);
  const conflicts = cohorts.reduce((total, cohort) => total + cohort.conflictCount, 0);
  const active = cohorts.filter((cohort) => cohort.status === 'active').length;

  return (
    <div className="min-w-0 space-y-5">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <h2 className="text-xl font-semibold">Classes</h2>
          <p className="mt-1 text-sm text-muted-foreground">Each class has an independent period, calendar, release cadence, and roster.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button asChild variant="outline">
            <Link href={`/workspace/learning/courses/${courseId}/classes/calendar`}>
              <CalendarDays className="size-4" />
              General calendar
            </Link>
          </Button>
          <NewClassSheet courseId={courseId} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {[
          { label: 'Total classes', value: cohorts.length, icon: CalendarDays },
          { label: 'Active now', value: active, icon: Clock3 },
          { label: 'Enrollments', value: enrolled, icon: Users },
          { label: 'Schedule conflicts', value: conflicts, icon: AlertTriangle },
        ].map((metric) => (
          <Card key={metric.label} className="gap-2 rounded-lg py-4 shadow-none">
            <CardContent className="flex items-center justify-between px-4">
              <div>
                <p className="text-2xl font-semibold tabular-nums">{metric.value}</p>
                <p className="text-xs text-muted-foreground sm:text-sm">{metric.label}</p>
              </div>
              <metric.icon className="size-5 text-muted-foreground" />
            </CardContent>
          </Card>
        ))}
      </div>

      <div className="flex flex-col gap-3 rounded-lg border p-3 sm:flex-row sm:items-center">
        <div className="relative w-full sm:max-w-sm">
          <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search classes" className="pl-9" aria-label="Search classes" />
        </div>
        <Select value={status} onValueChange={setStatus}>
          <SelectTrigger className="w-full sm:w-44" aria-label="Filter class status"><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All statuses</SelectItem>
            <SelectItem value="scheduled">Scheduled</SelectItem>
            <SelectItem value="active">Active</SelectItem>
            <SelectItem value="completed">Completed</SelectItem>
            <SelectItem value="cancelled">Cancelled</SelectItem>
          </SelectContent>
        </Select>
        <p className="text-sm text-muted-foreground sm:ml-auto">{visible.length} of {cohorts.length}</p>
      </div>

      {visible.length === 0 ? (
        <div className="rounded-lg border border-dashed px-6 py-16 text-center">
          <CalendarDays className="mx-auto size-8 text-muted-foreground" />
          <h3 className="mt-4 font-medium">No classes found</h3>
          <p className="mt-1 text-sm text-muted-foreground">Create a class or adjust the current filters.</p>
        </div>
      ) : (
        <>
          <div className="hidden overflow-hidden rounded-lg border md:block">
            <Table aria-label="Course classes">
              <TableHeader>
                <TableRow>
                  <TableHead>Class</TableHead>
                  <TableHead>Period</TableHead>
                  <TableHead>Meeting pattern</TableHead>
                  <TableHead>Enrollment</TableHead>
                  <TableHead>Next meeting</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Conflicts</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visible.map((cohort) => (
                  <TableRow key={cohort.id}>
                    <TableCell className="max-w-72 whitespace-normal">
                      <Link href={`/workspace/learning/courses/${courseId}/classes/${cohort.id}/schedule`} className="font-medium hover:underline">
                        {cohort.name}
                      </Link>
                    </TableCell>
                    <TableCell>{dateFormatter.format(new Date(cohort.period.startsAt))} - {dateFormatter.format(new Date(cohort.period.endsAt))}</TableCell>
                    <TableCell>{cohort.meetingPattern ?? 'Not configured'}</TableCell>
                    <TableCell>{cohort.enrollment.current}/{cohort.enrollment.capacity ?? 'Unlimited'}</TableCell>
                    <TableCell>{cohort.nextMeetingAt ? dateFormatter.format(new Date(cohort.nextMeetingAt)) : 'Not scheduled'}</TableCell>
                    <TableCell><CohortStatusBadge status={cohort.status} /></TableCell>
                    <TableCell className="text-right tabular-nums">{cohort.conflictCount}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          <div className="grid gap-3 md:hidden">
            {visible.map((cohort) => (
              <Link key={cohort.id} href={`/workspace/learning/courses/${courseId}/classes/${cohort.id}/schedule`} className="rounded-lg border p-4 transition-colors hover:bg-muted/40">
                <div className="flex items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="truncate font-medium">{cohort.name}</p>
                    <p className="mt-1 text-sm text-muted-foreground">{cohort.meetingPattern ?? 'Schedule not configured'}</p>
                  </div>
                  <CohortStatusBadge status={cohort.status} />
                </div>
                <div className="mt-4 grid grid-cols-2 gap-3 text-sm">
                  <div><p className="text-xs text-muted-foreground">Period</p><p>{dateFormatter.format(new Date(cohort.period.startsAt))}</p></div>
                  <div><p className="text-xs text-muted-foreground">Enrollment</p><p>{cohort.enrollment.current}/{cohort.enrollment.capacity ?? 'Unlimited'}</p></div>
                </div>
              </Link>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
