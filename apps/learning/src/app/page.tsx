import { auth } from '@/auth';
import { CourseCatalog } from '@/components/course-catalog';
import { LearnerDashboard } from '@/components/learner-dashboard';
import { StudentShell } from '@/components/student-shell';
import { getMyLearningCourses, getPublicCourses } from '@/lib/courses';
import { Button } from '@game-guild/ui/components/button';
import { GraduationCap } from 'lucide-react';
import Link from 'next/link';

export default async function LearningHomePage() {
    const session = await auth();
    if (session?.user) {
        const courses = await getMyLearningCourses();
        const name = session.user.name?.trim() || session.user.email?.split('@')[0] || 'learner';
        return <StudentShell user={{ id: session.user.id, name, email: session.user.email || '', image: session.user.image }}><LearnerDashboard learnerName={name.split(' ')[0] || name} courses={courses} /></StudentShell>;
    }

    const courses = await getPublicCourses();
    return <main className="min-h-screen bg-[#0c0d10] text-slate-100"><header className="border-b border-white/10"><div className="mx-auto flex h-16 max-w-7xl items-center px-4 lg:px-6"><Link href="/" className="flex items-center gap-2 font-semibold"><span className="flex size-8 items-center justify-center rounded-md bg-violet-600"><GraduationCap className="size-4" /></span>Game Guild Learning</Link><div className="ml-auto flex gap-2"><Button asChild variant="ghost"><Link href="/sign-in">Sign in</Link></Button><Button asChild><Link href="/sign-up">Join Game Guild</Link></Button></div></div></header><div className="mx-auto max-w-7xl space-y-12 px-4 py-10 lg:px-6"><section className="border-b border-white/10 pb-10"><p className="text-sm text-violet-300">Learn with the community</p><h1 className="mt-3 max-w-3xl text-4xl font-semibold sm:text-5xl">Courses built around practice, feedback, and real projects.</h1><p className="mt-4 max-w-2xl text-slate-400">Sign in to access your classes, schedules, assessments, grades, discussions, and certificates.</p></section><CourseCatalog courses={courses} /></div></main>;
}