'use client';

import React, { useEffect, useMemo, useRef, useState } from 'react';
import { Ide, TestResultsPanel, type IdeHandle, type TestReport } from '@game-guild/emception-ui';
import { buildTestPlan } from 'emception/testing';
// Cast bridge: the web `CodingAssignmentContent` (lib/coding-assignment/types)
// uses `readonly` arrays; the emception mapper input uses mutable arrays. The
// wire shape is identical at runtime — only TS strictness differs.
import type { CodingAssignmentContent as EmceptionAssignmentContent } from 'emception/testing';
import { scoreSubmission, formatFeedback, type BackendTestCaseDto, type ScoringDefinition } from '@/lib/emception/scoring';
import type { CodeFile } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';
import { Button } from '@game-guild/ui/components/button';
import { Loader2 } from 'lucide-react';

type GradeState = 'idle' | 'grading' | 'ready';

export interface ComputedScore {
  score: number;
  autoFeedback: string;
}

export interface CodeGraderPanelProps {
  /** Full assignment (Public + Private tests + all files) — Task 4 wrapper. */
  assignment: CodingAssignmentContent;
  /** Submitted student files (code-payload parsed server-side or client-side). */
  submittedFiles: CodeFile[];
  maxScore: number;
  manifestUrl: string;
  /** Submission id — namespaces Ide localStorage per submission to prevent stale-data blink. */
  submissionId?: string;
  /** Run-tests result seeds the grading panel's score input. */
  onComputedScore?: (result: ComputedScore) => void;
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
export function mergeWorkspaceWithSubmission(assignment: CodingAssignmentContent, submittedFiles: CodeFile[]): CodeFile[] {
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
      console.warn(`[grade] Submission attempted to override Private workspace file ${file.path}; skipping per Metis #30`);
      continue;
    }
    merged.set(file.path, file.content);
  }
  return Array.from(merged.entries()).map(([path, content]) => ({
    path,
    content,
  }));
}

/**
 * SpeedGrader code viewer — the emception IDE with the merged workspace plus
 * the full (Public + Private) test-run flow. Moved from the legacy
 * `grade-client.tsx`; the score/feedback/submit UX lives in the grading panel.
 */
export function CodeGraderPanel({ assignment, submittedFiles, maxScore, manifestUrl, submissionId, onComputedScore }: CodeGraderPanelProps): React.JSX.Element {
  const ideRef = useRef<IdeHandle>(null);
  const [gradeState, setGradeState] = useState<GradeState>('idle');
  const [report, setReport] = useState<TestReport | null>(null);
  const [computed, setComputed] = useState<ComputedScore | null>(null);
  const [error, setError] = useState<string | null>(null);

  const mergedFiles = useMemo(() => mergeWorkspaceWithSubmission(assignment, submittedFiles), [assignment, submittedFiles]);

  // ponytail: empty submittedFiles means the student submitted no code (payload
  // '{}' or null) OR the codePayload failed to parse (logged in submission-viewer).
  // The merged workspace still seeds the IDE with the instructor template, so
  // without this notice the instructor sees a generic/template IDE and can't
  // tell "no student code" from a rendering bug.
  const noStudentCode = submittedFiles.length === 0;

  // Seed the IDE with the merged workspace on mount.
  //
  // `Ide.setFiles` updates reactive state + the seed snapshot, and
  // `syncFilesToVfs` no-ops until `orchestratorRef.current` is set; once
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
    setComputed(null);
    try {
      // (e.1) Task 7 mapper — Public + Private cases + harness files.
      const { plan, generatedFiles } = buildTestPlan(assignment as unknown as EmceptionAssignmentContent, { mode: 'full' });

      // (e.2) Re-seed IDE with [current workspace, generated harnesses]. The
      // generated harness paths are stable across runs (`functional_<i>_test.cpp`)
      // so de-dupe them against any prior run.
      const currentFiles = (await ideRef.current!.getFiles()) as CodeFile[];
      const harnessPaths = new Set(generatedFiles.map((f) => f.path));
      const merged = [...currentFiles.filter((f: CodeFile) => !harnessPaths.has(f.path)), ...generatedFiles];
      await ideRef.current!.setFiles(merged);

      // (e.3) Run + (e.4) report. Engine contract (Metis #33): tool failures
      // resolve as a `ToolResult` with non-zero exit code; the test report
      // surfaces them as failing cases — `runTests` does NOT reject here.
      const r = (await ideRef.current!.runTests(plan as never)) as TestReport;

      // (f) Compute weighted score via the shared scoring utility.
      // ponytail: passingScore=0 — grader doesn't compute pass/fail; server's
      // GradeSubmissionAsync loads Program.PassingScore for the snapshot.
      const definition: ScoringDefinition = {
        testPlan: { cases: plan.cases as unknown as BackendTestCaseDto[] },
        maxScore,
        passingScore: 0,
      };
      const result = scoreSubmission(definition, r);
      const next: ComputedScore = {
        score: result.score,
        autoFeedback: formatFeedback(r, result.score),
      };
      setReport(r);
      setComputed(next);
      onComputedScore?.(next);
      setGradeState('ready');
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setGradeState('idle');
    }
  }

  return (
    <div data-testid="code-grader-panel" className="flex h-full flex-col gap-3 p-3">
      <div className="flex items-center gap-3">
        <Button type="button" variant="outline" size="sm" onClick={handleRunTests} disabled={gradeState === 'grading'} data-testid="run-tests-button">
          {gradeState === 'grading' ? <Loader2 className="mr-2 h-4 w-4 animate-spin" data-testid="run-tests-spinner" /> : null}
          Run Tests
        </Button>
        {computed && (
          <p className="text-sm font-semibold" data-testid="computed-score">
            Computed score: {computed.score} / {maxScore}
          </p>
        )}
      </div>

      {error && (
        <div role="alert" className="rounded border border-red-500 bg-red-50 p-3 text-sm text-red-800 dark:bg-red-950 dark:text-red-200">
          {error}
        </div>
      )}

      {noStudentCode && (
        <div data-testid="no-student-code" className="rounded border border-amber-500 bg-amber-50 p-3 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-200">
          This submission contains no student code. The IDE shows the assignment template only.
        </div>
      )}

      {report && <TestResultsPanel report={report} maxScore={maxScore} />}

      <div className="min-h-[400px] flex-1 border">
        <Ide ref={ideRef} manifestUrl={manifestUrl} maxScore={maxScore} assignmentToken={submissionId} />
      </div>
    </div>
  );
}
