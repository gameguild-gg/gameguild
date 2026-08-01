import { CourseAccessGate } from '@/components/learning/course-access-gate';
import { getCourseAccessData } from '@/lib/learner/courses';
import { getCourseLearnerContext } from '@/lib/learner/records';
import { LearnerGradebook } from '@game-guild/courses/components/learner';
import { notFound } from 'next/navigation';

export default async function CourseGradesPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const access = await getCourseAccessData(slug);

  if (access.kind === 'not-found') notFound();
  if (access.kind !== 'ready') return <CourseAccessGate access={access} />;

  return (
    <LearnerGradebook
      records={[
        {
          course: access.course,
          context: await getCourseLearnerContext(access.course.id),
        },
      ]}
    />
  );
}
