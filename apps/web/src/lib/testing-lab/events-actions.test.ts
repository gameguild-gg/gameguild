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
    TestinglabTestingeventsModule: vi.fn(() => mocks.events),
    TestinglabTestingeventparticipationModule: vi.fn(() => ({})),
  },
}));

import {
  addTestingEventCommitteeMember,
  createTestingEvent,
  createTestingEventSlot,
  rejectTestingEventApplication,
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
});
