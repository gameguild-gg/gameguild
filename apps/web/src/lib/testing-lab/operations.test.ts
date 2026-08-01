import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  requests: {
    getTestingRequests1: vi.fn(),
    getTestingRequests: vi.fn(),
  },
  sessions: {
    getTestingSessions1: vi.fn(),
    getTestingSessionsByRequest: vi.fn(),
    getTestingSessions: vi.fn(),
    getTestingSessionsProjects: vi.fn(),
  },
  locations: { getTestingLocations: vi.fn() },
  participants: {
    getTestingRequestsParticipants: vi.fn(),
    getTestingSessionsRegistrations: vi.fn(),
    getTestingSessionsWaitlist: vi.fn(),
  },
  feedback: { getTestingRequestsFeedback: vi.fn() },
  analytics: { getTestingAnalytics: vi.fn(), getTestingAnalyticsExport: vi.fn() },
  settings: { getApiTestingLabSettings: vi.fn() },
  permissions: { getApiTestingLabPermissionsRoleTemplates: vi.fn() },
}));

vi.mock('@/auth', () => ({ getToken: vi.fn(async () => 'token') }));
vi.mock('@game-guild/client', () => ({
  createServerClient: vi.fn(() => ({})),
  GeneratedApi: {
    TestinglabTestingrequestsModule: vi.fn(() => mocks.requests),
    TestinglabTestingsessionsModule: vi.fn(() => mocks.sessions),
    TestinglabTestinglocationsModule: vi.fn(() => mocks.locations),
    TestinglabTestingparticipantsModule: vi.fn(() => mocks.participants),
    TestinglabTestingfeedbackModule: vi.fn(() => mocks.feedback),
    TestinglabAnalyticsModule: vi.fn(() => mocks.analytics),
    TestinglabSettingsModule: vi.fn(() => mocks.settings),
    TestinglabPermissionModule: vi.fn(() => mocks.permissions),
    ProjectsModule: vi.fn(() => ({})),
  },
}));

import {
  getTestingLabAdministration,
  getTestingLabAnalytics,
  getTestingLabAnalyticsCsv,
  getTestingRequestDetail,
  getTestingSessionDetail,
} from './queries';

