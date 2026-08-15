import { auth, getToken } from '@/auth';
import { Link } from '@/i18n/navigation';
import { CourseAccessGate } from '@/components/learning/course-access-gate';
import {
  LearnerActivityForm,
  type LearnerActivityDescriptor,
} from '@/components/learning/learner-activity-form';
import { getCodingAssignmentPublic } from '@/lib/coding-assignment/client';
import { codePayloadToFiles } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/types';
import { getCourseAccessData } from '@/lib/learner/courses';
import { getCourseLearnerContext, getMyProjects } from '@/lib/learner/records';
import { createServerClient, GeneratedApi } from '@game-guild/client';
import { MarkdownRenderer } from '@game-guild/content-rendering';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowLeft, CalendarClock, ClipboardCheck } from 'lucide-react';
import { notFound } from 'next/navigation';
import { CodingActivityClient } from './coding-activity-client';

interface SubmissionFile {
  path: string;
  content: string;
  encoding: 'text';
  modifiable: boolean;
}

/**
 * Best-effort restore of the learner's latest code submission for an
 * assessment: list my submissions, keep matching ones with a code payload,
 * take the highest attemptNumber, parse the payload. Any failure returns
 * null so the page still renders with starter files.
 */
async function loadLastSubmissionFiles(
  assessmentId: string,
  enrollmentId: string,
): Promise<SubmissionFile[] | null> {
  try {
    const apiUrl =
      process.env.API_URL ||
      process.env.NEXT_PUBLIC_API_URL ||
      'http://localhost:8080';
    const client = createServerClient({
      baseUrl: apiUrl,
      auth: { getAccessToken: () => getToken() },
    });
    const assessments = new GeneratedApi.LearningAssessmentsModule(client);
    const result = await assessments.getAssessmentsMySubmissions(enrollmentId);
    if (!result.ok) return null;
    const matching = result.data.filter(
      (entry) =>
        entry.assessmentId === assessmentId && entry.codePayload != null,
    );
    const latest = matching.reduce<(typeof matching)[number] | null>(
      (max, entry) =>
        !max || (entry.attemptNumber ?? 0) > (max.attemptNumber ?? 0)
          ? entry
          : max,
      null,
    );
    if (!latest?.codePayload) return null;
    return codePayloadToFiles(latest.codePayload).map((file) => ({
      ...file,
      encoding: 'text' as const,
      modifiable: true,
    }));
  } catch (err) {
    console.error('loadLastSubmissionFiles: unexpected error', err);
    return null;
  }
}

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
  let codingUnpublished = false;
  let submissionFiles: SubmissionFile[] | null = null;
  let userId: string | null = null;
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
    // Unconditional: the coding experience needs a user id for its
    // user-scoped draft token, not just Project-type project listings.
    const session = await auth();
    userId = session?.user?.id ?? null;
    const projects = userId ? await getMyProjects(userId) : [];
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
        submissionFiles = await loadLastSubmissionFiles(
          assessment.id,
          access.course.enrollmentId,
        );
      }
    }
    codingUnpublished = codingEligible && !codingAssignment;
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
  // learn/layout.tsx already redirects signed-out visitors; the userId
  // guard keeps the client's user-scoped token well-formed even if that
  // invariant ever breaks.
  const codingProps =
    activity.kind === 'assessment' &&
    useCodingExperience &&
    codingAssignment &&
    activity.assessment.id &&
    userId
      ? {
          assessmentId: activity.assessment.id,
          assignment: codingAssignment,
          userId,
        }
      : null;

  return (
    <div
      className={
        codingProps ? 'w-full space-y-6' : 'mx-auto max-w-4xl space-y-6'
      }
    >
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
      {codingUnpublished ? (
        <Card className="border-amber-500/50 bg-amber-500/5">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg text-amber-600 dark:text-amber-400">
              <ClipboardCheck className="size-5" />
              Coding assignment not published yet
            </CardTitle>
          </CardHeader>
          <CardContent className="text-sm text-muted-foreground">
            This coding assignment hasn't been published by the instructor
            yet. The interactive code editor will appear here once the
            definition is saved — until then, you can still submit your
            response using the form below.
          </CardContent>
        </Card>
      ) : null}
      {codingProps ? (
        <div data-testid="ide-fullwidth-mount">
          <CodingActivityClient
            assessmentId={codingProps.assessmentId}
            enrollmentId={access.course.enrollmentId}
            courseId={access.course.id}
            slug={slug}
            assignment={codingProps.assignment}
            manifestUrl={process.env.NEXT_PUBLIC_EMCEPTION_MANIFEST_URL}
            userId={codingProps.userId}
            submissionFiles={submissionFiles}
          />
        </div>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Your response</CardTitle>
          </CardHeader>
          <CardContent>
            <LearnerActivityForm
              courseId={access.course.id}
              courseSlug={slug}
              enrollmentId={access.course.enrollmentId}
              activity={activity}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
