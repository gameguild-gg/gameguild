import { auth } from '@/auth';
import { CourseAccessGate } from '@/components/course-access-gate';
import { CourseAttendanceShell } from '@/components/course-attendance-shell';
import { getCourseAccessData } from '@/lib/courses';
import { notFound, redirect } from 'next/navigation';

export default async function CourseContentPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = await params;
    const session = await auth();

    if (!session) {
        redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}/content`)}`);
    }

    const access = await getCourseAccessData(slug);

    if (access.kind === 'not-found') {
        notFound();
    }

    if (access.kind !== 'ready') {
        return <CourseAccessGate access={access} />;
    }

    return <CourseAttendanceShell course={access.course} />;
}
