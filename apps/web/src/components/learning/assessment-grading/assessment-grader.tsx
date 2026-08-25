'use client';

import React, { useCallback, useMemo, useState } from 'react';
import {
  CodingAssessmentEditor,
  type AssessmentRunResult,
  type CodingLanguage,
} from '@game-guild/emception-ui';
import { createAssessmentWorkspaceConfig } from '@game-guild/emception-ui/assessment/presets';

import { formatFeedback } from '@/lib/emception/scoring';
import type { CodeFile } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';

export interface ComputedScore {
  score: number;
  autoFeedback: string;
}

export interface AssessmentGraderProps {
  assignment: CodingAssignmentContent;
  submittedFiles: CodeFile[];
  maxScore: number;
  manifestUrl: string;
  submissionId?: string;
  onComputedScore?: (result: ComputedScore) => void;
}

function buildPrivatePaths(assignment: CodingAssignmentContent): Set<string> {
  return new Set(
    Object.entries(assignment.Data.Files)
      .filter(([, file]) => file.Visibility === 'Private')
      .map(([path]) => path),
  );
}

/** Merge a frozen instructor workspace with student files without trusting private-path overrides. */
export function mergeWorkspaceWithSubmission(
  assignment: CodingAssignmentContent,
  submittedFiles: CodeFile[],
): CodeFile[] {
  const privatePaths = buildPrivatePaths(assignment);
  const merged = new Map<string, string>();

  for (const [path, file] of Object.entries(assignment.Data.Files)) {
    merged.set(path, file.Content);
  }
  for (const file of submittedFiles) {
    if (privatePaths.has(file.path)) {
      console.warn(
        `[grade] Submission attempted to override private workspace file ${file.path}; skipping`,
      );
      continue;
    }
    merged.set(file.path, file.content);
  }

  return [...merged].map(([path, content]) => ({ path, content }));
}

/**
 * Composes the GameGuild assessment adapter over the neutral IDE. Private
 * files are omitted from the editor workspace; the session overlays them only
 * while running trusted full tests in the instructor's browser.
 */
export function AssessmentGrader({
  assignment,
  submittedFiles,
  maxScore,
  manifestUrl,
  submissionId,
  onComputedScore,
}: AssessmentGraderProps): React.JSX.Element {
  const [computed, setComputed] = useState<ComputedScore | null>(null);
  const mergedFiles = useMemo(
    () => mergeWorkspaceWithSubmission(assignment, submittedFiles),
    [assignment, submittedFiles],
  );
  const workspaceConfig = useMemo(() => {
    const privatePaths = buildPrivatePaths(assignment);
    const files = Object.fromEntries(
      mergedFiles
        .filter((file) => !privatePaths.has(file.path))
        .map((file) => [
          file.path,
          {
            encoding: assignment.Data.Files[file.path]?.Encoding ?? 'text',
            content: file.content,
          },
        ]),
    );
    const language = (assignment.Environment.Language || 'cpp') as CodingLanguage;
    return createAssessmentWorkspaceConfig(language, files);
  }, [assignment, mergedFiles]);
  const noStudentCode = submittedFiles.length === 0;

  const handleRunResult = useCallback(
    (result: AssessmentRunResult) => {
      const next = {
        score: result.score.score,
        autoFeedback: formatFeedback(result.report, result.score.score),
      };
      setComputed(next);
      onComputedScore?.(next);
    },
    [onComputedScore],
  );

  return (
    <div data-testid="code-grader-panel" className="flex h-full flex-col gap-3 p-3">
      {computed ? (
        <p className="text-sm font-semibold" data-testid="computed-score">
          Computed score: {computed.score} / {maxScore}
        </p>
      ) : null}

      {noStudentCode ? (
        <div
          data-testid="no-student-code"
          className="rounded border border-amber-500 bg-amber-50 p-3 text-sm text-amber-800 dark:bg-amber-950 dark:text-amber-200"
        >
          This submission contains no student code. The editor shows the assignment template only.
        </div>
      ) : null}

      <div className="min-h-[400px] flex-1 border">
        <CodingAssessmentEditor
          mode="grader"
          definition={assignment}
          manifestUrl={manifestUrl}
          title="Grade submission"
          workspaceConfig={workspaceConfig}
          workspaceStorageKey={submissionId ? `emception:grader:${submissionId}` : undefined}
          enableWorkspace={false}
          maxScore={maxScore}
          passingScore={0}
          onRunResult={handleRunResult}
        />
      </div>
    </div>
  );
}
