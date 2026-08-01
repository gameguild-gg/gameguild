import { CourseAccessGate } from '@/components/learning/course-access-gate';
import { CourseContentOutline } from '@/components/learning/course-content-outline';
import { getCourseAccessData } from '@/lib/learner/courses';
import { notFound } from 'next/navigation';

export default async function CourseContentPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const access = await getCourseAccessData(slug);

  if (access.kind === 'not-found') notFound();
  if (access.kind !== 'ready') return <CourseAccessGate access={access} />;

  return <CourseContentOutline course={access.course} />;
}
