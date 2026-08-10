'use client';

import React, { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Ide, type IdeHandle } from '@game-guild/emception-ui';
import { TestResultsPanel, type TestReport } from '@game-guild/emception-ui';
import { scoreSubmission, formatFeedback, type BackendTestCaseDto, type ScoringDefinition } from '@/lib/emception/scoring';
import { gradeSubmission } from '@/lib/learning/grade-action';
import type { CodeFile } from '@/lib/emception/code-payload';
import { Loader2 } from 'lucide-react';
import { Button } from '@game-guild/ui/components/button';

type GradeState = 'idle' | 'grading' | 'ready' | 'posting' | 'done';

export interface GradeClientProps {
  courseSlug: string;
  assessmentId: string;
  submissionId: string;
  initialFiles: CodeFile[];
  workspaceConfig: Record<string, unknown> | null;
  testPlan: Record<string, unknown> | null;
  maxScore: number;
  passingScore: number;
  manifestUrl: string;
}

export function GradeClient({
  courseSlug,
  assessmentId,
  submissionId,
  initialFiles,
  workspaceConfig,
  testPlan,
  maxScore,
  passingScore,
  manifestUrl,
}: GradeClientProps): React.JSX.Element {
  const router = useRouter();
  const ideRef = useRef<IdeHandle>(null);
  const [gradeState, setGradeState] = useState<GradeState>('idle');
  const [report, setReport] = useState<TestReport | null>(null);
  const [score, setScore] = useState<number | null>(null);
  const [feedback, setFeedback] = useState<string>('');
  const [error, setError] = useState<string | null>(null);

  // Seed the IDE with the student's submitted files on mount.
  useEffect(() => {
    if (initialFiles.length === 0) return;
    ideRef.current?.setFiles(initialFiles).catch((err) => {
      console.error('Failed to seed IDE with submission files:', err);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleGrade() {
    if (!testPlan) {
      setError('No test plan on this assessment definition.');
      return;
    }
    setGradeState('grading');
    setError(null);
    setReport(null);
    setScore(null);
    try {
      const plan = testPlan as { cases: BackendTestCaseDto[] };
      // ponytail: backend TestCaseDto JSON is structurally compatible with emception TestPlan at runtime.
      const r = (await ideRef.current!.runTests(plan as unknown as never)) as TestReport;
      const definition: ScoringDefinition = {
        testPlan: plan,
        maxScore,
        passingScore,
      };
      const result = scoreSubmission(definition, r);
      setReport(r);
      setScore(result.score);
      setFeedback(formatFeedback(r, result.score));
      setGradeState('ready');
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setGradeState('idle');
    }
  }

  async function handleConfirm() {
    if (gradeState !== 'ready' || score === null) return;
    setGradeState('posting');
    setError(null);
    try {
      const result = await gradeSubmission({
        submissionId,
        score,
        feedback,
      });
      if (!result.success) {
        setError(result.error);
        setGradeState('ready');
        return;
      }
      setGradeState('done');
      router.push(
        `/dashboard/learning/courses/${encodeURIComponent(courseSlug)}/assessments/${encodeURIComponent(assessmentId)}`,
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setGradeState('ready');
    }
  }

  const grading = gradeState === 'grading' || gradeState === 'posting';

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Grade submission</h1>
          <p className="text-muted-foreground text-sm">
            Run the full test plan against the student&apos;s code, then confirm the grade.
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            type="button"
            onClick={handleGrade}
            disabled={grading}
            data-testid="grade-button"
          >
            {gradeState === 'grading' ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" data-testid="grade-spinner" />
            ) : null}
            Grade
          </Button>
          <Button
            type="button"
            onClick={handleConfirm}
            disabled={gradeState !== 'ready' || grading}
            data-testid="confirm-grade-button"
          >
            {gradeState === 'posting' ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" data-testid="confirm-spinner" />
            ) : null}
            Confirm grade
          </Button>
        </div>
      </div>

      {error && (
        <div
          role="alert"
          data-testid="grade-error"
          className="rounded border border-red-500 bg-red-50 p-3 text-sm text-red-800 dark:bg-red-950 dark:text-red-200"
        >
          {error}
        </div>
      )}

      {score !== null && report && (
        <div data-testid="grade-result" className="space-y-2">
          <p className="text-lg font-semibold" data-testid="grade-score">
            Computed score: {score} / {maxScore}
          </p>
          <TestResultsPanel
            report={report}
            maxScore={maxScore}
            passingScore={passingScore}
            weights={(testPlan as { cases?: { weight?: number }[] } | null)?.cases?.map((c) => c.weight ?? 1) ?? undefined}
          />
        </div>
      )}

      <div className="border">
        <Ide
          ref={ideRef}
          workspaceConfig={(workspaceConfig ?? undefined) as never}
          testPlan={testPlan as never}
          testMode="full"
          manifestUrl={manifestUrl}
          maxScore={maxScore}
          passingScore={passingScore}
        />
      </div>
    </div>
  );
}
