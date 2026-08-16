import { getCohort } from '@/lib/learning';
import { Progress } from '@game-guild/ui/components/progress';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@game-guild/ui/components/table';
import { notFound } from 'next/navigation';

export default async function CohortGradebookPage({ params }: { params: Promise<{ classId: string }> }) {
  const { classId } = await params;
  const cohort = await getCohort(classId);
  if (!cohort) notFound();

  return (
    <div className="overflow-hidden rounded-lg border">
      <Table aria-label="Class gradebook">
        <TableHeader><TableRow><TableHead>Student</TableHead><TableHead>Status</TableHead><TableHead className="w-64">Course progress</TableHead></TableRow></TableHeader>
        <TableBody>
          {cohort.attendees.map((attendee) => (
            <TableRow key={attendee.id}><TableCell className="font-medium">{attendee.userId}</TableCell><TableCell className="capitalize">{attendee.status}</TableCell><TableCell><Progress value={attendee.progress} /><span className="mt-1 block text-xs text-muted-foreground">{attendee.progress}%</span></TableCell></TableRow>
          ))}
          {cohort.attendees.length === 0 ? <TableRow><TableCell colSpan={3} className="h-32 text-center text-muted-foreground">No enrolled students</TableCell></TableRow> : null}
        </TableBody>
      </Table>
    </div>
  );
}
