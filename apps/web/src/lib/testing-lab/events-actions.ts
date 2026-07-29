'use server';

import { auth, getToken } from '@/auth';
import {
  createServerClient,
  GeneratedApi,
  type ApiError,
  type Result,
  type TestingLabTestingApplicationVoteDecision,
  type TestingLabTestingEventApprovalMode,
  type TestingLabTestingEventMode,
  type TestingLabTestingLearningCompletionRequirement,
} from '@game-guild/client';
import { revalidatePath } from 'next/cache';

const EVENTS_PATH = '/dashboard/testing-lab/events';

type ActionData<T> = [T] extends [void] ? null : T | null;
export type TestingEventActionResult<T = null> =
  | { success: true; data: ActionData<T>; message: string }
  | { success: false; error: string };

function createModules() {
  const client = createServerClient({
    baseUrl: process.env.API_URL || process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295',
    auth: { getAccessToken: () => getToken() },
    tenant: { getTenantId: async () => (await auth().catch(() => null))?.tenantId ?? null },
  });
  return {
    events: new GeneratedApi.TestinglabTestingeventsModule(client),
    participation: new GeneratedApi.TestinglabTestingeventparticipationModule(client),
  };
}

function text(formData: FormData, key: string) {
  const value = formData.get(key);
  return typeof value === 'string' ? value.trim() : '';
}

function optionalText(formData: FormData, key: string) {
  return text(formData, key) || null;
}

function checked(formData: FormData, key: string) {
  return formData.get(key) === 'on' || formData.get(key) === 'true';
}

function optionalNumber(formData: FormData, key: string) {
  const raw = text(formData, key);
  if (!raw) return null;
  const value = Number(raw);
  return Number.isFinite(value) ? value : null;
}

function isoDate(formData: FormData, key: string) {
  const raw = text(formData, key);
  if (!raw) return null;
  const value = new Date(raw);
  return Number.isNaN(value.valueOf()) ? null : value.toISOString();
}

function revalidateEvent(eventId?: string) {
  revalidatePath('/dashboard/testing-lab');
  revalidatePath(EVENTS_PATH);
  revalidatePath('/testing-lab');
  if (eventId) {
    revalidatePath(`${EVENTS_PATH}/${eventId}`);
    revalidatePath(`/testing-lab/events/${eventId}`);
  }
}

async function complete<T>(
  operation: Promise<Result<T, ApiError>>,
  message: string,
  eventId?: string,
): Promise<TestingEventActionResult<T>> {
  try {
    const result = await operation;
    if (!result.ok) return { success: false, error: result.error.message };
    revalidateEvent(eventId);
    return { success: true, data: (result.data ?? null) as ActionData<T>, message };
  } catch (error) {
    return {
      success: false,
      error: error instanceof Error ? error.message : 'The Testing Lab event operation failed.',
    };
  }
}

function required(formData: FormData, keys: string[], message: string): TestingEventActionResult | null {
  return keys.every((key) => text(formData, key)) ? null : { success: false, error: message };
}

function eventInput(formData: FormData) {
  const applicationsOpenAt = isoDate(formData, 'applicationsOpenAt');
  const applicationsCloseAt = isoDate(formData, 'applicationsCloseAt');
  const startsAt = isoDate(formData, 'startsAt');
  const endsAt = isoDate(formData, 'endsAt');
  if (!applicationsOpenAt || !applicationsCloseAt || !startsAt || !endsAt) return null;
  return {
    name: text(formData, 'name'),
    description: optionalText(formData, 'description'),
    mode: (text(formData, 'mode') || 'Online') as TestingLabTestingEventMode,
    approvalMode: (text(formData, 'approvalMode') || 'ManagerOnly') as TestingLabTestingEventApprovalMode,
    applicationsOpenAt,
    applicationsCloseAt,
    startsAt,
    endsAt,
    requiresFeedback: checked(formData, 'requiresFeedback'),
  };
}

export async function createTestingEvent(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const invalid = required(
    formData,
    ['name', 'applicationsOpenAt', 'applicationsCloseAt', 'startsAt', 'endsAt'],
    'Name, application window, and event schedule are required.',
  );
  if (invalid) return invalid;
  const input = eventInput(formData);
  if (!input) return { success: false, error: 'Enter valid event dates.' };
  return complete(createModules().events.postTestingEvents(input), 'Testing event created.');
}

export async function updateTestingEvent(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const eventId = text(formData, 'eventId');
  const input = eventInput(formData);
  if (!eventId || !input) return { success: false, error: 'Event and valid event details are required.' };
  return complete(createModules().events.putTestingEvents(eventId, input), 'Testing event updated.', eventId);
}

export async function deleteTestingEvent(formData: FormData): Promise<TestingEventActionResult<boolean>> {
  const eventId = text(formData, 'eventId');
  if (!eventId) return { success: false, error: 'Event is required.' };
  return complete(createModules().events.deleteTestingEvents(eventId), 'Draft event deleted.', eventId);
}

