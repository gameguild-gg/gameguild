import { auth } from '@/auth';
import { Link } from '@/i18n/navigation';
import { CourseAccessGate } from '@/components/learning/course-access-gate';
import {
  LearnerActivityForm,
  type LearnerActivityDescriptor,
} from '@/components/learning/learner-activity-form';
import { getCodingAssignmentPublic } from '@/lib/coding-assignment/client';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/types';
import { getCourseAccessData } from '@/lib/learner/courses';
import { getCourseLearnerContext, getMyProjects } from '@/lib/learner/records';
import { MarkdownRenderer } from '@game-guild/content-rendering';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, CalendarClock, ClipboardCheck } from 'lucide-react';
import { notFound } from 'next/navigation';
import { CodingActivityClient } from './coding-activity-client';

function promptBody(value: unknown): string {
  if (typeof value === 'string') return value;
  if (value && typeof value === 'object') return JSON.stringify(value, null, 2);
  return '';
}

/** submissionModalities is a comma-separated string of C# [Flags] enum names — "Code", not 8. */
function allowsCodeModality(modalities: string | undefined): boolean {
  if (!modalities) return false;
  return modalities
    .split(',')
    .map((entry) => entry.trim())
    .includes('Code');
}

export default async function LearnerActivityPage({
  params,
}: {
  params: Promise<{ activityId: string; slug: string }>;
}) {
  const { activityId, slug } = await params;
  const access = await getCourseAccessData(slug);
  if (access.kind === 'not-found') notFound();
  if (access.kind !== 'ready') return <CourseAccessGate access={access} />;
  if (!access.course.enrollmentId) notFound();

  const context = await getCourseLearnerContext(access.course.id);
  let activity: LearnerActivityDescriptor | null = null;
  let codingAssignment: CodingAssignmentContent | null = null;
  let useCodingExperience = false;
  let description = '';
  let dueAt: string | null | undefined;
  let points: number | undefined;

  if (activityId.startsWith('assessment-')) {
    const assessmentId = activityId.slice('assessment-'.length);
    const assessment = context.assessments.find((candidate) => candidate.id === assessmentId);
    if (!assessment) notFound();
    const submission = context.submissions.find(
      (candidate) => candidate.assessmentId === assessmentId,
    );
    const session = assessment.type === 'Project' ? await auth() : null;
    const projects = session?.user?.id ? await getMyProjects(session.user.id) : [];
    activity = { kind: 'assessment', assessment, submission, projects };
    description = assessment.description || '';
    dueAt = assessment.dueAt;
    points = assessment.maxScore;

    const codingEligible =
      Boolean(assessment.id) &&
      Boolean(assessment.contentId) &&
      (assessment.type === 'Assignment' || assessment.type === 'Project') &&
      allowsCodeModality(assessment.submissionModalities);
    if (codingEligible && assessment.id && assessment.contentId) {
      // v1: fetch via ProgramContent route — server strips Private tests + Private files.
      const candidate = await getCodingAssignmentPublic(
        access.course.id,
        assessment.contentId,
      );
      if (candidate && candidate.Type === 'coding-assignment') {
        codingAssignment = candidate;
        useCodingExperience = true;
      }
    }
  } else if (activityId.startsWith('content-')) {
    const contentId = activityId.slice('content-'.length);
    const item = access.course.modules
      .flatMap((module) => module.items)
      .find((candidate) => candidate.id === contentId);
    if (!item || !['Discussion', 'Reflection', 'Survey'].includes(item.contentType || '')) {
      notFound();
    }
    activity = {
      kind: 'content',
      contentId: item.id,
      contentType: item.contentType as 'Discussion' | 'Reflection' | 'Survey',
      title: item.title,
      description: item.description,
      completed: item.status === 'completed',
    };
    description = promptBody(item.content) || item.description || '';
  }

  if (!activity) notFound();
  const title = activity.kind === 'assessment' ? activity.assessment.title || 'Assessment' : activity.title;
  const type = activity.kind === 'assessment'
    ? activity.assessment.type
    : activity.contentType;

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <Button asChild variant="ghost" className="-ml-3">
        <Link href={`/learn/courses/${slug}/activities`}>
          <ArrowLeft className="size-4" />
          All activities
        </Link>
      </Button>
      <header className="border-b pb-6">
        <div className="flex flex-wrap items-center gap-2">
          <Badge variant="outline">{type}</Badge>
          {points != null ? <Badge variant="secondary">{points} points</Badge> : null}
        </div>
        <h1 className="mt-4 text-3xl font-semibold">{title}</h1>
        {dueAt ? (
          <p className="mt-3 inline-flex items-center gap-2 text-sm text-muted-foreground">
            <CalendarClock className="size-4" />
            Due{' '}
            {new Intl.DateTimeFormat('en-US', {
              dateStyle: 'long',
              timeStyle: 'short',
            }).format(new Date(dueAt))}
          </p>
        ) : null}
      </header>
      {description ? (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <ClipboardCheck className="size-5 text-primary" />
              Instructions
            </CardTitle>
          </CardHeader>
          <CardContent className="prose max-w-none dark:prose-invert">
            <MarkdownRenderer content={description} />
          </CardContent>
        </Card>
      ) : null}
      <Card>
        <CardHeader>
          <CardTitle className="text-lg">Your response</CardTitle>
        </CardHeader>
        <CardContent>
          {activity.kind === 'assessment' &&
          useCodingExperience &&
          codingAssignment &&
          activity.assessment.id ? (
            <CodingActivityClient
              assessmentId={activity.assessment.id}
              enrollmentId={access.course.enrollmentId}
              courseId={access.course.id}
              slug={slug}
              assignment={codingAssignment}
              manifestUrl={process.env.NEXT_PUBLIC_EMCEPTION_MANIFEST_URL}
            />
          ) : (
            <LearnerActivityForm
              courseId={access.course.id}
              courseSlug={slug}
              enrollmentId={access.course.enrollmentId}
              activity={activity}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
