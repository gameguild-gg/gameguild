import React from 'react';
import { Link } from '@/i18n/navigation';
import { ArrowLeft } from 'lucide-react';
import { getAssessment, getAssessmentSubmissions } from '@/lib/learning';
import { SubmissionsList } from './submissions-list';

/**
 * Instructor view: list of submissions for an assessment.
 *
 * Route: /[locale]/dashboard/learning/courses/[course]/assessments/[assessmentId]/submissions
 *
 * ponytail: simple list render; add server-side pagination when submission count exceeds 200
 */
export default async function AssessmentSubmissionsPage({
  params,
}: {
  params: Promise<{ locale: string; course: string; assessmentId: string }>;
}): Promise<React.JSX.Element> {
  const { course, assessmentId } = await params;

  const [assessment, submissions] = await Promise.all([
    getAssessment(assessmentId),
    getAssessmentSubmissions(assessmentId),
  ]);

  const maxScore = assessment?.maxScore ?? 0;
  const backHref = `/dashboard/learning/courses/${course}/assessments/${assessmentId}`;

  return (
    <div className="flex flex-col gap-4 p-6">
      <div className="flex items-center gap-2">
        <Link
          href={backHref}
          className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to assessment
        </Link>
      </div>
      <header>
        <h1 className="text-2xl font-semibold">Submissions</h1>
        {assessment ? (
          <p className="text-sm text-muted-foreground">{assessment.title}</p>
        ) : null}
      </header>
      <SubmissionsList
        courseSlug={course}
        assessmentId={assessmentId}
        maxScore={maxScore}
        submissions={submissions}
      />
    </div>
  );
}
