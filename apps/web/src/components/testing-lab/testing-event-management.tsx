'use client';

import {
  addTestingEventCommitteeMember,
  assignTestedProjectToRegistration,
  approveTestingEventApplication,
  beginTestingEventApplicationReview,
  configureTestingEventLearning,
  createTestingEvent,
  createTestingEventSlot,
  deleteTestingEvent,
  deleteTestingEventSlot,
  rejectTestingEventApplication,
  removeTestingEventCommitteeMember,
  transitionTestingEvent,
  updateTestingEventAttendance,
  updateTestingEvent,
  updateTestingEventSlot,
  voteOnTestingEventApplication,
  waitlistTestingEventApplication,
  type TestingEventActionResult,
} from '@/lib/testing-lab/events-actions';
import { formatTestingEventStatus } from '@/lib/testing-lab/format';
import type {
  TestingLabTestingEventCommitteeMemberProjection,
  TestingLabTestingEventProjection,
  TestingLabTestingEventSlotProjection,
  TestingLabTestingEventStatus,
  TestingLabTestingProjectApplicationProjection,
  TestingLabTestingSlotRegistrationProjection,
} from '@game-guild/client';
import { Alert, AlertDescription } from '@game-guild/ui/components/alert';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@game-guild/ui/components/dialog';
import { Input } from '@game-guild/ui/components/input';
import { Label } from '@game-guild/ui/components/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@game-guild/ui/components/select';
import { Textarea } from '@game-guild/ui/components/textarea';
import {
  AlertCircle,
  CheckCircle2,
  CircleStop,
  ClipboardCheck,
  Clock3,
  Pencil,
  Play,
  Plus,
  Send,
  ShieldCheck,
  Trash2,
  UserRoundCheck,
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useState, useTransition, type FormEvent, type ReactNode } from 'react';

type Action = (formData: FormData) => Promise<TestingEventActionResult<unknown>>;

export interface TestingLabMemberOption {
  id: string;
  label: string;
}

export interface TestingLabApprovedApplicationOption {
  id: string;
  label: string;
  slotId?: string | null;
}

export interface TestingLabLearningActivityOption {
  id: string;
  courseId: string;
  label: string;
}

function datetimeLocal(value?: string | null) {
  if (!value) return '';
  const date = new Date(value);
  if (Number.isNaN(date.valueOf())) return '';
  const local = new Date(date.valueOf() - date.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 16);
}


function ActionMessage({ result }: { result: TestingEventActionResult<unknown> | null }) {
  if (!result) return null;
  return (
    <Alert variant={result.success ? 'default' : 'destructive'} aria-live="polite">
      {result.success ? <CheckCircle2 className="size-4" /> : <AlertCircle className="size-4" />}
      <AlertDescription>{result.success ? result.message : result.error}</AlertDescription>
    </Alert>
  );
}

