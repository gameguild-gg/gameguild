import type { CourseAttendanceData, CourseAttendanceItem } from '@/lib/courses';
import type { LearnerCourseContext } from '@/lib/learner-data';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { ArrowRight, CalendarClock, CheckCircle2, ClipboardList, MessageSquareText } from 'lucide-react';
import Link from 'next/link';

function formatDate(value?: string | null) {
    if (!value) return null;
    return new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
}

function contentKind(item: CourseAttendanceItem) {
    switch (item.contentType) {
        case 'Discussion': return 'Discussion';
        case 'Reflection': return 'Reflection';
        case 'Survey': return 'Survey';
        default: return 'Activity';
    }
}

export function LearnerActivities({ course, context }: { course: CourseAttendanceData; context: LearnerCourseContext }) {
    const contentActivities = course.modules.flatMap((module) => module.items).filter((item) =>
        item.contentType === 'Discussion' || item.contentType === 'Reflection' || item.contentType === 'Survey',
    );

    return (
        <div className="space-y-8">
            <header className="flex flex-col gap-4 border-b border-white/10 pb-6 sm:flex-row sm:items-end sm:justify-between">
                <div>
                    <p className="text-sm font-medium text-violet-300">{course.title}</p>
                    <h1 className="mt-2 text-3xl font-semibold text-white">Assignments and activities</h1>
                    <p className="mt-2 max-w-2xl text-sm text-slate-400">Complete graded work and course participation in one place. Attempts, files, responses, grades, and feedback are stored in your enrollment record.</p>
                </div>
                <Button asChild variant="outline"><Link href={`/courses/${course.slug}/content`}>Course content</Link></Button>
            </header>

            {context.assessments.length === 0 && contentActivities.length === 0 ? (
                <Card className="border-white/10 bg-white/[0.03]"><CardContent className="flex min-h-56 flex-col items-center justify-center text-center"><ClipboardList className="size-8 text-slate-500" /><h2 className="mt-4 text-lg font-semibold">No activities assigned</h2><p className="mt-2 text-sm text-slate-400">Your instructor has not published graded or participatory work yet.</p></CardContent></Card>
            ) : (
                <div className="grid gap-4">
                    {context.assessments.map((assessment) => {
                        const submission = context.submissions.find((candidate) => candidate.assessmentId === assessment.id);
                        const status = submission?.status ?? (assessment.isAvailable === false ? 'Not available' : 'Ready');
                        const title = assessment.title || 'Untitled assessment';
                        return (
                            <Card key={assessment.id} className="border-white/10 bg-white/[0.03] transition hover:border-white/20">
                                <CardContent className="flex flex-col gap-4 p-5 md:flex-row md:items-center">
                                    <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-violet-500/10 text-violet-300"><ClipboardList className="size-5" /></div>
                                    <div className="min-w-0 flex-1">
                                        <div className="flex flex-wrap items-center gap-2"><h2 className="font-semibold text-white">{title}</h2><Badge variant="outline">{assessment.type === 'Exam' ? 'Quiz' : assessment.type || 'Assessment'}</Badge><Badge className={submission?.status === 'Graded' ? 'bg-emerald-500/15 text-emerald-300' : 'bg-white/10 text-slate-300'}>{status}</Badge></div>
                                        <p className="mt-1 line-clamp-2 text-sm text-slate-400">{assessment.description || 'Review the instructions and submit using the configured response method.'}</p>
                                        <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-xs text-slate-400">
                                            {assessment.dueAt ? <span className="inline-flex items-center gap-1.5"><CalendarClock className="size-3.5" />Due {formatDate(assessment.dueAt)}</span> : <span>No deadline</span>}
                                            <span>{assessment.maxScore ?? 0} points</span>
                                            {submission?.score != null ? <strong className="font-medium text-emerald-300">{submission.score} / {assessment.maxScore ?? 0}</strong> : null}
                                        </div>
                                    </div>
                                    {assessment.id && assessment.isAvailable !== false ? (
                                        <Button asChild><Link href={`/courses/${course.slug}/activities/assessment-${assessment.id}`}>{submission?.status === 'Graded' ? 'Review grade' : submission ? 'View submission' : 'Start'}<ArrowRight className="size-4" /></Link></Button>
                                    ) : (
                                        <Button disabled>Unavailable</Button>
                                    )}
                                </CardContent>
                            </Card>
                        );
                    })}

                    {contentActivities.map((item) => (
                        <Card key={item.id} className="border-white/10 bg-white/[0.03] transition hover:border-white/20">
                            <CardContent className="flex flex-col gap-4 p-5 md:flex-row md:items-center">
                                <div className="flex size-10 shrink-0 items-center justify-center rounded-md bg-sky-500/10 text-sky-300"><MessageSquareText className="size-5" /></div>
                                <div className="min-w-0 flex-1"><div className="flex flex-wrap items-center gap-2"><h2 className="font-semibold text-white">{item.title}</h2><Badge variant="outline">{contentKind(item)}</Badge>{item.status === 'completed' ? <Badge className="bg-emerald-500/15 text-emerald-300"><CheckCircle2 className="mr-1 size-3" />Completed</Badge> : <Badge className="bg-white/10 text-slate-300">{item.status === 'locked' ? 'Locked' : 'Ready'}</Badge>}</div><p className="mt-1 text-sm text-slate-400">{item.description || 'Participate and preserve your response in the course record.'}</p></div>
                                {item.status === 'locked' ? (
                                    <Button variant="outline" disabled>Locked</Button>
                                ) : (
                                    <Button asChild variant="outline"><Link href={`/courses/${course.slug}/activities/content-${item.id}`}>{item.status === 'completed' ? 'Review' : 'Open'}<ArrowRight className="size-4" /></Link></Button>
                                )}
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}
        </div>
    );
}