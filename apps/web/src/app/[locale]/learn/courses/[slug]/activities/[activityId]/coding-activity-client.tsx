'use client';

import { useRouter } from '@/i18n/navigation';
import { filesToCodePayload } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/types';
import { computeScore } from '@/lib/emception/scoring';
import {
  submitAssessment,
  type LearnerMutationResult,
} from '@/lib/learner/activity-actions';
import type { GradingCase, GradingPlan, IdeHandle, TestReport } from '@game-guild/emception-ui';
import type { TestPlan } from 'emception';
import { Button } from '@game-guild/ui/components/button';
import Script from 'next/script';
import { lazy, Suspense, useEffect, useRef, useState, type FormEvent } from 'react';

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
  /** v1 CodingAssignmentContent (Public-only — server strips Private). */
  assignment: CodingAssignmentContent;
  manifestUrl?: string;
}

/** Build the IDE-seedable file list from v1 Data.Files, defensively filtering Public. */
function publicSeedFiles(assignment: CodingAssignmentContent): Array<{
  path: string;
  content: string;
  modifiable: boolean;
}> {
  return Object.entries(assignment.Data.Files)
    .filter(([, meta]) => meta.Visibility === 'Public')
    .map(([path, meta]) => ({
      path,
      content: meta.Content,
      modifiable: meta.Modifiable,
    }));
}

/**
 * Map v1 Tests.Public (StandardTest | FunctionalTest) → emception GradingCase[].
 * FunctionalTest requires harness generation (Task 6/7) which is not yet wired
 * into the student runtime — skipped from the public-test banner for v1.
 * ponytail: include FunctionalTest in the banner once Task 7's buildTestPlan
 * is reachable from web (probably via Task 11 grader shared util).
 */
function publicTestsToGradingPlan(assignment: CodingAssignmentContent): GradingPlan {
  const cases: GradingCase[] = [];
  for (const test of assignment.Tests.Public) {
    if (test.kind !== 'standard') continue;
    cases.push({
      kind: 'stdio',
      name: test.Name ?? undefined,
      weight: test.Weight ?? 1,
      stdin: test.Stdin ?? undefined,
      expectedStdout: test.Stdout,
      expectedStderr: test.Stderr ?? undefined,
      expectedExit: test.ExitCode ?? undefined,
    });
  }
  return { cases };
}

export function CodingActivityClient({
  assessmentId,
  enrollmentId,
  slug,
  assignment,
  manifestUrl,
}: CodingActivityClientProps) {
  const ref = useRef<IdeHandle>(null);
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [report, setReport] = useState<TestReport | null>(null);
  const [result, setResult] = useState<LearnerMutationResult | null>(null);

  const seedFiles = useRef(publicSeedFiles(assignment));
  const gradingPlan = useRef<GradingPlan>(publicTestsToGradingPlan(assignment));
  const allowCreateFiles = assignment.Environment.AllowStudentCreateFiles !== false;
  const maxScore = assignment.Grading.MaxScore;
  // ponytail: PassingScore moved from GradingConfig to Program (T1/T3). Learner context
  // doesn't surface Program.PassingScore yet — default to 60 (the C# default + historical
  // constant) until T13 wires the course-level field through getCourseLearnerContext.
  const passingScore = 60;

  // Seed IDE with Public files + apply readOnly meta for non-modifiable files.
  // setFiles/setFileMeta mutate reactive state first; VFS sync happens later
  // when doBootstrap runs syncFilesToVfs(filesRef.current). Safe to call pre-boot.
  useEffect(() => {
    const handle = ref.current;
    if (!handle) return;
    const files = seedFiles.current;
    if (files.length === 0) return;
    let cancelled = false;
    (async () => {
      await handle.setFiles(files.map(({ path, content }) => ({ path, content })));
      if (cancelled) return;
      for (const { path, modifiable } of files) {
        if (!modifiable) {
          // per-file Monaco readOnly via Task 5 setFileMeta
          await handle.setFileMeta(path, { modifiable: false });
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

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
      // Content-diff semantics: only edits + student-created files; deletions absent.
      const modified = (await ref.current?.getModifiedFiles()) ?? [];
      const fd = new FormData();
      fd.set('assessmentId', assessmentId);
      fd.set('enrollmentId', enrollmentId);
      fd.set('modality', 'Code');
      // Wire shape (Metis #29): Record<path, {content, encoding: 'text'}>
      fd.set('response', filesToCodePayload(modified));
      const outcome = await submitAssessment({ success: false }, fd);
      setResult(outcome);
      if (outcome.success) {
        router.push(`/learn/courses/${slug}/activities`);
      }
    } finally {
      setSubmitting(false);
    }
  }

  // ponytail: <Ide> has no "hide new file button" prop. Gate via a wrapper
  // data-attribute + CSS rule. Upgrade path: Task 5 IdeHandle could expose
  // an allowCreateFiles prop, or FileExplorer could read it via context.
  const wrapperDataAttrs = {
    'data-allow-create-files': allowCreateFiles ? 'true' : 'false',
  };

  return (
    <>
      <Script src="/coi-serviceworker.js" strategy="beforeInteractive" />
      <form onSubmit={handleSubmit} className="space-y-4">
        <div {...wrapperDataAttrs}>
          <Suspense fallback={<IdeSkeleton />}>
            <Ide
              ref={ref}
              testPlan={gradingPlan.current}
              testMode="public"
              manifestUrl={manifestUrl}
              maxScore={maxScore}
              passingScore={passingScore}
              onTestReport={setReport}
            />
          </Suspense>
        </div>
      {report ? (
        <PublicTestEstimateBanner
          report={report}
          plan={gradingPlan.current}
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
    {/* Hide the IDE's internal "New File" buttons when students may not create files. */}
    {!allowCreateFiles ? (
      <style>{`[data-allow-create-files="false"] button[title^="New "] { display: none !important; }`}</style>
    ) : null}
  </>
);
}

function PublicTestEstimateBanner({
  report,
  plan,
  maxScore,
  passingScore,
}: {
  report: TestReport;
  plan: GradingPlan;
  maxScore: number;
  passingScore: number;
}) {
  let scoreText: string | null = null;
  let unavailable = false;
  try {
    if (plan.cases.length > 0) {
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
