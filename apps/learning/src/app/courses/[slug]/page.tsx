import { auth } from '@/auth';
import { getCourseAttendanceData } from '@/lib/courses';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowRight, Clock3, Layers3 } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { notFound, redirect } from 'next/navigation';

export default async function CourseOverviewPage({ params }: { params: Promise<{ slug: string }> }) {
    const { slug } = await params;
    const session = await auth();

    if (!session) {
        redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}`)}`);
    }

    const course = await getCourseAttendanceData(slug, { includeProgress: true });

    if (!course) {
        notFound();
    }

    return (
        <main className="mx-auto flex min-h-screen max-w-5xl flex-col gap-8 px-4 py-10 lg:px-6">
            <section className="grid gap-6 lg:grid-cols-[1.2fr_0.8fr]">
                <Card className="border-slate-800 bg-slate-900/80 text-slate-100 shadow-2xl shadow-slate-950/40">
                    <CardHeader className="space-y-4">
                        <div className="flex flex-wrap gap-2">
                            <Badge className="border border-sky-500/40 bg-sky-500/10 text-sky-200">Learning overview</Badge>
                            <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{course.totalItems} items</Badge>
                            <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{course.modules.length} modules</Badge>
                        </div>
                        <CardTitle className="text-3xl">{course.title}</CardTitle>
                        <p className="text-sm text-slate-300">{course.description || 'Attend this course from the dedicated learning application.'}</p>
                    </CardHeader>
                    <CardContent className="space-y-6">
                        <div className="grid gap-4 sm:grid-cols-3">
                            <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <p className="text-sm text-slate-400">Progress</p>
                                <p className="mt-2 text-2xl font-semibold text-white">{course.overallProgress}%</p>
                            </div>
                            <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <p className="text-sm text-slate-400">Modules</p>
                                <p className="mt-2 text-2xl font-semibold text-white">{course.modules.length}</p>
                            </div>
                            <div className="rounded-2xl border border-slate-800 bg-slate-950/60 p-4">
                                <p className="text-sm text-slate-400">Remaining</p>
                                <p className="mt-2 text-2xl font-semibold text-white">{Math.ceil(course.remainingMinutes / 60)}h</p>
                            </div>
                        </div>

                        <Button asChild className="bg-sky-600 text-white hover:bg-sky-500">
                            <Link href={`/courses/${course.slug}/content`}>
                                Open classroom
                                <ArrowRight className="ml-2 size-4" />
                            </Link>
                        </Button>
                    </CardContent>
                </Card>

                <Card className="border-slate-800 bg-slate-900/80 text-slate-100 shadow-xl shadow-slate-950/30">
                    <div className="relative aspect-video bg-slate-950">
                        {course.thumbnail ? <Image src={course.thumbnail} alt={course.title} fill className="object-cover" /> : null}
                    </div>
                    <CardContent className="space-y-4 p-6">
                        <div className="flex items-center gap-2 text-sm text-slate-300">
                            <Layers3 className="size-4 text-violet-300" />
                            {course.totalItems} learning items available
                        </div>
                        <div className="flex items-center gap-2 text-sm text-slate-300">
                            <Clock3 className="size-4 text-amber-300" />
                            {course.remainingMinutes} minutes remaining
                        </div>
                    </CardContent>
                </Card>
            </section>
        </main>
    );
}
