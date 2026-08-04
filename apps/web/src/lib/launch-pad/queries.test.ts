import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getToken: vi.fn(),
}));

vi.mock('@/auth', () => ({
  getToken: mocks.getToken,
}));

import { getLaunchProjectOptions, getPlanReadiness, normalizeLaunchStatus, type LaunchPlan } from './queries';

describe('launch pad queries', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
    mocks.getToken.mockResolvedValue('launch-token');
  });

  it('normalizes numeric launch statuses from the API', () => {
    expect(normalizeLaunchStatus(0)).toBe('Draft');
    expect(normalizeLaunchStatus(2)).toBe('Ready');
    expect(normalizeLaunchStatus(99)).toBe('Preparing');
    expect(normalizeLaunchStatus('Launched')).toBe('Launched');
  });

  it('calculates readiness from persisted checklist items when no aggregate is provided', () => {
    const plan: LaunchPlan = {
      id: 'launch-plan-1',
      projectId: 'project-1',
      name: 'Creator launch',
      status: 'Preparing',
      checklistItems: [
        { id: 'item-1', title: 'Storefront approved', category: 'Storefront', isRequired: true, isComplete: true },
        { id: 'item-2', title: 'Release package tested', category: 'Quality', isRequired: true, isComplete: false },
      ],
    };

    expect(getPlanReadiness(plan)).toBe(50);
  });

  it('maps project options from the Projects API without fallback fixtures', async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => [
        {
          id: 'project-1',
          title: 'Arena Tactics',
          slug: 'arena-tactics',
          status: 'Published',
        },
      ],
    });
    vi.stubGlobal('fetch', fetchMock);

    await expect(getLaunchProjectOptions()).resolves.toEqual([
      {
        id: 'project-1',
        title: 'Arena Tactics',
        slug: 'arena-tactics',
        status: 'Published',
      },
    ]);

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:8080/v1/projects?take=50&sortBy=UpdatedAt&sortDirection=DESC',
      expect.objectContaining({
        headers: { Authorization: 'Bearer launch-token' },
        next: { revalidate: 60 },
      }),
    );
  });
});
