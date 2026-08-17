import { getCohort } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Progress } from '@game-guild/ui/components/progress';
import { Users } from 'lucide-react';
import { notFound } from 'next/navigation';

export default async function CohortStudentsPage({ params }: { params: Promise<{ classId: string }> }) {
  const { classId } = await params;
  const cohort = await getCohort(classId);
  if (!cohort) notFound();

  if (cohort.attendees.length === 0) {
    return (
      <div className="rounded-lg border border-dashed px-6 py-16 text-center">
        <Users className="mx-auto size-8 text-muted-foreground" />
        <h3 className="mt-4 font-medium">No students enrolled</h3>
      </div>
    );
  }

  return (
    <div className="grid gap-3">
      {cohort.attendees.map((attendee) => (
        <Card key={attendee.id} className="rounded-lg py-4 shadow-none">
          <CardContent className="flex flex-col gap-3 px-4 sm:flex-row sm:items-center">
            <div className="min-w-0 flex-1"><p className="truncate font-medium">{attendee.userId}</p><p className="text-xs text-muted-foreground">Enrolled {attendee.enrolledAt ? new Date(attendee.enrolledAt).toLocaleDateString('en-US') : 'date unavailable'}</p></div>
            <div className="w-full sm:w-48"><Progress value={attendee.progress} /><p className="mt-1 text-xs text-muted-foreground">{attendee.progress}% complete</p></div>
            <Badge variant="outline" className="w-fit capitalize">{attendee.status}</Badge>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
