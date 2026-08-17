'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  Ide,
  TestResultsPanel,
  type IdeHandle,
  type TestReport,
} from '@game-guild/emception-ui';
import { buildTestPlan } from 'emception/testing';
// Cast bridge: the web `CodingAssignmentContent` (lib/coding-assignment/types)
// uses `readonly` arrays; the emception mapper input uses mutable arrays. The
// wire shape is identical at runtime — only TS strictness differs.
import type {
  CodingAssignmentContent as EmceptionAssignmentContent,
} from 'emception/testing';
import {
  scoreSubmission,
  formatFeedback,
  type BackendTestCaseDto,
  type ScoringDefinition,
} from '@/lib/emception/scoring';
import { gradeSubmission } from '@/lib/learning/grade-action';
import type { CodeFile } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';
import { composeFeedback } from './compose-feedback';
import { Button } from '@game-guild/ui/components/button';
import { Loader2 } from 'lucide-react';
import { useLearningBase } from '@/lib/learning/use-learning-base';

type GradeState = 'idle' | 'grading' | 'ready' | 'posting' | 'done';

export interface GradeClientProps {
  courseSlug: string;
  assessmentId: string;
  submissionId: string;
  /** Full assignment (Public + Private tests + all files) — Task 4 wrapper. */
  assignment: CodingAssignmentContent;
  /** Submitted student files (Task 9 code-payload parsed server-side). */
  submittedFiles: CodeFile[];
  maxScore: number;
  manifestUrl: string;
}

/**
 * Build the Set of Private workspace file paths. The submission MUST NOT
 * override these (Metis #30) — Private files carry the instructor's solution
 * or fixtures the student never sees.
 *
 * Memoized once per assignment.
 */
function buildPrivatePaths(assignment: CodingAssignmentContent): Set<string> {
  return new Set(
    Object.entries(assignment.Data.Files)
      .filter(([, meta]) => meta.Visibility === 'Private')
      .map(([path]) => path),
  );
}

/**
 * Merge the instructor's workspace with the student's submission.
 *
 * - Start from the workspace (Public + Private).
 * - For each submitted file:
 *     - IF its path matches a Private workspace path → log + skip (Metis #30).
 *     - ELSE override the workspace file (or add it if student-created).
 *
 * Pure + exported so the merge contract is unit-testable without rendering.
 */
export function mergeWorkspaceWithSubmission(
  assignment: CodingAssignmentContent,
  submittedFiles: CodeFile[],
): CodeFile[] {
  const privatePaths = buildPrivatePaths(assignment);
  const merged = new Map<string, string>();
  for (const [path, meta] of Object.entries(assignment.Data.Files)) {
    merged.set(path, meta.Content);
  }
  for (const file of submittedFiles) {
    if (privatePaths.has(file.path)) {
      // ponytail: console.warn is the only side-effect — no DB row, no telemetry.
      // Backend already rejects the same path on submit; this is the second-line
      // guard for adversarial payloads.
      console.warn(
        `[grade] Submission attempted to override Private workspace file ${file.path}; skipping per Metis #30`,
      );
      continue;
    }
    merged.set(file.path, file.content);
  }
  return Array.from(merged.entries()).map(([path, content]) => ({ path, content }));
}

