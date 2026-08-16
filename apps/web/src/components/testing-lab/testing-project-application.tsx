'use client';

import {
  submitTestingProjectApplication,
  updateTestingProjectApplication,
  withdrawTestingProjectApplication,
  type TestingEventActionResult,
} from '@/lib/testing-lab/events-actions';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Label } from '@game-guild/ui/components/label';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, CheckCircle2, FolderKanban, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { useState, useTransition, type FormEvent } from 'react';

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
}

function ResultMessage({ result }: { result: TestingEventActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
      {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
      <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
    </Alert>
  );
}

function ProjectApplicationSummary({
  application,
  eventId,
  acceptsApplications,
  projectVersions,
}: {
  application: CurrentApplication;
  eventId: string;
  acceptsApplications: boolean;
  projectVersions: ProjectVersionOption[];
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const distinctProjectIds = [...new Set(projectVersions.map((version) => version.projectId))];
  const applicationProjectId = application.projectId ?? (distinctProjectIds.length === 1 ? distinctProjectIds[0] : null);
  const matchingVersions = applicationProjectId
    ? projectVersions.filter((version) => version.projectId === applicationProjectId)
    : [];
  const [selectedVersionId, setSelectedVersionId] = useState(
    application.projectVersionId ?? matchingVersions[0]?.id ?? '',
  );
  const canUpdate = application.status === 'Pending' && acceptsApplications && matchingVersions.length > 0;
  const canWithdraw = ['Pending', 'UnderReview', 'Waitlisted'].includes(application.status ?? '');

  function update(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    startTransition(async () => setResult(await updateTestingProjectApplication(new FormData(event.currentTarget))));
  }

  function withdraw() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('applicationId', application.id);
    startTransition(async () => setResult(await withdrawTestingProjectApplication(formData)));
  }

  return (
    <section className="space-y-4 rounded-md border border-border p-4">
      <div className="flex flex-wrap items-center gap-3">
        <Badge variant="outline">{application.status ?? 'Submitted'}</Badge>
        <p className="text-sm text-muted-foreground">
          This application belongs to the Project. The submitting member is retained only for audit.
        </p>
      </div>
      {application.decisionRationale ? (
        <Alert>
          <AlertCircle className="size-4" />
          <AlertDescription>{application.decisionRationale}</AlertDescription>
        </Alert>
      ) : null}
      {canUpdate ? (
        <form className="space-y-3" onSubmit={update}>
          <input type="hidden" name="eventId" value={eventId} />
          <input type="hidden" name="applicationId" value={application.id} />
          <div className="space-y-2">
            <Label htmlFor={`application-version-${application.id}`}>Submitted project version</Label>
            <select
              id={`application-version-${application.id}`}
              name="projectVersionId"
              required
              value={selectedVersionId}
              onChange={(event) => setSelectedVersionId(event.target.value)}
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {matchingVersions.map((version) => (
                <option key={version.id} value={version.id}>
                  {version.projectTitle} · {version.versionNumber} ({version.status})
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-2">
            <Label htmlFor={`application-availability-${application.id}`}>Preferred availability</Label>
            <Textarea
              id={`application-availability-${application.id}`}
              name="preferredAvailability"
              rows={2}
              defaultValue={application.preferredAvailability ?? ''}
            />
          </div>
          <Button type="submit" disabled={pending || !selectedVersionId}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
            Update application
          </Button>
        </form>
      ) : null}
      {canWithdraw ? (
        <Button type="button" variant="outline" disabled={pending} onClick={withdraw}>
          {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
          Withdraw application
        </Button>
      ) : null}
      <ResultMessage result={result} />
    </section>
  );
}

function NewProjectApplicationForm({
  eventId,
  projectVersions,
  initialProjectId,
  hasExistingApplications,
}: {
  eventId: string;
  projectVersions: ProjectVersionOption[];
  initialProjectId?: string;
  hasExistingApplications: boolean;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const [selectedVersionId, setSelectedVersionId] = useState(
    () => projectVersions.find((version) => version.projectId === initialProjectId)?.id ?? '',
  );
  const selectedVersion = projectVersions.find((version) => version.id === selectedVersionId);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    startTransition(async () => setResult(await submitTestingProjectApplication(new FormData(event.currentTarget))));
  }

  return (
    <form className="space-y-4" onSubmit={submit}>
      <input type="hidden" name="eventId" value={eventId} />
      <div className="space-y-2">
        <Label htmlFor={`testing-project-${eventId}`}>Project version</Label>
        <div className="relative">
          <FolderKanban className="pointer-events-none absolute left-3 top-3 size-4 text-muted-foreground" />
          <select
            id={`testing-project-${eventId}`}
            name="projectVersionId"
            required
            value={selectedVersionId}
            onChange={(event) => setSelectedVersionId(event.target.value)}
            className="flex h-10 w-full rounded-md border border-input bg-background py-2 pl-10 pr-3 text-sm outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <option value="" disabled>Select a project version</option>
            {projectVersions.map((version) => (
              <option key={version.id} value={version.id}>
                {version.projectTitle} · {version.versionNumber} ({version.status})
              </option>
            ))}
          </select>
          <input type="hidden" name="projectId" value={selectedVersion?.projectId ?? ''} />
        </div>
      </div>
      <div className="space-y-2">
        <Label htmlFor={`preferred-availability-${eventId}`}>Preferred availability</Label>
        <Textarea
          id={`preferred-availability-${eventId}`}
          name="preferredAvailability"
          rows={3}
          placeholder="Share the slots or time windows that work best for the Project team."
        />
      </div>
      <p className="text-xs text-muted-foreground">
        Submitting is a candidacy. Event capacity is reserved only after approval by the manager or review committee.
      </p>
      <Button type="submit" disabled={pending}>
        {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
        {hasExistingApplications ? 'Submit another project application' : 'Submit project application'}
      </Button>
      <ResultMessage result={result} />
    </form>
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
}: {
  eventId: string;
  isAuthenticated: boolean;
  acceptsApplications: boolean;
  projectVersions: ProjectVersionOption[];
  application?: CurrentApplication;
  applications?: CurrentApplication[];
  initialProjectId?: string;
}) {
  if (!isAuthenticated) {
    return <Button asChild className="w-full sm:w-auto"><Link href="/sign-in">Sign in to apply</Link></Button>;
  }

  const currentApplications = applications ?? (application ? [application] : []);
  const distinctProjectIds = [...new Set(projectVersions.map((version) => version.projectId))];
  const activeProjectIds = new Set(currentApplications
    .filter((item) => !['Rejected', 'Withdrawn'].includes(item.status ?? ''))
    .map((item) => item.projectId ?? (distinctProjectIds.length === 1 ? distinctProjectIds[0] : null))
    .filter((projectId): projectId is string => Boolean(projectId)));
  const availableProjectVersions = projectVersions.filter((version) => !activeProjectIds.has(version.projectId));

  return (
    <div className="space-y-5">
      {currentApplications.map((item) => (
        <ProjectApplicationSummary
          key={item.id}
          application={item}
          eventId={eventId}
          acceptsApplications={acceptsApplications}
          projectVersions={projectVersions}
        />
      ))}

      {!acceptsApplications ? (
        <p className="text-sm text-muted-foreground">Project applications are currently closed.</p>
      ) : availableProjectVersions.length > 0 ? (
        <NewProjectApplicationForm
          eventId={eventId}
          projectVersions={availableProjectVersions}
          initialProjectId={initialProjectId}
          hasExistingApplications={currentApplications.length > 0}
        />
      ) : currentApplications.length === 0 ? (
        <div className="flex flex-col items-start gap-3">
          <p className="text-sm text-muted-foreground">
            Create an accessible project version before applying to this Testing Lab event.
          </p>
          <Button asChild variant="outline"><Link href="/projects">Browse projects</Link></Button>
        </div>
      ) : (
        <p className="text-sm text-muted-foreground">Every accessible Project already has an active application for this event.</p>
      )}
    </div>
  );
}
