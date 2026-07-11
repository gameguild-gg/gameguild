import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({ getToken: vi.fn() }));

vi.mock('@/auth', () => ({ getToken: mocks.getToken }));

import { learningApiGet } from './http';

describe('learningApiGet', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    })));
  });

  it('never caches authenticated dashboard data', async () => {
    mocks.getToken.mockResolvedValue('access-token');

    await learningApiGet('/v1/assessments/course/course-1/groups', 60);

    expect(fetch).toHaveBeenCalledWith('http://localhost:5295/v1/assessments/course/course-1/groups', {
      headers: { Authorization: 'Bearer access-token' },
      cache: 'no-store',
    });
  });

  it('retains bounded revalidation for anonymous public data', async () => {
    mocks.getToken.mockResolvedValue(null);

    await learningApiGet('/v1/courses', 120);

    expect(fetch).toHaveBeenCalledWith('http://localhost:5295/v1/courses', {
      headers: undefined,
      next: { revalidate: 120 },
    });
  });
});
