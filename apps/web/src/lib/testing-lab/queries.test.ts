import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  feedback: { getTestingFeedback: vi.fn() },
  auth: vi.fn(),
  getToken: vi.fn(),
  createServerClient: vi.fn(),
  requests: { getTestingRequests: vi.fn(), getTestingRequestsById: vi.fn() },
  sessions: {
    getTestingSessions: vi.fn(),
    getTestingPublicSessions: vi.fn(),
  },
  locations: { getTestingLocations: vi.fn() },
  analytics: {},
  projects: { getProjects: vi.fn(), getProjectsById: vi.fn() },
}));

vi.mock('@/auth', () => ({
  auth: mocks.auth,
  getToken: mocks.getToken,
}));

vi.mock('@game-guild/client', () => ({
  createServerClient: mocks.createServerClient,
  GeneratedApi: {
    TestinglabTestingrequestsModule: vi.fn(function TestinglabTestingrequestsModule() {
      return mocks.requests;
    }),
    TestinglabTestingsessionsModule: vi.fn(function TestinglabTestingsessionsModule() {
      return mocks.sessions;
    }),
    TestinglabTestinglocationsModule: vi.fn(function TestinglabTestinglocationsModule() {
      return mocks.locations;
    }),
    TestinglabTestingparticipantsModule: vi.fn(function TestinglabTestingparticipantsModule() {
      return {};
    }),
    TestinglabTestingfeedbackModule: vi.fn(function TestinglabTestingfeedbackModule() {
      return mocks.feedback;
    }),
    TestinglabTestinganalyticsModule: vi.fn(function TestinglabTestinganalyticsModule() {
      return mocks.analytics;
    }),
    TestinglabSettingsModule: vi.fn(function TestinglabSettingsModule() {
      return {};
    }),
    TestinglabPermissionModule: vi.fn(function TestinglabPermissionModule() {
      return {};
    }),
    ProjectsModule: vi.fn(function ProjectsModule() {
      return mocks.projects;
    }),
  },
}));

import {
  countAvailableTesterSlots,
  filterTestingLabLocations,
  getPublicTestingLabDirectory,
  getTestingLabDashboard,
  getTestingLabLocations,
  getTestingLabProjectDetail,
  getTestingProjectOptions,
  normalizeTestingLocationStatus,
  normalizeTestingRequestStatus,
  normalizeTestingSessionStatus,
} from './queries';

