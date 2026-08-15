'use client';

import { useRouter } from '@/i18n/navigation';
import { filesToCodePayload } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent, FileEncoding } from '@/lib/coding-assignment/types';
import {
  submitAssessment,
  type LearnerMutationResult,
} from '@/lib/learner/activity-actions';
import {
  ASSIGNMENT_SAMPLES,
  type CodingLanguage,
  type GradingCase,
  type GradingPlan,
  type IdeHandle,
  type TestReport,
  type WorkspaceConfig,
} from '@game-guild/emception-ui';
import { Button } from '@game-guild/ui/components/button';
import Script from 'next/script';
import {
  lazy,
  Suspense,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
  type FormEvent,
} from 'react';
import { PublicTestEstimateBanner } from './public-test-estimate-banner';

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
  encoding: FileEncoding;
  modifiable: boolean;
}> {
  return Object.entries(assignment.Data.Files)
    .filter(([, meta]) => meta.Visibility === 'Public')
    .map(([path, meta]) => ({
      path,
      content: meta.Content,
      encoding: meta.Encoding ?? 'text',
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
  // Gate for the seeding effect: lazy() mounts <Ide> only after its chunk
  // loads, so a mount-time effect reading ref.current races the chunk and
  // sees null. The callback ref flips this flag once the handle exists.
  const [ideMounted, setIdeMounted] = useState(false);
  const attachIde = useCallback((handle: IdeHandle | null) => {
    ref.current = handle;
    setIdeMounted(Boolean(handle));
  }, []);

  const seedFiles = useRef(publicSeedFiles(assignment));
  const gradingPlan = useRef<GradingPlan>(publicTestsToGradingPlan(assignment));
  const allowCreateFiles = assignment.Environment.AllowStudentCreateFiles !== false;
  const maxScore = assignment.Grading.MaxScore;
  // ponytail: PassingScore moved from GradingConfig to Program (T1/T3). Learner context
  // doesn't surface Program.PassingScore yet — default to 60 (the C# default + historical
  // constant) until T13 wires the course-level field through getCourseLearnerContext.
  const passingScore = 60;

  // Boot config for the IDE: language preset from the assignment (unknown/
  // legacy languages fall back to cpp) with the Public files swapped in.
  // Passing workspaceConfig also hides the preset picker — students must
  // never switch presets (it would wipe the assignment files).
  const workspaceConfig = useMemo<WorkspaceConfig>(() => {
    const language = (assignment.Environment.Language as CodingLanguage | undefined) ?? 'cpp';
    const sample = ASSIGNMENT_SAMPLES[language] ?? ASSIGNMENT_SAMPLES.cpp;
    const files: WorkspaceConfig['files'] = {};
    for (const { path, content, encoding } of seedFiles.current) {
      files[path] = { encoding, content };
    }
    return {
      ...sample.workspaceConfig,
      // Fall back to the preset files when the assignment has no Public files.
      files: Object.keys(files).length > 0 ? files : sample.workspaceConfig.files,
    };
    // seedFiles.current is a mount-time ref — never reassigned.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [assignment.Environment.Language]);

  // Seed IDE with Public files + apply readOnly meta for non-modifiable files.
  // setFiles/setFileMeta mutate reactive state first; VFS sync happens later
  // when doBootstrap runs syncFilesToVfs(filesRef.current). Safe to call pre-boot.
  // Gate on ideMounted (flipped by attachIde in the commit phase): when the
  // lazy chunk resolves synchronously the ref can already be set at first
  // effect-run — keying the run on the flag keeps seeding exactly-once.
  useEffect(() => {
    if (!ideMounted) return;
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
  }, [ideMounted]);

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
        <div {...wrapperDataAttrs} className="h-[70vh] min-h-[500px]">
          <Suspense fallback={<IdeSkeleton />}>
            <Ide
              ref={attachIde}
              workspaceConfig={workspaceConfig}
              assignmentToken={assessmentId}
              testPlan={gradingPlan.current.cases.length > 0 ? gradingPlan.current : undefined}
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
