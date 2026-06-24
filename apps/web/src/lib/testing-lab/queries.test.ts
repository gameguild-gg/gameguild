import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

import {
  countAvailableTesterSlots,
  getTestingLabDashboard,
  getTestingProjectOptions,
  normalizeTestingLocationStatus,
  normalizeTestingRequestStatus,
  normalizeTestingSessionStatus,
} from './queries';

describe('testing lab queries', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    mocks.getToken.mockResolvedValue('testing-token');
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

  it('loads dashboard datasets from versioned Testing Lab APIs', async () => {
    const fetchMock = vi.fn().mockImplementation(async (url: string) => ({
      ok: true,
      status: 200,
      json: async () => {
        if (url.includes('/requests')) return [{ id: 'request-1', title: 'Build test', status: 1 }];
        if (url.includes('/sessions')) return [{ id: 'session-1', sessionName: 'Friday lab', status: 0 }];
        if (url.includes('/locations')) return [{ id: 'location-1', name: 'Remote lab', status: 0 }];
        return [];
      },
    }));
    vi.stubGlobal('fetch', fetchMock);

    const dashboard = await getTestingLabDashboard();

    expect(dashboard.requests).toHaveLength(1);
    expect(dashboard.sessions).toHaveLength(1);
    expect(dashboard.locations).toHaveLength(1);
    expect(dashboard.publicSessions).toHaveLength(1);
    expect(dashboard.accessIssues).toEqual([]);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5295/v1/testing/requests?take=20',
      expect.objectContaining({ headers: expect.objectContaining({ Authorization: 'Bearer testing-token' }) }),
    );
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5295/v1/testing/public/sessions?take=20',
      expect.objectContaining({ headers: { Accept: 'application/json' }, next: { revalidate: 30 } }),
    );
  });

  it('loads Testing Lab project options from the Projects API', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [{ id: 'project-1', title: 'Arena Tactics', slug: 'arena-tactics', status: 'Published' }],
    });
    vi.stubGlobal('fetch', fetchMock);

    const projects = await getTestingProjectOptions();

    expect(projects).toEqual([{ id: 'project-1', title: 'Arena Tactics', slug: 'arena-tactics', status: 'Published' }]);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:5295/v1/projects?take=50&sortBy=UpdatedAt&sortDirection=DESC',
      expect.objectContaining({ headers: expect.objectContaining({ Authorization: 'Bearer testing-token' }) }),
    );
  });
});
