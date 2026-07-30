import { auth } from '@/auth';
import { CourseAccessGate } from '@/components/course-access-gate';
import { CourseCommunity } from '@/components/course-community';
import { getCourseAccessData } from '@/lib/courses';
import { getCourseLearnerContext } from '@/lib/learner-data';
import { notFound, redirect } from 'next/navigation';

export default async function CourseCommunityPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = await params;
    if (!await auth()) redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}/community`)}`);
    const access = await getCourseAccessData(slug);
    if (access.kind === 'not-found') notFound();
    if (access.kind !== 'ready') return <CourseAccessGate access={access} />;
    const context = await getCourseLearnerContext(access.course.id);
    return <CourseCommunity courseId={access.course.id} courseSlug={slug} courseTitle={access.course.title} discussions={context.discussions} />;
}