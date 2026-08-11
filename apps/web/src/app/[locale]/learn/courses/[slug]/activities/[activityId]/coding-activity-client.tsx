'use client';

import { useRouter } from '@/i18n/navigation';
import { filesToCodePayload } from '@/lib/emception/code-payload';
import { computeScore } from '@/lib/emception/scoring';
import type { CodingDefinition } from '@/lib/learning/queries/assessments';
import {
  submitAssessment,
  type LearnerMutationResult,
} from '@/lib/learner/activity-actions';
import type {
  GradingPlan,
  IdeHandle,
  TestReport,
  WorkspaceConfig,
} from '@game-guild/emception-ui';
import type { TestPlan } from 'emception';
import { Button } from '@game-guild/ui/components/button';
import { lazy, Suspense, useRef, useState, type FormEvent } from 'react';

const Ide = lazy(
  () => import('@game-guild/emception-ui').then((m) => ({ default: m.Ide })),
);

function IdeSkeleton() {
  return (
    <div
      data-testid="ide-skeleton"
      className="flex h-96 items-center justify-center rounded-md border border-white/10 bg-white/[0.03] text-sm text-muted-foreground"
    >
      Loading IDE…
    </div>
  );
}

export interface CodingActivityClientProps {
  assessmentId: string;
  enrollmentId: string;
  courseId: string;
  slug: string;
  workspaceConfig: CodingDefinition['workspaceConfig'];
  testPlan: CodingDefinition['testPlan'];
  manifestUrl?: string;
  maxScore: number;
  passingScore: number;
}

export function CodingActivityClient({
  assessmentId,
  enrollmentId,
  slug,
  workspaceConfig,
  testPlan,
  manifestUrl,
  maxScore,
  passingScore,
}: CodingActivityClientProps) {
  const ref = useRef<IdeHandle>(null);
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<LearnerMutationResult | null>(null);
  const [report, setReport] = useState<TestReport | null>(null);

  if (result?.success) {
    return (
      <div
        role="status"
        className="rounded-md border border-emerald-500/30 bg-emerald-500/10 p-5 text-emerald-100"
      >
        Submission received. Grades and instructor feedback will appear here
        when available.
      </div>
    );
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (submitting) return;
    setSubmitting(true);
    try {
      const files = (await ref.current?.getFiles()) ?? [];
      const fd = new FormData();
      fd.set('assessmentId', assessmentId);
      fd.set('enrollmentId', enrollmentId);
      fd.set('modality', 'Code');
      fd.set('response', filesToCodePayload(files));
      const outcome = await submitAssessment({ success: false }, fd);
      setResult(outcome);
      if (outcome.success) {
        router.push(`/learn/courses/${slug}/activities`);
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      <Suspense fallback={<IdeSkeleton />}>
        <Ide
          ref={ref}
          workspaceConfig={workspaceConfig as unknown as WorkspaceConfig | undefined}
          testPlan={testPlan as unknown as GradingPlan | undefined}
          testMode="public"
          manifestUrl={manifestUrl}
          maxScore={maxScore}
          passingScore={passingScore}
          onTestReport={setReport}
        />
      </Suspense>
      {report ? (
        <PublicTestEstimateBanner
          report={report}
          plan={testPlan as unknown as GradingPlan | undefined}
          maxScore={maxScore}
          passingScore={passingScore}
        />
      ) : null}
      {result?.error ? (
        <p role="alert" className="text-sm text-destructive">
          {result.error}
        </p>
      ) : null}
      <div className="flex justify-end">
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Submitting…' : 'Submit'}
        </Button>
      </div>
    </form>
  );
}

function PublicTestEstimateBanner({
  report,
  plan,
  maxScore,
  passingScore,
}: {
  report: TestReport;
  plan: GradingPlan | undefined;
  maxScore: number;
  passingScore: number;
}) {
  let scoreText: string | null = null;
  let unavailable = false;
  try {
    if (plan) {
      const { score } = computeScore(
        report,
        plan as unknown as TestPlan,
        maxScore,
        passingScore,
      );
      scoreText = Number.isFinite(score) ? `${score}/${maxScore}` : null;
    }
    unavailable = scoreText === null;
  } catch {
    unavailable = true;
  }

  if (unavailable) {
    return (
      <div
        role="alert"
        data-testid="public-test-estimate-unavailable"
        className="rounded-md border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-100"
      >
        Estimate unavailable.
      </div>
    );
  }

  const total = report.passed + report.failed;
  return (
    <div
      role="status"
      data-testid="public-test-estimate-banner"
      className="rounded-md border border-sky-500/30 bg-sky-500/10 p-3 text-sm text-sky-100"
    >
      Your public tests: {report.passed}/{total} passed (estimated score:{' '}
      {scoreText}). This is an estimate based on public tests only — hidden
      tests may change your final grade.
    </div>
  );
}
