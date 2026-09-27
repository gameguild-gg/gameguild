'use client';

import {
  withdrawTestingProjectApplication,
  type TestingEventActionResult,
} from '@/lib/testing-lab/events-actions';
import type {
  TestingLabQuestionnaireOutput,
  TestingLabQuestionnaireSchema,
  TestingLabTestingProjectBrief,
} from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, Check, CheckCircle2, ChevronLeft, ChevronRight, Gamepad2, Loader2, Save } from 'lucide-react';
import { Link } from '@/i18n/navigation';
import Image from 'next/image';
import { useState, useTransition } from 'react';
import { QuestionnaireBuilder } from './questionnaire-builder';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

interface ProjectVersionOption {
  id: string;
  projectId: string;
  projectTitle: string;
  imageUrl?: string | null;
  versionNumber: string;
  status: string;
}
interface CurrentApplication {
  id: string;
  projectId?: string | null;
  projectVersionId?: string | null;
  preferredAvailability?: string | null;
  status?: string | null;
  decisionRationale?: string | null;
  brief?: TestingLabTestingProjectBrief;
  eventApplicationResponse?: TestingLabQuestionnaireOutput;
  feedbackQuestionnaire?: TestingLabQuestionnaireSchema;
  rulesAcceptedAt?: string | null;
  submittedAssetReferenceIds?: string[] | null;
  submissionVersionPolicy?: string | null;
}

const STEPS = ['Game & build', 'Test brief', 'Feedback form', 'Event questions', 'Review'] as const;
const emptyBrief: TestingLabTestingProjectBrief = {
  testObjective: '',
  installationAndAccess: '',
  testTasks: [],
  controls: '',
  knownLimitations: '',
  links: [],
};

function ResultMessage({ result }: { result: TestingEventActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
      {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
      <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
    </Alert>
  );
}

function lines(value?: string[] | null) {
  return (value ?? []).join('\n');
}

function parseLines(value: string) {
  return value.split(/\r?\n/).map((line) => line.trim()).filter(Boolean);
}

async function saveDraftInBrowser(input: {
  eventId: string;
  projectId: string;
  applicationId?: string;
  projectVersionId?: string;
  brief: TestingLabTestingProjectBrief;
  feedbackQuestionnaire: TestingLabQuestionnaireSchema;
  eventApplicationResponse: TestingLabQuestionnaireOutput;
  acceptedRules: boolean;
  preferredAvailability: string;
  submittedAssetReferenceIds: string[];
  intent: 'save' | 'submit';
}): Promise<TestingEventActionResult<CurrentApplication>> {
  try {
    const response = await fetch('/api/testing-lab/applications/draft', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'same-origin',
      body: JSON.stringify(input),
    });
    const result = await response.json().catch(() => null) as TestingEventActionResult<CurrentApplication> | null;
    if (result && typeof result.success === 'boolean') return result;
    return { success: false, error: 'The application draft could not be saved.' };
  } catch {
    return { success: false, error: 'The application draft could not be saved.' };
  }
}

