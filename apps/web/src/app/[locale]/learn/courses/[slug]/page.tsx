import { auth } from '@/auth';
import { CourseAccessGate } from '@/components/learning/course-access-gate';
import { getCourseAccessData } from '@/lib/learner/courses';
import { getCourseLearnerContext } from '@/lib/learner/records';
import { createLearnerRoutes } from '@/lib/learner/routes';
import { getCourseGroupSetViews } from '@/lib/learning';
import { CourseLearnerOverview } from '@game-guild/courses/components/learner';
import { notFound } from 'next/navigation';
import { LearnCourseGroups } from './groups-section';

export default async function CourseOverviewPage({ params }: { params: Promise<{ locale: string; slug: string }> }) {
  const { locale, slug } = await params;
  const [access, session] = await Promise.all([getCourseAccessData(slug), auth()]);

  if (access.kind === 'not-found') notFound();
  if (access.kind !== 'ready') return <CourseAccessGate access={access} />;

  const groupSets = await getCourseGroupSetViews(access.course.id);

  return (
    <div className="space-y-8">
      <CourseLearnerOverview course={access.course} context={await getCourseLearnerContext(access.course.id)} routes={createLearnerRoutes(locale)} />
      <LearnCourseGroups courseId={access.course.id} currentUserId={session?.user?.id ?? ''} sets={groupSets} />
    </div>
  );
}
