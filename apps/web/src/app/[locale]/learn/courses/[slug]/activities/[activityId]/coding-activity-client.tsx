'use client';

import { filesToCodePayload } from '@/lib/emception/code-payload';
import type { CodingDefinition } from '@/lib/learning/queries/assessments';
import {
  submitAssessment,
  type LearnerMutationResult,
} from '@/lib/learner/activity-actions';
import type {
  GradingPlan,
  IdeHandle,
  WorkspaceConfig,
} from '@game-guild/emception-ui';
import { Button } from '@game-guild/ui/components/button';
import dynamic from 'next/dynamic';
import { useRouter } from 'next/navigation';
import { Suspense, useRef, useState, type FormEvent } from 'react';

const Ide = dynamic(
  () => import('@game-guild/emception-ui').then((m) => ({ default: m.Ide })),
  { ssr: false, loading: () => <IdeSkeleton /> },
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
        router.push(`/courses/${slug}/activities`);
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
        />
      </Suspense>
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
