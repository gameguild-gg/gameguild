'use client';

import React, { useMemo, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Loader2 } from 'lucide-react';

import {
  AssessmentGrader,
  mergeWorkspaceWithSubmission,
  type ComputedScore,
} from '@/components/learning/assessment-grading/assessment-grader';
import { gradeSubmission } from '@/lib/learning/grade-action';
import { useLearningBase } from '@/lib/learning/use-learning-base';
import type { CodeFile } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/client';
import { Button } from '@game-guild/ui/components/button';

import { composeFeedback } from './compose-feedback';

type GradeState = 'idle' | 'ready' | 'posting' | 'done';

export interface GradeClientProps {
  courseSlug: string;
  assessmentId: string;
  assessmentSlug: string;
  submissionId: string;
  /** Full assignment (Public + Private tests + all files). */
  assignment: CodingAssignmentContent;
  /** Submitted student files parsed server-side from the immutable submission. */
  submittedFiles: CodeFile[];
  maxScore: number;
  manifestUrl: string;
}

/** Re-exported for callers that previously used the grade-client helper. */
export { mergeWorkspaceWithSubmission };

export function GradeClient({
  courseSlug,
  assessmentId: _assessmentId,
  assessmentSlug,
  submissionId,
  assignment,
  submittedFiles,
  maxScore,
  manifestUrl,
}: GradeClientProps): React.JSX.Element {
  const learningBase = useLearningBase();
  const router = useRouter();
  const [gradeState, setGradeState] = useState<GradeState>('idle');
  const [score, setScore] = useState<number | null>(null);
  const [autoFeedback, setAutoFeedback] = useState('');
  const [overallComment, setOverallComment] = useState('');
  const [perFileComments, setPerFileComments] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  const commentableFiles = useMemo(
    () =>
      mergeWorkspaceWithSubmission(assignment, submittedFiles).filter(
        (file) => assignment.Data.Files[file.path]?.Visibility !== 'Private',
      ),
    [assignment, submittedFiles],
  );

  function handleComputedScore(result: ComputedScore) {
    setScore(result.score);
    setAutoFeedback(result.autoFeedback);
    setError(null);
    setGradeState('ready');
  }

  async function handleConfirm() {
    if (gradeState !== 'ready' || score === null) return;
    setGradeState('posting');
    setError(null);
    try {
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
        `${learningBase}/courses/${encodeURIComponent(courseSlug)}/assessments/${encodeURIComponent(assessmentSlug)}`,
      );
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : String(submitError));
      setGradeState('ready');
    }
  }

  return (
    <div className="space-y-4 p-4">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Grade submission</h1>
          <p className="text-muted-foreground text-sm">
            Run the full test plan against the student&apos;s code, then confirm the grade.
          </p>
        </div>
        <Button
          type="button"
          onClick={handleConfirm}
          disabled={gradeState !== 'ready'}
          data-testid="confirm-grade-button"
        >
          {gradeState === 'posting' ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" data-testid="confirm-spinner" />
          ) : null}
          Confirm grade
        </Button>
      </div>

      {error ? (
        <div
          role="alert"
          data-testid="grade-error"
          className="rounded border border-red-500 bg-red-50 p-3 text-sm text-red-800 dark:bg-red-950 dark:text-red-200"
        >
          {error}
        </div>
      ) : null}

      {score !== null ? (
        <div data-testid="grade-result" className="space-y-2">
          <p className="text-lg font-semibold" data-testid="grade-score">
            Computed score: {score} / {maxScore}
          </p>
        </div>
      ) : null}

      <div className="border h-[70vh] min-h-[500px]">
        <AssessmentGrader
          assignment={assignment}
          submittedFiles={submittedFiles}
          maxScore={maxScore}
          manifestUrl={manifestUrl}
          submissionId={submissionId}
          onComputedScore={handleComputedScore}
        />
      </div>

      <div className="space-y-3">
        <h2 className="text-lg font-semibold">Per-file comments</h2>
        {commentableFiles.map((file) => (
          <div key={file.path} className="space-y-1">
            <label
              htmlFor={`comment-${file.path}`}
              className="text-sm font-medium"
              data-testid={`comment-label-${file.path}`}
            >
              {file.path}
            </label>
            <textarea
              id={`comment-${file.path}`}
              value={perFileComments[file.path] ?? ''}
              onChange={(event) =>
                setPerFileComments((previous) => ({
                  ...previous,
                  [file.path]: event.target.value,
                }))
              }
              data-testid={`comment-${file.path}`}
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
          onChange={(event) => setOverallComment(event.target.value)}
          data-testid="overall-comment"
          rows={3}
          className="w-full rounded border p-2 text-sm"
          placeholder="Overall feedback for the student"
        />
      </div>
    </div>
  );
}
