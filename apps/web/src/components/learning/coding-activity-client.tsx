'use client';

import { useRouter } from '@/i18n/navigation';
import { filesToCodePayload } from '@/lib/coding-assignment/code-payload';
import type { CodingAssignmentContent } from '@/lib/coding-assignment/types';
import {
  submitAssessment,
  type LearnerMutationResult,
} from '@/lib/learner/activity-actions';
import {
  ASSIGNMENT_SAMPLES,
  type CodingLanguage,
  type GradingPlan,
  type IdeHandle,
  type TestReport,
  type WorkspaceConfig,
} from '@game-guild/emception-ui';
import { testing as emceptionTesting } from 'emception';
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
import {
  hasRestorableDraft,
  publicSeedFiles,
  resolveSeed,
  type SeedFile,
} from './resolve-seed';

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
  /** Namespaces the draft-storage token per user (page supplies; optional so
   *  the page can wire it independently — a bare assessmentId keeps the
   *  pre-namespacing keys, still reachable via the legacy fallback read). */
  userId?: string;
  /** Prior submission files for overlay restore; null = no submission yet. */
  submissionFiles?: SeedFile[] | null;
}

/**
 * Map v1 Tests.Public → emception GradingPlan via the shared assignment-plan
 * mapper (standard → stdio case; functional group → doctest case + generated
 * harness the Ide's run-tests handler compiles against the student sources).
 * Private tests never enter the plan (public-only mode) — the server also
 * strips them from the student fetch, so the exclusion is enforced twice.
 */
type WireAssignment = Parameters<typeof emceptionTesting.buildTestPlan>[0];

function publicTestsToGradingPlan(assignment: CodingAssignmentContent): GradingPlan {
  const { plan, generatedFiles } = emceptionTesting.buildTestPlan(
    // Wire shapes identical; the web type declares readonly arrays the mapper
    // does not — the mismatch is declarative only.
    assignment as unknown as WireAssignment,
    { mode: 'public-only' },
  );
  return { cases: plan.cases, generatedFiles };
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

  const language = (assignment.Environment.Language as CodingLanguage | undefined) ?? 'cpp';
  // Draft detection MUST run BEFORE the lazy <Ide> mounts: the IDE's
  // persistence effect writes initial state on mount, so any post-mount
  // probe is trivially true. The ref-init pattern reads storage exactly
  // once per mount — StrictMode's second render sees a non-null ref and
  // skips; a double-MOUNT re-inits but is still pre-Ide-mount (Suspense
  // re-suspends and the restore effect has not run yet either).
  const draftExistsRef = useRef<boolean | null>(null);
  if (draftExistsRef.current === null) {
    draftExistsRef.current = hasRestorableDraft(
      userId ? `${userId}:${assessmentId}` : assessmentId,
      (ASSIGNMENT_SAMPLES[language] ?? ASSIGNMENT_SAMPLES.cpp).workspaceConfig.id,
    );
  }
  const resolved = useMemo(
    () =>
      resolveSeed({
        draftExists: draftExistsRef.current ?? false,
        submissionFiles: submissionFiles ?? null,
        seedFiles: seedFiles.current,
      }),
    // seedFiles.current is a mount-time ref — never reassigned.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [draftExistsRef.current, submissionFiles],
  );

  // Boot config for the IDE: language preset from the assignment (unknown/
  // legacy languages fall back to cpp) with the resolved files swapped in so
  // the FIRST paint already shows the seed/submission overlay. Draft mode
  // resolves to [] → preset sample files (the IDE's restore effect overrides
  // the config files at mount anyway). Passing workspaceConfig also hides the
  // preset picker — students must never switch presets (it would wipe the
  // assignment files).
  const workspaceConfig = useMemo<WorkspaceConfig>(() => {
    const sample = ASSIGNMENT_SAMPLES[language] ?? ASSIGNMENT_SAMPLES.cpp;
    const toConfigFiles = (seed: SeedFile[]): WorkspaceConfig['files'] => {
      const out: WorkspaceConfig['files'] = {};
      for (const { path, content, encoding } of seed) {
        out[path] = { encoding, content };
      }
      return out;
    };
    return {
      ...sample.workspaceConfig,
      // Draft mode resolves to [] (the IDE restores its own state) — still pass
      // the instructor seed so Reset restores the instructor originals, not the
      // preset sample (which would contradict the reset confirm copy).
      files:
        resolved.files.length > 0
          ? toConfigFiles(resolved.files)
          : toConfigFiles(seedFiles.current),
    };
  }, [language, resolved]);

  // Load-order seeding (gated on ideMounted as today: the callback ref flips
  // it in the commit phase, keeping seeding exactly-once):
  // - seed: setFiles seeds both the workspace AND the diff baseline.
  // - submission: setFiles overlays the prior submission, then the baseline is
  //   re-pinned to the INSTRUCTOR seed — submissions are content-diffs vs the
  //   assignment, not vs the overlay.
  // - draft: the IDE restored its own state; the baseline is pinned to the
  //   instructor seed for the same reason (resync-to-current would make a
  //   restore-then-submit-without-edits send an empty payload and lose the
  //   student's work).
  // Image seeds are re-merged in every mode: they are excluded from draft
  // persistence (localStorage quota) and from setFiles (text-only).
  useEffect(() => {
    if (!ideMounted) return;
    const handle = ref.current;
    if (!handle) return;
    const seed = seedFiles.current;
    const seedText = seed.filter((f) => f.encoding === 'text').map(({ path, content }) => ({ path, content }));
    let cancelled = false;
    (async () => {
      if (resolved.mode !== 'draft' && resolved.files.length > 0) {
        await handle.setFiles(
          resolved.files
            .filter((file) => file.encoding === 'text')
            .map(({ path, content }) => ({ path, content })),
        );
      }
      if (resolved.mode === 'submission' || resolved.mode === 'draft') {
        handle.setBaseline(seedText);
      }
      const imageSeeds = seed.filter((file) => file.encoding !== 'text');
      if (imageSeeds.length > 0) {
        const current = await handle.getFiles();
        for (const { path, content, encoding } of imageSeeds) {
          if (current.some((file) => file.path === path)) continue;
          await handle.addFile(path, content, encoding);
        }
      }
      if (cancelled) return;
      for (const { path, modifiable } of seed) {
        if (!modifiable) {
          // per-file Monaco readOnly via Task 5 setFileMeta
          await handle.setFileMeta(path, { modifiable: false });
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [ideMounted, resolved]);

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

  return (
    <>
      <Script src="/coi-serviceworker.js" strategy="beforeInteractive" />
      <form onSubmit={handleSubmit} className="space-y-4">
        <div className="h-[70vh] min-h-[500px]">
          <Suspense fallback={<IdeSkeleton />}>
            <Ide
              ref={attachIde}
              workspaceConfig={workspaceConfig}
              assignmentToken={userId ? `${userId}:${assessmentId}` : assessmentId}
              allowCreateFiles={allowCreateFiles}
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
    </>
  );
}
