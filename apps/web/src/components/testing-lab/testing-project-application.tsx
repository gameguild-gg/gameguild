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
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, CheckCircle2, ChevronLeft, ChevronRight, FolderKanban, Loader2, Save } from 'lucide-react';
import Link from 'next/link';
import { useState, useTransition } from 'react';
import { QuestionnaireBuilder } from './questionnaire-builder';
import { QuestionnaireFieldset } from './questionnaire-fieldset';

interface ProjectVersionOption {
  id: string;
  projectId: string;
  projectTitle: string;
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

const STEPS = ['Version', 'Test brief', 'Feedback form', 'Event questions', 'Review'] as const;
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
  const initialVersion = application?.projectVersionId
    ?? matchingVersions.find((version) => version.projectId === initialProjectId)?.id
    ?? '';
  const [selectedVersionId, setSelectedVersionId] = useState(initialVersion);
  const selectedVersion = projectVersions.find((version) => version.id === selectedVersionId);
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
    startTransition(async () => setResult(await withdrawTestingProjectApplication(formData)));
  }

  if (!editable) {
    return (
      <section className="space-y-3 rounded-md border p-4">
        <Badge variant="outline">{status}</Badge>
        <p className="text-sm text-muted-foreground">This application package is frozen for review and historical integrity.</p>
        {application?.decisionRationale ? <Alert><AlertCircle className="size-4" /><AlertDescription>{application.decisionRationale}</AlertDescription></Alert> : null}
      </section>
    );
  }

  return (
    <section className="space-y-5 rounded-md border bg-card p-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Badge variant={status === 'Draft' ? 'secondary' : 'outline'}>{status}</Badge>
          <span className="text-xs text-muted-foreground">Saved application {applicationId ? applicationId.slice(0, 8) : 'not created yet'}</span>
        </div>
        <Button type="button" variant="ghost" size="sm" disabled={pending || !projectId} onClick={() => persist('save')}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : <Save className="mr-2 size-4" />}
          Save progress
        </Button>
      </div>

      <ol className="grid grid-cols-5 gap-1" aria-label="Application steps">
        {STEPS.map((label, index) => (
          <li key={label} className={`rounded-sm px-2 py-1.5 text-center text-[11px] ${index === step ? 'bg-primary text-primary-foreground' : index < step ? 'bg-muted text-foreground' : 'text-muted-foreground'}`}>
            <span className="hidden sm:inline">{index + 1}. </span>{label}
          </li>
        ))}
      </ol>

      {step === 0 ? (
        <div className="space-y-2">
          <Label htmlFor={`testing-project-${applicationId || eventId}`}>Eligible project version</Label>
          <div className="relative">
            <FolderKanban className="pointer-events-none absolute left-3 top-3 size-4 text-muted-foreground" />
            <select
              id={`testing-project-${applicationId || eventId}`}
              value={selectedVersionId}
              onChange={(event) => setSelectedVersionId(event.currentTarget.value)}
              disabled={!versionMutable}
              className="flex h-10 w-full rounded-md border border-input bg-background py-2 pl-10 pr-3 text-sm"
            >
              <option value="">Select a Ready for Testing or Released version</option>
              {matchingVersions.filter((version) => ['ReadyForTesting', 'Released'].includes(version.status)).map((version) => (
                <option key={version.id} value={version.id}>{version.projectTitle} · {version.versionNumber} ({version.status})</option>
              ))}
            </select>
          </div>
          <p className="text-xs text-muted-foreground">Draft versions cannot enter Testing Lab. Eligibility is verified again by the API.</p>
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

      {step < 3 ? (
        <div className="flex items-center justify-between gap-2 border-t pt-4">
          <Button type="button" variant="outline" disabled={step === 0} onClick={() => setStep((current) => Math.max(0, current - 1))}><ChevronLeft className="mr-2 size-4" />Previous</Button>
          <Button type="button" disabled={pending || (step === 0 && !selectedVersionId)} onClick={() => persist('save', step + 1)}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}Save and continue<ChevronRight className="ml-2 size-4" />
          </Button>
        </div>
      ) : null}

      {applicationId && ['Draft', 'Pending', 'UnderReview', 'Waitlisted'].includes(status) ? (
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
  applicationSchema,
  generalRules,
  candidateInstructions,
  requiresFeedback = false,
}: {
  eventId: string;
  isAuthenticated: boolean;
  acceptsApplications: boolean;
  projectVersions: ProjectVersionOption[];
  application?: CurrentApplication;
  applications?: CurrentApplication[];
  initialProjectId?: string;
  applicationSchema?: TestingLabQuestionnaireSchema | null;
  generalRules?: string | null;
  candidateInstructions?: string | null;
  requiresFeedback?: boolean;
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

  if (!applicationData) return <Button asChild className="w-full sm:w-auto"><Link href="/sign-in">Sign in to apply</Link></Button>;

  const currentApplications = applicationData.applications ?? (applicationData.application ? [applicationData.application] : []);
  const activeProjectIds = new Set(currentApplications.filter((item) => !['Rejected', 'Withdrawn'].includes(item.status ?? '')).map((item) => item.projectId).filter((id): id is string => Boolean(id)));
  const availableVersions = applicationData.projectVersions.filter((version) => !activeProjectIds.has(version.projectId));

  return (
    <div className="space-y-5">
      {currentApplications.map((item) => (
        <ApplicationWizard key={item.id} eventId={eventId} application={item} projectVersions={applicationData.projectVersions} applicationSchema={applicationSchema} generalRules={generalRules} candidateInstructions={candidateInstructions} requiresFeedback={requiresFeedback} acceptsApplications={acceptsApplications} />
      ))}
      {!acceptsApplications ? <p className="text-sm text-muted-foreground">Project applications are currently closed.</p> : availableVersions.length > 0 ? (
        <ApplicationWizard eventId={eventId} projectVersions={availableVersions} initialProjectId={applicationData.initialProjectId} applicationSchema={applicationSchema} generalRules={generalRules} candidateInstructions={candidateInstructions} requiresFeedback={requiresFeedback} acceptsApplications={acceptsApplications} />
      ) : currentApplications.length === 0 ? (
        <div className="flex flex-col items-start gap-3"><p className="text-sm text-muted-foreground">Create a Ready for Testing or Released project version before applying.</p><Button asChild variant="outline"><Link href="/projects">Browse projects</Link></Button></div>
      ) : null}
    </div>
  );
}