function ApplicationWizard({
  eventId,
  application,
  projectVersions,
  initialProjectId,
  applicationSchema,
  generalRules,
  candidateInstructions,
  requiresFeedback,
  acceptsApplications,
}: {
  eventId: string;
  application?: CurrentApplication;
  projectVersions: ProjectVersionOption[];
  initialProjectId?: string;
  applicationSchema?: TestingLabQuestionnaireSchema | null;
  generalRules?: string | null;
  candidateInstructions?: string | null;
  requiresFeedback: boolean;
  acceptsApplications: boolean;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const [applicationId, setApplicationId] = useState(application?.id ?? '');
  const [status, setStatus] = useState(application?.status ?? 'Draft');
  const [draftProjectId, setDraftProjectId] = useState(application?.projectId ?? '');
  const matchingVersions = draftProjectId
    ? projectVersions.filter((version) => version.projectId === draftProjectId)
    : projectVersions;
  const eligibleVersions = matchingVersions.filter((version) => ['ReadyForTesting', 'Released'].includes(version.status));
  const initialVersion = application?.projectVersionId && eligibleVersions.some((version) => version.id === application.projectVersionId)
    ? application.projectVersionId
    : eligibleVersions.find((version) => version.projectId === initialProjectId)?.id ?? '';
  const [selectedVersionId, setSelectedVersionId] = useState(initialVersion);
  const selectedVersion = projectVersions.find((version) => version.id === selectedVersionId);
  const gameGroups = Array.from(
    eligibleVersions
      .reduce((groups, version) => {
        const group = groups.get(version.projectId);
        if (group) group.versions.push(version);
        else groups.set(version.projectId, { projectId: version.projectId, projectTitle: version.projectTitle, imageUrl: version.imageUrl, versions: [version] });
        return groups;
      }, new Map<string, { projectId: string; projectTitle: string; imageUrl?: string | null; versions: ProjectVersionOption[] }>())
      .values(),
  );
  const projectId = draftProjectId || selectedVersion?.projectId || '';
  const [step, setStep] = useState(0);
  const [brief, setBrief] = useState<TestingLabTestingProjectBrief>(application?.brief ?? emptyBrief);
  const [feedbackQuestionnaire, setFeedbackQuestionnaire] = useState<TestingLabQuestionnaireSchema>(
    application?.feedbackQuestionnaire ?? { title: 'Playtest feedback', questions: [] },
  );
  const [eventResponses, setEventResponses] = useState<TestingLabQuestionnaireOutput>(
    application?.eventApplicationResponse ?? { answers: [] },
  );
  const [acceptedRules, setAcceptedRules] = useState(Boolean(application?.rulesAcceptedAt));
  const [preferredAvailability, setPreferredAvailability] = useState(application?.preferredAvailability ?? '');
  const [assetIds, setAssetIds] = useState(lines(application?.submittedAssetReferenceIds));
  const editable = acceptsApplications && (status === 'Draft' || status === 'Pending');
  const canWithdraw = Boolean(applicationId) && ['Draft', 'Pending', 'UnderReview', 'Waitlisted'].includes(status);
  const versionMutable = status === 'Draft' || application?.submissionVersionPolicy !== 'ReleasedImmutable';

  function persist(intent: 'save' | 'submit', nextStep?: number) {
    startTransition(async () => {
      const next = await saveDraftInBrowser({
        eventId,
        projectId,
        applicationId: applicationId || undefined,
        projectVersionId: selectedVersionId || undefined,
        brief,
        feedbackQuestionnaire,
        eventApplicationResponse: eventResponses,
        acceptedRules,
        preferredAvailability,
        submittedAssetReferenceIds: parseLines(assetIds),
        intent,
      });
      setResult(next);
      if (!next.success) return;
      if (next.data?.id) setApplicationId(next.data.id);
      if (next.data?.projectId) setDraftProjectId(next.data.projectId);
      if (next.data?.status) setStatus(next.data.status);
      if (nextStep !== undefined) setStep(nextStep);
    });
  }

  function withdraw() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('applicationId', applicationId);
    startTransition(async () => {
      try {
        setResult(await withdrawTestingProjectApplication(formData));
      } catch (error) {
        setResult({
          success: false,
          error: error instanceof Error ? error.message : 'The Testing Lab operation failed.',
        });
      }
    });
  }

  if (!editable) {
    return (
      <section className="space-y-3 rounded-md border p-4">
        <Badge variant="outline">{status}</Badge>
        <p className="text-sm text-muted-foreground">This application package is frozen for review and historical integrity.</p>
        {application?.decisionRationale ? <Alert><AlertCircle className="size-4" /><AlertDescription>{application.decisionRationale}</AlertDescription></Alert> : null}
        {canWithdraw ? (
          <Button type="button" variant="ghost" className="text-destructive" disabled={pending} onClick={withdraw}>
            Withdraw application
          </Button>
        ) : null}
        <ResultMessage result={result} />
      </section>
    );
  }

  return (
    <section className={`space-y-5 ${step === 0 ? '' : 'rounded-md border bg-card p-4'}`}>
      {applicationId ? (
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Badge variant={status === 'Draft' ? 'secondary' : 'outline'}>{status}</Badge>
            <span className="text-xs text-muted-foreground">Application {applicationId.slice(0, 8)}</span>
          </div>
          <Button type="button" variant="ghost" size="sm" disabled={pending || !projectId} onClick={() => persist('save')}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Save className="mr-2 size-4" />}
            Save progress
          </Button>
        </div>
      ) : null}

      <div className="space-y-2" aria-label="Application progress">
        <div className="flex items-center justify-between gap-3 text-xs">
          <span className="font-medium text-foreground">{STEPS[step]}</span>
          <span className="text-muted-foreground">Step {step + 1} of {STEPS.length}</span>
        </div>
        <div className="grid grid-cols-5 gap-1" aria-hidden="true">
          {STEPS.map((label, index) => (
            <span key={label} className={`h-1 rounded-full ${index <= step ? 'bg-primary' : 'bg-muted'}`} />
          ))}
        </div>
      </div>

      {step === 0 ? (
        <div className="space-y-4">
          <div className="flex items-end justify-between gap-3">
            <div>
              <h3 className="font-medium">Choose your game</h3>
              <p className="mt-1 text-sm text-muted-foreground">Select the build you want players to test.</p>
            </div>
            {gameGroups.length > 0 ? <span className="shrink-0 text-xs text-muted-foreground">{gameGroups.length} {gameGroups.length === 1 ? 'game' : 'games'}</span> : null}
          </div>

          {gameGroups.length > 0 ? (
            <div role="radiogroup" aria-label="Choose a game build" className="space-y-3">
              {gameGroups.map((game) => {
                const gameIsSelected = game.versions.some((version) => version.id === selectedVersionId);
                return (
                  <section key={game.projectId} className={`overflow-hidden rounded-xl border transition-colors ${gameIsSelected ? 'border-primary bg-primary/[0.04]' : 'border-border bg-card/50'}`}>
                    <div className="flex items-center gap-3 px-3 py-3 sm:px-4">
                      <div className="relative size-12 shrink-0 overflow-hidden rounded-lg bg-muted">
                        {game.imageUrl ? (
                          <Image src={game.imageUrl} alt="" fill sizes="48px" unoptimized className="object-cover" />
                        ) : (
                          <div className="grid size-full place-items-center text-muted-foreground"><Gamepad2 className="size-5" aria-hidden="true" /></div>
                        )}
                      </div>
                      <div className="min-w-0 flex-1">
                        <h4 className="truncate font-semibold">{game.projectTitle}</h4>
                        <p className="text-xs text-muted-foreground">{game.versions.length} eligible {game.versions.length === 1 ? 'build' : 'builds'}</p>
                      </div>
                      {gameIsSelected ? <Check className="size-5 shrink-0 text-primary" aria-hidden="true" /> : null}
                    </div>
                    <div className="space-y-2 border-t border-border/70 p-2 sm:p-3">
                      {game.versions.map((version) => {
                        const selected = version.id === selectedVersionId;
                        const eligibility = version.status === 'Released' ? 'Released' : 'Ready for testing';
                        return (
                          <label key={version.id} className="block cursor-pointer">
                            <input
                              type="radio"
                              name={`testing-project-version-${eventId}`}
                              value={version.id}
                              checked={selected}
                              disabled={!versionMutable}
                              onChange={() => setSelectedVersionId(version.id)}
                              aria-label={`${game.projectTitle} · ${version.versionNumber} · ${eligibility}`}
                              className="peer sr-only"
                            />
                            <span className={`flex min-h-12 items-center justify-between gap-3 rounded-lg border px-3 py-2 transition-colors peer-focus-visible:outline-none peer-focus-visible:ring-2 peer-focus-visible:ring-ring ${selected ? 'border-primary/70 bg-primary/10' : 'border-transparent bg-background/60 hover:border-border hover:bg-background'} ${!versionMutable ? 'cursor-not-allowed opacity-60' : ''}`}>
                              <span className="min-w-0">
                                <span className="font-medium">{version.versionNumber}</span>
                                <span className={`ml-2 inline-flex rounded-full px-2 py-0.5 text-[11px] font-medium ${version.status === 'Released' ? 'bg-muted text-muted-foreground' : 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300'}`}>{eligibility}</span>
                              </span>
                              {selected ? <Check className="size-4 shrink-0 text-primary" aria-hidden="true" /> : <span className="size-4 shrink-0 rounded-full border border-muted-foreground/50" aria-hidden="true" />}
                            </span>
                          </label>
                        );
                      })}
                    </div>
                  </section>
                );
              })}
            </div>
          ) : (
            <div className="flex flex-col items-center gap-3 rounded-xl border border-dashed p-6 text-center">
              <div className="grid size-10 place-items-center rounded-full bg-muted text-muted-foreground"><Gamepad2 className="size-5" aria-hidden="true" /></div>
              <div className="space-y-1">
                <p className="font-medium">No test-ready builds yet</p>
                <p className="max-w-sm text-sm text-muted-foreground">Publish a build as Ready for Testing or Released, then come back to submit it here.</p>
              </div>
              <Link href="/projects" className={buttonVariants({ variant: 'outline', size: 'sm' })}>Open your projects</Link>
            </div>
          )}
        </div>
      ) : null}

      {step === 1 ? (
        <div className="space-y-4">
          {candidateInstructions ? <Alert><AlertDescription>{candidateInstructions}</AlertDescription></Alert> : null}
          <div className="space-y-2"><Label htmlFor={`objective-${applicationId}`}>Test objective</Label><Textarea id={`objective-${applicationId}`} rows={3} value={brief.testObjective ?? ''} onChange={(event) => setBrief({ ...brief, testObjective: event.currentTarget.value })} /></div>
          <div className="space-y-2"><Label htmlFor={`install-${applicationId}`}>Installation and access</Label><Textarea id={`install-${applicationId}`} rows={3} value={brief.installationAndAccess ?? ''} onChange={(event) => setBrief({ ...brief, installationAndAccess: event.currentTarget.value })} /></div>
          <div className="space-y-2"><Label htmlFor={`tasks-${applicationId}`}>Test tasks (one per line)</Label><Textarea id={`tasks-${applicationId}`} rows={4} value={lines(brief.testTasks)} onChange={(event) => setBrief({ ...brief, testTasks: parseLines(event.currentTarget.value) })} /></div>
          <div className="space-y-2"><Label htmlFor={`controls-${applicationId}`}>Controls</Label><Textarea id={`controls-${applicationId}`} rows={2} value={brief.controls ?? ''} onChange={(event) => setBrief({ ...brief, controls: event.currentTarget.value })} /></div>
          <div className="space-y-2"><Label htmlFor={`limitations-${applicationId}`}>Known limitations</Label><Textarea id={`limitations-${applicationId}`} rows={2} value={brief.knownLimitations ?? ''} onChange={(event) => setBrief({ ...brief, knownLimitations: event.currentTarget.value })} /></div>
          <div className="space-y-2"><Label htmlFor={`links-${applicationId}`}>Links (one absolute URL per line)</Label><Textarea id={`links-${applicationId}`} rows={2} value={lines(brief.links)} onChange={(event) => setBrief({ ...brief, links: parseLines(event.currentTarget.value) })} /></div>
          <div className="space-y-2"><Label htmlFor={`assets-${applicationId}`}>Existing asset reference IDs (optional, one per line)</Label><Textarea id={`assets-${applicationId}`} rows={2} value={assetIds} onChange={(event) => setAssetIds(event.currentTarget.value)} /></div>
        </div>
      ) : null}

      {step === 2 ? (
        <div className="space-y-3">
          <div><h3 className="font-medium">Developer feedback questionnaire</h3><p className="text-sm text-muted-foreground">Testers answer this immutable revision after assignment.</p></div>
          <QuestionnaireBuilder value={feedbackQuestionnaire} onChange={setFeedbackQuestionnaire} required={requiresFeedback} />
        </div>
      ) : null}

      {step === 3 ? (
        <div className="space-y-4">
          <QuestionnaireFieldset schema={applicationSchema} value={eventResponses} onChange={setEventResponses} onComplete={() => persist('save', 4)} submitLabel="Review application" description="These fields were defined by the event organizer and are frozen for this event." />
          <Button type="button" variant="outline" onClick={() => setStep(2)}><ChevronLeft className="mr-2 size-4" />Back to feedback form</Button>
        </div>
      ) : null}

      {step === 4 ? (
        <div className="space-y-4">
          <div className="rounded-md bg-muted/40 p-4 text-sm">
            <p className="font-medium">{selectedVersion?.projectTitle} · {selectedVersion?.versionNumber}</p>
            <p className="mt-1 text-muted-foreground">{brief.testTasks?.length ?? 0} test tasks · {feedbackQuestionnaire.questions?.length ?? 0} developer questions</p>
          </div>
          <div className="space-y-2"><Label htmlFor={`availability-${applicationId}`}>Preferred availability</Label><Textarea id={`availability-${applicationId}`} rows={2} value={preferredAvailability} onChange={(event) => setPreferredAvailability(event.currentTarget.value)} /></div>
          <div className="max-h-48 overflow-y-auto rounded-md border p-3 text-sm leading-6 whitespace-pre-wrap">{generalRules || 'Event rules are unavailable.'}</div>
          <label className="flex items-start gap-3 text-sm"><input className="mt-1 size-4" type="checkbox" checked={acceptedRules} onChange={(event) => setAcceptedRules(event.currentTarget.checked)} /><span>I have read and accept the frozen rules for this Testing Lab event.</span></label>
          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="outline" onClick={() => setStep(3)}><ChevronLeft className="mr-2 size-4" />Back</Button>
            <Button type="button" disabled={pending || !acceptedRules || !selectedVersionId} onClick={() => persist(status === 'Draft' ? 'submit' : 'save')}>
              {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
              {status === 'Draft' ? 'Submit for review' : 'Update pending application'}
            </Button>
          </div>
        </div>
      ) : null}

      {step === 0 ? (
        <div className="flex items-center justify-between gap-3 border-t pt-4">
          <p aria-live="polite" aria-atomic="true" className="min-w-0 truncate text-sm text-muted-foreground">
            {selectedVersion ? <><span className="font-medium text-foreground">{selectedVersion.projectTitle}</span><span> · {selectedVersion.versionNumber}</span></> : 'Choose a build to continue.'}
          </p>
          <Button type="button" className="shrink-0" disabled={pending || !eligibleVersions.some((version) => version.id === selectedVersionId)} onClick={() => persist('save', 1)}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Continue<ChevronRight className="ml-2 size-4" />
          </Button>
        </div>
      ) : step < 3 ? (
        <div className="flex items-center justify-between gap-2 border-t pt-4">
          <Button type="button" variant="outline" onClick={() => setStep((current) => Math.max(0, current - 1))}><ChevronLeft className="mr-2 size-4" />Previous</Button>
          <Button type="button" disabled={pending} onClick={() => persist('save', step + 1)}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Save and continue<ChevronRight className="ml-2 size-4" />
          </Button>
        </div>
      ) : null}

      {canWithdraw ? (
        <Button type="button" variant="ghost" className="text-destructive" disabled={pending} onClick={withdraw}>Withdraw application</Button>
      ) : null}
      <ResultMessage result={result} />
    </section>
  );
}