describe('testing lab queries', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    mocks.auth.mockResolvedValue({ tenantId: 'tenant-1' });
    mocks.getToken.mockResolvedValue('testing-token');
    mocks.createServerClient.mockReturnValue({ kind: 'server-client' });
    mocks.feedback.getTestingFeedback.mockResolvedValue({
      ok: true,
      data: {
        items: [{ id: 'feedback-1', source: 'Event', eventName: 'Showcase', userId: 'user-1', feedbackData: '{}', testingContext: 'Online', isReported: false }],
        totalCount: 1,
        skip: 0,
        take: 20,
      },
    });
    mocks.requests.getTestingRequests.mockResolvedValue({
      ok: true,
      data: [{ id: 'request-1', title: 'Build test', status: 'Open' }],
    });
    mocks.sessions.getTestingSessions.mockResolvedValue({
      ok: true,
      data: [{ id: 'session-1', sessionName: 'Friday lab', status: 'Scheduled' }],
    });
    mocks.sessions.getTestingPublicSessions.mockResolvedValue({
      ok: true,
      data: [{ id: 'session-1', sessionName: 'Friday lab', status: 'Scheduled' }],
    });
    mocks.locations.getTestingLocations.mockResolvedValue({
      ok: true,
      data: [{ id: 'location-1', name: 'Remote lab', status: 'Active' }],
    });
    mocks.projects.getProjects.mockResolvedValue({
      ok: true,
      data: [{ id: 'project-1', tenantId: 'tenant-1', title: 'Arena Tactics', slug: 'arena-tactics', status: 'Published' }],
    });
    mocks.projects.getProjectsById.mockResolvedValue({
      ok: true,
      data: {
        id: 'project-1',
        title: 'Arena Tactics',
        slug: 'arena-tactics',
        status: 'Published',
        shortDescription: 'A shared project ready for testing.',
        developmentStatus: 'InDevelopment',
      },
    });
  });

  it('normalizes status values from API enums', () => {
    expect(normalizeTestingRequestStatus(0)).toBe('Draft');
    expect(normalizeTestingRequestStatus(3)).toBe('In Progress');
    expect(normalizeTestingRequestStatus('Completed')).toBe('Completed');
    expect(normalizeTestingSessionStatus(1)).toBe('Active');
    expect(normalizeTestingLocationStatus(2)).toBe('Inactive');
  });

  it('counts available capped tester slots', () => {
    expect(
      countAvailableTesterSlots([
        { id: 'request-1', title: 'One', status: 'Open', maxTesters: 10, currentTesterCount: 3 },
        { id: 'request-2', title: 'Two', status: 'Open', maxTesters: null, currentTesterCount: 2 },
        { id: 'request-3', title: 'Three', status: 'Open', maxTesters: 5, currentTesterCount: 9 },
      ]),
    ).toBe(7);
  });

  it('filters locations by query, lifecycle, and delivery mode', () => {
    const locations = [
      { id: 'physical', name: 'São Paulo Campus', city: 'São Paulo', country: 'Brazil', isVirtual: false, status: 'Active' as const },
      { id: 'remote', name: 'Global Remote Lab', isVirtual: true, status: 'Maintenance' as const },
      { id: 'archived', name: 'Legacy Room', isVirtual: false, status: 'Inactive' as const, isDeleted: true },
    ];

    expect(filterTestingLabLocations(locations, { q: 'paulo', status: 'active', mode: 'physical' })).toEqual([locations[0]]);
    expect(filterTestingLabLocations(locations, { status: 'archived', mode: 'all' })).toEqual([locations[2]]);
    expect(filterTestingLabLocations(locations, { status: 'all', mode: 'remote' })).toEqual([locations[1]]);
  });
  it('loads dashboard datasets through generated Testing Lab modules', async () => {
    const dashboard = await getTestingLabDashboard();

    expect(dashboard.requests).toHaveLength(1);
    expect(dashboard.sessions).toHaveLength(1);
    expect(dashboard.locations).toHaveLength(1);
    expect(dashboard.publicSessions).toHaveLength(1);
    expect(dashboard.accessIssues).toEqual([]);
    expect(mocks.createServerClient).toHaveBeenCalledWith({
      baseUrl: 'http://localhost:8080',
      auth: { getAccessToken: expect.any(Function) },
      tenant: { getTenantId: expect.any(Function) },
    });
    expect(mocks.requests.getTestingRequests).toHaveBeenCalledWith({ skip: 0, take: 200, includeArchived: true });
    expect(mocks.sessions.getTestingSessions).toHaveBeenCalledWith({ skip: 0, take: 200 });
    expect(mocks.sessions.getTestingPublicSessions).toHaveBeenCalledWith({ take: 200 });
    expect(mocks.locations.getTestingLocations).toHaveBeenCalledWith({ skip: 0, take: 200 });
  });

  it('keeps project context from the stable Testing Lab request projection', async () => {
    mocks.requests.getTestingRequests.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'request-1',
          title: 'Arena playtest',
          status: 'Open',
          projectVersionId: 'version-1',
          projectVersion: {
            id: 'version-1',
            projectId: 'project-1',
            versionNumber: '1.0.0',
            status: 'Published',
            project: {
              id: 'project-1',
              title: 'Arena Tactics',
              slug: 'arena-tactics',
            },
          },
        },
      ],
    });

    const dashboard = await getTestingLabDashboard();

    expect(dashboard.requests[0]?.projectVersion).toEqual({
      id: 'version-1',
      projectId: 'project-1',
      versionNumber: '1.0.0',
      status: 'Published',
      project: {
        id: 'project-1',
        title: 'Arena Tactics',
        name: undefined,
        slug: 'arena-tactics',
        status: undefined,
      },
    });
  });

  it('loads unified feedback in one tenant-scoped API request', async () => {
    const { getTestingFeedbackDirectory } = await import('./queries');

    const directory = await getTestingFeedbackDirectory({
      q: 'showcase',
      source: 'event',
      reported: false,
      skip: 0,
      take: 20,
    });

    expect(directory.items).toHaveLength(1);
    expect(directory.totalCount).toBe(1);
    expect(mocks.feedback.getTestingFeedback).toHaveBeenCalledWith({
      Search: 'showcase',
      Source: 'Event',
      EventId: undefined,
      RequestId: undefined,
      UserId: undefined,
      Reported: false,
      Quality: undefined,
      Skip: 0,
      Take: 20,
    });
  });

  it('loads the administration location directory including archived locations', async () => {
    mocks.locations.getTestingLocations.mockResolvedValue({
      ok: true,
      data: [
        {
          id: 'location-archived',
          name: 'Legacy lab',
          status: 'Inactive',
          isDeleted: true,
          state: 'SP',
          postalCode: '01000-000',
          contactEmail: 'lab@gameguild.gg',
          contactPhone: '+55 11 5555-0100',
        },
      ],
    });

    const directory = await getTestingLabLocations();

    expect(mocks.locations.getTestingLocations).toHaveBeenCalledWith({ skip: 0, take: 200, includeArchived: true });
    expect(directory.locations).toEqual([
      expect.objectContaining({
        id: 'location-archived',
        isDeleted: true,
        state: 'SP',
        postalCode: '01000-000',
        contactEmail: 'lab@gameguild.gg',
        contactPhone: '+55 11 5555-0100',
      }),
    ]);
  });
  it('loads the public Testing Lab catalog through generated modules', async () => {
    const directory = await getPublicTestingLabDirectory();

    expect(directory.sessions).toHaveLength(1);
    expect(directory.projects).toEqual([{ id: 'project-1', title: 'Arena Tactics', slug: 'arena-tactics', status: 'Published' }]);
    expect(directory.accessIssues).toEqual([]);
    expect(mocks.sessions.getTestingPublicSessions).toHaveBeenCalledWith({ take: 100 });
    expect(mocks.projects.getProjects).toHaveBeenCalledWith({
      skip: 0,
      take: 100,
      sortBy: 'UpdatedAt',
      sortDirection: 'DESC',
    });
  });

  it('loads Testing Lab project options through the generated Projects module', async () => {
    mocks.projects.getProjects.mockResolvedValue({
      ok: true,
      data: [
        { id: 'project-1', tenantId: 'tenant-1', title: 'Arena Tactics', slug: 'arena-tactics', status: 'Published' },
        { id: 'project-2', tenantId: 'tenant-2', title: 'Other tenant project', slug: 'other-tenant-project', status: 'Published' },
        { id: 'global-project', tenantId: null, title: 'Global showcase', slug: 'global-showcase', status: 'Published' },
      ],
    });

    const projects = await getTestingProjectOptions();

    expect(projects).toEqual([{ id: 'project-1', title: 'Arena Tactics', slug: 'arena-tactics', status: 'Published' }]);
    expect(mocks.projects.getProjects).toHaveBeenCalledWith({
      currentTenantOnly: true,
      skip: 0,
      take: 50,
      sortBy: 'UpdatedAt',
      sortDirection: 'DESC',
    });
  });

  it('resolves a shared project without treating it as a missing Testing Lab request', async () => {
    mocks.requests.getTestingRequests.mockResolvedValue({ ok: true, data: [] });

    const detail = await getTestingLabProjectDetail('project-1');

    expect(detail.project).toMatchObject({
      id: 'project-1',
      title: 'Arena Tactics',
      description: 'A shared project ready for testing.',
    });
    expect(detail.request).toBeNull();
    expect(detail.accessIssues).toEqual([]);
    expect(mocks.projects.getProjectsById).toHaveBeenCalledWith('project-1', {
      includeTeam: false,
      includeReleases: true,
      includeCollaborators: false,
      includeStatistics: true,
    });
    expect(mocks.requests.getTestingRequestsById).not.toHaveBeenCalled();
  });
});
