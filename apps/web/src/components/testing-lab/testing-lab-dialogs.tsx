'use client';

import {
  addTestingParticipant,
  createTestingLabLocation,
  createTestingLabRole,
  createTestingSession,
  linkTestingSessionProject,
  submitTestingBuild,
  updateTestingLabLocation,
  updateTestingLabRole,
  updateTestingRequest,
  updateTestingSession,
  type TestingLabActionResult,
} from '@/lib/testing-lab/actions';
import type { TestingLocationSummary, TestingProjectOption, TestingRequestSummary, TestingSessionSummary } from '@/lib/testing-lab/queries';
import { TestingLocationFields } from './testing-location-fields';
import type { TestingLabTestingLabRoleTemplate } from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Button } from '@game-guild/ui/components/button';
import { Checkbox } from '@game-guild/ui/components/checkbox';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Textarea } from '@game-guild/ui/components/textarea';
import { AlertCircle, CheckCircle2, FlaskConical, Link2, MapPin, Pencil, Plus, ShieldCheck, UserPlus } from 'lucide-react';
import { useState, useTransition, type FormEvent, type ReactNode } from 'react';

type Action = (formData: FormData) => Promise<TestingLabActionResult<unknown>>;

function ActionMessage({ result }: { result: TestingLabActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'}>
      {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
      <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
    </Alert>
  );
}

function ActionDialog({
  trigger,
  title,
  description,
  submitLabel,
  action,
  children,
}: {
  trigger: ReactNode;
  title: string;
  description: string;
  submitLabel: string;
  action: Action;
  children: ReactNode;
}) {
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingLabActionResult<unknown> | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const formData = new FormData(form);
    startTransition(async () => {
      const next = await action(formData);
      setResult(next);
      if (next.success) {
        form.reset();
        window.setTimeout(() => setOpen(false), 500);
      }
    });
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) setResult(null);
      }}
    >
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <form onSubmit={handleSubmit} className="space-y-5">
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>
          {children}
          <ActionMessage result={result} />
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setOpen(false)} disabled={pending}>
              Cancel
            </Button>
            <Button type="submit" disabled={pending}>
              {pending ? 'Saving...' : submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export function SubmitTestingBuildDialog({ projects }: { projects: TestingProjectOption[] }) {
  return (
    <ActionDialog
      trigger={
        <Button>
          <Plus className="mr-2 size-4" />
          New request
        </Button>
      }
      title="Submit a build for testing"
      description="Connect a real project build, define the testing window, and give testers a focused brief."
      submitLabel="Create request"
      action={submitTestingBuild}
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="request-title">Request title</Label>
          <Input id="request-title" name="title" required placeholder="Vertical slice usability pass" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="request-project">Project</Label>
          <Select name="projectId" required>
            <SelectTrigger id="request-project">
              <SelectValue placeholder="Choose a project" />
            </SelectTrigger>
            <SelectContent>
              {projects.map((project) => (
                <SelectItem key={project.id} value={project.id}>
                  {project.title}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="request-version">Build version</Label>
          <Input id="request-version" name="versionNumber" required placeholder="0.3.0" />
        </div>
        <input type="hidden" name="instructionsType" value="Text" />
        <div className="space-y-2">
          <Label htmlFor="request-start">Testing starts</Label>
          <Input id="request-start" name="startDate" type="datetime-local" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="request-end">Testing ends</Label>
          <Input id="request-end" name="endDate" type="datetime-local" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="request-capacity">Tester capacity</Label>
          <Input id="request-capacity" name="maxTesters" type="number" min="1" placeholder="Unlimited" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="request-download">Build URL</Label>
          <Input id="request-download" name="downloadUrl" type="url" placeholder="https://build.example/game" />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="request-description">Testing objective</Label>
          <Textarea id="request-description" name="description" rows={3} placeholder="What changed and what decisions should this test inform?" />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="request-instructions">Tester instructions</Label>
          <Textarea
            id="request-instructions"
            name="instructionsContent"
            rows={4}
            placeholder="Install steps, target tasks, known issues, and completion criteria."
          />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="request-feedback">Feedback prompt</Label>
          <Textarea id="request-feedback" name="feedbackFormContent" rows={3} placeholder="What should every tester answer?" />
        </div>
      </div>
    </ActionDialog>
  );
}

export function AddTestingParticipantDialog({
  requestId,
  members,
}: {
  requestId: string;
  members: Array<{
    id: string;
    displayName?: string | null;
    email?: string | null;
  }>;
}) {
  return (
    <ActionDialog
      trigger={
        <Button>
          <UserPlus className="mr-2 size-4" />
          Add participant
        </Button>
      }
      title="Add a participant"
      description="Grant a community member access to this testing request. Existing evidence remains tied to their account."
      submitLabel="Add participant"
      action={addTestingParticipant}
    >
      <input type="hidden" name="requestId" value={requestId} />
      <div className="space-y-2">
        <Label htmlFor={`participant-${requestId}`}>Member</Label>
        <Select name="userId" required>
          <SelectTrigger id={`participant-${requestId}`}>
            <SelectValue placeholder="Choose a member" />
          </SelectTrigger>
          <SelectContent>
            {members.map((member) => (
              <SelectItem key={member.id} value={member.id}>
                {member.displayName || member.email || 'Unknown member'}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
    </ActionDialog>
  );
}

export function LinkTestingSessionProjectDialog({ sessionId, projects }: { sessionId: string; projects: TestingProjectOption[] }) {
  return (
    <ActionDialog
      trigger={
        <Button disabled={projects.length === 0}>
          <Link2 className="mr-2 size-4" />
          Link project
        </Button>
      }
      title="Link a project"
      description="Choose an approved testing project for this session and record the build or testing focus."
      submitLabel="Link project"
      action={linkTestingSessionProject}
    >
      <input type="hidden" name="sessionId" value={sessionId} />
      <div className="space-y-2">
        <Label htmlFor={`session-project-${sessionId}`}>Project</Label>
        <Select name="projectId" required>
          <SelectTrigger id={`session-project-${sessionId}`}>
            <SelectValue placeholder="Choose a project" />
          </SelectTrigger>
          <SelectContent>
            {projects.map((project) => (
              <SelectItem key={project.id} value={project.id}>
                {project.title}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label htmlFor={`project-notes-${sessionId}`}>Notes</Label>
        <Input id={`project-notes-${sessionId}`} name="notes" placeholder="Build version or test focus" />
      </div>
    </ActionDialog>
  );
}
export function CreateTestingSessionDialog({ requests, locations }: { requests: TestingRequestSummary[]; locations: TestingLocationSummary[] }) {
  return (
    <ActionDialog
      trigger={
        <Button>
          <FlaskConical className="mr-2 size-4" />
          Schedule session
        </Button>
      }
      title="Schedule a testing session"
      description="Choose the request and location, then reserve tester and project capacity."
      submitLabel="Schedule session"
      action={createTestingSession}
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="session-name">Session name</Label>
          <Input id="session-name" name="sessionName" required placeholder="Friday moderated playtest" />
        </div>
        <div className="space-y-2">
          <Label>Testing request</Label>
          <Select name="testingRequestId" required>
            <SelectTrigger>
              <SelectValue placeholder="Choose a request" />
            </SelectTrigger>
            <SelectContent>
              {requests.map((request) => (
                <SelectItem key={request.id} value={request.id}>
                  {request.title}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label>Location</Label>
          <Select name="locationId" required>
            <SelectTrigger>
              <SelectValue placeholder="Choose a location" />
            </SelectTrigger>
            <SelectContent>
              {locations.map((location) => (
                <SelectItem key={location.id} value={location.id}>
                  {location.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="session-date">Date</Label>
          <Input id="session-date" name="sessionDate" type="date" required />
        </div>
        <div className="space-y-2">
          <Label>Status</Label>
          <Select name="status" defaultValue="Scheduled">
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Scheduled">Scheduled</SelectItem>
              <SelectItem value="Active">Active</SelectItem>
              <SelectItem value="Completed">Completed</SelectItem>
              <SelectItem value="Cancelled">Cancelled</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="session-start">Starts</Label>
          <Input id="session-start" name="startTime" type="datetime-local" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="session-end">Ends</Label>
          <Input id="session-end" name="endTime" type="datetime-local" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="session-testers">Max testers</Label>
          <Input id="session-testers" name="maxTesters" type="number" min="1" defaultValue="12" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="session-projects">Max projects</Label>
          <Input id="session-projects" name="maxProjects" type="number" min="1" defaultValue="4" required />
        </div>
      </div>
    </ActionDialog>
  );
}

export function CreateTestingLocationDialog() {
  return (
    <ActionDialog
      trigger={
        <Button>
          <MapPin className="mr-2 size-4" />
          New location
        </Button>
      }
      title="Create testing location"
      description="Configure a physical room or remote lab with the capacity and contact details used by scheduling."
      submitLabel="Create location"
      action={createTestingLabLocation}
    >
      <TestingLocationFields idPrefix="create-location" />
    </ActionDialog>
  );
}

const permissionOptions = [
  ['canViewRequests', 'View requests'],
  ['canCreateRequests', 'Create requests'],
  ['canEditRequests', 'Edit requests'],
  ['canApproveRequests', 'Approve requests'],
  ['canDeleteRequests', 'Archive requests'],
  ['canViewSessions', 'View sessions'],
  ['canCreateSessions', 'Create sessions'],
  ['canEditSessions', 'Edit sessions'],
  ['canDeleteSessions', 'Archive sessions'],
  ['canViewParticipants', 'View participants'],
  ['canManageParticipants', 'Manage participants'],
  ['canViewFeedback', 'View feedback'],
  ['canCreateFeedback', 'Create feedback'],
  ['canEditFeedback', 'Edit feedback'],
  ['canModerateFeedback', 'Moderate feedback'],
  ['canViewLocations', 'View locations'],
  ['canCreateLocations', 'Create locations'],
  ['canEditLocations', 'Edit locations'],
  ['canDeleteLocations', 'Archive locations'],
] as const;

export function CreateTestingLabRoleDialog() {
  return (
    <ActionDialog
      trigger={
        <Button>
          <ShieldCheck className="mr-2 size-4" />
          New role
        </Button>
      }
      title="Create Testing Lab role"
      description="Bundle operational permissions into a reusable role template."
      submitLabel="Create role"
      action={createTestingLabRole}
    >
      <div className="grid gap-4">
        <div className="space-y-2">
          <Label htmlFor="role-name">Role name</Label>
          <Input id="role-name" name="name" required placeholder="Session facilitator" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="role-description">Description</Label>
          <Textarea id="role-description" name="description" rows={2} />
        </div>
        <fieldset>
          <legend className="mb-3 text-sm font-medium">Permissions</legend>
          <div className="grid gap-2 sm:grid-cols-2">
            {permissionOptions.map(([name, label]) => (
              <div key={name} className="flex items-center gap-3 rounded-md border px-3 py-2 text-sm">
                <Checkbox id={'create-role-' + name} name={name} value="true" />
                <Label htmlFor={'create-role-' + name}>{label}</Label>
              </div>
            ))}
          </div>
        </fieldset>
      </div>
    </ActionDialog>
  );
}

function dateInputValue(value?: string | null, dateOnly = false) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return value;
  return dateOnly ? date.toISOString().slice(0, 10) : date.toISOString().slice(0, 16);
}

export function EditTestingRequestDialog({ request }: { request: TestingRequestSummary }) {
  return (
    <ActionDialog
      trigger={
        <Button variant="outline">
          <Pencil className="mr-2 size-4" />
          Edit request
        </Button>
      }
      title="Edit testing request"
      description="Update the test brief, build access, capacity, window, and lifecycle status."
      submitLabel="Save request"
      action={updateTestingRequest}
    >
      <input type="hidden" name="requestId" value={request.id} />
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`request-title-${request.id}`}>Title</Label>
          <Input id={`request-title-${request.id}`} name="title" required defaultValue={request.title} />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`request-description-${request.id}`}>Objective</Label>
          <Textarea id={`request-description-${request.id}`} name="description" rows={3} defaultValue={request.description ?? ''} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`request-build-${request.id}`}>Build URL</Label>
          <Input id={`request-build-${request.id}`} name="downloadUrl" type="url" defaultValue={request.downloadUrl ?? ''} />
        </div>
        <div className="space-y-2">
          <Label>Lifecycle status</Label>
          <Select name="status" defaultValue={String(request.status)}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {['Draft', 'Open', 'Active', 'InProgress', 'Paused', 'Completed', 'Cancelled'].map((status) => (
                <SelectItem key={status} value={status}>
                  {status === 'InProgress' ? 'In progress' : status}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor={`request-start-${request.id}`}>Starts</Label>
          <Input id={`request-start-${request.id}`} name="startDate" type="datetime-local" defaultValue={dateInputValue(request.startDate)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`request-end-${request.id}`}>Ends</Label>
          <Input id={`request-end-${request.id}`} name="endDate" type="datetime-local" defaultValue={dateInputValue(request.endDate)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`request-capacity-${request.id}`}>Tester capacity</Label>
          <Input
            id={`request-capacity-${request.id}`}
            name="maxTesters"
            type="number"
            min="1"
            defaultValue={request.maxTesters ?? ''}
            placeholder="Unlimited"
          />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`request-instructions-${request.id}`}>Instructions</Label>
          <Textarea id={`request-instructions-${request.id}`} name="instructionsContent" rows={4} defaultValue={request.instructionsContent ?? ''} />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`request-feedback-${request.id}`}>Feedback prompt</Label>
          <Textarea id={`request-feedback-${request.id}`} name="feedbackFormContent" rows={3} defaultValue={request.feedbackFormContent ?? ''} />
        </div>
      </div>
    </ActionDialog>
  );
}

export function EditTestingSessionDialog({ session, locations }: { session: TestingSessionSummary; locations: TestingLocationSummary[] }) {
  return (
    <ActionDialog
      trigger={
        <Button variant="outline">
          <Pencil className="mr-2 size-4" />
          Edit session
        </Button>
      }
      title="Edit testing session"
      description="Update scheduling, location, capacity, and lifecycle status."
      submitLabel="Save session"
      action={updateTestingSession}
    >
      <input type="hidden" name="sessionId" value={session.id} />
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`session-name-${session.id}`}>Name</Label>
          <Input id={`session-name-${session.id}`} name="sessionName" required defaultValue={session.sessionName} />
        </div>
        <div className="space-y-2">
          <Label>Date</Label>
          <Input name="sessionDate" type="date" required defaultValue={dateInputValue(session.sessionDate, true)} />
        </div>
        <div className="space-y-2">
          <Label>Status</Label>
          <Select name="status" defaultValue={String(session.status)}>
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {['Scheduled', 'Active', 'Completed', 'Cancelled'].map((status) => (
                <SelectItem key={status} value={status}>
                  {status}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label>Starts</Label>
          <Input name="startTime" type="datetime-local" required defaultValue={dateInputValue(session.startTime)} />
        </div>
        <div className="space-y-2">
          <Label>Ends</Label>
          <Input name="endTime" type="datetime-local" required defaultValue={dateInputValue(session.endTime)} />
        </div>
        <div className="space-y-2">
          <Label>Location</Label>
          <Select name="locationId" defaultValue={session.locationId ?? undefined}>
            <SelectTrigger>
              <SelectValue placeholder="Choose a location" />
            </SelectTrigger>
            <SelectContent>
              {locations.map((location) => (
                <SelectItem key={location.id} value={location.id}>
                  {location.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label>Tester capacity</Label>
          <Input name="maxTesters" type="number" min="1" defaultValue={session.maxTesters ?? 1} />
        </div>
        <div className="space-y-2">
          <Label>Project capacity</Label>
          <Input name="maxProjects" type="number" min="1" defaultValue={session.maxProjects ?? 1} />
        </div>
      </div>
    </ActionDialog>
  );
}

export function EditTestingLocationDialog({ location }: { location: TestingLocationSummary }) {
  return (
    <ActionDialog
      trigger={
        <Button size="sm" variant="outline">
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      }
      title={'Edit ' + location.name}
      description="Update delivery mode, address, capacity, contacts, and operating status."
      submitLabel="Save location"
      action={updateTestingLabLocation}
    >
      <input type="hidden" name="locationId" value={location.id} />
      <TestingLocationFields idPrefix={'edit-location-' + location.id} location={location} />
    </ActionDialog>
  );
}

export function EditTestingLabRoleDialog({ role }: { role: TestingLabTestingLabRoleTemplate }) {
  return (
    <ActionDialog
      trigger={
        <Button size="sm" variant="outline">
          <Pencil className="mr-2 size-4" />
          Edit
        </Button>
      }
      title={`Edit ${role.name}`}
      description="Change the reusable permission matrix for this Testing Lab role."
      submitLabel="Save role"
      action={updateTestingLabRole}
    >
      <input type="hidden" name="idOrName" value={role.id ?? role.name ?? ''} />
      <div className="grid gap-4">
        <div className="space-y-2">
          <Label>Role name</Label>
          <Input name="name" required defaultValue={role.name ?? ''} />
        </div>
        <div className="space-y-2">
          <Label>Description</Label>
          <Textarea name="description" rows={2} defaultValue={role.description ?? ''} />
        </div>
        <fieldset>
          <legend className="mb-3 text-sm font-medium">Permissions</legend>
          <div className="grid gap-2 sm:grid-cols-2">
            {permissionOptions.map(([name, label]) => (
              <div key={name} className="flex items-center gap-3 rounded-md border px-3 py-2 text-sm">
                <Checkbox id={'edit-role-' + name} name={name} value="true" defaultChecked={Boolean(role.permissions?.[name])} />
                <Label htmlFor={'edit-role-' + name}>{label}</Label>
              </div>
            ))}
          </div>
        </fieldset>
      </div>
    </ActionDialog>
  );
}
