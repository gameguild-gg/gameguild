import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  auth: vi.fn(),
  createServerClient: vi.fn(),
  events: {
    getTestingEvents: vi.fn(),
    getTestingEvents1: vi.fn(),
    getTestingEventsSlots: vi.fn(),
    getTestingEventsApplications: vi.fn(),
    getTestingEventsCommittee: vi.fn(),
  },
  participation: {
    getTestingEventsSlotsRegistrations: vi.fn(),
    getTestingEventsFeedbackObligationsMe: vi.fn(),
  },
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
  auth: mocks.auth,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    TestinglabTestingeventsModule: vi.fn(() => mocks.events),
    TestinglabTestingeventparticipationModule: vi.fn(() => mocks.participation),
  },
}));

import { getTestingEventManagerData, getTestingEventsDirectory } from './events-queries';

describe('Testing Lab event queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.auth.mockResolvedValue({ tenantId: 'tenant-1' });
    mocks.createServerClient.mockReturnValue({ kind: 'server-client' });
    mocks.events.getTestingEvents.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'event-1',
          name: 'Friday campus lab',
          status: 'ApplicationsOpen',
          mode: 'InPerson',
          slotCount: 2,
          applicationCount: 4,
        },
      ],
    });
    mocks.events.getTestingEvents1.mockResolvedValue({
      ok: true,
      data: { id: 'event-1', name: 'Friday campus lab', status: 'ApplicationsOpen' },
    });
    mocks.events.getTestingEventsSlots.mockResolvedValue({
      ok: true,
      data: [{ id: 'slot-1', eventId: 'event-1', registeredTesterCount: 3 }],
    });
    mocks.events.getTestingEventsApplications.mockResolvedValue({
      ok: true,
      data: [{ id: 'application-1', eventId: 'event-1', status: 'Pending' }],
    });
    mocks.events.getTestingEventsCommittee.mockResolvedValue({
      ok: true,
      data: [{ id: 'member-1', eventId: 'event-1', userId: 'user-1', userName: 'Reviewer' }],
    });
    mocks.participation.getTestingEventsSlotsRegistrations.mockResolvedValue({
      ok: true,
      data: [{ id: 'registration-1', slotId: 'slot-1', status: 'Registered' }],
    });
  });

  it('loads the manager event directory through the generated event client', async () => {
    const result = await getTestingEventsDirectory({ status: 'ApplicationsOpen', skip: 10, take: 25 });

    expect(result.events).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'http://localhost:5295',
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
    expect(mocks.events.getTestingEvents).toHaveBeenCalledWith({
      status: 'ApplicationsOpen',
      skip: 10,
      take: 25,
    });
  });

  it('loads event, slots, applications, committee, and registrations as one manager view', async () => {
    const result = await getTestingEventManagerData('event-1', { applicationStatus: 'Pending' });

    expect(result.event?.id).toBe('event-1');
    expect(result.slots).toHaveLength(1);
    expect(result.applications).toHaveLength(1);
    expect(result.committee).toHaveLength(1);
    expect(result.registrationsBySlot['slot-1']).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.events.getTestingEventsApplications).toHaveBeenCalledWith('event-1', {
      status: 'Pending',
      skip: 0,
      take: 100,
    });
    expect(mocks.participation.getTestingEventsSlotsRegistrations).toHaveBeenCalledWith('slot-1');
  });

  it('keeps partial manager data and reports generated-client failures', async () => {
    mocks.events.getTestingEventsApplications.mockResolvedValue({
      ok: false,
      error: { message: 'Forbidden', status: 403 },
    });

    const result = await getTestingEventManagerData('event-1');

    expect(result.event?.id).toBe('event-1');
    expect(result.applications).toEqual([]);
    expect(result.accessIssues).toContain('Applications returned 403: Forbidden');
  });
});
