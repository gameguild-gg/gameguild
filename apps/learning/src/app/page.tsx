import { getPublicCourses } from '@/lib/courses';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowRight, BookOpen, Star, Users } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';

export default async function LearningHomePage() {
    const courses = await getPublicCourses();
    const publicCatalogBaseUrl = process.env.NEXT_PUBLIC_WEB_URL || 'http://localhost:3000';
    const publicCatalogHref = `${publicCatalogBaseUrl.replace(/\/$/, '')}/courses#catalog`;

    return (
        <main className="mx-auto flex min-h-screen max-w-7xl flex-col gap-8 px-4 py-10 lg:px-6">
            <section className="rounded-3xl border border-slate-800 bg-slate-900/80 p-8 shadow-2xl shadow-slate-950/40">
                <Badge className="border border-sky-500/40 bg-sky-500/10 text-sky-200">Dedicated learner app</Badge>
                <h1 className="mt-4 text-4xl font-semibold text-white">Game Guild Learning</h1>
                <p className="mt-4 max-w-3xl text-lg text-slate-300">
                    This app is now the student-facing attendance surface. The public catalog stays in the web app, while course consumption moves here.
                </p>
            </section>

            <section className="space-y-4">
                <div className="flex items-center justify-between gap-4">
                    <div>
                        <h2 className="text-2xl font-semibold text-white">Available courses</h2>
                        <p className="text-sm text-slate-400">Backed by the live public course catalog.</p>
                    </div>
                    <Button asChild variant="outline" className="border-slate-700 bg-slate-950 text-slate-100 hover:bg-slate-900">
                        <Link href={publicCatalogHref}>Open public catalog</Link>
                    </Button>
                </div>

                <div className="grid gap-6 md:grid-cols-2 xl:grid-cols-3">
                    {courses.map((course) => (
                        <Card key={course.id} className="overflow-hidden border-slate-800 bg-slate-900/80 text-slate-100 shadow-xl shadow-slate-950/30">
                            <div className="relative aspect-video bg-slate-950">
                                {course.thumbnail ? (
                                    <Image src={course.thumbnail} alt={course.title} fill className="object-cover" />
                                ) : (
                                    <div className="flex h-full items-center justify-center text-slate-500">
                                        <BookOpen className="size-10" />
                                    </div>
                                )}
                            </div>
                            <CardHeader>
                                <CardTitle>{course.title}</CardTitle>
                                <p className="text-sm text-slate-300">{course.description || 'Live course data is ready for the learner app.'}</p>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="flex flex-wrap gap-2 text-xs text-slate-300">
                                    <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{course.category}</Badge>
                                    <Badge className="border border-slate-700 bg-slate-950 text-slate-200">{course.difficulty}</Badge>
                                </div>

                                <div className="flex flex-wrap gap-4 text-sm text-slate-400">
                                    <span className="flex items-center gap-1.5"><Users className="size-4 text-sky-300" />{course.currentEnrollments}</span>
                                    <span className="flex items-center gap-1.5"><Star className="size-4 text-amber-300" />{course.averageRating.toFixed(1)}</span>
                                </div>

                                <Button asChild className="w-full bg-sky-600 text-white hover:bg-sky-500">
                                    <Link href={`/courses/${course.slug}/content`}>
                                        Attend course
                                        <ArrowRight className="ml-2 size-4" />
                                    </Link>
                                </Button>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </section>
        </main>
    );
}
