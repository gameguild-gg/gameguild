import { getCourseAssessments } from '@/lib/learning/queries/assessments';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent } from '@game-guild/ui/components/card';

export default async function CohortAssessmentsPage({ params }: { params: Promise<{ course: string }> }) {
  const { course } = await params;
  const collection = await getCourseAssessments(course);

  return (
    <div className="grid gap-3">
      {collection.assessments.map((assessment) => (
        <Card key={assessment.id} className="rounded-lg py-4 shadow-none">
          <CardContent className="flex items-center justify-between gap-4 px-4">
            <div className="min-w-0"><p className="truncate font-medium">{assessment.title}</p><p className="text-sm text-muted-foreground">{assessment.type}</p></div>
            <Badge variant="outline">{assessment.maxScore} pts</Badge>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
