import { redirect } from '@/i18n/navigation';
import { getAssessment, getContentItem } from '@/lib/learning';
import { notFound } from 'next/navigation';

export interface CodingAuthoringRouteProps {
  readonly params: Promise<{ locale: string; course: string; assessmentSlug: string }>;
}

export default async function codingAuthoringRoute(props: CodingAuthoringRouteProps): Promise<never> {
  const { locale, course, assessmentSlug } = await props.params;
  const assessment = await getAssessment(course, assessmentSlug);
  if (!assessment?.contentId) notFound();
  const content = await getContentItem(assessment.courseId, assessment.contentId);
  if (!content || content.type !== 'Code') notFound();
  return redirect({
    href: `/workspace/learning/courses/${encodeURIComponent(course)}/content/${encodeURIComponent(content.slug || content.id)}`,
    locale,
  });
}
