import { CourseAccessGate } from '@/components/learning/course-access-gate';
import { CourseCommunity } from '@/components/learning/course-community';
import { getCourseAccessData } from '@/lib/learner/courses';
import { getCourseLearnerContext } from '@/lib/learner/records';
import { notFound } from 'next/navigation';

export default async function CourseCommunityPage({
  params,
}: {
  params: Promise<{ slug: string }>;
}) {
  const { slug } = await params;
  const access = await getCourseAccessData(slug);

  if (access.kind === 'not-found') notFound();
  if (access.kind !== 'ready') return <CourseAccessGate access={access} />;

  const context = await getCourseLearnerContext(access.course.id);
  return (
    <CourseCommunity
      courseId={access.course.id}
      courseSlug={slug}
      courseTitle={access.course.title}
      discussions={context.discussions}
    />
  );
}