type EventTransition =
  | 'open-applications'
  | 'close-applications'
  | 'schedule'
  | 'activate'
  | 'complete'
  | 'cancel';

export async function transitionTestingEvent(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const eventId = text(formData, 'eventId');
  const transition = text(formData, 'transition') as EventTransition;
  if (!eventId || !transition) return { success: false, error: 'Event and transition are required.' };
  const api = createModules().events;
  const operations: Record<EventTransition, () => Promise<Result<{ id?: string }, ApiError>>> = {
    'open-applications': () => api.postTestingEventsOpenApplications(eventId),
    'close-applications': () => api.postTestingEventsCloseApplications(eventId),
    schedule: () => api.postTestingEventsSchedule(eventId),
    activate: () => api.postTestingEventsActivate(eventId),
    complete: () => api.postTestingEventsComplete(eventId),
    cancel: () => api.postTestingEventsCancel(eventId, { reason: text(formData, 'reason') }),
  };
  const operation = operations[transition];
  if (!operation) return { success: false, error: 'Choose a valid event transition.' };
  if (transition === 'cancel' && !text(formData, 'reason'))
    return { success: false, error: 'A cancellation reason is required.' };
  return complete(operation(), 'Event status updated.', eventId);
}

function slotInput(formData: FormData) {
  const mode = (text(formData, 'mode') || 'Online') as TestingLabTestingEventMode;
  const startsAt = isoDate(formData, 'startsAt');
  const endsAt = isoDate(formData, 'endsAt');
  if (!startsAt || !endsAt) return { error: 'Enter a valid slot schedule.' } as const;
  if (mode === 'InPerson' && (!text(formData, 'campusName') || !text(formData, 'roomName')))
    return { error: 'Campus and room are required for in-person slots.' } as const;
  if (mode === 'Online' && !text(formData, 'meetingUrl'))
    return { error: 'A meeting URL is required for online slots.' } as const;
  return {
    data: {
      mode,
      startsAt,
      endsAt,
      maxTesters: optionalNumber(formData, 'maxTesters'),
      maxProjects: optionalNumber(formData, 'maxProjects'),
      campusName: optionalText(formData, 'campusName'),
      roomName: optionalText(formData, 'roomName'),
      meetingUrl: optionalText(formData, 'meetingUrl'),
      locationId: optionalText(formData, 'locationId'),
    },
  } as const;
}

export async function createTestingEventSlot(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const eventId = text(formData, 'eventId');
  if (!eventId) return { success: false, error: 'Event is required.' };
  const input = slotInput(formData);
  if ('error' in input && input.error) return { success: false, error: input.error };
  return complete(createModules().events.postTestingEventsSlots(eventId, input.data), 'Event slot created.', eventId);
}

export async function updateTestingEventSlot(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const eventId = text(formData, 'eventId');
  const slotId = text(formData, 'slotId');
  if (!eventId || !slotId) return { success: false, error: 'Event and slot are required.' };
  const input = slotInput(formData);
  if ('error' in input && input.error) return { success: false, error: input.error };
  return complete(createModules().events.putTestingEventsSlots(eventId, slotId, input.data), 'Event slot updated.', eventId);
}

export async function deleteTestingEventSlot(formData: FormData): Promise<TestingEventActionResult<boolean>> {
  const eventId = text(formData, 'eventId');
  const slotId = text(formData, 'slotId');
  if (!eventId || !slotId) return { success: false, error: 'Event and slot are required.' };
  return complete(createModules().events.deleteTestingEventsSlots(eventId, slotId), 'Event slot removed.', eventId);
}

export async function addTestingEventCommitteeMember(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const eventId = text(formData, 'eventId');
  const userId = text(formData, 'userId');
  if (!eventId || !userId) return { success: false, error: 'Event and reviewer are required.' };
  return complete(
    createModules().events.postTestingEventsCommittee(eventId, { userId, isChair: checked(formData, 'isChair') }),
    'Committee member added.',
    eventId,
  );
}

export async function removeTestingEventCommitteeMember(formData: FormData): Promise<TestingEventActionResult<boolean>> {
  const eventId = text(formData, 'eventId');
  const userId = text(formData, 'userId');
  if (!eventId || !userId) return { success: false, error: 'Event and reviewer are required.' };
  return complete(createModules().events.deleteTestingEventsCommittee(eventId, userId), 'Committee member removed.', eventId);
}

export async function configureTestingEventLearning(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const invalid = required(
    formData,
    ['eventId', 'courseId', 'learningActivityId', 'requirement'],
    'Event, course, activity, and completion requirement are required.',
  );
  if (invalid) return invalid;
  const eventId = text(formData, 'eventId');
  return complete(
    createModules().events.putTestingEventsLearning(eventId, {
      courseId: text(formData, 'courseId'),
      cohortId: optionalText(formData, 'cohortId'),
      learningActivityId: text(formData, 'learningActivityId'),
      requirement: text(formData, 'requirement') as TestingLabTestingLearningCompletionRequirement,
    }),
    'Learning evidence configured.',
    eventId,
  );
}

