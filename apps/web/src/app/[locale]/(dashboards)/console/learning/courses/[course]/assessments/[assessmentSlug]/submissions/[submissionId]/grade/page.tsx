import { notFound } from 'next/navigation';
import { redirect } from '@/i18n/navigation';
import { getAssessment } from '@/lib/learning';

/** Redirects the former grading page to the runtime-backed SpeedGrader. */
export default async function GradeSubmissionPage({
  params,
}: {
  params: Promise<{
    locale: string;
    course: string;
    assessmentSlug: string;
    submissionId: string;
  }>;
}): Promise<void> {
  const { locale, course, assessmentSlug, submissionId } = await params;
  const assessment = await getAssessment(course, assessmentSlug);
  if (!assessment) notFound();

  redirect({
    href: `/speedgrader/assessments/${assessment.id}?course=${encodeURIComponent(course)}&submission=${encodeURIComponent(submissionId)}`,
    locale,
  });
}
