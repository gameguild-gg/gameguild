import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  auth: vi.fn(),
  revalidatePath: vi.fn(),
  events: {
    postTestingEvents: vi.fn(),
    postTestingEventsSlots: vi.fn(),
    postTestingEventsApplicationsReject: vi.fn(),
    postTestingEventsOpenApplications: vi.fn(),
    postTestingEventsCommittee: vi.fn(),
  },
  participation: {
    deleteTestingEventsRegistrations: vi.fn(),
    postTestingEventsFeedbackObligationsFeedback: vi.fn(),
  },
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
  auth: mocks.auth,
}));

vi.mock('next/cache', () => ({
  revalidatePath: mocks.revalidatePath,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    TestinglabTestingeventsModule: vi.fn(function TestinglabTestingeventsModule() {
      return mocks.events;
    }),
    TestinglabTestingeventparticipationModule: vi.fn(function TestinglabTestingeventparticipationModule() {
      return mocks.participation;
    }),
  },
}));

import {
  addTestingEventCommitteeMember,
  cancelTestingEventRegistration,
  createTestingEvent,
  createTestingEventSlot,
  rejectTestingEventApplication,
  submitTestingEventFeedback,
  transitionTestingEvent,
} from './events-actions';

function form(values: Record<string, string>) {
  const data = new FormData();
  Object.entries(values).forEach(([key, value]) => data.set(key, value));
  return data;
}

describe('Testing Lab event actions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.auth.mockResolvedValue({ tenantId: 'tenant-1' });
  });

  it('creates an event through the generated client with normalized UTC dates', async () => {
    mocks.events.postTestingEvents.mockResolvedValue({
      ok: true,
      data: { id: 'event-1', name: 'Campus showcase' },
    });

    const result = await createTestingEvent(
      form({
        name: 'Campus showcase',
        description: 'Student projects',
        mode: 'InPerson',
        approvalMode: 'Committee',
        applicationsOpenAt: '2026-08-01T09:00',
        applicationsCloseAt: '2026-08-05T18:00',
        startsAt: '2026-08-08T18:00',
        endsAt: '2026-08-08T21:00',
        requiresFeedback: 'on',
      }),
    );

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEvents).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Campus showcase',
        mode: 'InPerson',
        approvalMode: 'Committee',
        requiresFeedback: true,
        startsAt: expect.stringMatching(/^2026-08-08T/),
      }),
    );
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/dashboard/testing-lab/events');
  });

  it('forwards a weekly recurrence to the generated client', async () => {
    mocks.events.postTestingEvents.mockResolvedValue({ ok: true, data: { id: 'event-2' } });
    const input = form({
      name: 'Weekly playtest',
      mode: 'Online',
      approvalMode: 'ManagerOnly',
      applicationsOpenAt: '2026-08-01T09:00',
      applicationsCloseAt: '2026-08-02T18:00',
      startsAt: '2026-08-03T18:00',
      endsAt: '2026-08-03T20:00',
      recurrenceFrequency: 'Weekly',
      recurrenceInterval: '1',
      recurrenceEndMode: 'count',
      recurrenceOccurrenceCount: '3',
    });
    input.append('recurrenceDaysOfWeek', 'Monday');

    const result = await createTestingEvent(input);

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEvents).toHaveBeenCalledWith(
      expect.objectContaining({
        recurrence: { frequency: 'Weekly', interval: 1, daysOfWeek: ['Monday'], occurrenceCount: 3, endsAt: null },
      }),
    );
  });

  it('requires campus and room for an in-person slot before calling the API', async () => {
    const result = await createTestingEventSlot(
      form({
        eventId: 'event-1',
        mode: 'InPerson',
        startsAt: '2026-08-08T18:00',
        endsAt: '2026-08-08T20:00',
      }),
    );

    expect(result).toEqual({ success: false, error: 'Campus and room are required for in-person slots.' });
    expect(mocks.events.postTestingEventsSlots).not.toHaveBeenCalled();
  });

  it('requires a rationale to reject a project application', async () => {
    const result = await rejectTestingEventApplication(form({ applicationId: 'application-1', rationale: ' ' }));

    expect(result).toEqual({ success: false, error: 'A rejection rationale is required.' });
    expect(mocks.events.postTestingEventsApplicationsReject).not.toHaveBeenCalled();
  });

  it('maps an event lifecycle operation to its generated client method', async () => {
    mocks.events.postTestingEventsOpenApplications.mockResolvedValue({
      ok: true,
      data: { id: 'event-1', status: 'ApplicationsOpen' },
    });

    const result = await transitionTestingEvent(form({ eventId: 'event-1', transition: 'open-applications' }));

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEventsOpenApplications).toHaveBeenCalledWith('event-1');
  });

  it('adds a committee member through the generated event client', async () => {
    mocks.events.postTestingEventsCommittee.mockResolvedValue({
      ok: true,
      data: { id: 'member-1', userId: 'user-1', isChair: true },
    });

    const result = await addTestingEventCommitteeMember(
      form({ eventId: 'event-1', userId: 'user-1', isChair: 'on' }),
    );

    expect(result.success).toBe(true);
    expect(mocks.events.postTestingEventsCommittee).toHaveBeenCalledWith('event-1', {
      userId: 'user-1',
      isChair: true,
    });
  });

  it('cancels the current tester registration through the participation client', async () => {
    mocks.participation.deleteTestingEventsRegistrations.mockResolvedValue({
      ok: true,
      data: true,
    });

    const result = await cancelTestingEventRegistration(
      form({ eventId: 'event-1', registrationId: 'registration-1' }),
    );

    expect(result.success).toBe(true);
    expect(mocks.participation.deleteTestingEventsRegistrations).toHaveBeenCalledWith('registration-1');
    expect(mocks.revalidatePath).toHaveBeenCalledWith('/testing-lab/events/event-1');
  });

  it('submits required structured feedback for an assigned project', async () => {
    mocks.participation.postTestingEventsFeedbackObligationsFeedback.mockResolvedValue({
      ok: true,
      data: { id: 'feedback-1' },
    });

    const result = await submitTestingEventFeedback(
      form({
        eventId: 'event-1',
        obligationId: 'obligation-1',
        feedbackData: 'The onboarding and controls are clear.',
        overallRating: '8',
        wouldRecommend: 'on',
        additionalNotes: 'Retest after the tutorial polish.',
      }),
    );

    expect(result.success).toBe(true);
    expect(mocks.participation.postTestingEventsFeedbackObligationsFeedback).toHaveBeenCalledWith(
      'obligation-1',
      {
        feedbackData: 'The onboarding and controls are clear.',
        overallRating: 8,
        wouldRecommend: true,
        additionalNotes: 'Retest after the tutorial polish.',
      },
    );
  });

  it('validates feedback before calling the participation client', async () => {
    const result = await submitTestingEventFeedback(
      form({ obligationId: 'obligation-1', feedbackData: ' ', overallRating: '11' }),
    );

    expect(result).toEqual({
      success: false,
      error: 'Structured feedback and a rating from 1 to 10 are required.',
    });
    expect(mocks.participation.postTestingEventsFeedbackObligationsFeedback).not.toHaveBeenCalled();
  });
});
