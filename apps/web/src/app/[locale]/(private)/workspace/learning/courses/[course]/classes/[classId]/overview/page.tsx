import { getCohort } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { AlertTriangle, CalendarClock, CalendarRange, Users } from 'lucide-react';
import { notFound } from 'next/navigation';

const dateFormatter = new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric', timeZone: 'UTC' });

export default async function CohortOverviewPage({ params }: { params: Promise<{ classId: string }> }) {
  const { classId } = await params;
  const cohort = await getCohort(classId);
  if (!cohort) notFound();

  const metrics = [
    { label: 'Students', value: `${cohort.enrollment.current}/${cohort.enrollment.capacity ?? 'Unlimited'}`, icon: Users },
    { label: 'Scheduled items', value: cohort.schedule?.itemCount ?? 0, icon: CalendarRange },
    { label: 'Conflicts', value: cohort.conflictCount, icon: AlertTriangle },
    { label: 'Next meeting', value: cohort.nextMeetingAt ? dateFormatter.format(new Date(cohort.nextMeetingAt)) : 'Not scheduled', icon: CalendarClock },
  ];

  return (
    <div className="space-y-5">
      <div className="grid grid-cols-2 gap-3 xl:grid-cols-4">
        {metrics.map((metric) => (
          <Card key={metric.label} className="gap-3 rounded-lg py-4 shadow-none">
            <CardContent className="px-4">
              <metric.icon className="size-4 text-muted-foreground" />
              <p className="mt-3 text-xl font-semibold tabular-nums">{metric.value}</p>
              <p className="text-sm text-muted-foreground">{metric.label}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="rounded-lg shadow-none">
        <CardHeader><CardTitle className="text-base">Class details</CardTitle></CardHeader>
        <CardContent className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <div><p className="text-xs text-muted-foreground">Period</p><p className="mt-1 text-sm font-medium">{dateFormatter.format(new Date(cohort.period.startsAt))} - {dateFormatter.format(new Date(cohort.period.endsAt))}</p></div>
          <div><p className="text-xs text-muted-foreground">Meeting pattern</p><p className="mt-1 text-sm font-medium">{cohort.meetingPattern ?? 'Not configured'}</p></div>
          <div><p className="text-xs text-muted-foreground">Timezone</p><p className="mt-1 text-sm font-medium">{cohort.schedule?.timezoneId ?? 'Not configured'}</p></div>
          <div><p className="text-xs text-muted-foreground">Enrollment</p><div className="mt-1"><Badge variant="outline">{cohort.isOpen ? 'Open' : 'Closed'}</Badge></div></div>
        </CardContent>
      </Card>
    </div>
  );
}