export function GradeClient({
  courseSlug,
  assessmentId,
  submissionId,
  assignment,
  submittedFiles,
  maxScore,
  manifestUrl,
}: GradeClientProps): React.JSX.Element {
  const learningBase = useLearningBase();
  const router = useRouter();
  const ideRef = useRef<IdeHandle>(null);
  const [gradeState, setGradeState] = useState<GradeState>('idle');
  const [report, setReport] = useState<TestReport | null>(null);
  const [score, setScore] = useState<number | null>(null);
  const [autoFeedback, setAutoFeedback] = useState<string>('');
  const [overallComment, setOverallComment] = useState<string>('');
  const [perFileComments, setPerFileComments] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const mergedFiles = useMemo(
    () => mergeWorkspaceWithSubmission(assignment, submittedFiles),
    [assignment, submittedFiles],
  );

  // Seed the IDE with the merged workspace on mount.
  //
  // The plan recommends gating on `<Ide>` `onReady`, but the component exposes
  // no such callback and no `useEmception` hook is in use here. The call is
  // safe pre-boot: `Ide.setFiles` updates reactive state + the seed snapshot,
  // and `syncFilesToVfs` no-ops until `orchestratorRef.current` is set; once
  // boot completes the boot effect re-syncs from `filesRef.current`, so the
  // merged workspace is in the worker VFS before any compile runs.
  useEffect(() => {
    if (mergedFiles.length === 0) return;
    ideRef.current?.setFiles(mergedFiles).catch((err) => {
      console.error('Failed to seed IDE with merged workspace:', err);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleRunTests() {
    setGradeState('grading');
    setError(null);
    setReport(null);
    setScore(null);
    try {
      // (e.1) Task 7 mapper — Public + Private cases + harness files.
      const { plan, generatedFiles } = buildTestPlan(
        assignment as unknown as EmceptionAssignmentContent,
        { mode: 'full' },
      );

      // (e.2) Re-seed IDE with [current workspace, generated harnesses]. The
      // generated harness paths are stable across runs (`functional_<i>_test.cpp`)
      // so de-dupe them against any prior run.
      const currentFiles = (await ideRef.current!.getFiles()) as CodeFile[];
      const harnessPaths = new Set(generatedFiles.map((f) => f.path));
      const merged = [
        ...currentFiles.filter((f: CodeFile) => !harnessPaths.has(f.path)),
        ...generatedFiles,
      ];
      await ideRef.current!.setFiles(merged);

      // (e.3) Run + (e.4) report. Engine contract (Metis #33): tool failures
      // resolve as a `ToolResult` with non-zero exit code; the test report
      // surfaces them as failing cases — `runTests` does NOT reject here.
      const r = (await ideRef.current!.runTests(plan as never)) as TestReport;

      // (f) Compute weighted score via the shared scoring utility.
      // ponytail: passingScore=0 — grader doesn't compute pass/fail; server's
      // GradeSubmissionAsync loads Program.PassingScore for the snapshot.
      // ScoreResult.passed is unused here; only .score is displayed.
      const definition: ScoringDefinition = {
        testPlan: { cases: plan.cases as unknown as BackendTestCaseDto[] },
        maxScore,
        passingScore: 0,
      };
      const result = scoreSubmission(definition, r);
      setReport(r);
      setScore(result.score);
      setAutoFeedback(formatFeedback(r, result.score));
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
      // (h) Compose markdown from instructor comments + auto-feedback and POST.
      const feedback = composeFeedback({
        overallComment,
        perFileComments,
        autoFeedback,
      });
      const result = await gradeSubmission({ submissionId, score, feedback });
      if (!result.success) {
        setError(result.error);
        setGradeState('ready');
        return;
      }
      setGradeState('done');
      router.push(
        `${learningBase}/courses/${encodeURIComponent(courseSlug)}/assessments/${encodeURIComponent(assessmentId)}`,
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
            onClick={handleRunTests}
            disabled={grading}
            data-testid="grade-button"
          >
            {gradeState === 'grading' ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" data-testid="grade-spinner" />
            ) : null}
            Run Tests
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
          />
        </div>
      )}

      <div className="border h-[70vh] min-h-[500px]">
        <Ide
          ref={ideRef}
          manifestUrl={manifestUrl}
          maxScore={maxScore}
        />
      </div>

      {/* (g) Per-file comments — one textarea per merged file, kept in page
          state. No DB schema; composed into the final Feedback column on submit. */}
      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Per-file comments</h2>
        {mergedFiles.map((f) => (
          <div key={f.path} className="space-y-1">
            <label
              htmlFor={`comment-${f.path}`}
              className="text-sm font-medium"
              data-testid={`comment-label-${f.path}`}
            >
              {f.path}
            </label>
            <textarea
              id={`comment-${f.path}`}
              value={perFileComments[f.path] ?? ''}
              onChange={(e) =>
                setPerFileComments((prev) => ({ ...prev, [f.path]: e.target.value }))
              }
              data-testid={`comment-${f.path}`}
              rows={2}
              className="w-full rounded border p-2 text-sm"
              placeholder="Optional comment for this file"
            />
          </div>
        ))}
      </div>

      <div className="space-y-1">
        <h2 className="text-lg font-semibold">Overall comment</h2>
        <textarea
          value={overallComment}
          onChange={(e) => setOverallComment(e.target.value)}
          data-testid="overall-comment"
          rows={3}
          className="w-full rounded border p-2 text-sm"
          placeholder="Overall feedback for the student"
        />
      </div>
    </div>
  );
}
