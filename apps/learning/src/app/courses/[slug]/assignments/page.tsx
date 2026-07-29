import { auth } from '@/auth';
import { CourseAccessGate } from '@/components/course-access-gate';
import { LearnerActivities } from '@/components/learner-activities';
import { getCourseAccessData } from '@/lib/courses';
import { getCourseLearnerContext } from '@/lib/learner-data';
import { notFound, redirect } from 'next/navigation';

export default async function CourseAssignmentsPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = await params;
    if (!await auth()) redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}/assignments`)}`);
    const access = await getCourseAccessData(slug);
    if (access.kind === 'not-found') notFound();
    if (access.kind !== 'ready') return <CourseAccessGate access={access} />;
    const context = await getCourseLearnerContext(access.course.id);
    return <LearnerActivities course={access.course} context={context} />;
}