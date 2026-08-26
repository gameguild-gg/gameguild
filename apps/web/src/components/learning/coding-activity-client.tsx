'use client';

import { useRouter } from '@/i18n/navigation';
import { filesToCodePayload } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/types';
import {
  submitAssessment,
  type LearnerMutationResult,
} from '@/lib/learner/activity-actions';
import { buildAssessmentExecutionPlan } from '@game-guild/emception-ui/assessment/plan';
import type {
  AssessmentRunResult,
  AssessmentSession,
  CodingAssessmentEditorProps,
} from '@game-guild/emception-ui/assessment/editor';
import { createAssessmentWorkspaceConfig, type CodingLanguage } from '@game-guild/emception-ui/assessment/presets';
import { workspaceStorageKey } from '@game-guild/emception-ui/assessment/storage';
import { Button } from '@game-guild/ui/components/button';
import type { TestReport, WorkspaceConfig } from 'emception';
import Script from 'next/script';
import {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ComponentType,
  type FormEvent,
} from 'react';
import { PublicTestEstimateBanner } from './public-test-estimate-banner';
import { publicSeedFiles, type SeedFile } from './resolve-seed';

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
  /** Namespaces the draft-storage token per user (page supplies; optional so
   *  the page can wire it independently — a bare assessmentId keeps the
   *  pre-namespacing keys, still reachable via the legacy fallback read). */
  userId?: string;
  /** Prior submission files for overlay restore; null = no submission yet. */
  submissionFiles?: SeedFile[] | null;
}

export function CodingActivityClient({
  assessmentId,
  enrollmentId,
  slug,
  assignment,
  manifestUrl,
  userId,
  submissionFiles,
}: CodingActivityClientProps) {
  const sessionRef = useRef<AssessmentSession | null>(null);
  const router = useRouter();
  const [submitting, setSubmitting] = useState(false);
  const [report, setReport] = useState<TestReport | null>(null);
  const [result, setResult] = useState<LearnerMutationResult | null>(null);
  const [sessionReady, setSessionReady] = useState(false);
  const [Editor, setEditor] = useState<ComponentType<CodingAssessmentEditorProps> | null>(null);
  const [editorLoadError, setEditorLoadError] = useState<string | null>(null);

  // The neutral IDE owns browser-only APIs (Monaco, Worker and WASM). Import it
  // after hydration so the server never evaluates its module graph. This avoids
  // Next's app-page loadable alias while keeping the IDE outside SSR.
  useEffect(() => {
    let active = true;

    void import('@game-guild/emception-ui/assessment/editor')
      .then(({ CodingAssessmentEditor }) => {
        if (active) setEditor(() => CodingAssessmentEditor);
      })
      .catch((error: unknown) => {
        if (!active) return;
        setEditorLoadError(error instanceof Error ? error.message : 'Unable to load the coding editor.');
      });

    return () => {
      active = false;
    };
  }, []);

  const receiveSession = useCallback((session: AssessmentSession) => {
    sessionRef.current = session;
    setSessionReady(true);
  }, []);
  const receiveRunResult = useCallback((runResult: AssessmentRunResult) => {
    setReport(runResult.report);
  }, []);

  const seedFiles = useMemo(() => publicSeedFiles(assignment), [assignment]);
  const publicPlan = useMemo(
    () => buildAssessmentExecutionPlan(assignment, 'public').plan,
    [assignment],
  );
  const maxScore = assignment.Grading.MaxScore;
  // ponytail: PassingScore moved from GradingConfig to Program (T1/T3). Learner context
  // doesn't surface Program.PassingScore yet — default to 60 (the C# default + historical
  // constant) until T13 wires the course-level field through getCourseLearnerContext.
  const passingScore = 60;

  const language = (assignment.Environment.Language as CodingLanguage | undefined) ?? 'cpp';
  // The host template supplies language-specific compiler/runtime settings.
  // Its files are the public seed overlaid by a previous server submission;
  // the neutral IDE restores a newer local draft from workspaceStorageKey.
  const workspaceConfig = useMemo<WorkspaceConfig>(() => {
    const files = new Map(seedFiles.map(({ path, content, encoding }) => [path, { encoding, content }]));
    for (const file of submissionFiles ?? []) {
      files.set(file.path, { encoding: file.encoding, content: file.content });
    }
    return createAssessmentWorkspaceConfig(language, Object.fromEntries(files));
  }, [language, seedFiles, submissionFiles]);

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
      // The assessment session returns only editable public changes plus
      // permitted student-created text files.
      const modified = (await sessionRef.current?.getSubmissionDelta()) ?? [];
      const fd = new FormData();
      fd.set('assessmentId', assessmentId);
      fd.set('enrollmentId', enrollmentId);
      fd.set('modality', 'Code');
      // Wire shape (Metis #29): Record<path, {content, encoding: 'text'}>
      fd.set('response', filesToCodePayload([...modified]));
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
    <>
      <Script src="/coi-serviceworker.js" strategy="beforeInteractive" />
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="h-[70vh] min-h-[500px]">
          {Editor ? (
            <Editor
              mode="learner"
              definition={assignment}
              workspaceConfig={workspaceConfig}
              workspaceStorageKey={workspaceStorageKey(
                userId ? `${userId}:${assessmentId}` : assessmentId,
                workspaceConfig.id,
              )}
              manifestUrl={manifestUrl}
              maxScore={maxScore}
              passingScore={passingScore}
              onSessionReady={receiveSession}
              onRunResult={receiveRunResult}
            />
          ) : editorLoadError ? (
            <p role="alert" className="text-sm text-destructive">
              {editorLoadError}
            </p>
          ) : (
            <IdeSkeleton />
          )}
        </div>
      {report ? (
        <PublicTestEstimateBanner
          report={report}
          plan={publicPlan}
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
        <Button type="submit" disabled={submitting || !sessionReady}>
          {submitting ? 'Submitting…' : 'Submit'}
        </Button>
      </div>
    </form>
    </>
  );
}
