import { getCohortSchedule } from '@/lib/learning';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { CalendarDays } from 'lucide-react';

export default async function CohortSchedulePage({
  params,
}: {
  params: Promise<{ course: string; classId: string }>;
}) {
  const { course, classId } = await params;
  const schedule = await getCohortSchedule(course, classId);

  return (
    <Card className="rounded-lg shadow-none">
      <CardContent className="py-12 text-center">
        <CalendarDays className="mx-auto size-8 text-muted-foreground" />
        <h3 className="mt-4 font-medium">{schedule ? `${schedule.items?.length ?? 0} scheduled items` : 'Schedule not configured'}</h3>
      </CardContent>
    </Card>
  );
}
