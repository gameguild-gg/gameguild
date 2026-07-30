import { auth } from '@/auth';
import { CourseAccessGate } from '@/components/course-access-gate';
import { LearnerActivityForm, type LearnerActivityDescriptor } from '@/components/learner-activity-form';
import { getCourseAccessData } from '@/lib/courses';
import { getCourseLearnerContext, getMyProjects } from '@/lib/learner-data';
import { MarkdownRenderer } from '@game-guild/content-rendering';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, CalendarClock, ClipboardCheck } from 'lucide-react';
import Link from 'next/link';
import { notFound, redirect } from 'next/navigation';

function promptBody(value: unknown) {
    if (typeof value === 'string') return value;
    if (value && typeof value === 'object') return JSON.stringify(value, null, 2);
    return '';
}

export default async function LearnerActivityPage({ params }: { params: Promise<{ slug: string; activityId: string }> }) {
    const { slug, activityId } = await params;
    const session = await auth();
    if (!session) redirect(`/sign-in?redirectTo=${encodeURIComponent(`/courses/${slug}/activities/${activityId}`)}`);
    const access = await getCourseAccessData(slug);
    if (access.kind === 'not-found') notFound();
    if (access.kind !== 'ready') return <CourseAccessGate access={access} />;
    if (!access.course.enrollmentId) notFound();
    const context = await getCourseLearnerContext(access.course.id);

    let activity: LearnerActivityDescriptor | null = null;
    let description = '';
    let dueAt: string | null | undefined;
    let points: number | undefined;

    if (activityId.startsWith('assessment-')) {
        const assessmentId = activityId.slice('assessment-'.length);
        const assessment = context.assessments.find((candidate) => candidate.id === assessmentId);
        if (!assessment) notFound();
        const submission = context.submissions.find((candidate) => candidate.assessmentId === assessmentId);
        const projects = assessment.type === 'Project' && session?.user?.id ? await getMyProjects(session.user.id) : [];
        activity = { kind: 'assessment', assessment, submission, projects };
        description = assessment.description || '';
        dueAt = assessment.dueAt;
        points = assessment.maxScore;
    } else if (activityId.startsWith('content-')) {
        const contentId = activityId.slice('content-'.length);
        const item = access.course.modules.flatMap((module) => module.items).find((candidate) => candidate.id === contentId);
        if (!item || !['Discussion', 'Reflection', 'Survey'].includes(item.contentType || '')) notFound();
        activity = { kind: 'content', contentId: item.id, contentType: item.contentType as 'Discussion' | 'Reflection' | 'Survey', title: item.title, description: item.description, completed: item.status === 'completed' };
        description = promptBody(item.content) || item.description || '';
        points = item.maxPoints;
    }

    if (!activity) notFound();
    const title = activity.kind === 'assessment' ? activity.assessment.title || 'Assessment' : activity.title;
    const type = activity.kind === 'assessment' ? (activity.assessment.type === 'Exam' ? 'Quiz' : activity.assessment.type) : activity.contentType;

    return (
        <div className="mx-auto max-w-4xl space-y-6">
            <Button asChild variant="ghost" className="-ml-3 text-slate-400"><Link href={`/courses/${slug}/activities`}><ArrowLeft className="size-4" />All activities</Link></Button>
            <header className="border-b border-white/10 pb-6"><div className="flex flex-wrap items-center gap-2"><Badge variant="outline">{type}</Badge>{points != null ? <Badge className="bg-white/10 text-slate-300">{points} points</Badge> : null}</div><h1 className="mt-4 text-3xl font-semibold text-white">{title}</h1>{dueAt ? <p className="mt-3 inline-flex items-center gap-2 text-sm text-slate-400"><CalendarClock className="size-4" />Due {new Intl.DateTimeFormat('en-US', { dateStyle: 'long', timeStyle: 'short' }).format(new Date(dueAt))}</p> : null}</header>
            {description ? <Card className="border-white/10 bg-white/[0.03]"><CardHeader><CardTitle className="flex items-center gap-2 text-lg"><ClipboardCheck className="size-5 text-violet-300" />Instructions</CardTitle></CardHeader><CardContent className="prose prose-invert max-w-none"><MarkdownRenderer content={description} /></CardContent></Card> : null}
            <Card className="border-white/10 bg-white/[0.03]"><CardHeader><CardTitle className="text-lg">Your response</CardTitle></CardHeader><CardContent><LearnerActivityForm courseId={access.course.id} courseSlug={slug} enrollmentId={access.course.enrollmentId} activity={activity} /></CardContent></Card>
        </div>
    );
}