export function TestingProjectApplication({
  eventId,
  isAuthenticated,
  acceptsApplications,
  projectVersions,
  application,
  applications,
  initialProjectId,
  selectedApplicationId,
  applicationSchema,
  generalRules,
  candidateInstructions,
  requiresFeedback = false,
  signInReturnUrl,
}: {
  eventId: string;
  isAuthenticated: boolean;
  acceptsApplications: boolean;
  projectVersions: ProjectVersionOption[];
  application?: CurrentApplication;
  applications?: CurrentApplication[];
  initialProjectId?: string;
  selectedApplicationId?: string;
  applicationSchema?: TestingLabQuestionnaireSchema | null;
  generalRules?: string | null;
  candidateInstructions?: string | null;
  requiresFeedback?: boolean;
  signInReturnUrl?: string;
}) {
  const [lastAuthenticatedData] = useState(() =>
    isAuthenticated ? { application, applications, initialProjectId, projectVersions } : null,
  );

  // A Server Action can cause Next to merge a refreshed public RSC payload
  // without its request cookies. Keep the last verified private projection for
  // this mounted wizard so a saved draft does not disappear mid-flow. Every
  // mutation still performs its authorization and eligibility checks in the API.
  const applicationData = isAuthenticated
    ? { application, applications, initialProjectId, projectVersions }
    : lastAuthenticatedData;

  if (!applicationData) {
    if (!acceptsApplications) {
      return <p className="text-sm text-muted-foreground">Game submissions are closed for this event. Browse the Testing Lab for another opportunity.</p>;
    }
    const redirectTo = signInReturnUrl ?? `/testing-lab/events/${eventId}#join`;
    const signInHref = `/sign-in?redirectTo=${encodeURIComponent(redirectTo)}`;
    return (
      <div className="space-y-2">
        <Link href={signInHref} className={buttonVariants({ className: 'w-full sm:w-auto' })}>Sign in or create a free account</Link>
        <p className="text-xs text-muted-foreground">You’ll return to this event to choose a build and complete your application.</p>
      </div>
    );
  }

  const currentApplications = applicationData.applications ?? (applicationData.application ? [applicationData.application] : []);
  const activeProjectIds = new Set(currentApplications.filter((item) => !['Rejected', 'Withdrawn'].includes(item.status ?? '')).map((item) => item.projectId).filter((id): id is string => Boolean(id)));
  const availableVersions = applicationData.projectVersions.filter((version) => !activeProjectIds.has(version.projectId));
  const applicationsToReview = selectedApplicationId
    ? currentApplications.filter((item) => item.id === selectedApplicationId)
    : initialProjectId
      ? currentApplications.filter((item) => item.projectId === initialProjectId)
      : [];

  return (
    <div className="space-y-5">
      {applicationsToReview.map((item) => (
        <ApplicationWizard key={item.id} eventId={eventId} application={item} projectVersions={applicationData.projectVersions} applicationSchema={applicationSchema} generalRules={generalRules} candidateInstructions={candidateInstructions} requiresFeedback={requiresFeedback} acceptsApplications={acceptsApplications} />
      ))}
      {!acceptsApplications ? <p className="text-sm text-muted-foreground">Game submissions are closed for this event. You can still review any application already in progress.</p> : availableVersions.length > 0 ? (
        <ApplicationWizard eventId={eventId} projectVersions={availableVersions} initialProjectId={applicationData.initialProjectId} applicationSchema={applicationSchema} generalRules={generalRules} candidateInstructions={candidateInstructions} requiresFeedback={requiresFeedback} acceptsApplications={acceptsApplications} />
      ) : currentApplications.length === 0 ? (
        <div className="grid gap-4 rounded-xl border border-dashed bg-card/40 p-5 sm:grid-cols-[auto_1fr] sm:items-start">
          <div className="grid size-11 place-items-center rounded-lg bg-muted text-muted-foreground"><Gamepad2 className="size-5" aria-hidden="true" /></div>
          <div className="space-y-3">
            <div className="space-y-1">
              <p className="font-semibold">No test-ready builds available</p>
              <p className="text-sm text-muted-foreground">Choose a project with a build marked Ready for Testing or Released. You can submit it here once it is ready.</p>
            </div>
            <Link href="/projects" className={buttonVariants({ variant: 'outline', size: 'sm' })}>Open your projects</Link>
          </div>
        </div>
      ) : null}
    </div>
  );
}
