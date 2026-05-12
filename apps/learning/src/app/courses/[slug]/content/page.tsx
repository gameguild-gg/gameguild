import { CourseAttendanceShell } from '@/components/course-attendance-shell';
import { auth } from '@/auth';
import { getCourseAttendanceData } from '@/lib/courses';
import { redirect } from 'next/navigation';
import { notFound } from 'next/navigation';

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
