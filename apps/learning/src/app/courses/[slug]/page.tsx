import { auth } from '@/auth';
import { CourseAccessGate } from '@/components/course-access-gate';
import { CourseLearnerOverview } from '@/components/course-learner-overview';
import { getCourseAccessData } from '@/lib/courses';
import { getCourseLearnerContext } from '@/lib/learner-data';
import { notFound, redirect } from 'next/navigation';

export default async function CourseOverviewPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = await params;
    const session = await auth();
    if (!session) redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}`)}`);
    const access = await getCourseAccessData(slug);
    if (access.kind === 'not-found') notFound();
    if (access.kind !== 'ready') return <CourseAccessGate access={access} />;
    const context = await getCourseLearnerContext(access.course.id);
    return <CourseLearnerOverview course={access.course} context={context} />;
}