export async function submitTestingProjectApplication(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const eventId = text(formData, 'eventId');
  const projectId = text(formData, 'projectId');
  if (!eventId || !projectId) return { success: false, error: 'Event and existing project are required.' };
  return complete(
    createModules().events.postTestingEventsApplications(eventId, {
      projectId,
      projectVersionId: optionalText(formData, 'projectVersionId'),
      preferredAvailability: optionalText(formData, 'preferredAvailability'),
    }),
    'Project application submitted.',
    eventId,
  );
}

export async function withdrawTestingProjectApplication(formData: FormData): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  if (!applicationId) return { success: false, error: 'Application is required.' };
  return complete(
    createModules().events.postTestingEventsApplicationsWithdraw(applicationId),
    'Project application withdrawn.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function beginTestingEventApplicationReview(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  if (!applicationId) return { success: false, error: 'Application is required.' };
  return complete(
    createModules().events.postTestingEventsApplicationsReview(applicationId),
    'Application review started.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function voteOnTestingEventApplication(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  const decision = text(formData, 'decision') as TestingLabTestingApplicationVoteDecision;
  if (!applicationId || !decision) return { success: false, error: 'Application and vote are required.' };
  return complete(
    createModules().events.postTestingEventsApplicationsVotes(applicationId, {
      decision,
      comments: optionalText(formData, 'comments'),
    }),
    'Committee vote recorded.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function approveTestingEventApplication(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  const slotId = text(formData, 'slotId');
  if (!applicationId || !slotId) return { success: false, error: 'Application and slot are required for approval.' };
  return complete(
    createModules().events.postTestingEventsApplicationsApprove(applicationId, {
      slotId,
      rationale: optionalText(formData, 'rationale'),
    }),
    'Project application approved.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function rejectTestingEventApplication(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  const rationale = text(formData, 'rationale');
  if (!applicationId) return { success: false, error: 'Application is required.' };
  if (!rationale) return { success: false, error: 'A rejection rationale is required.' };
  return complete(
    createModules().events.postTestingEventsApplicationsReject(applicationId, { slotId: null, rationale }),
    'Project application rejected.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function waitlistTestingEventApplication(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  if (!applicationId) return { success: false, error: 'Application is required.' };
  return complete(
    createModules().events.postTestingEventsApplicationsWaitlist(applicationId, {
      slotId: null,
      rationale: optionalText(formData, 'rationale'),
    }),
    'Project application waitlisted.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function assignTestingEventApplicationSlot(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const applicationId = text(formData, 'applicationId');
  const slotId = text(formData, 'slotId');
  if (!applicationId || !slotId) return { success: false, error: 'Application and slot are required.' };
  return complete(
    createModules().events.putTestingEventsApplicationsSlot(applicationId, { slotId }),
    'Application slot updated.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function registerForTestingEventSlot(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const slotId = text(formData, 'slotId');
  if (!slotId) return { success: false, error: 'Slot is required.' };
  return complete(
    createModules().participation.postTestingEventsSlotsRegistrations(slotId, {
      notes: optionalText(formData, 'notes'),
    }),
    'Testing slot registration submitted.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}

export async function updateTestingEventAttendance(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const registrationId = text(formData, 'registrationId');
  const attendance = text(formData, 'attendance');
  if (!registrationId || !attendance) return { success: false, error: 'Registration and attendance action are required.' };
  const api = createModules().participation;
  const operations: Record<string, () => Promise<Result<{ id?: string }, ApiError>>> = {
    'check-in': () => api.postTestingEventsRegistrationsCheckIn(registrationId),
    'check-out': () => api.postTestingEventsRegistrationsCheckOut(registrationId),
    'no-show': () => api.postTestingEventsRegistrationsNoShow(registrationId),
    complete: () => api.postTestingEventsRegistrationsComplete(registrationId),
  };
  const operation = operations[attendance];
  if (!operation) return { success: false, error: 'Choose a valid attendance action.' };
  return complete(operation(), 'Attendance updated.', optionalText(formData, 'eventId') ?? undefined);
}

export async function assignTestedProjectToRegistration(
  formData: FormData,
): Promise<TestingEventActionResult<{ id?: string }>> {
  const registrationId = text(formData, 'registrationId');
  const applicationId = text(formData, 'applicationId');
  if (!registrationId || !applicationId) return { success: false, error: 'Registration and approved project are required.' };
  return complete(
    createModules().participation.postTestingEventsRegistrationsTestedProjects(registrationId, { applicationId }),
    'Tested project assigned.',
    optionalText(formData, 'eventId') ?? undefined,
  );
}