function EventActionDialog({
  trigger,
  title,
  description,
  submitLabel,
  action,
  children,
  destructive = false,
}: {
  trigger: ReactNode;
  title: string;
  description: string;
  submitLabel: string;
  action: Action;
  children: ReactNode;
  destructive?: boolean;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    startTransition(async () => {
      const next = await action(data);
      setResult(next);
      if (next.success) {
        form.reset();
        router.refresh();
        window.setTimeout(() => setOpen(false), 450);
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
        <form onSubmit={submit} className="space-y-5">
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>
          {children}
          <ActionMessage result={result} />
          <DialogFooter>
            <Button type="button" variant="outline" disabled={pending} onClick={() => setOpen(false)}>
              Cancel
            </Button>
            <Button type="submit" variant={destructive ? 'destructive' : 'default'} disabled={pending}>
              {pending ? 'Working...' : submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function EventFields({ event }: { event?: TestingLabTestingEventProjection }) {
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      {event?.id ? <input type="hidden" name="eventId" value={event.id} /> : null}
      <div className="space-y-2 sm:col-span-2">
        <Label htmlFor={`event-name-${event?.id ?? 'new'}`}>Event name</Label>
        <Input id={`event-name-${event?.id ?? 'new'}`} name="name" required defaultValue={event?.name ?? ''} />
      </div>
      <div className="space-y-2 sm:col-span-2">
        <Label htmlFor={`event-description-${event?.id ?? 'new'}`}>Purpose and tester brief</Label>
        <Textarea
          id={`event-description-${event?.id ?? 'new'}`}
          name="description"
          rows={3}
          defaultValue={event?.description ?? ''}
        />
      </div>
      <div className="space-y-2">
        <Label>Delivery mode</Label>
        <Select name="mode" defaultValue={event?.mode ?? 'Online'}>
          <SelectTrigger><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="Online">Online</SelectItem>
            <SelectItem value="InPerson">In person</SelectItem>
            <SelectItem value="Hybrid">Hybrid</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label>Approval</Label>
        <Select name="approvalMode" defaultValue={event?.approvalMode ?? 'ManagerOnly'}>
          <SelectTrigger><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="ManagerOnly">Manager decision</SelectItem>
            <SelectItem value="Committee">Review committee</SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label htmlFor={`applications-open-${event?.id ?? 'new'}`}>Applications open</Label>
        <Input
          id={`applications-open-${event?.id ?? 'new'}`}
          name="applicationsOpenAt"
          type="datetime-local"
          required
          defaultValue={datetimeLocal(event?.applicationsOpenAt)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`applications-close-${event?.id ?? 'new'}`}>Applications close</Label>
        <Input
          id={`applications-close-${event?.id ?? 'new'}`}
          name="applicationsCloseAt"
          type="datetime-local"
          required
          defaultValue={datetimeLocal(event?.applicationsCloseAt)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`event-start-${event?.id ?? 'new'}`}>Event starts</Label>
        <Input
          id={`event-start-${event?.id ?? 'new'}`}
          name="startsAt"
          type="datetime-local"
          required
          defaultValue={datetimeLocal(event?.startsAt)}
        />
      </div>
      <div className="space-y-2">
        <Label htmlFor={`event-end-${event?.id ?? 'new'}`}>Event ends</Label>
        <Input
          id={`event-end-${event?.id ?? 'new'}`}
          name="endsAt"
          type="datetime-local"
          required
          defaultValue={datetimeLocal(event?.endsAt)}
        />
      </div>
      <label className="flex items-start gap-3 rounded-md border p-3 text-sm sm:col-span-2">
        <input name="requiresFeedback" type="checkbox" defaultChecked={event?.requiresFeedback ?? true} className="mt-1" />
        <span>
          <strong className="block font-medium">Require tester feedback</strong>
          <span className="text-muted-foreground">Attendance can only be completed after required project feedback is submitted.</span>
        </span>
      </label>
    </div>
  );
}

export function CreateTestingEventDialog() {
  return (
    <EventActionDialog
      trigger={<Button><Plus className="mr-2 size-4" />New event</Button>}
      title="Create testing event"
      description="Create the application window first. Project capacity is reserved only after approval."
      submitLabel="Create event"
      action={createTestingEvent}
    >
      <EventFields />
    </EventActionDialog>
  );
}

export function EditTestingEventDialog({ event }: { event: TestingLabTestingEventProjection }) {
  return (
    <EventActionDialog
      trigger={<Button variant="outline"><Pencil className="mr-2 size-4" />Edit</Button>}
      title="Edit testing event"
      description="Adjust the event brief, review model, application window, and delivery dates."
      submitLabel="Save event"
      action={updateTestingEvent}
    >
      <EventFields event={event} />
    </EventActionDialog>
  );
}

export function CreateTestingEventSlotDialog({ eventId }: { eventId: string }) {
  return (
    <EventActionDialog
      trigger={<Button size="sm"><Plus className="mr-2 size-4" />Add slot</Button>}
      title="Add testing slot"
      description="A slot defines one test window and its independent tester and approved-project capacity."
      submitLabel="Create slot"
      action={createTestingEventSlot}
    >
      <input type="hidden" name="eventId" value={eventId} />
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Mode</Label>
          <Select name="mode" defaultValue="InPerson">
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="InPerson">In person</SelectItem>
              <SelectItem value="Online">Online</SelectItem>
              <SelectItem value="Hybrid">Hybrid</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-location">Saved location id</Label>
          <Input id="slot-location" name="locationId" placeholder="Optional location UUID" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-start">Starts</Label>
          <Input id="slot-start" name="startsAt" type="datetime-local" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-end">Ends</Label>
          <Input id="slot-end" name="endsAt" type="datetime-local" required />
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-campus">Campus</Label>
          <Input id="slot-campus" name="campusName" placeholder="Required in person" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-room">Room</Label>
          <Input id="slot-room" name="roomName" placeholder="Required in person" />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="slot-url">Meeting URL</Label>
          <Input id="slot-url" name="meetingUrl" type="url" placeholder="Required online" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-testers">Tester capacity</Label>
          <Input id="slot-testers" name="maxTesters" type="number" min="1" placeholder="Unlimited" />
        </div>
        <div className="space-y-2">
          <Label htmlFor="slot-projects">Approved project capacity</Label>
          <Input id="slot-projects" name="maxProjects" type="number" min="1" placeholder="Unlimited" />
        </div>
      </div>
    </EventActionDialog>
  );
}

export function ManageTestingEventSlotDialog({
  eventId,
  slot,
}: {
  eventId: string;
  slot: TestingLabTestingEventSlotProjection;
}) {
  if (!slot.id) return null;
  return (
    <EventActionDialog
      trigger={<Button size="sm" variant="outline"><Pencil className="mr-2 size-4" />Edit slot</Button>}
      title="Edit testing slot"
      description="Change this slot without affecting the schedules and capacity of other slots."
      submitLabel="Save slot"
      action={updateTestingEventSlot}
    >
      <input type="hidden" name="eventId" value={eventId} />
      <input type="hidden" name="slotId" value={slot.id} />
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="space-y-2">
          <Label>Mode</Label>
          <Select name="mode" defaultValue={slot.mode ?? 'Online'}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              <SelectItem value="InPerson">In person</SelectItem>
              <SelectItem value="Online">Online</SelectItem>
              <SelectItem value="Hybrid">Hybrid</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-location-${slot.id}`}>Saved location id</Label>
          <Input id={`slot-location-${slot.id}`} name="locationId" defaultValue={slot.locationId ?? ''} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-start-${slot.id}`}>Starts</Label>
          <Input id={`slot-start-${slot.id}`} name="startsAt" type="datetime-local" required defaultValue={datetimeLocal(slot.startsAt)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-end-${slot.id}`}>Ends</Label>
          <Input id={`slot-end-${slot.id}`} name="endsAt" type="datetime-local" required defaultValue={datetimeLocal(slot.endsAt)} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-campus-${slot.id}`}>Campus</Label>
          <Input id={`slot-campus-${slot.id}`} name="campusName" defaultValue={slot.campusName ?? ''} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-room-${slot.id}`}>Room</Label>
          <Input id={`slot-room-${slot.id}`} name="roomName" defaultValue={slot.roomName ?? ''} />
        </div>
        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor={`slot-url-${slot.id}`}>Meeting URL</Label>
          <Input id={`slot-url-${slot.id}`} name="meetingUrl" type="url" defaultValue={slot.meetingUrl ?? ''} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-testers-${slot.id}`}>Tester capacity</Label>
          <Input id={`slot-testers-${slot.id}`} name="maxTesters" type="number" min="1" defaultValue={slot.maxTesters ?? ''} />
        </div>
        <div className="space-y-2">
          <Label htmlFor={`slot-projects-${slot.id}`}>Approved project capacity</Label>
          <Input id={`slot-projects-${slot.id}`} name="maxProjects" type="number" min="1" defaultValue={slot.maxProjects ?? ''} />
        </div>
      </div>
      <div className="border-t pt-4">
        <EventActionDialog
          trigger={<Button type="button" size="sm" variant="destructive"><Trash2 className="mr-2 size-4" />Delete slot</Button>}
          title="Delete this testing slot?"
          description="A slot with approved projects or tester registrations cannot be deleted."
          submitLabel="Delete slot"
          action={deleteTestingEventSlot}
          destructive
        >
          <input type="hidden" name="eventId" value={eventId} />
          <input type="hidden" name="slotId" value={slot.id} />
        </EventActionDialog>
      </div>
    </EventActionDialog>
  );
}

export function TestingEventLifecycleActions({ event }: { event: TestingLabTestingEventProjection }) {
  const [pending, startTransition] = useTransition();
  const [result, setResult] = useState<TestingEventActionResult<unknown> | null>(null);
  const nextByStatus: Partial<Record<TestingLabTestingEventStatus, [string, string, typeof Send]>> = {
    Draft: ['open-applications', 'Open applications', Send],
    ApplicationsOpen: ['close-applications', 'Close applications', CircleStop],
    ApplicationsClosed: ['schedule', 'Schedule event', Clock3],
    Scheduled: ['activate', 'Start event', Play],
    Active: ['complete', 'Complete event', CheckCircle2],
  };
  const next = nextByStatus[event.status ?? 'Draft'];

  function run(transition: string) {
    if (!event.id) return;
    const form = new FormData();
    form.set('eventId', event.id);
    form.set('transition', transition);
    startTransition(async () => setResult(await transitionTestingEvent(form)));
  }

  const NextIcon = next?.[2];
  return (
    <div className="space-y-3">
      <div className="flex flex-wrap gap-2">
        {next && NextIcon ? (
          <Button size="sm" disabled={pending} onClick={() => run(next[0])}>
            <NextIcon className="mr-2 size-4" />{next[1]}
          </Button>
        ) : null}
        {!['Completed', 'Cancelled'].includes(event.status ?? '') ? (
          <EventActionDialog
            trigger={<Button size="sm" variant="outline"><CircleStop className="mr-2 size-4" />Cancel event</Button>}
            title="Cancel this testing event?"
            description="Cancellation preserves applications, attendance, feedback, and audit history."
            submitLabel="Cancel event"
            action={transitionTestingEvent}
            destructive
          >
            <input type="hidden" name="eventId" value={event.id} />
            <input type="hidden" name="transition" value="cancel" />
            <div className="space-y-2">
              <Label htmlFor="event-cancellation-reason">Cancellation reason</Label>
              <Textarea id="event-cancellation-reason" name="reason" required rows={4} />
            </div>
          </EventActionDialog>
        ) : null}
        {event.status === 'Draft' && event.id ? (
          <EventActionDialog
            trigger={<Button size="sm" variant="destructive"><Trash2 className="mr-2 size-4" />Delete draft</Button>}
            title="Delete this draft event?"
            description="Only an unused draft can be deleted. This action cannot be undone."
            submitLabel="Delete draft"
            action={deleteTestingEvent}
            destructive
          >
            <input type="hidden" name="eventId" value={event.id} />
          </EventActionDialog>
        ) : null}
      </div>
      <ActionMessage result={result} />
    </div>
  );
}

export function TestingEventCommittee({
  event,
  members,
  committee,
  readOnly = false,
}: {
  event: TestingLabTestingEventProjection;
  members: TestingLabMemberOption[];
  committee: TestingLabTestingEventCommitteeMemberProjection[];
  readOnly?: boolean;
}) {
  return (
    <section>
      <div className="mb-3 flex items-center justify-between gap-3">
        <div>
          <h2 className="font-semibold">Review committee</h2>
          <p className="text-sm text-muted-foreground">
            {event.approvalMode === 'Committee' ? 'Committee votes inform approval; the manager resolves ties.' : 'Manager-only approval is active.'}
          </p>
        </div>
        {event.id && !readOnly ? (
          <EventActionDialog
            trigger={<Button size="sm" variant="outline"><UserRoundCheck className="mr-2 size-4" />Add reviewer</Button>}
            title="Add committee reviewer"
            description="Only active tenant members can review project applications."
            submitLabel="Add reviewer"
            action={addTestingEventCommitteeMember}
          >
            <input type="hidden" name="eventId" value={event.id} />
            <div className="space-y-2">
              <Label>Member</Label>
              <Select name="userId" required>
                <SelectTrigger><SelectValue placeholder="Choose a member" /></SelectTrigger>
                <SelectContent>
                  {members.map((member) => <SelectItem key={member.id} value={member.id}>{member.label}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input type="checkbox" name="isChair" /> Committee chair
            </label>
          </EventActionDialog>
        ) : null}
      </div>
      {committee.length === 0 ? (
        <p className="rounded-md border border-dashed p-4 text-sm text-muted-foreground">No reviewers assigned.</p>
      ) : (
        <div className="divide-y rounded-md border">
          {committee.map((reviewer) => (
            <div key={reviewer.id ?? reviewer.userId} className="flex items-center justify-between gap-3 p-3">
              <div className="min-w-0">
                <p className="truncate text-sm font-medium">{reviewer.userName ?? reviewer.userEmail ?? reviewer.userId}</p>
                <p className="truncate text-xs text-muted-foreground">{reviewer.userEmail}</p>
              </div>
              <div className="flex items-center gap-2">
                {reviewer.isChair ? <Badge variant="secondary">Chair</Badge> : null}
                {event.id && reviewer.userId && !readOnly ? (
                  <EventActionDialog
                    trigger={<Button size="icon" variant="ghost" aria-label={`Remove ${reviewer.userName ?? 'reviewer'}`}><Trash2 className="size-4" /></Button>}
                    title="Remove this reviewer?"
                    description="A reviewer with recorded votes cannot be removed from the audit trail."
                    submitLabel="Remove reviewer"
                    action={removeTestingEventCommitteeMember}
                    destructive
                  >
                    <input type="hidden" name="eventId" value={event.id} />
                    <input type="hidden" name="userId" value={reviewer.userId} />
                  </EventActionDialog>
                ) : null}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

export function TestingEventApplications({
  eventId,
  applications,
  slots,
  projectLabels = {},
  memberLabels = {},
  readOnly = false,
}: {
  eventId: string;
  applications: TestingLabTestingProjectApplicationProjection[];
  slots: TestingLabTestingEventSlotProjection[];
  readOnly?: boolean;
  projectLabels?: Record<string, string>;
  memberLabels?: Record<string, string>;
}) {
  const router = useRouter();
  const [reviewingApplicationId, setReviewingApplicationId] = useState<string | null>(null);
  const [reviewPending, startReviewTransition] = useTransition();
  const [reviewResult, setReviewResult] = useState<{
    applicationId: string;
    result: TestingEventActionResult<unknown>;
  } | null>(null);

  if (applications.length === 0) {
    return <p className="rounded-md border border-dashed p-5 text-center text-sm text-muted-foreground">No project applications yet.</p>;
  }

  return (
    <div className="divide-y rounded-md border">
      {applications.map((application) => {
        const status = application.status ?? 'Pending';
        const projectLabel = application.projectId
          ? projectLabels[application.projectId] ?? 'Project details unavailable'
          : 'Project details unavailable';
        const memberLabel = application.submittedByUserId
          ? memberLabels[application.submittedByUserId] ?? 'Member details unavailable'
          : 'Member details unavailable';
        return (
          <div key={application.id} className="flex flex-col gap-3 p-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="min-w-0">
              <p className="truncate font-medium">{projectLabel}</p>
              <p className="text-xs text-muted-foreground">Submitted by {memberLabel}</p>
              {application.decisionRationale ? <p className="mt-1 text-sm text-muted-foreground">{application.decisionRationale}</p> : null}
              {reviewResult?.applicationId === application.id ? (
                <div className="mt-3">
                  <ActionMessage result={reviewResult?.result ?? null} />
                </div>
              ) : null}
            </div>
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant="outline">{formatTestingEventStatus(status)}</Badge>
              {!readOnly && status === 'Pending' && application.id ? (
                <Button
                  size="sm"
                  variant="outline"
                  disabled={reviewPending && reviewingApplicationId === application.id}
                  onClick={() => {
                    setReviewingApplicationId(application.id!);
                    startReviewTransition(async () => {
                      const form = new FormData();
                      form.set('eventId', eventId);
                      form.set('applicationId', application.id!);
                      const result = await beginTestingEventApplicationReview(form);
                      setReviewResult({ applicationId: application.id!, result });
                      if (result.success) router.refresh();
                      setReviewingApplicationId(null);
                    });
                  }}
                >
                  {reviewPending && reviewingApplicationId === application.id ? 'Starting...' : 'Review'}
                </Button>
              ) : null}
              {!readOnly && ['Pending', 'UnderReview', 'Waitlisted'].includes(status) && application.id ? (
                <>
                  <EventActionDialog
                    trigger={<Button size="sm"><CheckCircle2 className="mr-2 size-4" />Approve</Button>}
                    title="Approve project application"
                    description="Capacity is reserved only after this approval is accepted."
                    submitLabel="Approve project"
                    action={approveTestingEventApplication}
                  >
                    <input type="hidden" name="eventId" value={eventId} />
                    <input type="hidden" name="applicationId" value={application.id} />
                    <div className="space-y-2">
                      <Label htmlFor={`approve-slot-${application.id}`}>Testing slot</Label>
                      <Select name="slotId" required>
                        <SelectTrigger
                          id={`approve-slot-${application.id}`}
                          aria-label="Testing slot"
                        >
                          <SelectValue placeholder="Choose a slot" />
                        </SelectTrigger>
                        <SelectContent>
                          {slots.filter((slot) => slot.id).map((slot) => (
                            <SelectItem key={slot.id} value={slot.id!}>
                              {new Date(slot.startsAt ?? '').toLocaleString()} · {slot.campusName ?? slot.meetingUrl ?? slot.mode}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor={`approve-rationale-${application.id}`}>Decision notes</Label>
                      <Textarea id={`approve-rationale-${application.id}`} name="rationale" rows={3} />
                    </div>
                  </EventActionDialog>
                  <EventActionDialog
                    trigger={<Button size="sm" variant="outline">Waitlist</Button>}
                    title="Waitlist project application"
                    description="The application remains eligible but does not consume project capacity."
                    submitLabel="Add to waitlist"
                    action={waitlistTestingEventApplication}
                  >
                    <input type="hidden" name="eventId" value={eventId} />
                    <input type="hidden" name="applicationId" value={application.id} />
                    <div className="space-y-2">
                      <Label htmlFor={`waitlist-rationale-${application.id}`}>Notes</Label>
                      <Textarea id={`waitlist-rationale-${application.id}`} name="rationale" rows={3} />
                    </div>
                  </EventActionDialog>
                  <EventActionDialog
                    trigger={<Button size="sm" variant="destructive">Reject</Button>}
                    title="Reject project application"
                    description="A clear rationale is required and remains in the application audit trail."
                    submitLabel="Reject project"
                    action={rejectTestingEventApplication}
                    destructive
                  >
                    <input type="hidden" name="eventId" value={eventId} />
                    <input type="hidden" name="applicationId" value={application.id} />
                    <div className="space-y-2">
                      <Label htmlFor={`reject-rationale-${application.id}`}>Rejection rationale</Label>
                      <Textarea id={`reject-rationale-${application.id}`} name="rationale" required rows={4} />
                    </div>
                  </EventActionDialog>
                  <EventActionDialog
                    trigger={<Button size="sm" variant="ghost"><ShieldCheck className="mr-2 size-4" />Vote</Button>}
                    title="Record committee vote"
                    description="Votes are auditable and cannot be silently replaced by another reviewer."
                    submitLabel="Record vote"
                    action={voteOnTestingEventApplication}
                  >
                    <input type="hidden" name="eventId" value={eventId} />
                    <input type="hidden" name="applicationId" value={application.id} />
                    <div className="space-y-2">
                      <Label>Vote</Label>
                      <Select name="decision" required>
                        <SelectTrigger><SelectValue placeholder="Choose decision" /></SelectTrigger>
                        <SelectContent>
                          <SelectItem value="Approve">Approve</SelectItem>
                          <SelectItem value="Reject">Reject</SelectItem>
                        </SelectContent>
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor={`vote-comments-${application.id}`}>Comments</Label>
                      <Textarea id={`vote-comments-${application.id}`} name="comments" rows={3} />
                    </div>
                  </EventActionDialog>
                </>
              ) : null}
            </div>
          </div>
        );
      })}
    </div>
  );
}

export function TestingSlotRegistrations({
  eventId,
  registrations,
  memberLabels,
  approvedApplications,
  readOnly = false,
}: {
  eventId: string;
  registrations: TestingLabTestingSlotRegistrationProjection[];
  memberLabels: Record<string, string>;
  approvedApplications: TestingLabApprovedApplicationOption[];
  readOnly?: boolean;
}) {
  if (registrations.length === 0) return <p className="text-sm text-muted-foreground">No tester registrations.</p>;
  return (
    <div className="mt-3 divide-y border-t">
      {registrations.map((registration) => {
        const testerLabel = registration.userId ? memberLabels[registration.userId] : undefined;
        const assignableProjects = approvedApplications.filter((application) => !application.slotId || application.slotId === registration.slotId);
        const canAssignProject = ['CheckedIn', 'Attended'].includes(registration.status ?? '');

        return (
          <div key={registration.id} className="flex flex-col gap-3 py-3 lg:flex-row lg:items-center lg:justify-between">
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">{testerLabel ?? 'Unknown tester'}</p>
              <p className="text-xs text-muted-foreground">
                {registration.pendingFeedbackCount ?? 0} pending feedback / {formatTestingEventStatus(registration.status)}
              </p>
            </div>
            {registration.id && !readOnly ? (
              <div className="flex flex-wrap items-center gap-2">
                {canAssignProject && assignableProjects.length > 0 ? (
                  <EventActionDialog
                    trigger={
                      <Button size="sm" variant="outline">
                        <ClipboardCheck className="mr-2 size-4" />
                        Assign tested project
                      </Button>
                    }
                    title="Assign a tested project"
                    description="Create the feedback obligation for this tester after check-in."
                    submitLabel="Assign project"
                    action={assignTestedProjectToRegistration}
                  >
                    <input type="hidden" name="eventId" value={eventId} />
                    <input type="hidden" name="registrationId" value={registration.id} />
                    <div className="space-y-2">
                      <Label>Approved project</Label>
                      <Select name="applicationId" required>
                        <SelectTrigger>
                          <SelectValue placeholder="Choose a project" />
                        </SelectTrigger>
                        <SelectContent>
                          {assignableProjects.map((application) => (
                            <SelectItem key={application.id} value={application.id}>
                              {application.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </div>
                  </EventActionDialog>
                ) : null}
                <form
                  className="flex items-center gap-2"
                  action={async (formData) => {
                    await updateTestingEventAttendance(formData);
                  }}
                >
                  <input type="hidden" name="eventId" value={eventId} />
                  <input type="hidden" name="registrationId" value={registration.id} />
                  <Select name="attendance" required>
                    <SelectTrigger className="w-36">
                      <SelectValue placeholder="Attendance" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="check-in">Check in</SelectItem>
                      <SelectItem value="check-out">Check out</SelectItem>
                      <SelectItem value="no-show">No show</SelectItem>
                      <SelectItem value="complete">Complete</SelectItem>
                    </SelectContent>
                  </Select>
                  <Button size="sm" type="submit">
                    Update
                  </Button>
                </form>
              </div>
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
export function TestingEventLearningDialog({
  event,
  activities,
  readOnly = false,
}: {
  event: TestingLabTestingEventProjection;
  activities: TestingLabLearningActivityOption[];
  readOnly?: boolean;
}) {
  const initialActivity = activities.find((activity) => activity.id === event.learningActivityId);
  const [selectedActivityId, setSelectedActivityId] = useState(initialActivity?.id ?? '');
  const selectedActivity = activities.find((activity) => activity.id === selectedActivityId);

  if (!event.id || readOnly) return null;
  return (
    <EventActionDialog
      trigger={<Button size="sm" variant="outline"><Pencil className="mr-2 size-4" />Configure learning</Button>}
      title="Connect learning evidence"
      description="Testing Lab publishes completion evidence; Learning remains responsible for enrollment and grades."
      submitLabel="Save learning link"
      action={configureTestingEventLearning}
    >
      <input type="hidden" name="eventId" value={event.id} />
      <input type="hidden" name="courseId" value={selectedActivity?.courseId ?? event.courseId ?? ''} />
      <div className="space-y-2">
        <Label>Course activity</Label>
        <Select name="learningActivityId" required value={selectedActivityId} onValueChange={setSelectedActivityId}>
          <SelectTrigger><SelectValue placeholder="Choose a lesson or graded activity" /></SelectTrigger>
          <SelectContent>
            {activities.map((activity) => (
              <SelectItem key={`${activity.courseId}:${activity.id}`} value={activity.id}>{activity.label}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>
      <div className="space-y-2">
        <Label htmlFor="learning-cohort">Cohort id</Label>
        <Input
          id="learning-cohort"
          name="cohortId"
          defaultValue={event.cohortId ?? ''}
          placeholder="Optional: restrict evidence to one cohort"
        />
      </div>
      <div className="space-y-2">
        <Label>Completion requirement</Label>
        <Select name="requirement" defaultValue={event.learningCompletionRequirement ?? 'AttendanceAndFeedback'}>
          <SelectTrigger><SelectValue /></SelectTrigger>
          <SelectContent>
            <SelectItem value="Attendance">Attendance</SelectItem>
            <SelectItem value="Feedback">Required feedback</SelectItem>
            <SelectItem value="AttendanceAndFeedback">Attendance and feedback</SelectItem>
            <SelectItem value="ProjectTested">Assigned project tested</SelectItem>
          </SelectContent>
        </Select>
      </div>
    </EventActionDialog>
  );
}
