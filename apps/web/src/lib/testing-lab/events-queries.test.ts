import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
  auth: vi.fn(),
  createServerClient: vi.fn(),
  events: {
    getTestingEventsArchived: vi.fn(),
    getTestingEvents: vi.fn(),
    getTestingEventsByEventId: vi.fn(),
    getTestingEventsSlots: vi.fn(),
    getTestingEventsApplicationsByApplicationId: vi.fn(),
    getTestingEventsByEventIdApplications: vi.fn(),
    getTestingEventsCommittee: vi.fn(),
    getTestingEventsPublic: vi.fn(),
    getTestingEventsPublicByEventId: vi.fn(),
    getTestingEventsApplicationsMe: vi.fn(),
  },
  participation: {
    getTestingEventsParticipants: vi.fn(),
    getTestingEventsSlotsRegistrations: vi.fn(),
    getTestingEventsRegistrationsMe: vi.fn(),
    getTestingEventsFeedbackObligationsMe: vi.fn(),
    getTestingEventsFeedback: vi.fn(),
  },
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
  auth: mocks.auth,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
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
  getArchivedTestingEventsDirectory,
  getPublicTestingEventExperience,
  getPublicTestingEventsDirectory,
  getTestingEventFeedbackReview,
  getTestingEventManagerData,
  getTestingEventsDirectory,
  getTestingParticipantDirectory,
} from './events-queries';

