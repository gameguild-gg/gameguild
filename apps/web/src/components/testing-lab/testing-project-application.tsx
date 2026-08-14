'use client';

import {
  submitTestingProjectApplication,
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

export function TestingProjectApplication({
  eventId,
  isAuthenticated,
  acceptsApplications,
  projectVersions,
  application,
  initialProjectId,
}: {
  eventId: string;
  isAuthenticated: boolean;
  acceptsApplications: boolean;
  projectVersions: ProjectVersionOption[];
  application?: CurrentApplication;
  initialProjectId?: string;
}) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const [selectedVersionId, setSelectedVersionId] = useState(
    () => projectVersions.find((version) => version.projectId === initialProjectId)?.id ?? '',
  );
  const selectedVersion = projectVersions.find((version) => version.id === selectedVersionId);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    startTransition(async () => {
      const next = await submitTestingProjectApplication(new FormData(form));
      setResult(next);
    });
  }

  function withdraw() {
    const formData = new FormData();
    formData.set('eventId', eventId);
    formData.set('applicationId', application?.id ?? '');
    startTransition(async () => {
      const next = await withdrawTestingProjectApplication(formData);
      setResult(next);
    });
  }

  if (!isAuthenticated) {
    return (
      <Button asChild className="w-full sm:w-auto">
        <Link href="/sign-in">Sign in to apply</Link>
      </Button>
    );
  }

  if (application) {
    const canWithdraw = ['Pending', 'UnderReview', 'Waitlisted'].includes(application.status ?? '');
    return (
      <div className="space-y-4">
        <div className="flex flex-wrap items-center gap-3">
          <Badge variant="outline">{application.status ?? 'Submitted'}</Badge>
          <p className="text-sm text-muted-foreground">This project application is already linked to your account.</p>
        </div>
        {application.decisionRationale ? (
          <Alert>
            <AlertCircle className="size-4" />
            <AlertDescription>{application.decisionRationale}</AlertDescription>
          </Alert>
        ) : null}
        {canWithdraw ? (
          <Button type="button" variant="outline" disabled={pending} onClick={withdraw}>
            {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
            Withdraw application
          </Button>
        ) : null}
        <ResultMessage result={result} />
      </div>
    );
  }

  if (!acceptsApplications) {
    return <p className="text-sm text-muted-foreground">Project applications are currently closed.</p>;
  }

  if (projectVersions.length === 0) {
    return (
      <div className="flex flex-col items-start gap-3">
        <p className="text-sm text-muted-foreground">
          Create an accessible project version before applying to this Testing Lab event.
        </p>
        <Button asChild variant="outline">
          <Link href="/projects">Browse projects</Link>
        </Button>
      </div>
    );
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
            <option value="" disabled>
              Select a project version
            </option>
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
          placeholder="Share the slots or time windows that work best for you."
        />
      </div>
      <p className="text-xs text-muted-foreground">
        Submitting is a candidacy. Event capacity is reserved only after approval by the manager or review committee.
      </p>
      <Button type="submit" disabled={pending}>
        {pending ? <Loader2 className="mr-2 size-4 animate-spin" /> : null}
        Submit project application
      </Button>
      <ResultMessage result={result} />
    </form>
  );
}
