import { auth } from '@/auth';
import { CourseAttendanceShell } from '@/components/course-attendance-shell';
import { getCourseAttendanceData } from '@/lib/courses';
import { notFound, redirect } from 'next/navigation';

export default async function CourseContentPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = await params;
    const session = await auth();

    if (!session) {
        redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}/content`)}`);
    }

    const course = await getCourseAttendanceData(slug, { includeProgress: true });

    if (!course) {
        notFound();
    }

    return <CourseAttendanceShell course={course} />;
}