describe('Testing Lab event queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getToken.mockResolvedValue('access-token');
    mocks.auth.mockResolvedValue({ tenantId: 'tenant-1', user: { id: 'user-1' } });
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
    mocks.events.getTestingEventsArchived.mockResolvedValue({
      ok: true,
      data: [{ id: 'event-archived', name: 'Archived playtest', status: 'Completed' }],
    });
    mocks.events.getTestingEventsByEventId.mockResolvedValue({
      ok: true,
      data: { id: 'event-1', name: 'Friday campus lab', status: 'ApplicationsOpen' },
    });
    mocks.events.getTestingEventsSlots.mockResolvedValue({
      ok: true,
      data: [{ id: 'slot-1', eventId: 'event-1', registeredTesterCount: 3 }],
    });
    mocks.events.getTestingEventsByEventIdApplications.mockResolvedValue({
      ok: true,
      data: [{ id: 'application-1', eventId: 'event-1', status: 'Pending' }],
    });
    mocks.events.getTestingEventsCommittee.mockResolvedValue({
      ok: true,
      data: [{ id: 'member-1', eventId: 'event-1', userId: 'user-1', userName: 'Reviewer' }],
    });
    mocks.participation.getTestingEventsParticipants.mockResolvedValue({
      ok: true,
      data: {
        items: [
          {
            registrationId: 'registration-1',
            eventId: 'event-1',
            eventName: 'Friday campus lab',
            userId: 'user-1',
            userName: 'Ada Player',
            userEmail: 'ada@example.com',
            status: 'Registered',
          },
        ],
        totalCount: 1,
        registeredCount: 1,
        waitlistedCount: 0,
        checkedInCount: 0,
        attendedCount: 0,
        completedCount: 0,
        noShowCount: 0,
      },
    });
    mocks.participation.getTestingEventsSlotsRegistrations.mockResolvedValue({
      ok: true,
      data: [{ id: 'registration-1', slotId: 'slot-1', status: 'Registered' }],
    });
    mocks.events.getTestingEventsPublic.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'event-1',
          name: 'Friday campus lab',
          status: 'ApplicationsOpen',
          mode: 'InPerson',
          applicationCount: 4,
          slots: [{ id: 'slot-1', availableTesterCount: 7, availableProjectCount: 2 }],
        },
      ],
    });
    mocks.events.getTestingEventsPublicByEventId.mockResolvedValue({
      ok: true,
      data: {
        id: 'event-1',
        name: 'Friday campus lab',
        status: 'ApplicationsOpen',
        slots: [{ id: 'slot-1', availableTesterCount: 7, availableProjectCount: 2 }],
      },
    });
    mocks.events.getTestingEventsApplicationsMe.mockResolvedValue({
      ok: true,
      data: [{ id: 'application-1', eventId: 'event-1', status: 'Pending' }],
    });
    mocks.participation.getTestingEventsRegistrationsMe.mockResolvedValue({
      ok: true,
      data: [{ id: 'registration-1', eventId: 'event-1', slotId: 'slot-1', status: 'Registered' }],
    });
    mocks.participation.getTestingEventsFeedbackObligationsMe.mockResolvedValue({
      ok: true,
      data: [{ id: 'obligation-1', eventId: 'event-1', applicationId: 'application-1', status: 'Pending' }],
    });
    mocks.participation.getTestingEventsFeedback.mockResolvedValue({
      ok: true,
      data: [
        {
          obligationId: 'obligation-1',
          eventId: 'event-1',
          slotId: 'slot-1',
          applicationId: 'application-1',
          testerUserId: 'tester-1',
          status: 'Fulfilled',
          feedback: { id: 'feedback-1', overallRating: 9 },
        },
      ],
    });
  });

  it('loads the manager event directory through the generated event client', async () => {
    const result = await getTestingEventsDirectory({ status: 'ApplicationsOpen', skip: 10, take: 25 });

    expect(result.events).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'http://localhost:8080',
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
    expect(mocks.events.getTestingEvents).toHaveBeenCalledWith({
      status: 'ApplicationsOpen',
      skip: 10,
      take: 25,
    });
  });

  it('loads archived events through the dedicated generated-client operation', async () => {
    const result = await getArchivedTestingEventsDirectory({ skip: 5, take: 20 });

    expect(result.events).toEqual([]);
    expect(result.accessIssues).toEqual([]);
  });

  it('loads the tenant participant directory through one generated-client call', async () => {
    const result = await getTestingParticipantDirectory({
      search: 'Ada',
      status: 'Registered',
      skip: 25,
      take: 25,
    });

    expect(result.directory?.items).toHaveLength(1);
    expect(result.directory?.items?.[0]?.userName).toBe('Ada Player');
    expect(result.accessIssues).toEqual([]);
    expect(mocks.participation.getTestingEventsParticipants).toHaveBeenCalledTimes(1);
    expect(mocks.participation.getTestingEventsParticipants).toHaveBeenCalledWith({
      search: 'Ada',
      status: 'Registered',
      skip: 25,
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
    expect(mocks.events.getTestingEventsByEventIdApplications).toHaveBeenCalledWith('event-1', {
      status: 'Pending',
      skip: 0,
      take: 100,
    });
    expect(mocks.events.getTestingEventsApplicationsByApplicationId).not.toHaveBeenCalled();
    expect(mocks.participation.getTestingEventsSlotsRegistrations).toHaveBeenCalledWith('slot-1');
  });

  it('loads manager feedback review through the generated participation client', async () => {
    const result = await getTestingEventFeedbackReview('event-1');

    expect(result.feedback).toHaveLength(1);
    expect(result.feedback[0]?.feedback?.overallRating).toBe(9);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.participation.getTestingEventsFeedback).toHaveBeenCalledWith('event-1');
  });
  it('keeps partial manager data and reports generated-client failures', async () => {
    mocks.events.getTestingEventsByEventIdApplications.mockResolvedValue({
      ok: false,
      error: { message: 'Forbidden', status: 403 },
    });

    const result = await getTestingEventManagerData('event-1');

    expect(result.event?.id).toBe('event-1');
    expect(result.applications).toEqual([]);
    expect(result.accessIssues).toContain('Applications returned 403: Forbidden');
  });

  it('retains structured generated-client error messages instead of hiding them', async () => {
    mocks.events.getTestingEventsByEventId.mockRejectedValue({
      name: 'ApiError',
      message: 'Response validation failed: event details are malformed',
    });

    const result = await getTestingEventManagerData('event-1');

    expect(result.event).toBeNull();
    expect(result.accessIssues).toContain('Event failed: Response validation failed: event details are malformed');
  });

  it('loads the anonymous public event directory without requiring actor state', async () => {
    mocks.auth.mockResolvedValue(null);

    const result = await getPublicTestingEventsDirectory({ skip: -4, take: 400 });

    expect(result.events).toHaveLength(1);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'http://localhost:8080',
      cache: 'no-store',
    });
    expect(mocks.events.getTestingEventsPublic).toHaveBeenCalledWith({ skip: 0, take: 100 });
    expect(mocks.events.getTestingEventsApplicationsMe).not.toHaveBeenCalled();
  });

  it('loads a public event together with the signed-in actor application, registration, and obligations', async () => {
    const result = await getPublicTestingEventExperience('event-1');

    expect(result.event?.id).toBe('event-1');
    expect(result.applications).toHaveLength(1);
    expect(result.registrations).toHaveLength(1);
    expect(result.feedbackObligations).toHaveLength(1);
    expect(result.isAuthenticated).toBe(true);
    expect(result.accessIssues).toEqual([]);
    expect(mocks.events.getTestingEventsPublicByEventId.mock.invocationCallOrder[0]).toBeLessThan(
      mocks.auth.mock.invocationCallOrder[0]!,
    );
    expect(mocks.createServerClient).toHaveBeenNthCalledWith(1, {
      baseUrl: 'http://localhost:8080',
      cache: 'no-store',
    });
    expect(mocks.createServerClient).toHaveBeenNthCalledWith(2, {
      baseUrl: 'http://localhost:8080',
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
    expect(mocks.events.getTestingEventsApplicationsMe).toHaveBeenCalledWith({ eventId: 'event-1' });
    expect(mocks.participation.getTestingEventsRegistrationsMe).toHaveBeenCalledWith({ eventId: 'event-1' });
    expect(mocks.participation.getTestingEventsFeedbackObligationsMe).toHaveBeenCalledWith({ eventId: 'event-1' });
  });

  it('keeps an anonymous public event readable and skips private self-service calls', async () => {
    mocks.auth.mockResolvedValue(null);

    const result = await getPublicTestingEventExperience('event-1');

    expect(result.event?.id).toBe('event-1');
    expect(result.applications).toEqual([]);
    expect(result.registrations).toEqual([]);
    expect(result.feedbackObligations).toEqual([]);
    expect(result.isAuthenticated).toBe(false);
    expect(mocks.events.getTestingEventsApplicationsMe).not.toHaveBeenCalled();
    expect(mocks.participation.getTestingEventsRegistrationsMe).not.toHaveBeenCalled();
  });
});