describe('Testing Lab operational queries', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.requests.getTestingRequests1.mockResolvedValue({
      ok: true,
      data: { id: 'request-1', title: 'Arena playtest', status: 'Open' },
    });
    mocks.sessions.getTestingSessionsByRequest.mockResolvedValue({
      ok: true,
      data: [{ id: 'session-1', sessionName: 'Friday lab', status: 'Scheduled' }],
    });
    mocks.participants.getTestingRequestsParticipants.mockResolvedValue({
      ok: true,
      data: [{ id: 'participant-1', userId: 'user-1', testingRequestId: 'request-1', status: 'Active' }],
    });
    mocks.feedback.getTestingRequestsFeedback.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'feedback-1',
          testingRequestId: 'request-1',
          userId: 'user-1',
          feedbackData: '{}',
          feedbackFormId: 'form-1',
          testingContext: 'Online',
          overallRating: 4,
          wouldRecommend: true,
        },
      ],
    });
    mocks.sessions.getTestingSessions1.mockResolvedValue({
      ok: true,
      data: { id: 'session-1', sessionName: 'Friday lab', status: 'Scheduled' },
    });
    mocks.participants.getTestingSessionsRegistrations.mockResolvedValue({
      ok: true,
      data: [{ id: 'registration-1', sessionId: 'session-1', userId: 'user-1', status: 'Confirmed' }],
    });
    mocks.participants.getTestingSessionsWaitlist.mockResolvedValue({
      ok: true,
      data: [{ id: 'wait-1', sessionId: 'session-1', userId: 'user-2', position: 1 }],
    });
    mocks.sessions.getTestingSessionsProjects.mockResolvedValue({
      ok: true,
      data: [{ linkId: 'link-1', sessionId: 'session-1', projectId: 'project-1', isActive: true }],
    });
    mocks.settings.getApiTestingLabSettings.mockResolvedValue({
      ok: true,
      data: { labName: 'GameGuild Testing Lab', timezone: 'America/Sao_Paulo' },
    });
    mocks.permissions.getApiTestingLabPermissionsRoleTemplates.mockResolvedValue({
      ok: true,
      data: [{ id: 'role-1', name: 'Facilitator', permissions: { canViewSessions: true } }],
    });
    mocks.requests.getTestingRequests.mockResolvedValue({
      ok: true,
      data: [
        { id: 'request-1', title: 'Arena playtest', status: 'Open', currentTesterCount: 1, maxTesters: 8 },
        { id: 'request-2', title: 'Racing playtest', status: 'Completed', currentTesterCount: 4, maxTesters: 4 },
      ],
    });
    mocks.sessions.getTestingSessions.mockResolvedValue({
      ok: true,
      data: [
        { id: 'session-1', sessionName: 'Friday lab', status: 'Scheduled', registeredTesterCount: 1, maxTesters: 8 },
        { id: 'session-2', sessionName: 'Saturday lab', status: 'Completed', registeredTesterCount: 4, maxTesters: 4 },
      ],
    });
    mocks.locations.getTestingLocations.mockResolvedValue({
      ok: true,
      data: [{ id: 'location-1', name: 'Remote', status: 'Active' }],
    });
    mocks.analytics.getTestingAnalytics.mockResolvedValue({
      ok: true,
      data: {
        fromDate: '2026-07-01T00:00:00.000Z',
        toDate: '2026-07-08T00:00:00.000Z',
        current: {
          events: 2,
          completedEvents: 1,
          applications: 4,
          approvedProjects: 2,
          registeredTesters: 5,
          attendedTesters: 4,
          feedback: 3,
          averageRating: 8.5,
          recommendationRate: 75,
          capacity: 12,
          fillRate: 41.67,
        },
        previous: null,
        locations: { total: 1, active: 1 },
        trend: [],
        events: [],
      },
    });
    mocks.analytics.getTestingAnalyticsExport.mockResolvedValue({ ok: true, data: 'event,applications\nJuly lab,4' });
  });

  it('loads request details with sessions, participants, and feedback', async () => {
    const detail = await getTestingRequestDetail('request-1');

    expect(detail.request?.title).toBe('Arena playtest');
    expect(detail.sessions).toHaveLength(1);
    expect(detail.participants).toHaveLength(1);
    expect(detail.feedback).toHaveLength(1);
    expect(detail.accessIssues).toEqual([]);
  });

  it('loads session details with registrations, waitlist, and projects', async () => {
    const detail = await getTestingSessionDetail('session-1');

    expect(detail.session?.sessionName).toBe('Friday lab');
    expect(detail.registrations).toHaveLength(1);
    expect(detail.waitlist).toHaveLength(1);
    expect(detail.projects).toHaveLength(1);
  });

  it('loads settings and role templates through generated modules', async () => {
    const administration = await getTestingLabAdministration();

    expect(administration.settings?.labName).toBe('GameGuild Testing Lab');
    expect(administration.roles).toEqual([expect.objectContaining({ id: 'role-1', name: 'Facilitator' })]);
  });

  it('loads tenant analytics through one generated CQRS endpoint', async () => {
    const analytics = await getTestingLabAnalytics({
      fromDate: '2026-07-01T00:00:00.000Z',
      toDate: '2026-07-08T00:00:00.000Z',
      includeComparison: true,
    });

    expect(analytics.current.events).toBe(2);
    expect(analytics.current.fillRate).toBe(41.67);
    expect(analytics.current.averageRating).toBe(8.5);
    expect(analytics.accessIssues).toEqual([]);
    expect(mocks.analytics.getTestingAnalytics).toHaveBeenCalledWith({
      fromDate: '2026-07-01T00:00:00.000Z',
      toDate: '2026-07-08T00:00:00.000Z',
      includeComparison: true,
    });
    expect(mocks.feedback.getTestingRequestsFeedback).not.toHaveBeenCalled();
  });

  it('loads the tenant CSV through the authenticated generated client', async () => {
    const result = await getTestingLabAnalyticsCsv({
      fromDate: '2026-07-01T00:00:00.000Z',
      toDate: '2026-07-08T00:00:00.000Z',
    });

    expect(result).toEqual({ data: 'event,applications\nJuly lab,4' });
    expect(mocks.analytics.getTestingAnalyticsExport).toHaveBeenCalledWith({
      fromDate: '2026-07-01T00:00:00.000Z',
      toDate: '2026-07-08T00:00:00.000Z',
    });
  });
});
