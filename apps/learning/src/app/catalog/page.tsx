import { auth } from '@/auth';
import { CourseCatalog } from '@/components/course-catalog';
import { StudentShell } from '@/components/student-shell';
import { getPublicCourses } from '@/lib/courses';

export default async function CatalogPage() {
    const [session, courses] = await Promise.all([auth(), getPublicCourses()]);
    if (!session?.user) return <main className="mx-auto min-h-screen max-w-7xl px-4 py-10 text-slate-100 lg:px-6"><CourseCatalog courses={courses} /></main>;
    const name = session.user.name?.trim() || session.user.email?.split('@')[0] || 'Learner';
    return <StudentShell user={{ id: session.user.id, name, email: session.user.email || '', image: session.user.image }}><CourseCatalog courses={courses} /></StudentShell>;
}